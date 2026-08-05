namespace EndlessRooms.Map
{
    /// <summary>How much the player knows about a room. Never demoted once promoted.</summary>
    public enum RoomDiscoveryState
    {
        /// <summary>Not on the map at all.</summary>
        Unknown,

        /// <summary>A neighboring room has been entered, so this room's position (but not its category) is known.</summary>
        Glimpsed,

        /// <summary>The player has actually walked into this room.</summary>
        Entered
    }
}
