using EndlessRooms.Core;

namespace EndlessRooms.World
{
    /// <summary>World-mutating command that flips a door's open/closed state.</summary>
    public sealed class ToggleDoorCommand : IWorldCommand
    {
        private readonly Door _door;

        public ToggleDoorCommand(Door door)
        {
            _door = door;
        }

        public string CommandId => $"ToggleDoor:{_door.SaveId}";

        public void Execute()
        {
            _door.SetOpen(!_door.IsOpen);
        }
    }
}
