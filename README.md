# Unity-Movement
Unity-Movement is a package that uses OpenXR’s tracking layer APIs to expose Meta Quest Pro’s Body Tracking (BT), Eye Tracking (ET), and Face Tracking (FT) capabilities. With this package, developers can leverage tracking to populate VR environments with custom avatars that bring the expressiveness of users into the virtual environments that they create.

### Requirements
- Unity 2020.3.34f1 (2020 LTS) or newer installed
- v46.0 or newer of the Oculus Integration SDK with OVRPlugin set to use OpenXR as backend
- A project set up with these [configuration settings](https://developer.oculus.com/documentation/unity/unity-conf-settings/)

### Licenses
The Unity-Movement package is released under the [Oculus License](https://github.com/oculus-samples/Unity-Movement/blob/main/LICENSE). The MIT License applies to only certain, clearly marked documents. If an individual file does not indicate which license it is subject to, then the Oculus License applies.

## Getting Started
First, ensure that all of the [requirements](#requirements) are met.

Then, bring this package into the project.
- In Package Manager, click on the add button below the window title and select **Add package from git URL…**, using this URL: https://github.com/oculus-samples/Unity-Movement.git
- Alternatively, in package manager, click on the add button below the window title and select **Add package from disk...**, using the package.json located after unzipping one of the releases here: https://github.com/oculus-samples/Unity-Movement/releases

The sample scenes are located under the **Samples/../Scenes** folders. The Character (layer index 10), the MirroredCharacter (layer index 11), and the HiddenMesh layers must be present in the project.

## Samples
The project contains several sample scenes. For more information about the samples, read [Aura Sample](https://developer.oculus.com/documentation/unity/move-sample-aura/), [Hip Pinning Sample](https://developer.oculus.com/documentation/unity/move-sample-hip-pinning/), and [High Fidelity Sample](https://developer.oculus.com/documentation/unity/move-high-fidelity/).

## Documentation
The documentation for this package can be found [here](https://developer.oculus.com/documentation/unity/move-overview/).
The API reference for this package can be found [here](https://oculus-samples.github.io/Unity-Movement/).
