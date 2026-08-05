using System;
using System.Collections.Generic;

namespace EndlessRooms.Core
{
    /// <summary>
    /// Single authority point for executing <see cref="IWorldCommand"/> instances.
    /// Single-player today; the intended future extension point is a server-authoritative
    /// subclass/wrapper that validates ownership before calling <see cref="IWorldCommand.Execute"/>.
    /// </summary>
    public sealed class WorldCommandExecutor
    {
        private readonly List<IWorldCommand> _history = new List<IWorldCommand>();

        public event Action<IWorldCommand> CommandExecuted;

        public IReadOnlyList<IWorldCommand> History => _history;

        public void Submit(IWorldCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.Execute();
            _history.Add(command);
            CommandExecuted?.Invoke(command);
        }
    }
}
