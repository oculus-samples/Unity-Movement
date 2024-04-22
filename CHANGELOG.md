## [5.0.1](https://github.com/oculus-samples/Unity-Movement/compare/v5.0.0...v5.0.1) (2024-04-22)


### Bug Fixes

* **Editor:** Update retargeting one-click for multiple upper body objects ([f2c1dc2](https://github.com/oculus-samples/Unity-Movement/commit/f2c1dc268360a66f116f594a6f52c198aae4353a))
* **Samples:** Disable normals on animation clips that copy from other avatar ([b2d497e](https://github.com/oculus-samples/Unity-Movement/commit/b2d497eb677db774c8c75ec7c003a04fd4b82492))

# [5.0.0](https://github.com/oculus-samples/Unity-Movement/compare/v4.4.0...v5.0.0) (2024-04-19)


### Bug Fixes

* **Editor:** refresh bones on object change ([5b9493b](https://github.com/oculus-samples/Unity-Movement/commit/5b9493bc934864c539ce594cf0141f2d976624a5))
* **Editor:** refresh bones on scene change ([c9d0398](https://github.com/oculus-samples/Unity-Movement/commit/c9d03985c15a888aaed3c50641b15d97c4d480cb))
* **Editor:** Update spine adjustments calculation to affect only one axis ([8bc9397](https://github.com/oculus-samples/Unity-Movement/commit/8bc93977f85417988bbd6a660ae48f4c9415cf45))
* **Runtime:** Add explicit dependency to burst in the Movement package ([34025a0](https://github.com/oculus-samples/Unity-Movement/commit/34025a04456face1b2821e282869df5a27345b86))
* **Runtime:** Fix deformation's toes interpolation ([5c0e126](https://github.com/oculus-samples/Unity-Movement/commit/5c0e126efb18c71bb6f80b00a20a66d98ffb60ee))
* **Runtime:** Fix edge case with AnimatorBoneVisualizer's TryGetBoneTransforms ([336ea4f](https://github.com/oculus-samples/Unity-Movement/commit/336ea4f17f7adf89f476372582746b1f7faa065d))
* **Runtime:** Fix warnings for unused retargeted bone mappings ([c1ba908](https://github.com/oculus-samples/Unity-Movement/commit/c1ba908a7937c5ce24f92677ba5893269f4fb3f0))
* **Runtime:** Optimize performance of retargeting ([7beeb41](https://github.com/oculus-samples/Unity-Movement/commit/7beeb4121041352fe8afbe44be3ec7254a7034b9))
* **Runtime:** RetargetingProcessorCorrectBones interpolates using adjustments, fix finger position weight usage ([f51ea12](https://github.com/oculus-samples/Unity-Movement/commit/f51ea12afc6c0c0ef323f1143012dabf605faf02)), closes [#1](https://github.com/oculus-samples/Unity-Movement/issues/1) [#2](https://github.com/oculus-samples/Unity-Movement/issues/2)
* **Runtime:** Use correct world-to-local transformation for start to end vector ([f41a622](https://github.com/oculus-samples/Unity-Movement/commit/f41a622e4038381c297918fb8f6209b23d0a99ce))
* **Samples:** added chest to all bones listing ([e957e08](https://github.com/oculus-samples/Unity-Movement/commit/e957e080a651fbe86e276e41f8694a37f8368d97))
* **Samples:** added controllers forfitness to control UI ([adf1582](https://github.com/oculus-samples/Unity-Movement/commit/adf1582e2dfaf943de811f0cce7374fc0ec81cba))
* **Samples:** Fix icon positioning of menu outline ([c121fb9](https://github.com/oculus-samples/Unity-Movement/commit/c121fb9e7354306b370afd53d7288a03c9e465bb))
* **Samples:** hide skeleton lines when object disables ([1d82ea1](https://github.com/oculus-samples/Unity-Movement/commit/1d82ea1b8a6dd49b0ebdc3d0dde75a8393dab76e))
* **Samples:** renamed movement events ([a273892](https://github.com/oculus-samples/Unity-Movement/commit/a2738926f55f1ae0e936045cb7f1f7a110351756))


### Documentation

* **Samples:** Update README to new SDK version ([ae7bc2e](https://github.com/oculus-samples/Unity-Movement/commit/ae7bc2e965c7b9bfe89dd8ab484f0962b764bbac))


### Features

* **Editor:** Update one-click retargeting with new parameters ([a827e6f](https://github.com/oculus-samples/Unity-Movement/commit/a827e6f775383a0ec11fb18292da916ff0506edc))
* **Runtime:** Add support for automated custom bone mappings in retargeting ([df044c1](https://github.com/oculus-samples/Unity-Movement/commit/df044c112edc3919f72efee896f5112772f8a92e))
* **Runtime:** Add the shoulder roll property for deformation ([1e4dfa0](https://github.com/oculus-samples/Unity-Movement/commit/1e4dfa05a681b33d1006cbfebe251412a4022e3e))
* **runtime:** BodyTrackingForFitness sample ([0bfa946](https://github.com/oculus-samples/Unity-Movement/commit/0bfa946b07bb4513d6c16a5f76a516d879e39f84))
* **Runtime:** Deformation allows straightening spine ([5901e5a](https://github.com/oculus-samples/Unity-Movement/commit/5901e5a0984b93e30c88a1b8cceae12ab42ef7ec))
* **Runtime:** Finger position correction weight ([d49cb39](https://github.com/oculus-samples/Unity-Movement/commit/d49cb39743517826b58734c892207777c9fbbdb4))
* **Runtime:** Optimize more, and clean up ([7d530fa](https://github.com/oculus-samples/Unity-Movement/commit/7d530facb8d857ba692e1b4d694f33d7b40ea0d9))
* **Runtime:** Original spine offsets should be proportional ([90b248a](https://github.com/oculus-samples/Unity-Movement/commit/90b248a4dc369f9f2668d6986c8ee23531c9b438))
* **Runtime:** Update retargeting blend hand processor for tracked head options ([24778ba](https://github.com/oculus-samples/Unity-Movement/commit/24778ba4e8139dd21e9df9f647ac823d53584252))
* **Samples:** Show hands or controllers with disembodied character ([1ae5cdc](https://github.com/oculus-samples/Unity-Movement/commit/1ae5cdc78964633f41361d14676cd76015eb4098))


### BREAKING CHANGES

* **Runtime:** Updated bone mappings requires V63 SDK.

V63 SDK introduces the OVRHumanBodyBonesMappingsInterface, which can be used to create custom bone pair mappings for retargeting. This feature adds support for that capability, adding the ability to set the bone mappings based on which optional bones are present on the Unity humanoid.

The automatic mapping maps the HumanBodyBones.Chest bone to the OVRSkeleton.SpineMiddle bone, for a better result with the spine retargeting on most characters. However, if there is a missing UpperChest bone, the OVRSkeleton.SpineUpper bone will be mapped instead. These mappings can be modified in the inspector under Retargeted Bone Mappings.

Reviewed By: sohailshafiiWk

Differential Revision: D55996107

fbshipit-source-id: af32288b922282f595bd653852f051b6171738a1
* **Samples:** using v62 instead of v60

Updated references to the correct SDK version.
Updated instructions for adding a Camera Rig.
Added troubleshooting note about attempting to run scenes form the Packages folder (they may need to be copied to /Assets).

Reviewed By: sohailshafiiWk

Differential Revision: D55378812

fbshipit-source-id: b1cdf4c2a039f278e976c3f7f80ab21bb0d03542

# [4.4.0](https://github.com/oculus-samples/Unity-Movement/compare/v4.3.0...v4.4.0) (2024-03-13)


### Bug Fixes

* **Editor:** Add FullBodyOVRSkeletonBoneVisualizerBoneTupleDrawer to display full body tuples ([eb4e7cd](https://github.com/oculus-samples/Unity-Movement/commit/eb4e7cdf4f8dff5b3b7c496f74d2a29edc2cfac1))
* **Editor:** Fix one-click upper body retargeting setup ([4e40556](https://github.com/oculus-samples/Unity-Movement/commit/4e4055632cec5576c0ed3e12958386b7c23956f1))
* **Editor:** Fix serialized legs weights display on new characters ([294c70e](https://github.com/oculus-samples/Unity-Movement/commit/294c70e784ac6617f2e8f499da323bb0b9830e9c))
* **Runtime:** Address foot target orientation for grounding (hip pinning) ([0aabd0d](https://github.com/oculus-samples/Unity-Movement/commit/0aabd0d19f582f0da484dceffe188adfe19c21aa))
* **Runtime:** Fix one-click setup for retargeted characters when the root isn't zero ([868aa83](https://github.com/oculus-samples/Unity-Movement/commit/868aa834fed79a2ddadeb1204699207d99ba0f45))
* **Runtime:** Handle root motion edge case with 2022/2023 ([7bf9a7f](https://github.com/oculus-samples/Unity-Movement/commit/7bf9a7f620f34caaa5ac931583a3fb9800ef21eb))
* **Runtime:** Improve auto adjustments axis calculation ([5ed2139](https://github.com/oculus-samples/Unity-Movement/commit/5ed2139aa2ea50fbb66ba7551fa002ad2f7df7bb))
* **Runtime:** RetargetingHandProcessor uses absolute scale for hand world position ([1d546d3](https://github.com/oculus-samples/Unity-Movement/commit/1d546d37a5ff8533d860b947eeae6681029d9e01))
* **Runtime:** Update auto adjustments for skeletons with a different rest pose right ([30da68f](https://github.com/oculus-samples/Unity-Movement/commit/30da68fc6e920f51aa19d18924383825eae78d24))
* **Runtime:** Update bone visualizer to better reflect the bone mapping ([6a88513](https://github.com/oculus-samples/Unity-Movement/commit/6a885134c390d1ef2e78c94ba7c441eb4d776ce7))


### Features

* **Editor:** Update auto adjustments calculation ([24ad9d0](https://github.com/oculus-samples/Unity-Movement/commit/24ad9d082d17661471f3a9fbbd79d6c01ffcceae))
* **Runtime:** Account for missing bones when calculating adjustments ([5e8263d](https://github.com/oculus-samples/Unity-Movement/commit/5e8263de6f11f5acef37d23b9b2dafe6fb0c3c63))
* **Runtime:** Add BlendHandConstraintProcessor ([90e1dd3](https://github.com/oculus-samples/Unity-Movement/commit/90e1dd3ff6953e2fc2fad12523a55320ed30bf12))
* **Runtime:** Add ExternalBoneTargets ([0bc2fc0](https://github.com/oculus-samples/Unity-Movement/commit/0bc2fc06c7731e14e6ce9b6c27d3a6fa95a8cce7))
* **Runtime:** Add RetargetingAnimationRig ([0739a30](https://github.com/oculus-samples/Unity-Movement/commit/0739a30647a2d2a292937f1a1263957300669c5a))
* **Runtime:** Add RetargetingHandDeformationProcessor ([9bd4596](https://github.com/oculus-samples/Unity-Movement/commit/9bd4596f2a35bb36f8fc1686a4d599f73c2b2b91))
* **Runtime:** Add upper body option to FullBodyDeformationConstraint ([aa84fb9](https://github.com/oculus-samples/Unity-Movement/commit/aa84fb9f0d36b63d88f53302e70790080fb94604))
* **runtime:** Incorporate BlendHandConstraintProcessor ([a78affd](https://github.com/oculus-samples/Unity-Movement/commit/a78affd4c9f96a5bd871e19c8cd69c79863b5731))
* **Runtime:** Incorporate ExternalBoneTargets ([d72bb8f](https://github.com/oculus-samples/Unity-Movement/commit/d72bb8f92c6b44e418dc754c7a90f26915747c37))
* **Runtime:** Integrate RetargetingAnimationRig ([11a9c19](https://github.com/oculus-samples/Unity-Movement/commit/11a9c1957abf6435bd9de42caa57831fb2d1cc38))
* **Runtime:** Integrate RetargetingHandDeformationProcessor ([3b05073](https://github.com/oculus-samples/Unity-Movement/commit/3b05073909c84fcfc9ee90a369b6611059eb3d6a))
* **Runtime:** Limit deformation's squash and stretch ([2999663](https://github.com/oculus-samples/Unity-Movement/commit/2999663e680d930007891db294fbd6e2a8efeb3b))
* **Runtime:** Update full body deformation arm weight behaviour ([e586f7c](https://github.com/oculus-samples/Unity-Movement/commit/e586f7cc943e25c1c097575a55adb2da85696664))
* **Runtime:** Update upper body deformation for OVRCustomSkeleton ([456c9ec](https://github.com/oculus-samples/Unity-Movement/commit/456c9ec59887d614cfd7cd403a3cee15f3f19217))

# [4.3.0](https://github.com/oculus-samples/Unity-Movement/compare/v4.2.1...v4.3.0) (2024-02-15)


### Bug Fixes

* **Runtime:** RetargetingProcessorCorrectBones uses base class adjustments, update all adjustments in RtLayer ([7fa22af](https://github.com/oculus-samples/Unity-Movement/commit/7fa22af527f153cf7496f8a7386d046b60bb9192))
* **Runtime:** Update AddComponentsRuntime with new adjustments ([3fd5cd6](https://github.com/oculus-samples/Unity-Movement/commit/3fd5cd6a467d4f8f2ff76ac58267781e37d9ba55))


### Features

* **Runtime:** Add custom target hand position for correct hands and update one-click adjustments ([46c642b](https://github.com/oculus-samples/Unity-Movement/commit/46c642b9fa8dc824506f3b8d258ad99f9d61ff3d))
* **Runtime:** Add FABRIK option for hand correction processor ([0837ee4](https://github.com/oculus-samples/Unity-Movement/commit/0837ee4e6fcafc2322c3a1aa62bbee548c45190a))
* **Runtime:** Add spine bone adjustments to deformation and automate adjustments ([a38ed8f](https://github.com/oculus-samples/Unity-Movement/commit/a38ed8f1b18346e073cfabe9a0bc1a88c8ecc217))
* **Runtime:** Automatic adjustments for retargeting and deformation on full body ([5fec824](https://github.com/oculus-samples/Unity-Movement/commit/5fec824b68b3dd5ffbf77799ff5c811c970d18f0))
* **Runtime:** Update feet placement for accurate head full body deformation ([b8543cd](https://github.com/oculus-samples/Unity-Movement/commit/b8543cda5db6e9facb91f3a4efd89b2f1eacfdda))

## [4.2.1](https://github.com/oculus-samples/Unity-Movement/compare/v4.2.0...v4.2.1) (2024-02-01)


### Bug Fixes

* **Runtime:** Update tongue to use captured expression weights ([d1fb642](https://github.com/oculus-samples/Unity-Movement/commit/d1fb6426dbf63d7d824886e0445c165e1546ec3a))

# [4.2.0](https://github.com/oculus-samples/Unity-Movement/compare/v4.1.0...v4.2.0) (2024-02-01)


### Bug Fixes

* **Runtime:** Check for invalid shoulders in FullBodyDeformationJob ([6785b0c](https://github.com/oculus-samples/Unity-Movement/commit/6785b0ca14445d07b5770f35995e9cec819a6524))
* **Runtime:** Check for null targetBone in CheckPossibleMetacarpal ([50dc6fb](https://github.com/oculus-samples/Unity-Movement/commit/50dc6fb22930c1a84d6ffd75f71ae4632905cab6))
* **Runtime:** Fix spawning positional problems in retargeting scene ([70f0a7c](https://github.com/oculus-samples/Unity-Movement/commit/70f0a7c58aa3b61a801d7788d3198c125416d006))
* **Runtime:** Set initial shoulder stretch to be optional ([3f55759](https://github.com/oculus-samples/Unity-Movement/commit/3f557594507f41c5902caff60fabf413ba83be16))


### Features

* **Editor:** Simplify full body deformation constraint parameters ([79b3acb](https://github.com/oculus-samples/Unity-Movement/commit/79b3acb7f867501ef2c017b75df9b98221ff7b67))
* **Runtime:** Add an option for using world hand position as the target ([cc57dee](https://github.com/oculus-samples/Unity-Movement/commit/cc57dee48bda9e3b6f3b8849d121fa77ea3a076f))
* **Runtime:** Allow precomputation of adjustments ([c3e8cde](https://github.com/oculus-samples/Unity-Movement/commit/c3e8cde8156223d679dac95d804f6b7f90add512))
* **Runtime:** Put proxy bones under a parent transform ([11733ed](https://github.com/oculus-samples/Unity-Movement/commit/11733ed9650a82e009cc670fa712a8ac460bc047))
* **Runtime:** Update FullBodyDeformation with additional settings for shoulders and arms ([576b0da](https://github.com/oculus-samples/Unity-Movement/commit/576b0da4687f073c17f37c07540c7bae40b67487))

# [4.1.0](https://github.com/oculus-samples/Unity-Movement/compare/v4.0.1...v4.1.0) (2024-01-11)


### Bug Fixes

* **Editor:** Update one-clicks for adding body tracking to disable apply root motion on animators ([cb7b68c](https://github.com/oculus-samples/Unity-Movement/commit/cb7b68c76b1308deeb52cad17498cf0d659800f5))
* **Runtime:** DeformationJob checks for animator avatar for bone queries ([f91bb92](https://github.com/oculus-samples/Unity-Movement/commit/f91bb92aaefdc33ed6c053bd9f907b491b556870))
* **Runtime:** Fix potential missing transform handle in CopyPoseJob ([10dd1bc](https://github.com/oculus-samples/Unity-Movement/commit/10dd1bcf76d342f8af7aa9ee7eeb5a94513544a7))


### Features

* **Runtime:** Add shoulders adjustments when using the one-click for setting up body tracking ([327ad57](https://github.com/oculus-samples/Unity-Movement/commit/327ad575dd2078c809940457eba25a1ec45c89df))
* **Runtime:** Check for humanoid, translation DoF for retargeting ([591148f](https://github.com/oculus-samples/Unity-Movement/commit/591148fead153e9770f37a36d479d5469cd80a55))
* **Samples:** Update Locomotion sample with character prefab ([217a99a](https://github.com/oculus-samples/Unity-Movement/commit/217a99a4e3aba2657d3d08614907d7421f0fa2f1))

## [4.0.1](https://github.com/oculus-samples/Unity-Movement/compare/v4.0.0...v4.0.1) (2023-12-21)


### Bug Fixes

* **Samples:** Add OVRBody to high fidelity prefabs ([3326a4f](https://github.com/oculus-samples/Unity-Movement/commit/3326a4f393616ce78bee49df387edfd71bf733c5))

# [4.0.0](https://github.com/oculus-samples/Unity-Movement/compare/v3.1.1...v4.0.0) (2023-12-16)


### Bug Fixes

* **Runtime:** Add max stretch for CCDIK and update deformation ([dbe19d1](https://github.com/oculus-samples/Unity-Movement/commit/dbe19d149ec58b7332868e14ace0dc02f476ee97))
* **Runtime:** Allow controlling blendshape example's constraints via skeletal state ([6579f9e](https://github.com/oculus-samples/Unity-Movement/commit/6579f9e7d5e9e0f7f4288abf28d815c087a18fda)), closes [#1](https://github.com/oculus-samples/Unity-Movement/issues/1) [#2](https://github.com/oculus-samples/Unity-Movement/issues/2)
* **Runtime:** Disable null check on BlendHandConstraint _autoAddTo ([c3fd6ad](https://github.com/oculus-samples/Unity-Movement/commit/c3fd6ad03195c5ae3f6458bd31a7621ca659a808))
* **Runtime:** Update the accurate head option in deformation ([7a20b3b](https://github.com/oculus-samples/Unity-Movement/commit/7a20b3bf0f4ea3cebdfe4ce5d18abdc0b049b3b1))
* **Samples:** Disable _rigToggleOnFocus ([c6dc3bc](https://github.com/oculus-samples/Unity-Movement/commit/c6dc3bcdda0abd2a4dd1cdc5ac77a31f76036bfc))


### Features

* **Editor/Runtime/Samples:** Update for v60 ([8d5f1bb](https://github.com/oculus-samples/Unity-Movement/commit/8d5f1bb429a116b177ffefd1bd2e61b4e75b11c0))
* **Editor:** Add IntAsEnum property attribute and drawer ([ccaa9cd](https://github.com/oculus-samples/Unity-Movement/commit/ccaa9cd300a8a188c7b3c5053f63284c4e0a3493))
* **Editor:** Update helper menus to add retargeting processors on the one-click setup ([cb2d5da](https://github.com/oculus-samples/Unity-Movement/commit/cb2d5daa0b218505548872022bb1922fc70d850a))
* **Editor:** Update one-click to also setup elbows and new deformation parameters ([11bf4b0](https://github.com/oculus-samples/Unity-Movement/commit/11bf4b067c616fb92241163f279ffe13534e2378))
* **Runtime:** Add a weight to correct shoulder rotations in late update ([fb8d357](https://github.com/oculus-samples/Unity-Movement/commit/fb8d3572782f1fc88a2fafed9388a6c45d0036df))
* **Runtime:** Add additional spine alignment weights for the other spine bones ([63ba963](https://github.com/oculus-samples/Unity-Movement/commit/63ba96322049d358c38ad689c9f65b0549f82fa5))
* **Runtime:** Add animation playback constraint ([5854f9e](https://github.com/oculus-samples/Unity-Movement/commit/5854f9e7fcd12d204b85c088f60527c96348c7cf))
* **Runtime:** Add retargeting processors for retargeting layer, and update hand placement in deformation ([29e529b](https://github.com/oculus-samples/Unity-Movement/commit/29e529b2846c8ed151704b3d8b9c3248842dbeb3))
* **Runtime:** Add shoulder stretch to RetargetingProcessorCorrectHand ([bf05baa](https://github.com/oculus-samples/Unity-Movement/commit/bf05baa01c6475677487b70cb685e24b9863345e))
* **Runtime:** Hip pinning supports animator ([67a19fe](https://github.com/oculus-samples/Unity-Movement/commit/67a19fe585d9398218d10299a7ee368d16d95f98))
* **Runtime:** Retargeter sets hips via root motion function ([d79ee8f](https://github.com/oculus-samples/Unity-Movement/commit/d79ee8f68470d964ca87e05a81bdabe51c1bf573))
* **Runtime:** Update BlendHandConstraint to control the weight of the correct hand retargeting processor ([cc340eb](https://github.com/oculus-samples/Unity-Movement/commit/cc340eb8a499c3f1a58fe4db95cc0aac571c416c))
* **Runtime:** Update Deformation constraint logic to correct the spine more accurately along with the shoulders ([c4d9e31](https://github.com/oculus-samples/Unity-Movement/commit/c4d9e314506b4f5a08fa10c82ee29643f85215ef))
* **Samples:** Buttons for ISDK and locomotion scenes ([02de41b](https://github.com/oculus-samples/Unity-Movement/commit/02de41bb84066f066fe7af6b5236e6cde58ef935))
* **Samples:** Trilinear/aniso settings for locomotion textures ([eb1f38f](https://github.com/oculus-samples/Unity-Movement/commit/eb1f38f5f044ed03e139c3daac4d41ecb96cac04))
* **Samples:** Turn on translate dof on HighFi model ([ff68e17](https://github.com/oculus-samples/Unity-Movement/commit/ff68e176a1ce4e8db13965a263335cb7eab4fe03))


### BREAKING CHANGES

* **Editor/Runtime/Samples:** Samples rely on scripts found in v60 of the Meta SDK.

Editor:
- Add OVRProjectSetupMovementSDKSamples task to integrate with the Oculus Project Setup Tool for sample specific tasks
- Remove ProjectValidationWindow, as the Project Setup Tool is used instead now

Runtime:
- Add support for tongue tracking
- Add DeformationRig for driving available correctives on the face
- Add full body for retargeting
- Add full body animation constraints
- Add toggle between high and low fidelity upper body tracking (IOBT)
- Add body tracking calibration example
- Updated helper menus to include setup for full body, and new constraints for upper body
- Updated locomotion to support full body, and fix several bugs with locomotion
- Updated locomotion to use animation blending between tracking (full body) and the locomotion animator

Samples:
- Updated the MovementAura scene with tongue support
- Updated the MovementRetargeting scene to use a full body
- Updated the MovementISDKIntegration scene to use a full body
- Updated the MovementLocomotion scene to use a full body

DEPRECATED: All legacy scripts have been placed in the Legacy folder. This includes the following: CustomMappings, FullBodyCustomMappings, AnimationConstraintMasker.

Reviewed By: sohailshafiiWk

Differential Revision: D52219081

fbshipit-source-id: 3dbdebc730609ec9c51ea8dee581ef36c831e9db

## [3.1.1](https://github.com/oculus-samples/Unity-Movement/compare/v3.1.0...v3.1.1) (2023-09-21)


### Bug Fixes

* **Runtime:** Add ActivateToggle copyright notice ([0f799b6](https://github.com/oculus-samples/Unity-Movement/commit/0f799b64ca7de70549114b2659f4a2b995cbe85b))

# [3.1.0](https://github.com/oculus-samples/Unity-Movement/compare/v3.0.0...v3.1.0) (2023-09-20)


### Bug Fixes

* **Runtime:** _jointRotationTweaks null check ([649d1db](https://github.com/oculus-samples/Unity-Movement/commit/649d1dbe6487477a6aea84f10c7c477291ecb9e9))
* **Runtime:** AnimationRigSetup checks proxy setting relative to RetargingLayer ([361fe8c](https://github.com/oculus-samples/Unity-Movement/commit/361fe8c420592c25cdaf2813ef812b73dd6780db))
* **Runtime:** Catch exceptions in RetargetingLayer, avoid null ref in copy pose ([0a9f6ad](https://github.com/oculus-samples/Unity-Movement/commit/0a9f6ad1b4824a14eefbd61810f9a96d1279e7ce))
* **Runtime:** Change humanoid checks to warnings ([4db5b73](https://github.com/oculus-samples/Unity-Movement/commit/4db5b737da7220ce9aa4e094fdfeecc148b5c013))
* **Runtime:** IProcessor null check ([6638763](https://github.com/oculus-samples/Unity-Movement/commit/6638763f0e4e546fdcb3419ad1a6661a0aac3731))


### Features

* **Editor:** Project settings validation window ([20d7a57](https://github.com/oculus-samples/Unity-Movement/commit/20d7a57cc34b1591259afa427f0ac5366a7b201a))
* **Runtime:** Turn on dynamic adjustments to true by default ([29d0ecc](https://github.com/oculus-samples/Unity-Movement/commit/29d0ecce57a562622a23debe0a16480372c11e5e))

# [3.0.0](https://github.com/oculus-samples/Unity-Movement/compare/v2.4.0...v3.0.0) (2023-09-05)


### Bug Fixes

* **Runtime:** Allow multiple rotation adjustments for retargeting ([40bfb0e](https://github.com/oculus-samples/Unity-Movement/commit/40bfb0ec1ceb2cd8ec271680389ed7c3530cf990))
* **Runtime:** Hand constraint affects retargeting layer's weight per hand ([bd05634](https://github.com/oculus-samples/Unity-Movement/commit/bd05634e786e5a6f729149a0c28ba26973e30b85))
* **Runtime:** Hand correction wrist inclusion fix ([33ca1cd](https://github.com/oculus-samples/Unity-Movement/commit/33ca1cd750b2ef6727754a6b1e616714777ec78c))


### Features

* **Editor:** Add ARKit one-click menu ([084757e](https://github.com/oculus-samples/Unity-Movement/commit/084757e5e4a90044a48f462d19bee2833aa996dc))
* **Editor:** Add constraints to retargeting helper menu ([fe410bf](https://github.com/oculus-samples/Unity-Movement/commit/fe410bf60461944074ed317e0673c69caad4ced2))
* **Editor:** Allow changing duplicate mapping bool from 1-click FT menus ([b634ae6](https://github.com/oculus-samples/Unity-Movement/commit/b634ae63c9d3c2c211a876a771f929dae23f6176))
* **Runtime:** Add functions to add face tracking at runtime ([c9561f5](https://github.com/oculus-samples/Unity-Movement/commit/c9561f5eb39b0e9b2da686d7385470f304d61c17))
* **Runtime:** Update version in README to v56 SDK ([ae6861f](https://github.com/oculus-samples/Unity-Movement/commit/ae6861f708e71a8be5efdd3db97f38cd1cf570d3))
* **Runtime:** Upgrade to v56.0 SDK ([67670ff](https://github.com/oculus-samples/Unity-Movement/commit/67670ffa19b7dde7fe3bb8bba154fd5f4fdf2553))
* **samples:** Adds menu to toggle animation on retargeted character ([fd8e15b](https://github.com/oculus-samples/Unity-Movement/commit/fd8e15bb6b16a1ed7f4b64e9a1368ef425833ba8))


### BREAKING CHANGES

* **Runtime:** Updates to support v56.0 SDK, depends on script changes in that version.

Since `PointableUnityEventWrapper` `UnityEvent`s uses `PointerEvent` as the generic variable now, functions used for `PointerEvent`s had to be updated.

See the following changes:
1. `ToggleObjects.cs`
2. `DriveThirdPartySkeleton.cs`
3. `HipPinningCalibration.cs`
4. `ToggleMenu.cs`
5. `BlendshapesMenuToggle.cs`
6. `ButtonToggleIcon.cs`

Reviewed By: andkim-meta

Differential Revision: D48727866

fbshipit-source-id: 6cc0d502cb3b594a607b781fbe8626bc170e3c62

# [2.4.0](https://github.com/oculus-samples/Unity-Movement/compare/v2.3.0...v2.4.0) (2023-08-18)


### Bug Fixes

* **Runtime:** Add more checks to prevent invalid look rotation operations for twist distribution ([2da6751](https://github.com/oculus-samples/Unity-Movement/commit/2da67517ecc0fbd9d0fce09563bcdcc5e84b76a4))
* **Runtime:** Adjust conditionals related to late update ([39b3998](https://github.com/oculus-samples/Unity-Movement/commit/39b3998d7f6b3af505aa188a93ae11026fe508f2))
* **Runtime:** Allow multiple tuples with same starting bone for OVRSkeleton visualizer ([ccc60b0](https://github.com/oculus-samples/Unity-Movement/commit/ccc60b0ac06a4b234637adf12749411866eae501))
* **Runtime:** Check IsFinite on input position when fixing bones in LateUpdate ([4ddeade](https://github.com/oculus-samples/Unity-Movement/commit/4ddeade2cfc0dec3cd893e25d6df355c7cecfa5c))
* **Runtime:** Deformation constraint sets up data at edit time ([74dc7a6](https://github.com/oculus-samples/Unity-Movement/commit/74dc7a68273eb3b7935fc1e796ba1944f8dfa4bc))
* **Runtime:** Fix constraints using the same job binder data when multiple constraints are present ([d9d9519](https://github.com/oculus-samples/Unity-Movement/commit/d9d9519a2c3628755cf34932c87511893b410c13))
* **Runtime:** Fix OVR bone pairs issue, cast to BodyTrackingBoneId when creating visual objects ([5b44a85](https://github.com/oculus-samples/Unity-Movement/commit/5b44a85678fcf42404a0e6f926efb74895a695d3))
* **Runtime:** Initialize vectors in CopyPoseJob ([1de0d7e](https://github.com/oculus-samples/Unity-Movement/commit/1de0d7e5cb1106a09445b1847e55a50a1e787253)), closes [#3](https://github.com/oculus-samples/Unity-Movement/issues/3)
* **Runtime:** Make hands more accurate with constraints ([a03b423](https://github.com/oculus-samples/Unity-Movement/commit/a03b4237790e1a47e6b30810edb29a31a67465f3))
* **Runtime:** Prevent duplicates of tuples in visual skeleton, allow tapering ([5508364](https://github.com/oculus-samples/Unity-Movement/commit/5508364f0e3d57378ce24ecd7fd8b4caa0707933))
* **Runtime:** Remove LateUpdate correction mask ([b189f20](https://github.com/oculus-samples/Unity-Movement/commit/b189f201af695d7d2d179ada9fa2757ae8b8ad78))
* **Runtime:** Retargeting layer respects weight of constraint ([c71aa09](https://github.com/oculus-samples/Unity-Movement/commit/c71aa09e8e0ccb6fece51792ddb515b6e05caa96))
* **Runtime:** Straighten legs with constraints ([364d235](https://github.com/oculus-samples/Unity-Movement/commit/364d235ed5a14b36659fc8c4a73c23feb2bcaf9d))
* **Runtime:** Treat masks as optional in AddComponentsRuntime ([5d7e2bc](https://github.com/oculus-samples/Unity-Movement/commit/5d7e2bc8a959af415593fe6a04d7169fe347f2c9))
* **Runtime:** Twist distro set up during edit time ([8e97568](https://github.com/oculus-samples/Unity-Movement/commit/8e975689b37abd5b91a04cbcf82320db67f6d578))
* **Runtime:** Update twist distribution to check against the look rotation being zero ([330620f](https://github.com/oculus-samples/Unity-Movement/commit/330620f6b072852cf6eeba39748b6335fc25a427))
* **Samples:** Fix legs of spawnable green robot ([163cf9a](https://github.com/oculus-samples/Unity-Movement/commit/163cf9aa8ffdf45a141b1b0fba4bfb0087670d7b))


### Features

* **Runtime:** Add bone axis renderer, make color shaders support stereo ([1f4065f](https://github.com/oculus-samples/Unity-Movement/commit/1f4065fec6adc34bac33d34a501eebcee60decdc))
* **Runtime:** Add checks for upper chest and shoulders ([07cac4c](https://github.com/oculus-samples/Unity-Movement/commit/07cac4cbdaa3022dc2aecc297781fa74023d4ab2))
* **Runtime:** Add IsFiniteVector3 check to RiggingUtilities ([d17a86f](https://github.com/oculus-samples/Unity-Movement/commit/d17a86fc3edebcb7e3f6f282eb8a6538e9568207))
* **Runtime:** Allow duplicate tuples to be rendered for animator bones ([0cb2314](https://github.com/oculus-samples/Unity-Movement/commit/0cb2314c21ad429be197f7d42eced3fe94d311c9))
* **Runtime:** Create animator bone visual ([625c102](https://github.com/oculus-samples/Unity-Movement/commit/625c102de2e5a4e18659481e4bd1237d502e5d12))
* **Runtime:** Custom bone visualizer ([c7ddb2a](https://github.com/oculus-samples/Unity-Movement/commit/c7ddb2a15994a091c2a74585f5bb762d23763337))
* **Runtime:** OVRSkeleton visualizer ([22da8aa](https://github.com/oculus-samples/Unity-Movement/commit/22da8aa154f954c17e36decf90f462539fc05eb8))
* **Runtime:** Run visualizers in LateUpdate ([b19c5ea](https://github.com/oculus-samples/Unity-Movement/commit/b19c5ea17552325763291db85505bcae19dbba2d))
* **Runtime:** Update unity functions in AnimatorBoneVisualizer to be inheritable ([3547708](https://github.com/oculus-samples/Unity-Movement/commit/3547708e6f44364e692f9bf021d449246ab09102))
* **Runtime:** Validate humanoid bones for retargeting ([c8d1eeb](https://github.com/oculus-samples/Unity-Movement/commit/c8d1eeb21581c5dac0e9c469beb5f6b9d98ba766))

# [2.3.0](https://github.com/oculus-samples/Unity-Movement/compare/v2.2.0...v2.3.0) (2023-07-07)


### Bug Fixes

* **Editor:** Validation checks certain names by layer and others by index ([20582e1](https://github.com/oculus-samples/Unity-Movement/commit/20582e12b3b425e40dd9be0fd574697f048be4f1))
* **Runtime:** Address edge case where GetNumberOfTransformsRetargeted is called too early ([01878e7](https://github.com/oculus-samples/Unity-Movement/commit/01878e7971cb654161f17b60daf152b70904b7f8))
* **Runtime:** Deprecate OVRSkeleton, use OVRCustomSkeleton in DeformationConstraint ([b4ffada](https://github.com/oculus-samples/Unity-Movement/commit/b4ffada0ab99c60ba9188811717685cd5e752ddd))
* **Runtime:** Disable constraint game objects until data has been prepared ([57bfdc8](https://github.com/oculus-samples/Unity-Movement/commit/57bfdc838b4449f4637e7250952b047b80d39958))
* **Runtime:** Disambiguate between focus changes in build and editor ([6761a7e](https://github.com/oculus-samples/Unity-Movement/commit/6761a7e31c9d3d4990f0f7108bc83a8c978a0707))
* **Runtime:** Do not disable animator and rig before skeleton is initialized ([d1f4f55](https://github.com/oculus-samples/Unity-Movement/commit/d1f4f5509beb53eee1566caed9721a941ba10289))
* **Runtime:** Don't call DisableRigAndUpdateState before Setup in focus function ([f5c8e61](https://github.com/oculus-samples/Unity-Movement/commit/f5c8e613c9f77e7f2c4d13deb7b81e07e2ce751e))
* **Runtime:** Don't check for animator initialized state in constraints ([c1cca0f](https://github.com/oculus-samples/Unity-Movement/commit/c1cca0f7752e7d1ed536a86c1600ae11d77475e8))
* **Runtime:** Enlarge bounds of robot, call rig evaluate during disabling ([ef79190](https://github.com/oculus-samples/Unity-Movement/commit/ef79190ec0fa24ba20dd8d226dca9ffa0a1bc8eb))
* **Runtime:** Fix and update the code that adds animation rigging retargeting ([2f4a4be](https://github.com/oculus-samples/Unity-Movement/commit/2f4a4be1288d857aaf2f04ac32d570e3bcf7abde))
* **Runtime:** Fix AnimationRigSetup ranSetup condition ([25fb689](https://github.com/oculus-samples/Unity-Movement/commit/25fb689206f337e1507f01b00f444bf4679dfded))
* **Runtime:** Fix deformation job starting without required data ([9bb822c](https://github.com/oculus-samples/Unity-Movement/commit/9bb822c305a39b32588bf37eac5703f54969bdcc))
* **Runtime:** Fix proxy, retargeter update check logic in CheckForSkeletalChanges ([1ac2a51](https://github.com/oculus-samples/Unity-Movement/commit/1ac2a51384658cb36e46525439dbad3085d626a8))
* **Runtime:** OnValidate function check should make sure editor is not playing ([f2ae6f7](https://github.com/oculus-samples/Unity-Movement/commit/f2ae6f747f76b4e459c6a6d7b59192d9b4416e4c))
* **Runtime:** Rebuilt skeletal constraint interface reference if constraint is added ([89e017f](https://github.com/oculus-samples/Unity-Movement/commit/89e017ffe0da640dce653dd13bef775477426d3e))
* **Runtime:** Regenerate masks instance if not set ([beceb70](https://github.com/oculus-samples/Unity-Movement/commit/beceb708b7538b0d46b3059a1a7adfaaae783f9b))
* **Runtime:** RetargetingConstraint uses AvatarMask instances ([fab08ca](https://github.com/oculus-samples/Unity-Movement/commit/fab08ca8fac1546a397d81311d7faada40658ef8))
* **Runtime:** Update animation rigging jobs to ignore Time.timeScale ([a7b719f](https://github.com/oculus-samples/Unity-Movement/commit/a7b719f8a6e6ed0ebd55b317bcb87740860b3a79))
* **Runtime:** Update deformation constraint to restore tracked hand positions ([c515630](https://github.com/oculus-samples/Unity-Movement/commit/c515630fa8a0aea1ae0c7a4de4462d80899b3f08))
* **Runtime:** Update deformation constraint to run with OVRCustomSkeleton ([b9ad911](https://github.com/oculus-samples/Unity-Movement/commit/b9ad911175c2c281d5e27a9cd59efe02f04279ad))
* **Runtime:** Update deformation constraint to take into account scale ([a6fe8ed](https://github.com/oculus-samples/Unity-Movement/commit/a6fe8ed528451cd76275d61257f88b6efb37e737))
* **Runtime:** Update transform handles ([1a51588](https://github.com/oculus-samples/Unity-Movement/commit/1a515887229b8fcfec2c93745a14162268eb1f48))


### Features

* **Runtime:** Add animator support to TwistDistributionConstraint ([930d473](https://github.com/oculus-samples/Unity-Movement/commit/930d473d72503695c25c728e71d341af91e25dc2))
* **Runtime:** Add RetargetingLayer skeleton postprocessing ([e6af064](https://github.com/oculus-samples/Unity-Movement/commit/e6af0644f814a398fada720f151c845aeb5ec034))
* **Runtime:** All animation rigging jobs use the weight field ([5999837](https://github.com/oculus-samples/Unity-Movement/commit/5999837e32dd64b9892fe61e0118e2273c56c4e9))
* **Runtime:** Apply animation rigging constraints to positions to correct in late update ([8d0e81d](https://github.com/oculus-samples/Unity-Movement/commit/8d0e81d294826e0d7f8a3033ac2a20c9827003bd))
* **Runtime:** Grounding constraint finds hips and computes offsets at edit time ([6c85e34](https://github.com/oculus-samples/Unity-Movement/commit/6c85e348051d7bf15df05467629b34e3e3f9f29c))
* **Runtime:** GroundingConstraint supports Animator; fix IsValid functions ([0a0a00c](https://github.com/oculus-samples/Unity-Movement/commit/0a0a00c9507f856865ea0c59bf04335a715309ed))
* **Runtime:** Retargeting job supports runtime mask changes ([d18f8f3](https://github.com/oculus-samples/Unity-Movement/commit/d18f8f32c8fc08eae135fe75dca6a30e7157ea0c))
* **Runtime:** Update deformation to optionally affect both arms and hands ([3534557](https://github.com/oculus-samples/Unity-Movement/commit/35345578818d92337812c48460496385122a0f52))
* **Samples:** Add twists for the neck in the blendshape mapping sample ([30d9b3b](https://github.com/oculus-samples/Unity-Movement/commit/30d9b3b79fe6aeee844e0f3c73487c88ec70a8c7))
* **Samples:** Update BlendshapeMapping character constraints ([e94e75a](https://github.com/oculus-samples/Unity-Movement/commit/e94e75a40fa30685f8e78557cc48998a890b4bd8))

# [2.2.0](https://github.com/oculus-samples/Unity-Movement/compare/v2.1.0...v2.2.0) (2023-05-30)


### Bug Fixes

* **Runtime:** Address retargeting job arrays edge case ([a0f6a92](https://github.com/oculus-samples/Unity-Movement/commit/a0f6a92155d6eb6e57e7416d7e315f151f874d7b))
* **Runtime:** Do not enable rig on focus if skeleton is not initialized ([fd099f1](https://github.com/oculus-samples/Unity-Movement/commit/fd099f166f229096e54472c48a405e8635a8c5be))


### Features

* **Editor:** Add CorrectivesFace menu ([abbacfd](https://github.com/oculus-samples/Unity-Movement/commit/abbacfd945f7447315397883c441c3e2bc753330))
* **Runtime:** Add tracking bone transforms by proxy ([c94580f](https://github.com/oculus-samples/Unity-Movement/commit/c94580f9c2c296f10fc5e16d402f4fa56f17b2d4))
* **Runtime:** Allow mirroring scale in LateMirroredObject ([80e0357](https://github.com/oculus-samples/Unity-Movement/commit/80e0357bcff0f8ee93a05ec2276c9057818f18ba))
* **Runtime:** Deformation constraint supports humanoid Animator ([ec91abf](https://github.com/oculus-samples/Unity-Movement/commit/ec91abfccc219f1ea82247388f9740f7152ddfd3))
* **Runtime:** Deformation constraint supports OVRSkeleton ([cb922d1](https://github.com/oculus-samples/Unity-Movement/commit/cb922d132c09cae11b06c2b90939aea56e0770ad))

# [2.1.0](https://github.com/oculus-samples/Unity-Movement/compare/v2.0.0...v2.1.0) (2023-05-23)


### Bug Fixes

* **Editor:** Update copyright headers for editor scripts ([a6ba69a](https://github.com/oculus-samples/Unity-Movement/commit/a6ba69aef97e9c413c7fc99d96ba728dd6fd1b98))
* **Runtime:** Fix copyright headers ([615beaa](https://github.com/oculus-samples/Unity-Movement/commit/615beaa188931d6a4cd166de9b16370380b37767))
* **Samples:** Clean up high fidelity model ([218009d](https://github.com/oculus-samples/Unity-Movement/commit/218009d9c28d6c664a9d4b3bcfb1658d601cd3c4))
* **Samples:** Old scene names use the word Legacy ([bbe1079](https://github.com/oculus-samples/Unity-Movement/commit/bbe10798977e6646f3bd1bcb05378bd840ae0f9d))


### Features

* **Runtime:** Add accessors for RetargetingLayer masks ([4eb4e99](https://github.com/oculus-samples/Unity-Movement/commit/4eb4e9900ae45eba3451c3080beae6a1b82efa36))
* **Runtime:** Allow animation rig to be disabled then re-enabled for OnApplicationFocus ([7d17c4c](https://github.com/oculus-samples/Unity-Movement/commit/7d17c4c44a87f8ac39137f35f44755204004cb30))
* **Samples:** Update blendshape modifiers and head in BlendshapeMappingExample ([3c0192c](https://github.com/oculus-samples/Unity-Movement/commit/3c0192c90f90a4b458ea01bf64142f22f68f8d14))

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
