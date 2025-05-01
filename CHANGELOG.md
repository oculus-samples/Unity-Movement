## [76.0.0]
- Replaced retargeting system with a more efficient, data-driven CharacterRetargeter.
- Added powerful and easy-to-use tooling system to create retargeted characters.
- Revised body tracking, networking, ISDK, hip pinning and locomotion scenes to use new retargeting system.

## [74.0.0]
- Fixed MovementBodyTrackingForFitness scene so that the UI in the scene is visible to cameras.
- Updated Project Setup Tool to enable audio-based face tracking when using A2E, and enable face tracking visemes output when using visemes.
- Fixed bug in VisemeDriver where it was returning if the OVRFaceExpressions reference wasn't null.
- Updated BlendshapeMappingExample's body mesh, mirrored or not, to animate based on body tracking movements when not visible.

## [72.0.0]
- Updated sample scenes to use OpenXRHands.
- Fixed readme, documentation links and UI path.
- Changed Normal recalculator to run independently by default.
- Added `VisemeDriver.cs` to allow using visemes.

## [71.0.0]
- Normal recalculator has been fixed to run on submeshes with index greater than zero.
- New networking sample shows how to efficiently network body movement by compressing the joint-related movement information for transmission and is compatible with common network providers.
- New Audio To Expressions (A2E) Lina sample, BlendshapeMapping (ARKit) character and other characters modified to support A2E.

## [69.0.0]
- Fixed bug in ISDK scene where controllers were not able to be used
- Improved performance and removed garbage creation when drawing the debug skeleton
- Added HMD remount + restart tracking feature via HMDRemountRestartTracking.cs
- Added menu action to build all available samples (Movement/Build Samples APK)
- Remove unused duplicate face quick action
- Fixed FaceExpressionModifierDrawer call to GUI properties on constructor
