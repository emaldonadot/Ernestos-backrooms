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
            EnsureRegistered();
        }

        /// <summary>
        /// The actual registration logic, exposed publicly (not just private Awake) so
        /// headless Edit-mode tooling can call it directly. Batch-mode Unity without a
        /// Play session doesn't reliably run Awake for scene-resident objects — only
        /// for objects instantiated at runtime — so tooling that never enters Play mode
        /// can't depend on Awake alone. Real gameplay always goes through Awake.
        /// </summary>
        public void EnsureRegistered()
        {
            if (!GameServices.TryGet<WorldCommandExecutor>(out _))
            {
                GameServices.Register(new WorldCommandExecutor());
            }

            if (!GameServices.TryGet<SaveableRegistry>(out _))
            {
                GameServices.Register(new SaveableRegistry());
            }
        }
    }
}
