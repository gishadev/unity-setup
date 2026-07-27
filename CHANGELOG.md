## [1.3.0] - 2026-07-27
### Added
- `Folders.CreateExtensionsClass` now generates `GeneratedExtensionMethods.cs` as part of "Import Main Tools" — moved here from `gishadev.tools.Core.CodeGenerator` since it's static, one-time scaffolding rather than something that needs to re-run on every pool/audio data edit
### Changed
- Stub pool enums renamed: `SoundEffectsEnum` → `SFXPoolEnum`, `VisualEffectsEnum` → `VFXPoolEnum`
- Generated `EmitAt` extension overloads now take `rotation` as optional, matching the updated `gishadev.tools` emitter signatures

## [1.2.1] - 2026-07-27
### Fixed
- Installing a package that brings new scripts (e.g. VContainer, then `tools-polish` right after it) could get interrupted by Unity's domain reload mid-step, silently dropping every install queued after the one that triggered it. Each step run now locks assembly reloads (`EditorApplication.LockReloadAssemblies`) until it's fully done

## [1.2.0] - 2026-07-27
### Changed
- Simplified the Unity Setup window: removed the checkbox multi-select and batched "Run Selected" (which was buggy) in favor of one "Run" button per step that runs immediately on its own

## [1.1.1] - 2026-07-27
### Changed
- Menu moved to `Tools/gishadev/Unity Setup`
- Added the project logo to the setup window header

## [1.1.0] - 2026-07-27
### Added
- `Tools/Setup/Unity Setup` window replacing the blind one-click menu items: pick any combination of setup steps and watch a live log of exactly what's being installed/imported.
- Import Essentials now also installs UniTask and imports TMP Essential Resources.

## [1.0.0] - 2025-07-02
### First Release
- Unity setup