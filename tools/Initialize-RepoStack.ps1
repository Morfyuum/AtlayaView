[CmdletBinding()]
param(
    [ValidateSet("python", "rust", "hybrid")]
    [string]$Stack,

    [string]$TargetPath = (Get-Location).Path,

    [string]$ProjectName,

    [switch]$WorkspaceOnly,

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Ensure-Directory {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Get-ProjectSlug {
    param([string]$Name)

    $slug = $Name.ToLowerInvariant() -replace "[^a-z0-9]+", "_"
    $slug = $slug.Trim("_")
    if ([string]::IsNullOrWhiteSpace($slug)) {
        return "app"
    }
    return $slug
}

function ConvertTo-Hashtable {
    param($InputObject)

    if ($null -eq $InputObject) {
        return @{}
    }

    if ($InputObject -is [System.Collections.IDictionary]) {
        $result = @{}
        foreach ($key in $InputObject.Keys) {
            $result[$key] = ConvertTo-Hashtable -InputObject $InputObject[$key]
        }
        return $result
    }

    if ($InputObject -is [System.Collections.IEnumerable] -and $InputObject -isnot [string]) {
        $items = @()
        foreach ($item in $InputObject) {
            $items += ,(ConvertTo-Hashtable -InputObject $item)
        }
        return $items
    }

    return $InputObject
}

function Merge-Hashtable {
    param(
        [hashtable]$Base,
        [hashtable]$Overlay
    )

    foreach ($key in $Overlay.Keys) {
        $overlayValue = $Overlay[$key]

        if (-not $Base.ContainsKey($key)) {
            $Base[$key] = $overlayValue
            continue
        }

        $baseValue = $Base[$key]

        if ($baseValue -is [hashtable] -and $overlayValue -is [hashtable]) {
            Merge-Hashtable -Base $baseValue -Overlay $overlayValue
            continue
        }

        if ($baseValue -is [System.Collections.IEnumerable] -and $baseValue -isnot [string] -and
            $overlayValue -is [System.Collections.IEnumerable] -and $overlayValue -isnot [string]) {
            $merged = @()
            foreach ($item in $baseValue) {
                $merged += ,$item
            }
            foreach ($item in $overlayValue) {
                if ($item -is [string]) {
                    if ($merged -notcontains $item) {
                        $merged += ,$item
                    }
                    continue
                }

                $json = $item | ConvertTo-Json -Depth 20 -Compress
                $existing = $merged | ForEach-Object { $_ | ConvertTo-Json -Depth 20 -Compress }
                if ($existing -notcontains $json) {
                    $merged += ,$item
                }
            }
            $Base[$key] = $merged
            continue
        }

        $Base[$key] = $overlayValue
    }
}

function Read-JsonFile {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return @{}
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        return @{}
    }

    $parsed = $content | ConvertFrom-Json -Depth 100
    return ConvertTo-Hashtable -InputObject $parsed
}

function Write-JsonFile {
    param(
        [string]$Path,
        [hashtable]$Value
    )

    $json = $Value | ConvertTo-Json -Depth 100
    Set-Content -LiteralPath $Path -Value ($json + [Environment]::NewLine) -Encoding utf8
}

function Upsert-JsonFile {
    param(
        [string]$Path,
        [hashtable]$Overlay
    )

    $current = Read-JsonFile -Path $Path
    Merge-Hashtable -Base $current -Overlay $Overlay
    Write-JsonFile -Path $Path -Value $current
}

function Write-FileIfMissing {
    param(
        [string]$Path,
        [string]$Content
    )

    if ((Test-Path -LiteralPath $Path) -and -not $Force) {
        return $false
    }

    $parent = Split-Path -Path $Path -Parent
    if ($parent) {
        Ensure-Directory -Path $parent
    }

    Set-Content -LiteralPath $Path -Value $Content -Encoding utf8
    return $true
}

function Add-UniqueLines {
    param(
        [string]$Path,
        [string[]]$Lines
    )

    $currentLines = @()
    if (Test-Path -LiteralPath $Path) {
        $currentLines = Get-Content -LiteralPath $Path
    }

    $updated = @($currentLines)
    foreach ($line in $Lines) {
        if ($updated -notcontains $line) {
            $updated += $line
        }
    }

    Set-Content -LiteralPath $Path -Value ($updated -join [Environment]::NewLine) -Encoding utf8
}

function New-Task {
    param(
        [string]$Label,
        [string]$Command,
        [string[]]$Args,
        [bool]$IsBackground = $false,
        [string[]]$ProblemMatcher = @()
    )

    return @{
        label = $Label
        type = "shell"
        command = $Command
        args = $Args
        isBackground = $IsBackground
        problemMatcher = $ProblemMatcher
    }
}

function New-LaunchConfiguration {
    param(
        [string]$Name,
        [hashtable]$Value
    )

    $copy = @{}
    foreach ($key in $Value.Keys) {
        $copy[$key] = $Value[$key]
    }
    $copy["name"] = $Name
    return $copy
}

function Add-OrUpdateByName {
    param(
        [object[]]$Existing,
        [object[]]$Incoming,
        [string]$KeyName
    )

    $result = @()
    if ($Existing) {
        $result += $Existing
    }

    foreach ($item in $Incoming) {
        $matchIndex = -1
        for ($i = 0; $i -lt $result.Count; $i++) {
            if ($result[$i].ContainsKey($KeyName) -and $item.ContainsKey($KeyName) -and $result[$i][$KeyName] -eq $item[$KeyName]) {
                $matchIndex = $i
                break
            }
        }

        if ($matchIndex -ge 0) {
            $result[$matchIndex] = $item
        }
        else {
            $result += $item
        }
    }

    return $result
}

function Set-TasksFile {
    param(
        [string]$Path,
        [object[]]$Tasks
    )

    $current = Read-JsonFile -Path $Path
    if (-not $current.ContainsKey("version")) {
        $current["version"] = "2.0.0"
    }

    $existingTasks = @()
    if ($current.ContainsKey("tasks")) {
        $existingTasks = @($current["tasks"])
    }

    $current["tasks"] = Add-OrUpdateByName -Existing $existingTasks -Incoming $Tasks -KeyName "label"
    Write-JsonFile -Path $Path -Value $current
}

function Set-LaunchFile {
    param(
        [string]$Path,
        [object[]]$Configurations
    )

    $current = Read-JsonFile -Path $Path
    if (-not $current.ContainsKey("version")) {
        $current["version"] = "0.2.0"
    }

    $existingConfigurations = @()
    if ($current.ContainsKey("configurations")) {
        $existingConfigurations = @($current["configurations"])
    }

    $current["configurations"] = Add-OrUpdateByName -Existing $existingConfigurations -Incoming $Configurations -KeyName "name"
    Write-JsonFile -Path $Path -Value $current
}

if ([string]::IsNullOrWhiteSpace($ProjectName)) {
    if (Test-Path -LiteralPath $TargetPath) {
        $ProjectName = Split-Path -Path (Resolve-Path -LiteralPath $TargetPath) -Leaf
    }
    else {
        $ProjectName = Split-Path -Path $TargetPath -Leaf
    }
}

$projectSlug = Get-ProjectSlug -Name $ProjectName

Ensure-Directory -Path $TargetPath
$vscodePath = Join-Path $TargetPath ".vscode"
Ensure-Directory -Path $vscodePath

$commonSettings = @{
    "terminal.integrated.env.windows" = @{
        "PATH" = '${env:PATH};${env:USERPROFILE}\\.cargo\\bin'
    }
}

$pythonSettings = @{
    "[python]" = @{
        "editor.defaultFormatter" = "charliermarsh.ruff"
        "editor.formatOnSave" = $true
        "editor.codeActionsOnSave" = @{
            "source.fixAll" = "explicit"
            "source.organizeImports" = "explicit"
        }
    }
    "python.analysis.autoImportCompletions" = $true
}

$rustSettings = @{
    "rust-analyzer.check.command" = "clippy"
    "rust-analyzer.cargo.features" = "all"
    "rust-analyzer.inlayHints.bindingModeHints.enable" = $true
    "rust-analyzer.inlayHints.closingBraceHints.minLines" = 10
    "rust-analyzer.inlayHints.closureReturnTypeHints.enable" = "with_block"
    "rust-analyzer.inlayHints.discriminantHints.enable" = "fieldless"
    "rust-analyzer.inlayHints.expressionAdjustmentHints.enable" = "reborrow"
    "rust-analyzer.inlayHints.lifetimeElisionHints.enable" = "skip_trivial"
    "rust-analyzer.inlayHints.parameterHints.enable" = $true
    "[rust]" = @{
        "editor.defaultFormatter" = "rust-lang.rust-analyzer"
        "editor.formatOnSave" = $true
        "editor.codeActionsOnSave" = @{
            "source.fixAll" = "explicit"
        }
    }
    "[toml]" = @{
        "editor.defaultFormatter" = "tamasfe.even-better-toml"
        "editor.formatOnSave" = $true
    }
}

$recommendations = @()
$tasks = @()
$launchConfigurations = @()

switch ($Stack) {
    "python" {
        Merge-Hashtable -Base $commonSettings -Overlay $pythonSettings
        $recommendations = @(
            "ms-python.python",
            "ms-python.vscode-pylance",
            "charliermarsh.ruff"
        )
        $tasks = @(
            (New-Task -Label "Python: Run app" -Command "pwsh" -Args @("-NoLogo", "-Command", "$env:PYTHONPATH='src'; uv run python -m $projectSlug.main")),
            (New-Task -Label "Python: Ruff check" -Command "uv" -Args @("run", "ruff", "check", "."))
        )
        $launchConfigurations = @(
            (New-LaunchConfiguration -Name "Python: Launch app" -Value @{
                type = "debugpy"
                request = "launch"
                module = "$projectSlug.main"
                console = "integratedTerminal"
                cwd = '${workspaceFolder}'
                env = @{
                    PYTHONPATH = '${workspaceFolder}\src'
                }
            })
        )

        if (-not $WorkspaceOnly) {
            $pyproject = @"
[project]
name = "$projectSlug"
version = "0.1.0"
description = ""
readme = "README.md"
requires-python = ">=3.12"
dependencies = []

[build-system]
requires = ["hatchling"]
build-backend = "hatchling.build"

[tool.ruff]
line-length = 100

[tool.ruff.lint]
select = ["E", "F", "I", "UP", "B"]
"@
            Write-FileIfMissing -Path (Join-Path $TargetPath "pyproject.toml") -Content $pyproject | Out-Null
            Write-FileIfMissing -Path (Join-Path $TargetPath "README.md") -Content "# $ProjectName`r`n" | Out-Null
            Write-FileIfMissing -Path (Join-Path $TargetPath "src\$projectSlug\__init__.py") -Content "" | Out-Null
            $pythonMain = @"
def main() -> None:
    print("Hello from $projectSlug")


if __name__ == "__main__":
    main()
"@
            Write-FileIfMissing -Path (Join-Path $TargetPath "src\$projectSlug\main.py") -Content $pythonMain | Out-Null
        }

        Add-UniqueLines -Path (Join-Path $TargetPath ".gitignore") -Lines @(".venv/", "__pycache__/", ".ruff_cache/")
    }
    "rust" {
        Merge-Hashtable -Base $commonSettings -Overlay $rustSettings
        $recommendations = @(
            "rust-lang.rust-analyzer",
            "vadimcn.vscode-lldb",
            "tamasfe.even-better-toml"
        )
        $tasks = @(
            (New-Task -Label "Rust: Build" -Command "cargo" -Args @("build") -ProblemMatcher @('$rustc')),
            (New-Task -Label "Rust: Run" -Command "cargo" -Args @("run")),
            (New-Task -Label "Rust: Clippy" -Command "cargo" -Args @("clippy", "--all-targets", "--all-features"))
        )
        $launchConfigurations = @(
            (New-LaunchConfiguration -Name "Rust: Debug current crate" -Value @{
                type = "lldb"
                request = "launch"
                cargo = @{
                    args = @("build")
                    filter = @{
                        kind = "bin"
                        name = $projectSlug
                    }
                }
                args = @()
                cwd = '${workspaceFolder}'
            })
        )

        if (-not $WorkspaceOnly) {
            $cargoToml = @"
[package]
name = "$projectSlug"
version = "0.1.0"
edition = "2024"

[dependencies]
"@
            Write-FileIfMissing -Path (Join-Path $TargetPath "Cargo.toml") -Content $cargoToml | Out-Null
            Write-FileIfMissing -Path (Join-Path $TargetPath "README.md") -Content "# $ProjectName`r`n" | Out-Null
            $rustMain = @"
fn main() {
    println!("Hello from $projectSlug");
}
"@
            Write-FileIfMissing -Path (Join-Path $TargetPath "src\main.rs") -Content $rustMain | Out-Null
        }

    Add-UniqueLines -Path (Join-Path $TargetPath ".gitignore") -Lines @("/target/")
    }
    "hybrid" {
        Merge-Hashtable -Base $commonSettings -Overlay $pythonSettings
        Merge-Hashtable -Base $commonSettings -Overlay $rustSettings
        $recommendations = @(
            "ms-python.python",
            "ms-python.vscode-pylance",
            "charliermarsh.ruff",
            "rust-lang.rust-analyzer",
            "vadimcn.vscode-lldb",
            "tamasfe.even-better-toml"
        )
        $tasks = @(
            (New-Task -Label "Python: Run app" -Command "pwsh" -Args @("-NoLogo", "-Command", "$env:PYTHONPATH='src'; uv run python -m $projectSlug.main") -ProblemMatcher @()),
            (New-Task -Label "Rust: Build core" -Command "cargo" -Args @("build", "--manifest-path", "rust/Cargo.toml")),
            (New-Task -Label "Rust: Clippy core" -Command "cargo" -Args @("clippy", "--manifest-path", "rust/Cargo.toml", "--all-targets", "--all-features")),
            (New-Task -Label "Hybrid: Validate" -Command "pwsh" -Args @("-NoLogo", "-Command", "$env:PYTHONPATH='src'; Push-Location python; uv run python -m $projectSlug.main; Pop-Location; cargo check --manifest-path rust/Cargo.toml"))
        )
        $launchConfigurations = @(
            (New-LaunchConfiguration -Name "Python: Launch app" -Value @{
                type = "debugpy"
                request = "launch"
                module = "$projectSlug.main"
                console = "integratedTerminal"
                cwd = '${workspaceFolder}\python'
                env = @{
                    PYTHONPATH = '${workspaceFolder}\python\src'
                }
            }),
            (New-LaunchConfiguration -Name "Rust: Debug core" -Value @{
                type = "lldb"
                request = "launch"
                cargo = @{
                    args = @("build", "--manifest-path", "rust/Cargo.toml")
                    filter = @{
                        kind = "bin"
                        name = $projectSlug
                    }
                }
                args = @()
                cwd = '${workspaceFolder}\rust'
            })
        )

        if (-not $WorkspaceOnly) {
            $hybridReadme = @"
# $ProjectName

- Python app in `python/`
- Rust core in `rust/`
"@
            Write-FileIfMissing -Path (Join-Path $TargetPath "README.md") -Content $hybridReadme | Out-Null

            $hybridPyproject = @"
[project]
name = "$projectSlug"
version = "0.1.0"
description = ""
readme = "README.md"
requires-python = ">=3.12"
dependencies = []

[build-system]
requires = ["hatchling"]
build-backend = "hatchling.build"

[tool.ruff]
line-length = 100

[tool.ruff.lint]
select = ["E", "F", "I", "UP", "B"]
"@
            Write-FileIfMissing -Path (Join-Path $TargetPath "python\pyproject.toml") -Content $hybridPyproject | Out-Null
            Write-FileIfMissing -Path (Join-Path $TargetPath "python\src\$projectSlug\__init__.py") -Content "" | Out-Null
            $hybridPythonMain = @"
def main() -> None:
    print("Python app for $projectSlug")


if __name__ == "__main__":
    main()
"@
            Write-FileIfMissing -Path (Join-Path $TargetPath "python\src\$projectSlug\main.py") -Content $hybridPythonMain | Out-Null

            $hybridCargoToml = @"
[package]
name = "$projectSlug"
version = "0.1.0"
edition = "2024"

[dependencies]
"@
            Write-FileIfMissing -Path (Join-Path $TargetPath "rust\Cargo.toml") -Content $hybridCargoToml | Out-Null
            $hybridRustMain = @"
fn main() {
    println!("Rust core for $projectSlug");
}
"@
            Write-FileIfMissing -Path (Join-Path $TargetPath "rust\src\main.rs") -Content $hybridRustMain | Out-Null
        }

        Add-UniqueLines -Path (Join-Path $TargetPath ".gitignore") -Lines @(".venv/", "__pycache__/", ".ruff_cache/", "/rust/target/", "/target/")
    }
}

Upsert-JsonFile -Path (Join-Path $vscodePath "settings.json") -Overlay $commonSettings
Upsert-JsonFile -Path (Join-Path $vscodePath "extensions.json") -Overlay @{
    recommendations = $recommendations
}
Set-TasksFile -Path (Join-Path $vscodePath "tasks.json") -Tasks $tasks
Set-LaunchFile -Path (Join-Path $vscodePath "launch.json") -Configurations $launchConfigurations

Write-Host "Initialized $Stack repo profile at $TargetPath"
if ($WorkspaceOnly) {
    Write-Host "WorkspaceOnly enabled: project manifests were left untouched."
}