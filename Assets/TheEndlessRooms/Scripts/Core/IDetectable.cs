using UnityEngine;

namespace EndlessRooms.Core
{
    /// <summary>
    /// Marks something that creature perception (Milestone 7) can sense visually or
    /// aurally. Defined now, alongside the other Milestone 1 core interfaces, so the
    /// player controller can expose noise output without waiting on the AI framework.
    /// </summary>
    public interface IDetectable
    {
        Transform DetectionPoint { get; }

        /// <summary>Normalized 0-1 noise output; consumed by creature audio perception later.</summary>
        float NoiseLevel { get; }

        /// <summary>True while concealed in a <c>HidingSpot</c> — visual perception must always fail against a hidden target regardless of line-of-sight.</summary>
        bool IsHidden { get; }
    }
}
