using System.Text;
using EndlessRooms.Core;
using EndlessRooms.Player;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessRooms.UI
{
    /// <summary>
    /// A simple text readout of carried items along the bottom of the screen, with the
    /// currently selected one marked — enough to test picking items up, cycling
    /// selection, and using the selected one, without needing per-item icon art yet.
    /// </summary>
    public sealed class InventoryHudController : MonoBehaviour
    {
        [SerializeField] private Inventory _inventory;
        [SerializeField] private InventorySelectionController _selection;
        [SerializeField] private Text _listText;

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
            if (_listText == null || _inventory == null)
            {
                return;
            }

            if (_inventory.Items.Count == 0)
            {
                _listText.text = "(no items — [ ] to cycle, F to use)";
                return;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < _inventory.Items.Count; i++)
            {
                bool isSelected = _selection != null && _selection.SelectedIndex == i;
                builder.Append(isSelected ? "> " : "  ");
                builder.Append(_inventory.Items[i].DisplayName);
                if (i < _inventory.Items.Count - 1)
                {
                    builder.Append("   ");
                }
            }

            _listText.text = builder.ToString();
        }
    }
}
