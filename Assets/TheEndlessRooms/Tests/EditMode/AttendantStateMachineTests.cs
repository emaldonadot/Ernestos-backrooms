using EndlessRooms.AI;
using NUnit.Framework;
using UnityEngine;

namespace EndlessRooms.Tests.EditMode
{
    public class AttendantStateMachineTests
    {
        private static AttendantConfig MakeConfig()
        {
            var config = ScriptableObject.CreateInstance<AttendantConfig>();
            config.InvestigateDurationSeconds = 2f;
            config.SearchDurationSeconds = 2f;
            return config;
        }

        private static PerceptionResult NoDetection() => new(false, false, Vector3.zero);

        private static PerceptionResult Seen(Vector3 position) => new(true, false, position);

        private static PerceptionResult Heard(Vector3 position) => new(false, true, position);

        [Test]
        public void Patrol_SeesTarget_TransitionsToChase()
        {
            var machine = new AttendantStateMachine(MakeConfig());

            AttendantDecision decision = machine.Tick(new AttendantPerceptionTick(Seen(new Vector3(1f, 0f, 1f)), null, false, 0.1f));

            Assert.AreEqual(AttendantState.Chase, decision.State);
            Assert.AreEqual(new Vector3(1f, 0f, 1f), decision.LastKnownTargetPosition);
        }

        [Test]
        public void Patrol_HearsTarget_TransitionsToInvestigate()
        {
            var machine = new AttendantStateMachine(MakeConfig());

            AttendantDecision decision = machine.Tick(new AttendantPerceptionTick(Heard(new Vector3(2f, 0f, 0f)), null, false, 0.1f));

            Assert.AreEqual(AttendantState.Investigate, decision.State);
        }

        [Test]
        public void Patrol_DoorEventNearby_TransitionsToInvestigateAtDoorPosition()
        {
            var machine = new AttendantStateMachine(MakeConfig());
            var doorPosition = new Vector3(3f, 0f, 3f);

            AttendantDecision decision = machine.Tick(new AttendantPerceptionTick(NoDetection(), doorPosition, false, 0.1f));

            Assert.AreEqual(AttendantState.Investigate, decision.State);
            Assert.AreEqual(doorPosition, decision.LastKnownTargetPosition);
        }

        [Test]
        public void Investigate_TimesOutWithoutRedetection_TransitionsToReturning()
        {
            var machine = new AttendantStateMachine(MakeConfig());
            machine.Tick(new AttendantPerceptionTick(Heard(Vector3.one), null, false, 0.1f)); // enters Investigate

            AttendantDecision decision = machine.Tick(new AttendantPerceptionTick(NoDetection(), null, false, 3f));

            Assert.AreEqual(AttendantState.Returning, decision.State);
        }

        [Test]
        public void Investigate_SeesTarget_TransitionsToChase()
        {
            var machine = new AttendantStateMachine(MakeConfig());
            machine.Tick(new AttendantPerceptionTick(Heard(Vector3.one), null, false, 0.1f));

            AttendantDecision decision = machine.Tick(new AttendantPerceptionTick(Seen(new Vector3(5f, 0f, 5f)), null, false, 0.1f));

            Assert.AreEqual(AttendantState.Chase, decision.State);
        }

        [Test]
        public void Chase_LosesSight_TransitionsToSearchAtLastKnownPosition()
        {
            var machine = new AttendantStateMachine(MakeConfig());
            var lastSeenPosition = new Vector3(4f, 0f, 4f);
            machine.Tick(new AttendantPerceptionTick(Seen(lastSeenPosition), null, false, 0.1f)); // enters Chase

            AttendantDecision decision = machine.Tick(new AttendantPerceptionTick(NoDetection(), null, false, 0.1f));

            Assert.AreEqual(AttendantState.Search, decision.State);
            Assert.AreEqual(lastSeenPosition, decision.LastKnownTargetPosition, "Search must target the last known position, not the target's current (unknown) position.");
        }

        [Test]
        public void Search_TimesOutWithoutRedetection_TransitionsToReturning()
        {
            var machine = new AttendantStateMachine(MakeConfig());
            machine.Tick(new AttendantPerceptionTick(Seen(Vector3.one), null, false, 0.1f)); // Chase
            machine.Tick(new AttendantPerceptionTick(NoDetection(), null, false, 0.1f)); // Search

            AttendantDecision decision = machine.Tick(new AttendantPerceptionTick(NoDetection(), null, false, 3f));

            Assert.AreEqual(AttendantState.Returning, decision.State);
        }

        [Test]
        public void Search_SeesTargetAgain_TransitionsBackToChase()
        {
            var machine = new AttendantStateMachine(MakeConfig());
            machine.Tick(new AttendantPerceptionTick(Seen(Vector3.one), null, false, 0.1f)); // Chase
            machine.Tick(new AttendantPerceptionTick(NoDetection(), null, false, 0.1f)); // Search

            AttendantDecision decision = machine.Tick(new AttendantPerceptionTick(Seen(new Vector3(9f, 0f, 9f)), null, false, 0.1f));

            Assert.AreEqual(AttendantState.Chase, decision.State);
        }

        [Test]
        public void Returning_ArrivesHome_TransitionsToPatrol()
        {
            var machine = new AttendantStateMachine(MakeConfig());
            machine.Tick(new AttendantPerceptionTick(Heard(Vector3.one), null, false, 0.1f)); // Investigate
            machine.Tick(new AttendantPerceptionTick(NoDetection(), null, false, 3f)); // Returning

            AttendantDecision decision = machine.Tick(new AttendantPerceptionTick(NoDetection(), null, hasArrivedAtTarget: true, 0.1f));

            Assert.AreEqual(AttendantState.Patrol, decision.State);
        }

        [Test]
        public void Returning_SeesTargetBeforeArriving_TransitionsToChase()
        {
            var machine = new AttendantStateMachine(MakeConfig());
            machine.Tick(new AttendantPerceptionTick(Heard(Vector3.one), null, false, 0.1f)); // Investigate
            machine.Tick(new AttendantPerceptionTick(NoDetection(), null, false, 3f)); // Returning

            AttendantDecision decision = machine.Tick(new AttendantPerceptionTick(Seen(new Vector3(7f, 0f, 7f)), null, false, 0.1f));

            Assert.AreEqual(AttendantState.Chase, decision.State);
        }
    }
}
