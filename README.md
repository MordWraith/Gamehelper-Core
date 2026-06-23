# Gamehelper Core

English-only GameHelper distribution for Path of Exile 2, based on [Gordin/GameHelper2](https://github.com/MordWraith/Gamehelper-Core), with a fixed plugin set and signed updates via the launcher.

**Version:** 1.0.0

## Download & install

**Releases:** https://github.com/MordWraith/Gamehelper-Core/releases

1. Download the latest release ZIP (or the downloader, if provided).
2. Extract to a folder of your choice.
3. Run `GameHelper.exe`.

No .NET SDK or build tools required for normal use.

## Updates

Updates are delivered through the **launcher** (signed `manifest.json` from GitHub Releases).  
You do not need to clone this repository or run any scripts to stay up to date.

## Included plugins

| [Gordin](https://github.com/MordWraith/Gamehelper-Core) · GameHelper2 | [yokkenUA](https://github.com/yokkenUA) | [MordWraith](https://github.com/MordWraith) |
|----------------------------------------------------------------|----------------------------------------|---------------------------------------------|
| AutoHotKeyTrigger | [Atlas](https://github.com/MordWraith/Gamehelper-Core) | [RitualHelper](https://github.com/MordWraith/Gamehelper-Core) |
| Radar | [LootTracker](https://github.com/MordWraith/Gamehelper-Core) | [Autopot](https://github.com/MordWraith/Gamehelper-Core) |
| HealthBars | [RunecraftHelper](https://github.com/MordWraith/Gamehelper-Core) | [PlayerBuffBar](https://github.com/MordWraith/Gamehelper-Core) |
| PreloadAlert | [SekhemaHelper](https://github.com/MordWraith/Gamehelper-Core) | [Hiveblood](https://github.com/MordWraith/Gamehelper-Core) |
| LootValue | | [AuraTracker](https://github.com/MordWraith/Gamehelper-Core) |
| | | [SimpleBars](https://github.com/MordWraith/Gamehelper-Core) |
| | | [AmanamuVoidAlert](https://github.com/MordWraith/Gamehelper-Core) |

*Gordin plugins are bundled with [GameHelper2](https://github.com/MordWraith/Gamehelper-Core) — no separate repositories.*

**Note:** LootTracker and LootValue can run together; you may see overlapping loot overlays.

## Source code

This repository contains the **application source** (core, launcher, shared libraries, and bundled plugin sources) under GPLv3.

To build from source you need the [.NET SDK](https://dotnet.microsoft.com/download):

```powershell
dotnet build GameOverlay.sln -c Release
```

Pre-built binaries with all plugins are published on the [Releases](https://github.com/MordWraith/Gamehelper-Core/releases) page.

## License

See [CREDITS.md](CREDITS.md) for authors and upstream projects.
