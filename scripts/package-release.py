from pathlib import Path
import hashlib
import sys
import zipfile

root = Path(__file__).resolve().parents[1]
dist = root / 'dist'
version = sys.argv[1]
with zipfile.ZipFile(dist / 'ScreenMirror-source.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for directory in ('src', 'scripts', 'installer', 'tests', '.github'):
        for path in sorted((root / directory).rglob('*')):
            if path.is_file() and '__pycache__' not in path.parts:
                archive.write(path, 'ScreenMirror/' + path.relative_to(root).as_posix())
    for name in ('LICENSE', 'README.md', '.gitignore'):
        archive.write(root / name, 'ScreenMirror/' + name)
    for path in sorted((dist / 'corresponding-source').rglob('*')):
        if path.is_file():
            archive.write(path, 'corresponding-source/' + path.relative_to(dist / 'corresponding-source').as_posix())
artifacts = [dist / f'ScreenMirror-Setup-{version}-x64.exe', dist / 'ScreenMirror-source.zip']
lines = []
for path in artifacts:
    with path.open('rb') as stream:
        digest = hashlib.file_digest(stream, 'sha256').hexdigest()
    lines.append(f'{digest}  {path.name}')
(dist / 'SHA256SUMS.txt').write_text('\n'.join(lines) + '\n')
