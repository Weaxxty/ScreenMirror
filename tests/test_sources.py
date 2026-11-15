import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('collect_sources', Path(__file__).parents[1] / 'scripts/collect-sources.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class PackageSourcesTest(unittest.TestCase):
    def test_source_base_differs_from_binary_package(self):
        self.assertEqual(module.archive_name({'NAME': 'mingw-w64-ucrt-x86_64-gstreamer',
                                             'BASE': 'mingw-w64-gstreamer', 'VERSION': '1.28.9-1'}),
                         'mingw-w64-gstreamer-1.28.9-1.src.tar.zst')

    def test_epoch_not_in_source_filename(self):
        self.assertEqual(module.archive_name({'BASE': 'mingw-w64-ffmpeg', 'VERSION': '2:8.0-3'}),
                         'mingw-w64-ffmpeg-8.0-3.src.tar.zst')

    def test_missing_source_base_fails(self):
        with self.assertRaises(ValueError):
            module.archive_name({'NAME': 'unknown', 'VERSION': '1-1'})

    def test_local_database_keeps_multiline_fields(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'desc'
            path.write_text('%NAME%\nbinary\n\n%BASE%\nsource\n\n%VERSION%\n1-2\n\n%LICENSE%\nMIT\nLGPL\n', encoding='utf-8')
            self.assertEqual(module.read_desc(path), {'NAME': 'binary', 'BASE': 'source',
                                                     'VERSION': '1-2', 'LICENSE': 'MIT\nLGPL'})


if __name__ == '__main__':
    unittest.main()
