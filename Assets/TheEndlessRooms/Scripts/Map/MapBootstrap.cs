using EndlessRooms.Core;
using EndlessRooms.World;
using UnityEngine;

namespace EndlessRooms.Map
{
    /// <summary>
    /// Registers a fresh <see cref="FieldLogService"/> for this Play session and wires
    /// it to the level builder's completion event. This is the one place that needs to
    /// know both Map and World exist — <see cref="FieldLogService"/> itself only
    /// depends on Core and Procedural.
    /// </summary>
    public sealed class MapBootstrap : MonoBehaviour
    {
        [SerializeField] private ProceduralLevelBuilder _levelBuilder;

        private FieldLogService _service;

        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// The actual setup logic, exposed publicly so headless Edit-mode tooling can
        /// call it directly — batch-mode Unity without a Play session doesn't reliably
        /// run Awake for scene-resident objects. Real gameplay always goes through
        /// Awake. Safe to call more than once: re-registers a fresh service rather than
        /// leaving a stale one from a previous call.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_service != null)
            {
                return;
            }

            _service = new FieldLogService();
            GameServices.Register(_service);

            if (_levelBuilder != null)
            {
                _levelBuilder.LevelBuilt += _service.Initialize;
            }
        }

        private void OnDestroy()
        {
            if (_levelBuilder != null)
            {
                _levelBuilder.LevelBuilt -= _service.Initialize;
            }

            _service?.Dispose();
            GameServices.Unregister<FieldLogService>();
        }
    }
}
