# Gamehelper Core

English-only **Path of Exile 2** overlay for Windows x64 — a ready-made bundle based on [Gordin/GameHelper2](https://github.com/Gordin/GameHelper2), with selected community plugins and signed updates.

**Download:** [Releases](https://github.com/MordWraith/Gamehelper-Core/releases) · **Trust & updates:** [SECURITY.md](SECURITY.md) · **Credits:** [CREDITS.md](CREDITS.md)

## Download

| What | Link |
|------|------|
| **Installer (recommended)** | [GameHelperDownloader.exe](https://github.com/MordWraith/Gamehelper-Core/releases/latest/download/GameHelperDownloader.exe) |
| **Full ZIP** | [latest release ZIP](https://github.com/MordWraith/Gamehelper-Core/releases/latest) |
| **All releases** | [Releases](https://github.com/MordWraith/Gamehelper-Core/releases/latest) |

**Requires:** Windows x64 and the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).

## Install

1. Use an **empty folder** — do not extract over an old install.
2. Run **GameHelperDownloader.exe** there, or extract the full ZIP.
3. Start **`GameHelper.exe`** (launcher). Do not start `GameHelper.App.exe` directly.
4. Check **`VERSION.txt`** matches the release you downloaded.

## Updates

The launcher can download signed updates from GitHub Releases. Your settings (`configs/`, `Plugins/*/config/`) are **not** overwritten.

You do **not** need this repository or any build tools to play or update. Details: [SECURITY.md](SECURITY.md).

**No auto-update?** Install from the full ZIP only.

## First run

Enabled by default: **HealthBars**, **Radar**, **AutoHotKeyTrigger**. Other plugins are installed but off until you enable them in **Plugins**. Performance stats are off by default.

## Included plugins

| [Gordin](https://github.com/Gordin/GameHelper2) · GameHelper2 | [yokkenUA](https://github.com/yokkenUA) | [MordWraith](https://github.com/MordWraith) |
|----------------------------------------------------------------|----------------------------------------|---------------------------------------------|
| AutoHotKeyTrigger | [Atlas](https://github.com/yokkenUA/Atlas) | [RitualHelper](https://github.com/MordWraith/RitualHelper) |
| Radar | [LootTracker](https://github.com/yokkenUA/LootTracker) | [Autopot](https://github.com/MordWraith/Autopot) |
| HealthBars | [RunecraftHelper](https://github.com/yokkenUA/RunecraftHelper) | [PlayerBuffBar](https://github.com/MordWraith/PlayerBuffBar) |
| PreloadAlert | [SekhemaHelper](https://github.com/yokkenUA/SekhemaHelper) | [Hiveblood](https://github.com/MordWraith/Hiveblood) |
| LootValue | | [AuraTracker](https://github.com/MordWraith/AuraTracker) |
| | | [SimpleBars](https://github.com/MordWraith/SimpleBars) |
| | | [AmanamuVoidAlert](https://github.com/MordWraith/AmanamuVoidAlert) |

*Gordin plugins ship with [GameHelper2](https://github.com/Gordin/GameHelper2).*

LootTracker and LootValue can run together; loot overlays may overlap.

## Troubleshooting

| Problem | What to try |
|---------|-------------|
| Wrong or mixed version | Fresh install in a **new empty folder**; read `VERSION.txt` |
| Overlay does not attach | Run GameHelper with the **same admin level** as the game |
| Blocked by Windows Defender | [SECURITY.md → false positives](SECURITY.md#windows-defender-and-antivirus-false-positives) |
| Missing .NET runtime | Install [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) |

## Source code

This repo is the **application source** (GPLv3). Pre-built binaries are on [Releases](https://github.com/MordWraith/Gamehelper-Core/releases).

Developers: open [`GameOverlay.sln`](GameOverlay.sln), **Rebuild Solution** (Release), run `GameHelper.exe` from `GameHelper\bin\Release\net10.0-windows\win-x64\`. Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

## License

[GPLv3](LICENSE) — see [CREDITS.md](CREDITS.md) for authors and upstream projects.
