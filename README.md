# Call Center Chaos — iOS (Unity prototype)

This repository is for an **independent Unity prototype**, not the original commercial game and not a finished iPhone IPA.

## Prepare the source

The project archive is **not yet present in this repository**. From your iPhone, open **Add file → Upload files** and upload `CallCenterChaos_iOS_IPA_BuildReady.zip` into the repository root. **Do not rename it.** This archive has a top-level `CallCenterChaos_iOS_BuildReady/` folder which the CI workflow extracts automatically. Keep your API keys out of Git.

## Build with GitHub Actions

1. Add an activated Unity licence using **Settings → Secrets and variables → Actions**. Set `UNITY_LICENSE` for a licence-file flow, or the Unity account/serial secrets that apply to your Unity licence. Do not commit credentials.
2. Open **Actions → Build iOS IPA from Unity archive → Run workflow**.
3. The first job exports a Unity iOS Xcode project; the second job uses macOS/Xcode to attempt an **unsigned IPA**.
4. If green, download **CallCenterChaos-UNSIGNED-IPA** under Artifacts. It is **not installable on an ordinary iPhone until signed** with an appropriate Apple provisioning profile/certificate.

Workflow errors should be reviewed before claiming an IPA is ready; the Unity prototype has not yet been successfully compiled/tested on iPhone.

### Features currently prototyped

- 144 illustrated caller portraits and Arabic-language profiles/conversations
- Generated 3D office with five employees and a manager
- Native iOS speech recognition and text-to-speech bridge
- AI chat client and a separate Node server (requires your own HTTPS deployment and server-side OpenAI API configuration)
- This is **not** the commercial game, its original 3D models, or completed multiplayer.

### Privacy

This GitHub repository was reported **public** on creation. To make it private, go to Settings → General → Danger Zone → Change repository visibility. Never upload API keys or certificates.

See `IOS_IPA_BUILD_AR.md` within the archive for full Arabic instructions.
