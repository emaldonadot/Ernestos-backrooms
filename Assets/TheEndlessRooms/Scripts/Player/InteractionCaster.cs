using System;
using EndlessRooms.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EndlessRooms.Player
{
    /// <summary>
    /// Casts a ray each frame to find the nearest <see cref="IInteractable"/> in
    /// focus, and executes it on the Interact action. Raises <see cref="FocusChanged"/>
    /// so UI (or anything else) can react without this class knowing about the UI.
    /// The ray normally comes from the view camera (PC, mouse-look), but
    /// <see cref="_rayOriginOverride"/> — set to a VR controller's transform — lets the
    /// same component and the same <see cref="IInteractable"/> objects work unchanged
    /// on Quest. Existing PC scenes are unaffected: the override defaults to unset.
    /// </summary>
    public sealed class InteractionCaster : MonoBehaviour
    {
        [SerializeField] private Camera _viewCamera;
        [SerializeField] private InputActionReference _interactAction;
        [SerializeField] private float _interactionRange = 2.5f;
        [SerializeField] private LayerMask _interactionMask = ~0;
        [Tooltip("SphereCast radius instead of a thin ray, so small pickups (batteries, keys, ID cards) don't need pixel-perfect aim.")]
        [SerializeField] private float _aimAssistRadius = 0.12f;

        [Tooltip("Optional. When set (e.g. to a VR controller's transform), this is used as the ray origin/direction instead of the view camera.")]
        [SerializeField] private Transform _rayOriginOverride;

        private IInteractable _focusedInteractable;

        private Transform RayOrigin => _rayOriginOverride != null ? _rayOriginOverride : _viewCamera?.transform;

        public event Action<IInteractable> FocusChanged;

        private void OnEnable()
        {
            if (_interactAction != null)
            {
                _interactAction.action.Enable();
                _interactAction.action.performed += OnInteractPerformed;
            }
        }

        private void OnDisable()
        {
            if (_interactAction != null)
            {
                _interactAction.action.performed -= OnInteractPerformed;
                _interactAction.action.Disable();
            }
        }

        private void Update()
        {
            IInteractable candidate = FindInteractableInView();

            if (!ReferenceEquals(candidate, _focusedInteractable))
            {
                _focusedInteractable = candidate;
                FocusChanged?.Invoke(_focusedInteractable);
                GameEvents.RaiseInteractableFocusChanged(_focusedInteractable);
            }
        }

        private IInteractable FindInteractableInView()
        {
            Transform rayOrigin = RayOrigin;
            if (rayOrigin == null)
            {
                return null;
            }

            var ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (!Physics.SphereCast(ray, _aimAssistRadius, out RaycastHit hit, _interactionRange, _interactionMask, QueryTriggerInteraction.Collide))
            {
                return null;
            }

            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                return null;
            }

            var context = new InteractionContext(gameObject);
            return interactable.CanInteract(context) ? interactable : null;
        }

        private void OnInteractPerformed(InputAction.CallbackContext callbackContext)
        {
            if (_focusedInteractable == null)
            {
                return;
            }

            var context = new InteractionContext(gameObject);
            if (!_focusedInteractable.CanInteract(context))
            {
                return;
            }

            _focusedInteractable.Interact(context);
            GameEvents.RaiseInteractionPerformed(gameObject, _focusedInteractable);
        }
    }
}
