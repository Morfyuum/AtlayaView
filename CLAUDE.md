# CLAUDE.md – AtlayaView Projektkontext
# Chris Deliga / CNS Capecter NetworXs System / atlaya.capecter.com

## Was AtlayaView ist
Ein schneller, rein lokaler Festplatten-Visualisierer für Windows (WPF/.NET 9), der den
belegten Speicherplatz eines oder mehrerer Laufwerke als **Cushion-Treemap** zeichnet. Das
Zeichnen übernimmt ein natives Rust-Renderer-Modul (`native/atlaya_renderer`, kompiliert zu
`atlaya_renderer.dll`); ohne die DLL fällt die App automatisch auf einen verwalteten
C#-Renderer zurück. Single-File-Publish, portabel, keine Cloud-Anbindung.

## Verhältnis zu Atlaya
„AtlayaView" ist ein **eigenständiges zweites Produkt** neben der Atlaya-Assistentin
(Projektwurzel `D:\Atlaya\`). Beide teilen sich nur den Markennamen „Atlaya" und die
gemeinsame Produktwahl-Startseite auf der Webseite – technisch sind es getrennte Codebasen.
Dieser Ordner (`D:\AtlayaView\`) ist die **einzige aktive** AtlayaView-Codebasis (bis
2026-07-16: `D:\AtlayaView_Rust\` – umbenannt, damit „AtlayaView_Rust" nicht fälschlich als
eigenes drittes Produkt neben Atlaya/AtlayaView gelesen wird).

## Webseiten-Integration (Single Source of Truth liegt in D:\Atlaya)
Die öffentliche Präsenz von AtlayaView lebt **nicht** in diesem Ordner, sondern im
Atlaya-Repo unter `D:\Atlaya\website\atlayaview\<lang>\` (5 Sprachen: de/en/fr/it/es,
generiert von `D:\Atlaya\scripts\build_website.py`). Inhaltsquellen dort:
- `D:\Atlaya\website\data\atlayaview_site.json` – Funktionskatalog (Hero + Features, inline
  5-sprachig, kein separates Übersetzungs-Fallback-System)
- `D:\Atlaya\website\data\atlayaview_i18n.json` – Navigation/UI-Texte, Seiten-Metadaten,
  Download-Beschriftungen
- `D:\Atlaya\website\data\atlayaview_faq.json`, `atlayaview_help.json` – FAQ und Hilfeseiten
- `D:\Atlaya\website\data\atlayaview_updates.json` – Versionsverlauf + Downloadpfade
  (`download_full` / `download_fx`, `null` = „folgt mit dem nächsten Build")
- `D:\Atlaya\website\assets\atlayaview\*.svg` – Platzhalter-Icons je Funktion (Chris kann sie
  später 1:1 durch echte Screenshots gleichen Dateinamens ersetzen)

Die Wurzel-Startseite `atlaya.capecter.com` zeigt eine Produktwahl zwischen AtlayaView und
der Atlaya-Assistentin; AtlayaViews Version wird dort per Regex direkt aus
`AtlayaView.csproj` (`<Version>`) gelesen, mit Fallback auf `atlayaview_updates.json`, falls
dieser Ordner auf dem Zielrechner nicht existiert.

## PFLICHT: In-App-Oberfläche immer alle 5 Sprachen (DE/EN/FR/IT/ES)
`Core/LocalizationManager.cs` (`AppLanguage`-Enum + `S(de, en, fr, it, es)`-Helper) muss
IMMER dieselben 5 Sprachen abdecken wie die Webseite (siehe oben) – nicht nur DE/EN/FR/ES.
Bis 2026-07-15 fehlte Italienisch komplett im Enum (nur 4 Sprachen), obwohl die Webseite
längst 5-sprachig war; Chris musste das explizit nachfordern. **Bei jedem neuen UI-Text
(neue Property in `LocalizationManager.cs`) sofort alle 5 Übersetzungen mitschreiben, nie
nur eine Teilmenge** – gilt auch für neue `MenuItem`/`ComboBoxItem`-Einträge in XAML (Sprach-
Menü in `MainWindow.xaml` hat für jede Sprache einen eigenen `MenuItem` mit `Tag`-Index, der
exakt zur Enum-Reihenfolge passen muss).

## PFLICHT: Downloads dürfen nie Quellcode oder Geheimnisse enthalten
Absolute, dauerhafte Regel (siehe auch Chris' persönliche Notiz dazu): Öffentliche
AtlayaView-Downloads sind **niemals** Quellcode, Debug-Symbole oder Geheimnisse. Es gibt
**zwei Downloadvarianten** pro Version:
1. **Mit .NET-Inhalten** (self-contained) – bündelt die komplette .NET-9-Runtime, läuft ohne
   Vorinstallation, größerer Download.
2. **Ohne .NET-Inhalte** (framework-dependent) – setzt eine installierte .NET-9-Runtime
   voraus, deutlich kleinerer Download.

## Build- und Release-Ablauf
1. `pwsh -File .\build-hybrid.ps1 -Configuration Release` baut **beide** Varianten:
   - `dist\publish\AtlayaView-<version>-win-x64\` (self-contained, „mit .NET")
   - `dist\publish\AtlayaView-<version>-win-x64-fx\` (framework-dependent, „ohne .NET", per
     `-p:SelfContained=false -p:PublishSingleFile=false` erzeugt)
   - Erzeugt zusätzlich `dist\AtlayaView-<version>-win-x64.zip` und `-fx.zip` (Rohartefakte,
     NICHT direkt öffentlich verlinken – die enthalten z. B. noch `.pdb`-Dateien).
2. Im Atlaya-Repo `python scripts\package_atlayaview.py` ausführen. Dieses Skript:
   - liest die Version aus `AtlayaView.csproj`,
   - kopiert aus beiden Publish-Ordnern **nur** eine feste Whitelist (`.exe`, `.dll`, `.json`,
     `.ico`, `LICENSE*`) – explizit **ohne** `.pdb`/`.cs`/`.rs`/`.sln`/`.csproj`/`.git*` – in
     saubere ZIPs unter `D:\Atlaya\website\downloads\atlayaview\<version>\`,
   - trägt die Downloadpfade in `atlayaview_updates.json` ein (nur wenn die Versionszeile
     dort schon existiert – neue Releases zuerst mit Datum + 5-sprachigen Notizen ergänzen),
   - baut die Webseite neu (`build_website.py`).
3. **Nach jeder AtlayaView-Änderung** (Feature, Fix, Version) automatisch mitziehen:
   - neue Release-Zeile (Datum + Notizen in allen 5 Sprachen) oben in
     `D:\Atlaya\website\data\atlayaview_updates.json`,
   - bei neuen/geänderten Funktionen: `atlayaview_site.json` (+ ggf. neues Platzhalter-Icon)
     und `atlayaview_faq.json`/`atlayaview_help.json` bei Bedarf,
   - `<Version>` in `AtlayaView.csproj` anheben,
   - **`D:\AtlayaView\README.md`**: Versionsnummer in beiden Download-Blöcken (Kopfbereich
     und „## Downloads") auf die neue Version anheben – zwei getrennte Stellen mit je zwei
     Links (Full/FX), leicht zu vergessen, weil sie nicht Teil von `package_atlayaview.py`
     sind. War am 2026-07-20 drei Versionen (2.0.16 statt 2.0.35) veraltet, weil dieser
     Schritt beim Release-Zyklus schlicht übersprungen wurde. Änderung committen und pushen,
     sonst bleibt die Korrektur nur lokal.
   - Schritte 1–2 oben ausführen.
4. **PFLICHT – sofort und ohne Rückfrage ausführen (Dauerfreigabe von Chris, 2026-07-16,
   verschärft nach einem zweiten Vorfall am selben Tag):** Nach jeder AtlayaView-Änderung
   (Code oder `atlayaview_*.json`) SOBALD `dotnet build` lokal erfolgreich war, im selben
   Arbeitsgang den vollen Zyklus fahren: Schritt 1 (`build-hybrid.ps1`) → Schritt 2
   (`package_atlayaview.py`, baut Webseite mit) → `python D:\Atlaya\scripts\deploy_website.py`
   (lädt live auf atlaya.capecter.com hoch). `dotnet build` ist der EINZIGE Gate – ein von
   Chris selbst durchgeführter Live-Test ist KEINE Voraussetzung.
   **Nicht** vorher fragen „soll ich paketieren/deployen?" und **nicht** als Option anbieten
   „soll ich das deployen oder willst du erst selbst testen?" – auch nicht höflich formuliert.
   Einfach den Zyklus fahren und danach berichten, welche Version live ist. Vor Abschluss
   jeder Antwort mit AtlayaView-Änderung kurz selbst prüfen: „ist das schon deployed?" – falls
   nein, sofort nachholen statt bis zur nächsten Nutzer-Nachricht zu warten. Gilt nur für
   diesen AtlayaView-Release-Zyklus, keine pauschale Freigabe für andere riskante Aktionen
   (git push, sonstige Deploys bleiben bestätigungspflichtig).

## Update-Mechanismus (Self-Updater)
AtlayaView ist die **einzige** der drei Atlaya-Produktfamilien (Desktop/Android/AtlayaView)
mit einem echten, selbstersetzenden Auto-Updater – bewusst unterschiedlich weit ausgebaut,
nicht 1:1 auf die anderen übertragbar:
- **Atlaya Desktop:** zeigt ein gefundenes Update nur an, tauscht keine Dateien aus.
- **Android:** prüft nur + Systembenachrichtigung, kein Downloadziel (keine APK veröffentlicht).
- **AtlayaView:** `Core/SelfUpdater.cs` lädt die zur installierten Variante (mit/ohne .NET)
  passende ZIP vom Webseiten-Feed herunter, erzeugt ein PowerShell-Skript, das auf Prozessende
  wartet, Dateien austauscht und die App neu startet. Das ist die **einzige Netzwerkfunktion**
  der sonst komplett offline arbeitenden App.
- Einstellungen (`Manuell` / `Automatisch prüfen` / `Automatisch prüfen und updaten`,
  Intervall täglich/wöchentlich/monatlich/jährlich) liegen unter Einstellungen/Info.
- **Fallstrick Program Files (v2.0.11 behoben):** Ist AtlayaView unter
  `C:\Program Files (x86)\AtlayaView` installiert (UAC-geschützt), scheitert `Copy-Item` beim
  Dateitausch lautlos (Access Denied, keine sichtbare Fehlermeldung) – die App startet einfach
  unverändert neu, was wie „Update tut nichts" aussieht. Fix: `CanWriteTo(installDir)`-
  Schreibprobe vor dem Start des Swap-Skripts; wenn nicht beschreibbar,
  `ProcessStartInfo.Verb = "runas"` (UAC-Prompt). Swap-Skript loggt zusätzlich nach
  `%TEMP%\AtlayaView_apply.log`. Bei künftigen „Update funktioniert nicht"-Meldungen **zuerst
  den Installationsort prüfen**, bevor ein Download-/Netzwerkproblem angenommen wird.
- Beim Ändern des Update-Feed-Schemas (`atlayaview_updates.json` / `build_website.py`) immer
  gegen den tatsächlichen JSON-Output prüfen – ein Feldnamen-Mismatch (z. B.
  `app_versionCode` vs. `versionCode`) bleibt sonst unbemerkt und die Update-Erkennung liefert
  immer „kein Update gefunden".

## Webseiten-Deploy (live hochladen)
Das Hochladen der Webseite (inkl. AtlayaView-Unterseiten/Downloads) passiert **nicht** aus
diesem Ordner heraus, sondern im Atlaya-Repo:
1. `D:\Atlaya\scripts\package_atlayaview.py` – siehe „Build- und Release-Ablauf" oben.
2. `D:\Atlaya\scripts\build_website.py` – erzeugt alle Sprachseiten neu.
3. `D:\Atlaya\scripts\deploy_website.py` – lädt `website/` (ohne `website/data/`, das ist nur
   Generator-Quelle) per FTP auf den netcup-Webspace für `atlaya.capecter.com`. Lädt nur
   Dateien mit geänderter Größe neu hoch (bleibt trotz ~200 MB `downloads/`-Ordner schnell).
   Löscht **nie** etwas remote – rein lokal→remote gespiegelt; entfernte alte Versionen
   brauchen einen separaten FTP-Cleanup (einzeln `ftp.delete()`, dann `ftp.rmd()`).
   Zugangsdaten liegen in `D:\Atlaya\.env` (NICHT hier, NICHT im Vault – Credentials-Verbot).
4. Downloadzähler: `website/dl.php` zählt in `downloads/counts.json`, Schlüssel ist
   `atlayaview/full` bzw. `atlayaview/fx` (**kumulativ über alle Versionen**, nicht pro
   Einzelversion) – bei Schema-Änderungen an `counts.json` den Live-Stand migrieren, nicht
   überschreiben. `dl.php`/`counts.json` stehen in `deploy_website.py`s `EXCLUDE_FILENAMES`,
   damit ein Deploy den Live-Zählerstand nicht mit der leeren lokalen Version überschreibt.
5. Der netcup-Webspace führt PHP 8.4 aus – serverseitige Dynamik ohne externe Dienste möglich.

**Namensneutral-Regel:** In `atlayaview_site.json`/`atlayaview_i18n.json`/`atlayaview_faq.json`/
`atlayaview_help.json` keinen Personennamen verwenden (direkte Anrede „du"/„dein" statt
„Chris") – erlaubt bleibt „Chris" nur in Copyright-/Programmierer-Zeilen, die
`build_website.py` selbst setzt.

## Bekannte Fallstricke (WPF – kompiliert klaglos, bricht zur Laufzeit)
Drei bereits aufgetretene Muster, bei denen `dotnet build` fehlerfrei durchläuft, das
Ergebnis zur Laufzeit aber sichtbar/interaktiv kaputt ist – bei neuen Bugs dieser Art zuerst
hier nachsehen:
1. **ComboBox/Dropdown lässt sich per Klick nicht öffnen:** `Resources/Styles.xaml`s globales
   `ComboBox`-`ControlTemplate` braucht zwingend einen `ToggleButton`-Teil (WPF-„Template-
   Vertrag"), sonst wird `IsDropDownOpen` nie gesetzt – app-weit, nicht nur eine Stelle.
2. **Neue Sektion in einem Dialog mit fester Höhe (`ResizeMode="NoResize"`, `Height="..."`)
   wird unerreichbar**, wenn der tatsächliche Inhalt die Fensterhöhe übersteigt und kein
   `ScrollViewer` vorhanden ist. Bei neuen Sektionen in `Dialogs/*.xaml`: Höhe gegenrechnen
   oder präventiv `ScrollViewer` + `ResizeMode="CanResizeWithGrip"` (Muster seit v2.0.8).
3. **Bild/Icon im XAML referenziert, zeigt aber nichts an:** `.png`/`.jpg` werden von
   `Microsoft.NET.Sdk`+`UseWPF` NICHT implizit als `Resource` eingebunden (anders als
   `.xaml`, das automatisch als `Page` erkannt wird). Braucht immer einen expliziten
   `<ItemGroup><Resource Include="Resources\bild.png" /></ItemGroup>` im `.csproj`.
4. **`[CallerMemberName]`-Falle:** Wenn ein Property-Setter über eine Zwischenmethode
   (statt direkt) `Set(ref _feld, value)` aufruft, löst `[CallerMemberName]` auf den Namen
   der Zwischenmethode auf, nicht auf den Property-Namen – `PropertyChanged`-Abonnenten, die
   auf den echten Property-Namen prüfen, feuern dann nie. Fix: Namen explizit übergeben,
   `Set(ref _feld, value, nameof(Property))`. Betroffen war `IsScanning` (v2.0.13, „Scan
   bleibt für immer bei 'läuft'"-Bug).
   **Diagnose-Muster bei „App wirkt eingefroren":** nicht raten oder den Prozess killen –
   `dotnet-dump collect -p <pid>` auf den echten laufenden Prozess (nicht-destruktiv), dann
   `dotnet-dump analyze <dmp> -c "clrthreads" -c "clrstack -all" -c "dumpheap -type <Klasse>"
   -c "dumpobj <addr>"`. Zeigt sofort, ob Threads wirklich etwas tun oder ob nur die UI vom
   tatsächlichen (bereits korrekten) Zustand nichts mitbekommen hat.
   **Vorsicht bei Test-Instanzen:** `taskkill /F /IM AtlayaView.exe` (nach Image-Name) kann
   eine echte, parallel laufende Chris-Instanz treffen – immer nach PID killen, wenn eine
   reale Nutzer-Instanz dieselbe exe teilen könnte.

## Schneller Scan / NTFS-Fast-Path (seit v2.0.19, Stand 2026-07-19: UNGETESTET im echten Admin-Betrieb)
Opt-in-Feature (Einstellungen → Scan-Geschwindigkeit → „Schneller Scan", standardmäßig AUS),
implementiert in `Core/NtfsFastScanner.cs` (rohe `DeviceIoControl`-Aufrufe: `FSCTL_ENUM_USN_DATA`
für den ersten Volumen-Scan, `FSCTL_QUERY_USN_JOURNAL`/`FSCTL_READ_USN_JOURNAL` für inkrementelle
Re-Scans), `Core/FileTreeCache.cs` (Baum+Cursor-Persistierung unter
`%LocalAppData%\AtlayaView\fastscan-cache\`), `Core/FastScanCoordinator.cs` (Entscheidungslogik
+ Fallback), `Core/ElevationHelper.cs` (Admin-Check + `runas`-Selbst-Neustart).

**Wichtig für jede künftige Bearbeitung dieses Bereichs:** Diese Komponenten wurden von Claude
blind implementiert – die Entwicklungsumgebung hatte keine Administratorrechte, und ein
Volume-Handle (`\\.\C:`) für `FSCTL_QUERY_USN_JOURNAL`/`FSCTL_ENUM_USN_DATA` verlangt laut
Microsoft-Doku zwingend `SE_BACKUP_NAME`-Privileg bzw. Adminrechte (unabhängig davon, ob nur das
Journal oder die volle MFT-Enumeration gelesen wird – beides sperrt Windows für normale Prozesse).
Getestet wurde deshalb nur der **nicht-elevated Pfad** (Gate-Logik: `FastScanCoordinator.TryScan`
liefert bei fehlenden Rechten, bei Unterordner-Scans und bei jedem Fehler sauber `false`, normaler
`FileSystemScanner` läuft dann unverändert weiter wie vor v2.0.19). Der eigentliche NTFS-Zugriff,
das USN-Record-Parsing und die Rename/Delete-Erkennung beim inkrementellen Update sind **nicht**
live verifiziert. Vor dem nächsten Ausbau (oder bei einer „Schneller Scan zeigt falsche Größen"-
Meldung von Chris) zuerst hier nachsehen, ob Chris das Feature inzwischen selbst mit Adminrechten
getestet und bestätigt hat (siehe Auto-Memory `atlayaview-fast-scan-status`); wenn nicht, gilt der
Code weiterhin als unverifiziert, auch wenn er kompiliert und ausliefert.

## Technische Hinweise (aus `docs/release-notes.md`)
- `AtlayaView.csproj` schließt `obj`, `bin`, `dist/publish` und `.artifacts` explizit von der
  Kompilierung aus, um WPF-Doppeldefinitionen aus `wpftmp`-Zwischenständen zu vermeiden.
- `build-hybrid.ps1` nutzt für Publish-Zwischenartefakte Temp-Pfade außerhalb des Repos
  (`publish-obj`, `publish-obj-fx`), um Konflikte mit Security-Scannern auf
  `obj/.../refint/*.dll` zu vermeiden.
- Rust-Validierung vor Release: `cargo check` in `native/atlaya_renderer`.

## Gedächtnis / Speicher – wie das hier und in Atlaya funktioniert
Claude Code hat zwei getrennte Gedächtnis-Ebenen, die man auseinanderhalten muss:

1. **Diese Datei (`D:\AtlayaView\CLAUDE.md`)** – versioniert im Projekt, wird bei
   **jeder** Claude-Code-Sitzung in diesem Ordner automatisch eingelesen. Das ist die
   **einzige Quelle, die garantiert auf beiden Seiten ankommt**: Atlayas eigene
   `D:\Atlaya\CLAUDE.md` verweist unter „Zweites Produkt: AtlayaView" ausdrücklich hierher
   (siehe dort). Deshalb gilt: **alles, was für Atlaya UND AtlayaView relevant ist
   (Webseiten-Prozess, Update-Feed-Schema, Downloads/Sicherheitsregeln, 5-Sprachen-Pflicht),
   gehört in diese Datei geschrieben – nicht nur ins Auto-Memory unten.**
2. **Automatisches Auto-Memory** (projektgebunden, ordnerabhängig) – liegt unter
   `C:\Users\capin\.claude\projects\d--AtlayaView\memory\` mit eigenem `MEMORY.md`-Index
   und Einzeldateien (Typen: `user`/`feedback`/`project`/`reference`), analog zum Aufbau, den
   Atlaya selbst unter `C:\Users\capin\.claude\projects\d--Atlaya\memory\` führt. **Diese
   beiden Auto-Memory-Ordner sind getrennt und synchronisieren NICHT automatisch** – eine
   Lektion, die hier (im AtlayaView-Workspace) als Auto-Memory gespeichert wird, taucht in
   einer Atlaya-Sitzung (anderer Arbeitsordner) nicht von selbst auf, und umgekehrt.

**Praktische Konsequenz – Faustregel:**
- Rein AtlayaView-interne technische Lektionen (WPF-Fallstricke, Debugging-Methoden,
  Build-Skript-Details) → normales Auto-Memory hier im AtlayaView-Workspace reicht, ist aber
  für dauerhaft wichtige Fallstricke zusätzlich oben unter „Bekannte Fallstricke"
  festgehalten (Redundanz ist hier gewollt, weil CLAUDE.md nie durch Kontext-Kompression
  verloren geht).
- Alles, was die **Webseite, den Update-Feed, Downloads oder Produktregeln** betrifft (also
  die Schnittstelle zu Atlaya) → **immer in diese CLAUDE.md schreiben**, unabhängig davon, in
  welchem Workspace gerade gearbeitet wird. Nur so bekommt „Atlaya selbst" (die Sitzung unter
  `D:\Atlaya\`) es garantiert mit, ohne dass jemand die Auto-Memory-Ordner manuell abgleicht.
- Bei größeren Entscheidungen zusätzlich einen Einzeiler in Atlayas eigenem
  `08_Memory\decisions-summary.md` im Obsidian-Vault ergänzen (siehe Dokumentationspflicht in
  `D:\Atlaya\CLAUDE.md`) – das ist die dritte, vaultbasierte Ebene, die aus jedem Workspace
  heraus erreichbar ist (`D:\AtlayaClawObsidian\Atlaya\08_Memory\`).

## Copyright-Header
Neue Quelldateien: `// Chris Deliga / CNS Capecter NetworXs System / atlaya.capecter.com`
