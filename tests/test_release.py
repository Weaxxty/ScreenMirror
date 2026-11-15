from pathlib import Path
import hashlib
import shutil
import subprocess
import sys
import tempfile
import unittest
import zipfile


class ReleaseArchiveTest(unittest.TestCase):
    def test_sources_and_checksums_without_private_repository_files(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for folder in ('src', 'scripts', 'installer', 'tests', '.github', 'dist/corresponding-source/runtime', '.git'):
                (root / folder).mkdir(parents=True, exist_ok=True)
            shutil.copy(Path(__file__).parents[1] / 'scripts/package-release.py', root / 'scripts/package-release.py')
            (root / 'src/ScreenMirror.cs').write_text('// source fixture')
            (root / 'dist/corresponding-source/runtime/library.src.tar.zst').write_bytes(b'source fixture')
            (root / '.git/private-token').write_text('must not be distributed')
            for name in ('LICENSE', 'README.md', '.gitignore'):
                (root / name).write_text(name)
            setup = root / 'dist/ScreenMirror-Setup-1.0.0-x64.exe'
            setup.write_bytes(b'installer test fixture, not an executable')
            subprocess.run([sys.executable, str(root / 'scripts/package-release.py'), '1.0.0'], check=True)
            with zipfile.ZipFile(root / 'dist/ScreenMirror-source.zip') as archive:
                names = archive.namelist()
                self.assertIn('ScreenMirror/src/ScreenMirror.cs', names)
                self.assertIn('corresponding-source/runtime/library.src.tar.zst', names)
                self.assertFalse(any('.git/' in name for name in names))
            checksums = (root / 'dist/SHA256SUMS.txt').read_text()
            self.assertIn(hashlib.sha256(setup.read_bytes()).hexdigest(), checksums)
            self.assertIn('ScreenMirror-source.zip', checksums)


if __name__ == '__main__':
    unittest.main()
