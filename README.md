# RFID-Video Player

An interactive video installation built in **Unity 6 (6000.0.27f1)** that plays different animated videos in response to physical RFID/NFC tags. A visitor taps a tagged object or card on the reader, and the application reads the tag's UID and instantly swaps to the video mapped to that tag — with a fade transition between clips. When the tag is removed, an outro plays and the installation returns to its looping default video.

## About the project

This was a **collaborative project with an animation student, created for his senior capstone**. The animator produced the looping video pieces; this repository contains the Unity application that drives the interactive playback hardware.

The finished installation was **displayed in a campus art gallery**, where gallery visitors could physically interact with the work by tapping tagged objects to trigger different animations. The project went on to take **first place in the capstone competition**. 🥇

## How it works

- **RFID/NFC input** — Uses an **ACR1252** reader (via the [PCSC](https://github.com/danm-de/pcsc-sharp) smart-card library) to detect tags and read their UIDs.
- **UID → video mapping** — Each registered tag UID is mapped to a specific `VideoClip`. Tapping a tag plays its clip on a loop; an unmapped tag is ignored.
- **Transitions** — Inserting or removing a tag triggers a fade overlay animation so clips swap cleanly without a visible flash.
- **Idle state** — With no tag present, a default video loops until the next interaction.
- **Threading** — NFC reader events fire on a background thread, so a `UnityMainThreadDispatcher` marshals them back onto Unity's main thread for safe playback control.

## Project structure

| Path | Purpose |
|------|---------|
| `Assets/NFC_Driver.cs` | Core driver — connects to the ACR1252 reader, monitors for tag insert/remove events, reads UIDs, and controls video playback + transitions. |
| `Assets/mainthread.cs` | `UnityMainThreadDispatcher` — queues background reader callbacks onto Unity's main thread. |
| `Assets/behavior.cs` | Animator `StateMachineBehaviour` hook for the fade transitions. |
| `Assets/*.mp4` | The animated video pieces (tracked with Git LFS). |
| `Assets/Plugins/PCSC.dll` | Smart-card / NFC reader library. |

## Requirements

- **Unity 6** (`6000.0.27f1`)
- An **ACR1252** (or compatible PC/SC) RFID/NFC reader
- Windows with the **Smart Card (PC/SC)** service available
- [Git LFS](https://git-lfs.com/) — the video assets are stored via LFS, so run `git lfs install` before cloning

## Getting started

```bash
git lfs install
git clone https://github.com/Gavin-Ipp/RFID-Video-Player.git
```

1. Open the project in Unity 6 (`6000.0.27f1`).
2. Plug in the ACR1252 reader.
3. In the scene, assign your `VideoPlayer`, the `videoClips` array, the `defaultVideoClip`, and the overlay UI on the `ACR1252Reader` component.
4. Register each tag's UID in `InitializeUidVideoMapping()` in `Assets/NFC_Driver.cs`.
5. Press **Play**. Tap a tag to trigger its video; press **Q** to quit.
