# Perspective mapping

/!\ Only compatible with Unity URP !

Add PerspectiveMappingFeature to your renderer feature

By default the mapping is applied after post-processing, as the last step of the frame. Change `Pass Event` on the feature to apply it earlier.

Add PerspectiveMappingCamera.cs script to your camera

Add PerspectiveMappingUI.cs script to your camera to get UI for easier mapping