using System.ComponentModel;
using System.Reflection;

namespace AtlayaView.Core;

public enum AppLanguage { Deutsch, English, Français, Italiano, Español }

/// <summary>
/// Singleton-Lokalisierungs-Manager mit INotifyPropertyChanged.
/// Alle XAML-Bindings verwenden Source={x:Static app:App.Loc}.
/// </summary>
public sealed class LocalizationManager : INotifyPropertyChanged
{
    private static readonly LocalizationManager _instance = new();
    public static LocalizationManager Instance => _instance;

    private static readonly string AppVersion = typeof(LocalizationManager).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(LocalizationManager).Assembly.GetName().Version?.ToString(3)
        ?? "unbekannt";

    /// <summary>Installierte AtlayaView-Version, für den Vergleich mit dem Update-Feed.</summary>
    public static string CurrentVersion => AppVersion;

    /// <summary>Kurze Versionsanzeige ("v2.0.22") für die Toolbar, sprachunabhängig.</summary>
    public string HeaderVersionText => $"v{AppVersion}";

    /// <summary>Anzeigetext "Version x.y.z" für den Leer-Zustand, sprachabhängig.</summary>
    public string EmptyStateVersion => _language switch
    {
        AppLanguage.Italiano => $"Versione {AppVersion}",
        AppLanguage.Español => $"Versión {AppVersion}",
        _ => $"Version {AppVersion}",
    };

    private static readonly string Bitness = Environment.Is64BitProcess ? "64-bit" : "32-bit";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    private AppLanguage _language = AppLanguage.Deutsch;

    public AppLanguage Language
    {
        get => _language;
        set
        {
            if (_language == value) return;
            _language = value;
            // string.Empty = alle Properties geändert → alle Bindings aktualisieren
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private string S(string de, string en, string fr, string it, string es) => _language switch
    {
        AppLanguage.English => en,
        AppLanguage.Français => fr,
        AppLanguage.Italiano => it,
        AppLanguage.Español => es,
        _ => de,
    };

    // ── Menüleiste ──────────────────────────────────────────────────────────
    public string MenuFile => S("_Datei", "_File", "_Fichier", "_File", "_Archivo");
    public string MenuOpenFolder => S("📁  Ordner öffnen …", "📁  Open Folder …", "📁  Ouvrir un dossier …", "📁  Apri cartella …", "📁  Abrir carpeta …");
    public string MenuSaveBitmap => S("💾  Als Bitmap speichern …", "💾  Save as Bitmap …", "💾  Enregistrer en bitmap …", "💾  Salva come bitmap …", "💾  Guardar como mapa de bits …");
    public string MenuExit => S("Beenden", "Exit", "Quitter", "Esci", "Salir");
    public string MenuView => S("_Ansicht", "_View", "_Affichage", "_Visualizza", "_Vista");
    public string MenuBack => S("◀  Zurück", "◀  Back", "◀  Retour", "◀  Indietro", "◀  Atrás");
    public string MenuUp => S("▲  Hoch", "▲  Up", "▲  Haut", "▲  Su", "▲  Arriba");
    public string MenuRefresh => S("🔄  Aktualisieren", "🔄  Refresh", "🔄  Actualiser", "🔄  Aggiorna", "🔄  Actualizar");
    public string MenuShowLegend => S("Legende anzeigen", "Show Legend", "Afficher la légende", "Mostra legenda", "Mostrar leyenda");
    public string MenuShowDiskSpace => S("Freier Speicher anzeigen", "Show Free Space", "Afficher l'espace libre", "Mostra spazio libero", "Mostrar espacio libre");
    public string MenuShowFreeCushion => S("Freier Speicher als Kissen", "Free Space as Cushion", "Espace libre en coussin", "Spazio libero come cuscino", "Espacio libre como almohadilla");
    public string MenuShowAllTypes => S("✔  Alle Typen anzeigen", "✔  Show All Types", "✔  Afficher tous les types", "✔  Mostra tutti i tipi", "✔  Mostrar todos los tipos");
    public string MenuOptions => S("_Optionen", "_Options", "_Options", "_Opzioni", "_Opciones");
    public string MenuSettings => S("⚙  Optionen …", "⚙  Settings …", "⚙  Paramètres …", "⚙  Opzioni …", "⚙  Configuración …");
    public string MenuColorScheme => S("🎨  Farbschema …", "🎨  Color Scheme …", "🎨  Schéma de couleurs …", "🎨  Schema colori …", "🎨  Esquema de colores …");
    public string MenuFilter => S("🔍  Filter …", "🔍  Filter …", "🔍  Filtre …", "🔍  Filtro …", "🔍  Filtro …");
    public string MenuLanguage => S("Sprache", "Language", "Langue", "Lingua", "Idioma");
    public string MenuHelp => S("_Hilfe", "_Help", "_Aide", "_Aiuto", "_Ayuda");
    public string MenuAbout => S("ℹ  Über AtlayaView …", "ℹ  About AtlayaView …", "ℹ  À propos d'AtlayaView …", "ℹ  Informazioni su AtlayaView …", "ℹ  Acerca de AtlayaView …");
    public string MenuImprint => S("⚖  Impressum (Web) …", "⚖  Imprint (Web) …", "⚖  Mentions légales (Web) …", "⚖  Note legali (Web) …", "⚖  Aviso legal (Web) …");
    public string MenuPrivacy => S("🔒  Datenschutz (Web) …", "🔒  Privacy Policy (Web) …", "🔒  Confidentialité (Web) …", "🔒  Privacy (Web) …", "🔒  Privacidad (Web) …");
    public string MenuCheckUpdate => S("🔄  Auf Updates prüfen …", "🔄  Check for Updates …", "🔄  Vérifier les mises à jour …", "🔄  Controlla aggiornamenti …", "🔄  Buscar actualizaciones …");

    // ── Toolbar ──────────────────────────────────────────────────────────────
    public string BtnFolder => S("📁  Ordner", "📁  Folder", "📁  Dossier", "📁  Cartella", "📁  Carpeta");
    public string BtnDrives => S("🗂  Laufwerke", "🗂  Drives", "🗂  Lecteurs", "🗂  Unità", "🗂  Unidades");
    public string TipOpenFolder => S("Ordner öffnen (Ctrl+O)", "Open Folder (Ctrl+O)", "Ouvrir un dossier (Ctrl+O)", "Apri cartella (Ctrl+O)", "Abrir carpeta (Ctrl+O)");
    public string TipScanDrive => S("Laufwerk scannen", "Scan Drive", "Analyser le lecteur", "Analizza unità", "Analizar unidad");
    public string TipDrivePicker => S("Mehrere Laufwerke auswählen und direkt starten", "Select multiple drives and start immediately", "Sélectionner plusieurs lecteurs et démarrer immédiatement", "Seleziona più unità e avvia subito", "Seleccionar varias unidades e iniciar inmediatamente");
    public string TipBack => S("Zurück (Backspace)", "Back (Backspace)", "Retour (Retour arrière)", "Indietro (Backspace)", "Atrás (Retroceso)");
    public string TipUp => S("Hoch (↑)", "Up (↑)", "Haut (↑)", "Su (↑)", "Arriba (↑)");
    public string TipRefresh => S("Aktualisieren (F5)", "Refresh (F5)", "Actualiser (F5)", "Aggiorna (F5)", "Actualizar (F5)");
    public string BtnCancelText => S("Scan abbrechen", "Cancel Scan", "Annuler l'analyse", "Annulla scansione", "Cancelar análisis");
    public string TipLegendFilter => S("Klicken zum Filtern · Strg+Klick für Mehrfachauswahl",
                                           "Click to Filter · Ctrl+Click for Multi-Select",
                                           "Cliquer pour filtrer · Ctrl+Clic pour sélection multiple",
                                           "Clicca per filtrare · Ctrl+clic per selezione multipla",
                                           "Clic para filtrar · Ctrl+Clic para selección múltiple");

    // ── Leer-Zustand ─────────────────────────────────────────────────────────
    public string EmptyHint1 => S("Ordner oder Laufwerk zum Visualisieren wählen",
                                           "Select a Folder or Drive to Visualize",
                                           "Sélectionner un dossier ou un lecteur à visualiser",
                                           "Seleziona una cartella o un'unità da visualizzare",
                                           "Seleccionar una carpeta o unidad para visualizar");
    public string EmptyHint2 => S("Datei › Ordner öffnen   oder Laufwerk in der Toolbar wählen",
                                           "File › Open Folder   or select Drive in Toolbar",
                                           "Fichier › Ouvrir un dossier   ou sélectionner un lecteur dans la barre d'outils",
                                           "File › Apri cartella   oppure seleziona un'unità nella barra degli strumenti",
                                           "Archivo › Abrir carpeta   o seleccionar unidad en la barra de herramientas");

    // ── Scan-Overlay ──────────────────────────────────────────────────────────
    public string ScanOverlayTitle => S("Scan läuft …", "Scan in progress …", "Analyse en cours …", "Scansione in corso …", "Análisis en curso …");
    public string ScanWindowTitle => S("AtlayaView – Scanstatus", "AtlayaView – Scan Status", "AtlayaView – État de l'analyse", "AtlayaView – Stato scansione", "AtlayaView – Estado del análisis");
    public string ScanWindowHint => S("Dieses Fenster bleibt sichtbar, während AtlayaView scannt.",
                                      "This window stays visible while AtlayaView is scanning.",
                                      "Cette fenêtre reste visible pendant l'analyse d'AtlayaView.",
                                      "Questa finestra rimane visibile durante la scansione di AtlayaView.",
                                      "Esta ventana permanece visible mientras AtlayaView analiza.");

    // ── Status ────────────────────────────────────────────────────────────────
    public string StatusReady => S("Bereit – Laufwerk oder Ordner wählen",
                                           "Ready – Select Drive or Folder",
                                           "Prêt – Sélectionner un lecteur ou un dossier",
                                           "Pronto – Seleziona unità o cartella",
                                           "Listo – Seleccionar unidad o carpeta");
    public string StatusNoDrive => S("Kein Laufwerk ausgewählt", "No Drive Selected", "Aucun lecteur sélectionné", "Nessuna unità selezionata", "Ninguna unidad seleccionada");
    public string StatusDriveSelected => S("ausgewählt – F5 zum Scannen", "selected – F5 to Scan", "sélectionné – F5 pour analyser", "selezionata – F5 per analizzare", "seleccionada – F5 para analizar");
    public string StatusDrivesSelected => S("Laufwerke ausgewählt – F5 zum Scannen",
                                            "Drives Selected – F5 to Scan",
                                            "Lecteurs sélectionnés – F5 pour analyser",
                                            "Unità selezionate – F5 per analizzare",
                                            "Unidades seleccionadas – F5 para analizar");
    public string StatusScanningPrefix => S("Scanne", "Scanning", "Analyse de", "Analisi di", "Analizando");
    public string StatusScanningMulti => S("Laufwerke parallel…", "Drives in Parallel…", "Lecteurs en parallèle…", "Unità in parallelo…", "Unidades en paralelo…");
    public string StatusAnalyzing => S("Verzeichnisstruktur analysieren …", "Analysing directory structure …", "Analyse de la structure …", "Analisi della struttura delle directory …", "Analizando estructura …");
    public string StatusReadingPhase => S("Dateien und Ordner werden eingelesen …", "Reading files and folders …", "Lecture des fichiers et dossiers …", "Lettura di file e cartelle …", "Leyendo archivos y carpetas …");
    public string StatusProcessingPhase => S("Daten werden verarbeitet …", "Processing data …", "Traitement des données …", "Elaborazione dati …", "Procesando datos …");
    public string StatusDone => S("Fertig", "Done", "Terminé", "Fatto", "Listo");
    public string StatusCancelled => S("Scan abgebrochen", "Scan Cancelled", "Analyse annulée", "Scansione annullata", "Análisis cancelado");
    public string StatusDriveScanFailedFmt => S("nicht lesbar, abgewählt: {0}", "unreadable, deselected: {0}", "illisible, désélectionné : {0}", "illeggibile, deselezionata: {0}", "ilegible, deseleccionada: {0}");
    public string StatusEtaPrefix => S("noch", "remaining:", "restant :", "restante:", "restante:");

    // ── Hover-Popup ──────────────────────────────────────────────────────────
    public string TypeFolder => S("Ordner", "Folder", "Dossier", "Cartella", "Carpeta");
    public string TypeFile => S("Datei", "File", "Fichier", "File", "Archivo");
    public string HoverModified => S("Geändert:", "Modified:", "Modifié :", "Modificato:", "Modificado:");

    // ── Kontext-Menü ──────────────────────────────────────────────────────────
    public string CtxOpenFile => S("▶  Datei öffnen", "▶  Open File", "▶  Ouvrir le fichier", "▶  Apri file", "▶  Abrir archivo");
    public string CtxOpenExplorer => S("📂  Im Explorer öffnen", "📂  Open in Explorer", "📂  Ouvrir dans l'Explorateur", "📂  Apri in Esplora risorse", "📂  Abrir en el Explorador");
    public string CtxCopyPath => S("📋  Pfad kopieren", "📋  Copy Path", "📋  Copier le chemin", "📋  Copia percorso", "📋  Copiar ruta");
    public string CtxNavigateInto => S("▶  Hineinnavigieren", "▶  Navigate Into", "▶  Naviguer dedans", "▶  Naviga dentro", "▶  Navegar dentro");
    public string CtxDrivesHeader => S("─── Laufwerke ───", "─── Drives ───", "─── Lecteurs ───", "─── Unità ───", "─── Unidades ───");

    // ── Info-Dialog ──────────────────────────────────────────────────────────
    public string AboutTitle => S("Über AtlayaView", "About AtlayaView", "À propos d'AtlayaView", "Informazioni su AtlayaView", "Acerca de AtlayaView");

    public string AboutTagline => S(
        "Fast Disk Visualizer  ·  Squarified Cushion-Treemap",
        "Fast Disk Visualizer  ·  Squarified Cushion-Treemap",
        "Visualiseur de disque rapide  ·  Squarified Cushion-Treemap",
        "Visualizzatore rapido del disco  ·  Squarified Cushion-Treemap",
        "Visualizador de disco rápido  ·  Squarified Cushion-Treemap");

    public string AboutVersionLine => $"{S("Version", "Version", "Version", "Versione", "Versión")} {AppVersion}  ·  .NET 9  ·  {Bitness}";

    public string AboutRendererLine => S(
        $"UI: WPF / C#  ·  Nativer Renderer: Rust (atlaya_renderer {NativeRenderer.RendererVersion})",
        $"UI: WPF / C#  ·  Native Renderer: Rust (atlaya_renderer {NativeRenderer.RendererVersion})",
        $"UI : WPF / C#  ·  Moteur natif : Rust (atlaya_renderer {NativeRenderer.RendererVersion})",
        $"UI: WPF / C#  ·  Motore nativo: Rust (atlaya_renderer {NativeRenderer.RendererVersion})",
        $"UI: WPF / C#  ·  Renderizador nativo: Rust (atlaya_renderer {NativeRenderer.RendererVersion})");

    public string AboutBuildLine => "Build: MSBuild + cargo build --release";

    public string AboutFallbackLine => S(
        "Fallback: Verwalteter C#-Renderer, falls die Rust-DLL nicht geladen werden kann",
        "Fallback: Managed C# renderer if the Rust DLL is unavailable",
        "Solution de secours : rendu C# géré si la DLL Rust est indisponible",
        "Fallback: renderer C# gestito se la DLL Rust non è disponibile",
        "Alternativa: renderizador C# administrado si la DLL de Rust no está disponible");

    // "Grundidee von" statt "Basierend auf dem Algorithmus von" (Chris' Wortwahl,
    // 2026-07-16) - van Wijk & van de Wetering lieferten die Kissen-Idee, die tatsächliche
    // Umsetzung (Rust-Renderer, Squarified-Layout) ist eigener Code.
    public string AboutIdeaLead => S("Grundidee von", "Original idea by", "Idée originale de", "Idea originale di", "Idea original de");

    // Eigenname, nicht übersetzt.
    public string AboutAlgorithmName => "van Wijk & van de Wetering (1999)";

    public string AboutCopyright => "© 2026 Chris Deliga  ·  CNS Capecter NetworXs System";
    public string AboutWebsite => "atlaya.capecter.com";

    // Datenschutz-Kurzhinweis im Info-Dialog – AtlayaView verarbeitet KEINE personenbezogenen
    // Daten; Scan/Rendering laufen zu 100% lokal. Die EINZIGE Netzwerkverbindung ist die
    // Update-Prüfung/-Installation (nur Versionsabfrage + ggf. Download, keine Datei-/
    // Ordnerdaten) - vorher stand hier faelschlich "stellt keine Netzwerkverbindung her",
    // was seit dem Self-Updater (v2.0.7) nicht mehr stimmte (Korrektur 2026-07-16).
    // Volltext über "Rechtliches" im Hilfe-Menü.
    public string PrivacyNote => S(
        "Datenschutz: Diese Anwendung verarbeitet keine personenbezogenen Daten; Scan und Zeichnen laufen zu 100 % lokal. Einzige Netzwerkverbindung: die Update-Prüfung/-Installation (nur Versionsabfrage & ggf. Download, keine Datei-/Ordnerdaten). Impressum & Datenschutzerklärung: Hilfe-Menü → „Rechtliches“.",
        "Privacy: This application processes no personal data; scanning and rendering run 100% locally. The only network connection is the update check/installation (version query and, if used, the download only – no file or folder data). Imprint & privacy policy: Help menu → “Legal”.",
        "Confidentialité : cette application ne traite aucune donnée personnelle ; l'analyse et le rendu se déroulent à 100 % en local. Seule connexion réseau : la vérification/l'installation des mises à jour (uniquement la requête de version et, le cas échéant, le téléchargement – aucune donnée de fichier ou de dossier). Mentions légales & confidentialité : menu Aide → « Mentions légales ».",
        "Privacy: questa applicazione non elabora dati personali; scansione e rendering sono al 100% locali. Unica connessione di rete: il controllo/l'installazione degli aggiornamenti (solo richiesta di versione ed eventuale download – nessun dato di file o cartelle). Note legali e informativa sulla privacy: menu Aiuto → «Note legali».",
        "Privacidad: esta aplicación no procesa datos personales; el escaneo y el renderizado se ejecutan 100% en local. Única conexión de red: la comprobación/instalación de actualizaciones (solo consulta de versión y, en su caso, la descarga – sin datos de archivos o carpetas). Aviso legal y privacidad: menú Ayuda → «Aviso legal».");

    // ── Speichern-Dialog ─────────────────────────────────────────────────────
    public string SaveTitle => S("Treemap als Bitmap speichern", "Save Treemap as Bitmap", "Enregistrer la Treemap en bitmap", "Salva Treemap come bitmap", "Guardar Treemap como mapa de bits");
    public string SaveFilter => S("PNG-Bild (*.png)|*.png|BMP-Bild (*.bmp)|*.bmp",
                                           "PNG Image (*.png)|*.png|BMP Image (*.bmp)|*.bmp",
                                           "Image PNG (*.png)|*.png|Image BMP (*.bmp)|*.bmp",
                                           "Immagine PNG (*.png)|*.png|Immagine BMP (*.bmp)|*.bmp",
                                           "Imagen PNG (*.png)|*.png|Imagen BMP (*.bmp)|*.bmp");
    public string MsgNoTreemap => S("Kein Treemap vorhanden.", "No Treemap available.", "Aucune Treemap disponible.", "Nessuna Treemap disponibile.", "No hay Treemap disponible.");
    public string MsgRenderError => S("Render-Fehler:", "Render Error:", "Erreur de rendu :", "Errore di rendering:", "Error de renderizado:");
    public string MsgSaveError => S("Fehler beim Speichern:", "Error Saving:", "Erreur lors de l'enregistrement :", "Errore durante il salvataggio:", "Error al guardar:");

    // ── Legende ──────────────────────────────────────────────────────────────
    public string LegImages => S("Bilder", "Images", "Images", "Immagini", "Imágenes");
    public string LegVideos => S("Videos", "Videos", "Vidéos", "Video", "Videos");
    public string LegAudio => S("Audio", "Audio", "Audio", "Audio", "Audio");
    public string LegDocuments => S("Dokumente", "Documents", "Documents", "Documenti", "Documentos");
    public string LegArchives => S("Archive", "Archives", "Archives", "Archivi", "Archivos");
    public string LegExecutables => S("Ausführbar", "Executables", "Exécutables", "Eseguibili", "Ejecutables");
    public string LegSourceCode => S("Quellcode", "Source Code", "Code source", "Codice sorgente", "Código fuente");
    public string LegDatabase => S("Datenbank", "Database", "Base de données", "Database", "Base de datos");
    public string LegFonts => S("Schriften", "Fonts", "Polices", "Font", "Fuentes");
    public string LegOther => S("Sonstiges", "Other", "Autre", "Altro", "Otros");

    // ── Multi-Laufwerk ────────────────────────────────────────────────────────
    public string MultiDriveFreeOf => S("frei /", "free /", "libre /", "liberi /", "libre /");

    // ── Scan-Texte (ViewModel) ─────────────────────────────────────────────
    public string ScanDriveSizeLabel => S("Laufwerksgröße:", "Drive Size:", "Taille du lecteur :", "Dimensione unità:", "Tamaño de unidad:");
    public string ScanDriveFreeLabel => S("Frei:", "Free:", "Libre :", "Libero:", "Libre:");
    public string ScanDiskFreeOf => S("von", "of", "sur", "di", "de");
    public string ScanFileCount => S("Dateien", "Files", "Fichiers", "File", "Archivos");
    public string ScanReadCount => S("Eintraege eingelesen", "Entries read", "Entrees lues", "Voci lette", "Entradas leidas");
    public string ScanTotal => S("Gesamt:", "Total:", "Total :", "Totale:", "Total:");
    public string OpenFolderDesc => S("Ordner für AtlayaView auswählen", "Select Folder for AtlayaView", "Sélectionner un dossier pour AtlayaView", "Seleziona cartella per AtlayaView", "Seleccionar carpeta para AtlayaView");
    public string FreeSpaceNodeFmt => S("Freier Speicher ({0})", "Free Space ({0})", "Espace libre ({0})", "Spazio libero ({0})", "Espacio libre ({0})");

    // ── Optionen-Dialog ──────────────────────────────────────────────────────
    public string OptWindowTitle => S("AtlayaView – Optionen", "AtlayaView – Options", "AtlayaView – Options", "AtlayaView – Opzioni", "AtlayaView – Opciones");
    public string OptDialogTitle => S("Darstellung – Cushion-Treemap", "Appearance – Cushion-Treemap", "Apparence – Cushion-Treemap", "Aspetto – Cushion-Treemap", "Apariencia – Cushion-Treemap");
    public string OptCushionHeight => S("Kissenintensität", "Cushion Intensity", "Intensité du coussin", "Intensità del cuscino", "Intensidad del almohadón");
    public string OptCushionHeightHint => S("Stärke des 3D-Kissens (höher = stärker gewölbt)",
                                            "Strength of 3D cushion (higher = more curved)",
                                            "Intensité du coussin 3D (plus élevé = plus courbé)",
                                            "Intensità del cuscino 3D (più alto = più curvo)",
                                            "Intensidad del almohadón 3D (mayor = más curvado)");
    public string OptCushionDecay => S("Kissendämpfung pro Tiefe", "Cushion Decay per Level", "Atténuation du coussin par niveau", "Attenuazione del cuscino per livello", "Atenuación del almohadón por nivel");
    public string OptCushionDecayHint => S("Kleiner = flacher in tieferen Ebenen",
                                           "Smaller = flatter at deeper levels",
                                           "Plus petit = plus plat aux niveaux inférieurs",
                                           "Minore = più piatto nei livelli più profondi",
                                           "Menor = más plano en niveles más profundos");
    public string OptAmbientLight => S("Umgebungslicht", "Ambient Light", "Lumière ambiante", "Luce ambientale", "Luz ambiental");
    public string OptAmbientLightHint => S("Helligkeit der Schattenseiten (höher = weniger Kontrast)",
                                           "Shadow side brightness (higher = less contrast)",
                                           "Luminosité des zones d'ombre (plus élevé = moins de contraste)",
                                           "Luminosità delle zone d'ombra (più alto = meno contrasto)",
                                           "Brillo del lado sombreado (mayor = menos contraste)");
    public string OptMinPixel => S("Minimale Pixelgröße", "Minimum Pixel Size", "Taille minimale en pixels", "Dimensione minima in pixel", "Tamaño mínimo en píxeles");
    public string OptMinPixelHint => S("Dateifelder kleiner als dieser Wert werden ausgelassen",
                                           "File cells smaller than this value are skipped",
                                           "Les cellules de fichier inférieures à cette valeur sont ignorées",
                                           "Le celle file più piccole di questo valore vengono omesse",
                                           "Las celdas menores a este valor se omiten");
    public string OptDrawBorders => S("Verzeichnisgrenzen zeichnen", "Draw Directory Borders", "Dessiner les bordures des répertoires", "Disegna bordi delle directory", "Dibujar bordes de directorios");
    public string OptFreeSpaceCushion => S("Freier Speicher als Kissen anzeigen",
                                           "Show Free Space as Cushion",
                                           "Afficher l'espace libre en coussin",
                                           "Mostra spazio libero come cuscino",
                                           "Mostrar espacio libre como almohadilla");

    // ── Schnellscan (Options-Dialog) ──────────────────────────────────────────
    public string OptScanSectionTitle => S("Scan-Geschwindigkeit", "Scan Speed", "Vitesse d'analyse", "Velocità di scansione", "Velocidad de análisis");
    public string OptFastScan => S("Schneller Scan (benötigt Admin-Rechte)",
                                   "Fast Scan (requires administrator rights)",
                                   "Analyse rapide (nécessite des droits administrateur)",
                                   "Scansione rapida (richiede diritti di amministratore)",
                                   "Análisis rápido (requiere derechos de administrador)");
    public string OptFastScanHint => S(
        "Liest NTFS-Laufwerke direkt über den USN-Änderungsjournal-Mechanismus statt Ordner für Ordner – deutlich schneller, aber nur für ganze Laufwerke und nur mit erhöhten Rechten. Ohne Adminrechte läuft der normale Scan unverändert weiter.",
        "Reads NTFS drives directly via the USN change journal mechanism instead of folder by folder – much faster, but only for whole drives and only with elevated rights. Without admin rights, the normal scan keeps working unchanged.",
        "Lit les lecteurs NTFS directement via le journal des modifications USN au lieu de dossier en dossier – bien plus rapide, mais uniquement pour des lecteurs entiers et avec des droits élevés. Sans droits administrateur, l'analyse normale continue de fonctionner sans changement.",
        "Legge le unità NTFS direttamente tramite il journal delle modifiche USN invece che cartella per cartella – molto più veloce, ma solo per unità intere e solo con diritti elevati. Senza diritti di amministratore la scansione normale continua a funzionare invariata.",
        "Lee las unidades NTFS directamente mediante el journal de cambios USN en lugar de carpeta por carpeta – mucho más rápido, pero solo para unidades completas y solo con derechos elevados. Sin derechos de administrador, el análisis normal sigue funcionando sin cambios.");
    public string MsgFastScanElevateTitle => S("Adminrechte erforderlich", "Administrator rights required", "Droits administrateur requis", "Diritti di amministratore richiesti", "Se requieren derechos de administrador");
    public string MsgFastScanElevateText => S(
        "Der schnelle Scan braucht erhöhte Rechte, um NTFS-Laufwerke direkt lesen zu können. AtlayaView jetzt mit Administratorrechten neu starten?",
        "Fast scan needs elevated rights to read NTFS drives directly. Restart AtlayaView as Administrator now?",
        "L'analyse rapide nécessite des droits élevés pour lire directement les lecteurs NTFS. Redémarrer AtlayaView en tant qu'administrateur maintenant ?",
        "La scansione rapida richiede diritti elevati per leggere direttamente le unità NTFS. Riavviare ora AtlayaView come amministratore?",
        "El análisis rápido necesita derechos elevados para leer las unidades NTFS directamente. ¿Reiniciar AtlayaView ahora como administrador?");
    public string MsgFastScanElevateDenied => S(
        "Ohne erhöhte Rechte bleibt der schnelle Scan ausgeschaltet, AtlayaView läuft normal weiter.",
        "Without elevated rights, fast scan stays off – AtlayaView keeps running normally.",
        "Sans droits élevés, l'analyse rapide reste désactivée – AtlayaView continue de fonctionner normalement.",
        "Senza diritti elevati, la scansione rapida resta disattivata – AtlayaView continua a funzionare normalmente.",
        "Sin derechos elevados, el análisis rápido permanece desactivado – AtlayaView sigue funcionando con normalidad.");

    // ── Updates (Options-Dialog + Update-Dialog) ──────────────────────────────
    public string OptUpdateSectionTitle => S("Updates", "Updates", "Mises à jour", "Aggiornamenti", "Actualizaciones");
    public string OptUpdateMode => S("Update-Prüfung", "Update check", "Vérification des mises à jour", "Controllo aggiornamenti", "Comprobación de actualizaciones");
    public string OptUpdateModeManual => S("Auf Updates prüfen (manuell)", "Check for updates (manual)", "Vérifier les mises à jour (manuel)", "Controlla aggiornamenti (manuale)", "Buscar actualizaciones (manual)");
    public string OptUpdateModeAutoCheck => S("Automatisch prüfen", "Check automatically", "Vérifier automatiquement", "Controlla automaticamente", "Comprobar automáticamente");
    public string OptUpdateModeAutoApply => S("Automatisch prüfen und updaten", "Check and update automatically", "Vérifier et mettre à jour automatiquement", "Controlla e aggiorna automaticamente", "Comprobar y actualizar automáticamente");
    public string OptUpdateInterval => S("Prüfabstand", "Check interval", "Intervalle de vérification", "Intervallo di controllo", "Intervalo de comprobación");
    public string OptUpdateIntervalStartup => S("Bei jedem Start", "On every start", "À chaque démarrage", "A ogni avvio", "En cada inicio");
    public string OptUpdateIntervalDaily => S("Täglich", "Daily", "Quotidien", "Giornaliero", "Diario");
    public string OptUpdateIntervalWeekly => S("Wöchentlich", "Weekly", "Hebdomadaire", "Settimanale", "Semanal");
    public string OptUpdateIntervalMonthly => S("Monatlich", "Monthly", "Mensuel", "Mensile", "Mensual");
    public string OptUpdateIntervalYearly => S("Jährlich", "Yearly", "Annuel", "Annuale", "Anual");

    public string UpdateDlgTitle => S("Update", "Update", "Mise à jour", "Aggiornamento", "Actualización");
    public string UpdateDlgChecking => S("Prüfe auf Updates …", "Checking for updates …", "Vérification des mises à jour …", "Controllo aggiornamenti in corso …", "Buscando actualizaciones …");
    public string UpdateDlgCurrent => S("Installierte Version:", "Installed version:", "Version installée :", "Versione installata:", "Versión instalada:");
    public string UpdateDlgUpToDate => S("Du hast die aktuelle Version.", "You have the latest version.", "Vous avez la dernière version.", "Hai la versione più recente.", "Tienes la última versión.");
    public string UpdateDlgAvailable => S("Neue Version verfügbar:", "New version available:", "Nouvelle version disponible :", "Nuova versione disponibile:", "Nueva versión disponible:");
    public string UpdateDlgUnreachable => S("Update-Server nicht erreichbar.", "Update server unreachable.", "Serveur de mise à jour injoignable.", "Server degli aggiornamenti non raggiungibile.", "Servidor de actualizaciones inaccesible.");
    public string UpdateDlgNoMatchingVariant => S(
        "Für die installierte Variante ist kein automatischer Download verfügbar – bitte manuell von der Webseite laden.",
        "No automatic download is available for the installed variant – please download it manually from the website.",
        "Aucun téléchargement automatique n'est disponible pour la variante installée – veuillez le télécharger manuellement depuis le site.",
        "Per la variante installata non è disponibile alcun download automatico – scaricala manualmente dal sito web.",
        "No hay descarga automática disponible para la variante instalada – descárgala manualmente desde la web.");
    public string UpdateDlgApplyNow => S("Jetzt aktualisieren", "Update now", "Mettre à jour maintenant", "Aggiorna ora", "Actualizar ahora");
    public string UpdateDlgApplying => S(
        "Update wird heruntergeladen und installiert – AtlayaView startet gleich neu …",
        "Downloading and installing the update – AtlayaView will restart shortly …",
        "Téléchargement et installation de la mise à jour – AtlayaView va redémarrer …",
        "Download e installazione dell'aggiornamento in corso – AtlayaView si riavvierà a breve …",
        "Descargando e instalando la actualización – AtlayaView se reiniciará en breve …");
    public string UpdateDlgDownloadFailed => S(
        "Update konnte nicht heruntergeladen werden:", "Could not download the update:",
        "Impossible de télécharger la mise à jour :", "Impossibile scaricare l'aggiornamento:",
        "No se pudo descargar la actualización:");
    public string UpdateDlgElevationCancelled => S(
        "Aktualisierung abgebrochen: Der Installationsordner ist geschützt (z. B. „Program Files“) und benötigt Administratorrechte, die Sicherheitsabfrage wurde aber nicht bestätigt.",
        "Update cancelled: the install folder is protected (e.g. “Program Files”) and requires administrator rights, but the prompt was not confirmed.",
        "Mise à jour annulée : le dossier d'installation est protégé (par ex. « Program Files ») et nécessite des droits d'administrateur, mais l'invite n'a pas été confirmée.",
        "Aggiornamento annullato: la cartella di installazione è protetta (ad es. «Program Files») e richiede diritti di amministratore, ma la richiesta non è stata confermata.",
        "Actualización cancelada: la carpeta de instalación está protegida (p. ej. «Program Files») y requiere derechos de administrador, pero no se confirmó el aviso.");

    public string BtnDefaults => S("Standard", "Defaults", "Par défaut", "Predefiniti", "Predeterminado");
    public string BtnCancelDialog => S("Abbrechen", "Cancel", "Annuler", "Annulla", "Cancelar");
    public string BtnOkText => S("OK", "OK", "OK", "OK", "OK");

    // ── Filter-Dialog ────────────────────────────────────────────────────────
    public string FilterWindowTitle => S("AtlayaView – Filter", "AtlayaView – Filter", "AtlayaView – Filtre", "AtlayaView – Filtro", "AtlayaView – Filtro");
    public string FilterDialogTitle => S("Dateien einschränken", "Restrict Files", "Restreindre les fichiers", "Limita file", "Restringir archivos");
    public string FilterMinSize => S("Minimale Dateigröße", "Minimum File Size", "Taille minimale des fichiers", "Dimensione minima del file", "Tamaño mínimo de archivo");
    public string FilterNoLimit => S("(0 = kein Limit)", "(0 = no limit)", "(0 = sans limite)", "(0 = nessun limite)", "(0 = sin límite)");
    public string FilterExcludeExt => S("Erweiterungen ausschließen", "Exclude Extensions", "Exclure les extensions", "Escludi estensioni", "Excluir extensiones");
    public string FilterCommonTypes => S("Häufige Typen:", "Common Types:", "Types courants :", "Tipi comuni:", "Tipos comunes:");
    public string FilterCustomExt => S("Eigene Erweiterung:", "Custom Extension:", "Extension personnalisée :", "Estensione personalizzata:", "Extensión personalizada:");
    public string FilterCustomTip => S("z.B. .xyz oder .backup", "e.g. .xyz or .backup", "ex. .xyz ou .backup", "es. .xyz o .backup", "p.ej. .xyz o .backup");
    public string FilterAdd => S("+ Hinzufügen", "+ Add", "+ Ajouter", "+ Aggiungi", "+ Añadir");
    public string FilterActive => S("Aktiv ausgeschlossen:", "Currently Excluded:", "Actuellement exclu :", "Attualmente esclusi:", "Excluidos actualmente:");
    public string FilterHidden => S("Versteckte Dateien ausblenden", "Hide Hidden Files", "Masquer les fichiers cachés", "Nascondi file nascosti", "Ocultar archivos ocultos");
    public string FilterSystem => S("Systemdateien ausblenden", "Hide System Files", "Masquer les fichiers système", "Nascondi file di sistema", "Ocultar archivos del sistema");
    public string BtnResetText => S("Zurücksetzen", "Reset", "Réinitialiser", "Ripristina", "Restablecer");

    // ── Farbschema-Dialog ────────────────────────────────────────────────────
    public string ColorWindowTitle => S("AtlayaView – Farbschema", "AtlayaView – Color Scheme", "AtlayaView – Schéma de couleurs", "AtlayaView – Schema colori", "AtlayaView – Esquema de colores");
    public string ColorDialogTitle => S("Erweiterungsfarben bearbeiten", "Edit Extension Colors", "Modifier les couleurs des extensions", "Modifica colori delle estensioni", "Editar colores de extensiones");
    public string ColorSearchHint => S("🔍 Suchen …", "🔍 Search …", "🔍 Rechercher …", "🔍 Cerca …", "🔍 Buscar …");
    public string ColorSelectExt => S("Erweiterung wählen …", "Select Extension …", "Sélectionner une extension …", "Seleziona estensione …", "Seleccionar extensión …");
    public string ColorPreviewText => S("Vorschau", "Preview", "Aperçu", "Anteprima", "Vista previa");
    public string BtnResetOneText => S("Zurücksetzen", "Reset", "Réinitialiser", "Ripristina", "Restablecer");
    public string ColorQuickSelect => S("Schnellauswahl", "Quick Select", "Sélection rapide", "Selezione rapida", "Selección rápida");
    public string BtnResetAllText => S("Alle zurücksetzen", "Reset All", "Tout réinitialiser", "Ripristina tutto", "Restablecer todo");

    // ── Datei-Öffner-Sektion im Farbschema-Dialog ───────────────────────────────
    public string OpenerLabel => S("Öffnen mit:", "Open with:", "Ouvrir avec :", "Apri con:", "Abrir con:");
    public string OpenerDefault => S("(Systemstandard)", "(System Default)", "(Défaut système)", "(Predefinito di sistema)", "(Predeterminado)");
    public string OpenerBrowse => S("📂", "📂", "📂", "📂", "📂");
    public string OpenerBrowseTip => S("Programm wählen …", "Choose Program …", "Choisir un programme …", "Scegli programma …", "Elegir programa …");
    public string OpenerClear => S("✕", "✕", "✕", "✕", "✕");
    public string OpenerClearTip => S("Systemstandard verwenden", "Use System Default", "Utiliser le défaut système", "Usa predefinito di sistema", "Usar predeterminado del sistema");

    // ── Erweiterung hinzufügen (Farbschema-Dialog) ──────────────────────────────
    public string ColorAddExtHint => S(".neu", ".new", ".nouv", ".nuovo", ".nuevo");
    public string ColorAddExtButton => S("+ Hinzufügen", "+ Add", "+ Ajouter", "+ Aggiungi", "+ Añadir");
    public string ColorAddExtTooltip => S("Neue Datei-Erweiterung zur Liste hinzufügen", "Add a new file extension to the list", "Ajouter une nouvelle extension de fichier à la liste", "Aggiungi una nuova estensione di file all'elenco", "Añadir una nueva extensión de archivo a la lista");
    public string ColorAddExtInvalid => S("Ungültige Erweiterung – z. B. „.mpg“ eingeben.", "Invalid extension – e.g. enter “.mpg”.", "Extension invalide – saisissez par ex. « .mpg ».", "Estensione non valida – inserisci ad es. «.mpg».", "Extensión no válida – introduce, p. ej., «.mpg».");
    public string ColorAddExtDuplicate => S("Diese Erweiterung existiert bereits.", "This extension already exists.", "Cette extension existe déjà.", "Questa estensione esiste già.", "Esta extensión ya existe.");

    // ── Farbprofile (Farbschema-Dialog) ─────────────────────────────────────────
    public string ColorProfilesButton => S("🎨 Farbprofile …", "🎨 Color Profiles …", "🎨 Profils de couleurs …", "🎨 Profili colore …", "🎨 Perfiles de color …");
    public string ProfileWindowTitle => S("AtlayaView – Farbprofile", "AtlayaView – Color Profiles", "AtlayaView – Profils de couleurs", "AtlayaView – Profili colore", "AtlayaView – Perfiles de color");
    public string ProfileDialogTitle => S("Farbprofile verwalten", "Manage Color Profiles", "Gérer les profils de couleurs", "Gestisci profili colore", "Gestionar perfiles de color");
    public string ProfileDialogHint => S("Ein Farbprofil fasst mehrere Erweiterungen zusammen und weist ihnen auf einen Klick dieselbe Farbe zu.", "A color profile groups several extensions together and assigns them all the same color in one click.", "Un profil de couleurs regroupe plusieurs extensions et leur attribue la même couleur en un clic.", "Un profilo colore raggruppa più estensioni e assegna loro lo stesso colore con un clic.", "Un perfil de color agrupa varias extensiones y les asigna el mismo color con un clic.");
    public string ProfileListEmpty => S("Noch keine Farbprofile – „Neu“ klicken, um eines anzulegen.", "No color profiles yet – click “New” to create one.", "Pas encore de profils de couleurs – cliquez sur « Nouveau » pour en créer un.", "Nessun profilo colore ancora – fai clic su «Nuovo» per crearne uno.", "Aún no hay perfiles de color – haz clic en «Nuevo» para crear uno.");
    public string ProfileExtCountFmt => S("{0} Erweiterungen", "{0} extensions", "{0} extensions", "{0} estensioni", "{0} extensiones");
    public string ProfileNameLabel => S("Name", "Name", "Nom", "Nome", "Nombre");
    public string ProfileNameHint => S("z. B. Videos", "e.g. Videos", "p. ex. Vidéos", "ad es. Video", "p. ej. Vídeos");
    public string ProfileExtensionsLabel => S("Erweiterungen auswählen (Haken setzen)", "Select extensions (check the ones you want)", "Sélectionner les extensions (cocher)", "Seleziona le estensioni (spunta)", "Seleccionar extensiones (marcar)");
    public string ProfileExtensionsHint => S("Angehakte Erweiterungen gehören zum Profil. Neue, noch unbekannte Erweiterung unten eingeben und mit „+“ hinzufügen.", "Checked extensions belong to the profile. Type a new, not-yet-known extension below and add it with “+”.", "Les extensions cochées font partie du profil. Saisissez une nouvelle extension inconnue ci-dessous et ajoutez-la avec « + ».", "Le estensioni spuntate appartengono al profilo. Digita di seguito una nuova estensione non ancora nota e aggiungila con «+».", "Las extensiones marcadas pertenecen al perfil. Escribe abajo una extensión nueva aún desconocida y añádela con «+».");
    public string ProfileColorTargetChecked => S("Farbe für die angehakten Erweiterungen", "Color for the checked extensions", "Couleur pour les extensions cochées", "Colore per le estensioni spuntate", "Color para las extensiones marcadas");
    public string ProfileBtnUseListColor => S("Farbe aus Grundliste übernehmen", "Use color from base list", "Utiliser la couleur de la liste de base", "Usa il colore dall'elenco base", "Usar color de la lista base");
    public string ProfileNoneCheckedStatus => S("Bitte zuerst mindestens eine Erweiterung ankreuzen.", "Please check at least one extension first.", "Veuillez d'abord cocher au moins une extension.", "Spunta prima almeno un'estensione.", "Marca primero al menos una extensión.");
    public string ProfileBtnNew => S("Neu", "New", "Nouveau", "Nuovo", "Nuevo");
    public string ProfileBtnSave => S("Speichern", "Save", "Enregistrer", "Salva", "Guardar");
    public string ProfileBtnDelete => S("Löschen", "Delete", "Supprimer", "Elimina", "Eliminar");
    public string ProfileBtnApply => S("Auf Liste anwenden", "Apply to List", "Appliquer à la liste", "Applica all'elenco", "Aplicar a la lista");
    public string ProfileBtnClose => S("Schließen", "Close", "Fermer", "Chiudi", "Cerrar");
    public string ProfileNameRequired => S("Bitte einen Namen für das Profil eingeben.", "Please enter a name for the profile.", "Veuillez saisir un nom pour le profil.", "Inserisci un nome per il profilo.", "Introduce un nombre para el perfil.");
    public string ProfileExtensionsRequired => S("Bitte mindestens eine Erweiterung eingeben.", "Please enter at least one extension.", "Veuillez saisir au moins une extension.", "Inserisci almeno un'estensione.", "Introduce al menos una extensión.");
    public string ProfileDeleteConfirm => S("Profil „{0}“ wirklich löschen?", "Really delete profile “{0}”?", "Vraiment supprimer le profil « {0} » ?", "Eliminare davvero il profilo «{0}»?", "¿Eliminar realmente el perfil «{0}»?");
    public string ProfileAppliedStatus => S("„{0}“ ist jetzt aktiv ({1} Erweiterungen farbig, alle übrigen silbergrau) – gespeichert.", "“{0}” is now active ({1} extensions colored, all others silver-gray) – saved.", "« {0} » est maintenant actif ({1} extensions colorées, toutes les autres gris argenté) – enregistré.", "«{0}» è ora attivo ({1} estensioni colorate, tutte le altre grigio argento) – salvato.", "«{0}» está ahora activo ({1} extensiones coloreadas, todas las demás gris plateado) – guardado.");
    public string ProfileDefaultName => S("Startprofil", "Default profile", "Profil par défaut", "Profilo predefinito", "Perfil predeterminado");
    public string ProfileDefaultCount => S("Standardfarben für alle Erweiterungen", "Default colors for all extensions", "Couleurs par défaut pour toutes les extensions", "Colori predefiniti per tutte le estensioni", "Colores predeterminados para todas las extensiones");
    public string ProfileDefaultAppliedStatus => S("Startprofil aktiv – alle Erweiterungen zeigen wieder ihre Standardfarben.", "Default profile active – all extensions show their default colors again.", "Profil par défaut actif – toutes les extensions affichent à nouveau leurs couleurs par défaut.", "Profilo predefinito attivo – tutte le estensioni mostrano di nuovo i loro colori predefiniti.", "Perfil predeterminado activo – todas las extensiones vuelven a mostrar sus colores predeterminados.");
    public string ProfileDefaultNotEditable => S("Das Startprofil ist fest eingebaut – zum Bearbeiten ein eigenes Profil anlegen oder auswählen.", "The default profile is built in – create or select a custom profile to edit.", "Le profil par défaut est intégré – créez ou sélectionnez un profil personnalisé pour le modifier.", "Il profilo predefinito è integrato – crea o seleziona un profilo personalizzato per modificarlo.", "El perfil predeterminado está integrado – crea o selecciona un perfil personalizado para editarlo.");
}
