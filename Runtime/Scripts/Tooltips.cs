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

        public const string DuplicateLayerName =
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
            public const string LocalPosition = "Position to set.";
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
        public const string Skeleton =
            "[Deprecated] The OVR Skeleton component. " +
            "NOTE: not recommended as this was experimental.";

        public const string CustomSkeleton =
            "The OVRCustomSkeleton component for the character.";

        public const string Animator =
            "The animator component.";

        public const string SpineTranslationCorrectionType =
            "The type of spine translation correction that should be applied.";

        public const string ApplyToArms =
            "Apply deformation on arms.";

        public const string CorrectSpineOnce =
            "Allows the spine correction to run only once, assuming the skeleton's positions don't get updated multiple times.";

        public const string MoveTowardsArms =
            "If true, the arms will move towards the deformation target position.";

        public const string SnapThreshold =
            "The distance between the target and current position before the bone snaps to the target position.";

        public const string ArmWeight =
            "The weight for the deformation on arms.";

        public const string ArmMoveSpeed =
            "The move towards speed for the arms.";
    }

    public static class HipPinningDataTooltips
    {
        public const string Skeleton =
            "The OVR Skeleton component.";

        public const string HipPinningTargets =
            "The list of hip pinning targets in the scene.";

        public const string HipPinningHeightAdjustment =
            "If true, hip pinning will adjust the height of the seat to match the tracked position.";

        public const string HipPinningLeave =
            "If true, hip pinning will be disabled when the character leaves a certain range.";

        public const string HipPinningLeaveRange =
            "The range from the hip pinning target before hip pinning is disabled.";
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

    public static class RetargetingConstraintDataTooltips
    {
        public const string RetargetingLayer =
            "Retargeting layer component to get data from.";

        public const string AllowDynamicAdjustmentsRuntime =
            "Allow dynamic adjustments at runtime.";

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

        public const string RotationOffsets =
            "Rotation offset to be applied during retargeting.";

        public const string RotationAdjustments =
            "Optional rotational adjustment to be applied during retargeting.";
    }

    public static class RetargetingLayerTooltips
    {
        public const string DisableAvatar =
            "Disable avatar for accurate positions, especially fingers.";

        public const string PositionsToCorrectLateUpdate =
            "Positions to correct after the fact. Avatar " +
            "masks prevent setting positions of the hands precisely.";

        public const string MaskToSetToTPose =
            "Since some bones are not affected by retargeting, " +
            "some joints should be reset to t-pose.";

        public const string EnableTrackingByProxy =
            "Create proxy transforms that track the skeletal bones. If the " +
            "skeletal bone transforms change, that won't necessitate creating new " +
            "proxy transforms in most cases.";
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

    public static class RetargetingMenuTooltips
    {
        public const string CharacterToSpawn =
            "Main character prefab to spawn.";

        public const string SpawnParent =
            "Parent to spawn under.";

        public const string SpawnOffset =
            "Offset per spawn.";
    }
}
