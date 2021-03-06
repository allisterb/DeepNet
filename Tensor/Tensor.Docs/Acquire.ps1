#!/usr/bin/pwsh
# Script to acquire DocFX.
# It fetches DocFX, either using NuGet or a local source and runs to build command.

param(
    [string] $docfxSrc = "NuGet",
    [string] $docfxNuGet = "https://www.myget.org/F/docfx_fsharp/api/v3/index.json",
    [string] $docfxPath = "../../../../docfx"
)

$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot
if (Test-Path -Path docfx) {
    Remove-Item -Recurse -Force docfx
}
New-Item -ItemType Directory docfx

if ($PSVersionTable.Platform -eq "Unix") {
    $nuget = "nuget"
} else {
    $nuget = "$PSScriptRoot/docfx/nuget.exe"
    Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nuget
}

Push-Location docfx
if ($docfxSrc -eq "NuGet") {
    & $nuget install docfx.console -source $docfxNuGet -ExcludeVersion -Prerelease 
    & $nuget install memberpage -source $docfxNuGet -ExcludeVersion -Prerelease 
} elseif ($docfxSrc -eq "Path") {
    $docfxPath = Resolve-Path $docfxPath
    New-Item -ItemType Directory -Path "docfx.console"
    New-Item -ItemType SymbolicLink -Path "docfx.console/tools" -Value "$docfxPath/src/docfx/bin/Release/net461"
    New-Item -ItemType Directory -Path "memberpage/content"
    New-Item -ItemType SymbolicLink -Path "memberpage/content/ManagedReference.extension.js" -Value "$docfxPath/plugins/Microsoft.DocAsCode.Build.MemberLevelManagedReference/resources/ManagedReference.extension.js"
    New-Item -ItemType SymbolicLink -Path "memberpage/content/ManagedReference.overwrite.js" -Value "$docfxPath/plugins/Microsoft.DocAsCode.Build.MemberLevelManagedReference/resources/ManagedReference.overwrite.js"
    New-Item -ItemType SymbolicLink -Path "memberpage/content/toc.html.js" -Value "$docfxPath/plugins/Microsoft.DocAsCode.Build.MemberLevelManagedReference/resources/toc.html.js"
    New-Item -ItemType SymbolicLink -Path "memberpage/content/partials" -Value "$docfxPath/plugins/Microsoft.DocAsCode.Build.MemberLevelManagedReference/resources/partials"
    New-Item -ItemType SymbolicLink -Path "memberpage/content/plugins" -Value "$docfxPath/plugins/Microsoft.DocAsCode.Build.MemberLevelManagedReference/bin/Release/net461"
} else {
    Write-Host "docfxSrc must be either NuGet or Path."
    exit 1
}
if ($PSVersionTable.Platform -eq "Unix") {
    chmod +x "docfx.console/tools/docfx.exe"
}
Pop-Location

Pop-Location

