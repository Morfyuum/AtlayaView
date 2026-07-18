# Repo Stack Template

Das Skript [tools/Initialize-RepoStack.ps1](../tools/Initialize-RepoStack.ps1) richtet neue Repos oder bestehende Workspaces fuer Python, Rust oder Hybrid ein.

## Profile

- `python`: `pyproject.toml`, `src/<paket>/main.py`, VS-Code-Settings, Tasks und Python-Debugprofil.
- `rust`: `Cargo.toml`, `src/main.rs`, VS-Code-Settings, Tasks und CodeLLDB-Debugprofil.
- `hybrid`: `python/` plus `rust/` in einem Repo, gemeinsame VS-Code-Konfiguration und getrennte Debugprofile.

## Beispiele

Neues Python-Repo anlegen:

```powershell
pwsh -File .\tools\Initialize-RepoStack.ps1 -Stack python -TargetPath C:\Projects\MyPythonApp -ProjectName MyPythonApp
```

Neues Rust-Repo anlegen:

```powershell
pwsh -File .\tools\Initialize-RepoStack.ps1 -Stack rust -TargetPath C:\Projects\MyRustTool -ProjectName MyRustTool
```

Bestehenden Workspace nur mit Editor- und Debugprofilen ergaenzen:

```powershell
pwsh -File .\tools\Initialize-RepoStack.ps1 -Stack hybrid -TargetPath C:\Projects\ExistingRepo -WorkspaceOnly
```

Bestehenden Workspace erweitern und fehlende Basisdateien anlegen:

```powershell
pwsh -File .\tools\Initialize-RepoStack.ps1 -Stack hybrid -TargetPath C:\Projects\ExistingRepo -ProjectName ExistingRepo
```

Vorhandene Startdateien bewusst ueberschreiben:

```powershell
pwsh -File .\tools\Initialize-RepoStack.ps1 -Stack rust -TargetPath C:\Projects\ExistingRepo -Force
```

## Verhalten in bestehenden Repos

- Vorhandene `.vscode/settings.json` und `.vscode/extensions.json` werden gemergt.
- `tasks.json` und `launch.json` werden nach Label bzw. Name aktualisiert oder ergaenzt.
- Projektdateien wie `Cargo.toml`, `pyproject.toml` oder `src/main.rs` werden nur erstellt, wenn sie fehlen, ausser `-Force` ist gesetzt.
- Mit `-WorkspaceOnly` werden nur die VS-Code-Dateien angepasst.

## Empfehlung

- `python`, wenn du schnell entwickeln, skripten, Daten verarbeiten oder KI-Workflows bauen willst.
- `rust`, wenn Performance, Verteilung als Binary oder robuste Systemwerkzeuge wichtig sind.
- `hybrid`, wenn Python die Orchestrierung macht und Rust den schnellen oder sicherheitskritischen Kern uebernimmt.