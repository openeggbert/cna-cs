[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $XnaReferencePath,

    [string] $XnaRuntimePath,

    [string] $OutputDirectory = "artifacts/xna-snapshots",

    [string] $CompareFile
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$referencePath = (Resolve-Path $XnaReferencePath).Path
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Executable,

        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]] $Arguments
    )

    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "'$Executable' exited with code $LASTEXITCODE."
    }
}

function Write-NormalizedSnapshot {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Executable,

        [Parameter(Mandatory = $true)]
        [string] $Destination,

        [Parameter(Mandatory = $true)]
        [int] $ExpectedCount
    )

    $rawLines = @(& $Executable)
    if ($LASTEXITCODE -ne 0) {
        throw "'$Executable' exited with code $LASTEXITCODE."
    }

    $lines = @($rawLines | Where-Object {
        $_ -match '^[a-z][a-z0-9_.]*=.*$'
    })
    if ($lines.Count -ne $ExpectedCount) {
        throw "'$Executable' emitted $($lines.Count) normalized observations; expected $ExpectedCount."
    }

    [System.IO.File]::WriteAllText(
        $Destination,
        (($lines -join "`n") + "`n"),
        $utf8WithoutBom)
}

Push-Location $repositoryRoot
try {
    $commonBuildArguments = @(
        "build",
        "-c", "Release",
        "-m:1",
        "-p:CompatibilityTarget=XNA",
        "-p:XnaWindowsSnapshot=true",
        "-p:XnaReferencePath=$referencePath"
    )

    Invoke-Checked dotnet @commonBuildArguments "tests/CNA.XnaCompat.CompileProbe/CNA.XnaCompat.CompileProbe.csproj"
    Invoke-Checked dotnet @commonBuildArguments "tests/CNA.XnaCompat.GraphicsProbe/CNA.XnaCompat.GraphicsProbe.csproj"

    if ($XnaRuntimePath) {
        $env:XNA_RUNTIME_PATH = (Resolve-Path $XnaRuntimePath).Path
    } else {
        Remove-Item Env:XNA_RUNTIME_PATH -ErrorAction SilentlyContinue
    }
    $env:XNA_GRAPHICS_PROBE_DRAW_VALIDATION = "1"
    $env:XNA_GRAPHICS_PROBE_DESTRUCTIVE_LIFECYCLE = "1"
    $env:XNA_GRAPHICS_PROBE_UNSAFE_CONSTRUCTORS = "1"

    [System.IO.Directory]::CreateDirectory($outputPath) | Out-Null
    $purePath = Join-Path $outputPath "xna-math-input-content.txt"
    $graphicsPath = Join-Path $outputPath "xna-graphics-resource.txt"
    $combinedPath = Join-Path $outputPath "xna-all.txt"

    Write-NormalizedSnapshot `
        (Join-Path $repositoryRoot "tests/CNA.XnaCompat.CompileProbe/bin/Release/net48/CNA.XnaCompat.CompileProbe.exe") `
        $purePath `
        133
    Write-NormalizedSnapshot `
        (Join-Path $repositoryRoot "tests/CNA.XnaCompat.GraphicsProbe/bin/Release/net48/CNA.XnaCompat.GraphicsProbe.exe") `
        $graphicsPath `
        166

    $combined = [System.IO.File]::ReadAllText($purePath) +
        [System.IO.File]::ReadAllText($graphicsPath)
    [System.IO.File]::WriteAllText($combinedPath, $combined, $utf8WithoutBom)

    Write-Host "Captured 299 normalized XNA observations in '$outputPath'."
    if ($CompareFile) {
        Invoke-Checked git diff --no-index -- $CompareFile $combinedPath
    }
} finally {
    Pop-Location
}
