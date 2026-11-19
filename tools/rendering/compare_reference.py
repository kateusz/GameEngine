"""Compare procedural GPU captures, and write portable PNG previews. Standard library only.

python tools/rendering/compare_reference.py CAPTURE --compare REFERENCE --exact
No golden is ever overwritten. Exact mode is for repeated runs on the same driver/build.
"""
import argparse
import json
import math
from pathlib import Path
import struct
import zlib


def png(path, rgba, width, height):
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data))
    rows = b''.join(b'\0' + rgba[y * width * 4:(y + 1) * width * 4] for y in range(height))
    path.write_bytes(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0))
                     + chunk(b'IDAT', zlib.compress(rows)) + chunk(b'IEND', b''))


def compare(actual, expected, floating):
    if len(actual) != len(expected):
        raise ValueError('capture dimensions/length changed')
    a = struct.unpack('<' + 'f' * (len(actual) // 4), actual) if floating else actual
    b = struct.unpack('<' + 'f' * (len(expected) // 4), expected) if floating else expected
    if not all(math.isfinite(x) for x in a) or not all(math.isfinite(x) for x in b):
        return {'identical': actual == expected, 'nonfinite': True}
    differences = [abs(x - y) for x, y in zip(a, b)]
    return {'identical': actual == expected, 'max_abs': max(differences, default=0),
            'mse': sum(x * x for x in differences) / max(len(differences), 1)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('capture', type=Path)
    parser.add_argument('--compare', type=Path)
    parser.add_argument('--exact', action='store_true')
    args = parser.parse_args()
    run = json.loads((args.capture / 'run.json').read_text())
    names = {f'{scene}-{frame}' for scene in run['Scenarios'] for frame in range(3)}
    report = {}
    if args.compare:
        other = json.loads((args.compare / 'run.json').read_text())
        if run.get('CaptureVersion') != other.get('CaptureVersion'):
            raise ValueError('capture input version changed; these runs are not equivalent reference inputs')
        for key in ('Width', 'Height', 'Scenarios', 'HdrFormat', 'DisplayFormat'):
            if run[key] != other[key]:
                raise ValueError(f'incompatible capture setting: {key}')
    for name in sorted(names):
        rgba = (args.capture / (name + '.rgba8')).read_bytes()
        if len(rgba) != run['Width'] * run['Height'] * 4:
            raise ValueError(f'{name}: invalid display byte count')
        png(args.capture / (name + '.png'), rgba, run['Width'], run['Height'])
        if args.compare:
            report[name] = {}
            for extension in ('rgba8', 'rgba32f', 'ids'):
                a = (args.capture / (name + '.' + extension)).read_bytes()
                b = (args.compare / (name + '.' + extension)).read_bytes()
                if extension == 'ids':
                    if len(a) != len(b):
                        raise ValueError(f'{name}: ID surface size changed')
                    ids_a = struct.unpack('<' + 'i' * (len(a) // 4), a)
                    ids_b = struct.unpack('<' + 'i' * (len(b) // 4), b)
                    report[name][extension] = {'identical': a == b,
                                              'different_pixels': sum(x != y for x, y in zip(ids_a, ids_b))}
                else:
                    report[name][extension] = compare(a, b, extension == 'rgba32f')
    if args.compare:
        identical = sum(all(x['identical'] for x in frame.values()) for frame in report.values())
        (args.capture / 'comparison.json').write_text(json.dumps(report, indent=2))
        print(f'Identical HDR + display + IDs: {identical}/{len(names)} frames')
        for name, frame in report.items():
            if not all(x['identical'] for x in frame.values()):
                print(name, json.dumps(frame))
        if args.exact and identical != len(names):
            raise SystemExit(1)
    else:
        print(f'Wrote {len(names)} PNG previews; original HDR/display/ID captures preserved')


if __name__ == '__main__':
    main()
