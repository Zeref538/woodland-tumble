# Unity 6000.5.10f1: Mac lab offline setup

## Exact version

Install **Unity 6000.5.10f1**, revision **3bd4f66ad299**. This matches `ProjectSettings/ProjectVersion.txt`. Packages include Input System 1.19.0 and URP 17.6.0.

The lab's 6000.2.2f1 is older. Opening this project in that editor is an unverified downgrade. Keep that existing installation and add 6000.5.10f1 separately, with lab permission.

## What can work without Wi-Fi

The Editor installer can run from USB. Downloading it needs internet beforehand. A fresh Unity Personal activation requires Hub and internet on the target Mac; copying an installer does not activate a licence. Ask the lab staff whether the Mac already has an appropriate active licence. Do not copy another computer's licence files.

The project also needs its package dependencies. An empty package cache can still cause downloads even with the Editor installed. Prepare the target Mac while connected once, or have the lab technician supply its required package cache. A Windows Library folder is not a substitute for a Mac import.

## Download before going to the lab

1. On the target Mac, open **Apple menu > About This Mac**.
2. Read **Chip** or **Processor** to identify Apple Silicon or Intel.
3. Download the matching **6000.5.10f1 Unity.pkg** below on an internet-connected computer.
4. Copy that installer onto a USB drive.
5. Copy the complete `TiltRivals` project folder onto the USB drive.

Official installers, HTTP 200 checked on 2026-10-18:

- [Apple Silicon, M-series Macs](https://download.unity3d.com/download_unity/3bd4f66ad299/MacEditorInstallerArm64/Unity.pkg)
- [Intel Macs](https://download.unity3d.com/download_unity/3bd4f66ad299/MacEditorInstaller/Unity.pkg)
- [Release page](https://unity.com/releases/editor/whats-new/6000.5.10f1)

You can download either Mac installer from Windows. The installed Windows Editor itself cannot run on macOS. The installers are not included in this repository or the project copy.

## Install locally at the lab

1. Copy `TiltRivals` from USB to the Mac's Desktop.
2. Open the matching **Unity.pkg** from USB.
3. Follow the installer prompts using the lab's approved installation location.
4. In Unity Hub **Installs**, use **Locate** to register the installed **Unity.app**.
5. In Hub **Projects**, use **Add project from disk** for `Desktop/TiltRivals`.

Installation can require an administrator password. Ask the lab staff if prompted. If Hub is missing, download its Mac installer beforehand too. Do not overwrite the existing lab Editor. Use the actual installation path chosen by the installer when locating Unity.app.

The base Editor is enough for editing and Play Mode. Exporting to Windows, Android or another target requires the corresponding build-support installer for this exact Editor and host Mac. Bring those components beforehand if required.

## Prepare packages and prove offline use

1. While connected, activate the target Mac's licence through Unity Hub if needed.
2. Open this project in **6000.5.10f1** on that Mac.
3. Wait until package resolution and asset import finish without errors.
4. Close Unity and disconnect Wi-Fi.
5. Reopen the project and test **Play**.

If package resolution fails offline, restore internet to complete it before the lesson. Unity's default Mac registry cache is `~/Library/Caches/Unity/upm`. Keep that cache for the same Mac user. Do not delete it before class. Do not change manifest or lock-file package versions during offline work. A technician may stage an exact matching cache from an already prepared Mac, but that transfer has not been tested here.

The project copy contains `Assets`, `Packages`, `ProjectSettings` and documentation. It omits `Library`, `Temp`, `Logs`, compiled builds and the Unity application. Unity rebuilds its local cache; your C# source is already in `Assets/Scripts`. A USB source copy has no Git history. If editing that copy, keep a backup and transfer changed source files back into a Git clone before committing.

## Official references and limits

Checked 2026-10-18. Windows gameplay/build checks passed; installation and offline opening on a physical Mac have not been tested.

- [Install using downloaded installers](https://docs.unity3d.com/6000.0/Documentation/Manual/install-unity-with-installers.html)
- [Unity package cache and offline reuse](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-cache.html)
- [Licence activation methods and Unity Personal requirements](https://docs.unity.com/en-us/engine/6000.0/manual/get-started/install-and-upgrade/licenses-and-activation/license-activation-methods)

See `HANDOFF.md` for controls, project files and saving lab edits.
