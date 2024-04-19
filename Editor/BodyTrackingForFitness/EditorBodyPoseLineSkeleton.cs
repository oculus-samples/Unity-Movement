// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Oculus.Interaction.Body.Input;
using Oculus.Interaction.Body.PoseDetection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Oculus.Movement.BodyTrackingForFitness
{
    /// <summary>
    /// Uses <see cref="EditorTransformAwareness"/> to automatically connect
    /// <see cref="BodyPoseBoneTransforms"/> & <see cref="BodyPoseController"/>
    /// scripts at editor time, also drawing a line visualization in the editor
    /// of the most relevant <see cref="IBodyPose"/> on a GameObject.
    /// </summary>
    public class EditorBodyPoseLineSkeleton
    {
        /// <summary>
        /// Private singleton so some editor time functionality can be static.
        /// </summary>
        private static EditorBodyPoseLineSkeleton _instance;

        /// <summary>
        /// Any GameObject with a <inheritdoc cref="IBodyPose"/> script could draw a skeleton
        /// </summary>
        private Dictionary<GameObject,IBodyPose> _drawList =
            new Dictionary<GameObject,IBodyPose>();
        
        /// <code>[InitializeOnLoadMethod]</code> will trigger the static constructor
        [InitializeOnLoadMethod]
        static void OnProjectLoadedInEditor() { }

        /// <summary>
        /// Look for objects with <see cref="IBodyPose"/> components, add them
        /// to a list of poses to draw, and update when transforms change
        /// <see cref="BodyPoseController"/> with
        /// <see cref="BodyPoseBoneTransforms"/>.
        /// </summary>
        static EditorBodyPoseLineSkeleton()
        {
            RefreshSystem();
        }

        /// <summary>
        /// Re-initializes the <see cref="EditorBodyPoseLineSkeleton"/> system
        /// </summary>
        public static void RefreshSystem()
        {
            _instance = new EditorBodyPoseLineSkeleton();
            SceneView.duringSceneGui -= OnGui;
            SceneView.duringSceneGui += OnGui;
            EditorSceneManager.sceneOpened -= OnScene;
            EditorSceneManager.sceneOpened += OnScene;
            ObjectChangeEvents.changesPublished -= OnChange;
            ObjectChangeEvents.changesPublished += OnChange;
            EditorApplication.delayCall += _instance.BecomeAwareOfInitialIBodyPoseObjects;
        }

        private static void OnGui(SceneView sceneView) => _instance.DrawIBodyPoseObjects(sceneView);
        private static void OnScene(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode) =>
            RefreshSystem();
        private static void OnChange(ref ObjectChangeEventStream stream) =>
            _instance.UpdateChangedIBodyPoseObjects(ref stream);

        private void BecomeAwareOfInitialIBodyPoseObjects()
        {
            GameObject[] list = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var item in list)
            {
                UpdateIBodyPoseUiOn(item);
            }
        }

        private bool UpdateIBodyPoseUiOn(GameObject gameObject)
        {
            bool change = UpdateBodyPoseSkeletonToDraw(gameObject);
            change |= UpdateTransformAwareness(gameObject);
            return change;
        }

        private bool UpdateBodyPoseSkeletonToDraw(GameObject gameObject)
        {
            IBodyPose pose = GetMostConsequentialBodyPose(gameObject);
            if (pose != null)
            {
                _drawList[gameObject] = pose;
                return true;
            }
            _drawList.Remove(gameObject);
            return false;
        }

        private static IBodyPose GetMostConsequentialBodyPose(GameObject gameObject)
        {
            IBodyPose[] bodyPoses = gameObject.GetComponents<IBodyPose>();
            if (bodyPoses == null || bodyPoses.Length == 0)
            {
                return null;
            }
            IBodyPose bestCandidate = bodyPoses[0];
            bool candidateIsTransient = IsTransientBodyPose(bestCandidate);
            for (int i = 1; i < bodyPoses.Length && candidateIsTransient; ++i)
            {
                IBodyPose pose = bodyPoses[i];
                bool isTransient = IsTransientBodyPose(pose);
                if (!isTransient)
                {
                    bestCandidate = pose;
                    candidateIsTransient = false;
                }
            }
            return bestCandidate;
        }

        /// <summary>
        /// Check if the data source refers to other poses, or to real data accessed at runtime.
        /// </summary>
        private static bool IsTransientBodyPose(IBodyPose body)
        {
            Component reference = null;
            switch (body)
            {
                case OVRBodyPose:
                    return true;
                case BodyPoseBoneTransforms skeleton:
                    reference = skeleton.BodyPose as Component;
                    break;
                case BodyPoseController controller:
                    reference = controller.BodyPose as Component;
                    break;
            }
            return reference != null && !(reference is IBody);
        }

        private bool UpdateTransformAwareness(GameObject gameObject)
        {
            bool change = false;
            BodyPoseController[] controllers = gameObject.GetComponents<BodyPoseController>();
            foreach (var controller in controllers)
            {
                BodyPoseBoneTransforms skeleton = controller.SkeletonTransforms;
                if (skeleton != null)
                {
                    EditorTransformAwareness.SetBoneListener(controller, skeleton.OwnsBone, Notify);
                    change = true;
                }
            }
            BodyPoseBoneTransforms[] skeletons = gameObject.GetComponents<BodyPoseBoneTransforms>();
            foreach (var skeleton in skeletons)
            {
                EditorTransformAwareness.SetBoneListener(skeleton, skeleton.OwnsBone, Notify);
                change = true;
            }
            return change;
        }

        private static void Notify(Transform movedBone)
        {
            IEnumerable<Object> owners = EditorTransformAwareness.GetOwners(movedBone);
            foreach (Object obj in owners)
            {
                switch (obj)
                {
                    case BodyPoseController controller:
                        NotifyPoseList(controller);
                        break;
                    case BodyPoseBoneTransforms skeleton:
                        NotifySkeleton(skeleton);
                        break;
                }
            }
        }

        private static void NotifyPoseList(BodyPoseController controller)
        {
            ApplyBoneTransformsToBodyPose(controller);
        }

        private static void NotifySkeleton(BodyPoseBoneTransforms skeleton)
        {
            skeleton.NotifyBodyPoseUpdate();
        }

        private void UpdateChangedIBodyPoseObjects(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                ObjectChangeKind kind = stream.GetEventType(i);
                Object obj = null;
                switch (kind)
                {
                    case ObjectChangeKind.ChangeGameObjectStructure:
                        stream.GetChangeGameObjectStructureEvent(i,
                            out ChangeGameObjectStructureEventArgs structArgs);
                        obj = EditorUtility.InstanceIDToObject(structArgs.instanceId);
                        break;
                    case ObjectChangeKind.ChangeAssetObjectProperties:
                        stream.GetChangeAssetObjectPropertiesEvent(i,
                            out ChangeAssetObjectPropertiesEventArgs propArgs);
                        obj = EditorUtility.InstanceIDToObject(propArgs.instanceId);
                        break;
                    default:
                        continue;
                }
                AddBodyPoseTransformsFromThisObject(obj as GameObject);
            }
        }

        private void AddBodyPoseTransformsFromThisObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }
            if (UpdateIBodyPoseUiOn(gameObject))
            {
                EditorTransformAwareness.RefreshCallbacks();
            }
        }

        private void DrawIBodyPoseObjects(SceneView sceneView)
        {
            bool missingSomething = false;
            foreach (var kvp in _drawList)
            {
                if (kvp.Key == null)
                {
                    missingSomething = true;
                    continue;
                }
                if (kvp.Value != null)
                {
                    DrawIBodyPoseGizmos(kvp.Value);
                }
            }
            if (missingSomething)
            {
                _drawList = EditorTransformAwareness.SameDictionaryWithoutNullKey(_drawList);
            }
        }

        private static void DrawIBodyPoseGizmos(IBodyPose self)
        {
            if (self is Behaviour behaviour && (behaviour == null || !behaviour.enabled ||
                                                !behaviour.gameObject.activeInHierarchy))
            {
                return;
            }
            Gizmos.color = Color.blue;
            DrawIBodyPoseGizmos(self, 10);
        }

        private static void DrawIBodyPoseGizmos(IBodyPose self, float thick)
        {
            Transform container = null;
            IList<Transform> transforms = null;
            IBodyPose sourceData = null;
            Component selfComponent = null;
            switch (self)
            {
                case BodyPoseController controller:
                    selfComponent = controller;
                    BodyPoseBoneTransforms skeletonPartner = controller.SkeletonTransforms;
                    if (skeletonPartner != null)
                    {
                        transforms = skeletonPartner.BoneTransforms;
                        container = skeletonPartner.BoneContainer;
                    }
                    else
                    {
                        container = controller.transform;
                    }
                    sourceData = controller.BodyPose;
                    break;
                case BodyPoseBoneTransforms skeleton:
                    selfComponent = skeleton;
                    transforms = skeleton.BoneTransforms;
                    container = skeleton.BoneContainer;
                    sourceData = skeleton.BodyPose;
                    break;
            }
            Component sourceComponent = sourceData as Component;
            bool bodyDataSomewhereElse = sourceComponent != null &&
                sourceComponent.transform != selfComponent.transform && !(sourceComponent is IBody);
            if (!bodyDataSomewhereElse)
            {
                DrawSkeletonGizmo(self, container, thick, transforms);
            }
            else
            {
                DrawReferenceArrow(selfComponent.transform.position, 
                    sourceComponent.transform.position, thick);
            }
        }

        /// <param name="body">what skeleton to draw</param>
        /// <param name="root">where in the scene to draw it (position + rotation)</param>
        /// <param name="thick">how thick the line of the skeleton should be</param>
        /// <param name="transforms">transforms mapped to bones, for checking selection</param>
        private static void DrawSkeletonGizmo(IBodyPose body, Transform root, float thick,
            IList<Transform> transforms)
        {
            Color color = Gizmos.color;
            for (int i = 0; i < FullBodySkeletonTPose.TPose.ExpectedBoneCount; ++i)
            {
                bool isSelected = transforms != null && i < transforms.Count &&
                                  EditorTransformAwareness.IsSelected(transforms[i]);
                Gizmos.color = !isSelected ? color :
                    new Color(1 - color.r, 1 - color.g, 1 - color.b);
                body.GetJointPoseFromRoot((BodyJointId)i, out Pose pose);
                DrawBoneGizmo(i, pose, root, thick);
            }
        }

        private static void DrawReferenceArrow(Vector3 start, Vector3 end, float thick)
        {
            Vector3 delta = end - start;
            float distance = delta.magnitude;
            Vector3 direction = delta / distance;
            Vector3 cross = Vector3.Cross(direction, Vector3.right);
            Vector3 arrowFlangeA = end + (-direction + cross) * (distance / 16);
            Vector3 arrowFlangeB = end + (-direction - cross) * (distance / 16);
            Handles.DrawBezier(start, end,
                start, end, Gizmos.color, null, thick);
            Handles.DrawBezier(arrowFlangeA, end,
                arrowFlangeA, end, Gizmos.color, null, thick);
            Handles.DrawBezier(arrowFlangeB, end,
                arrowFlangeB, end, Gizmos.color, null, thick);
        }
        
        private static void DrawBoneGizmo(int boneIndex, Pose pose, Transform root, float thick)
        {
            int next = FullBodySkeletonTPose.TPose.GetNext(boneIndex);
            if (next < 0)
            {
                return;
            }
            Vector3 start = pose.position;
            if (root != null)
            {
                start = root.TransformPoint(start);
            }
            Quaternion boneRotation = pose.rotation *
                                      FullBodySkeletonTPose.TPose.GetForwardRotation(boneIndex);
            if (root != null)
            {
                boneRotation = root.rotation * boneRotation;
            }
            float boneLength = FullBodySkeletonTPose.TPose.GetBoneLength(boneIndex);
            if (boneLength < EditorTransformAwareness.Epsilon)
            {
                boneLength = EditorTransformAwareness.Epsilon;
            }
            Vector3 end = boneRotation * Vector3.forward * boneLength + start;
            Handles.DrawBezier(start, end, start, end,
                Gizmos.color, null, thick);
        }

        /// <summary>
        /// Applies <see cref="Transform"/> values to a <see cref="Pose"/> list
        /// </summary>
        /// <param name="self"></param>
        private static void ApplyBoneTransformsToBodyPose(BodyPoseController self)
        {
            if (self.SkeletonTransforms == null)
            {
                self.SkeletonTransforms = self.GetComponent<BodyPoseBoneTransforms>();
                if (self.SkeletonTransforms == null)
                {
                    return;
                }
            }
            Transform boneRoot = self.SkeletonTransforms.BoneContainer;
            if (boneRoot == null)
            {
                return;
            }
            IList<Transform> bones = self.SkeletonTransforms.BoneTransforms;
            Quaternion boneRootRotationOffset = Quaternion.Inverse(boneRoot.rotation);
            const float rotationAngleEpsilon = 1f / 1024;
            bool boneMoved = false;
            bool boneRotated = false;
            for (int id = 0; id < bones.Count; ++id)
            {
                BodyJointId boneId = (BodyJointId)id;
                if (!self.GetJointPoseFromRoot(boneId, out Pose oldBoneData))
                {
                    continue;
                }
                Transform bone = bones[id];
                Pose newPose = new Pose(
                    boneRoot.InverseTransformPoint(bone.position),
                    bone.rotation * boneRootRotationOffset);
                Vector3 moveDiff = newPose.position - oldBoneData.position;
                boneMoved |= moveDiff != Vector3.zero;
                Quaternion rotateDiff = newPose.rotation * Quaternion.Inverse(oldBoneData.rotation);
                float angleDiff = Quaternion.Angle(Quaternion.identity, rotateDiff);
                boneRotated |= angleDiff > rotationAngleEpsilon;
            }
            if (boneMoved || boneRotated)
            {
                self.ApplyTransformsToBonePoses();
            }
        }
    }
}
