# Additional XNA profile inventory

Generated from `tools/profile-inventory/profiles.json`. The completed seven-assembly Windows runtime baseline remains separate and unchanged at 257 types. Microsoft reference binaries are never copied into this repository.

| Profile | Reference assemblies | Evidence | Types | Members | Baseline overlap | Recommended status |
| --- | ---: | --- | ---: | ---: | ---: | --- |
| GamerServices and Avatar | 2 | measured | 51 | 502 | 0 | inventory-only; consider deterministic unsupported behavior for service-backed operations |
| Networking / Net | 1 | measured | 23 | 174 | 0 | inventory-only; separate future compatibility profile |
| Xbox 360 runtime | 0 | not-configured | pending | pending | pending | out-of-scope until an authoritative reference pack and executable platform evidence are supplied |
| Windows Phone runtime and sensors/devices | 0 | not-configured | pending | pending | pending | out-of-scope until an authoritative reference pack and executable platform evidence are supplied |
| Content Pipeline / build-time | 7 | measured | 128 | 743 | 0 | separate future build-time product and roadmap; never merge into the 257-type runtime profile |

## GamerServices and Avatar

- Platform/service availability: Historical Games for Windows LIVE/Guide services are not qualified and may be extinct or unavailable.
- Reference set: authoritative for the locally archived Windows XNA 4.0 GamerServices/Avatar extension assemblies
- Assemblies: `Microsoft.Xna.Framework.GamerServices.dll`, `Microsoft.Xna.Framework.Avatar.dll`
  - `Microsoft.Xna.Framework.GamerServices.dll`: 4.0.0.0, SHA-256 `7c6effed97aa25a95c5e095d9c261f5581e402180cc073271a367b9eef79c8af`
  - `Microsoft.Xna.Framework.Avatar.dll`: 4.0.0.0, SHA-256 `b3c70bbe469000b9e11507cc63b88d54a4e0bc5c27afc826d66e0aee51640871`

## Networking / Net

- Platform/service availability: Packet APIs are local; discovery, sessions, transport, and gamer identity require unqualified historical services/runtime behavior.
- Reference set: authoritative for the locally archived Windows XNA 4.0 Net extension assembly
- Assemblies: `Microsoft.Xna.Framework.Net.dll`
  - `Microsoft.Xna.Framework.Net.dll`: 4.0.0.0, SHA-256 `39739dbf5f6ba02e1d0b02ed404f6fe0692497848bc1a6a25be132d47ed9c151`

## Xbox 360 runtime

- Platform/service availability: Requires Xbox 360 hardware/runtime and platform services unavailable in this checkpoint.
- Reference set: not-established: no legally supplied Xbox 360 XNA 4.0 reference pack is available locally
- Assemblies: pending authoritative reference pack

## Windows Phone runtime and sensors/devices

- Platform/service availability: Requires phone-specific reference assemblies, device APIs, and executable device/emulator evidence unavailable in this checkpoint.
- Reference set: not-established: no legally supplied Windows Phone XNA reference pack is available locally
- Assemblies: pending authoritative reference pack

## Content Pipeline / build-time

- Platform/service availability: Build-time Windows/D3D/D3DX tooling surface; not a runtime game assembly profile.
- Reference set: authoritative for the locally archived Windows XNA Game Studio 4.0 pipeline/importer assemblies
- Assemblies: `Microsoft.Xna.Framework.Content.Pipeline.dll`, `Microsoft.Xna.Framework.Content.Pipeline.AudioImporters.dll`, `Microsoft.Xna.Framework.Content.Pipeline.EffectImporter.dll`, `Microsoft.Xna.Framework.Content.Pipeline.FBXImporter.dll`, `Microsoft.Xna.Framework.Content.Pipeline.TextureImporter.dll`, `Microsoft.Xna.Framework.Content.Pipeline.VideoImporters.dll`, `Microsoft.Xna.Framework.Content.Pipeline.XImporter.dll`
  - `Microsoft.Xna.Framework.Content.Pipeline.dll`: 4.0.0.0, SHA-256 `1ea481da62eb9e66bad8589df83f173725e62d22362801e1264cd02347721c69`
  - `Microsoft.Xna.Framework.Content.Pipeline.AudioImporters.dll`: 4.0.0.0, SHA-256 `1daca08f4e4f12da5ff095e7861e7702fd41f761a8c2ece5ee714894b7684165`
  - `Microsoft.Xna.Framework.Content.Pipeline.EffectImporter.dll`: 4.0.0.0, SHA-256 `9f30ac3db23c14afce527ac18fdf29905dcd7bad48273a88b3040c918b789c8d`
  - `Microsoft.Xna.Framework.Content.Pipeline.FBXImporter.dll`: 4.0.0.0, SHA-256 `01a22ab5ed7a08f0e4b7a856fe73b1e3fa12a8789026d43125bedb7edcf93f91`
  - `Microsoft.Xna.Framework.Content.Pipeline.TextureImporter.dll`: 4.0.0.0, SHA-256 `3b418ce4a72f1d090563363530a9fc0859b0d6a9c637a500dab1a4901696eba6`
  - `Microsoft.Xna.Framework.Content.Pipeline.VideoImporters.dll`: 4.0.0.0, SHA-256 `8768d9992c303af401fed6af20eb4d3548bed48a9c05f125f22e278b2fc0b492`
  - `Microsoft.Xna.Framework.Content.Pipeline.XImporter.dll`: 4.0.0.0, SHA-256 `ccb90834938ee0ab129f0a9089cff8dcd7827759d05cd86ea97e70be1b1928e8`
