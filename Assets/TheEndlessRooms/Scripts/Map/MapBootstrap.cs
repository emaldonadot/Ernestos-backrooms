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
