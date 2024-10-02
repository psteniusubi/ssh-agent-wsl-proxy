[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [version]
    $Version
)
process {
    $root = Split-Path $PSScriptRoot -Parent -Resolve -ErrorAction Stop
    $csproj = Join-Path $root "ssh-agent-wsl-proxy/ssh-agent-wsl-proxy.csproj" -Resolve -ErrorAction Stop | Convert-Path
    
    $xml = [xml]::new()
    $xml.PreserveWhitespace = $true
    $null = $xml.Load($csproj)
    $ProjectVersion = $xml.SelectSingleNode("/Project/PropertyGroup/Version")
    $ProjectVersion.InnerText = $Version
    $null = $xml.Save($csproj)

    Write-Host -ForegroundColor Green @"
nano README.md
git commit -a -m "release v$($Version)"
git push
git branch "release/v$($Version)"
git push origin "release/v$($Version)"
"@
}