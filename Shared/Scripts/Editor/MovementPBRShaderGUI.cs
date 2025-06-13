// Copyright (c) Meta Platforms, Inc. and affiliates. All rights reserved.

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    /// <summary>
    /// Based on Unity's StandardShaderGUI.cs. Defines interface
    /// for Movement's PBR shaders.
    /// </summary>
    internal class MovementPBRShaderGUI : ShaderGUI
    {
        private enum WorkflowMode
        {
            Specular,
            Metallic,
            Dielectric
        }

        public enum BlendMode
        {
            Opaque,
            Cutout,
            Fade,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
        }

        public enum SmoothnessMapChannel
        {
            SpecularMetallicAlpha,
            AlbedoAlpha,
        }

        private static class Styles
        {
            public static GUIContent AlbedoText = EditorGUIUtility.TrTextContent(
                "Albedo",
                "Albedo (RGB) and Transparency (A)");
            public static GUIContent AlphaCutoffText = EditorGUIUtility.TrTextContent(
                "Alpha Cutoff",
                "Threshold for alpha cutoff");

            public static GUIContent SpecularMapText = EditorGUIUtility.TrTextContent(
                "Specular",
                "Specular (RGB) and Smoothness (A)");
            public static GUIContent MetallicMapText = EditorGUIUtility.TrTextContent(
                "Metallic",
                "Metallic (R) and Smoothness (A)");
            public static GUIContent SmoothnessText = EditorGUIUtility.TrTextContent(
                "Smoothness",
                "Smoothness value");
            public static GUIContent SmoothnessScaleText = EditorGUIUtility.TrTextContent(
                "Smoothness",
                "Smoothness scale factor");
            public static GUIContent SmoothnessMapChannelText = EditorGUIUtility.TrTextContent(
                "Source",
                "Smoothness texture and channel");

            public static GUIContent HighlightsText = EditorGUIUtility.TrTextContent(
                "Specular Highlights",
                "Specular Highlights");
            public static GUIContent ReflectionsText = EditorGUIUtility.TrTextContent(
                "Reflections",
                "Glossy Reflections");
            public static GUIContent NormalMapText = EditorGUIUtility.TrTextContent(
                "Normal Map",
                "Normal Map");
            public static GUIContent OcclusionText = EditorGUIUtility.TrTextContent(
                "Occlusion",
                "Occlusion (G)");
            public static GUIContent EmissionText = EditorGUIUtility.TrTextContent(
                "Color",
                "Emission (RGB)");
            public static GUIContent EmissionStrength = EditorGUIUtility.TrTextContent(
                "Strength",
                "Strength");

            public static GUIContent StencilValueText = EditorGUIUtility.TrTextContent(
                "Stencil Value",
                "Stencil Value");
            public static GUIContent StencilCompText = EditorGUIUtility.TrTextContent(
                "Stencil Comp",
                "Stencil Comp");

            public static GUIContent AreaLightSampleEnableText = EditorGUIUtility.TrTextContent(
                "Area Light Sampling Toggle",
                "Area Light Sampling Toggle");
            public static GUIContent AreaLightSampleDistText = EditorGUIUtility.TrTextContent(
                "Area Light Sample Dist",
                "Area Light Sample Distance");

            public static GUIContent DiffuseWrapEnabledText = EditorGUIUtility.TrTextContent(
                "Diffuse Wrap Enabled",
                "Diffuse Wrap Enabled");
            public static GUIContent DiffuseWrapColorText = EditorGUIUtility.TrTextContent(
                "Diffuse Wrap Color",
                "Diffuse Wrap Color");
            public static GUIContent DiffuseWrapColorMultText = EditorGUIUtility.TrTextContent(
                "Diffuse Wrap Color Multiplier",
                "Diffuse Wrap Color Multiplier");
            public static GUIContent DiffuseWrapDistanceText = EditorGUIUtility.TrTextContent(
                "Diffuse Wrap Distance",
                "Diffuse Wrap Distance");

            public static GUIContent SpecAffectedByNDotL = EditorGUIUtility.TrTextContent(
                "Specularity Affected by NDotL",
                "Specularity Affected by NDotL");

            public static GUIContent VertexDisplacementShadows = EditorGUIUtility.TrTextContent(
                "Vertex Displacement Shadows",
                "Vertex Displacement Shadows");

            public static GUIContent RecalcNormalsEnableText = EditorGUIUtility.TrTextContent(
                "Normal Recalculation Toggle",
                "Normal Recalculation Toggle");

            public static GUIContent RenderQueue = EditorGUIUtility.TrTextContent(
                "Render Queue",
                "Render Queue");

            public static string PrimaryMapsText = "Main Maps";
            public static string ForwardText = "Forward Rendering Options";
            public static string RenderingMode = "Rendering Mode";
            public static string AdvancedText = "Advanced Options";
            public static readonly string[] BlendNames = Enum.GetNames(typeof(BlendMode));
        }

        private MaterialProperty _blendMode = null;
        private MaterialProperty _albedoMap = null;
        private MaterialProperty _albedoColor = null;
        private MaterialProperty _alphaCutoff = null;

        private MaterialProperty _specularMap = null;
        private MaterialProperty _specularColor = null;
        private MaterialProperty _metallicMap = null;
        private MaterialProperty _metallic = null;
        private MaterialProperty _smoothness = null;
        private MaterialProperty _smoothnessScale = null;
        private MaterialProperty _smoothnessMapChannel = null;

        private MaterialProperty _highlights = null;
        private MaterialProperty _reflections = null;
        private MaterialProperty _bumpScale = null;
        private MaterialProperty _bumpMap = null;
        private MaterialProperty _occlusionStrength = null;
        private MaterialProperty _occlusionMap = null;
        private MaterialProperty _emissionColorForRendering = null;
        private MaterialProperty _emissionMap = null;
        private MaterialProperty _emissionStrength = null;

        private MaterialProperty _stencilValue = null;
        private MaterialProperty _stencilComp = null;

        private MaterialProperty _areaLightSampleDistance = null;

        private MaterialProperty _diffuseWrapColor = null;
        private MaterialProperty _diffuseWrapColorMult = null;
        private MaterialProperty _diffuseWrapDist = null;

        private MaterialProperty _vertexDisplShadows = null;

        private MaterialEditor _materialEditor;
        private WorkflowMode _workflowMode = WorkflowMode.Specular;

        private bool _firstTimeApply = true;

        /// <summary>
        /// Creates GUI for material.
        /// </summary>
        /// <param name="materialEditor">Editor of material being rendered.</param>
        /// <param name="props">Properties of material.</param>
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            // MaterialProperties can be animated so we do not cache them but fetch them
            // every event to ensure animated values are updated correctly
            FindProperties(props);
            _materialEditor = materialEditor;
            Material material = materialEditor.target as Material;

            // Make sure that needed setup (ie keywords/renderqueue) are set up if we're
            // switching some existing material to a standard shader.
            // Do this before any GUI code has been issued to prevent layout issues
            // in subsequent GUILayout statements (case 780071)
            if (_firstTimeApply)
            {
                MaterialChanged(material,
                    _workflowMode,
                    material.GetFloat("_AreaLightSampleToggle") > 0.0f,
                    material.GetFloat("_DiffuseWrapEnabled") > 0.0f,
                    material.GetFloat("_SpecularityNDotL") > 0.0f,
                    material.GetFloat("_RecalculateNormalsToggle") > 0.0f);
                _firstTimeApply = false;
            }

            ShaderPropertiesGUI(material);
        }

        private void FindProperties(MaterialProperty[] props)
        {
            _blendMode = FindProperty("_Mode", props);
            _albedoMap = FindProperty("_MainTex", props);
            _albedoColor = FindProperty("_Color", props);
            _alphaCutoff = FindProperty("_Cutoff", props);
            _specularMap = FindProperty("_SpecGlossMap", props, false);
            _specularColor = FindProperty("_SpecColor", props, false);
            _metallicMap = FindProperty("_MetallicGlossMap", props, false);
            _metallic = FindProperty("_Metallic", props, false);
            DetermineWorkflow(props);
            _smoothness = FindProperty("_Glossiness", props);
            _smoothnessScale = FindProperty("_GlossMapScale", props, false);
            _smoothnessMapChannel = FindProperty("_SmoothnessTextureChannel", props, false);
            _highlights = FindProperty("_SpecularHighlights", props, false);
            _reflections = FindProperty("_GlossyReflections", props, false);
            _bumpScale = FindProperty("_BumpScale", props);
            _bumpMap = FindProperty("_BumpMap", props);
            _occlusionStrength = FindProperty("_OcclusionStrength", props);
            _occlusionMap = FindProperty("_OcclusionMap", props);
            _emissionColorForRendering = FindProperty("_EmissionColor", props);
            _emissionMap = FindProperty("_EmissionMap", props);
            _emissionStrength = FindProperty("_EmissionStrength", props);

            _stencilValue = FindProperty("_StencilValue", props);
            _stencilComp = FindProperty("_StencilComp", props);

            _areaLightSampleDistance = FindProperty("_AreaLightSampleDistance", props);

            _diffuseWrapColor = FindProperty("_DiffuseWrapColor", props);
            _diffuseWrapColorMult = FindProperty("_DiffuseWrapColorMult", props);
            _diffuseWrapDist = FindProperty("_DiffuseWrapDist", props);

            _vertexDisplShadows = FindProperty("_VertexDisplShadows", props);
        }

        private void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                BlendModePopup();

                // Primary properties
                GUILayout.Label(Styles.PrimaryMapsText, EditorStyles.boldLabel);
                DoAlbedoArea(material);
                DoSpecularMetallicArea();
                DoNormalArea();
                _materialEditor.TexturePropertySingleLine(Styles.OcclusionText,
                    _occlusionMap, _occlusionMap.textureValue != null ? _occlusionStrength : null);
                DoEmissionArea();
                EditorGUI.BeginChangeCheck();
                _materialEditor.TextureScaleOffsetProperty(_albedoMap);
                if (EditorGUI.EndChangeCheck())
                {
                    // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake
                    _emissionMap.textureScaleAndOffset = _albedoMap.textureScaleAndOffset;
                }
                EditorGUILayout.Space();

                // Third properties
                GUILayout.Label(Styles.ForwardText, EditorStyles.boldLabel);
                if (_highlights != null)
                {
                    _materialEditor.ShaderProperty(_highlights, Styles.HighlightsText);
                }
                if (_reflections != null)
                {
                    _materialEditor.ShaderProperty(_reflections, Styles.ReflectionsText);
                }

                EditorGUILayout.Space();
                DoStencilArea();
                DoAreaLightArea(material);
                DoEnhancedLightingSection(material);
                DoRecalcNormals(material);
            }

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in _blendMode.targets)
                {
                    MaterialChanged((Material)obj, _workflowMode,
                        material.GetFloat("_AreaLightSampleToggle") > 0.0f,
                        material.GetFloat("_DiffuseWrapEnabled") > 0.0f,
                        material.GetFloat("_SpecularityNDotL") > 0.0f,
                        material.GetFloat("_RecalculateNormalsToggle") > 0.0f);
                }
            }

            EditorGUILayout.Space();

            _materialEditor.RenderQueueField();

            // NB renderqueue editor is not shown on purpose: we want to override it based on blend mode
            GUILayout.Label(Styles.AdvancedText, EditorStyles.boldLabel);
            _materialEditor.EnableInstancingField();
            _materialEditor.DoubleSidedGIField();
        }

        internal void DetermineWorkflow(MaterialProperty[] props)
        {
            if (FindProperty("_MetallicGlossMap", props, false) != null
              && FindProperty("_Metallic", props, false) != null)
            {
                _workflowMode = WorkflowMode.Metallic;
            }
            else if (FindProperty("_SpecGlossMap", props, false) != null
                && FindProperty("_SpecColor", props, false) != null)
            {
                _workflowMode = WorkflowMode.Specular;
            }
            else
            {
                _workflowMode = WorkflowMode.Dielectric;
            }
        }

        /// <summary>
        /// Called when shader is assigned to material.
        /// </summary>
        /// <param name="material">Material being re-assigned.</param>
        /// <param name="oldShader">Old shader.</param>
        /// <param name="newShader">New shader.</param>
        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));
                return;
            }

            BlendMode blendMode = BlendMode.Opaque;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                blendMode = BlendMode.Cutout;
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                blendMode = BlendMode.Fade;
            }
            material.SetFloat("_Mode", (float)blendMode);

            DetermineWorkflow(MaterialEditor.GetMaterialProperties(
                new Material[] { material }));
            MaterialChanged(material,
                _workflowMode,
                material.GetFloat("_AreaLightSampleToggle") > 0.0f,
                material.GetFloat("_DiffuseWrapEnabled") > 0.0f,
                material.GetFloat("_SpecularityNDotL") > 0.0f,
                material.GetFloat("_RecalculateNormalsToggle") > 0.0f);
        }

        private void BlendModePopup()
        {
            EditorGUI.showMixedValue = _blendMode.hasMixedValue;
            var mode = (BlendMode)_blendMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (BlendMode)EditorGUILayout.Popup(Styles.RenderingMode, (int)mode, Styles.BlendNames);
            if (EditorGUI.EndChangeCheck())
            {
                _materialEditor.RegisterPropertyChangeUndo("Rendering Mode");
                _blendMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        private void DoNormalArea()
        {
            _materialEditor.TexturePropertySingleLine(Styles.NormalMapText, _bumpMap, _bumpMap.textureValue != null ? _bumpScale : null);
        }

        private void DoAlbedoArea(Material material)
        {
            _materialEditor.TexturePropertySingleLine(Styles.AlbedoText, _albedoMap, _albedoColor);
            if ((BlendMode)material.GetFloat("_Mode") == BlendMode.Cutout)
            {
                _materialEditor.ShaderProperty(_alphaCutoff, Styles.AlphaCutoffText.text, MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);
            }
        }

        private void DoEmissionArea()
        {
            // Emission for GI?
            if (_materialEditor.EmissionEnabledProperty())
            {
                bool hadEmissionTexture = _emissionMap.textureValue != null;

                // Texture and HDR color controls
                _materialEditor.TexturePropertyWithHDRColor(Styles.EmissionText, _emissionMap, _emissionColorForRendering, false);

                // If texture was assigned and color was black set color to white
                float brightness = _emissionColorForRendering.colorValue.maxColorComponent;
                if (_emissionMap.textureValue != null && !hadEmissionTexture && brightness <= 0f)
                {
                    _emissionColorForRendering.colorValue = Color.white;
                }

                // change the GI flag and fix it up with emissive as black if necessary
                _materialEditor.LightmapEmissionFlagsProperty(
                    MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);

                _materialEditor.ShaderProperty(_emissionStrength,
                    Styles.EmissionStrength);
            }
        }

        private void DoSpecularMetallicArea()
        {
            bool hasGlossMap = false;
            if (_workflowMode == WorkflowMode.Specular)
            {
                hasGlossMap = _specularMap.textureValue != null;
                _materialEditor.TexturePropertySingleLine(Styles.SpecularMapText, _specularMap, hasGlossMap ? null : _specularColor);
            }
            else if (_workflowMode == WorkflowMode.Metallic)
            {
                hasGlossMap = _metallicMap.textureValue != null;
                _materialEditor.TexturePropertySingleLine(Styles.MetallicMapText, _metallicMap, hasGlossMap ? null : _metallic);
            }

            bool showSmoothnessScale = hasGlossMap;
            if (_smoothnessMapChannel != null)
            {
                int smoothnessChannel = (int)_smoothnessMapChannel.floatValue;
                if (smoothnessChannel == (int)SmoothnessMapChannel.AlbedoAlpha)
                    showSmoothnessScale = true;
            }

            int indentation = 2; // align with labels of texture properties
            _materialEditor.ShaderProperty(showSmoothnessScale ? _smoothnessScale : _smoothness, showSmoothnessScale ? Styles.SmoothnessScaleText : Styles.SmoothnessText, indentation);

            ++indentation;
            if (_smoothnessMapChannel != null)
                _materialEditor.ShaderProperty(_smoothnessMapChannel, Styles.SmoothnessMapChannelText, indentation);
        }

        private void DoStencilArea()
        {
            _materialEditor.ShaderProperty(
                _stencilValue,
                Styles.StencilValueText);
            _materialEditor.ShaderProperty(
                _stencilComp,
                Styles.StencilCompText);
        }

        private void DoAreaLightArea(Material material)
        {
            var oldValue = material.GetFloat("_AreaLightSampleToggle");
            var newValue = GUILayout.Toggle(
                oldValue < 1.0f ? false : true, Styles.AreaLightSampleEnableText);
            if (newValue)
            {
                _materialEditor.ShaderProperty(_areaLightSampleDistance,
                    Styles.AreaLightSampleDistText);
            }
            material.SetFloat("_AreaLightSampleToggle", newValue ? 1.0f : 0.0f);
        }

        private void DoEnhancedLightingSection(Material material)
        {
            _materialEditor.ShaderProperty(_vertexDisplShadows,
                Styles.VertexDisplacementShadows);
            EditorGUILayout.Space();

            var oldValue = material.GetFloat("_DiffuseWrapEnabled");
            var newValue = GUILayout.Toggle(
                oldValue < 1.0f ? false : true, Styles.DiffuseWrapEnabledText);
            if (newValue)
            {
                _materialEditor.ShaderProperty(_diffuseWrapColor,
                    Styles.DiffuseWrapColorText);
                _materialEditor.ShaderProperty(_diffuseWrapColorMult,
                    Styles.DiffuseWrapColorMultText);
                _materialEditor.ShaderProperty(_diffuseWrapDist,
                    Styles.DiffuseWrapDistanceText);
            }
            material.SetFloat("_DiffuseWrapEnabled", newValue ? 1.0f : 0.0f);

            oldValue = material.GetFloat("_SpecularityNDotL");
            newValue = GUILayout.Toggle(
                oldValue < 1.0f ? false : true, Styles.SpecAffectedByNDotL);
            material.SetFloat("_SpecularityNDotL", newValue ? 1.0f : 0.0f);
        }

        private void DoRecalcNormals(Material material)
        {
            var oldValue = material.GetFloat("_RecalculateNormalsToggle");
            var newValue = GUILayout.Toggle(
                oldValue < 1.0f ? false : true, Styles.RecalcNormalsEnableText);
            material.SetFloat("_RecalculateNormalsToggle", newValue ? 1.0f : 0.0f);
        }

        /// <summary>
        /// Sets blend mode but does not interfere with render queue because that is up
        /// to user. The queue is dependent upon stencil operations.
        /// </summary>
        public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Opaque:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.SetShaderPassEnabled("ShadowCaster", true);
                    material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    break;
                case BlendMode.Cutout:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    // Transparent objects in URP should not cast shadows.
                    if (GraphicsSettings.defaultRenderPipeline != null)
                    {
                        material.SetShaderPassEnabled("ShadowCaster", false);
                    }
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    break;
                case BlendMode.Fade:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    // Transparent objects in URP should not cast shadows.
                    if (GraphicsSettings.defaultRenderPipeline != null)
                    {
                        material.SetShaderPassEnabled("ShadowCaster", false);
                    }
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    break;
                case BlendMode.Transparent:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    // Transparent objects in URP should not cast shadows.
                    if (GraphicsSettings.defaultRenderPipeline != null)
                    {
                        material.SetShaderPassEnabled("ShadowCaster", false);
                    }
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    break;
            }
        }

        private static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
        {
            int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
            if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
            {
                return SmoothnessMapChannel.AlbedoAlpha;
            }
            else
            {
                return SmoothnessMapChannel.SpecularMetallicAlpha;
            }
        }

        private static void SetMaterialKeywords(Material material, WorkflowMode workflowMode,
            bool enableAreaLightSpec, bool enableDiffuseWrap, bool specAffectedByNDotL, bool enableRecalcNormals)
        {
            // Note: keywords must be based on Material value not on MaterialProperty
            // due to multi-edit & material animation
            // (MaterialProperty value might come from renderer material property block)
            SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
            if (workflowMode == WorkflowMode.Specular)
            {
                SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
            }
            else if (workflowMode == WorkflowMode.Metallic)
            {
                SetKeyword(material, "_METALLICGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
            }
            // A material's GI flag internally keeps track of whether emission
            // is enabled at all, it's enabled but has no effect
            // or is enabled and may be modified at runtime. This state depends
            // on the values of the current flag and emissive color.
            // The fixup routine makes sure that the material is in the correct
            // state if/when changes are made to the mode or color.
            MaterialEditor.FixupEmissiveFlag(material);
            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags &
                MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

            if (material.HasProperty("_SmoothnessTextureChannel"))
            {
                SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A",
                    GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha);
            }

            SetKeyword(material, "_AREA_LIGHT_SPECULAR", enableAreaLightSpec);
            SetKeyword(material, "_DIFFUSE_WRAP", enableDiffuseWrap);
            SetKeyword(material, "_SPECULAR_AFFECT_BY_NDOTL", specAffectedByNDotL);
            SetKeyword(material, "_RECALCULATE_NORMALS", enableRecalcNormals);
        }

        private static void MaterialChanged(Material material, WorkflowMode workflowMode,
            bool enableAreaLightSpec, bool enableDiffuseWrap, bool specAffectedByNDotL, bool enableRecalcNormals)
        {
            SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));

            SetMaterialKeywords(material, workflowMode, enableAreaLightSpec,
                enableDiffuseWrap, specAffectedByNDotL, enableRecalcNormals);
        }

        private static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
            {
                m.EnableKeyword(keyword);
            }
            else
            {
                m.DisableKeyword(keyword);
            }
        }
    }
}
