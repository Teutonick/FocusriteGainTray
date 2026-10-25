# Rights and dependency notices

Project-authored source and documentation: MIT, see LICENSE. The existing contributor copyright notice is retained; contributors remain responsible for rights to their submissions.

Focusrite / Scarlett / Focusrite Control 2 are third-party product names used to identify compatibility. The project is independent, unaffiliated and not endorsed. No trademark rights or vendor software licenses are granted by MIT. Internal namespace/file names FocusriteGainTray are identifiers, not proof of vendor authorship. No vendor audio, firmware, drivers or FC2 installers are included. The user-supplied FC2 setup screenshots are documented below.

Direct dependencies declared by the application:

| Package | Version | Distribution boundary |
| --- | --- | --- |
| Microsoft.Windows.CsWinRT | 2.2.0 | MIT (verified package LICENSE); preserve Microsoft copyright/license and applicable notices |
| Windows.Devices.Midi2 | 0.99.92-preview.11 | NuSpec declares MIT; additional Preview 11 go-live conditions are published by Microsoft; review both before distributing API binaries |

.NET, Windows Forms, Windows SDK, Windows MIDI Services and Focusrite Control 2 retain their own licenses. Source availability does not authorize bundling these components. No binary redistribution approval is claimed here. Restore and Release build succeeded using the official release asset. Package metadata was inspected locally for both direct dependencies; build/toolchain components retain their own notices. This is not a legal certification of every future published binary.

Sources checked 2026-10-25:

- [Microsoft Preview 11 release / distribution conditions](https://github.com/microsoft/MIDI/releases/tag/inbox-preview-11)
- [CsWinRT source and license](https://github.com/microsoft/CsWinRT)
- [Pinned MIDI NuGet release asset](https://github.com/microsoft/MIDI/releases/download/inbox-preview-11/Windows.Devices.Midi2.0.99.92-preview.11.nupkg)
- [Focusrite MIDI documentation](https://support.focusrite.com/hc/en-gb/articles/38789425032594-Controlling-Focusrite-Control-2-via-MIDI)
- [Focusrite website terms](https://focusrite.com/terms-conditions)

This is a technical source-tree review, not a trademark clearance search or legal opinion. It cannot certify worldwide naming freedom, authorship of pre-existing code, or compliance of future binary releases. For a public commercial brand, consider a neutral product name and jurisdiction-specific trademark review.

## Microsoft Preview go-live conditions

Preview 11 permits shipping the API DLL, WinMD and PRI under its go-live agreement. A distributor must provide a way to update/remove app-local API binaries when the API ships in Windows, keep up with preview fixes as required, disclose preview usage, target Windows 11 25H2 or later, report encountered bugs and help debug them, and encourage users to update to the in-box API release. The PRI accompanies the DLL. Read the complete linked agreement and package license before a binary release. FocGrain setup supports manual version upgrades and uninstall, including removal of app-local Preview API files. There is no automatic updater. Distributors must maintain future releases and inform their users; shipping this installer alone does not discharge the ongoing go-live obligations.

## Installer / self-contained payload

FocGrain setup is built using Inno Setup 6.7.3. Its compiler/runtime have their own license and commercial-use terms; project MIT does not override them. The installer preserves its built-in attribution, and the build includes the supplied Inno license. The self-contained .NET runtime and CsWinRT notices are copied from the restored packages into the installed third-party folder. Windows SDK build-time reference packages are not bundled as installers.

Focusrite Control 2/driver and Microsoft Visual C++ Redistributable installers are not bundled or relicensed. On an explicit user choice, setup downloads them from vendor-hosted addresses, checks the reviewed SHA-256 and opens the vendor installer. The original vendor licenses govern those components. No Windows MIDI Services Tools or Transports installers are redistributed. System MIDI components come through Windows Update.

## Original FocGrain visual assets

The gain-dial SVG/PNG, state ICO files and installer banner under assets/ are original project artwork covered by the project MIT license. scripts/Generate-Icons.ps1 generates the Windows assets from code-native geometry. The FocGrain mark contains no Focusrite logo or product artwork; no font files are bundled. The separate FC2 setup screenshots are listed below.

## User-supplied Focusrite Control 2 screenshots

assets/setup/fc2-preferences.png and fc2-inputs.png were supplied by the user specifically for the setup wizard/offline guide. They depict third-party FC2 UI and marks; the project does not claim copyright in that UI or trademark rights. Project MIT does not relicense the vendor UI. See assets/setup/README.md. The screenshots are included unchanged solely as configuration illustrations; their inclusion does not imply Focusrite endorsement.
