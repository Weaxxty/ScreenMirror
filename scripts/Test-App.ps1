param([Parameter(Mandatory=$true)][string]$AppDirectory)
$ErrorActionPreference = 'Stop'
$runtime = Join-Path $AppDirectory 'runtime'
$env:PATH = (Join-Path $runtime 'bin') + ';' + $env:PATH
$env:GST_PLUGIN_PATH = Join-Path $runtime 'lib\gstreamer-1.0'
$env:GST_PLUGIN_SYSTEM_PATH_1_0 = $env:GST_PLUGIN_PATH
$env:GST_PLUGIN_SCANNER = Join-Path $runtime 'libexec\gstreamer-1.0\gst-plugin-scanner.exe'
$tempDir = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { $env:TEMP }
$env:GST_REGISTRY_1_0 = Join-Path $tempDir ('gst-test-' + [guid]::NewGuid() + '.bin')
& (Join-Path $runtime 'bin\uxplay.exe') -h
if ($LASTEXITCODE -ne 0) { throw 'Bundled UxPlay cannot start.' }
foreach ($element in @('appsrc','queue','decodebin','h264parse','avdec_h264','videoconvert','videoscale','videoflip','d3d11videosink','avdec_aac','avdec_alac','audioconvert','audioresample','volume','level','autoaudiosink','wasapisink','jpegdec','imagefreeze','textoverlay')) {
    Write-Host "Checking GStreamer element: $element"
    & (Join-Path $runtime 'bin\gst-inspect-1.0.exe') --exists $element
    if ($LASTEXITCODE -ne 0) { throw "Missing or broken GStreamer element: $element" }
}
$app = Start-Process (Join-Path $AppDirectory 'ScreenMirror.exe') -ArgumentList '--smoke-test' -PassThru
if (!$app.WaitForExit(20000)) { $app.Kill(); throw 'GUI smoke test timed out.' }
if ($app.ExitCode -ne 0) {
    $logs = Join-Path $env:LOCALAPPDATA 'ScreenMirror'
    Get-ChildItem $logs -Filter 'session-*.log' | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | ForEach-Object { Get-Content $_.FullName -Tail 100 }
    throw 'GUI could not keep the bundled receiver running.'
}
Write-Host 'Engine, plugins and GUI startup: OK'
