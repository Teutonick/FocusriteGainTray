# Rights and dependency notices

Project-authored source and documentation: MIT, see LICENSE. The existing contributor copyright notice is retained; contributors remain responsible for rights to their submissions.

Focusrite / Scarlett / Focusrite Control 2 are third-party product names used to identify compatibility. The project is independent, unaffiliated and not endorsed. No trademark rights or vendor software licenses are granted by MIT. Internal namespace/file names FocusriteGainTray are identifiers, not proof of vendor authorship. No vendor logos, artwork, audio, firmware, drivers or FC2 installers are present in this source tree.

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

Preview 11 permits shipping the API DLL, WinMD and PRI under its go-live agreement. A distributor must provide a way to update/remove app-local API binaries when the API ships in Windows, keep up with preview fixes as required, disclose preview usage, target Windows 11 25H2 or later, report encountered bugs and help debug them, and encourage users to update to the in-box API release. The PRI accompanies the DLL. Read the complete linked agreement and package license before a binary release. This repository does not yet provide an update mechanism or a release installer; publishing source under MIT does not fulfill these binary-distribution obligations.
