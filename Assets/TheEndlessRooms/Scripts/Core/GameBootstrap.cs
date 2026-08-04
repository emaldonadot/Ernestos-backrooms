using UnityEngine;

namespace EndlessRooms.Core
{
    /// <summary>
    /// Registers cross-cutting services that must exist before any other system runs.
    /// Add one instance to the test/gameplay scene; safe to call Awake multiple times
    /// across scene loads since registration is idempotent.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (!GameServices.TryGet<WorldCommandExecutor>(out _))
            {
                GameServices.Register(new WorldCommandExecutor());
            }
        }
    }
}
