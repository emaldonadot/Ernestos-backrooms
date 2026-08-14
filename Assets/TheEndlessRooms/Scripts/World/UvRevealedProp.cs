using EndlessRooms.Core;
using UnityEngine;

namespace EndlessRooms.World
{
    /// <summary>
    /// Toggles a separate target GameObject (completely inert, invisible, not
    /// interactable) active/inactive based on whether the UV flashlight is on — a
    /// global toggle (not distance/angle gated against the actual beam) for simplicity,
    /// the same scope-reduction <see cref="EndlessRooms.Player.PlayerUvFlashlight"/>
    /// uses for its battery requirement instead of a real combine UI. This component
    /// has to live on an object that stays active itself (e.g. an empty parent), since a
    /// disabled GameObject never runs OnEnable to (re)subscribe to the event — it can't
    /// toggle its own GameObject's activity for that same reason.
    /// </summary>
    public sealed class UvRevealedProp : MonoBehaviour
    {
        [SerializeField] private GameObject _target;

        private void OnEnable()
        {
            GameEvents.UvLightToggled += HandleUvLightToggled;
            HandleUvLightToggled(false);
        }

        private void OnDisable()
        {
            GameEvents.UvLightToggled -= HandleUvLightToggled;
        }

        private void HandleUvLightToggled(bool isOn)
        {
            if (_target != null)
            {
                _target.SetActive(isOn);
            }
        }
    }
}
