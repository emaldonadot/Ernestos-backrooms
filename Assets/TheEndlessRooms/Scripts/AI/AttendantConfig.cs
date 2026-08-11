using UnityEngine;

namespace EndlessRooms.AI
{
    /// <summary>
    /// Every tunable for The Attendant lives here, per PRD Section 12's "configurable
    /// behavior through data assets" requirement — none of it is hardcoded in
    /// <see cref="AttendantController"/> or <see cref="AttendantStateMachine"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "AttendantConfig", menuName = "The Endless Rooms/Attendant Config")]
    public sealed class AttendantConfig : ScriptableObject
    {
        [Header("Perception")]
        [Min(0f)] public float VisionRangeMeters = 10f;
        [Range(1f, 180f)] public float VisionAngleDegrees = 70f;
        [Min(0f)] public float HearingRangeMeters = 14f;
        [Range(0f, 1f)] public float NoiseDetectionThreshold = 0.35f;
        [Min(0f)] public float DoorReactionRangeMeters = 16f;

        [Header("Movement")]
        [Min(0f)] public float PatrolSpeed = 2.5f;
        [Min(0f)] public float ChaseSpeed = 5.5f;
        [Min(0f)] public float WaypointArrivalRadius = 0.75f;

        [Header("Timing")]
        [Min(0f)] public float InvestigateDurationSeconds = 6f;
        [Min(0f)] public float SearchDurationSeconds = 8f;

        [Header("Capture")]
        [Min(0f)] public float CaptureRangeMeters = 1.2f;

        [Header("Territory")]
        [Min(0)] public int TerritoryRoomRadius = 3;
    }
}
