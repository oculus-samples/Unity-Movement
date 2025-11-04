# Unity-Movement
Unity-Movement is a package that uses OpenXR’s tracking layer APIs to expose Body Tracking (BT), Eye Tracking (ET), and Face Tracking (FT) capabilities. With this package, developers can leverage tracking to populate VR environments with custom avatars that bring the expressiveness of users into the virtual environments that they create.

### License
The Unity-Movement package is released under the [Oculus License](https://github.com/oculus-samples/Unity-Movement/blob/main/LICENSE.md). The MIT License applies to only certain, clearly marked documents. If an individual file does not indicate which license it is subject to, then the Oculus License applies.

### Requirements
- Unity 2022.3.15f1 or newer.
- v81.0 or newer of the Meta XR SDK. You will need the [Meta XR Core SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-core-sdk-269169) and the [Meta XR Interaction SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-interaction-sdk-265014) packages found [on this page](https://assetstore.unity.com/publishers/25353).
- A project set up with these [steps](https://developer.oculus.com/documentation/unity/move-overview/#unity-project-setup).

## Getting Started
First, ensure that all of the [requirements](#requirements) are met.

Then, bring this package into the project.
- In Package Manager, click on the add button below the window title and select **Add package from git URL…**, using this URL: https://github.com/oculus-samples/Unity-Movement.git
- To grab a specific version of the package, append the version number with a # to the git URL (i.e. https://github.com/oculus-samples/Unity-Movement.git#v74.0.0)
- Alternatively, in package manager, click on the add button below the window title and select **Add package from disk...**, using the package.json located after unzipping one of the releases here: https://github.com/oculus-samples/Unity-Movement/releases

The sample scenes are located under the **Samples~/Scenes** folder. For detailed information, please visit the [related page](https://developers.meta.com/horizon/documentation/unity/move-unity-getting-started).

## Unity Setup

If the new scene or an existing scene doesn’t have a GameObject with the OVRCameraRig component, integrate body tracking as mentioned [here](https://developer.oculus.com/documentation/unity/move-body-tracking/#integrate-body-tracking) and then follow these verification steps:
1. From the Hierarchy tab, look for a Main Camera GameObject which comes in a new scene by default. If it it exists, then please delete it.
2. Make sure a Camera Rig is in your scene, either by using the OVRCameraRig or by creating one using Building Blocks. To create a Camera Rig using Building Blocks, go to **Meta->Tools->Building Blocks** and select the (+) icon on the lower right of the Camera Rig option.
3. Select the Camera Rig object in the Hierarchy, and in the Inspector tab, go to the OVRManager component and look for the "Quest Features" section.
4. In the General tab, there are options to enable body, face, and eye tracking support. Make sure that Supported or Required is enabled for the type of tracking that you require.
5. Under OVRManager's "Permission Requests On Startup" section, verify that Body, Face and Eye Tracking are enabled.
6. Ensure that OVRManager's "Tracking Origin Type" is set to "Floor Level".
7. In OVRManager's "Movement Tracking" verify that "High" for "Body Tracking Fidelity" is selected.
8. In OVRManager's "Movement Tracking" verify that "Full Body" for "Body Tracking Joint Set" is selected.

Layer index 10, layer index 11, and the HiddenMesh layer must be present in the project for RecalculateNormals to work correctly.

You can validate project settings by navigating to **Meta->Tools->Project Setup Tool**.

## Rendering Quality
Navigate to your Project Settings (**Edit->Project Settings...**) and click on
the "Quality" section. If your project uses URP,
then some of these settings might be part the rendering pipeline asset currently
in use. The pipeline picked will be shown in the Quality menu.

The following settings are recommended:
1. Four bones for Skin Weights.
2. 2x or 4x Multi Sampling Anti Aliasing.
3. Full resolution textures.
4. Shadow settings:
    - Hard and soft shadows.
    - Very high shadow resolution.
    - Stable fit.
    - Shadow distance of 3 meters with cascades. This will allow viewing shadows
nearby without experiencing poor quality.
5. At least one pixel light.

## Samples

The project contains several sample scenes. To test the samples, they must be imported into the project's Assets folder:
- Select the "Meta XR Movement SDK" package in the package manager. Once selected, expand the Samples section and import the desired sample scenes.
<br>


For more information about body tracking, please refer to [this page](https://developer.oculus.com/documentation/unity/move-body-tracking/).

For more information about the samples, please refer to the [body samples page](https://developers.meta.com/horizon/documentation/unity/body-tracking-samples) and [face samples page](https://developers.meta.com/horizon/documentation/unity/face-tracking-samples).

## Build Settings

In order for the SceneSelectMenu buttons to work, add imported scenes in the [Samples](#samples) step to the Build Settings.

## Documentation
The documentation for this package can be found [here](https://developer.oculus.com/documentation/unity/move-overview/).
The API reference for this package can be found [here](https://oculus-samples.github.io/Unity-Movement/).

## License
Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved. Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at https://developer.oculus.com/licenses/oculussdk/

Files from [Unity](https://unity.com/legal/licenses/unity-companion-license) and [SchemingDeveloper](https://github.com/oculus-samples/Unity-Movement/blob/main/Runtime/Tracking/ThirdParty/SchemingDeveloper/LICENSE.txt) are licensed under their respective licensing terms.
