# AtlayaView WinGet Release Workflow

AtlayaView is submitted to the public WinGet catalog as `Morfyuum.AtlayaView` and uses the self-contained GitHub release ZIP as the canonical installer source.

## Update flow for a new AtlayaView release

1. Publish the normal AtlayaView release first so the GitHub asset already exists.
2. Regenerate the local WinGet manifests from the live GitHub release:

```powershell
pwsh -File .\tools\Update-WingetManifest.ps1 -ReleaseNotes "Version 2.0.16 moves public release delivery to GitHub Releases and keeps website update metadata aligned with the latest published assets." -Validate
```

3. This writes the three manifest files under `winget/Morfyuum.AtlayaView/<version>/`.
4. If `-Validate` is used, the script immediately runs `winget validate` against that directory.
5. Push those three files to the fork branch used for `microsoft/winget-pkgs` and create or update the PR.

## Notes

- The script reads the version directly from `AtlayaView.csproj`.
- It reads the Full ZIP and SHA256 from `gh release view v<version> --json assets`.
- The framework-dependent ZIP is intentionally not used for WinGet; the catalog should point to the self-contained package so end users do not need a preinstalled .NET runtime.
- The only field that usually still needs per-release wording is `-ReleaseNotes`.