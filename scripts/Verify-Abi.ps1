[CmdletBinding()]
param(
    [string] $CnaUpstreamRoot = (Join-Path $PSScriptRoot "..\..\..\cna"),
    [string] $OutputPath = (Join-Path $PSScriptRoot "..\artifacts\abi\windows.json"),
    [string] $DotnetCommand = "dotnet",
    [string] $Compiler
)

$ErrorActionPreference = "Stop"
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$includeDirectory = [IO.Path]::GetFullPath((Join-Path $CnaUpstreamRoot "modules\c-api\include"))
$arguments = @(
    "run", "--project", (Join-Path $repoRoot "tools\abi-verify\CNA.AbiVerify.csproj"), "--",
    "--include", $includeDirectory,
    "--output", ([IO.Path]::GetFullPath($OutputPath))
)
if ($Compiler) {
    $arguments += @("--compiler", $Compiler)
}

& $DotnetCommand @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Portable ABI verification failed with exit code $LASTEXITCODE."
}
