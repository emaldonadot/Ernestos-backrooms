using UnityEngine;

namespace EndlessRooms.AI
{
    public readonly struct AttendantPerceptionTick
    {
        public readonly PerceptionResult Perception;
        public readonly Vector3? DoorEventPosition;
        public readonly bool HasArrivedAtTarget;
        public readonly float DeltaTime;

        public AttendantPerceptionTick(PerceptionResult perception, Vector3? doorEventPosition, bool hasArrivedAtTarget, float deltaTime)
        {
            Perception = perception;
            DoorEventPosition = doorEventPosition;
            HasArrivedAtTarget = hasArrivedAtTarget;
            DeltaTime = deltaTime;
        }
    }

    public readonly struct AttendantDecision
    {
        public readonly AttendantState State;
        public readonly Vector3 LastKnownTargetPosition;

        public AttendantDecision(AttendantState state, Vector3 lastKnownTargetPosition)
        {
            State = state;
            LastKnownTargetPosition = lastKnownTargetPosition;
        }
    }

    /// <summary>
    /// Pure C# state machine — no <see cref="MonoBehaviour"/>, fully EditMode-testable.
    /// Deliberately owns only "what state am I in and what point am I reacting to,"
    /// not movement or patrol-route selection: <c>AttendantController</c> is the only
    /// thing that knows about the room graph, so it resolves what a Patrol/Returning
    /// target position actually is. This class resolves Investigate/Chase/Search
    /// targets itself via <see cref="_lastKnownTargetPosition"/>, updated only on a
    /// positive detection (sight or sound) or a door event — never from the target's
    /// live position while undetected, so the creature can't "cheat."
    /// </summary>
    public sealed class AttendantStateMachine
    {
        private readonly AttendantConfig _config;
        private AttendantState _state = AttendantState.Patrol;
        private Vector3 _lastKnownTargetPosition;
        private float _stateTimer;

        public AttendantStateMachine(AttendantConfig config)
        {
            _config = config;
        }

        public AttendantState CurrentState => _state;

        public AttendantDecision Tick(AttendantPerceptionTick input)
        {
            _stateTimer += input.DeltaTime;

            if (input.Perception.CanSeeTarget || input.Perception.CanHearTarget)
            {
                _lastKnownTargetPosition = input.Perception.TargetPosition;
            }

            switch (_state)
            {
                case AttendantState.Patrol:
                    TickPatrol(input);
                    break;
                case AttendantState.Investigate:
                    TickInvestigate(input);
                    break;
                case AttendantState.Chase:
                    TickChase(input);
                    break;
                case AttendantState.Search:
                    TickSearch(input);
                    break;
                case AttendantState.Returning:
                    TickReturning(input);
                    break;
            }

            return new AttendantDecision(_state, _lastKnownTargetPosition);
        }

        private void TickPatrol(AttendantPerceptionTick input)
        {
            if (input.Perception.CanSeeTarget)
            {
                TransitionTo(AttendantState.Chase);
            }
            else if (input.Perception.CanHearTarget)
            {
                TransitionTo(AttendantState.Investigate);
            }
            else if (input.DoorEventPosition.HasValue)
            {
                _lastKnownTargetPosition = input.DoorEventPosition.Value;
                TransitionTo(AttendantState.Investigate);
            }
        }

        private void TickInvestigate(AttendantPerceptionTick input)
        {
            if (input.Perception.CanSeeTarget)
            {
                TransitionTo(AttendantState.Chase);
                return;
            }

            if (input.DoorEventPosition.HasValue)
            {
                _lastKnownTargetPosition = input.DoorEventPosition.Value;
                _stateTimer = 0f;
                return;
            }

            if (input.Perception.CanHearTarget)
            {
                _stateTimer = 0f;
                return;
            }

            if (_stateTimer >= _config.InvestigateDurationSeconds)
            {
                TransitionTo(AttendantState.Returning);
            }
        }

        private void TickChase(AttendantPerceptionTick input)
        {
            if (!input.Perception.CanSeeTarget)
            {
                TransitionTo(AttendantState.Search);
            }
        }

        private void TickSearch(AttendantPerceptionTick input)
        {
            if (input.Perception.CanSeeTarget)
            {
                TransitionTo(AttendantState.Chase);
            }
            else if (_stateTimer >= _config.SearchDurationSeconds)
            {
                TransitionTo(AttendantState.Returning);
            }
        }

        private void TickReturning(AttendantPerceptionTick input)
        {
            if (input.Perception.CanSeeTarget)
            {
                TransitionTo(AttendantState.Chase);
            }
            else if (input.HasArrivedAtTarget)
            {
                TransitionTo(AttendantState.Patrol);
            }
        }

        private void TransitionTo(AttendantState next)
        {
            _state = next;
            _stateTimer = 0f;
        }
    }
}
