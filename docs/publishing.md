# Publishing Hand Quickbelts

Thunderstore package versions are immutable. Confirm the package name, team, version, README, and links before submitting each release.

## Build the package

From the repository root:

```powershell
.\tools\package.ps1
```

The script builds Release, validates the manifest, icon, DLL version, and archive contents, then writes a versioned ZIP to `dist`.

## One-time GitHub setup

1. Create a public repository named `hqb` under the `quaternion7` GitHub account. Do not initialize it with generated files.
2. Connect and publish this local repository:

```powershell
git remote add origin https://github.com/quaternion7/hqb.git
git push -u origin main
```

## First Thunderstore upload

1. Sign in to [Thunderstore](https://thunderstore.io/) with GitHub, Discord, or Overwolf.
2. Open [Team settings](https://thunderstore.io/settings/teams/) and create or select the `quaternion` team.
3. If `quaternion` is unavailable, stop before uploading. The team becomes part of the package ID and the README link must be updated.
4. Optionally check `README.md` in the [Markdown preview](https://new.thunderstore.io/tools/markdown-preview/) and `manifest.json` in the [manifest validator](https://thunderstore.io/tools/manifest-v1-validator/).
5. Open [Upload package](https://thunderstore.io/package/create/) and select the versioned ZIP from `dist`.
6. Select team `quaternion`.
7. Select community `H3VR`.
8. Select categories `Mods` and `Tweaks`.
9. Leave `Contains NSFW content` set to `No`.
10. Review the generated package card, then submit.

The first public package ID should be `quaternion-Hand_Quickbelts`, and its page should be:

`https://thunderstore.io/c/h3vr/p/quaternion/Hand_Quickbelts/`

After publishing, install the public package into a clean r2modman profile and verify startup, interaction, live configuration, miniaturization, and scene transitions.

## Publishing updates

1. Increment the version in `plugin/HQB.csproj`, `manifest.json`, and `mm_v2_manifest.json`.
2. Add the user-facing changes to `CHANGELOG.md`.
3. Build and test locally.
4. Run `tools/package.ps1`.
5. Upload the new ZIP under the same Thunderstore team and H3VR community.

Even README-only changes require a new version after the first upload. A demo-video link added after `0.4.2` therefore needs a later package version.

For later automation, create a Thunderstore service token in the team settings and store it as a secret. Never commit the token to this repository.
