# Security & trust

How installs and updates work, so you can decide whether to use pre-built releases or build from source.

## Distribution

| Channel | What you get |
|---------|----------------|
| [GitHub `main`](https://github.com/MordWraith/Gamehelper-Core/tree/main) | Application source |
| [GitHub Releases](https://github.com/MordWraith/Gamehelper-Core/releases) | Pre-built binaries (downloader, full ZIP) |
| In-app auto-update | Same signed release ZIP as on GitHub Releases |

Auto-update is **optional**. Manual install from the full ZIP works the same.

## Auto-update

1. The launcher downloads **`manifest.json`** and **`manifest.sig`** from GitHub Releases.
2. The signature is checked with an **RSA public key embedded in the launcher**.
3. The update is delivered as **one ZIP**; its hash must match the signed manifest.
4. User data is **not** included in update packages:
   - `configs/` (core settings, `plugins.json`)
   - `Plugins/*/config/` (per-plugin settings)

You still trust the **maintainer** who signs releases — the same model as any pre-built game overlay that is not independently audited.

## Build from source

Clone this repository, open `GameOverlay.sln`, **Rebuild Solution** (Release), and run `GameHelper.exe` from the build output. No downloader or auto-update required.

## Windows Defender and antivirus (false positives)

Game overlays that **download**, **extract**, and **load DLLs** are often flagged. That is common for tools like GameHelper and does **not** necessarily mean malware.

### Typical detection names

| Name | Typical trigger |
|------|-----------------|
| `Trojan:Win32/Wacatac.C!ml` | `GameHelper.App.dll` — overlay + memory reads |
| `Trojan:Win32/PowhidSubExec.B` | Auto-update — copies from `%TEMP%\GameHelperUpdate\` into your install folder |

Other vendors may use different names for the same behavior.

### If GameHelper will not start

1. Open **Windows Security → Protection history**, find blocked entries for `GameHelper.exe`, `GameHelper.App.dll`, or `GameHelperUpdate`.
2. **Allow** / **Restore**, or add a **folder exclusion** for your install path.
3. Do not mix files from different versions.
4. **Clean install:** download the [full ZIP](https://github.com/MordWraith/Gamehelper-Core/releases/latest), extract to a **new empty folder**, run `GameHelper.exe`.

### Why auto-update triggers AV heuristics

When you approve an update, the launcher downloads a signed ZIP to `%TEMP%\GameHelperUpdate\`, then uses a small background copy step after the launcher exits. That pattern matches generic “dropper” heuristics. The same files are on GitHub Releases with SHA256 checks in the signed manifest.

### Release integrity

- `manifest.json` is **RSA-signed**; tampered manifests are rejected.
- The package hash in the manifest must match the downloaded ZIP.

You still trust whoever holds the signing key.

## Reporting

Security or trust questions: [GitHub Issues](https://github.com/MordWraith/Gamehelper-Core/issues).

Please include `VERSION.txt`, install method (downloader / ZIP / self-built), and whether auto-update was used.
