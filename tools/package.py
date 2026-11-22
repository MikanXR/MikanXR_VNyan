#!/usr/bin/env python3
"""Package the built plugin as the release zip, or check a release zip.

The zip is what MikanLauncher installs the plugin from: plugin.json at the root beside
the two files it names. The version comes from <Version> in MikanXR_VNyan.csproj.

Usage: python tools/package.py [--dll <file>] [--bundle <file>] [--out <dir>]
       python tools/package.py --verify <zip> [--tag <tag>]

  --dll <file>      the built assembly, bin/Release/netstandard2.1/MikanXR_VNyan.dll by default
  --bundle <file>   the shader bundle, Build/mikanxr_shaders.bundle by default
  --out <dir>       where the zip is written, dist/ by default
  --verify <zip>    check a zip against the contract instead of building one
  --tag <tag>       with --verify, the release tag the zip's version must match (v<version>)

Building writes dist/MikanXR_VNyan_<version>.zip, checks it and prints the SHA-256 of
each file in it. Both modes exit non-zero on a problem.
"""

import argparse
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path, PurePosixPath

REPO = Path(__file__).resolve().parent.parent
CSPROJ = REPO / "MikanXR_VNyan.csproj"

PLUGIN_ID = "mikanxr-vnyan"
HOST = "vnyan"
ZIP_PREFIX = "MikanXR_VNyan_"
# The zip's files and where each one goes, relative to the VNyan install folder
FILES = [
    ("MikanXR_VNyan.dll", "Items/Assemblies/MikanXR_VNyan.dll"),
    ("mikanxr_shaders.bundle", "Items/Assemblies/MikanXR/mikanxr_shaders.bundle"),
]


def fail(message):
    print(f"error: {message}", file=sys.stderr)
    sys.exit(1)


def csproj_version():
    found = re.search(r"<Version>([^<]+)</Version>", CSPROJ.read_text(encoding="utf-8"))
    if not found:
        fail(f"no <Version> in {CSPROJ}")
    return found.group(1).strip()


def assembly_version(data):
    """The ProductVersion string of a built assembly's version resource, or None."""
    key = "ProductVersion".encode("utf-16-le")
    at = data.find(key)
    if at < 0:
        return None
    # The value follows the key's terminator, padded to a 4 byte boundary within the resource
    rest = data[at + len(key):at + len(key) + 256].decode("utf-16-le", errors="replace")
    return rest.lstrip("\0").split("\0", 1)[0] or None


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def manifest(version):
    return {
        "id": PLUGIN_ID,
        "version": version,
        "host": HOST,
        "files": [{"src": src, "dest": dest} for src, dest in FILES],
    }


def is_plain_relative(path):
    parts = PurePosixPath(path).parts
    return bool(parts) and "\\" not in path and ":" not in path \
        and not path.startswith("/") and ".." not in parts and "." not in parts


def verify(zip_path, tag=None):
    """Checks a zip against the contract. Returns the list of problems, empty when it passes."""
    problems = []
    zip_path = Path(zip_path)
    try:
        archive = zipfile.ZipFile(zip_path)
    except (OSError, zipfile.BadZipFile) as error:
        return [f"cannot open {zip_path}: {error}"]

    with archive:
        names = [info.filename for info in archive.infolist() if not info.is_dir()]
        expected_names = ["plugin.json"] + [src for src, _ in FILES]
        if sorted(names) != sorted(expected_names):
            problems.append(f"zip holds {sorted(names)}, expected {sorted(expected_names)}")
        if "plugin.json" not in names:
            return problems

        try:
            found = json.loads(archive.read("plugin.json").decode("utf-8"))
        except (UnicodeDecodeError, json.JSONDecodeError) as error:
            return problems + [f"plugin.json does not parse: {error}"]

        version = found.get("version") if isinstance(found, dict) else None
        if not isinstance(version, str) or not version:
            return problems + ["plugin.json has no version"]
        if found != manifest(version):
            problems.append(f"plugin.json is {found}, expected {manifest(version)}")
        for entry in found.get("files", []) if isinstance(found.get("files"), list) else []:
            for key in ("src", "dest"):
                value = entry.get(key) if isinstance(entry, dict) else None
                if not isinstance(value, str) or not is_plain_relative(value):
                    problems.append(f"{key} {value!r} is not a plain relative path")

        if zip_path.name != f"{ZIP_PREFIX}{version}.zip":
            problems.append(f"zip is named {zip_path.name}, expected {ZIP_PREFIX}{version}.zip")
        if tag is not None and tag != f"v{version}":
            problems.append(f"tag {tag} does not match version {version}, expected v{version}")

        for src, _ in FILES:
            if src in names and archive.getinfo(src).file_size == 0:
                problems.append(f"{src} is empty")
        if "MikanXR_VNyan.dll" in names:
            stamped = assembly_version(archive.read("MikanXR_VNyan.dll"))
            if stamped != version:
                problems.append(f"MikanXR_VNyan.dll is stamped {stamped}, plugin.json says {version}")
    return problems


def build(dll, bundle, out_dir):
    version = csproj_version()
    sources = {"MikanXR_VNyan.dll": Path(dll), "mikanxr_shaders.bundle": Path(bundle)}
    hints = {
        "MikanXR_VNyan.dll": "dotnet build MikanXR_VNyan.csproj -c Release",
        "mikanxr_shaders.bundle": "the Unity menu MikanXR / Build Shader Bundle, see the README",
    }
    contents = {}
    for name, path in sources.items():
        if not path.is_file():
            fail(f"no {name} at {path}; build it with {hints[name]}")
        contents[name] = path.read_bytes()

    stamped = assembly_version(contents["MikanXR_VNyan.dll"])
    if stamped != version:
        fail(f"{sources['MikanXR_VNyan.dll']} is stamped {stamped} but the csproj says {version}; rebuild it")

    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    zip_path = out_dir / f"{ZIP_PREFIX}{version}.zip"
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("plugin.json", json.dumps(manifest(version), indent=4) + "\n")
        for src, _ in FILES:
            archive.writestr(src, contents[src])

    problems = verify(zip_path)
    if problems:
        fail(f"{zip_path} failed its own check: " + "; ".join(problems))

    print(f"Wrote {zip_path} (version {version}, tag v{version})")
    for src, _ in FILES:
        print(f"  {sha256(contents[src])}  {src}")
    print(f"  {sha256(zip_path.read_bytes())}  {zip_path.name}")


def main():
    parser = argparse.ArgumentParser(description="Package the built plugin, or check a release zip.")
    parser.add_argument("--dll", default=REPO / "bin/Release/netstandard2.1/MikanXR_VNyan.dll")
    parser.add_argument("--bundle", default=REPO / "Build/mikanxr_shaders.bundle")
    parser.add_argument("--out", default=REPO / "dist")
    parser.add_argument("--verify", metavar="ZIP")
    parser.add_argument("--tag")
    args = parser.parse_args()

    if args.verify:
        problems = verify(args.verify, args.tag)
        for problem in problems:
            print(f"error: {problem}", file=sys.stderr)
        if problems:
            sys.exit(1)
        print(f"{args.verify} matches the plugin zip contract")
    else:
        build(args.dll, args.bundle, args.out)


if __name__ == "__main__":
    main()
