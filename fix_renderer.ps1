$file = "C:\Projects\AtlayaView\Core\CushionRenderer.cs"
$lines = [System.IO.File]::ReadAllLines($file, [System.Text.Encoding]::UTF8)
Write-Host "Zeilen vorher: $($lines.Count)"
$part1 = $lines[0..259]
$part2 = $lines[585..($lines.Count-1)]
$result = $part1 + @("") + $part2
[System.IO.File]::WriteAllLines($file, $result, [System.Text.Encoding]::UTF8)
Write-Host "Fertig: $($result.Count) Zeilen"
