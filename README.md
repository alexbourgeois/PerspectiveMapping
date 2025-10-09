# Perspective Mapping in Unity

## ✨ Overview

**Perspective Mapping** is a Unity plugin that enables real-time perspective deformation of a camera’s output by adjusting the **four corners** of the rendered image.
It is particularly useful for **video projection mapping**, as it eliminates the need for external tools such as **Resolume** or similar software.

Key features include:

* Independent control of each corner and the output center
* Mouse and keyboard interaction for flexible adjustments
* Automatic configuration saving to JSON files
* Full compatibility with **Unity URP** (version 14.0 or higher) via a **Renderer Feature**
* Support both Legacy Input system and New Input System
* Support for multiple cameras, each with its own configuration stored in: **StreamingAssets/PerspectiveMapping/**

---

## 🕹️ Controls

| Key / Action           | Function                                |
| ---------------------- | --------------------------------------- |
| **P**                  | Enter interactive mapping mode          |
| **Escape**             | Exit mapping mode without saving        |
| **R**                  | Reset corners to their default position |
| **O**                  | Toggle grid overlay for alignment       |
| **1–5**                | Select a corner or the center           |
| **Tab / Shift + Tab**  | Select next / previous point            |
| **Arrow Keys**         | Move the selected point                 |
| **Shift + Arrow Keys** | Fast movement                           |
| **Ctrl + Arrow Keys**  | Precise movement                        |

---

## 🌀 Mapping Invariants

Two mapping invariant modes are available:

* **Corners** *(default)*: corners remain fixed
* **Circle**: circle points remain fixed

This allows for either **square-based** or **circular** projection mapping, depending on the use case.

---

## 📦 Installation

1. Open the **Unity Package Manager**.

2. Add the following **Scoped Registry**:

   ```
   Name:   Tools
   URL:    https://registry.npmjs.com
   Scope:  com.alexbourgeois
   ```

3. Install the package **Perspective Mapping** from the Package Manager.

---

## 🚀 Usage

1. Add the **Renderer Feature** `PerspectiveMappingFeature` to your **Render Pipeline Asset**.
2. Drag and drop the prefab **`PerspectiveCamera`** from the package folder into your scene.
3. Enter **Play Mode**, press "P" to perform the mapping interactively.

---

## 🙏 Acknowledgements

* Developed with [gaheldev](https://github.com/gaheldev)
* Inspired by [cecarlsen](https://github.com/cecarlsen/ViewportPerspective)
