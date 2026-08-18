# binding-generator (planned, not implemented)

This directory is a placeholder for the codegen tool referenced throughout
`docs/architecture.md` and `plan.md`: something that generates the
repetitive, mechanical parts of `CNA.XnaCompat` from `CNA.Framework` (and
possibly from `CNA.Interop`'s ABI declarations) instead of hand-writing them.

Per `openeggbert/cna`'s `analysis_binding.md` §74, automation is a good fit for:

- raw FFI declarations,
- enum mappings (see the two parallel `Keys` enums this would remove),
- simple value structs with implicit-conversion pairs (see the duplicated
  `Vector2`/`Color` in `CNA.Framework` vs `CNA.XnaCompat`),
- repetitive resource wrapper boilerplate,
- documentation/parity tables.

and a poor fit for anything involving actual API/behavior design (the `Game`
callback bridge, `ContentManager.Load<T>` dispatch, ownership rules) — those
stay hand-written.

## Why this doesn't exist yet

Building a generator before there is enough real, hand-written surface to
generalize from would be premature abstraction. The current plan (see
`../../plan.md` Phase 4-5) is: keep growing `CNA.Framework`/`CNA.XnaCompat`
by hand for now, and revisit this tool once the duplicated-value-type and
enum-mirroring pattern shows up often enough to be worth automating.
