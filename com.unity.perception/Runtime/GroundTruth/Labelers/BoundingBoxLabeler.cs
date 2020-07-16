﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.Serialization;
using Unity.Simulation;
using UnityEngine.UI;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Produces 2d bounding box annotations for all visible objects each frame.
    /// </summary>
    [Serializable]
    public sealed class BoundingBox2DLabeler : CameraLabeler
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        struct BoundingBoxValue
        {
            public int label_id;
            public string label_name;
            public uint instance_id;
            public float x;
            public float y;
            public float width;
            public float height;
        }

        static ProfilerMarker s_BoundingBoxCallback = new ProfilerMarker("OnBoundingBoxesReceived");

        /// <summary>
        /// The GUID id to associate with the annotations produced by this labeler.
        /// </summary>
        public string annotationId = "F9F22E05-443F-4602-A422-EBE4EA9B55CB";
        /// <summary>
        /// The <see cref="IdLabelConfig"/> which associates objects with labels.
        /// </summary>
        [FormerlySerializedAs("labelingConfiguration")]
        public IdLabelConfig idLabelConfig;

        Dictionary<int, AsyncAnnotation> m_AsyncAnnotations;
        AnnotationDefinition m_BoundingBoxAnnotationDefinition;
        BoundingBoxValue[] m_BoundingBoxValues;

        private Dictionary<string, RectTransform> visualizationPanelCache = null;
        private GameObject visualizationHolder = null;

        /// <summary>
        /// Creates a new BoundingBox2DLabeler. Be sure to assign <see cref="idLabelConfig"/> before adding to a <see cref="PerceptionCamera"/>.
        /// </summary>
        public BoundingBox2DLabeler()
        {
        }

        /// <summary>
        /// Creates a new BoundingBox2DLabeler with the given <see cref="IdLabelConfig"/>.
        /// </summary>
        /// <param name="labelConfig">The label config for resolving the label for each object.</param>
        public BoundingBox2DLabeler(IdLabelConfig labelConfig)
        {
            this.idLabelConfig = labelConfig;
        }

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException("BoundingBox2DLabeler's idLabelConfig field must be assigned");

            m_AsyncAnnotations = new Dictionary<int, AsyncAnnotation>();

            m_BoundingBoxAnnotationDefinition = DatasetCapture.RegisterAnnotationDefinition("bounding box", idLabelConfig.GetAnnotationSpecification(),
                "Bounding box for each labeled object visible to the sensor", id: new Guid(annotationId));

            perceptionCamera.RenderedObjectInfosCalculated += OnRenderedObjectInfosCalculated;

            supportsVisualization = true;
            EnableVisualization(supportsVisualization);
        }

        /// <inheritdoc/>
        protected override void OnBeginRendering()
        {
            m_AsyncAnnotations[Time.frameCount] = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_BoundingBoxAnnotationDefinition);
        }

        void OnRenderedObjectInfosCalculated(int frameCount, NativeArray<RenderedObjectInfo> renderedObjectInfos)
        {
            if (!m_AsyncAnnotations.TryGetValue(frameCount, out var asyncAnnotation))
                return;

            m_AsyncAnnotations.Remove(frameCount);

            using (s_BoundingBoxCallback.Auto())
            {
                if (m_BoundingBoxValues == null || m_BoundingBoxValues.Length != renderedObjectInfos.Length)
                    m_BoundingBoxValues = new BoundingBoxValue[renderedObjectInfos.Length];

                for (var i = 0; i < renderedObjectInfos.Length; i++)
                {
                    var objectInfo = renderedObjectInfos[i];
                    if (!idLabelConfig.TryGetLabelEntryFromInstanceId(objectInfo.instanceId, out var labelEntry))
                        continue;

                    m_BoundingBoxValues[i] = new BoundingBoxValue
                    {
                        label_id = labelEntry.id,
                        label_name = labelEntry.label,
                        instance_id = objectInfo.instanceId,
                        x = objectInfo.boundingBox.x,
                        y = objectInfo.boundingBox.y,
                        width = objectInfo.boundingBox.width,
                        height = objectInfo.boundingBox.height,
                    };
                }

                if (!CaptureOptions.useAsyncReadbackIfSupported && frameCount != Time.frameCount) 
                    Debug.LogWarning("Not on current frame: " + frameCount + "(" + Time.frameCount + ")");

                if (IsVisualizationEnabled()) 
                    Visualize();
                
                asyncAnnotation.ReportValues(m_BoundingBoxValues);
            }
        }

        /// <inheritdoc/>
        protected override void SetupVisualizationPanel(GameObject panel)
        {
            var toggle  = GameObject.Instantiate(Resources.Load<GameObject>("GenericToggle"));
            toggle.transform.SetParent(panel.transform);
            toggle.GetComponentInChildren<Text>().text = "Bounding Boxes";
            toggle.GetComponent<Toggle>().onValueChanged.AddListener(enabled => {
                EnableVisualization(enabled);
            });

            visualizationHolder = GameObject.Instantiate(Resources.Load<GameObject>("BoundsHolder"));
            visualizationHolder.transform.SetParent(panel.transform.parent, false);
            
            visualizationPanelCache = new Dictionary<string, RectTransform>();
        }

        void Visualize()
        {
            foreach (var box in m_BoundingBoxValues)
            {
                var rectTrans = GetVisualizationPanel(box.label_name);
                rectTrans.anchoredPosition = new Vector2(box.x, -box.y);
                rectTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, box.width);
                rectTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, box.height);
            }
        }

        RectTransform GetVisualizationPanel(string label)
        {
            if (!visualizationPanelCache.ContainsKey(label))
            {
                var box = GameObject.Instantiate(Resources.Load<GameObject>("BoundingBoxPrefab"));
                box.name = label + "_boundingbox";
                box.GetComponentInChildren<Text>().text = label;
                box.transform.SetParent(visualizationHolder.transform, false);
                visualizationPanelCache[label] = box.transform as RectTransform;
            }

            return visualizationPanelCache[label];
        }

        /// <inheritdoc/>
        override protected void OnVisualizerEnabled(bool enabled)
        {
            if (visualizationHolder != null) 
                visualizationHolder.SetActive(enabled);
        }
    }
}
