Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ==== Paths ====
$root       = Split-Path -Parent $PSCommandPath
$resultsDir = Join-Path $root 'Reports\TestResults'
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

# Lấy .trx mới nhất
$trx = Get-ChildItem $resultsDir -Filter *.trx -File -ErrorAction SilentlyContinue |
       Sort-Object LastWriteTime -Descending | Select-Object -First 1

# ❗ Nếu không có .trx thì bỏ qua, KHÔNG báo lỗi để CI không fail
if (-not $trx) {
    Write-Host "⚠ Không tìm thấy file .trx trong $resultsDir. Bỏ qua bước generate HTML."
    exit 0        # <- QUAN TRỌNG: kết thúc step với exit code 0
}

$ts      = Get-Date -Format 'yyyyMMdd_HHmmss'
$outHtml = Join-Path $resultsDir ("Result_{0}.html" -f $ts)

# =================== XSLT ===================
$xsl = @'
<xsl:stylesheet version="1.0"
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:t="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <xsl:output method="html" encoding="utf-8" indent="yes"/>

  <xsl:template match="/">
    <html>
      <head>
        <meta charset="utf-8"/>
        <title>Test run details</title>
        <style>
          body{font-family:Segoe UI,Arial,sans-serif;margin:24px}
          h1{margin:0 0 16px 0}
          .grid{display:grid;grid-template-columns:repeat(4,200px);gap:16px;margin-bottom:24px}
          .card{border:1px solid #ddd;border-radius:8px;padding:12px}
          table{border-collapse:collapse;width:100%}
          th,td{border-bottom:1px solid #eee;padding:6px 8px;text-align:left;font-size:14px}
          .pass{color:#138000;font-weight:600}
          .fail{color:#c40000;font-weight:600}
        </style>
      </head>
      <body>
        <h1>Test run details</h1>

        <div class="grid">
          <div class="card">
            <div>Tổng tests</div>
            <div style="font-size:28px;">
              <xsl:value-of select="count(//t:UnitTestResult)"/>
            </div>
          </div>
          <div class="card">
            <div>Passed</div>
            <div style="font-size:28px;" class="pass">
              <xsl:value-of select="count(//t:UnitTestResult[@outcome='Passed'])"/>
            </div>
          </div>
          <div class="card">
            <div>Failed</div>
            <div style="font-size:28px;" class="fail">
              <xsl:value-of select="count(//t:UnitTestResult[@outcome='Failed'])"/>
            </div>
          </div>
          <div class="card">
            <div>Duration</div>
            <div style="font-size:28px;">
              <xsl:choose>
                <xsl:when test="//t:Times/@duration">
                  <xsl:value-of select="//t:Times/@duration"/>
                </xsl:when>
                <xsl:otherwise>N/A</xsl:otherwise>
              </xsl:choose>
            </div>
          </div>
        </div>

        <div class="card">
          <table>
            <thead>
              <tr><th>Result</th><th>Test Name</th><th>Duration</th><th>Error Message</th></tr>
            </thead>
            <tbody>
              <xsl:for-each select="//t:UnitTestResult">
                <tr>
                  <td>
                    <xsl:attribute name="class">
                      <xsl:choose>
                        <xsl:when test="@outcome='Passed'">pass</xsl:when>
                        <xsl:otherwise>fail</xsl:otherwise>
                      </xsl:choose>
                    </xsl:attribute>
                    <xsl:value-of select="@outcome"/>
                  </td>
                  <td>
                    <xsl:variable name="id" select="@testId"/>
                    <xsl:value-of select="//t:UnitTest[@id=$id]/t:TestMethod/@name"/>
                  </td>
                  <td><xsl:value-of select="@duration"/></td>
                  <td>
                    <xsl:value-of select="normalize-space(t:Output/t:ErrorInfo/t:Message)"/>
                  </td>
                </tr>
              </xsl:for-each>
            </tbody>
          </table>
        </div>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>
'@

# ============== Transform ==============
$xslt      = New-Object System.Xml.Xsl.XslCompiledTransform
$xmlReader = [System.Xml.XmlReader]::Create([System.IO.StringReader]$xsl)
$xslt.Load($xmlReader)

$input  = [System.Xml.XmlReader]::Create($trx.FullName)
$output = New-Object System.IO.FileStream($outHtml, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
$xslt.Transform($input, $null, $output)
$output.Close()
$input.Close()

Write-Host "✅ Generated HTML report: $outHtml"
