// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace Oculus.Movement
{
    public static class DriveSkeletalLateUpdateLogicTooltips
    {
        public const string DeformationLogics =
            "Deformation logic components to drive.";

        public const string RecalculateNormals =
            "Normal recalculation components to drive.";

        public const string TwistDistributions =
            "Twist distribution components to drive.";

        public const string HipPinnings =
            "Hip pinning components to drive.";

        public const string Groundings =
            "Grounding logic components to drive.";
    }

    public static class DriveThirdPartySkeletonTooltips
    {
        public static class JointAdjustmentTooltips
        {
            public const string Joint =
                "Joint to adjust.";

            public const string Rotation =
                "Rotation to apply to the joint, post-retargeting.";

            public const string JointDisplacement =
                "Amount to displace the joint, based on percentage of distance to " +
                "the next joint.";

            public const string DisableRotationTransform =
                "Allows disable rotational transform on joint.";

            public const string DisablePositionTransform =
                "Allows disable positional transform on joint.";

            public const string BoneIdOverrideValue =
                "Allows mapping this human body bone to OVRSkeleton bone different from the " +
                "standard. An ignore value indicates to not override; remove means to exclude " +
                "from retargeting. Cannot be changed at runtime.";
        }

        public const string TargetAnimator =
            "Animator on target character that needs to be driven. Keep disabled.";

        public const string BodySectionsToAlign =
            "A list of body sections to align. While all bones should be " +
            "driven, only a subset will have their axes aligned with source.";

        public const string BodySectionsToPosition =
            "A list of body sections to fix the position of by matching against " +
            "the body tracking rig. The other bones can be fixed via IK. Back bones " +
            "differ among rigs, so be careful about aligning those.";

        public const string AnimatorTargetTPose =
            "Animator of target character in T-pose.";

        public const string UpdatePositions =
            "Whether we should update the positions of target character or not.";

        public const string OvrSkeleton =
            "OVRSkeleton component to query bind pose and bone values from.";

        public const string Adjustments =
            "Adjustments to apply to certain bones that need to be fixed " +
            "post-retargeting.";

        public const string BoneGizmosSource =
            "Allow visualization of gizmos for certain bones for source.";

        public const string BoneGizmosTarget =
            "Allow visualization of gizmos for certain bones for target.";

        public const string BoneGizmosSourceTPose =
            "Allow visualization of gizmos for certain bones for source T-pose.";

        public const string BoneGizmosTargetTPose =
            "Allow visualization of gizmos for certain bones for target T-pose.";

        public const string BodySectionsToRenderDebugText =
            "Body sections to show debug text.";

        public const string LineRendererSource =
            "Line renderer of source prefab, allows visual debugging in game view.";

        public const string LineRendererTarget =
            "Line renderer of target prefab, allows visual debugging in game view.";

        public const string BoneRendererSource =
            "Joint renderer prefab used for source.";

        public const string BoneRendererTarget =
            "Joint renderer prefab used for target.";

        public const string AxisRendererTargetPrefab =
            "Axes debug prefab, target.";

        public const string AxisRendererSourcePrefab =
            "Axes debug prefab, source.";

        public const string ShowDebugAxes =
            "Show or hide debug axes.";

        public const string DebugSkeletalViews =
            "Allows debugging of skeletal views.";

        public const string TargetRenderer =
            "Renderer of target character.";
    }

    public static class TogglesMenuTooltips
    {
        public const string FaceTrackingSystem =
            "Face tracking system to toggle features on.";

        public const string RecalculateNormals =
            "Recalculate normals component to control.";

        public const string DeformationLogic =
            "Deformation logic component to control.";

        public const string TwistDistributions =
            "Twist distribution components to control.";

        public const string RecalculateNormalsText =
            "Recalc normals text to update based on toggle state.";

        public const string CorrectivesText =
            "Correctives text to update based on toggle state";

        public const string DeformationText =
            "Deformation text to update based on toggle state";

        public const string TwistText =
            "Twist distribution text to update based on twist distribution toggle state";
    }

    public static class BlendshapesMenuToggleTooltips
    {
        public const string BlendShapesMenus =
            "Blend shapes menus to turn on/off.";
    }

    public static class BlendshapeMenuVisualTooltips
    {
        public const string OvrFaceExpressions =
            "Blend shapes menus to turn on/off.";

        public const string WorldText =
            "Text mesh pro visual for blendshape values.";

        public const string MinBlendshapeThreshold =
            "Threshold that blendshapes must passed before being rendered.";

        public const string FilterArray =
            " Can be used to filter blendshapes for rendering.";

        public const string ExpressionsPrefix =
            "Prefix for rendered text.";
    }

    public static class MirrorTransformationTooltips
    {
        public const string MirrorNormal =
            "Mirror normal, perpendicular to mirror face.";

        public const string MirrorPlaneOffset =
            "Allows mirror to be pushed into reflection plane somewhat," +
            " assuming mirror geometry has some thickness.";

        public const string TransformToMirror =
            "Transform to be reflected.";
    }

    public static class TransformMirroredEyesTooltips
    {
        public const string LeftEyeOriginal =
            "The original left eye that will be mirrored.";

        public const string LeftEyeMirrored =
            "The to-be-mirrored left eye.";

        public const string RightEyeOriginal =
            "The original right eye that will be mirrored.";

        public const string RightEyeMirrored =
            "The to-be-mirrored right eye.";
    }

    public static class CompareVectorHashesTooltips
    {
        public const string NumVerticesToTest =
            "Number of vertices to test with hashes.";

        public const string MarginOfError =
            "Margin of error used to generate random vertex positions.";
    }

    public static class RecalculateNormalsTooltips
    {
        public const string SkinnedMeshRenderer =
            "Skinned mesh renderer requiring normal recalc.";

        public const string SubMesh =
            "Submesh index to recalc normals on.";

        public const string EnableRecalculate =
            "Allows toggling recalculation on/off.";

        public const string UseUnityFunction =
            "Allows using Unity's stock recalc instead. Cannot change at runtime.";

        public const string RecalculateIndependently =
            "Allows recalculate normals to be calculated independently on LateUpdate, instead of being driven from DriveSkeletalLateUpdateLogic.";

        public const string DuplicateLayer =
            "The visible layer of the duplicate mesh with recalculate normals.";

        public const string HiddenMeshLayerName =
            "The invisible layer of original mesh with invalid normals";

        public const string RecalculateMaterialIndices =
            "Index of material that needs meta data for normal recalc.";
    }

    public static class GroundingLogicTooltips
    {
        public const string Skeleton =
            "The OVR Skeleton component.";

        public const string HipsTarget =
            "The hips target transform.";

        public const string GroundingLayers =
            "The layers that the raycast will check against for grounding.";

        public const string GroundRaycastDist =
            "The maximum distance that the raycast will go when checking for grounding.";

        public const string LeftLegProperties =
            "The leg properties for the left leg.";

        public const string RightLegProperties =
            "The leg properties for the right leg.";

        public static class LegPropertiesTooltips
        {
            public const string IkSolver =
                "The two-bone IK solver.";

            public const string InitialRotationOffset =
                "The initial rotation offset for the feet.";

            public const string StepDist =
                "The distance before the step is triggered.";

            public const string StepHeight =
                "The height of the step taken.";

            public const string StepSpeed =
                "The speed of the step for the foot.";

            public const string FloorOffset =
                "The height offset from the grounded floor to be applied to the foot.";

            public const string FootHeightScaleOnDist =
                "The maximum distance for the step height to not be scaled.";

            public const string LowerThresholdMoveProgress =
                "The lower bound of the move progress before the other foot can take a step.";

            public const string HigherThresholdMoveProgress =
                "The upper bound of the move progress before the other foot can take a step.";

            public const string StepCurve =
                "The animation curve for evaluating the step height value.";
        }
    }

    public static class TwoBoneIKTooltips
    {
        public const string UpperTransform =
            "The upper transform.";

        public const string MiddleTransform =
            "The middle transform.";

        public const string LowerTransform =
            "The lower transform.";

        public const string TargetTransform =
            "The target transform.";

        public const string PoleTransform =
            "The pole transform.";

        public const string TargetPosOffset =
            "The target position offset.";

        public const string TargetRotOffset =
            "The target rotation offset.";
    }

    public static class HipPinningCalibrationTooltips
    {
        public const string MainChairProp =
            "The hip pinning target for the main character.";

        public const string MirroredChairProp =
            "The hip pinning target for the mirrored character.";

        public const string MainCharacterHipPinning =
            "The hip pinning logic for the main character.";

        public const string MirroredCharacterHipPinning =
            "The hip pinning logic for the mirrored character.";

        public const string MainCharacterGrounding
            = "The grounding logic for main character.";

        public const string MirroredCharacterGrounding
            = "The grounding logic for mirrored character.";

        public const string MainCharacterRenderer =
            "The game object that contains the mesh renderers for the main character.";

        public const string MirroredCharacterRenderer =
            "The game object that contains the mesh renderers for the main character.";

        public const string DataProvider =
            "The tracking data provider for the main character.";

        public const string CalibrateMenu =
            "The game object that contains the renderers for this calibration menu.";
    }

    public static class HipPinningLogicTooltips
    {
        public const string EnableLegRotation =
            "If true, leg rotation will be enabled.";

        public const string EnableLegRotationLimits =
            "If true, leg rotation limits will be enabled.";

        public const string EnableConstrainedMovement =
            "If true, movement around the constrained surface will be enabled.";

        public const string EnableApplyTransformations =
            "If true, the entire body will be transformed to undo the offset applied by the hip pinning position constraint.";

        public const string HipPinningActive =
            "If true, hip pinning is currently active.";

        public const string HipPinningLeave =
            "If true, hip pinning will be disabled when the character leaves a certain range.";

        public const string HipPinningHeightAdjustment =
            "If true, hip pinning will adjust the height of the seat to match the tracked position.";

        public const string HipPinningLeaveRange =
            "The range from the hip pinning target before hip pinning is disabled.";

        public const string HipPinningTargets =
            "The list of hip pinning targets in the scene.";

        public const string Skeleton =
            "The OVR Skeleton component.";

        public const string DataProvider =
            "The tracking data provider.";

        public static class HipPropertiesTooltips
        {
            public const string BodyJointProperty =
                "Body joint property for hip pinning.";

            public const string PositionConstraint =
                "Position constraint for the specified joint for hip pinning.";

            public const string RotationLimit =
                "Rotation limit for an axis from the hips for hip pinning.";
        }

        public static class BodyJointPropertiesTooltips
        {
            public const string BodyJoint =
                "The body joint";

            public const string InitialLocalRotation =
                "The initial local rotation of the body joint.";

            public const string ConstraintWeight =
                "The positional constraint weight for this body joint.";

            public const string OffsetWeight =
                "The weight applied for the offsets applied by hip pinning.";

            public const string PositionDistanceThreshold =
                "The distance before offsets stop getting applied to this body joint.";

            public const string PositionDistanceWeight =
                "The weight of the constraint scaling with distance when offsets stop getting applied to this body joint.";
        }
    }

    public static class HipPinningTargetTooltips
    {
        public const string ChairObject =
            "The game object containing the renderers for this hip pinning target.";

        public const string HipTargetTransform =
            "The transform that the character's hip is positionally constrained to.";

        public const string ChairSeat =
            "The chair's seat transform.";

        public const string ChairCylinder =
            "The chair's cylinder transform.";

        public const string ChairCylinderScaleMultiplier =
            "The chair's cylinder scale multiplier.";

        public const string SpineTargetTransforms =
            "The transforms that the character's spine bones are positionally constrained to.";

        public const string ConstrainedArea =
            "The amount of constrained movement allowed from this hip pinning target.";

        public const string ConstrainedAreaOffset =
            "The offset to be applied to the center of where the constrained area will be.";
    }

    public static class HipPinningMenuTooltips
    {
        public const string MainCharacter
            = "Main character driven by user.";

        public const string MirroredCharacter
            = "Mirrored character.";

        public const string Text
            = "Informational text.";

        public const string BodyParts
            = "Body parts that can be toggled.";

        public static class BodyPartTooltips
        {
            public const string BodyPartObject =
                "The body part object.";

            public const string BodyPartName =
                "The name of the body part.";

            public const string Enabled =
                "If true, the body part is active and visible.";
        }
    }

    public static class CorrectiveShapesDriverTooltips
    {
        public const string SkinnedMeshRendererToCorrect =
            "The skinned mesh renderer that contains the blendshapes to be corrected.";

        public const string CombinationShapesTextAsset =
            "The json file containing the in-betweens and combinations data.";

        public static class InBetweenTooltips
        {
            public const string DrivenIndex =
                "The blendshape index to be driven on the skinned mesh renderer.";

            public const string DriverIndex =
                "The target blendshape index used for calculating the blendshape weight.";

            public const string Slope =
                "The slope from the function curve of the in-between.";

            public const string OffsetX =
                "The x offset from the function curve of the in-between.";

            public const string OffsetY =
                "The y offset from the function curve of the in-between.";

            public const string DomainStart =
                "The domain range start from the function curve of the in-between.";

            public const string DomainEnd =
                "The domain range end from the function curve of the in-between.";
        }

        public static class CombinationTooltips
        {
            public const string DrivenIndex =
                "The blendshape index to be driven on the skinned mesh renderer.";

            public const string DriverIndices =
                "The blendshape indices used in calculating the blendshape weight for the driven index.";
        }

        public static class RigLogicDataTooltips
        {
            public const string InBetweens =
                "Array of all of the in-betweens data.";

            public const string Combinations =
                "Array of all of the combinations data.";
        }
    }

    public static class CorrectivesModuleTooltips
    {
        public static class InBetweenTooltips
        {
            public const string DrivenIndex =
                "The blendshape index to be driven on the skinned mesh renderer.";

            public const string DriverIndex =
                "The target blendshape index used for calculating the blendshape weight.";

            public const string Slope =
                "The slope from the function curve of the in-between.";

            public const string OffsetX =
                "The x offset from the function curve of the in-between.";

            public const string OffsetY =
                "The y offset from the function curve of the in-between.";

            public const string DomainStart =
                "The domain range start from the function curve of the in-between.";

            public const string DomainEnd =
                "The domain range end from the function curve of the in-between.";
        }

        public static class CombinationTooltips
        {
            public const string DrivenIndex =
                "The blendshape index to be driven on the skinned mesh renderer.";

            public const string DriverIndices =
                "The blendshape indices used in calculating the blendshape weight for the driven index.";
        }

        public static class RigLogicDataTooltips
        {
            public const string InBetweens =
                "Array of all of the in-betweens data.";

            public const string Combinations =
                "Array of all of the combinations data.";
        }
    }

    public static class BlendshapeMappingTooltips
    {
        public const string Meshes =
            "List of all mesh mappings - supported blendshapes on meshes on this character.";

        public static class MeshMappingTooltips
        {
            public const string Mesh =
                "The skinned mesh renderer that has blendshapes.";

            public const string Blendshapes =
                "List of all supported blendshapes on this skinned mesh renderer.";
        }
    }

    public static class FaceTrackingSystemTooltips
    {
        public const string BlendshapeMapping =
            "Blendshape mapping component.";

        public const string OVRFaceExpressions =
            "OVR face expressions component.";

        public const string CorrectiveShapesDriver =
            "Optional corrective shapes driver component.";

        public const string BlendshapeModifier =
            "Optional blendshape modifier component.";
    }

    public static class CorrectivesFaceTooltips
    {
        public const string BlendshapeModifier =
            "Optional blendshape modifier component.";

        public const string CombinationShapesTextAsset =
            "The json file containing the in-betweens and combinations data.";
    }

    public static class FacialExpressionDetectorTooltips
    {
        public static class ExpressionThresholdsTooltips
        {
            public const string FaceExpression =
                "Face expression blendshape to evaluate.";

            public const string EntryThreshold =
                "Threshold to enter expression, usually high.";

            public const string ExitThreshold =
                "Threshold to exit expression, usually low.";
        }

        public static class ExpressionDataTooltips
        {
            public const string Thresholds =
                "Array of thresholds.";

            public const string ExpressionTypeDetected =
                "Expression type (macro) detected.";
        }

        public const string OvrFaceExpressions =
            "OVRFaceExpressions component.";

        public const string ExpressionsToEvaluate =
            "Array of macro expressions test for.";
    }

    public static class SmileEffectTooltips
    {
        public const string SmileEnabled =
            "Returns the current state of if smile is enabled or disabled.";

        public const string FacialExpressionDetector =
            "Facial expression detector to query events from.";

        public const string MaterialIndex =
            "Material index to modify.";

        public const string Renderer =
            "Renderer of the face.";

        public const string GlowCurve =
            "Glow curve that modulates emission strength on face.";

        public const string Animator =
            "Petal animator to affect when smiling.";

        public const string SmileDelay =
            "Delay until smile gets triggered (seconds).";

        public const string SmileStateNake =
            "State name for smile.";

        public const string ReverseSmileStateName =
            "State name for reverse smile (when it \"undoes\" itself).";
    }

    public static class ScreenshotFaceExpressionsTooltips
    {
        public const string Camera =
            "The camera which its viewport will be used to take the screenshot.";

        public const string Mapping =
            "The target blendshape mapping.";

        public const string Correctives =
            "The target corrective shapes driver.";

        public const string ScreenshotWidth =
            "The width of the screenshot texture.";

        public const string ScreenshotHeight =
            "The height of the screenshot texture.";

        public const string ScreenshotNeutral =
            "If true, take a screenshot of the viewport without any blendshapes.";

        public const string ScreenshotFolder =
            "The path to the screenshots folder.";
    }

    public static class ScreenshotFaceExpressionsCaptureTooltips
    {
        public const string CorrectivesFaceComponents =
            "All face components to drive while capturing screenshots.";

        public const string Camera =
            "The camera which its viewport will be used to take the screenshot.";

        public const string ScreenshotWidth =
            "The width of the screenshot texture.";

        public const string ScreenshotHeight =
            "The height of the screenshot texture.";

        public const string ScreenshotNeutral =
            "If true, take a screenshot of the viewport without any blendshapes.";

        public const string ScreenshotFolder =
            "The path to the screenshots folder.";
    }

    public static class BlendshapeModifierTooltips
    {
        public static class FaceExpressionModifier
        {
            public const string FaceExpressions =
                "The facial expressions that will be modified.";

            public const string MinValue =
                "The minimum clamped blendshape weight for this set of facial expressions.";

            public const string MaxValue =
                "The maximum clamped blendshape weight for this set of facial expressions.";

            public const string Multiplier =
                "The blendshape weight multiplier for this set of facial expressions.";
        }

        public const string FaceExpressionsModifiers =
            "The array of facial expression modifier data to be used.";

        public const string DefaultBlendshapeModifierPreset =
            "Optional text asset containing the array of face expression modifier data to be used.";

        public const string GlobalMultiplier =
            "Global blendshape multiplier.";

        public const string GlobalMin =
            "Global blendshape clamp min.";

        public const string GlobalMax =
            "Global blendshape clamp max.";

        public const string ApplyGlobalClampingNonMapped =
            "Global blendshape clamp max.";
    }

    public static class MirrorSkeletonTooltips
    {
        public const string SkeletonToCopy =
            "The camera which its viewport will be used to take the screenshot.";

        public const string MySkeleton =
            "The target blendshape mapping.";
    }

    public static class HandDeformationTooltips
    {
        public static class FingerOffset
        {
            public const string FingerId =
                "The id of the finger to apply the offset to.";

            public const string FingerPosOffset =
                "The finger position offset.";

            public const string FingerRotOffset =
                "The finger rotation offset.";
        }

        public const string CustomSkeleton =
            "Custom skeleton to reference.";

        public const string CopyFingerDataInUpdate =
            "If true, copy the finger offsets data into FingerInfo during every update.";

        public const string FingerOffsets =
            "Offsets that will be applied to the fingers.";

        public const string Animator =
            "The character's animator.";

        public const string Skeleton =
            "The source skeleton.";

        public const string LeftHand =
            "The character's left hand bone.";

        public const string RightHand =
            "The character's right hand bone.";

        public const string InterpolatedFingers =
            "Possible metacarpal bones.";

        public const string Fingers =
            "All finger joints.";

        public const string CalculateFingerData =
            "If finger data has been calculated or not.";
    }

    public static class DeformationLogicTooltips
    {
        public static class BoneDistanceInfo
        {
            public const string StartBoneTransform =
                "The start bone transform.";

            public const string EndBoneTransform =
                "The end bone transform.";
        }

        public static class ArmPositionInfo
        {
            public const string Shoulder =
                "The shoulder transform.";

            public const string UpperArm =
                "The upper arm transform.";

            public const string LowerArm =
                "The lower arm transform.";

            public const string Weight =
                "The weight of the offset position.";

            public const string MoveSpeed =
                "The speed of the arm move towards if enabled.";
        }

        public const string Skeleton =
            "The OVR Skeleton component.";

        public const string MirrorSkeleton =
            "The Mirror Skeleton component.";

        public const string Animator =
            "Animator component. Setting this will cause this script " +
            "to ignore the skeleton field..";

        public const string SpineTranslationCorrectionType =
            "The type of spine translation correction that should be applied.";

        public const string LeftArmPositionInfo =
            "The position info for the left arm.";

        public const string RightArmPositionInfo =
            "The position info for the right arm.";

        public const string FixArms =
            "Fix arms toggle.";

        public const string CorrectSpineOnce =
            "Allows the spine correction to run only once, assuming the skeleton's positions don't get updated multiple times.";

        public const string MoveTowardsArms =
            "If true, the arms will move towards the deformation target position.";
    }

    public static class TwistDistributionTooltips
    {
        public const string GlobalWeight =
            "The global weight of the twist joints.";

        public const string SegmentStart =
            "The start transform on the opposite side of the twist source (like an elbow).";

        public const string SegmentEnd =
            "The target transform containing the twist (like a wrist).";

        public const string SegmentEndUpTransform =
            "Optional. Assign a different transform to be used for the Segment End up vector.";

        public const string TwistJoints =
            "The list of twist joints to affect by the source transform's rotation.";

        public const string TwistForwardAxis =
            "The forward axis for the twist joints, one that points along the twist axis toward segment end.";

        public const string TwistUpAxis =
            "The up axis for the twist joints, one that matches the segment end up axis.";

        public static class TwistJointTooltips
        {
            public const string Joint =
                "The twist joint transform.";

            public const string Weight =
                "The weight of the source transform's rotation on the twist joint.";
        }
    }

    public static class ScriptEffectToggleTooltips
    {
        public const string ComponentToToggle =
            "The component to be toggled.";

        public const string TextToUpdate =
            "The text component to be updated when the component is toggled.";

        public const string FeatureString =
            "The feature text.";
    }

    public static class SceneSelectIconTooltips
    {
        public static class IconPositionInformation
        {
            public const string ButtonTransform = "Button transform.";
            public const string SceneName = "Scene name to check for.";
        }

        public const string IconInformationArray =
            "Icon positions array.";
        public const string IconTransform =
            "Icon transform to affect.";
    }

    public static class ButtonToggleIconTooltips
    {
        public const string OutlineObject =
            "Outline object that indicates toggle state.";

        public const string SelectColor =
            "Select color.";

        public const string DeselectColor =
            "Deselected color.";
    }

    public static class HipPinningNotificationTooltips
    {
        public const string HipPinningLogic =
            "The hip pinning logic component.";

        public const string DisplayTime =
            "The amount of time that this notification should be enabled for.";
    }

    public static class DeformationDataTooltips
    {
        public const string DeformationBodyType =
            "The deformation body type for the character.";

        public const string CustomSkeleton =
            "The OVRCustomSkeleton component for the character.";

        public const string Animator =
            "The animator component.";

        public const string SpineTranslationCorrectionType =
            "The type of spine translation correction that should be applied.";

        public const string SpineAlignmentWeight =
            "The weight for the spine alignment.";

        public const string ChestAlignmentWeight =
            "The weight for the chest alignment.";

        public const string ShouldersHeightReductionWeight =
            "The weight for the shoulders height reduction.";

        public const string ShouldersWidthReductionWeight =
            "The weight for the shoulders width reduction.";

        public const string AffectArmsBySpineCorrection =
            "True if arms should be affected by spine correction.";

        public const string LeftShoulderWeight =
            "The weight for the deformation on the left shoulder.";

        public const string RightShoulderWeight =
            "The weight for the deformation on the right shoulder.";

        public const string LeftArmWeight =
            "The weight for the deformation on the left arm.";

        public const string RightArmWeight =
            "The weight for the deformation on the right arm.";

        public const string LeftHandWeight =
            "The weight for the deformation on the left hand.";

        public const string RightHandWeight =
            "The weight for the deformation on the right hand.";

        public const string SquashLimit =
            "Prevents character from squashing too much. " +
            "WARNING: reducing this reduces perceived body tracking accuracy.";

        public const string StretchLimit =
            "Prevents character from stretching too much. " +
            "WARNING: reducing this reduces perceived body tracking accuracy.";

        public const string AlignLeftLegWeight =
            "The weight for the alignment on the left leg.";

        public const string AlignRightLegWeight =
            "The weight for the alignment on the right leg.";

        public const string LeftToesWeight =
            "The weight for the deformation on the left toe.";

        public const string RightToesWeight =
            "The weight for the deformation on the right toe.";

        public const string AlignFeetWeight =
            "Weight used for feet alignment.";

        public const string OriginalSpinePositionsWeight =
            "Attempts to match the original spine positions. WARNING: " +
            " increasing this value might cause inaccuracy wrt to body tracking.";

        public const string ArmLengthMultiplier =
            "Allows stretching arms. WARNING:" +
            "increasing this value might cause inaccuracy wrt to body tracking.";

        public const string OriginalSpineBoneCount =
            "Number of spine bones to fix when matching the original spine.";

        public const string OriginalSpineUseHipsToHeadToScale =
            "When using the original spine bone positions to influence " +
            "the current ones, scale them based on the current hips to head.";

        public const string OriginalSpineFixRotations =
            "Allows rotation correction when using the original spine bone " +
            "positions.";

        public const string HipsToHeadBones =
            "Array of transform bones from hips to head.";

        public const string HipsToHeadBoneTargets =
            "Array of transform bone targets from hips to head.";

        public const string FeetToToesBoneTargets =
            "Array of transform bone targets from feet to toes.";

        public const string LeftArmData =
            "Left arm data.";

        public const string RightArmData =
            "Right arm data.";

        public const string LeftLegData =
            "Left leg data.";

        public const string RightLegData =
            "Right leg data.";

        public const string BonePairData =
            "All bone pair data.";

        public const string BoneAdjustmentData =
            "All bone adjustment data.";

        public const string StartingScale =
            "Starting scale of character.";

        public const string HipsToHeadDistance =
            "Distances between head and hips.";

        public const string HipsToFootDistance =
            "Distances between hips and feet.";
    }

    public static class FullBodyDeformationConstraintToolTips
    {
        public const string CalculateBoneData =
            "Allows calculating bone data via a button.";
    }

    public static class HipPinningDataTooltips
    {
        public const string Skeleton =
            "The OVR Skeleton component.";

        public const string Animator =
            "Animator component.";

        public const string HipPinningTargets =
            "The list of hip pinning targets in the scene.";

        public const string HipPinningHeightAdjustment =
            "If true, hip pinning will adjust the height of the seat to match the tracked position.";

        public const string HipPinningLeave =
            "If true, hip pinning will be disabled when the character leaves a certain range.";

        public const string HipPinningLeaveRange =
            "The range from the hip pinning target before hip pinning is disabled.";
    }

    public static class CaptureAnimationDataTooltips
    {
        public const string ConstraintAnimator =
            "The animator used by the constraint.";

        public const string TargetAnimatorLayer =
            "The target animator layer to capture animations on.";

        public const string ReferencePoseTime =
            "The normalized time from which the reference pose should be captured from.";

        public const string ReferencePose =
            "The bone data for the reference pose.";

        public const string CurrentPose =
            "The bone data for the current pose.";
    }

    public static class PlaybackAnimationDataTooltips
    {
        public const string AnimationPlaybackType =
            "The animation playback type.";

        public const string SourceConstraint =
            "The capture animation constraint to source animation data from.";

        public const string AvatarMask =
            "The avatar mask for masking the animation.";

        public const string AffectPositions =
            "Affect positions via the animation.";

        public const string AffectRotations =
            "Affect rotations via the animation.";

        public const string BonesArrayMask =
            "Bones to mask by array.";

        public const string FixedHipsPosition =
            "Allows setting hips to a fixed position.";

        public const string FixedHipsRotation =
            "Allows setting hips to a fixed rotation.";

        public const string UsedFixedHipsPose =
            "Used fixed hips pose or not.";

        public const string AffectHipsPositionY =
            "Affect hips position Y value.";

        public const string AffectHipsRotationX =
            "Affect hips rotation X value.";

        public const string AffectHipsRotationY =
            "Affect hips rotation Y value.";

        public const string AffectHipsRotationZ =
            "Affect hips rotation Z value.";
    }

    public static class AnimationRigSetupTooltips
    {
        public const string Skeleton =
            "Skeletal component of character.";

        public const string Animator =
            "Animator component of character.";

        public const string RigBuilder =
            "Rig builder on character supporting Animation rigging.";

        public const string OVRSkeletonConstraints =
            "IOVRSkeletonConstraint-based components.";

        public const string RebindAnimator =
            "If true, rebind the animator upon a skeletal change.";

        public const string ReEnableRig =
            "If true, disable then re-enable the rig upon a skeletal change.";

        public const string RigToggleOnFocus =
            "If true, disable then re-enable the rig upon a focus change.";

        public const string RetargetingLayer =
            "Retargeting layer component to get data from.";

        public const string CheckSkeletalUpdatesByProxy =
            "Use proxy transforms to check skeletal changes. " +
            "Proxy transforms can be used in case the original " +
            "skeleton updates too much.";
    }

    public static class HipPinningConstraintCalibrationTooltips
    {
        public const string MainHipPinningTargetRenderer =
            "The game object that contains the mesh renderers for the main hip pinning target.";

        public const string MirrorHipPinningTargetRenderer =
            "The game object that contains the mesh renderers for the mirrored hip pinning target.";

        public const string HipPinningConstraints =
            "The hip pinning constraints.";

        public const string MainCharacterRenderer =
            "The game object that contains the mesh renderers for the main character.";

        public const string MirroredCharacterRenderer =
            "The game object that contains the mesh renderers for the main character.";

        public const string Skeleton =
            "The skeletal tracking data provider for the interface character.";

        public const string CalibrateMenu =
            "The game object that contains the renderers for this calibration menu.";
    }

    public static class HipPinningConstraintNotificationTooltips
    {
        public const string HipPinningConstraint =
            "The hip pinning constraint.";

        public const string DisplayTime =
            "The amount of time that this notification should be enabled for.";
    }

    public static class GroundingDataTooltips
    {
        public const string Skeleton =
            "The OVR Skeleton component for the character.";

        public const string Animator =
            "The Animator component for the character.";

        public const string Pair =
            "Optional. The other leg's grounding constraint, used to check if this leg can move.";

        public const string GroundingLayers =
            "The layers that the raycast will check against for grounding.";

        public const string GroundRaycastDist =
            "The maximum distance that the raycast will go when checking for grounding.";

        public const string GroundOffset =
            "The height offset from the grounded floor to be applied to the foot.";

        public const string HipsTarget =
            "The hips target transform.";

        public const string KneeTarget =
            "The knee target for the leg.";

        public const string FootTarget =
            "The foot target for the leg.";

        public const string Leg =
            "The leg transform.";

        public const string Foot =
            "The foot transform.";

        public const string Hips =
            "The hips transform.";

        public const string FootRotationOffset =
            "The initial rotation offset for the feet.";

        public const string StepCurve =
            "The animation curve for evaluating the step height value.";

        public const string StepDist =
            "The distance before the step is triggered.";

        public const string StepSpeed =
            "The speed of the step for the foot.";

        public const string StepHeight =
            "The height of the step taken.";

        public const string StepHeightScaleDist =
            "The maximum distance for the step height to not be scaled.";

        public const string MoveLowerThreshold =
            "The lower bound of the move progress before the other foot can take a step.";

        public const string MoveHigherThreshold =
            "The upper bound of the move progress before the other foot can take a step.";
    }

    public static class TwistDistributionDataTooltips
    {
        public const string Skeleton =
            "The OVR Skeleton component for the character.";

        public const string Animator =
            "The Animator component for the character.";

        public const string SegmentStart =
            "The start transform on the opposite side of the twist source (like an elbow).";

        public const string SegmentUp =
            "Optional. Assign a different transform to be used for the Segment End up vector.";

        public const string SegmentEnd =
            "The target transform containing the twist (like a wrist).";

        public const string TwistNodes =
            "The list of twist nodes to affect by the source transform's rotation.";

        public const string TwistForwardAxis =
            "The forward axis for the twist joints, one that points along the twist axis toward segment end.";

        public const string TwistUpAxis =
            "The up axis for the twist joints, one that matches the segment end up axis.";

        public const string InvertForwardAxis =
            "If true, invert the forward axis.";

        public const string InvertUpAxis =
            "If true, invert the up axis.";
    }

    public static class CopyPoseDataTooltips
    {
        public const string Animator =
            "The Animator component for the character.";

        public const string CopyPoseToOriginal =
            "True if the pose being copied is the original pose. If false, the copied pose " +
            "is assumed to be the final pose.";

        public const string RetargetingLayer =
            "Retargeting layer component to get data from.";
    }

    public static class RetargetingConstraintDataTooltips
    {
        public const string RetargetingLayer =
            "Retargeting layer component to get data from.";

        public const string AllowDynamicAdjustmentsRuntime =
            "Allow dynamic adjustments at runtime. Editor-only.";

        public const string AvatarMask =
            "Avatar mask to restrict retargeting. While the humanoid retargeter " +
            "class has similar fields, this one is easier to use.";

        public const string SourceTransforms =
            "Source transforms used for retargeting.";

        public const string TargetTransforms =
            "Target transforms affected by retargeting.";

        public const string ShouldUpdatePositions =
            "Indicates if target transform's position should be updated. " +
            "Once a position is updated, the original position will be lost.";

        public const string ShouldUpdateRotations =
            "Indicates if target transform's rotation should be updated. " +
            "Once a rotation is updated, the original rotation will be lost.";

        public const string RotationOffsets =
            "Rotation offset to be applied during retargeting.";

        public const string RotationAdjustments =
            "Optional rotational adjustment to be applied during retargeting.";
    }

    public static class RetargetingLayerTooltips
    {
        public const string CorrectPositionsLateUpdate =
            "Allows correcting positions in LateUpdate for accuracy.";

        public const string LeftHandCorrectionWeightLateUpdate =
            "Allow correcting rotations in LateUpdate. This can produce more " +
            "accurate hands, for instance.";

        public const string RightHandCorrectionWeightLateUpdate =
            "Allow correcting rotations in LateUpdate. This can produce more " +
            "accurate hands, for instance.";

        public const string ShoulderCorrectionWeightLateUpdate =
            "Allow correcting shoulder transforms in LateUpdate. This can produce more " +
            "accurate shoulders, for instance.";

        public const string HandIKType =
            "The type of IK that should be applied to modify the arm bones toward the " +
            "correct hand target.";

        public const string UseWorldHandPosition =
            "If true, use the world hand position for placing the hand instead of the scaled position.";

        public const string UseCustomHandTargetPosition =
            "If true, use the custom hand target position for the target position.";

        public const string UseSecondaryBoneId =
            "If true, use the secondary bone position before solving for the target position.";

        public const string CustomHandTargetPosition =
            "The custom hand target position.";

        public const string MaxHandStretch =
            "The maximum stretch for the hand to reach the target position that is allowed.";

        public const string MaxShoulderStretch =
            "The maximum stretch for the shoulder to help the hand reach the target position that is allowed.";

        public const string IKTolerance =
            "The maximum distance between the resulting position and target position that is allowed.";

        public const string IKIterations =
            "The maximum number of iterations allowed for the IK algorithm..";

        public const string ApplyAnimationConstraintsToCorrectedPositions =
            "Apply position offsets done by animation rigging constraints for corrected " +
            "positions. Due to the limited motion of humanoid avatars, this should be set if any " +
            "animation rigging constraints are applied after the retargeting job runs.";

        public const string EnableTrackingByProxy =
            "Create proxy transforms that track the skeletal bones. If the " +
            "skeletal bone transforms change, that won't necessitate creating new " +
            "proxy transforms in most cases.";

        public const string RetargetingAnimationConstraint =
            "Related retargeting constraint.";

        public const string RetargetingProcessors =
            "List of retargeting processors, which run in late update after retargeting and animation rigging.";

        public const string RetargetingAnimationRig =
            "Retargeting animation rig to be updated based on body tracking.";

        public const string ExternalBoneTargets =
            "External bone targets to be updated based on body tracking.";

        public const string RetargetedBoneMappings =
            "Retargeted bone mappings to be updated based on valid bones in the humanoid.";

        public const string FingerPositionCorrectionWeight =
            "Finger position correction weight.";

        public const string RegenJobData =
            "Regenerate job data.";

        public const string ProcessorType =
            "Whether to use jobs or not.";
    }

    public static class LateMirroredObjectTooltips
    {
        public static class MirroredTransformPairTooltips
        {
            public const string OriginalTransform =
                "The original transform.";

            public const string MirroredTransform =
                "The mirrored transform.";
        }

        public const string TransformToCopy =
            "The transform which transform values are being mirrored from.";

        public const string MyTransform =
            "The target transform which transform values are being mirrored to.";

        public const string MirroredTransformPairs =
            "The array of mirrored transform pairs.";

        public const string MirrorScale =
            "Mirror scale.";
    }

    public static class LateMirroredSkeletonTooltips
    {
        public static class MirroredBonePairTooltips
        {
            public const string ShouldBeReparented =
                "If true, this mirrored bone should be reparented to match the original bone.";

            public const string OriginalBone =
                "The original transform.";

            public const string MirroredBone =
                "The mirrored transform.";
        }

        public const string SkeletonToCopy =
            "The skeleton which transform values are being mirrored from.";

        public const string MySkeleton =
            "The target skeleton which transform values are being mirrored to.";

        public const string MirroredBonePairs =
            "The array of mirrored bone pairs.";
    }

    public static class RetargetedBoneTargetsTooltips
    {
        public static class RetargetedBoneTargetTooltips
        {
            public const string HumanBodyBone =
                "The human body bone representation of this bone.";

            public const string Target =
                "The target transform to update with the retargeted bone data.";

            public const string PositionOffset =
                "The position offset from the target transform.";
        }

        public const string RetargetedBoneTargets =
            "The array of retargeted bone targets.";
    }

    public static class RetargetingMenuTooltips
    {
        public const string CharacterToSpawn =
            "Main character prefab to spawn.";

        public const string SpawnParent =
            "Parent to spawn under.";

        public const string SpawnOffset =
            "Offset per spawn.";

        public const string RestPoseObject =
            "The rest pose humanoid object.";

        public const string RestTPoseObject =
            "The rest T-pose humanoid object.";

        public const string TPoseMask =
            "Positions to correct mask, intended to set certain joints of " +
            "animation rigged characters to T-Pose during retargeting.";
    }

    public static class AnimatorBoneVisualizerTooltips
    {
        public const string AnimatorComp =
            "Animator component to visualize bones for.";
    }

    public static class OVRSkeletonBoneVisualizerTooltips
    {
        public const string OVRSkeletonComp =
            "OVRSkeleton component to visualize bones for.";

        public const string VisualizeBindPose =
            "Whether to visualize bind pose or not.";
    }

    public static class BoneVisualizerTooltips
    {

        public const string WhenToRender =
            "When to render this skeleton during the Unity gameloop.";

        public const string VisualizationGuideType =
            "The type of guide used to visualize bones.";

        public const string MaskToVisualize =
            "Mask to use for visualization.";

        public const string BoneVisualData =
            "Bone collection to use for visualization.";

        public const string LineRendererPrefab =
            "Line renderer to use for visualization.";

        public const string AxisRendererPrefab =
            "Axis renderer to use for visualization.";

        public const string VisualType =
            "Indicates what kind of visual is desired.";
    }

    public static class BoneVisualizerLineColorTooltips
    {
        public const string BoneVisualizer =
            nameof(Utils.BoneVisualizer) + " to change the color of";

        public const string LineColor =
            "The color to change the " + nameof(Utils.BoneVisualizer) + " to";
    }

    public static class BlendHandConstraintsTooltips
    {
        public const string Constraints =
            "Constraints to control the weight of.";

        public const string RetargetingLayer =
            "The character's retargeting layer.";

        public const string BoneIdToTest =
            "Bone ID, usually the wrist. Can be modified depending " +
            "on the skeleton used.";

        public const string HeadTransform =
            "Head transform to do distance checks against.";

        public const string AutoAddTo =
            "MonoBehaviour to add to.";

        public const string ConstraintsMinDistance =
            "Distance where constraints are set to 1.0.";

        public const string ConstraintsMaxDistance =
            "Distance where constraints are set to 0.0.";

        public const string BlendMultiplier =
            "Multiplier that influences weight interpolation based on distance.";

        public const string MaxWeight =
            "Max constraint weight.";
    }

    public static class CustomAnimToggleTooltips
    {
        public const string AnimClip =
            "Animation clip to play.";
        public const string CustomMask =
            "Mask to apply.";
        public const string RetargetingConstraints =
            "Retargeting constraints to fix based on animation state.";
        public const string Animators =
            "Animators to control.";
        public const string CustomAnimEnabled =
            "True if animation is enabled, false is not.";
        public const string WorldText =
            "Text to update to based on animation state.";
        public const string AnimParamName =
            "Animator parameter name.";
    }

    public static class SkeletonHandAdjustmentTooltips
    {
        public const string Hand =
            "ISDK Hand source. Can be Synthetic hand. First hand in list that is active and enabled is used.";

        public const string CameraRig =
            "Should be the OVRCameraRig, which ISDK hands will offset to follow";

        public const string HandsAreOffset =
            "If the camera rig moves but the body is stationary, this should be true.\n" +
            "If the camera rig and body move together, this can stay false.";
    }

    public static class ActivateToggleTooltips
    {
        public const string Index =
            "Which state is set from state list";

        public const string WrapIndex =
            "Whether or not Next/Prev will wrap back around after the limit";

        public const string States =
            "The list of triggerable states";

        public const string OnSetNameChange =
            "When the Index changes, these callbacks will be called with the name of the triggered state";

        public const string Set_Name =
            "Name of this state";

        public const string Set_Ignored =
            "If true, this state will be ignored by Prev() and Next() methods";

        public const string Set_ObjectsToActivate =
            "Objects that will activate and deactivate with this state";
    }

    public static class TransformsFollowTooltips
    {
        public const string FollowingTransforms =
            "Other Transforms that will follow this Transform";
    }

    public static class AnimationConstraintMaskerTooltips
    {
        public const string Mask =
            "Section of body where body tracking continues animating while activity triggers";

        public const string ActivityExitTime =
            "Seconds of inactivity till entire animator follows body tracking again";

        public const string Animator =
            "The body being animated";

        public const string ConstraintsToDeactivate =
            "Partial names of constraints to deactivate when animation masking is active. For"
            + "example, {\"_Leg\"} will disable all rig constraints with \"_Leg\" in the name.";
    }

    public static class AnimationConstraintBlenderTooltips
    {
        public const string ActivityExitTime =
            "Seconds of inactivity till entire animator follows body tracking again.";

        public const string Animator =
            "The body being animated.";

        public const string ConstraintsToDeactivate =
            "Constraints to deactivate when animation is active.";

        public const string ConstraintsToBlend =
            "Constraints to blend when animation is active.";
    }

    public static class AnimatorHooksTooltips
    {
        public const string AutoAssignAnimatorsFromChildren =
            "If true, Animators field will be dynamically set";

        public const string Animators =
            "Animators who should receive signals to animate";

        public const string MaxInputAcceleration =
            "Max input acceleration.";
    }

    public static class JumpingRigidbodyTooltips
    {
        public const string Rigidbody =
            "Should reference the main Rigidbody of the character controller";

        public const string TargetJumpHeight =
            "How many units high the character controller should jump";

        public const string CanOnlyJumpOnGround =
            "Prevents jump impulse if the character controller is not grounded";

        public const string JumpEvents =
            "Callbacks to trigger at certain stages of jumping";

        public const string FloorLayerMask =
            "These collision layers will be checked for collision as valid ground to jump from";
    }

    public static class MovementSDKLocomotionTooltips
    {
        public const string Rigidbody =
            "Should reference the main Rigidbody of the character controller";

        public const string Collider =
            "The Collider that counts as this character's feet";

        public const string EnableMovement =
            "Joystick will move forward and backward, as well as strafe left and right at Speed";

        public const string EnableRotation =
            "Joystick will turn according to RotationAngle";

        public const string ScaleInputByActualVelocity =
            "If Input given to input events reflects obstacles hampering velocity";

        public const string RotationAngle =
            "Default snap turn amount.";

        public const string RotationPerSecond =
            "Default turn speed, when rotating smoothly";

        public const string Speed =
            "How quickly the controller will move with fully extended joystick";

        public const string CameraRig =
            "The Camera Rig";

        public const string MovementEvents =
            "Callbacks to trigger on certain movement input events";
    }

    public static class ReappearAfterFallTooltips
    {
        public const string Rigidbody =
            "Should reference the main Rigidbody of the character controller";

        public const string MinimumY =
            "When _rigidbody.transform.position.y is lower than this value, restart it";

    }

    public static class SphereColliderFollowerTooltips
    {
        public const string Collider =
            "Which sphere is being followed by this graphic?";
    }

    public static class SphereColliderStaysBelowHipsTooltips
    {
        public const string Collider =
            "The sphere collider at the foot of the body tracked user";

        public const string CharacterRoot =
            "Root of the character object, which has specially named body tracked bones as children somewhere in it's hierarchy";

        public const string TrackedHipTransform =
            "Transform that move when the player moves in real life.";

        public const string TrackingToes =
            "Transforms belonging to feet, at the ground, that move when the animated character moves.";

        public const string ColliderFollowsToes =
            "If true, the sphere collider will be influenced by the y-position of toes";

        public const string FloorLayerMask =
            "Collision layers to avoid merging foot collision area into (due to animation or body tracking)";

        public const string ExpectedBodyCapsule =
            "Capsule volume where the body is expected, should be a trigger";
    }

    public static class ActivatableStateSetTooltips
    {
        public const string Set =
            "Set of activatable states";

        public const string Index =
            "Index of current activatable state";

        public const string MinimumActivationDuration =
            "Minimum number of seconds a state can count as active. Exit time.";

        public const string OnTimerChange =
            "Callback to notify as the activation timeout timer advances";
    }

    public static class OVRInputBindingTooltips
    {
        public const string ButtonBindings =
            "Button state listeners";

        public const string JoystickBindings =
            "Joysticks state listeners";

        public const string OnUpdate =
            "Check input bindings on Update";

        public const string OnFixedUpdate =
            "Check input bindings on FixedUpdate";
    }

    public static class UnityInputBindingTooltips
    {
        public const string ButtonJump =
            "Triggered by Input.GetButton(\"Jump\")";

        public const string AxisHorizontalVertical =
            "Triggered by Input.GetAxis() \"Hoizontal\" and \"Vertical\"";

        public const string KeyBindings =
            "Key bindings listening to Input.GetKey(KeyCode)";
    }

    public static class ReorderInHierarchyTooltips
    {
        public const string HowToReorder =
            "How to reorder this transform in the hierarchy";

        public const string Target =
            "Which object to reorder in relation to";
    }

    public static class OVRBodyTrackingStateListenerTooltips
    {
        public const string StateListeners =
            "Body tracking state listeners";

        public const string MaxWaitTimeForBodyTracking =
            "How many seconds before body tracking times out and is considered disconnected";
    }

    public static class ToggleConstraintsSkeletonStateTooltips
    {
        public const string Constraints =
            "Constraints to control the weight of.";

        public const string Skeleton =
            "The skeleton object that needs to be tracked.";
    }

    public static class BodyTrackingFidelityToggleTooltips
    {
        public const string CurrentFidelity =
            "The current fidelity set.";

        public const string WorldText =
            "The text to update after body tracking fidelity is changed.";
    }

    public static class SuggestBodyTrackingCalibrationButtonTooltips
    {
        public const string WorldText =
            "The text to modify once height is modified.";

        public const string Height =
            "The height to set in meters.";

        public const string CalibrateOnStartup =
            "Allows calibration on startup.";
    }

    public static class BlendHandConstraintsFullBodyTooltips
    {
        public const string Constraints =
            "Constraints to control the weight of.";

        public const string RetargetingLayer =
            "The character's retargeting layer.";

        public const string BoneIdToTest =
            "Bone ID, usually the wrist. Can be modified depending " +
            "on the skeleton used.";

        public const string HeadTransform =
            "Head transform to do distance checks against.";

        public const string AutoAddTo =
            "MonoBehaviour to add to.";

        public const string ConstraintsMinDistance =
            "Distance where constraints are set to 1.0.";

        public const string ConstraintsMaxDistance =
            "Distance where constraints are set to 0.0.";

        public const string BlendCurve =
            "Multiplier that influences weight interpolation based on distance.";

        public const string MaxWeight =
            "Max constraint weight.";
    }

    public static class RestPoseObjectHumanoidTooltips
    {
        public const string BonePoseDataArray =
            "A flat array containing all bone pose data.";
    }

    public static class RetargetingBlendHandProcessorTooltips
    {
        public const string MinDistance =
            "Distance where weight is set to 1.0.";

        public const string MaxDistance =
            "Distance where weight is set to 0.0.";

        public const string BlendCurve =
            "Multiplier that influences weight interpolation based on distance.";

        public const string FullBodySecondBoneIdToTest =
            "(Full Body) Secondary Bone ID, usually the lower arm. " +
            "This is the target bone that the upper arm will pre-rotate to for a more accurate IK solve. " +
            "Can be modified depending on the skeleton used.";

        public const string FullBodyBoneIdToTest =
            "(Full Body) Bone ID, usually the wrist. Can be modified depending on the skeleton used.";

        public const string BoneIdToTest =
            "Bone ID, usually the wrist. Can be modified depending on the skeleton used.";

        public const string IsFullBody =
            "Specifies if this is full body or not.";

        public const string HeadView =
            "The type of head that should be used to blend hands.";
    }

    public static class RetargetingProcessorCorrectHandTooltips
    {
        public static class SyncOvrControllersAndHandsSettingsTooltips
        {
            public const string SyncOvrOption =
                "Specifies how hand target data should be synced with the OVRControllers and OVRHands data.";

            public const string OvrHandControllerPositionOffset =
                "The offset to get the hand position from the OVR controller root position. \n" +
                "This offset value is taken from Interaction SDK.";

            public const string OvrHandControllerOrientationOffset =
                "The offset to get the hand rotation from the OVR controller root rotation. \n" +
                "This offset value is taken from Interaction SDK.";

            public const string MirrorHandControllerOffsets =
                "True if the hand controller offsets should be mirrored for the left hand.";
        }

        public const string BlendHandWeight =
            "The weight of the hand blending.";

        public const string HandIKWeight =
            "The weight of the hand blending.";

        public const string ArmChainBones =
            "The weight of the hand blending.";

        public const string SyncOvrControllersAndHandsSettings =
            "Settings for syncing with OVRControllers and OVRHands.";

        public const string LeftHandProcessor =
            "Left hand processor.";

        public const string RightHandProcessor =
            "Right hand processor.";
    }

    public static class ExternalBoneTargetsTooltips
    {
        public static class BoneTargetTooltips
        {
            public const string BoneId =
                "The OVRSkeleton.BoneId that must be tracked.";
            public const string HumanBodyBone =
                "The human body bone representation of this bone.";
            public const string Target =
                "The target transform to update with body tracking bone data.";
        }

        public const string BoneTargets =
            "The array of bone targets.";
        public const string IsFullBody =
            "Is it full body (or not).";
        public const string Enabled =
            "Enables or disables functionality.";
    }

    public static class RetargetedBoneMappingsTooltips
    {
        public const string HumanBodyBonePairs =
        "HumanBodyBone pairs for this humanoid.";

        public const string HumanBodyBoneToBoneId =
            "HumanBodyBone to BodyJointId mapping for this humanoid.";
    }

    public static class TargetOffsetCorrectionTooltips
    {
        public const string RigBuilder =
            "Rig builder of character.";
        public const string AnkleBone =
            "Ankle bone.";
        public const string TipBone =
            "Foot tip bone.";
        public const string GroundingConstraint =
            "Foot grounding constraint.";
    }
}
