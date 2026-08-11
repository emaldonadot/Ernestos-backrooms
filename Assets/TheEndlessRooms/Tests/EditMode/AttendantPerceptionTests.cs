using EndlessRooms.AI;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    public class AttendantPerceptionTests
    {
        private static AttendantConfig MakeConfig()
        {
            var config = ScriptableObject.CreateInstance<AttendantConfig>();
            config.VisionRangeMeters = 10f;
            config.VisionAngleDegrees = 90f;
            config.HearingRangeMeters = 10f;
            config.NoiseDetectionThreshold = 0.5f;
            return config;
        }

        [Test]
        public void Evaluate_TargetInRangeAndInViewAndUnoccluded_CanSee()
        {
            var perception = new AttendantPerception(MakeConfig());

            PerceptionResult result = perception.Evaluate(
                selfPosition: Vector3.zero,
                selfForward: Vector3.forward,
                targetPosition: new Vector3(0f, 0f, 5f),
                targetNoiseLevel: 0f,
                targetIsHidden: false,
                hasClearLineOfSight: (_, _) => true);

            Assert.IsTrue(result.CanSeeTarget);
        }

        [Test]
        public void Evaluate_TargetOutsideVisionRange_CannotSee()
        {
            var perception = new AttendantPerception(MakeConfig());

            PerceptionResult result = perception.Evaluate(
                Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 50f), 0f, false, (_, _) => true);

            Assert.IsFalse(result.CanSeeTarget);
        }

        [Test]
        public void Evaluate_TargetBehindVisionAngle_CannotSee()
        {
            var perception = new AttendantPerception(MakeConfig());

            PerceptionResult result = perception.Evaluate(
                Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -5f), 0f, false, (_, _) => true);

            Assert.IsFalse(result.CanSeeTarget);
        }

        [Test]
        public void Evaluate_LineOfSightOccluded_CannotSee()
        {
            var perception = new AttendantPerception(MakeConfig());

            PerceptionResult result = perception.Evaluate(
                Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 5f), 0f, false, (_, _) => false);

            Assert.IsFalse(result.CanSeeTarget);
        }

        [Test]
        public void Evaluate_HiddenTarget_CannotSeeEvenWithClearLineOfSight()
        {
            var perception = new AttendantPerception(MakeConfig());

            PerceptionResult result = perception.Evaluate(
                Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 5f), 0f, targetIsHidden: true, hasClearLineOfSight: (_, _) => true);

            Assert.IsFalse(result.CanSeeTarget);
        }

        [Test]
        public void Evaluate_LoudNoiseNearby_CanHear()
        {
            var perception = new AttendantPerception(MakeConfig());

            PerceptionResult result = perception.Evaluate(
                Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 2f), targetNoiseLevel: 1f, targetIsHidden: false, hasClearLineOfSight: (_, _) => false);

            Assert.IsTrue(result.CanHearTarget);
        }

        [Test]
        public void Evaluate_QuietNoiseFar_CannotHear()
        {
            var perception = new AttendantPerception(MakeConfig());

            PerceptionResult result = perception.Evaluate(
                Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 9f), targetNoiseLevel: 0.6f, targetIsHidden: false, hasClearLineOfSight: (_, _) => false);

            Assert.IsFalse(result.CanHearTarget, "Distance falloff should reduce a moderate noise level below threshold near max hearing range.");
        }

        [Test]
        public void Evaluate_NoNoise_CannotHear()
        {
            var perception = new AttendantPerception(MakeConfig());

            PerceptionResult result = perception.Evaluate(
                Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 1f), targetNoiseLevel: 0f, targetIsHidden: false, hasClearLineOfSight: (_, _) => false);

            Assert.IsFalse(result.CanHearTarget);
        }
    }
}
