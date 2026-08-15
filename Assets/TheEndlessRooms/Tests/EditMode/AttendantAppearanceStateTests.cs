using EndlessRooms.AI;
using NUnit.Framework;

namespace EndlessRooms.Tests.EditMode
{
    public class AttendantAppearanceStateTests
    {
        private static AttendantAppearanceState MakeState(float idleSeconds = 10f, float warningSeconds = 3f, float huntSeconds = 20f)
        {
            return new AttendantAppearanceState(idleSeconds, idleSeconds, warningSeconds, huntSeconds, (_, _) => idleSeconds);
        }

        [Test]
        public void Constructor_StartsInIdle()
        {
            AttendantAppearanceState state = MakeState();

            Assert.AreEqual(AttendantAppearancePhase.Idle, state.Phase);
        }

        [Test]
        public void Tick_BeforeIdleDurationElapses_StaysIdleAndReturnsFalse()
        {
            AttendantAppearanceState state = MakeState(idleSeconds: 10f);

            bool changed = state.Tick(5f);

            Assert.IsFalse(changed);
            Assert.AreEqual(AttendantAppearancePhase.Idle, state.Phase);
        }

        [Test]
        public void Tick_AfterIdleDurationElapses_TransitionsToWarning()
        {
            AttendantAppearanceState state = MakeState(idleSeconds: 10f);

            bool changed = state.Tick(10f);

            Assert.IsTrue(changed);
            Assert.AreEqual(AttendantAppearancePhase.Warning, state.Phase);
        }

        [Test]
        public void Tick_AfterWarningDurationElapses_TransitionsToHunting()
        {
            AttendantAppearanceState state = MakeState(idleSeconds: 10f, warningSeconds: 3f);
            state.Tick(10f); // Idle -> Warning

            bool changed = state.Tick(3f);

            Assert.IsTrue(changed);
            Assert.AreEqual(AttendantAppearancePhase.Hunting, state.Phase);
        }

        [Test]
        public void Tick_AfterHuntDurationElapsesWithoutCapture_ReturnsToIdle()
        {
            AttendantAppearanceState state = MakeState(idleSeconds: 10f, warningSeconds: 3f, huntSeconds: 20f);
            state.Tick(10f); // Idle -> Warning
            state.Tick(3f); // Warning -> Hunting

            bool changed = state.Tick(20f);

            Assert.IsTrue(changed);
            Assert.AreEqual(AttendantAppearancePhase.Idle, state.Phase);
        }

        [Test]
        public void ForceIdle_FromHunting_ImmediatelyReturnsToIdle()
        {
            AttendantAppearanceState state = MakeState(idleSeconds: 10f, warningSeconds: 3f, huntSeconds: 20f);
            state.Tick(10f);
            state.Tick(3f);
            Assert.AreEqual(AttendantAppearancePhase.Hunting, state.Phase);

            state.ForceIdle();

            Assert.AreEqual(AttendantAppearancePhase.Idle, state.Phase);
        }

        [Test]
        public void ForceIdle_ResetsTheIdleTimer()
        {
            AttendantAppearanceState state = MakeState(idleSeconds: 10f, warningSeconds: 3f, huntSeconds: 20f);
            state.Tick(10f);
            state.Tick(3f);

            state.ForceIdle();

            // A tiny tick shouldn't immediately roll into Warning again — the idle timer
            // should have been freshly reset, not left at whatever it was mid-hunt.
            bool changed = state.Tick(0.01f);
            Assert.IsFalse(changed);
            Assert.AreEqual(AttendantAppearancePhase.Idle, state.Phase);
        }
    }
}
