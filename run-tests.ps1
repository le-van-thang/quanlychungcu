# =========================
# run-tests.ps1 (stable + ReportGenerator)
# =========================
param(
  [string]$Config = "Release",     # Debug | Release
  [string]$Platform = "x64",       # x64 | AnyCPU
  [switch]$UseVSTest               # Nếu muốn dùng vstest.console.exe
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# ---- Thư mục gốc ----
$root = $PSScriptRoot
if (-not $root) { $root = (Get-Location).Path }

# ---- Đường dẫn ----
$projTest   = Join-Path $root 'WpfApp1.Tests\WpfApp1.Tests.csproj'
$binDir     = Join-Path $root "WpfApp1.Tests\bin\$Platform\$Config"
$resultsDir = Join-Path $root 'Reports\TestResults'
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

# ---- Tìm .runsettings ----
$runsettings = Join-Path $root 'Test.runsettings'
if (-not (Test-Path $runsettings)) {
  $alt = Join-Path $root 'WpfApp1.Tests\Test.runsettings'
  if (Test-Path $alt) { $runsettings = $alt }
}
if (-not (Test-Path $runsettings)) {
  throw "Không tìm thấy .runsettings (mong đợi: .\Test.runsettings hoặc .\WpfApp1.Tests\Test.runsettings)."
}

# ---- Tên file kết quả ----
$ts     = Get-Date -Format 'yyyyMMdd_HHmmss'
$trx    = "Result_$ts.trx"
$trxOut = Join-Path $resultsDir $trx
$start  = Get-Date

# ============================
# 1. Chạy test
# ============================
if (-not $UseVSTest) {
  dotnet test $projTest -c $Config -p:Platform=$Platform --settings $runsettings `
    --results-directory $resultsDir --logger "trx;LogFileName=$trx"
}
else {
  dotnet build $projTest -c $Config -p:Platform=$Platform | Out-Null
  $testDll = Get-ChildItem (Join-Path $binDir '*Tests.dll') | Select-Object -First 1 -Expand FullName
  if (-not $testDll) { throw "Không thấy DLL test trong $binDir" }

  $vstest = (Get-ChildItem "C:\Program Files\Microsoft Visual Studio\2022" -Recurse -Filter vstest.console.exe -ErrorAction SilentlyContinue |
             Select-Object -First 1 -Expand FullName)
  if (-not $vstest) { $vstest = "vstest.console.exe" }

  & $vstest $testDll "/Settings:$runsettings" "/ResultsDirectory:$resultsDir" "/Logger:trx;LogFileName=$trx"
}

Write-Host ""
Write-Host "Test run complete."
Write-Host "TRX: $trxOut"

# ============================
# 2. Chờ file xuất hiện
# ============================
$exists = $false
for ($i=0; $i -lt 30; $i++) {
  if (Test-Path $trxOut) { $exists = $true; break }
  Start-Sleep -Milliseconds 500
}
if (-not $exists) {
  $latest = Get-ChildItem $resultsDir -Filter *.trx |
            Where-Object { $_.LastWriteTime -ge $start.AddSeconds(-5) } |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
  if ($latest) { $trxOut = $latest.FullName; $exists = $true }
}

# ============================
# 3. Sinh HTML bằng ReportGenerator (nếu có)
# ============================
$html = $null
$htmlDir = Join-Path $resultsDir 'HtmlReport'

if ($exists -and (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
  Write-Host "`nGenerating HTML report..."
  New-Item -ItemType Directory -Force -Path $htmlDir | Out-Null
  reportgenerator -reports:"$trxOut" -targetdir:"$htmlDir" -reporttypes:HtmlInline_AzurePipelines | Out-Null
  $html = Join-Path $htmlDir 'index.html'
}
elseif ($exists -and (Get-Command reportunit -ErrorAction SilentlyContinue)) {
  reportunit $resultsDir $resultsDir | Out-Null
  $html = (Get-ChildItem $resultsDir -Filter *.html | Sort-Object LastWriteTime -Desc | Select-Object -First 1 -Expand FullName)
}

# ============================
# 4. Mở kết quả
# ============================
if ($html -and (Test-Path $html)) {
  Write-Host "HTML: $html"
  Start-Process $html
}
elseif ($exists) {
  Start-Process explorer.exe "/select,`"$trxOut`""
}
else {
  Write-Warning "Không tìm thấy file kết quả. Kiểm tra thư mục: $resultsDir"
}

Write-Host ""
Write-Host "Hoàn tất. Kết quả test lưu tại: $resultsDir"
