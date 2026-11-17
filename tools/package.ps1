$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$version = (Get-Content -LiteralPath (Join-Path $root 'version.txt') -First 1).Trim()
$dist = Join-Path $root 'dist'
if (-not (Test-Path -LiteralPath $dist)) { New-Item -ItemType Directory -Path $dist | Out-Null }

$zip = Join-Path $dist ('BahamaStudio-' + $version + '-x64.zip')
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$items = @(
  @{ Src = Join-Path $root 'bin\BahamaStudio.exe';        Entry = 'BahamaStudio.exe' },
  @{ Src = Join-Path $root 'bin\BahamaStudio.exe.config'; Entry = 'BahamaStudio.exe.config' },
  @{ Src = Join-Path $root 'install.cmd';                 Entry = 'install.cmd' },
  @{ Src = Join-Path $root 'uninstall.cmd';               Entry = 'uninstall.cmd' },
  @{ Src = Join-Path $root 'README.md';                   Entry = 'README.md' },
  @{ Src = Join-Path $root 'LICENSE';                     Entry = 'LICENSE' },
  @{ Src = Join-Path $root 'version.txt';                 Entry = 'version.txt' }
)

$archive = [System.IO.Compression.ZipFile]::Open($zip, [System.IO.Compression.ZipArchiveMode]::Create)
try {
  foreach ($item in $items) {
    $entry = $archive.CreateEntry($item.Entry, [System.IO.Compression.CompressionLevel]::Optimal)
    $target = $entry.Open()
    try {
      $bytes = [System.IO.File]::ReadAllBytes($item.Src)
      $target.Write($bytes, 0, $bytes.Length)
    }
    finally { $target.Dispose() }
  }

  # Nomes de entrada com '/' explicito: compativel com todas as ferramentas ZIP.
  $engineDir = Join-Path $root 'engine'
  foreach ($file in (Get-ChildItem -LiteralPath $engineDir -File)) {
    $entry = $archive.CreateEntry('engine/' + $file.Name, [System.IO.Compression.CompressionLevel]::Optimal)
    $target = $entry.Open()
    try {
      $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
      $target.Write($bytes, 0, $bytes.Length)
    }
    finally { $target.Dispose() }
  }
}
finally { $archive.Dispose() }

$sums = Join-Path $dist 'SHA256SUMS.txt'
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath $sums -Value ($hash + '  ' + (Split-Path -Leaf $zip)) -Encoding ASCII

Write-Output ('Pacote: ' + $zip)
Write-Output ('Somas: ' + $sums)
