# unity-setup

`com.gishadev.unity-setup` — a project-setup automation window for new Unity 6 projects. Scaffolds the standard folder structure and imports the packages/assets I use in every project, so a fresh project is ready to work in within a couple minutes.

## Install

Add via Package Manager → **Add package from git URL**:

```
https://github.com/gishadev/unity-setup.git
```

Or drop it in as a git submodule under `Assets/`.

## Usage

Open **Tools → gishadev → Unity Setup**. Each step below has its own "Run" button and shows a live log of exactly what's being installed/imported.

- **Create Folders** — scaffolds the standard `_Project` folder structure (Materials, Prefabs, Scripts, Scenes, Settings) and cleans up the default template clutter
- **Import Essentials** — 2D Animation, Cinemachine, Input System, UniTask, TextMeshPro
- **Import Main Tools** — PrimeTween, VContainer, [`com.gishadev.tools`](https://github.com/gishadev/gishadev-tools), plus the generated pool/audio enums and extension methods it needs
- **Import Odin** — Odin Inspector & Serializer, imported from your local Asset Store cache
- **Import Editor Helpers** — vFolders 2 and vFavorites 2, also from the Asset Store cache

Steps that install packages lock assembly reloads for their duration, so a package that brings new scripts (VContainer, gishadev-tools, ...) can't get interrupted by a domain reload mid-install.

## License

MIT — see [LICENSE](LICENSE).
