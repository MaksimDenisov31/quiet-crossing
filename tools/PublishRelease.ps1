param([switch]$CreateRepository, [switch]$Release)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $project
$env:GIT_TERMINAL_PROMPT = '0'
$raw = "protocol=https`nhost=github.com`n`n" | git credential fill
if ($LASTEXITCODE -ne 0) { throw 'Git credential helper failed.' }
$credential = @{}
foreach ($line in $raw) {
    if ($line -match '^([^=]+)=(.*)$') { $credential[$matches[1]] = $matches[2] }
}
if (-not $credential['password']) { throw 'No saved Git credential.' }
$headers = @{ Authorization = 'Bearer ' + $credential['password']; 'User-Agent' = 'QuietCrossing-Release'; Accept = 'application/vnd.github+json' }
$base = 'https://api.github.com'
$repo = 'MaksimDenisov31/quiet-crossing'
$account = Invoke-RestMethod -Uri "$base/user" -Headers $headers
if ($account.login -ne 'MaksimDenisov31') { throw 'Unexpected GitHub account.' }
if ($CreateRepository) {
    try { $existing = Invoke-RestMethod -Uri "$base/repos/$repo" -Headers $headers }
    catch {
        if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw }
        $body = @{ name='quiet-crossing'; description='Тихий переход — минималистичная пиксельная игра для Windows'; private=$false; auto_init=$false } | ConvertTo-Json
        $existing = Invoke-RestMethod -Method Post -Uri "$base/user/repos" -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($body))
    }
    if (-not $existing.permissions.push) { throw 'GitHub did not grant write permission.' }
    Write-Output ('Repository ready: ' + $existing.html_url)
}
if ($Release) {
    $revision = git -c "safe.directory=$project" rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve release commit.' }
    $sha = $revision.Trim()
    $body = @{
        tag_name='v1.2.0'; target_commitish=$sha; name='Тихий переход 1.2.0 — маршрут через город'; draft=$false; prerelease=$false
        body="Большая карта и маршрут от точки А до точки Б вместо одного перехода.`n`n- Пять улиц с двумя–четырьмя полосами и разным случайным потоком машин.`n- Скверы, тротуары, дорожки с поворотами и дома, которые нужно обходить.`n- Пять последовательных отметок; победа только после полного маршрута и достижения Б.`n- Мини-карта, общая карта на M и прогресс маршрута. Открытая карта ставит игру на паузу.`n- Все три вида камеры и прежние настройки сохранены. При столкновении маршрут начинается заново с А.`n`nWASD / стрелки — идти; M — карта; V — вид; Q/E — оглядеться; Esc — пауза; R — повтор. Windows 10/11, .NET Framework 4.8+. Setup — установщик, Portable.zip — переносимая версия."
    } | ConvertTo-Json
    $releaseInfo = Invoke-RestMethod -Method Post -Uri "$base/repos/$repo/releases" -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($body))
    $upload = $releaseInfo.upload_url -replace '\{.*$', ''
    foreach ($name in @('QuietCrossing-Setup-1.2.0.exe','QuietCrossing-Portable-1.2.0.zip','SHA256SUMS.txt')) {
        $file = Join-Path $project "installer-output\$name"
        $asset = Invoke-RestMethod -Method Post -Uri ($upload + '?name=' + [Uri]::EscapeDataString($name)) -Headers $headers -ContentType 'application/octet-stream' -InFile $file
        Write-Output ($asset.name + ': ' + $asset.browser_download_url)
    }
    Write-Output ('Release: ' + $releaseInfo.html_url)
}
