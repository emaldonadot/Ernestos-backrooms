using EndlessRooms.Core;

namespace EndlessRooms.World
{
    /// <summary>World-mutating command that permanently unlocks a door once its gating puzzle is solved.</summary>
    public sealed class UnlockDoorCommand : IWorldCommand
    {
        private readonly Door _door;

        public UnlockDoorCommand(Door door)
        {
            _door = door;
        }

        public string CommandId => $"UnlockDoor:{_door.SaveId}";

        public void Execute()
        {
            _door.SetLocked(false);
        }
    }
}
