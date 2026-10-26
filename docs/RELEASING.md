# Versions and releases

`Directory.Build.props` is the version source (currently 1.0.4). The EXE file/assembly metadata, UI version labels and default setup filename use it. Build-Setup.ps1 accepts an explicit major.minor.patch override for a deliberate custom build and checks tag/version agreement if run in a tag context. No commit, tag, release or remote publication is performed by the build script.

## Maintainer release procedure

1. Change the version in Directory.Build.props and add the changelog entry.
2. Run model tests, Release publish/setup build and Test-SetupPayload.ps1. Perform the relevant manual/VM/hardware checks; record exclusions.
3. Review/sign the EXE and setup if a signing certificate is available; do not describe unsigned builds as signed.
4. The maintainer creates the matching `vMAJOR.MINOR.PATCH` tag and a GitHub Release in `Teutonick/FocusriteGainTray`, initially as a draft for review. Upload the setup and its SHA-256 sidecar. Publish only after review. The workflow currently builds a downloadable CI artifact on manual dispatch; it does not publish releases or request repository write permission.
5. Keep the installer AppId unchanged for upgrades. Maintain the Preview API go-live obligations and update notices to users when moving to the in-box API.

## In-app check contract

The button in the tray/settings opens a localized dialog, displays the current version and asynchronously requests `https://api.github.com/repos/Teutonick/FocusriteGainTray/releases/latest`. It uses no token or stored account, sends no application logs/settings and performs no request on startup. GitHub will receive the normal network request and FocGrain version in User-Agent when the user requests a check.

Only public, stable, published releases with a `v1.2.3` or `1.2.3` tag are supported. Draft/prerelease/malformed tags are rejected. The parser compares numeric major/minor/patch versions, ignores a local assembly revision and constructs the link inside the fixed repository; an untrusted response cannot substitute an arbitrary website. The app never downloads or runs an update. The user can open the release and manually run its setup.

404 means no published release or an inaccessible/private repository, not proof that the current version is latest. Offline/timeout, malformed JSON, HTTP errors and rate limits remain visible failures. The HTTP timeout is 10 seconds and the response buffer is bounded. The configured API returned HTTP 404 during this review; no release was created.

Tests use fake HTTP responses for newer/equal/older versions, 404, 403, 429, server errors, malicious html_url and invalid/draft/prerelease tags. They do not require network or publish anything.
