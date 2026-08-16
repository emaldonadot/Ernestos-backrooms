using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// The progression-hint cobweb: shows a spider while this room's item/clue is the
    /// player's next obtainable step, an empty web while it exists but isn't reachable
    /// yet (or has already been used), and nothing once dismissed with no obtainable
    /// swap-in configured. Every dependency is optional so the same component covers
    /// every case in the design (a freely-sitting item, an item behind a locked drawer,
    /// a safe gated on a known code, a UV-revealed clue with no item at all) without
    /// needing a different clue script per room.
    /// </summary>
    public sealed class SpiderWebClue : MonoBehaviour
    {
        [SerializeField] private Inventory _inventory;
        [Tooltip("The item this clue is about. Once the player holds it, the clue counts as used up. Leave blank for a clue with no physical pickup (e.g. the UV bathroom code).")]
        [SerializeField] private InventoryItemDefinition _obtainedItem;
        [Tooltip("Item the player must already hold for this clue to be currently actionable. Leave blank if nothing is required.")]
        [SerializeField] private InventoryItemDefinition _requiredItem;
        [Tooltip("Extra unlock condition beyond (or instead of) _requiredItem — e.g. a LockableDrawer or KeypadSafe. Must implement IProgressionGate.")]
        [SerializeField] private MonoBehaviour _showGateBehaviour;
        [Tooltip("If set and unlocked, forces this clue to its used-up state regardless of everything else — e.g. the bathroom code clue dismisses once the safe it unlocks has been opened.")]
        [SerializeField] private MonoBehaviour _dismissGateBehaviour;

        [SerializeField] private GameObject _spiderVisual;
        [SerializeField] private GameObject _webOnlyVisual;

        private IProgressionGate _showGate;
        private IProgressionGate _dismissGate;

        private void Awake()
        {
            _showGate = _showGateBehaviour as IProgressionGate;
            _dismissGate = _dismissGateBehaviour as IProgressionGate;
        }

        private void OnEnable()
        {
            if (_inventory != null)
            {
                _inventory.Changed += Refresh;
            }

            if (_showGate != null)
            {
                _showGate.Changed += Refresh;
            }

            if (_dismissGate != null)
            {
                _dismissGate.Changed += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_inventory != null)
            {
                _inventory.Changed -= Refresh;
            }

            if (_showGate != null)
            {
                _showGate.Changed -= Refresh;
            }

            if (_dismissGate != null)
            {
                _dismissGate.Changed -= Refresh;
            }
        }

        private void Refresh()
        {
            bool obtained = _obtainedItem != null && _inventory != null && _inventory.HasItem(_obtainedItem.ItemId);
            bool dismissed = obtained || (_dismissGate != null && _dismissGate.IsUnlocked);

            bool requiredItemHeld = _requiredItem == null || (_inventory != null && _inventory.HasItem(_requiredItem.ItemId));
            bool gateOpen = _showGate == null || _showGate.IsUnlocked;
            bool showSpider = !dismissed && requiredItemHeld && gateOpen;

            if (_spiderVisual != null)
            {
                _spiderVisual.SetActive(showSpider);
            }

            if (_webOnlyVisual != null)
            {
                _webOnlyVisual.SetActive(!showSpider);
            }
        }
    }
}
