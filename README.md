# Gamehelper Core

English-only GameHelper distribution based on [Gordin/GameHelper2](https://github.com/Gordin/GameHelper2), with a fixed plugin set and signed ZIP updates via the Stable-style launcher.

**Version:** 1.0.0  
**Update repo (default):** `MordWraith/Gamehelper-Core`

## Included plugins

| Plugin | Source |
|--------|--------|
| AutoHotKeyTrigger, Radar, HealthBars, PreloadAlert, LootValue | Gordin |
| Atlas, LootTracker, RunecraftHelper, SekhemaHelper | yokkenUA |
| RitualHelper | [MordWraith/RitualHelper](https://github.com/MordWraith/RitualHelper) (port of caio / AutoRitualPricer) |
| Autopot | [MordWraith/Autopot](https://github.com/MordWraith/Autopot) |
| PlayerBuffBar, Hiveblood, AuraTracker, SimpleBars, AmanamuVoidAlert | MordWraith forks (see `scripts\plugins-sources.json`) |

**Note:** LootTracker and LootValue can run together; you may see overlapping loot overlays.

**Not included:** FarmTracker, Gordin Atlas, Experimental plugin store.

## Quick start (developers)

```powershell
cd "D:\Gamehelper Core"
maintain.cmd              # GUI (CMD schliesst sich sofort)
maintain-gui.vbs          # GUI ohne CMD-Flackern (Doppelklick-Shortcut)
maintain.cmd -Console     # Textmenue (CMD bleibt offen)
```

Or manually:

```powershell
dotnet build GameOverlay.sln -c Release
.\scripts\build.ps1 -Configuration Release
.\publish\GameHelper.exe
```

## Maintainer scripts

| Entry | Purpose |
|-------|---------|
| **`maintain.cmd`** | **All-in-one hub** — build, Gordin sync, plugin sync/push, publish, git, workflow guide |
| `scripts\bootstrap.ps1` | One-time (re-runnable) full tree assembly |
| `scripts\sync-plugin-repos.ps1` | Clone/pull MordWraith + upstream plugin repos (`plugins-sources.json`) |
| `sync-plugin-repos.bat` / `pull-external-plugins.bat` | Shortcut: update all plugin git clones |
| `scripts\sync-gordin.ps1` | Pull Gordin core/offsets and optional Gordin plugins |
| `scripts\build.ps1` | Build solution and deploy to `publish\` |
| `scripts\publish.ps1` | Signed GitHub release (after `github.config.json` is set) |

## First install vs updates

- **First install:** Full ZIP from GitHub release (no SDK required).
- **Core update:** Signed `manifest.json` ZIP, opt-in in launcher.
- **Plugin update:** `scripts\sync-plugin-repos.ps1` (git pull per plugin repo) → `scripts\build.ps1` (.NET SDK required)

## License

See upstream repositories and `CREDITS.md`.
