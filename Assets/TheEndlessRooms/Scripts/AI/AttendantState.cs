namespace EndlessRooms.AI
{
    /// <summary>
    /// Collapses PRD Section 12's Idle/Patrol/Suspicion/Investigation/Detection/Chase/
    /// Search/Losing-the-player/Returning-to-territory list into five states: Patrol
    /// covers Idle+Patrol (a territorial patroller is never truly idle), Investigate
    /// covers Suspicion+Investigation (both are "something's off, go check"), Chase
    /// covers Detection+Chase (detection is the trigger, not a lingering state), and
    /// Search covers Search+Losing-the-player (losing sight of the player during a
    /// chase transitions straight into searching its last known position).
    /// </summary>
    public enum AttendantState
    {
        Patrol,
        Investigate,
        Chase,
        Search,
        Returning,
    }
}
