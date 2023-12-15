// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace DeformationRig
{
    /// <summary>
    /// Driver for corrective shapes that automatically finds and drives the appropriate shapes.
    ///
    /// Only drives correctives that can be derived from already-set driver shapes. Shapes that
    /// require the mutation or replacement of driver shapes (i.e. opposing shapes) should use
    /// </summary>
    [ExecuteInEditMode]
    public class AutoCorrectiveShapeDriver : MonoBehaviour
    {
        /// <summary>
        /// An enumeration of the types of corrective blendshapes that can be driven by this driver.
        /// </summary>
        [Flags]
        private enum DriveableTypes
        {
            /// <summary>Required for Unity inspector.</summary>
            None = 0,
            /// <summary>A blendshape driven by combining 1 or more driver shapes.</summary>.
            Combination = 1,
            /// <summary>A blendshape driven during part of a driver shape signal.</summary>.
            InBetween = 2,
        }

        /// <summary>Enum specifying when this driver should update correctives.</summary>
        private enum UpdateTime
        {
            /// <summary>
            /// Don't automatically update correctives. Useful for deactivating the driver or
            /// controlling updates manually.
            /// </summary>
            ExternallyDriven,
            /// <summary>Update correctives during Update().</summary>
            Update,
            /// <summary>Update correctives during LateUpdate().</summary>
            LateUpdate,
            /// <summary>Do not drive the correctives at any time.</summary>
            Disabled,
        }

        /// <summary>A simple pairing of a renderer with its configured correctives.</summary>
        private struct CorrectiveConfig
        {
            public IBlendshapeInterface BlendshapeInterface;
            public List<ICorrectiveShape> Correctives;
        }

        [SerializeField]
        [Tooltip("Renderers whose corrective shapes should be driven.")]
        private SkinnedMeshRenderer[] _renderers;

        [SerializeField]
        [Tooltip("Which shape type or types to drive.")]
        private DriveableTypes _drivenShapeTypes =
            DriveableTypes.Combination | DriveableTypes.InBetween;

        [SerializeField]
        [Tooltip("What lifecycle events this driver should run on.")]
        private UpdateTime _updateTime = UpdateTime.LateUpdate;

        private List<CorrectiveConfig> _perMeshCorrectiveConfigs = null;

        private bool IsDriving(DriveableTypes type) => _drivenShapeTypes.HasFlag(type);

        private bool IsConfigured => _perMeshCorrectiveConfigs is not null;

        private void Awake()
        {
            Assert.IsNotNull(_renderers);
            Configure();
        }

        // Runs when things are changed in editor in EditMode.
        private void OnValidate() => Configure();

        private void Update() => MaybeDriveCorrectives(UpdateTime.Update);

        private void LateUpdate() => MaybeDriveCorrectives(UpdateTime.LateUpdate);

        /// <summary>
        /// Find all driven shapes and their drivers, and save that information to apply on updates.
        /// </summary>
        [ContextMenu("Configure Driver")]
        private void Configure()
        {
            _perMeshCorrectiveConfigs = _renderers
                .Select(renderer => new CorrectiveConfig()
                {
                    BlendshapeInterface = new SkinnedMeshRendererBlendshapeInterface(renderer),
                    Correctives = NamingSchemes.V0.ParseCorrectives(
                            GetAllShapeNames(renderer.sharedMesh)),
                })
                .ToList();
        }

        /// <summary>
        /// Gets all blendshape names for a given mesh, stripping a leading geometry name if found.
        /// </summary>
        private List<string> GetAllShapeNames(Mesh mesh) =>
          Enumerable
              .Range(0, mesh.blendShapeCount)
              .Select(idx => mesh.GetBlendShapeName(idx))
              .Select(
                  name =>
                  {
                      var dotIdx = name.IndexOf(".");
                      return dotIdx != -1 ? name.Substring(dotIdx + 1) : name;
                  })
              .Select(name => name.Trim())
              .ToList();

        /// <summary>
        /// Drives correctives if the provided update time is the one this driver is configured to
        /// update during. Simplifies the update methods above.
        /// </summary>
        private void MaybeDriveCorrectives(UpdateTime updateTime)
        {
            if (updateTime == _updateTime)
            {
                DriveCorrectives();
            }
        }

        /// <summary>
        /// Drives correctives. Is a no-op if the driver has not yet been configured.
        /// </summary>
        public void DriveCorrectives()
        {
            if (!IsConfigured)
            {
                Debug.LogError("Attempted to drive unconfigured correctives.", this);
                return;
            }
            else if (_updateTime == UpdateTime.Disabled)
            {
                Debug.LogError("Attempted to drive a disabled correctives driver.", this);
                return;
            }

            // This would look cleaner as a foreach, but you can't use foreach iterator fields as
            // ref arguments.
            for (int i = 0; i < _perMeshCorrectiveConfigs.Count; i++)
            {
                var config = _perMeshCorrectiveConfigs[i];
                foreach (var corrective in config.Correctives)
                {
                    corrective.Apply(config.BlendshapeInterface);
                }
            }
        }
    }
}
