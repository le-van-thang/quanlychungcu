# =========================
# run-tests.ps1
# =========================
# Chạy test của project WpfApp1.Tests, lưu TRX vào Reports\TestResults
# Mặc định mở file TRX bằng Visual Studio (thông qua explorer.exe).
# Nếu có cài ReportUnit/trx2html thì sẽ tự sinh HTML và mở HTML.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---- Đường dẫn cơ sở (script nằm ngay thư mục solution) ----
$root        = Split-Path -Parent $PSCommandPath
$testDll     = Join-Path $root 'WpfApp1.Tests\bin\Debug\WpfApp1.Tests.dll'
$runsettings = Join-Path $root 'Test.runsettings'
$resultsDir  = Join-Path $root 'Reports\TestResults'

# ---- Tạo thư mục kết quả nếu chưa có ----
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

# ---- File kết quả theo timestamp ----
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$trxName   = "Result_$timestamp.trx"
$trxPath   = Join-Path $resultsDir $trxName

# ---- Tìm vstest.console.exe (VS2022 Community) ----
$vstest = Get-ChildItem -Path "C:\Program Files\Microsoft Visual Studio\2022" `
                        -Recurse -Filter vstest.console.exe `
                        -ErrorAction SilentlyContinue |
          Select-Object -First 1 -ExpandProperty FullName
if (-not $vstest) { $vstest = "vstest.console.exe" } # fallback nếu PATH đã có

# ---- Kiểm tra đầu vào tối thiểu ----
if (-not (Test-Path $testDll)) {
    throw "Không tìm thấy test DLL: $testDll. Hãy Build project test (Debug) trước."
}
if (-not (Test-Path $runsettings)) {
    throw "Không tìm thấy runsettings: $runsettings."
}

# ---- Chạy test ----
& $vstest `
  $testDll `
  "/Settings:$runsettings" `
  "/Logger:trx;LogFileName=$trxName" `
  "/ResultsDirectory:$resultsDir"

Write-Host ""
Write-Host "✅ Test run complete."
Write-Host "TRX : $trxPath"

# =========================
#  TÙY CHỌN: Sinh HTML nếu có tool
#    - ReportUnit:   reportunit <srcFolder> <outFolder>
#    - trx2html:     trx2html <input.trx> <output.html>
# =========================
$htmlPath = $null
if (Get-Command reportunit -ErrorAction SilentlyContinue) {
    reportunit $resultsDir $resultsDir | Out-Null
    # ReportUnit đặt tên theo assembly; tìm file HTML mới nhất
    $htmlPath = Get-ChildItem $resultsDir -Filter *.html | Sort-Object LastWriteTime -Desc | Select-Object -First 1 -ExpandProperty FullName
}
elseif (Get-Command trx2html -ErrorAction SilentlyContinue) {
    $htmlPath = Join-Path $resultsDir ("Result_{0}.html" -f $timestamp)
    trx2html $trxPath $htmlPath | Out-Null
    if (-not (Test-Path $htmlPath)) { $htmlPath = $null }
}

# ---- Mở kết quả: ưu tiên HTML, không có thì mở TRX ----
if ($htmlPath -and (Test-Path $htmlPath)) {
    Write-Host "HTML: $htmlPath"
    Start-Process "explorer.exe" $htmlPath
}
elseif (Test-Path $trxPath) {
    Start-Process "explorer.exe" $trxPath
}
else {
    Write-Host "⚠️ Không tìm thấy file kết quả để mở."
}
