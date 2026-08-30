"""Upstream CNA ABI release-to-release diff, restricted to what this binding consumes.

`sweep.py` answers "does the current binding match the current headers". This answers the
different question the native ABI policy asks before a new CNA generation may enter the reviewed
matrix in `eng/cna-native-abi-policy.json`: **what changed upstream between the generation the
matrix already accepts and the one being proposed, and does any of it reach the surface this
binding actually consumes.**

Two evidence paths are compared, because neither alone is sufficient:

* **Header prototypes** are the C authority for signatures. A function can change its return type,
  parameter types, or arity without changing its exported name, and an exported-symbol list cannot
  see that.
* **CNA's own `tools/c-api/abi_baseline.json`** is the authority for struct sizes/alignments/field
  offsets, scalar widths, named integer constants, and string constants. Header text cannot
  cheaply answer what `sizeof(CNA_GameCreateInfo)` is on this platform; the upstream baseline was
  measured by a compiler.

Struct, scalar, constant, and string changes are reported for the *whole* upstream surface rather
than filtered to a consumed subset. That is deliberate and conservative: this binding declares 80
interop structs and several enum-like identities whose native counterparts are named only in prose
and in the C probe, so a filter here would be a guess, and a guess that silently hides a layout
change is worse than a report that occasionally names something irrelevant. Function prototypes
*are* filtered, because the imported entry points are an exact, machine-readable list.

Usage:

    python3 tools/coverage/baselinediff.py --from <source> --to <source>

where each source is a CNA checkout directory, optionally suffixed with a git revision:

    python3 tools/coverage/baselinediff.py \\
      --from ../../cna@1d6da4af8 --to ../../cnanext

A difference that has been reviewed and adjudicated -- CNA removing renderers this binding does not
name, say -- goes in an allowlist file passed with ``--allowlist``, one exact finding per line with
a ``#`` comment saying who decided and why. The allowlist is checked in both directions: an entry
that does not match anything is reported as stale and fails, because a reviewed exception that has
silently stopped applying is how an allowlist turns into a blindfold.

Exit codes: 0 when nothing the policy calls breaking changed, 1 when something did, 2 for a bad
invocation. A zero exit is evidence for a matrix entry; it is not the matrix entry itself, and it
says nothing about behavior that keeps its shape.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path

from paths import REPO_ROOT

HEADER_SUBPATH = Path("modules/c-api/include/CNA/C")
BASELINE_SUBPATH = Path("tools/c-api/abi_baseline.json")
NATIVE_CS = REPO_ROOT / "src/CNA.Interop/Native.cs"

_BLOCK_COMMENT = re.compile(r"/\*.*?\*/", re.S)
_LINE_COMMENT = re.compile(r"//[^\n]*")
_DECLARATION = re.compile(
    r"CNA_C_API\s+([^;{]*?)\b(cna_[A-Za-z0-9_]+)\s*\(([^;]*?)\)\s*;", re.S
)
_IMPORT = re.compile(r"internal\s+static\s+(?:unsafe\s+)?partial\s+\S+\s+(cna_[a-z0-9_]+)\s*\(")


class Source:
    """One side of the comparison: a checkout, optionally at a git revision."""

    def __init__(self, spec: str, stack: list) -> None:
        self.spec = spec
        checkout, _, revision = spec.partition("@")
        self.root = Path(checkout).expanduser().resolve()
        self.revision = revision or None
        if not self.root.is_dir():
            raise SystemExit(f"{spec}: {self.root} is not a directory.")

        if self.revision is None:
            self.header_dir = self.root / HEADER_SUBPATH
            baseline_path = self.root / BASELINE_SUBPATH
            self.baseline = _read_json(baseline_path) if baseline_path.is_file() else None
        else:
            extracted = Path(tempfile.mkdtemp(prefix="cna-baselinediff-"))
            stack.append(extracted)
            _extract(self.root, self.revision, HEADER_SUBPATH, extracted)
            self.header_dir = extracted / HEADER_SUBPATH
            self.baseline = _read_git_json(self.root, self.revision, BASELINE_SUBPATH)

        if not self.header_dir.is_dir():
            raise SystemExit(f"{spec}: no {HEADER_SUBPATH} below {self.root}.")
        self.prototypes = _prototypes(self.header_dir)

    @property
    def abi_version(self) -> str:
        if self.baseline and "abi_version" in self.baseline:
            version = self.baseline["abi_version"]
            return f"{version['major']}.{version['minor']}.{version['patch']}"
        return _header_abi_version(self.header_dir)

    def __str__(self) -> str:
        return f"{self.spec} (ABI {self.abi_version}, {len(self.prototypes)} exports)"


def _read_json(path: Path):
    with path.open(encoding="utf-8") as handle:
        return json.load(handle)


def _read_git_json(root: Path, revision: str, subpath: Path):
    completed = subprocess.run(
        ["git", "-C", str(root), "show", f"{revision}:{subpath.as_posix()}"],
        capture_output=True,
        text=True,
        check=False,
    )
    return json.loads(completed.stdout) if completed.returncode == 0 else None


def _extract(root: Path, revision: str, subpath: Path, destination: Path) -> None:
    archive = subprocess.run(
        ["git", "-C", str(root), "archive", revision, subpath.as_posix()],
        capture_output=True,
        check=False,
    )
    if archive.returncode != 0:
        raise SystemExit(
            f"git archive {revision}:{subpath} failed in {root}: "
            f"{archive.stderr.decode(errors='replace').strip()}"
        )
    extract = subprocess.run(
        ["tar", "-x", "-C", str(destination)], input=archive.stdout, capture_output=True, check=False
    )
    if extract.returncode != 0:
        raise SystemExit(f"Extracting {revision}:{subpath} failed: {extract.stderr.decode()}")


def _prototypes(header_dir: Path) -> dict[str, str]:
    """name -> normalised `return-type|parameter-list`, parsed from the declaration syntax.

    Keying on `CNA_C_API` rather than on the identifier is the same rule `sweep.py` documents: a
    doc comment that names a route in prose is indistinguishable from a declaration to any pattern
    built from the name.
    """
    found: dict[str, str] = {}
    for path in sorted(header_dir.rglob("*.h")):
        text = path.read_text(encoding="utf-8", errors="replace")
        text = _LINE_COMMENT.sub("", _BLOCK_COMMENT.sub("", text))
        for match in _DECLARATION.finditer(text):
            returns, name, parameters = match.groups()
            found[name] = " ".join(f"{returns}|{parameters}".split())
    return found


def _header_abi_version(header_dir: Path) -> str:
    abi = header_dir / "abi.h"
    if not abi.is_file():
        return "unknown"
    text = abi.read_text(encoding="utf-8", errors="replace")
    parts = []
    for field in ("MAJOR", "MINOR", "PATCH"):
        match = re.search(rf"#define\s+CNA_ABI_VERSION_{field}\s+UINT32_C\((\d+)\)", text)
        parts.append(match.group(1) if match else "?")
    return ".".join(parts)


def _consumed_symbols() -> set[str]:
    return set(_IMPORT.findall(NATIVE_CS.read_text(encoding="utf-8")))


def _diff_mapping(old, new, label: str, findings: list[str], notes: list[str]) -> None:
    """Report removals and value changes in a name -> value mapping; count additions only."""
    if old is None or new is None:
        notes.append(f"{label}: not measured on both sides; no evidence either way")
        return
    removed = sorted(key for key in old if key not in new)
    changed = sorted(key for key in old if key in new and old[key] != new[key])
    added = sum(1 for key in new if key not in old)
    for key in removed:
        findings.append(f"{label}: {key} was removed")
    for key in changed:
        findings.append(f"{label}: {key} changed {old[key]!r} -> {new[key]!r}")
    notes.append(
        f"{label}: {len(old)} -> {len(new)}; {len(removed)} removed, {len(changed)} changed, "
        f"{added} added"
    )


def _diff_structs(old, new, findings: list[str], notes: list[str]) -> None:
    if old is None or new is None:
        notes.append("structs: not measured on both sides; no evidence either way")
        return
    changed = 0
    for name, before in sorted(old.items()):
        after = new.get(name)
        if after is None:
            findings.append(f"struct {name} was removed")
            changed += 1
            continue
        if (before["size"], before["align"]) != (after["size"], after["align"]):
            findings.append(
                f"struct {name} size/align {before['size']}/{before['align']} -> "
                f"{after['size']}/{after['align']}"
            )
            changed += 1
            continue
        for field, offset in sorted(before["fields"].items()):
            moved = after["fields"].get(field)
            if moved is None:
                findings.append(f"struct {name}: field {field} was removed")
                changed += 1
            elif moved != offset:
                findings.append(
                    f"struct {name}: field {field} offset/size {offset} -> {moved}"
                )
                changed += 1
    added = sum(1 for name in new if name not in old)
    notes.append(
        f"structs: {len(old)} -> {len(new)}; {changed} breaking difference(s), {added} added"
    )


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(
        description="Diff two upstream CNA ABI generations across the surface this binding consumes."
    )
    parser.add_argument("--from", dest="old", required=True, help="checkout[@revision] already in the matrix")
    parser.add_argument("--to", dest="new", required=True, help="checkout[@revision] being proposed")
    parser.add_argument("--json", dest="json_output", help="write the machine-readable report here")
    parser.add_argument(
        "--allowlist",
        help="file of reviewed, adjudicated findings to accept; one exact finding per line, "
        "'#' comments allowed",
    )
    arguments = parser.parse_args(argv)

    stack: list[Path] = []
    try:
        old = Source(arguments.old, stack)
        new = Source(arguments.new, stack)

        consumed = _consumed_symbols()
        findings: list[str] = []
        notes: list[str] = []

        missing = sorted(name for name in consumed if name not in new.prototypes)
        for name in missing:
            findings.append(f"consumed export {name} is absent from the proposed headers")

        signature_changes = sorted(
            name
            for name in consumed
            if name in old.prototypes
            and name in new.prototypes
            and old.prototypes[name] != new.prototypes[name]
        )
        for name in signature_changes:
            findings.append(
                f"consumed export {name} changed prototype:\n"
                f"      was: {old.prototypes[name]}\n"
                f"      now: {new.prototypes[name]}"
            )

        unknown = sorted(name for name in consumed if name not in old.prototypes)
        notes.append(
            f"consumed exports: {len(consumed)}; {len(missing)} absent, "
            f"{len(signature_changes)} changed prototype, {len(unknown)} not present in the "
            f"accepted generation"
        )
        notes.append(
            f"all exports: {len(old.prototypes)} -> {len(new.prototypes)}; "
            f"{len([n for n in old.prototypes if n not in new.prototypes])} removed, "
            f"{len([n for n in new.prototypes if n not in old.prototypes])} added"
        )

        # A consumed removal is already reported above with the reason that matters; repeating it
        # here under "not consumed here" would contradict itself.
        removed_any = sorted(
            name
            for name in old.prototypes
            if name not in new.prototypes and name not in consumed
        )
        for name in removed_any:
            findings.append(f"export {name} was removed upstream (not consumed here)")

        old_baseline = old.baseline or {}
        new_baseline = new.baseline or {}
        _diff_structs(old_baseline.get("structs"), new_baseline.get("structs"), findings, notes)
        _diff_mapping(old_baseline.get("scalars"), new_baseline.get("scalars"), "scalars", findings, notes)
        _diff_mapping(old_baseline.get("strings"), new_baseline.get("strings"), "strings", findings, notes)

        old_integers = dict(old_baseline.get("integers") or {})
        new_integers = dict(new_baseline.get("integers") or {})
        version_constants = [
            "CNA_ABI_VERSION",
            "CNA_ABI_VERSION_MAJOR",
            "CNA_ABI_VERSION_MINOR",
            "CNA_ABI_VERSION_PATCH",
        ]
        for name in version_constants:
            old_integers.pop(name, None)
            new_integers.pop(name, None)
        _diff_mapping(
            old_integers or None, new_integers or None, "integer constants", findings, notes
        )
        notes.append(
            "integer constants: the four CNA_ABI_VERSION_* values are excluded; a version that "
            "did not change would mean the two sides are the same generation"
        )

        allowed: list[str] = []
        if arguments.allowlist:
            allowlist_path = Path(arguments.allowlist)
            if not allowlist_path.is_file():
                raise SystemExit(f"--allowlist {allowlist_path} is not a file.")
            allowed = [
                line.strip()
                for line in allowlist_path.read_text(encoding="utf-8").splitlines()
                if line.strip() and not line.lstrip().startswith("#")
            ]

        accepted = [finding for finding in findings if finding in allowed]
        findings = [finding for finding in findings if finding not in allowed]
        stale = [entry for entry in allowed if entry not in accepted]
        for entry in stale:
            findings.append(
                f"allowlist entry matched nothing and is stale: {entry}"
            )
        if allowed:
            notes.append(
                f"allowlist: {len(allowed)} reviewed entr(y/ies), {len(accepted)} applied, "
                f"{len(stale)} stale"
            )

        print(f"# from: {old}")
        print(f"# to:   {new}")
        print()
        for note in notes:
            print(f"  {note}")
        print()
        if accepted:
            print(f"REVIEWED AND ALLOWED ({len(accepted)}):")
            for finding in accepted:
                print(f"  - {finding}")
            print()
        if findings:
            print(f"BREAKING DIFFERENCES ({len(findings)}):")
            for finding in findings:
                print(f"  - {finding}")
        else:
            print("BREAKING DIFFERENCES (0): none across exports, prototypes, structs, scalars,")
            print("  integer constants, and string constants.")

        if arguments.json_output:
            report = {
                "schemaVersion": 1,
                "status": "failed" if findings else "passed",
                "from": {"spec": old.spec, "abiVersion": old.abi_version},
                "to": {"spec": new.spec, "abiVersion": new.abi_version},
                "consumedExportCount": len(consumed),
                "notes": notes,
                "allowed": accepted,
                "findings": findings,
            }
            output = Path(arguments.json_output)
            output.parent.mkdir(parents=True, exist_ok=True)
            output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")

        return 1 if findings else 0
    finally:
        for path in stack:
            subprocess.run(["rm", "-rf", str(path)], check=False)


if __name__ == "__main__":
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    raise SystemExit(main(sys.argv[1:]))
