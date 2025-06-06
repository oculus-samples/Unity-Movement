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
