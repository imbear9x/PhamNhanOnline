$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$packetDir = Join-Path $repoRoot 'GameShared\Packets\Packets'

if (-not (Test-Path $packetDir)) {
    Write-Error "Packet directory not found: $packetDir"
}

$packetFiles = Get-ChildItem -Path $packetDir -Filter *.cs -File | Sort-Object FullName
$pattern = '(?ms)\[Packet(?:\((?<id>\d+)\))?\]\s*(?:\[[^\]]+\]\s*)*public\s+partial\s+class\s+(?<name>\w+)\s*:\s*IPacket'
$entries = New-Object System.Collections.Generic.List[object]

foreach ($file in $packetFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    $matches = [System.Text.RegularExpressions.Regex]::Matches($content, $pattern)

    foreach ($match in $matches) {
        $idText = $match.Groups['id'].Value
        $entries.Add([pscustomobject]@{
                Name = $match.Groups['name'].Value
                Id = if ([string]::IsNullOrWhiteSpace($idText)) { $null } else { [int]$idText }
                File = $file.FullName
            })
    }
}

if ($entries.Count -eq 0) {
    Write-Error "No packet declarations were found under $packetDir"
}

$missingId = @($entries | Where-Object { $null -eq $_.Id })
$duplicateGroups = @($entries | Where-Object { $null -ne $_.Id } | Group-Object Id | Where-Object { $_.Count -gt 1 })
$invalidIds = @($entries | Where-Object { $null -ne $_.Id -and $_.Id -le 0 })

if ($missingId.Count -eq 0 -and $duplicateGroups.Count -eq 0 -and $invalidIds.Count -eq 0) {
    Write-Host "Packet ID check passed. Found $($entries.Count) packet declarations."
    exit 0
}

foreach ($packet in $missingId) {
    Write-Host "Missing [Packet(id)] on $($packet.Name) in $($packet.File)" -ForegroundColor Red
}

foreach ($packet in $invalidIds) {
    Write-Host "Invalid packet id $($packet.Id) on $($packet.Name) in $($packet.File)" -ForegroundColor Red
}

foreach ($group in $duplicateGroups) {
    $owners = $group.Group | ForEach-Object { "$($_.Name) [$($_.File)]" }
    Write-Host "Duplicate packet id $($group.Name): $($owners -join '; ')" -ForegroundColor Red
}

exit 1
