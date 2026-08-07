using System;
using System.Collections.Generic;
using EndlessRooms.Core;
using EndlessRooms.Player;
using EndlessRooms.World;
using UnityEngine;

namespace EndlessRooms.AI
{
    /// <summary>
    /// The Attendant: a territorial per-Sector patroller that investigates recently
    /// opened doors and disturbed noise (see docs/MILESTONE_PLAN.md's identity
    /// section). Ties <see cref="AttendantPerception"/> and
    /// <see cref="AttendantStateMachine"/> (both pure C#, tested independently) to real
    /// raycasts, a <see cref="CharacterController"/>, and graph-based movement via
    /// <see cref="RoomGraphPathfinder"/>. Finds the player through
    /// <see cref="IDetectable"/> rather than a direct reference, so it works unchanged
    /// against PC's <see cref="PlayerController"/> or VR's <see cref="VRNoiseSource"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class AttendantController : MonoBehaviour
    {
        [SerializeField] private AttendantConfig _config;
        [SerializeField] private ProceduralLevelBuilder _levelBuilder;
        [SerializeField] private Transform _eyes;
        [SerializeField] private LayerMask _visionOcclusionMask = ~0;
        [SerializeField] private AudioSource _stateAudioSource;
        [SerializeField] private AudioClip _patrolCue;
        [SerializeField] private AudioClip _investigateCue;
        [SerializeField] private AudioClip _chaseCue;

        private CharacterController _characterController;
        private AttendantPerception _perception;
        private AttendantStateMachine _stateMachine;
        private IDetectable _target;
        private CameraShakeEffect _targetCameraShake;
        private readonly System.Random _rng = new();

        private Guid _homeNodeId;
        private Guid _currentPatrolNodeId;
        private readonly List<Vector3> _pathWaypoints = new();
        private int _waypointIndex;
        private Door _pendingDoorEvent;
        private AttendantState _lastAnnouncedState;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (_eyes == null)
            {
                _eyes = transform;
            }
        }

        /// <summary>
        /// Wires runtime dependencies that would otherwise be found via Awake/OnEnable
        /// — exposed as a public method (see DECISIONS.md's 2026-08-07 MonoBehaviour
        /// lifecycle entry) so headless tooling and real gameplay share one code path.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_perception != null)
            {
                return;
            }

            _perception = new AttendantPerception(_config);
            _stateMachine = new AttendantStateMachine(_config);

            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            _target = playerGo != null ? playerGo.GetComponentInChildren<IDetectable>() : null;
            _targetCameraShake = playerGo != null ? playerGo.GetComponentInChildren<CameraShakeEffect>() : null;

            if (_levelBuilder != null && _levelBuilder.LastGraph != null)
            {
                _homeNodeId = RoomGraphPathfinder.FindNearestNode(_levelBuilder.LastGraph, transform.position, NodeToWorld);
                _currentPatrolNodeId = _homeNodeId;
            }

            SubscribeToDoors();
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void OnDisable()
        {
            UnsubscribeFromDoors();
        }

        private void SubscribeToDoors()
        {
            if (_levelBuilder == null)
            {
                return;
            }

            foreach (Transform child in _levelBuilder.transform)
            {
                Door door = child.GetComponent<Door>();
                if (door != null)
                {
                    door.DoorToggled += OnDoorToggled;
                }
            }
        }

        private void UnsubscribeFromDoors()
        {
            if (_levelBuilder == null)
            {
                return;
            }

            foreach (Transform child in _levelBuilder.transform)
            {
                Door door = child.GetComponent<Door>();
                if (door != null)
                {
                    door.DoorToggled -= OnDoorToggled;
                }
            }
        }

        private void OnDoorToggled(Door door)
        {
            float distance = Vector3.Distance(transform.position, door.transform.position);
            if (distance <= _config.DoorReactionRangeMeters)
            {
                _pendingDoorEvent = door;
            }
        }

        private void Update()
        {
            if (_perception == null)
            {
                return;
            }

            PerceptionResult perceptionResult = EvaluatePerception();
            Vector3? doorEventPosition = _pendingDoorEvent != null ? _pendingDoorEvent.transform.position : null;
            _pendingDoorEvent = null;

            bool hasArrived = _pathWaypoints.Count == 0;
            var tick = new AttendantPerceptionTick(perceptionResult, doorEventPosition, hasArrived, Time.deltaTime);
            AttendantDecision decision = _stateMachine.Tick(tick);

            AnnounceStateIfChanged(decision.State);
            MoveTowardStateTarget(decision);
            HandleCapture(decision);
        }

        private PerceptionResult EvaluatePerception()
        {
            if (_target == null)
            {
                return new PerceptionResult(false, false, transform.position);
            }

            Vector3 targetPosition = _target.DetectionPoint != null ? _target.DetectionPoint.position : Vector3.zero;
            return _perception.Evaluate(_eyes.position, _eyes.forward, targetPosition, _target.NoiseLevel, _target.IsHidden, HasClearLineOfSight);
        }

        private bool HasClearLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            float distance = direction.magnitude;
            if (distance <= 0.0001f)
            {
                return true;
            }

            // Shortened slightly so the check tests everything between the two points
            // (walls, closed doors) without clipping the target's own collider at the
            // very end of the ray and reporting a false "occluded by the player".
            float checkDistance = Mathf.Max(distance - 0.2f, 0f);
            return !Physics.Raycast(from, direction.normalized, checkDistance, _visionOcclusionMask, QueryTriggerInteraction.Ignore);
        }

        private void MoveTowardStateTarget(AttendantDecision decision)
        {
            float speed = decision.State == AttendantState.Chase ? _config.ChaseSpeed : _config.PatrolSpeed;

            if (decision.State == AttendantState.Patrol)
            {
                TickPatrolRoute();
            }
            else if (decision.State == AttendantState.Returning)
            {
                EnsurePathTo(_homeNodeId);
            }
            else
            {
                EnsurePathToPosition(decision.LastKnownTargetPosition);
            }

            FollowWaypoints(speed);
        }

        private void TickPatrolRoute()
        {
            if (_pathWaypoints.Count > 0)
            {
                return;
            }

            if (_levelBuilder == null || _levelBuilder.LastGraph == null)
            {
                return;
            }

            List<Guid> territoryNodes = RoomGraphPathfinder.GetNodesWithinHops(_levelBuilder.LastGraph, _homeNodeId, _config.TerritoryRoomRadius);
            Guid nextNodeId = territoryNodes[_rng.Next(territoryNodes.Count)];
            EnsurePathTo(nextNodeId);
        }

        private void EnsurePathTo(Guid targetNodeId)
        {
            if (_levelBuilder == null || _levelBuilder.LastGraph == null || _pathWaypoints.Count > 0)
            {
                return;
            }

            Guid fromNodeId = RoomGraphPathfinder.FindNearestNode(_levelBuilder.LastGraph, transform.position, NodeToWorld);
            List<Guid> path = RoomGraphPathfinder.FindPath(_levelBuilder.LastGraph, fromNodeId, targetNodeId);

            foreach (Guid nodeId in path)
            {
                if (_levelBuilder.TryGetRoomWorldPosition(nodeId, out Vector3 worldPosition))
                {
                    _pathWaypoints.Add(worldPosition);
                }
            }

            _waypointIndex = 0;
        }

        private void EnsurePathToPosition(Vector3 targetPosition)
        {
            if (_levelBuilder == null || _levelBuilder.LastGraph == null || _pathWaypoints.Count > 0)
            {
                return;
            }

            Guid nearestToTarget = RoomGraphPathfinder.FindNearestNode(_levelBuilder.LastGraph, targetPosition, NodeToWorld);
            EnsurePathTo(nearestToTarget);
        }

        private void FollowWaypoints(float speed)
        {
            if (_pathWaypoints.Count == 0 || _waypointIndex >= _pathWaypoints.Count)
            {
                _pathWaypoints.Clear();
                _waypointIndex = 0;
                return;
            }

            Vector3 waypoint = _pathWaypoints[_waypointIndex];
            Vector3 toWaypoint = waypoint - transform.position;
            toWaypoint.y = 0f;

            if (toWaypoint.magnitude <= _config.WaypointArrivalRadius)
            {
                _waypointIndex++;
                if (_waypointIndex >= _pathWaypoints.Count)
                {
                    _pathWaypoints.Clear();
                    _waypointIndex = 0;
                }

                return;
            }

            Vector3 moveDirection = toWaypoint.normalized;
            transform.forward = moveDirection;
            _characterController.Move(moveDirection * speed * Time.deltaTime);
        }

        private Vector3 NodeToWorld(Guid nodeId)
        {
            return _levelBuilder.TryGetRoomWorldPosition(nodeId, out Vector3 position) ? position : transform.position;
        }

        private void HandleCapture(AttendantDecision decision)
        {
            if (decision.State != AttendantState.Chase || _target?.DetectionPoint == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, _target.DetectionPoint.position);

            if (_targetCameraShake != null)
            {
                float intensity = 1f - Mathf.Clamp01(distance / Mathf.Max(_config.VisionRangeMeters, 0.01f));
                _targetCameraShake.SetChaseIntensity(intensity);
            }

            if (distance <= _config.CaptureRangeMeters)
            {
                GameEvents.RaisePlayerCaptured();
                ResetAfterCapture();
            }
        }

        private void ResetAfterCapture()
        {
            _pathWaypoints.Clear();
            _waypointIndex = 0;
            _pendingDoorEvent = null;
        }

        private void AnnounceStateIfChanged(AttendantState state)
        {
            if (state == _lastAnnouncedState || _stateAudioSource == null)
            {
                _lastAnnouncedState = state;
                return;
            }

            _lastAnnouncedState = state;
            AudioClip clip = state switch
            {
                AttendantState.Chase => _chaseCue,
                AttendantState.Investigate => _investigateCue,
                AttendantState.Search => _investigateCue,
                _ => _patrolCue,
            };

            if (clip != null)
            {
                _stateAudioSource.PlayOneShot(clip);
            }
        }
    }
}
