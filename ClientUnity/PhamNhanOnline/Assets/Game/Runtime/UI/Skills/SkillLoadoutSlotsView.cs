using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Models;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Skills
{
    public sealed class SkillLoadoutSlotsView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private SkillLoadoutSlotView slotTemplate;

        [Header("Behavior")]
        [SerializeField] private bool hideTemplateObject = true;

        private readonly List<SkillLoadoutSlotView> spawnedSlots = new List<SkillLoadoutSlotView>(8);
        private string lastSnapshot = string.Empty;
        private int lastSlotCount = -1;
        private bool lastDragEnabled = true;

        public event Action<int, PlayerSkillModel> SkillDropped;

        private void Awake()
        {
            if (contentRoot == null && slotTemplate != null)
                contentRoot = slotTemplate.transform.parent;

            if (hideTemplateObject && slotTemplate != null)
                slotTemplate.gameObject.SetActive(false);
        }

        public void SetSlots(
            IReadOnlyList<SkillLoadoutSlotModel> slots,
            SkillPresentationCatalog presentationCatalog,
            bool dragEnabled,
            bool force = false)
        {
            slots ??= Array.Empty<SkillLoadoutSlotModel>();
            var snapshot = BuildSnapshot(slots);
            if (!force &&
                lastSlotCount == slots.Count &&
                lastDragEnabled == dragEnabled &&
                string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
            {
                return;
            }

            lastSlotCount = slots.Count;
            lastDragEnabled = dragEnabled;
            lastSnapshot = snapshot;

            EnsureSlotCount(slots.Count);
            for (var i = 0; i < spawnedSlots.Count; i++)
            {
                var slotView = spawnedSlots[i];
                if (slotView == null)
                    continue;

                var shouldBeVisible = i < slots.Count;
                if (slotView.gameObject.activeSelf != shouldBeVisible)
                    slotView.gameObject.SetActive(shouldBeVisible);

                if (!shouldBeVisible)
                {
                    slotView.Clear(force: true);
                    continue;
                }

                var slot = slots[i];
                slotView.SetSlotIndex(slot.SlotIndex, force: true);
                slotView.SetDragEnabled(dragEnabled);

                if (slot.HasSkill && slot.Skill.HasValue)
                {
                    var presentation = presentationCatalog != null
                        ? presentationCatalog.Resolve(slot.Skill.Value)
                        : new SkillPresentation(null);
                    slotView.SetItem(slot.Skill.Value, presentation, force: true);
                }
                else
                {
                    slotView.Clear(force: true);
                    slotView.SetDragEnabled(dragEnabled);
                }
            }
        }

        public void Clear(bool force = false)
        {
            lastSlotCount = 0;
            lastSnapshot = string.Empty;
            lastDragEnabled = true;

            for (var i = 0; i < spawnedSlots.Count; i++)
            {
                var slotView = spawnedSlots[i];
                if (slotView == null)
                    continue;

                slotView.Clear(force: true);
                slotView.SetDragEnabled(true);
                if (slotView.gameObject.activeSelf)
                    slotView.gameObject.SetActive(false);
            }
        }

        private void EnsureSlotCount(int targetCount)
        {
            if (targetCount <= spawnedSlots.Count)
                return;

            if (slotTemplate == null)
            {
                Debug.LogWarning("SkillLoadoutSlotsView is missing slotTemplate.");
                return;
            }

            var parent = contentRoot != null ? contentRoot : slotTemplate.transform.parent;
            for (var i = spawnedSlots.Count; i < targetCount; i++)
            {
                var instance = Instantiate(slotTemplate, parent);
                instance.name = string.Format("{0}_{1}", slotTemplate.name, i + 1);
                instance.gameObject.SetActive(true);
                instance.SkillDropped += HandleSkillDropped;
                spawnedSlots.Add(instance);
            }
        }

        private void HandleSkillDropped(int slotIndex, PlayerSkillModel skill)
        {
            var handler = SkillDropped;
            if (handler != null)
                handler(slotIndex, skill);
        }

        private static string BuildSnapshot(IReadOnlyList<SkillLoadoutSlotModel> slots)
        {
            if (slots == null || slots.Count == 0)
                return string.Empty;

            var parts = new string[slots.Count];
            for (var i = 0; i < slots.Count; i++)
            {
                parts[i] = string.Concat(
                    slots[i].SlotIndex.ToString(CultureInfo.InvariantCulture),
                    ":",
                    slots[i].HasSkill ? "1" : "0",
                    ":",
                    slots[i].HasSkill && slots[i].Skill.HasValue
                        ? slots[i].Skill.Value.PlayerSkillId.ToString(CultureInfo.InvariantCulture)
                        : "0");
            }

            return string.Join("|", parts);
        }
    }
}
