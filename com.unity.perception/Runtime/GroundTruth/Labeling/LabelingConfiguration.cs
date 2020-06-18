using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine.Serialization;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// A definition for how a <see cref="Labeling"/> should be resolved to a single label and id for ground truth generation.
    /// </summary>
    [CreateAssetMenu(fileName = "LabelingConfiguration", menuName = "Perception/Labeling Configuration", order = 1)]
    public class LabelingConfiguration : ScriptableObject
    {
        [FormerlySerializedAs("LabelingConfigurations")]
        [FormerlySerializedAs("LabelEntries")]
        [SerializeField]
        List<LabelEntry> m_LabelEntries = new List<LabelEntry>();

        /// <summary>
        /// Whether the inspector will auto-assign ids based on the id of the first element.
        /// </summary>
        public bool AutoAssignIds = true;

        /// <summary>
        /// Whether the inspector will start label ids at zero or one when <see cref="AutoAssignIds"/> is enabled.
        /// </summary>
        public StartingLabelId StartingLabelId = StartingLabelId.One;

        /// <summary>
        /// A sequence of <see cref="LabelEntry"/> which defines the labels relevant for this configuration and their values.
        /// </summary>
        public IReadOnlyList<LabelEntry> LabelEntries => m_LabelEntries;

        /// <summary>
        /// Attempts to find the matching index in <see cref="LabelEntries"/> for the given <see cref="Labeling"/>.
        /// </summary>
        /// <remarks>
        /// The matching index is the first class name in the given Labeling which matches an entry in <see cref="LabelEntries"/>.
        /// </remarks>
        /// <param name="labeling">The <see cref="Labeling"/> to match </param>
        /// <param name="labelEntry">When this method returns, contains the matching <see cref="LabelEntry"/>, or <code>default</code> if no match was found.</param>
        /// <returns>Returns true if a match was found. False if not.</returns>
        public bool TryGetMatchingConfigurationEntry(Labeling labeling, out LabelEntry labelEntry)
        {
            return TryGetMatchingConfigurationEntry(labeling, out labelEntry, out int _);
        }

        /// <summary>
        /// Attempts to find the matching index in <see cref="LabelEntries"/> for the given <see cref="Labeling"/>.
        /// </summary>
        /// <remarks>
        /// The matching index is the first class name in the given Labeling which matches an entry in <see cref="LabelEntries"/>.
        /// </remarks>
        /// <param name="labeling">The <see cref="Labeling"/> to match </param>
        /// <param name="labelEntry">When this method returns, contains the matching <see cref="LabelEntry"/>, or <code>default</code> if no match was found.</param>
        /// <param name="labelEntryIndex">When this method returns, contains the index of the matching <see cref="LabelEntry"/>, or <code>-1</code> if no match was found.</param>
        /// <returns>Returns true if a match was found. False if not.</returns>
        public bool TryGetMatchingConfigurationEntry(Labeling labeling, out LabelEntry labelEntry, out int labelEntryIndex)
        {
            foreach (var labelingClass in labeling.labels)
            {
                for (var i = 0; i < LabelEntries.Count; i++)
                {
                    var entry = LabelEntries[i];
                    if (string.Equals(entry.label, labelingClass))
                    {
                        labelEntry = entry;
                        labelEntryIndex = i;
                        return true;
                    }
                }
            }

            labelEntryIndex = -1;
            labelEntry = default;
            return false;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal struct LabelEntrySpec
        {
            /// <summary>
            /// The label id prepared for reporting in the annotation
            /// </summary>
            [UsedImplicitly]
            public int label_id;
            /// <summary>
            /// The label name prepared for reporting in the annotation
            /// </summary>
            [UsedImplicitly]
            public string label_name;
        }

        internal LabelEntrySpec[] GetAnnotationSpecification()
        {
            return LabelEntries.Select((l) => new LabelEntrySpec()
            {
                label_id = l.id,
                label_name = l.label,
            }).ToArray();
        }
    }

    /// <summary>
    /// Structure defining a label configuration for <see cref="LabelingConfiguration"/>.
    /// </summary>
    [Serializable]
    public struct LabelEntry
    {
        /// <summary>
        /// The id associated with the label. Used to associate objects with labels in various forms of ground truth.
        /// </summary>
        public int id;
        /// <summary>
        /// The label string
        /// </summary>
        public string label;
        /// <summary>
        /// The value to use when generating semantic segmentation images.
        /// </summary>
        public int value;

        /// <summary>
        /// Creates a new LabelingConfigurationEntry with the given values.
        /// </summary>
        /// <param name="id">The id associated with the label. Used to associate objects with labels in various forms of ground truth.</param>
        /// <param name="label">The label string.</param>
        /// <param name="value">The value to use when generating semantic segmentation images.</param>
        public LabelEntry(int id, string label, int value)
        {
            this.id = id;
            this.label = label;
            this.value = value;
        }
    }
}
