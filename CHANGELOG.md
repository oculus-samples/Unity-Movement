# [2.0.0](https://github.com/oculus-samples/Unity-Movement/compare/v1.4.1...v2.0.0) (2023-05-11)


### Features

* **Editor/Runtime/Samples:** Update for v53 ([efd3001](https://github.com/oculus-samples/Unity-Movement/commit/efd3001184d162e8e18749bae1614960e2e50bba))


### BREAKING CHANGES

* **Editor/Runtime/Samples:** Samples rely on scripts found in v53 of the Oculus Integration SDK.

Editor:
- Add editor scripts for the new classes: LateMirroredObject, LateMirroredSkeleton, ARKitFace, CorrectivesFace, DeformationConstraint, HipPinningConstraint, TwistDistributionConstraint.
- Add HelperMenus to assist with setting up a new character with tracking.
- ScreenshotFaceExpressions has been replaced with ScreenshotFaceExpressionsCapture.

Runtime:
- Add animation rigging constraints, and animation rig setup for integrating OVRSkeleton with the animation rigging system.
- Added RetargetingLayer, which inherits from OVRUnityHumanoidSkeletonRetargeter to integrate animation rigging with retargeting. It is possible to play an animation on a retargeted character and mask specific bones.
- The added animation rigging constraints are:
  - DeformationConstraint
  - GroundingConstraint
  - HipPinningConstraint
  - PositionalJointConstraint
  - RetargetingAnimationConstraint
  - TwistDistributionConstraint
- Added new mirroring scripts, to consolidate MirrorSkeleton and TransformMirroredEyes.
  - LateMirroredObject mirrors a transform and its children.
  - LateMirroredSkeleton mirrors an OVRSkeleton.
- Added HandDeformation, a new script that performs similar logic to DeformationLogic to keep the proportions of the hand the same.

Samples:
- Created new scenes that show the new skeletal animation constraints running with OVRSkeleton, and using CorrectivesFace inheriting from OVRFace. The updated V2 scenes for Aura, HighFidelity, HipPinning, and Retargeting scenes are: AuraV2, HighFidelityV2, HipPinningV2, and RetargetingV2.
- Created a new sample scene, BlendshapeMappingExample that showcases how to use a character with ARKit blendshapes. By making use of ARKitFace, the blendshape mapping example character displays how a character rigged for ARKit blendshapes can use the face tracking output from OVRFaceExpressions.

DEPRECATED:  Oculus.Movement.Attributes have been removed; use Oculus.Interaction.Attributes instead. All non-animation rigging skeletal scripts have been placed into Legacy folders, and the old versions of the scenes will no longer be updated. FaceTrackingSystem has been deprecated in favor of CorrectivesFace.

Reviewed By: ethylee

Differential Revision: D45672379

fbshipit-source-id: 19b6494fb500f07fe1f5ad60d18e0d418e42987b

## [1.4.1](https://github.com/oculus-samples/Unity-Movement/compare/v1.4.0...v1.4.1) (2023-04-20)


### Bug Fixes

* **Runtime:** Fix skybox shader so that it works with single pass stereo ([eae6c69](https://github.com/oculus-samples/Unity-Movement/commit/eae6c69ada18fea72e229b5d412c5245914b53e7))
* **Runtime:** Fix transparent color shader so that it works with single pass stereo ([0e4b2ed](https://github.com/oculus-samples/Unity-Movement/commit/0e4b2edbd3b9e6358acf09e00a21a2cb9cebd8aa))
* **Runtime:** Update RecalculateNormals to check that the required layers are present ([f3c7787](https://github.com/oculus-samples/Unity-Movement/commit/f3c778786cefbcdc81a95a7da415e4b157d6a42a))

# [1.4.0](https://github.com/oculus-samples/Unity-Movement/compare/v1.3.3...v1.4.0) (2023-04-01)


### Bug Fixes

* **Runtime:** _SpecularityNDotL on by default ([3cffc88](https://github.com/oculus-samples/Unity-Movement/commit/3cffc880280119be18db93c5014a3daf32469f42))
* **Runtime:** Fix missing shader code on MovementCommon.hlsl ([d2e8e09](https://github.com/oculus-samples/Unity-Movement/commit/d2e8e09b238b1f3bd489e225c9cf34e8f1d7ac42))
* **Runtime:** Update MovementLitMetaPass and MovementShadowCasterPass to be compatible with multiple URP versions ([35bfbc1](https://github.com/oculus-samples/Unity-Movement/commit/35bfbc17ab993dbf551aa383fa9e263efe050188))
* **Samples:** Aura imported as humanoid ([63c2301](https://github.com/oculus-samples/Unity-Movement/commit/63c2301c16e6b30575df65b0fb05366515d70996))
* **Samples:** Update more button prefabs with ISDK prefabs ([8c9d1bd](https://github.com/oculus-samples/Unity-Movement/commit/8c9d1bd40b32111c60bb5299b0218d802c6b22ff))
* **Samples:** Update poke interaction button to use the ISDK prefab ([fcd2db9](https://github.com/oculus-samples/Unity-Movement/commit/fcd2db93af6bfcc91f6a69b10ffdf4d6cf423883))


### Features

* **Editor:** Custom drawer for BlendshapeModifier.FaceExpressionModifier ([886f4d0](https://github.com/oculus-samples/Unity-Movement/commit/886f4d07387ba60c3133670ad59996a753bc9df8))
* **Runtime:** Add option for recalculate normals to be calculated independently in late update ([ad903f4](https://github.com/oculus-samples/Unity-Movement/commit/ad903f4bd65401729379a76dfe7247b3bae0e721))
* **Runtime:** Allow creating duplicate skeleton from animator ([8420d95](https://github.com/oculus-samples/Unity-Movement/commit/8420d958190b39a396b662c77df0f27557504567))
* **runtime:** Update Movement PBR shaders to always compile recalculate normals, and remove shader variant collection project validation ([5b9ad95](https://github.com/oculus-samples/Unity-Movement/commit/5b9ad95e22d2ea785ff4c6e0be371c7c3b31d71b))
* **runtime:** Update URP shaders to be SRP batcher compatible ([96cd2d9](https://github.com/oculus-samples/Unity-Movement/commit/96cd2d901739437baa67bf2efa12ae18596dcbff))

## [1.3.3](https://github.com/oculus-samples/Unity-Movement/compare/v1.3.2...v1.3.3) (2023-01-21)


### Bug Fixes

* **Runtime:** FaceTrackingSystem Correctives now optional ([213d5b0](https://github.com/oculus-samples/Unity-Movement/commit/213d5b05841dd25b4b20bce6260cfe306c3415d8))
* **Runtime:** Remove maximum version in URP shader package requirements ([a3faab3](https://github.com/oculus-samples/Unity-Movement/commit/a3faab3b55a59da70e44523f57e7a12c6fc0c1ae))
* **Runtime:** Retargeting cleanup ([0febd9f](https://github.com/oculus-samples/Unity-Movement/commit/0febd9fc95bc0da7ebf264da29371d7f34d9cc14))
* **Samples:** Remove Aura's z-offset ([1193670](https://github.com/oculus-samples/Unity-Movement/commit/11936709db6c93780572738985edf40e28a4d3e1))
* **Samples:** Simplify AuraFirstPerson prefab to required components ([436ffb4](https://github.com/oculus-samples/Unity-Movement/commit/436ffb4546aca2180b2d0f14922446ccf3657bad))
* **Samples:** Updated smile effect ([f8bdd9f](https://github.com/oculus-samples/Unity-Movement/commit/f8bdd9f06502e7a8e0c8b3e789f7cb18b8c2e500))

## [1.3.2](https://github.com/oculus-samples/Unity-Movement/compare/v1.3.1...v1.3.2) (2022-12-05)


### Bug Fixes

* **Samples:** Set permission requests on startup for sample scenes, update Unity version in README ([7180d6f](https://github.com/oculus-samples/Unity-Movement/commit/7180d6f5e10b6d18424457da873100ee75e8e4a8))

## [1.3.1](https://github.com/oculus-samples/Unity-Movement/compare/v1.3.0...v1.3.1) (2022-11-30)


### Bug Fixes

* **Editor:** Add additional copyright text to copyright header in ShaderBuildPreprocessor ([9ef2c9f](https://github.com/oculus-samples/Unity-Movement/commit/9ef2c9f3d26c4b6f9a552e09744a1d035d662e3f))

# [1.3.0](https://github.com/oculus-samples/Unity-Movement/compare/v1.2.2...v1.3.0) (2022-11-29)


### Bug Fixes

* **Editor:** Validate that the recalculate normals shader variants are included in the project ([856b08e](https://github.com/oculus-samples/Unity-Movement/commit/856b08e9908f7715e85a0f87df04f0a76ab1cd41))
* **Runtime:** Disable position constraints in hip pinning ([53f5918](https://github.com/oculus-samples/Unity-Movement/commit/53f59180006b8313a492309d8f6c0e9bde940666))
* **Runtime:** Update Meta Pass fragment function for Unity 2021 URP ([db2ee81](https://github.com/oculus-samples/Unity-Movement/commit/db2ee814b2e9d77167b0c1522fc6fb26e22b433e))
* **Runtime:** Update URP shaders to support Unity 2021 LTS URP version (12.1.8) ([d852b57](https://github.com/oculus-samples/Unity-Movement/commit/d852b573dcf3ec14be117c61d1977c9aa7f6a0b2))
* **Runtime:** URP OpenGL ES3 support ([8d06093](https://github.com/oculus-samples/Unity-Movement/commit/8d06093b53e33ac42469aa1635ed95d6f36e81ef))
* **Runtime:** Variable should be called FreezeFacialExpressions ([0052187](https://github.com/oculus-samples/Unity-Movement/commit/00521871052538396e4ee9cf726230c3463cb1d1))
* **Samples:** Smile effect ([66f9676](https://github.com/oculus-samples/Unity-Movement/commit/66f9676e026ab8465ecdc683357acc93c7fe8ce9))


### Features

* **Runtime:** Add shader preprocessor to exclude URP shaders from compilation ([bb8f1d9](https://github.com/oculus-samples/Unity-Movement/commit/bb8f1d9b14bcc9a0ddf14a4bb3986319aa8fe504))
* **Runtime:** Expose mirrored skeleton's skeletons, update hip pinning logic, and remove interface support ([98e5fa6](https://github.com/oculus-samples/Unity-Movement/commit/98e5fa6f1c09fbf07ce55fc72dd17a383ac5ffc5))

## [1.2.2](https://github.com/oculus-samples/Unity-Movement/compare/v1.2.1...v1.2.2) (2022-11-11)


### Bug Fixes

* **Runtime:** Add missing smoothness texture channel shader feature ([c2f2f47](https://github.com/oculus-samples/Unity-Movement/commit/c2f2f473f6596af066a5a408a50cfab29ac44312))
* **Runtime:** Fix incorrect specular data in shaders ([63a8ec3](https://github.com/oculus-samples/Unity-Movement/commit/63a8ec3d448fd1efe55f6c174686fa9d6399f495))
* **Runtime:** Fix URP metallic gloss ([f1d24b8](https://github.com/oculus-samples/Unity-Movement/commit/f1d24b8ad30eb46a32bce6dd9f9e03bb274a7bbd))

## [1.2.1](https://github.com/oculus-samples/Unity-Movement/compare/v1.2.0...v1.2.1) (2022-11-08)


### Bug Fixes

* **Samples:** Add project settings validation wizard ([b2f5005](https://github.com/oculus-samples/Unity-Movement/commit/b2f5005213507401883bdf332e65028f8e76b01f))

# [1.2.0](https://github.com/oculus-samples/Unity-Movement/compare/v1.1.0...v1.2.0) (2022-11-07)


### Bug Fixes

* **Runtime:** Update URP shaders ([bae0b90](https://github.com/oculus-samples/Unity-Movement/commit/bae0b90632517cc56e07236fb1a6a89c725e0c83))
* **Samples, Runtime:** Fix serialization bug in retargeting, clean up script ([fb95851](https://github.com/oculus-samples/Unity-Movement/commit/fb95851f081c88aa66be399b968322d4fae52800))
* **Samples:** Added mirror prefab to scenes, remove text meta files ([9d80b6c](https://github.com/oculus-samples/Unity-Movement/commit/9d80b6cf24029778e93d429dac8fbf1c74421203))


### Features

* **Samples:** Add option to not calibrate the hip height for hip pinning ([f4cc1c2](https://github.com/oculus-samples/Unity-Movement/commit/f4cc1c29e95f9d158f86a5f0a03107b32ce2b14e))
* **Samples:** UI clean-up, remove old assets ([2675f46](https://github.com/oculus-samples/Unity-Movement/commit/2675f4695969a4d9d830b39769a7916ec7a40f3d))

# [1.1.0](https://github.com/oculus-samples/Unity-Movement/compare/v1.0.1...v1.1.0) (2022-11-02)


### Features

* **Runtime:** Update deformation logic ([c375c4c](https://github.com/oculus-samples/Unity-Movement/commit/c375c4cc4df990df880786bee74ef22e651832d8))

## [1.0.1](https://github.com/oculus-samples/Unity-Movement/compare/v1.0.0...v1.0.1) (2022-11-01)


### Bug Fixes

* Update hip pinning chair ([fba7b55](https://github.com/oculus-samples/Unity-Movement/commit/fba7b55784f97cf8e07a87954494153be217adab))

# 1.0.0 (2022-11-01)


### Features

* Initial commit ([e24717b](https://github.com/oculus-samples/Unity-Movement/commit/e24717b34ded22112bc94bd079b79c6745e6127c))
