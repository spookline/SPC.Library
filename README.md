# Spookline Core (SPC) Library

A collection of core utilities and systems for Unity development.

## Packages

###  Spookline Core (`com.spookline.spc`)
The base library containing essential utilities, extension methods, and core systems.
- **Dependencies:** Addressables, Input System, Newtonsoft JSON, Cinemachine, HELIX, UniTask

###  Spookline Core FishNet (`com.spookline.spc.fishnet`)
Networking extensions for Spookline Core using the [FishNet](https://fish-networking.com/) networking library.
- **Dependencies:** Spookline Core, FishNet.

---

## Installation

You can install these packages via the Unity Package Manager (UPM) using "Add package from git URL...".

### 1. Spookline Core (Required)
```
https://github.com/spookline/SPC.Library.git?path=SPC/
```

### 2. Spookline Core FishNet (Optional)
```
https://github.com/spookline/SPC.Library.git?path=SPC.FishNet/
```

---

## Features

### Core Systems (`SPC`)
- **Actor:** Base classes for game entities and actors (`Pawn`, `IMovementStateAttachment`).
- **Animation:** Animator helpers and management (e.g., `SpookAnimator`).
- **Audio:** Definitions and handles for flexible audio management.
- **Cameras:** Cinemachine extensions and camera utilities.
- **Events:** Lightweight event management system (`EventManager`, `Evt`).
- **Geometry:** Advanced geometric utilities (`OrientedBox`, `Frustum6`, `SpacialHash`).
- **Input:** Input System abstractions and helpers.
- **Save System:** Player data extensions and serialization utilities.
- **Console & Debugging:** In-game console support and global debugging helpers.
- **Extensions:** A wide range of extension methods for `GameObject`, `Transform`, etc.

### Modules (`SPC/Runtime/Modules`)
- **Cleaver:** A sophisticated environment management and optimization system.
    - **Environment & Sections:** Manage large worlds via partitioned sections.
    - **Portals & Proxies:** Optimization techniques for visibility and performance.
    - **Debug Tools:** Comprehensive editor and runtime overlays for spatial debugging.
- **UI:** Enhanced UI systems and components.
    - **Spook Console:** A powerful in-game command console with custom command support (`CommandSystem`).
    - **Bellevue USS:** Shared UI Toolkit styles.

### Networking (`SPC.FishNet`)
- **SpookNetworkBehaviour:** Base class for networked objects.
- **SpookManagerNetworkBehaviour:** Singleton pattern for networked managers.
- **Network Synchronization:** Simplified state and event syncing using FishNet.