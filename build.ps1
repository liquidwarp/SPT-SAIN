param(
    [string]$SPTPath = 'E:\Games\SPT 4.1',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$InstallMod,

    [switch]$InstallClientMod,

    [switch]$InstallServerMod,

    [switch]$PackageRelease
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path "$SPTPath\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll")) {
    throw "SPT not found at '$SPTPath'"
}

$buildArgs = @(
    "$PSScriptRoot\SAIN.slnx"
    "-c", $Configuration
    "-p:SPTPath=$SPTPath"
    "-p:InstallClientMod=$($InstallMod -or $InstallClientMod)"
    "-p:InstallServerMod=$($InstallMod -or $InstallServerMod)"
    "-p:PackageRelease=$($PackageRelease.IsPresent)"
    "-tlp:verbosity=detailed"
)
dotnet build @buildArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE)" }
