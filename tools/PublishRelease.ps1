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
        tag_name='v1.6.0'; target_commitish=$sha; name='Тихий переход 1.6.0 — свободная мышь и взгляд на Q/E'; draft=$false; prerelease=$false
        body="Мышь теперь управляет поворотом без удержания ПКМ.`n`n- Движение мыши поворачивает персонажа и позволяет смотреть вверх/вниз. Край экрана не ограничивает горизонтальный поворот.`n- Q/E временно поворачивают голову в стороны; после отпускания взгляд возвращается. W сохраняет направление тела.`n- Tab освобождает курсор для интерфейса, повторный Tab или щелчок по сцене возвращает обзор.`n- Пауза, карта, потеря фокуса и вид сверху освобождают системный курсор; возврат в игру включает обзор.`n- Курс тела и взгляд отдельно отмечены на мини-карте. Правило границ зебры и все прежние настройки сохранены.`n`nWASD — движение, мышь — поворот, Q/E — оглядеться, M — карта, V — вид, Esc — пауза. Windows 10/11, .NET Framework 4.8+. Setup — установщик, Portable.zip — переносимая версия."
    } | ConvertTo-Json
    $releaseInfo = Invoke-RestMethod -Method Post -Uri "$base/repos/$repo/releases" -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($body))
    $upload = $releaseInfo.upload_url -replace '\{.*$', ''
    foreach ($name in @('QuietCrossing-Setup-1.6.0.exe','QuietCrossing-Portable-1.6.0.zip','SHA256SUMS.txt')) {
        $file = Join-Path $project "installer-output\$name"
        $asset = Invoke-RestMethod -Method Post -Uri ($upload + '?name=' + [Uri]::EscapeDataString($name)) -Headers $headers -ContentType 'application/octet-stream' -InFile $file
        Write-Output ($asset.name + ': ' + $asset.browser_download_url)
    }
    Write-Output ('Release: ' + $releaseInfo.html_url)
}
