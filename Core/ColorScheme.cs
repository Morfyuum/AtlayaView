using System.Windows.Media;

namespace AtlayaView.Core;

/// <summary>
/// Ordnet Datei-Erweiterungen einer Basis-Farbe zu (für die Cushion-Schattierung).
/// </summary>
public static class ColorScheme
{
    private static readonly Dictionary<string, Color> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Bilder ───────────────────────────────────────────────────────────
        { ".jpg",  Color.FromRgb( 65, 130, 220) },
        { ".jpeg", Color.FromRgb( 65, 130, 220) },
        { ".png",  Color.FromRgb( 50, 110, 200) },
        { ".gif",  Color.FromRgb(100, 160, 230) },
        { ".bmp",  Color.FromRgb(130, 170, 220) },
        { ".tiff", Color.FromRgb( 80, 140, 210) },
        { ".tif",  Color.FromRgb( 80, 140, 210) },
        { ".webp", Color.FromRgb( 70, 150, 225) },
        { ".svg",  Color.FromRgb( 40, 120, 215) },
        { ".ico",  Color.FromRgb(110, 155, 215) },
        { ".heic", Color.FromRgb( 60, 125, 205) },
        { ".raw",  Color.FromRgb( 55, 120, 210) },

        // ── Videos ──────────────────────────────────────────────────────────
        { ".mp4",  Color.FromRgb( 50, 200,  80) },
        { ".avi",  Color.FromRgb( 40, 160,  60) },
        { ".mkv",  Color.FromRgb( 30, 140,  50) },
        { ".mov",  Color.FromRgb(100, 210, 100) },
        { ".wmv",  Color.FromRgb( 60, 180,  70) },
        { ".flv",  Color.FromRgb( 80, 190,  80) },
        { ".m4v",  Color.FromRgb( 55, 195,  75) },
        { ".webm", Color.FromRgb( 70, 185,  75) },

        // ── Audio ────────────────────────────────────────────────────────────
        { ".mp3",  Color.FromRgb(255, 165,  30) },
        { ".wav",  Color.FromRgb(255, 140,  20) },
        { ".flac", Color.FromRgb(255, 200,  40) },
        { ".aac",  Color.FromRgb(255, 185,  30) },
        { ".ogg",  Color.FromRgb(245, 175,  25) },
        { ".wma",  Color.FromRgb(250, 155,  20) },
        { ".m4a",  Color.FromRgb(255, 195,  35) },
        { ".opus", Color.FromRgb(250, 170,  25) },

        // ── Dokumente ────────────────────────────────────────────────────────
        { ".pdf",  Color.FromRgb(220,  60,  30) },
        { ".doc",  Color.FromRgb( 40, 130, 220) },
        { ".docx", Color.FromRgb( 40, 130, 220) },
        { ".xls",  Color.FromRgb( 30, 160,  80) },
        { ".xlsx", Color.FromRgb( 30, 160,  80) },
        { ".ppt",  Color.FromRgb(200,  80,  30) },
        { ".pptx", Color.FromRgb(200,  80,  30) },
        { ".txt",  Color.FromRgb(180, 180, 180) },
        { ".md",   Color.FromRgb(160, 160, 200) },
        { ".csv",  Color.FromRgb( 60, 170, 100) },
        { ".odt",  Color.FromRgb( 50, 140, 215) },
        { ".ods",  Color.FromRgb( 50, 165,  85) },
        { ".rtf",  Color.FromRgb(170, 170, 190) },

        // ── Archive ──────────────────────────────────────────────────────────
        { ".zip",  Color.FromRgb(155,  50, 220) },
        { ".rar",  Color.FromRgb(140,  40, 200) },
        { ".7z",   Color.FromRgb(160,  60, 230) },
        { ".tar",  Color.FromRgb(130,  30, 190) },
        { ".gz",   Color.FromRgb(150,  45, 210) },
        { ".bz2",  Color.FromRgb(145,  40, 205) },
        { ".xz",   Color.FromRgb(155,  50, 215) },
        { ".cab",  Color.FromRgb(135,  35, 195) },
        { ".iso",  Color.FromRgb(125,  25, 180) },
        { ".img",  Color.FromRgb(120,  20, 175) },

        // ── Ausführbare Dateien ──────────────────────────────────────────────
        { ".exe",  Color.FromRgb(220,  30,  50) },
        { ".dll",  Color.FromRgb(190,  30,  40) },
        { ".msi",  Color.FromRgb(200,  20,  40) },
        { ".bat",  Color.FromRgb(200,  80,  80) },
        { ".cmd",  Color.FromRgb(190,  75,  75) },
        { ".ps1",  Color.FromRgb(100, 100, 220) },
        { ".sh",   Color.FromRgb( 80, 180,  80) },

        // ── Quellcode ────────────────────────────────────────────────────────
        { ".cs",   Color.FromRgb( 80, 190, 210) },
        { ".vb",   Color.FromRgb( 90, 200, 215) },
        { ".js",   Color.FromRgb(240, 210,  60) },
        { ".jsx",  Color.FromRgb(240, 210,  60) },
        { ".ts",   Color.FromRgb( 49, 120, 198) },
        { ".tsx",  Color.FromRgb( 49, 120, 198) },
        { ".py",   Color.FromRgb( 55, 130, 180) },
        { ".java", Color.FromRgb(230, 100,  20) },
        { ".kt",   Color.FromRgb(160,  90, 210) },
        { ".cpp",  Color.FromRgb( 20, 160, 150) },
        { ".c",    Color.FromRgb( 20, 150, 140) },
        { ".h",    Color.FromRgb( 25, 140, 130) },
        { ".hpp",  Color.FromRgb( 25, 155, 145) },
        { ".rs",   Color.FromRgb(200, 100,  50) },
        { ".go",   Color.FromRgb( 90, 190, 200) },
        { ".php",  Color.FromRgb(140, 100, 190) },
        { ".rb",   Color.FromRgb(200,  50,  50) },
        { ".swift",Color.FromRgb(240, 100,  50) },
        { ".html", Color.FromRgb(240,  90,  40) },
        { ".htm",  Color.FromRgb(240,  90,  40) },
        { ".css",  Color.FromRgb( 40, 100, 220) },
        { ".scss", Color.FromRgb( 50, 110, 215) },
        { ".xml",  Color.FromRgb(255, 150,  30) },
        { ".json", Color.FromRgb(210, 200,  50) },
        { ".yaml", Color.FromRgb(220, 190,  50) },
        { ".yml",  Color.FromRgb(220, 190,  50) },
        { ".sql",  Color.FromRgb(100, 160, 210) },
        { ".lua",  Color.FromRgb( 50, 100, 200) },

        // ── System / Datenbanken ─────────────────────────────────────────────
        { ".sys",  Color.FromRgb(120, 120, 130) },
        { ".ini",  Color.FromRgb(150, 150, 160) },
        { ".cfg",  Color.FromRgb(145, 145, 155) },
        { ".log",  Color.FromRgb(160, 155, 145) },
        { ".db",   Color.FromRgb( 70, 120, 180) },
        { ".sqlite",Color.FromRgb( 75, 125, 185) },
        { ".mdb",  Color.FromRgb( 60, 110, 170) },
        { ".pdb",  Color.FromRgb(100, 100, 130) },

        // ── Schriften ────────────────────────────────────────────────────────
        { ".ttf",  Color.FromRgb(180, 140,  90) },
        { ".otf",  Color.FromRgb(185, 145,  95) },
        { ".woff", Color.FromRgb(175, 135,  85) },
        { ".woff2",Color.FromRgb(180, 140,  90) },

        // ── Freier Speicher (synthetisch) ────────────────────────────────────
        { ".__free__", Color.FromRgb(65, 115, 145) },  // Stahlblau → freier Speicher
    };

    private static readonly Color _defaultColor = Color.FromRgb(100, 110, 130);

    /// <summary>Benutzer-definierte Farbüberschreibungen (Erweiterung → Farbe).</summary>
    private static readonly Dictionary<string, Color> _overrides = new(StringComparer.OrdinalIgnoreCase);

    // ── Öffentliche API ──────────────────────────────────────────────────────
    public static Color GetColor(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return _defaultColor;
        if (_overrides.TryGetValue(extension, out var ov)) return ov;
        if (_map.TryGetValue(extension, out var c)) return c;
        return _defaultColor;
    }

    /// <summary>Setzt eine benutzerdefinierte Farbe für eine Erweiterung.</summary>
    public static void SetColor(string extension, Color color)
        => _overrides[extension] = color;

    /// <summary>Setzt alle benutzerdefinierten Farben zurück auf die Standardwerte.</summary>
    public static void ResetAll() => _overrides.Clear();

    /// <summary>Gibt true zurück, wenn für die Erweiterung eine Override-Farbe gesetzt ist.</summary>
    public static bool HasOverride(string extension)
        => _overrides.ContainsKey(extension);

    /// <summary>Alle benutzerdefinierten Farbüberschreibungen (für Persistenz).</summary>
    public static IReadOnlyDictionary<string, Color> Overrides => _overrides;

    /// <summary>Alle bekannten Erweiterungen (Standard + Override) alphabetisch.</summary>
    public static IEnumerable<string> AllExtensions
        => _map.Keys.Concat(_overrides.Keys).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(e => e);

    /// <summary>Alle Einträge (inkl. Overrides) als flaches Dictionary.</summary>
    public static IReadOnlyDictionary<string, Color> EffectiveMap
    {
        get
        {
            var combined = new Dictionary<string, Color>(_map, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _overrides) combined[kv.Key] = kv.Value;
            return combined;
        }
    }

    /// <summary>Gibt alle Standardeinträge zurück (für Legende / Reset).</summary>
    public static IReadOnlyDictionary<string, Color> Map => _map;

    // ── Kategorie-Zuordnung ──────────────────────────────────────────────────
    private static readonly Dictionary<string, string> _categoryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpg","Bilder" },{ ".jpeg","Bilder" },{ ".png","Bilder" },{ ".gif","Bilder" },
        { ".bmp","Bilder" },{ ".tiff","Bilder" },{ ".tif","Bilder" },{ ".webp","Bilder" },
        { ".svg","Bilder" },{ ".ico","Bilder"  },{ ".heic","Bilder"},{ ".raw","Bilder"  },

        { ".mp4","Videos" },{ ".avi","Videos" },{ ".mkv","Videos" },{ ".mov","Videos" },
        { ".wmv","Videos" },{ ".flv","Videos" },{ ".m4v","Videos" },{ ".webm","Videos" },

        { ".mp3","Audio" },{ ".wav","Audio" },{ ".flac","Audio" },{ ".aac","Audio" },
        { ".ogg","Audio" },{ ".wma","Audio" },{ ".m4a","Audio" },{ ".opus","Audio" },

        { ".pdf","Dokumente" },{ ".doc","Dokumente"  },{ ".docx","Dokumente" },
        { ".xls","Dokumente" },{ ".xlsx","Dokumente" },{ ".ppt","Dokumente"  },
        { ".pptx","Dokumente"},{ ".txt","Dokumente"  },{ ".md","Dokumente"   },
        { ".csv","Dokumente" },{ ".odt","Dokumente"  },{ ".ods","Dokumente"  },
        { ".rtf","Dokumente" },

        { ".zip","Archive" },{ ".rar","Archive" },{ ".7z","Archive" },{ ".tar","Archive" },
        { ".gz","Archive"  },{ ".bz2","Archive" },{ ".xz","Archive" },{ ".cab","Archive" },
        { ".iso","Archive" },{ ".img","Archive" },

        { ".exe","Ausführbar" },{ ".dll","Ausführbar" },{ ".msi","Ausführbar" },
        { ".bat","Ausführbar" },{ ".cmd","Ausführbar" },{ ".ps1","Ausführbar" },
        { ".sh","Ausführbar"  },

        { ".cs","Quellcode"  },{ ".vb","Quellcode"   },{ ".js","Quellcode"  },
        { ".jsx","Quellcode" },{ ".ts","Quellcode"   },{ ".tsx","Quellcode" },
        { ".py","Quellcode"  },{ ".java","Quellcode" },{ ".kt","Quellcode"  },
        { ".cpp","Quellcode" },{ ".c","Quellcode"    },{ ".h","Quellcode"   },
        { ".hpp","Quellcode" },{ ".rs","Quellcode"   },{ ".go","Quellcode"  },
        { ".php","Quellcode" },{ ".rb","Quellcode"   },{ ".swift","Quellcode"},
        { ".html","Quellcode"},{ ".htm","Quellcode"  },{ ".css","Quellcode" },
        { ".scss","Quellcode"},{ ".xml","Quellcode"  },{ ".json","Quellcode"},
        { ".yaml","Quellcode"},{ ".yml","Quellcode"  },{ ".sql","Quellcode" },
        { ".lua","Quellcode" },

        { ".sys","Datenbank" },{ ".ini","Datenbank" },{ ".cfg","Datenbank" },
        { ".log","Datenbank" },{ ".db","Datenbank"  },{ ".sqlite","Datenbank"},
        { ".mdb","Datenbank" },{ ".pdb","Datenbank" },

        { ".ttf","Schriften" },{ ".otf","Schriften" },{ ".woff","Schriften" },
        { ".woff2","Schriften"},

        // ── Freier Speicher (synthetisch) ────────────────────────────────────
        { ".__free__", "Sonstiges" },
    };

    /// <summary>Gibt die Kategorie einer Dateierweiterung zurück, z.B. "Bilder", "Videos", …</summary>
    public static string GetCategory(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return "Sonstiges";
        return _categoryMap.TryGetValue(extension, out var cat) ? cat : "Sonstiges";
    }
}
