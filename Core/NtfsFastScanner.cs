// Chris Deliga / CNS Capecter NetworXs System / atlaya.capecter.com
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace AtlayaView.Core;

/// <summary>
/// Schneller NTFS-Scan über den USN-Änderungsjournal-Mechanismus statt ordnerweiser
/// Win32-Verzeichnisauflistung.
///
/// Zwei Bausteine:
///  - <see cref="TryFullVolumeScan"/>: liest einmal sequenziell alle Datensätze des Volumes
///    (FSCTL_ENUM_USN_DATA – dieselbe Systemschnittstelle, über die z. B. auch die
///    USN-Journal-Indizierung läuft). Liefert Namen/Struktur in einem Rutsch, deutlich
///    billiger als N einzelne Verzeichnis-Aufrufe. Dateigrößen stehen in den USN-Datensätzen
///    NICHT drin (das wäre nur mit rohem MFT-$DATA-Parsing zu haben) und werden danach per
///    leichtgewichtigem <c>GetFileAttributesEx</c> pro Datei nachgeladen, parallelisiert.
///  - <see cref="TryIncrementalUpdate"/>: liest nur die Änderungen seit dem letzten Scan
///    (FSCTL_READ_USN_JOURNAL) und listet gezielt nur die betroffenen Ordner neu
///    (Directory.EnumerateFileSystemEntries – dieselbe, bereits bewährte API wie im normalen
///    Scanner) statt das ganze Volume erneut zu lesen.
///
/// Beides erfordert einen Volume-Handle (\\.\C:), der laut Microsoft-Dokumentation
/// SE_BACKUP_NAME-Privileg bzw. Administratorrechte voraussetzt – siehe
/// <see cref="ElevationHelper"/>. Jede Methode gibt bei jedem Fehler oder jeder
/// Inkonsistenz einfach <c>false</c>/<c>null</c> zurück; der Aufrufer fällt dann auf den
/// normalen <see cref="FileSystemScanner"/> zurück. Es wird nie ein Teilergebnis als
/// vollständig ausgegeben.
/// </summary>
public static class NtfsFastScanner
{
    // ── Win32-Konstanten ─────────────────────────────────────────────────────
    private const uint GENERIC_READ = 0x80000000;
    private const uint FILE_SHARE_READ = 1;
    private const uint FILE_SHARE_WRITE = 2;
    private const uint OPEN_EXISTING = 3;

    private const uint FSCTL_QUERY_USN_JOURNAL = 0x000900F4;
    private const uint FSCTL_ENUM_USN_DATA = 0x000900B3;
    private const uint FSCTL_READ_USN_JOURNAL = 0x000900BB;

    private const int ERROR_HANDLE_EOF = 38;

    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;

    /// <summary>NTFS: Datensatznummer 5 ist immer das Stammverzeichnis des Volumes.</summary>
    private const ulong RootFileReferenceNumber = 5;

    private const int UsnRecordFixedHeaderSize = 60; // USN_RECORD_V2 bis einschließlich FileNameOffset

    // ── P/Invoke ─────────────────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    private struct USN_JOURNAL_DATA_V0
    {
        public ulong UsnJournalID;
        public long FirstUsn;
        public long NextUsn;
        public long LowestValidUsn;
        public long MaxUsn;
        public long MaximumSize;
        public long AllocationDelta;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MFT_ENUM_DATA_V0
    {
        public ulong StartFileReferenceNumber;
        public long LowUsn;
        public long HighUsn;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct READ_USN_JOURNAL_DATA_V0
    {
        public long StartUsn;
        public uint ReasonMask;
        public uint ReturnOnlyOnClose;
        public ulong Timeout;
        public ulong BytesToWaitFor;
        public ulong UsnJournalID;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WIN32_FILE_ATTRIBUTE_DATA
    {
        public uint dwFileAttributes;
        public uint ftCreationTimeLow, ftCreationTimeHigh;
        public uint ftLastAccessTimeLow, ftLastAccessTimeHigh;
        public uint ftLastWriteTimeLow, ftLastWriteTimeHigh;
        public uint nFileSizeHigh, nFileSizeLow;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(
        string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes,
        uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern unsafe bool DeviceIoControl(
        SafeFileHandle hDevice, uint dwIoControlCode,
        void* lpInBuffer, uint nInBufferSize,
        void* lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetFileAttributesExW(string lpFileName, int fInfoLevelId, out WIN32_FILE_ATTRIBUTE_DATA fileData);

    private static SafeFileHandle OpenVolume(string driveRoot)
    {
        string letter = driveRoot.TrimEnd('\\', '/');
        if (letter.Length < 2 || letter[1] != ':')
            return new SafeFileHandle(new IntPtr(-1), false);

        return CreateFileW($@"\\.\{letter[..2]}", GENERIC_READ,
            FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
    }

    private static unsafe bool TryQueryJournal(SafeFileHandle handle, out USN_JOURNAL_DATA_V0 data)
    {
        data = default;
        bool ok;
        uint bytesReturned;
        fixed (USN_JOURNAL_DATA_V0* pOut = &data)
        {
            ok = DeviceIoControl(handle, FSCTL_QUERY_USN_JOURNAL, null, 0,
                pOut, (uint)sizeof(USN_JOURNAL_DATA_V0), out bytesReturned, IntPtr.Zero);
        }
        return ok && bytesReturned >= sizeof(USN_JOURNAL_DATA_V0);
    }

    // ── Datensatz-Parsing (USN_RECORD_V2, variable Länge wegen Dateiname) ────
    private readonly record struct RawUsnRecord(
        ulong FileReferenceNumber, ulong ParentFileReferenceNumber,
        string FileName, bool IsDirectory, DateTime LastWriteUtc);

    private static bool TryParseRecord(byte[] buffer, int pos, uint bufferedLength, out RawUsnRecord record, out uint recordLength)
    {
        record = default;
        recordLength = 0;
        if (pos + 4 > bufferedLength) return false;

        uint recLen = BitConverter.ToUInt32(buffer, pos);
        if (recLen < UsnRecordFixedHeaderSize || pos + recLen > bufferedLength) return false;
        recordLength = recLen;

        ulong fileRef = BitConverter.ToUInt64(buffer, pos + 8);
        ulong parentRef = BitConverter.ToUInt64(buffer, pos + 16);
        long fileTime = BitConverter.ToInt64(buffer, pos + 32);
        uint attrs = BitConverter.ToUInt32(buffer, pos + 52);
        ushort nameLen = BitConverter.ToUInt16(buffer, pos + 56);
        ushort nameOff = BitConverter.ToUInt16(buffer, pos + 58);

        if (pos + nameOff + nameLen > pos + recLen) return false;

        string name;
        try { name = Encoding.Unicode.GetString(buffer, pos + nameOff, nameLen); }
        catch { return false; }
        if (string.IsNullOrEmpty(name)) return false;

        DateTime lastWrite;
        try { lastWrite = DateTime.FromFileTimeUtc(fileTime); }
        catch { lastWrite = DateTime.MinValue; }

        record = new RawUsnRecord(fileRef, parentRef, name, (attrs & FILE_ATTRIBUTE_DIRECTORY) != 0, lastWrite);
        return true;
    }

    // ── Voller Volumen-Scan ──────────────────────────────────────────────────
    /// <summary>
    /// Liest alle Datensätze des Volumes in einem sequenziellen Durchlauf und baut daraus den
    /// kompletten Baum. Gibt bei jedem Problem (nicht NTFS, keine Rechte, inkonsistente Daten)
    /// <c>false</c> zurück – der Aufrufer scannt dann normal weiter.
    /// </summary>
    public static bool TryFullVolumeScan(
        string driveRoot, CancellationToken ct,
        out FileSystemNode? root, out ulong journalId, out long usnCursor)
    {
        root = null;
        journalId = 0;
        usnCursor = 0;

        using var handle = OpenVolume(driveRoot);
        if (handle.IsInvalid) return false;

        if (!TryQueryJournal(handle, out var journal)) return false;

        var raw = new Dictionary<ulong, RawUsnRecord>(capacity: 200_000);

        var mftEnum = new MFT_ENUM_DATA_V0 { StartFileReferenceNumber = 0, LowUsn = 0, HighUsn = long.MaxValue };
        var outBuffer = new byte[65536];

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            bool ok;
            uint bytesReturned;
            unsafe
            {
                fixed (byte* pOut = outBuffer)
                {
                    MFT_ENUM_DATA_V0* pIn = &mftEnum;
                    ok = DeviceIoControl(handle, FSCTL_ENUM_USN_DATA,
                        pIn, (uint)sizeof(MFT_ENUM_DATA_V0),
                        pOut, (uint)outBuffer.Length, out bytesReturned, IntPtr.Zero);
                }
            }

            if (!ok)
            {
                int err = Marshal.GetLastWin32Error();
                if (err == ERROR_HANDLE_EOF) break; // Volume vollständig gelesen
                return false;
            }
            if (bytesReturned < 8) break;

            ulong nextStart = BitConverter.ToUInt64(outBuffer, 0);
            int pos = 8;
            while (pos < bytesReturned)
            {
                if (!TryParseRecord(outBuffer, pos, bytesReturned, out var rec, out uint recLen))
                    break;
                raw[rec.FileReferenceNumber] = rec;
                pos += (int)recLen;
            }

            if (nextStart <= mftEnum.StartFileReferenceNumber) break;
            mftEnum.StartFileReferenceNumber = nextStart;
        }

        if (!raw.ContainsKey(RootFileReferenceNumber)) return false;

        var childrenByParent = new Dictionary<ulong, List<ulong>>(capacity: raw.Count);
        foreach (var (fileRef, rec) in raw)
        {
            if (fileRef == RootFileReferenceNumber) continue;
            if (!childrenByParent.TryGetValue(rec.ParentFileReferenceNumber, out var list))
                childrenByParent[rec.ParentFileReferenceNumber] = list = [];
            list.Add(fileRef);
        }

        string rootPath = driveRoot.TrimEnd('\\', '/') + "\\";
        var rootNode = new FileSystemNode
        {
            Name = rootPath,
            FullPath = rootPath,
            IsDirectory = true,
            FileReferenceNumber = RootFileReferenceNumber,
            Depth = 0,
        };

        var fileNodes = new List<FileSystemNode>(capacity: raw.Count);
        BuildTree(rootNode, RootFileReferenceNumber, rootPath, raw, childrenByParent, fileNodes, ct, 0);

        // Dateigrößen fehlen im USN-Datensatz -- pro Datei leichtgewichtig nachladen, parallel.
        ct.ThrowIfCancellationRequested();
        Parallel.ForEach(
            fileNodes,
            new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount * 4, 16, 64) },
            node => node.Size = TryGetFileSize(node.FullPath));

        AggregateSizes(rootNode);

        root = rootNode;
        journalId = journal.UsnJournalID;
        usnCursor = journal.NextUsn;
        return true;
    }

    private static void BuildTree(
        FileSystemNode parentNode, ulong parentRef, string parentPath,
        Dictionary<ulong, RawUsnRecord> raw,
        Dictionary<ulong, List<ulong>> childrenByParent,
        List<FileSystemNode> fileNodes,
        CancellationToken ct,
        int depth)
    {
        if (depth > 512) return; // Schutz gegen zirkuläre/kaputte Referenzketten
        if (!childrenByParent.TryGetValue(parentRef, out var childRefs)) return;

        foreach (var childRef in childRefs)
        {
            ct.ThrowIfCancellationRequested();
            if (childRef == parentRef || !raw.TryGetValue(childRef, out var rec)) continue;

            string fullPath = Path.Combine(parentPath, rec.FileName);
            var node = new FileSystemNode
            {
                Name = rec.FileName,
                FullPath = fullPath,
                IsDirectory = rec.IsDirectory,
                Extension = rec.IsDirectory ? string.Empty : Path.GetExtension(rec.FileName).ToLowerInvariant(),
                LastModified = rec.LastWriteUtc == DateTime.MinValue ? DateTime.MinValue : rec.LastWriteUtc.ToLocalTime(),
                FileReferenceNumber = childRef,
                Parent = parentNode,
                Depth = depth + 1,
            };

            if (rec.IsDirectory)
                BuildTree(node, childRef, fullPath, raw, childrenByParent, fileNodes, ct, depth + 1);
            else
                fileNodes.Add(node);

            parentNode.Children.Add(node);
        }
    }

    private static void AggregateSizes(FileSystemNode node)
    {
        if (!node.IsDirectory) return;
        long sum = 0;
        foreach (var child in node.Children)
        {
            if (child.IsDirectory) AggregateSizes(child);
            sum += child.Size;
        }
        node.Size = sum;
    }

    private static long TryGetFileSize(string path)
    {
        try
        {
            if (GetFileAttributesExW(path, 0, out var data))
                return ((long)data.nFileSizeHigh << 32) | data.nFileSizeLow;
        }
        catch { }
        return 0;
    }

    // ── Inkrementelles Update über den USN-Journal-Delta ─────────────────────
    /// <summary>
    /// Liest nur die Änderungen seit <paramref name="lastUsn"/> und listet gezielt nur die
    /// davon betroffenen Ordner neu (statt das ganze Volume erneut zu lesen). Verändert
    /// <paramref name="cachedRoot"/> und <paramref name="fileRefIndex"/> in-place bei Erfolg.
    /// Gibt bei jeder Inkonsistenz (Journal neu erstellt, Cursor zu alt/übergelaufen, I/O-Fehler)
    /// <c>false</c> zurück, ohne den Baum zu verändern – der Aufrufer soll dann
    /// <see cref="TryFullVolumeScan"/> erneut ausführen.
    /// </summary>
    public static bool TryIncrementalUpdate(
        string driveRoot,
        FileSystemNode cachedRoot,
        Dictionary<ulong, FileSystemNode> fileRefIndex,
        ulong journalId,
        long lastUsn,
        CancellationToken ct,
        out long newUsnCursor)
    {
        newUsnCursor = lastUsn;

        using var handle = OpenVolume(driveRoot);
        if (handle.IsInvalid) return false;

        if (!TryQueryJournal(handle, out var journal)) return false;
        if (journal.UsnJournalID != journalId) return false;      // Journal wurde neu angelegt
        if (lastUsn < journal.LowestValidUsn) return false;       // Cursor zu alt / Journal übergelaufen

        if (lastUsn >= journal.NextUsn)
        {
            newUsnCursor = lastUsn;
            return true; // keine Änderungen seit dem letzten Scan
        }

        var dirtyContainers = new HashSet<ulong>();
        var read = new READ_USN_JOURNAL_DATA_V0
        {
            StartUsn = lastUsn,
            ReasonMask = 0xFFFFFFFF,
            ReturnOnlyOnClose = 0,
            Timeout = 0,
            BytesToWaitFor = 0,
            UsnJournalID = journalId,
        };
        var outBuffer = new byte[65536];

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            bool ok;
            uint bytesReturned;
            unsafe
            {
                fixed (byte* pOut = outBuffer)
                {
                    READ_USN_JOURNAL_DATA_V0* pIn = &read;
                    ok = DeviceIoControl(handle, FSCTL_READ_USN_JOURNAL,
                        pIn, (uint)sizeof(READ_USN_JOURNAL_DATA_V0),
                        pOut, (uint)outBuffer.Length, out bytesReturned, IntPtr.Zero);
                }
            }
            if (!ok) return false;
            if (bytesReturned < 8) break;

            long nextUsn = BitConverter.ToInt64(outBuffer, 0);
            int pos = 8;
            while (pos < bytesReturned)
            {
                if (!TryParseRecord(outBuffer, pos, bytesReturned, out var rec, out uint recLen))
                    break;

                // Sowohl der Ordner, in dem sich etwas geändert hat (ParentFileReferenceNumber),
                // als auch – falls das geänderte Element selbst ein Ordner ist – dessen eigener
                // Cache-Eintrag (für den Fall, dass er selbst umbenannt/verschoben wurde und
                // sein Unterbaum wiederverwendet statt neu gescannt werden soll).
                dirtyContainers.Add(rec.ParentFileReferenceNumber);
                if (rec.IsDirectory) dirtyContainers.Add(rec.FileReferenceNumber);

                pos += (int)recLen;
            }

            if (nextUsn <= read.StartUsn) break;
            read.StartUsn = nextUsn;
            if (read.StartUsn >= journal.NextUsn) break;
        }

        if (dirtyContainers.Count == 0)
        {
            newUsnCursor = read.StartUsn;
            return true;
        }

        // Nur die betroffenen Ordner neu auflisten (bereits bewährte Win32-Aufrufe wie im
        // normalen Scanner) statt Rename/Create/Delete-Semantik pro Datensatz nachzubilden.
        foreach (var containerRef in dirtyContainers)
        {
            ct.ThrowIfCancellationRequested();
            if (!fileRefIndex.TryGetValue(containerRef, out var containerNode) || !containerNode.IsDirectory)
                continue; // unbekannt (z. B. bereits gelöschter Zwischenordner) -> einfach überspringen

            if (!RelistDirectory(containerNode, fileRefIndex, ct))
                return false; // irgendetwas inkonsistent -> lieber kompletter Neuscan
        }

        AggregateSizes(cachedRoot);
        newUsnCursor = read.StartUsn;
        return true;
    }

    private static bool RelistDirectory(FileSystemNode dirNode, Dictionary<ulong, FileSystemNode> fileRefIndex, CancellationToken ct)
    {
        if (!Directory.Exists(dirNode.FullPath))
        {
            // Ordner selbst wurde gelöscht (oder umbenannt -- dann taucht er beim Neuauflisten
            // seines Elternordners unter dem neuen Namen wieder auf). Aus dem Baum entfernen.
            RemoveFromTree(dirNode, fileRefIndex);
            return true;
        }

        var previousByName = new Dictionary<string, FileSystemNode>(dirNode.Children.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var child in dirNode.Children)
            previousByName[child.Name] = child;

        List<string> entries;
        try
        {
            entries = new List<string>(Directory.EnumerateFileSystemEntries(dirNode.FullPath));
        }
        catch (OperationCanceledException) { throw; }
        catch { return false; }

        var freshChildren = new List<FileSystemNode>(entries.Count);

        foreach (var entryPath in entries)
        {
            ct.ThrowIfCancellationRequested();
            string name = Path.GetFileName(entryPath);
            bool isDir;
            try { isDir = (File.GetAttributes(entryPath) & FileAttributes.Directory) != 0; }
            catch { continue; } // zwischenzeitlich verschwunden

            if (previousByName.TryGetValue(name, out var existing) && existing.IsDirectory == isDir)
            {
                // Unverändert (oder nur Inhalt darunter geändert -- wird über dessen eigenen
                // dirtyContainers-Eintrag separat behandelt). Größe/Zeit bei Dateien auffrischen.
                if (!isDir)
                {
                    existing.Size = TryGetFileSize(entryPath);
                    try { existing.LastModified = File.GetLastWriteTime(entryPath); } catch { }
                }
                freshChildren.Add(existing);
                continue;
            }

            // Neuer Name an dieser Stelle: prüfen, ob es sich um einen bereits bekannten Knoten
            // handelt, der hierher umbenannt/verschoben wurde (Unterbaum wiederverwenden statt
            // neu zu scannen).
            FileSystemNode? reused = TryFindByReferenceNumber(entryPath, isDir, fileRefIndex);
            if (reused != null)
            {
                RemoveFromOldParent(reused);
                reused.Name = name;
                reused.FullPath = entryPath;
                reused.Parent = dirNode;
                if (isDir) RepathSubtree(reused);
                else { reused.Size = TryGetFileSize(entryPath); try { reused.LastModified = File.GetLastWriteTime(entryPath); } catch { } }
                freshChildren.Add(reused);
                continue;
            }

            // Wirklich neu -> frisch (normal) einlesen.
            var built = BuildNodeViaNormalWalk(entryPath, dirNode, isDir, ct);
            if (built != null)
            {
                RegisterInIndex(built, fileRefIndex);
                freshChildren.Add(built);
            }
        }

        // Verschwundene Kinder aus dem Index entfernen.
        var stillPresent = new HashSet<FileSystemNode>(freshChildren);
        foreach (var child in dirNode.Children)
        {
            if (!stillPresent.Contains(child))
                RemoveFromIndexRecursive(child, fileRefIndex);
        }

        dirNode.Children.Clear();
        dirNode.Children.AddRange(freshChildren);
        return true;
    }

    private static FileSystemNode? TryFindByReferenceNumber(string path, bool isDirectory, Dictionary<ulong, FileSystemNode> fileRefIndex)
    {
        ulong fileRef = TryGetFileReferenceNumber(path);
        if (fileRef == 0) return null;
        return fileRefIndex.TryGetValue(fileRef, out var node) && node.IsDirectory == isDirectory ? node : null;
    }

    private static unsafe ulong TryGetFileReferenceNumber(string path)
    {
        const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        using var h = CreateFileW(path, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
        if (h.IsInvalid) return 0;
        if (!GetFileInformationByHandle(h, out var info)) return 0;
        return ((ulong)info.nFileIndexHigh << 32) | info.nFileIndexLow;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BY_HANDLE_FILE_INFORMATION
    {
        public uint dwFileAttributes;
        public long ftCreationTime, ftLastAccessTime, ftLastWriteTime;
        public uint dwVolumeSerialNumber;
        public uint nFileSizeHigh, nFileSizeLow;
        public uint nNumberOfLinks;
        public uint nFileIndexHigh, nFileIndexLow;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

    private static void RemoveFromOldParent(FileSystemNode node)
    {
        node.Parent?.Children.Remove(node);
    }

    private static void RepathSubtree(FileSystemNode node)
    {
        foreach (var child in node.Children)
        {
            child.FullPath = Path.Combine(node.FullPath, child.Name);
            if (child.IsDirectory) RepathSubtree(child);
        }
    }

    private static FileSystemNode? BuildNodeViaNormalWalk(string path, FileSystemNode parent, bool isDirectory, CancellationToken ct)
    {
        ulong fileRef = TryGetFileReferenceNumber(path);
        var node = new FileSystemNode
        {
            Name = Path.GetFileName(path),
            FullPath = path,
            IsDirectory = isDirectory,
            Extension = isDirectory ? string.Empty : Path.GetExtension(path).ToLowerInvariant(),
            Parent = parent,
            Depth = parent.Depth + 1,
            FileReferenceNumber = fileRef,
        };
        try { node.LastModified = isDirectory ? Directory.GetLastWriteTime(path) : File.GetLastWriteTime(path); } catch { }

        if (!isDirectory)
        {
            node.Size = TryGetFileSize(path);
            return node;
        }

        try
        {
            foreach (var entryPath in Directory.EnumerateFileSystemEntries(path))
            {
                ct.ThrowIfCancellationRequested();
                bool childIsDir;
                try { childIsDir = (File.GetAttributes(entryPath) & FileAttributes.Directory) != 0; }
                catch { continue; }
                var child = BuildNodeViaNormalWalk(entryPath, node, childIsDir, ct);
                if (child != null) node.Children.Add(child);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { /* Zugriff verweigert o. Ä. -- Ordner bleibt leer */ }

        node.Size = node.Children.Sum(c => c.Size);
        return node;
    }

    private static void RegisterInIndex(FileSystemNode node, Dictionary<ulong, FileSystemNode> fileRefIndex)
    {
        if (node.FileReferenceNumber != 0) fileRefIndex[node.FileReferenceNumber] = node;
        foreach (var child in node.Children) RegisterInIndex(child, fileRefIndex);
    }

    private static void RemoveFromTree(FileSystemNode node, Dictionary<ulong, FileSystemNode> fileRefIndex)
    {
        node.Parent?.Children.Remove(node);
        RemoveFromIndexRecursive(node, fileRefIndex);
    }

    private static void RemoveFromIndexRecursive(FileSystemNode node, Dictionary<ulong, FileSystemNode> fileRefIndex)
    {
        if (node.FileReferenceNumber != 0) fileRefIndex.Remove(node.FileReferenceNumber);
        foreach (var child in node.Children) RemoveFromIndexRecursive(child, fileRefIndex);
    }

    /// <summary>Baut den FileReferenceNumber-Index aus einem (z. B. gecachten) Baum neu auf.</summary>
    public static Dictionary<ulong, FileSystemNode> BuildIndex(FileSystemNode root)
    {
        var index = new Dictionary<ulong, FileSystemNode>();
        RegisterInIndex(root, index);
        return index;
    }
}
