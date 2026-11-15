#!/usr/bin/env python3
"""Collect exact MSYS2 corresponding source archives for shipped runtime files."""
import hashlib
import json
from pathlib import Path
import sys
import time
from urllib.request import urlopen


def read_desc(path):
    sections = {}
    current = None
    for line in path.read_text(encoding='utf-8').splitlines():
        if line.startswith('%') and line.endswith('%'):
            current = line.strip('%')
            sections[current] = []
        elif line and current:
            sections[current].append(line)
    return {k: '\n'.join(v) for k, v in sections.items()}


def archive_name(record):
    base = record.get('BASE')
    if not base:
        raise ValueError('Package has no BASE metadata: ' + record.get('NAME', '?'))
    version = record['VERSION'].split(':', 1)[-1]
    return f'{base}-{version}.src.tar.zst'


def download(name, destination):
    urls = [f'https://repo.msys2.org/mingw/sources/{name}',
            f'https://mirror.yandex.ru/mirrors/msys2/mingw/sources/{name}']
    last_error = None
    for attempt in range(3):
        for url in urls:
            temp = destination.with_suffix('.part')
            try:
                with urlopen(url, timeout=60) as response, temp.open('wb') as out:
                    while chunk := response.read(1024 * 1024):
                        out.write(chunk)
                if temp.stat().st_size < 100:
                    raise ValueError('Empty source archive')
                temp.replace(destination)
                return url
            except Exception as exc:
                last_error = exc
                temp.unlink(missing_ok=True)
        time.sleep(attempt + 1)
    raise RuntimeError(f'Cannot collect source archive {name}: {last_error}')


def main(root):
    packages = set((root / 'build/runtime-packages.txt').read_text().splitlines())
    records = {}
    for desc in Path('/var/lib/pacman/local').glob('*/desc'):
        record = read_desc(desc)
        if record.get('NAME') in packages:
            records[record['NAME']] = record
    if records.keys() != packages:
        raise RuntimeError('Missing installed package metadata: ' + str(packages - records.keys()))
    destination = root / 'dist/corresponding-source/runtime'
    destination.mkdir(parents=True, exist_ok=True)
    archives = {}
    result = []
    for name in sorted(packages):
        record = records[name]
        filename = archive_name(record)
        target = destination / filename
        if filename not in archives:
            print(f'Downloading source: {filename}', flush=True)
            url = download(filename, target)
            with target.open('rb') as stream:
                digest = hashlib.file_digest(stream, 'sha256').hexdigest()
            archives[filename] = (url, digest)
        url, digest = archives[filename]
        result.append({'package': name, 'version': record['VERSION'], 'source_archive': filename,
                       'source_url': url, 'source_sha256': digest})
    (root / 'dist/payload/runtime-sources.json').write_text(json.dumps(result, indent=2) + '\n')
    (root / 'dist/corresponding-source/runtime-sources.json').write_text(json.dumps(result, indent=2) + '\n')
    print(f'Collected {len(archives)} source archives for {len(packages)} runtime packages.')


if __name__ == '__main__':
    main(Path(sys.argv[1]).resolve())
