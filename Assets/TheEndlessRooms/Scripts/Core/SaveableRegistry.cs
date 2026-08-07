using System.Collections.Generic;

namespace EndlessRooms.Core
{
    /// <summary>
    /// Tracks every live <see cref="ISaveable"/> so the save system can walk them
    /// without an expensive scene-wide find. Mirrors <see cref="WorldCommandExecutor"/>'s
    /// shape: a small, explicit, per-session registry rather than reflection or scene
    /// scanning. Instances register in <c>OnEnable</c> and unregister in
    /// <c>OnDisable</c>.
    /// </summary>
    public sealed class SaveableRegistry
    {
        private readonly List<ISaveable> _saveables = new();

        public void Register(ISaveable saveable)
        {
            if (!_saveables.Contains(saveable))
            {
                _saveables.Add(saveable);
            }
        }

        public void Unregister(ISaveable saveable)
        {
            _saveables.Remove(saveable);
        }

        public IReadOnlyList<ISaveable> GetAll() => _saveables;
    }
}
