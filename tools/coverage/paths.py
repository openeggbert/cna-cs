"""Portable repository/native discovery shared by the legacy coverage utilities."""

from __future__ import annotations

import os
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
_HEADER_MARKER = Path("modules/c-api/include/CNA/C/media_library.h")


def find_cna_root() -> Path:
    configured = os.environ.get("CNA_ROOT")
    if configured:
        root = Path(configured).expanduser().resolve()
        if (root / _HEADER_MARKER).is_file():
            return root
        raise SystemExit(f"CNA_ROOT={root} does not contain {_HEADER_MARKER}.")

    search_parents = (REPO_ROOT.parent, REPO_ROOT.parent.parent)
    preferred = [parent / "cna" for parent in search_parents]
    candidates = preferred + [
        child
        for parent in search_parents
        if parent.is_dir()
        for child in sorted(parent.iterdir())
        if child.is_dir()
    ]

    seen: set[Path] = set()
    matches: list[Path] = []
    for candidate in candidates:
        candidate = candidate.resolve()
        if candidate not in seen and (candidate / _HEADER_MARKER).is_file():
            seen.add(candidate)
            matches.append(candidate)

    if not matches:
        raise SystemExit(
            "No CNA checkout was found near this repository. Set CNA_ROOT to a checkout "
            f"containing {_HEADER_MARKER}."
        )

    chosen = matches[0]
    if len(matches) > 1:
        print(f"# headers: {chosen} (of {len(matches)} checkouts; set CNA_ROOT to select one)")
    return chosen


def find_native_libraries(cna_root: Path) -> list[Path]:
    explicit = os.environ.get("CNA_NATIVE_LIBRARY")
    if explicit:
        library = Path(explicit).expanduser().resolve()
        if not library.is_file():
            raise SystemExit(f"CNA_NATIVE_LIBRARY={library} is not a file.")
        return [library]

    configured_directory = os.environ.get("CNA_NATIVE_DIR")
    directories = [Path(configured_directory).expanduser().resolve()] if configured_directory else []
    directories.extend(
        path
        for pattern in ("build*", "cmake-build-*")
        for path in cna_root.glob(pattern)
        if path.is_dir()
    )

    names = ("libcna_c_api.so", "libcna_c_api.dylib", "cna_c_api.dll")
    found: set[Path] = set()
    for directory in directories:
        for name in names:
            direct = directory / name
            nested = directory / "modules" / "c-api" / name
            if direct.is_file():
                found.add(direct.resolve())
            if nested.is_file():
                found.add(nested.resolve())
    return sorted(found)
