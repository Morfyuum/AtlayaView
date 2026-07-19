// Chris Deliga / CNS Capecter NetworXs System / atlaya.capecter.com
using System.IO;
using System.Text;

namespace AtlayaView.Core;

/// <summary>
/// Persistiert den zuletzt per <see cref="NtfsFastScanner"/> gelesenen Baum eines Laufwerks
/// zusammen mit dem USN-Journal-Cursor, damit ein erneuter Scan über
/// <see cref="NtfsFastScanner.TryIncrementalUpdate"/> nur noch die Änderungen seit dem letzten
/// Lauf nachziehen muss statt das ganze Volume erneut zu lesen. Reine Optimierung – jeder
/// Lese-/Schreibfehler wird verschluckt, der Aufrufer scannt dann einfach normal weiter.
/// </summary>
public static class FileTreeCache
{
    private const uint Magic = 0x41564643; // "AVFC"
    private const int FormatVersion = 1;
    private const int MaxPlausibleNodeCount = 20_000_000;

    private static string CacheDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AtlayaView", "fastscan-cache");

    private static string CachePath(string driveRoot)
    {
        string letter = driveRoot.TrimEnd('\\', '/', ':').ToUpperInvariant();
        return Path.Combine(CacheDir, $"{letter}.cache");
    }

    public static void Save(string driveRoot, FileSystemNode root, ulong journalId, long lastUsn)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            string tmpPath = CachePath(driveRoot) + ".tmp";

            using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs, Encoding.UTF8))
            {
                bw.Write(Magic);
                bw.Write(FormatVersion);
                bw.Write(driveRoot);
                bw.Write(journalId);
                bw.Write(lastUsn);

                var flat = new List<(FileSystemNode Node, int ParentIndex)>();
                Flatten(root, -1, flat);

                bw.Write(flat.Count);
                foreach (var (node, parentIndex) in flat)
                {
                    bw.Write(parentIndex);
                    bw.Write(node.Name);
                    bw.Write(node.IsDirectory);
                    bw.Write(node.Size);
                    bw.Write(node.LastModified.Ticks);
                    bw.Write(node.FileReferenceNumber);
                }
            }

            string finalPath = CachePath(driveRoot);
            File.Copy(tmpPath, finalPath, overwrite: true);
            File.Delete(tmpPath);
        }
        catch { /* Cache ist reine Optimierung -- Schreibfehler ignorieren */ }
    }

    private static void Flatten(FileSystemNode node, int parentIndex, List<(FileSystemNode, int)> outList)
    {
        int myIndex = outList.Count;
        outList.Add((node, parentIndex));
        foreach (var child in node.Children)
            Flatten(child, myIndex, outList);
    }

    public static bool TryLoad(
        string driveRoot,
        out FileSystemNode? root,
        out Dictionary<ulong, FileSystemNode>? fileRefIndex,
        out ulong journalId,
        out long lastUsn)
    {
        root = null;
        fileRefIndex = null;
        journalId = 0;
        lastUsn = 0;

        try
        {
            string path = CachePath(driveRoot);
            if (!File.Exists(path)) return false;

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs, Encoding.UTF8);

            if (br.ReadUInt32() != Magic) return false;
            if (br.ReadInt32() != FormatVersion) return false;

            string savedRoot = br.ReadString();
            if (!string.Equals(savedRoot, driveRoot, StringComparison.OrdinalIgnoreCase)) return false;

            journalId = br.ReadUInt64();
            lastUsn = br.ReadInt64();

            int count = br.ReadInt32();
            if (count <= 0 || count > MaxPlausibleNodeCount) return false;

            var nodes = new FileSystemNode[count];
            var index = new Dictionary<ulong, FileSystemNode>(count);

            for (int i = 0; i < count; i++)
            {
                int parentIndex = br.ReadInt32();
                string name = br.ReadString();
                bool isDir = br.ReadBoolean();
                long size = br.ReadInt64();
                long ticks = br.ReadInt64();
                ulong fileRef = br.ReadUInt64();

                if (parentIndex < -1 || parentIndex >= i) return false; // Eltern stehen immer vor Kindern

                var parent = parentIndex >= 0 ? nodes[parentIndex] : null;
                string fullPath = parent == null ? name : Path.Combine(parent.FullPath, name);

                var node = new FileSystemNode
                {
                    Name = name,
                    FullPath = fullPath,
                    IsDirectory = isDir,
                    Extension = isDir ? string.Empty : Path.GetExtension(name).ToLowerInvariant(),
                    Size = size,
                    LastModified = new DateTime(ticks),
                    FileReferenceNumber = fileRef,
                    Parent = parent,
                    Depth = parent == null ? 0 : parent.Depth + 1,
                };

                nodes[i] = node;
                parent?.Children.Add(node);
                if (fileRef != 0) index[fileRef] = node;
            }

            root = nodes[0];
            fileRefIndex = index;
            return true;
        }
        catch
        {
            root = null;
            fileRefIndex = null;
            return false;
        }
    }

    public static void Invalidate(string driveRoot)
    {
        try { File.Delete(CachePath(driveRoot)); } catch { }
    }
}
