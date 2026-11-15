param([string]$Version = '1.0.0')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$payload = Join-Path $root 'dist\payload'
New-Item -ItemType Directory -Force $payload | Out-Null
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (!(Test-Path $csc)) { throw '.NET Framework compiler not found.' }
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must be X.Y.Z.' }
$source = Join-Path $root 'build\ScreenMirror.Build.cs'
New-Item -ItemType Directory -Force (Split-Path $source) | Out-Null
$code = Get-Content (Join-Path $root 'src\ScreenMirror.cs') -Raw -Encoding UTF8
$code = $code.Replace('AssemblyVersion("1.0.0.0")', ('AssemblyVersion("' + $Version + '.0")'))
$code = $code.Replace('AssemblyFileVersion("1.0.0.0")', ('AssemblyFileVersion("' + $Version + '.0")'))
Set-Content -LiteralPath $source -Value $code -Encoding UTF8
$bootstrap = Join-Path $root 'build\IconBuilder.exe'
$icon = Join-Path $payload 'ScreenMirror.ico'
$exe = Join-Path $payload 'ScreenMirror.exe'
New-Item -ItemType Directory -Force (Split-Path $bootstrap) | Out-Null
$refs = @('/reference:System.dll','/reference:System.Core.dll','/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll')
& $csc /nologo /target:winexe /optimize+ /codepage:65001 @refs "/out:$bootstrap" $source
if ($LASTEXITCODE -ne 0) { throw 'Icon builder compilation failed.' }
$maker = Start-Process $bootstrap -ArgumentList @('--icon',('"' + $icon + '"')) -Wait -PassThru
if ($maker.ExitCode -ne 0 -or !(Test-Path $icon)) { throw 'Icon generation failed.' }
& $csc /nologo /target:winexe /optimize+ /codepage:65001 @refs "/win32icon:$icon" "/out:$exe" $source
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed.' }
Copy-Item (Join-Path $root 'LICENSE') (Join-Path $payload 'LICENSE.txt')
Copy-Item (Join-Path $root 'README.md') (Join-Path $payload 'README.md')
Copy-Item (Join-Path $root 'build\UxPlay\LICENSE') (Join-Path $payload 'licenses\UxPlay-GPL-3.0.txt')
$sourceUrl = 'See the matching ScreenMirror-source.zip distributed alongside this installer.'
if ($env:GITHUB_REPOSITORY -and $env:GITHUB_REF_TYPE -eq 'tag') {
    $sourceUrl = "https://github.com/$env:GITHUB_REPOSITORY/releases/download/$env:GITHUB_REF_NAME/ScreenMirror-source.zip"
}
@"
ScreenMirror $Version
Launcher: MIT; see LICENSE.txt.
UxPlay: GPL-3.0; see licenses/UxPlay-GPL-3.0.txt.
Runtime libraries retain their original licenses; see licenses/runtime.
Exact package versions: runtime-versions.txt.
Corresponding source files and build recipes: $sourceUrl
Runtime source provenance and SHA-256: runtime-sources.json.
All runtime libraries are dynamically linked. Replacement DLLs can be placed in runtime/bin.
"@ | Set-Content (Join-Path $payload 'THIRD-PARTY-NOTICES.txt') -Encoding UTF8
