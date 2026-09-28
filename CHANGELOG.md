## [207.0.0]

## What's New
- **Gaze and pinch in samples**: All sample scenes now support eye gaze and pinch to interact with UI buttons, in addition to poke interaction.

## What's Changed
- Renamed the "Calibrate Height" button in the body tracking calibration menu to "Override Height to X.XXm" to clarify that it overrides the automatically estimated user height with a fixed value.
- Regenerated the ARKit blendshape mapping example in the face tracking samples using the default retargeting setup.

## What's Fixed
- Fixed finger dislocation during retargeting. Hand joints are now rigid, so each child joint starts where its parent joint ends.
- Fixed unstable hand scale during full retargeting with `UseTPoseForJointScale`. Hand and wrist scale now come from the target T-pose, and finger bone lengths match the target rig proportions, which removes thick or elongated fingers.
- Fixed unstable head scale during full retargeting with `UseTPoseForJointScale` while preserving eye and facial joint transforms.
- Fixed the reference T-pose set with `UpdateSourceReferenceTPose` being discarded on the next retargeting frame, which could cause overlapping or twisted fingers with upper body tracking.
- Fixed Interaction SDK hand integration: `IHand` joint poses are now treated as world-space poses, body joints are preserved when hand data is unavailable, and wrist displacement limiting no longer distorts finger spacing.

## [205.0.0]

## What's Fixed
- Fixed a compilation error in the Movement SDK caused by the removal of `OVRConfigurationTaskException` from the Meta XR Core SDK.

## [203.0.0]

## What's New
- **Retargeting C# API**: New public `RunDefaultRetargetingSetup` C# API that runs the character retargeting setup programmatically. Previously the retargeting tool was only accessible through the custom Editor UI; it can now be driven from code.
- **T-Pose debug drawing**: Added T-pose debug drawing for both the source and target skeletons, making it easier to visualize and debug retargeting.

## What's Changed
- Disabled the T-pose debug buttons in the networking sample scene.

## What's Fixed
- Fixed: building project with Unity 6000.4 results in the "Namespace 'com.oculus.Integration' is used in multiple modules and/or libraries: :MetaMovementSDK_AIMotionSynthesizer" compilation error.

## [201.0.0]

## What's New
- We're standardizing our OS versioning to provide more clarity on the scope and impact of each release. This helps you plan your development by making critical updates predictable and support documentation easier to navigate. Going forward, you'll see this new versioning system (e.g. OS 200, 201, etc.).
- **Tracking space support**: Movement SDK now works correctly when `OVRCameraRig.trackingSpace` has a non-identity pose. Previously, the body-tracking avatar would detach from the user's body when used with features like MRUK World Locking.
- **RootMotionMode support**: New `RootMotionMode` enum (`WorldSpace`/`LocalSpace`) in C# bindings, with root pose extraction via `GetPoseByRef` and root motion application option on `AIMotionSynthesizerSourceDataProvider`. Includes debug skeleton visualization.
- **Networked root scale**: Root scale is now serialized as part of the snapshot header, enabling correct character scale in networked sessions without manual C# setup.
- **Cross-engine config compatibility**: Added a skeleton flag and coordinate space conversion fixes so retargeting configs created in Unity work in Unreal (and vice versa). Existing configs continue to work on their original engine; new configs are cross-engine compatible.
- **Improved auto-mapping**: KnownJoint detection algorithm now correctly identifies joints on previously failing rigs (Unreal Mannequin, Avatar Gen Mas). Auto Fill is now ~1-click for ~95% of character rigs. New reverse mapping utility lets configs daisy-chain rig→rig retargeting.

## What's Changed
- **Minimum Unity version raised from 2022.3 to Unity 6.**
- **Joystick input improvements**: Reference forward calculation now uses `ProjectOnPlane`; removed automatic `OVRCameraRig` dependency (uses component's transform as default); direction logic considers movement direction when look input is inactive.
- **Simplified C# serialization API**: Removed unused `createSnapshot` entry point. All C# serialization calls now use struct-based APIs.

## What's Fixed
- **Retargeter no longer overwrites eye bone transforms**: `CharacterRetargeter` now skips joints that have no mapping in the source body-tracking skeleton, allowing components like `OVREyeGaze` to control eyes without being clobbered each frame.
- **`AnimatorHorizontalParam` fix** (resolves [Unity-Movement#133](https://github.com/oculus-samples/Unity-Movement/issues/133)).
- **Memory leaks** caused by undisposed `Allocator.TempJob` allocations in `GetTrackerPosesAndIndices` and `SkeletonUtilities.GetPosesFromTheTracker`, plus a leak of `TargetReferencePoseLocal` (`Allocator.Persistent`) in `SkeletonRetargeter.Dispose`.
- **`RootMotionMode` enum order restored** so Unity serialized values and dropdown entries match prior behavior.
- **Unmapped joints with zero-length parents** no longer flicker/oscillate in debug draw; falls back to scale 1.0.

## [83.0.0]

## What's New
- **AI Motion Synthesizer:** Added AI Motion Synthesizer to the Movement SDK, which uses AI to generate natural, full-body character motion from sparse input signals. This feature supports joystick input with body tracking, synthesized standing poses that respect body tracking proportions for seated users, and smooth pose blending. Can be enabled on the MetaSourceDataProvider for any character using the retargeter with body tracking over PC Link and Quest devices.
- **Networking Data Stream**: Added new networking data stream classes that replace RPC-based synchronization with a more efficient system supporting larger payloads. The MovementNetworking scene now includes a realistic character demonstrating face blendshape synchronization over the network.
- **Mesh Draw Utility:** Added a new debugging utility for drawing meshes, primitive shapes, and skeletons at editor and runtime. Includes OVRSkeletonDraw support for visualizing the OVR skeleton from any code.
- **ISDK Locomotion Sample Update:** Updated the ISDK Locomotion sample to use the latest ISDK sample scene from v83.
- **Building Blocks Support:** Support v83 Meta XR Core SDK building blocks (CharacterRetargeter and NetworkCharacterRetargeter).

## What's Fixed
- Fixed networking character initialization order and delta compression setup.
- Fixed crash when using the "Zero Root" retargeting behavior mode with native retargeting.
- Fixed Hand Auto-Alignment not accounting for metacarpals, which can cause misaligned hands for some characters.
- Fixed T-Pose fixup function to filter outliers.
- Fixed CharacterRetargeter dispose handling for in-progress jobs.
- Fixed RecalculateNormals initialization tracking for Unity 6.
- Fixed face blendshape compression for networking.

## Improvements
- Refactored networking architecture to use NetworkVariables instead of RPCs, reducing bandwidth overhead and simplifying data synchronization flow.
- Added Retargeting Behavior Flags for finer control, including options to use TPose for joint scaling.
- Added support for saving and loading configs in different coordinate space types (RootSpace, Local, LocalScaled).
- Improved skeleton visualization and debugging tools.

## [81.0.0]

## What's New
- **Face tracking retargeting update:** Face tracking scenes use new retargeting pipeline.
- **Twist joints**: Twist joints have been integrated into the tooling UI.

## What's Fixed
- Fixed incorrect use of height estimate with half body tracking.
- Fixed error in retargeter related to taking reciprocal of values near zero.
- Fixed hands getting locked above head in hip pinning scene.
- Fixed fitness sample debug draw skeleton.
- Fixed numerous retargeting tooling errors and support for Unity 6.
- Prevent constant refreshing and saving of fitness body pose transforms when no changes have been made.
- Fixed bug where the debug draw skeleton was using the source skeleton used for retargeting.
- Fixed hip pinning processor from going into the ground due to varying leg lengths.

## Improvements
- Improved general tooling flow and character bone alignment.

## [78.0.0]

## What's New

- **Visemes:** Added support for Visemes.
- **Telemetry:** Added telemetry for Movement SDK features and tooling.
- **Build Script:** Added build samples menu option (Meta -> Samples -> Build Movement SDK Samples) to easily build imported samples.
- **Asset Naming:** Updated the realistic body tracking character name from High Fidelity to Realistic.
- **Retargeting Motions:** Added eight new common motion sequences to the retargeting editor for previewing runtime retargeting with body tracking

## What's Fixed

- Fixed retargeting editor issues related with configuration creation, previewing, and editing.
- Fixed ISDK hand weights not being applied correctly.
- Fixed ISDK processor hand component search for latest hands.

## Improvements

- **Debug Draw:** Invalid retargeting due to invalid body tracking is now drawn in red when debug draw is enabled.
- **Networking Packet Size:** Reduced average networking packet size by ~25%.
- **Networking Compression:** Improved networking compression accuracy.
- **Retargeting Known Joint Detection:** Improved known joint detection and naming for retargeting.
- **Retargeting Hand Alignment:** Improved hand alignment mapping algorithm.

## [77.0.2]

### What's Fixed

- Fixed retargeting editor issues related with configuration creation, previewing, and editing.

## [77.0.1]

### What's Fixed

- Fixed animation quaternion blending edge cases.
- Fixed various retargeting editor issues related to posing, playback, and scaling.

## [77.0.0]

### What's New

- **Upper Body Support**: Implemented upper body support for the CharacterRetargeter with configurable leg scaling options on full body characters.
- **Hand IK Processor**: Added HandIKProcessor to the CharacterRetargeter to support custom hands.
- **Retargeting Configuration Generation**: Improved automatic mapping, alignment, joint recognition and more for the retargeting configuration generation.
- **Automatic Twist Joint Mapping**: Added automatic twist joint mapping for arms & legs for skeletal retargeting, removing the need for Twist Joint Processors.

### What's Fixed

- Fixed quaternion calculation errors.
- Fixed out of view hands handling.
- Fixed incorrect skeleton visualization for some samples.
- Fixed various invalid configuration retargeting errors and erroneous joint mapping.
- Fixed sample UI inconsistent behavior.

### Improvements

- **Performance**: Optimized native retargeting resulting in a 25% performance improvement.
- **Retargeting Editor**: Updated UI and improved editor flow.

## [76.0.1]

### What's New

- **Platform Support**: Mac and Linux libraries.

### What's Fixed

- Fixed hip pinning scene target to use tracked anchors to prevent tracking invalid hands.
- Fixed CCD stretching arm stretching bug.
- Fixed potential race condition in NetworkCharacterBehaviorNGO.
- Fixed memory leaks by properly disposing TempJob arrays.
- Fixed project setup tool requirements for all platforms.

## [76.0.0]

### What's New

- **Replaced retargeting system**: Implemented a more efficient, data-driven CharacterRetargeter.
- **Added tooling system**: Created powerful and easy-to-use tooling system to create retargeted characters.

### Improvements

- **Revised sample scenes**: Updated body tracking, networking, ISDK, hip pinning and locomotion scenes to use new retargeting system.

## [74.0.0]

### What's Fixed

- Fixed MovementBodyTrackingForFitness scene so that the UI in the scene is visible to cameras.
- Fixed bug in VisemeDriver where it was returning if the OVRFaceExpressions reference wasn't null.
- Fixed BlendshapeMappingExample's body mesh animation to work based on body tracking movements when not visible.

### Improvements

- Updated Project Setup Tool to enable audio-based face tracking when using A2E, and enable face tracking visemes output when using visemes.

## [72.0.0]

### What's New

- **Added viseme support**: Implemented `VisemeDriver.cs` to allow using visemes.

### What's Fixed

- Fixed readme, documentation links and UI path.

### Improvements

- **Updated hand tracking**: Sample scenes now use OpenXRHands.
- **Improved normal recalculation**: Changed Normal recalculator to run independently by default.

## [71.0.0]

### What's New

- **New networking sample**: Added efficient body movement networking with compression, compatible with common network providers.
- **New A2E sample**: Implemented Audio To Expressions (A2E) Lina sample.

### What's Fixed

- Fixed normal recalculator to run on submeshes with index greater than zero.

### Improvements

- **Enhanced character support**: BlendshapeMapping (ARKit) character and other characters modified to support A2E.

## [69.0.0]

### What's New

- **Added HMD remount feature**: Implemented restart tracking feature via HMDRemountRestartTracking.cs
- **Added build menu**: Created menu action to build all available samples (Movement/Build Samples APK)

### What's Fixed

- Fixed bug in ISDK scene where controllers were not able to be used.
- Fixed FaceExpressionModifierDrawer call to GUI properties on constructor.
- Fixed duplicate face quick action by removing unused instance.

### Improvements

- **Improved debug visualization**: Enhanced performance and removed garbage creation when drawing the debug skeleton
