using System;
using UnityEngine;

namespace EndlessRooms.AI
{
    public readonly struct PerceptionResult
    {
        public readonly bool CanSeeTarget;
        public readonly bool CanHearTarget;
        public readonly Vector3 TargetPosition;

        public PerceptionResult(bool canSeeTarget, bool canHearTarget, Vector3 targetPosition)
        {
            CanSeeTarget = canSeeTarget;
            CanHearTarget = canHearTarget;
            TargetPosition = targetPosition;
        }
    }

    /// <summary>
    /// Pure C# perception math — no <see cref="MonoBehaviour"/>, no direct
    /// <see cref="Physics"/> call — so it's fully EditMode-testable across synthetic
    /// scenarios without a scene. The line-of-sight occlusion check is injected as a
    /// delegate specifically so tests can substitute a fake "always/never clear" check
    /// instead of needing real colliders.
    /// </summary>
    public sealed class AttendantPerception
    {
        private readonly AttendantConfig _config;

        public AttendantPerception(AttendantConfig config)
        {
            _config = config;
        }

        public PerceptionResult Evaluate(
            Vector3 selfPosition,
            Vector3 selfForward,
            Vector3 targetPosition,
            float targetNoiseLevel,
            bool targetIsHidden,
            Func<Vector3, Vector3, bool> hasClearLineOfSight)
        {
            bool canSee = !targetIsHidden && CanSeeTarget(selfPosition, selfForward, targetPosition, hasClearLineOfSight);
            bool canHear = CanHearTarget(selfPosition, targetPosition, targetNoiseLevel);

            return new PerceptionResult(canSee, canHear, targetPosition);
        }

        private bool CanSeeTarget(Vector3 selfPosition, Vector3 selfForward, Vector3 targetPosition, Func<Vector3, Vector3, bool> hasClearLineOfSight)
        {
            Vector3 toTarget = targetPosition - selfPosition;
            float distance = toTarget.magnitude;
            if (distance > _config.VisionRangeMeters)
            {
                return false;
            }

            if (distance <= 0.0001f)
            {
                return true;
            }

            float angle = Vector3.Angle(selfForward, toTarget);
            if (angle > _config.VisionAngleDegrees * 0.5f)
            {
                return false;
            }

            return hasClearLineOfSight == null || hasClearLineOfSight(selfPosition, targetPosition);
        }

        private bool CanHearTarget(Vector3 selfPosition, Vector3 targetPosition, float targetNoiseLevel)
        {
            float distance = Vector3.Distance(selfPosition, targetPosition);
            if (distance > _config.HearingRangeMeters)
            {
                return false;
            }

            float falloff = 1f - Mathf.Clamp01(distance / Mathf.Max(_config.HearingRangeMeters, 0.01f));
            float perceivedLevel = targetNoiseLevel * falloff;
            return perceivedLevel >= _config.NoiseDetectionThreshold;
        }
    }
}
