# Contributing

Thanks for your interest in AtlayaView.

## Before you start

- Check existing issues before opening a new one.
- For security-sensitive findings, do not open a public bug report. Follow the guidance in [SECURITY.md](SECURITY.md).
- Keep changes focused. Small, isolated pull requests are easier to review and verify.

## Development setup

Requirements:

- Windows
- .NET 9 SDK
- Rust toolchain with cargo

Build the app:

```powershell
dotnet build .\AtlayaView.csproj -c Release
```

Build the native renderer:

```powershell
cd .\native\atlaya_renderer
cargo build --release
```

Create both release variants:

```powershell
pwsh -File .\build-hybrid.ps1 -Configuration Release
```

## Contribution guidelines

- Preserve the existing Windows-first desktop focus.
- Keep the application local-first. Avoid introducing unnecessary external services or cloud dependencies.
- Match the existing code style and naming.
- Prefer targeted fixes over broad refactors.
- If you add UI text, keep localization requirements in mind.

## Pull requests

- Describe the change and the user-visible effect.
- Include validation details such as build results or manual test notes.
- Mention if the change affects release packaging, updates, or the Rust renderer.
