[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $XnaReferencePath,

    [string] $XnaRuntimePath,

    [string] $ExpectedAssemblyHashesPath,

    [string] $CnaSnapshotPath = "artifacts/cna-snapshots/cna-all.txt",

    [string] $OutputDirectory = "artifacts/xna-snapshots",

    [switch] $AllowDirtySource,

    [switch] $Force
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
$xnaAssemblyNames = @(
    "Microsoft.Xna.Framework.dll",
    "Microsoft.Xna.Framework.Game.dll",
    "Microsoft.Xna.Framework.Graphics.dll",
    "Microsoft.Xna.Framework.Storage.dll",
    "Microsoft.Xna.Framework.Video.dll",
    "Microsoft.Xna.Framework.Input.Touch.dll",
    "Microsoft.Xna.Framework.Xact.dll"
)

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Executable,

        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "'$Executable' exited with code $LASTEXITCODE."
    }
}

function Get-FullConfiguredPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string] $Description
    )

    $candidate = if ([System.IO.Path]::IsPathRooted($Path)) {
        $Path
    } else {
        Join-Path $repositoryRoot $Path
    }

    if (-not (Test-Path -LiteralPath $candidate)) {
        throw "$Description '$candidate' does not exist."
    }

    return (Resolve-Path -LiteralPath $candidate).Path
}

function Get-PublicKeyToken {
    param([Parameter(Mandatory = $true)][System.Reflection.AssemblyName] $AssemblyName)

    return (($AssemblyName.GetPublicKeyToken() | ForEach-Object { $_.ToString("x2") }) -join "")
}

function Get-XnaAssemblyRecord {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Kind
    )

    $identity = [System.Reflection.AssemblyName]::GetAssemblyName($Path)
    $expectedName = [System.IO.Path]::GetFileNameWithoutExtension($Path)
    $token = Get-PublicKeyToken $identity
    if ($identity.Name -ne $expectedName -or
        $identity.Version -ne [Version]"4.0.0.0" -or
        $token -ne "842cf8be1de50553") {
        throw "'$Path' is not the expected XNA 4.0 assembly identity: $($identity.FullName)."
    }

    return [ordered]@{
        kind = $Kind
        fileName = [System.IO.Path]::GetFileName($Path)
        path = $Path
        assemblyIdentity = $identity.FullName
        sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

function Get-ConfiguredHash {
    param(
        [Parameter(Mandatory = $true)] $HashDocument,
        [Parameter(Mandatory = $true)][string] $FileName
    )

    $container = if ($null -ne $HashDocument.assemblies) {
        $HashDocument.assemblies
    } else {
        $HashDocument
    }
    $property = $container.PSObject.Properties[$FileName]
    return if ($null -eq $property) { $null } else { [string]$property.Value }
}

function Write-RawOutput {
    param(
        [Parameter(Mandatory = $true)][string] $Executable,
        [Parameter(Mandatory = $true)][string] $Path
    )

    $lines = @(& $Executable)
    if ($LASTEXITCODE -ne 0) {
        throw "'$Executable' exited with code $LASTEXITCODE."
    }

    [System.IO.File]::WriteAllText($Path, (($lines -join "`n") + "`n"), $utf8WithoutBom)
}

if ([System.Environment]::OSVersion.Platform -ne [System.PlatformID]::Win32NT) {
    throw "Microsoft XNA 4.0 snapshot capture must run on Windows; current platform is '$([System.Environment]::OSVersion.Platform)'."
}

$powerShellEdition = if ($PSVersionTable.PSEdition) { $PSVersionTable.PSEdition } else { "Desktop" }
if ($PSVersionTable.PSVersion -lt [Version]"5.1") {
    throw "PowerShell 5.1 or newer is required; found $($PSVersionTable.PSVersion)."
}
if ($powerShellEdition -ne "Desktop") {
    throw "Run this workflow with Windows PowerShell (Desktop edition), not PowerShell Core. XNA 4.0 is a .NET Framework/x86 runtime."
}

Invoke-Checked dotnet @("--version")
$installedSdks = @(& dotnet --list-sdks)
if ($LASTEXITCODE -ne 0 -or $installedSdks.Count -eq 0) {
    throw "A .NET SDK is required to build the net48/x86 probes and corpus utility."
}

$frameworkRelease = [int](Get-ItemPropertyValue `
    -LiteralPath "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" `
    -Name Release)
if ($frameworkRelease -lt 528040) {
    throw ".NET Framework 4.8 or newer is required; registry release is $frameworkRelease."
}
$net48References = Join-Path `
    ([Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)) `
    "Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
if (-not (Test-Path -LiteralPath (Join-Path $net48References "mscorlib.dll"))) {
    throw ".NET Framework 4.8 Developer Pack reference assemblies were not found at '$net48References'."
}

$referencePath = Get-FullConfiguredPath $XnaReferencePath "XNA reference directory"
$referenceRecords = @()
foreach ($assemblyName in $xnaAssemblyNames) {
    $assemblyPath = Join-Path $referencePath $assemblyName
    if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
        throw "XNA reference assembly '$assemblyPath' is missing."
    }
    $referenceRecords += Get-XnaAssemblyRecord $assemblyPath "reference"
}

if ($ExpectedAssemblyHashesPath) {
    $expectedHashPath = Get-FullConfiguredPath $ExpectedAssemblyHashesPath "Expected XNA hash manifest"
    $hashDocument = Get-Content -LiteralPath $expectedHashPath -Raw | ConvertFrom-Json
    foreach ($record in $referenceRecords) {
        $expectedHash = Get-ConfiguredHash $hashDocument $record.fileName
        if ([string]::IsNullOrWhiteSpace($expectedHash)) {
            throw "Expected XNA hash manifest '$expectedHashPath' has no '$($record.fileName)' entry."
        }
        if ($record.sha256 -ne $expectedHash.ToLowerInvariant()) {
            throw "XNA hash mismatch for '$($record.fileName)': expected $expectedHash, actual $($record.sha256)."
        }
    }
}

$runtimeRecords = @()
if ($XnaRuntimePath) {
    $runtimeDirectory = Get-FullConfiguredPath $XnaRuntimePath "XNA runtime directory"
    foreach ($assemblyName in $xnaAssemblyNames) {
        $assemblyPath = Join-Path $runtimeDirectory $assemblyName
        if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
            throw "XNA runtime assembly '$assemblyPath' is missing."
        }
        $runtimeRecords += Get-XnaAssemblyRecord $assemblyPath "runtime"
    }
    $env:XNA_RUNTIME_PATH = $runtimeDirectory
} else {
    Remove-Item Env:XNA_RUNTIME_PATH -ErrorAction SilentlyContinue
    foreach ($assemblyName in $xnaAssemblyNames) {
        $simpleName = [System.IO.Path]::GetFileNameWithoutExtension($assemblyName)
        $identity = "$simpleName, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"
        try {
            $assembly = [System.Reflection.Assembly]::ReflectionOnlyLoad($identity)
        } catch {
            throw "XNA 4.0 runtime assembly '$identity' was not found in the .NET Framework GAC. Install the official XNA Framework Redistributable 4.0 or pass -XnaRuntimePath."
        }
        if ([string]::IsNullOrWhiteSpace($assembly.Location)) {
            throw "XNA runtime assembly '$identity' loaded without a hashable physical location. Pass -XnaRuntimePath."
        }
        $runtimeRecords += Get-XnaAssemblyRecord $assembly.Location "runtime"
    }
}

$sourceStatus = @(& git -C $repositoryRoot status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0) {
    throw "Could not inspect the probe source revision."
}
if ($sourceStatus.Count -ne 0 -and -not $AllowDirtySource) {
    throw "The repository has uncommitted or untracked source. Commit it or pass -AllowDirtySource so the manifest records a dirty capture explicitly."
}
$sourceRevision = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw "Could not resolve the probe source revision."
}

$outputPath = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    [System.IO.Path]::GetFullPath($OutputDirectory)
} else {
    [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
}
$repositoryFullPath = [System.IO.Path]::GetFullPath($repositoryRoot)
if ($outputPath -eq $repositoryFullPath -or
    $outputPath -eq [System.IO.Path]::GetPathRoot($outputPath)) {
    throw "Refusing unsafe output directory '$outputPath'."
}
if ((Test-Path -LiteralPath $outputPath) -and -not $Force) {
    throw "Output directory '$outputPath' already exists. Pass -Force to replace it."
}

$cnaSnapshot = Get-FullConfiguredPath $CnaSnapshotPath "Normalized CNA snapshot"
$corpusPath = Join-Path $repositoryRoot "tests\behavior-corpus-counts.json"
$corpus = Get-Content -LiteralPath $corpusPath -Raw | ConvertFrom-Json
$behaviorTool = Join-Path $repositoryRoot "tools\behavior-corpus\CNA.BehaviorCorpus.csproj"
$stagingPath = Join-Path ([System.IO.Path]::GetDirectoryName($outputPath)) `
    ("." + [System.IO.Path]::GetFileName($outputPath) + ".staging-" + [Guid]::NewGuid().ToString("N"))

[System.IO.Directory]::CreateDirectory($stagingPath) | Out-Null
Push-Location $repositoryRoot
try {
    Invoke-Checked dotnet @("build", $behaviorTool, "-c", "Release", "-m:1")
    foreach ($probe in $corpus.probes) {
        $buildArguments = @(
            "build", [string]$probe.sourceProject,
            "-c", "Release",
            "-m:1",
            "-p:CompatibilityTarget=XNA",
            "-p:XnaWindowsSnapshot=true",
            "-p:XnaReferencePath=$referencePath"
        )
        Invoke-Checked dotnet $buildArguments
    }

    $env:XNA_GRAPHICS_PROBE_DRAW_VALIDATION = "1"
    $env:XNA_GRAPHICS_PROBE_DESTRUCTIVE_LIFECYCLE = "1"
    $env:XNA_GRAPHICS_PROBE_UNSAFE_CONSTRUCTORS = "1"

    $inputArguments = @()
    foreach ($probe in $corpus.probes) {
        $projectDirectory = [System.IO.Path]::GetDirectoryName([string]$probe.sourceProject)
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension([string]$probe.sourceProject)
        $executable = Join-Path $repositoryRoot `
            (Join-Path $projectDirectory ("bin\Release\net48\" + $projectName + ".exe"))
        if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
            throw "Expected x86/net48 probe executable '$executable' was not produced."
        }

        $rawPath = Join-Path $stagingPath ($probe.id + ".raw.txt")
        $snapshotPath = Join-Path $stagingPath ([string]$probe.expectedSnapshotFilename)
        Write-RawOutput $executable $rawPath
        Invoke-Checked dotnet @(
            "run", "--project", $behaviorTool, "-c", "Release", "--no-build", "--",
            "validate", "--probe", [string]$probe.id,
            "--input", $rawPath,
            "--output", $snapshotPath
        )
        Remove-Item -LiteralPath $rawPath
        $inputArguments += @("--input", ($probe.id + "=" + $snapshotPath))
    }

    $combinedPath = Join-Path $stagingPath ([string]$corpus.combinedSnapshotFilename)
    Invoke-Checked dotnet (@(
        "run", "--project", $behaviorTool, "-c", "Release", "--no-build", "--", "combine"
    ) + $inputArguments + @("--output", $combinedPath))
} catch {
    if (Test-Path -LiteralPath $stagingPath) {
        Remove-Item -LiteralPath $stagingPath -Recurse -Force
    }
    throw
} finally {
    Pop-Location
}

if (Test-Path -LiteralPath $outputPath) {
    Remove-Item -LiteralPath $outputPath -Recurse -Force
}
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($outputPath)) | Out-Null
Move-Item -LiteralPath $stagingPath -Destination $outputPath

$combinedPath = Join-Path $outputPath ([string]$corpus.combinedSnapshotFilename)
$differencePath = Join-Path $outputPath "differences.json"
& dotnet run --project $behaviorTool -c Release --no-build -- `
    compare --reference $combinedPath --candidate $cnaSnapshot --output $differencePath
$comparisonExitCode = $LASTEXITCODE
if ($comparisonExitCode -gt 1) {
    throw "Behavior comparison failed to run; exit code $comparisonExitCode."
}
$differenceReport = Get-Content -LiteralPath $differencePath -Raw | ConvertFrom-Json

$categoryCounts = @()
foreach ($category in $corpus.categories) {
    $aggregateName = if ($category.aggregateAs) { [string]$category.aggregateAs } else { [string]$category.id }
    $existing = $categoryCounts | Where-Object { $_.id -eq $aggregateName } | Select-Object -First 1
    if ($null -eq $existing) {
        $existing = [pscustomobject][ordered]@{
            id = $aggregateName
            displayName = [string]$category.displayName
            observationCount = 0
        }
        $categoryCounts += $existing
    }
    $existing.observationCount += [int]$category.expectedObservationCount
}

$os = Get-CimInstance Win32_OperatingSystem
$manifest = [ordered]@{
    schemaVersion = 1
    capturedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
    probeSourceRevision = $sourceRevision
    probeSourceDirty = ($sourceStatus.Count -ne 0)
    observationCount = [int]$corpus.combinedExpectedObservationCount
    categories = $categoryCounts
    probes = @($corpus.probes | ForEach-Object {
        [ordered]@{
            id = [string]$_.id
            sourceProject = [string]$_.sourceProject
            platformTarget = "x86"
            targetFramework = "net48"
            observationCount = [int](($_.categories | ForEach-Object {
                $categoryId = $_
                ($corpus.categories | Where-Object { $_.id -eq $categoryId }).expectedObservationCount
            } | Measure-Object -Sum).Sum)
            snapshotFilename = [string]$_.expectedSnapshotFilename
        }
    })
    xnaAssemblies = @($referenceRecords + $runtimeRecords)
    environment = [ordered]@{
        osCaption = $os.Caption
        osVersion = $os.Version
        osArchitecture = $os.OSArchitecture
        processArchitecture = $env:PROCESSOR_ARCHITECTURE
        powerShellVersion = $PSVersionTable.PSVersion.ToString()
        powerShellEdition = $powerShellEdition
        dotnetSdk = (& dotnet --version).Trim()
        dotNetFrameworkRelease = $frameworkRelease
    }
    comparison = [ordered]@{
        cnaSnapshot = $cnaSnapshot
        reportFilename = [System.IO.Path]::GetFileName($differencePath)
        differenceCount = [int]$differenceReport.differenceCount
        status = if ($comparisonExitCode -eq 0) { "match" } else { "different" }
    }
}
[System.IO.File]::WriteAllText(
    (Join-Path $outputPath "manifest.json"),
    (($manifest | ConvertTo-Json -Depth 10) + "`n"),
    $utf8WithoutBom)

Write-Host "Captured $($corpus.combinedExpectedObservationCount) normalized XNA observations in '$outputPath'."
if ($comparisonExitCode -ne 0) {
    throw "XNA/CNA behavior comparison found $($differenceReport.differenceCount) unexpected differences. See '$differencePath'."
}
