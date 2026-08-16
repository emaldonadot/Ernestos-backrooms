using System;
using System.Collections.Generic;
using EndlessRooms.Core;
using EndlessRooms.Player;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessRooms.UI
{
    /// <summary>
    /// One pre-built inventory slot — icon + name + a selection-box frame the HUD
    /// toggles on the currently-selected slot. Built once at scene-build time
    /// (<c>Milestone9Level1AssetBuilder.BuildInventoryHudUi</c>) up to
    /// <see cref="InventoryState.MaxItems"/>; unused slots (no item carried yet) are
    /// just hidden rather than destroyed/recreated.
    /// </summary>
    [Serializable]
    public struct InventoryHudSlot
    {
        public GameObject Root;
        public Image Icon;
        public Text NameText;
        public GameObject SelectionBox;
    }

    /// <summary>
    /// A row of icon+name slots along the bottom of the screen, with a highlighted box
    /// around the currently selected one — replaces the earlier plain-text readout now
    /// that items have real rendered icons (see
    /// Milestone9Level1AssetBuilder.RenderItemIcon).
    /// </summary>
    public sealed class InventoryHudController : MonoBehaviour
    {
        [SerializeField] private Inventory _inventory;
        [SerializeField] private InventorySelectionController _selection;
        [SerializeField] private List<InventoryHudSlot> _slots = new();

        private void OnEnable()
        {
            if (_inventory != null)
            {
                _inventory.Changed += Refresh;
            }

            if (_selection != null)
            {
                _selection.SelectionChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_inventory != null)
            {
                _inventory.Changed -= Refresh;
            }

            if (_selection != null)
            {
                _selection.SelectionChanged -= Refresh;
            }
        }

        private void Refresh()
        {
            if (_inventory == null)
            {
                return;
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                InventoryHudSlot slot = _slots[i];
                bool hasItem = i < _inventory.Items.Count;

                if (slot.Root != null)
                {
                    slot.Root.SetActive(hasItem);
                }

                if (!hasItem)
                {
                    continue;
                }

                InventoryItemDefinition item = _inventory.Items[i];

                if (slot.Icon != null)
                {
                    slot.Icon.sprite = item.Icon;
                    slot.Icon.enabled = item.Icon != null;
                }

                if (slot.NameText != null)
                {
                    slot.NameText.text = item.DisplayName;
                }

                if (slot.SelectionBox != null)
                {
                    bool isSelected = _selection != null && _selection.SelectedIndex == i;
                    slot.SelectionBox.SetActive(isSelected);
                }
            }
        }
    }
}
