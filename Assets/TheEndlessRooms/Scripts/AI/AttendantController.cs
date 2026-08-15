using System;
using System.Collections.Generic;
using EndlessRooms.Core;
using EndlessRooms.Player;
using EndlessRooms.Procedural;
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
        private PlayerController _targetPlayerController;
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
        /// Deliberately does *not* resolve a home room or subscribe to doors here: this
        /// runs from <see cref="OnEnable"/>, which fires before
        /// <c>LevelPlayerSpawner.Start()</c> has actually generated the level, so
        /// <see cref="ProceduralLevelBuilder.LastGraph"/> would still be null (or stale
        /// from a previous build). That part happens in <see cref="OnLevelBuilt"/>
        /// instead, driven by <see cref="ProceduralLevelBuilder.LevelBuilt"/>.
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
            // Null for VR rigs (no PlayerController there) — forcing a VR camera to snap
            // to a direction the player didn't choose is disorienting/nausea-inducing, so
            // the capture look-snap below is PC-only by construction, not an oversight.
            _targetPlayerController = playerGo != null ? playerGo.GetComponentInChildren<PlayerController>() : null;

            if (_levelBuilder != null)
            {
                _levelBuilder.LevelBuilt += OnLevelBuilt;
                if (_levelBuilder.LastGraph != null)
                {
                    OnLevelBuilt(_levelBuilder.LastGraph);
                }
            }
        }

        private bool _hasBeenEnabledBefore;

        private void OnEnable()
        {
            EnsureInitialized();

            // EnsureInitialized's own door-subscription (via OnLevelBuilt) only ever
            // runs on the very first OnEnable — fine when a level only builds once, but
            // Milestone 9's AttendantAppearanceController disables/re-enables this
            // GameObject repeatedly to make the Attendant appear and disappear, and
            // OnDisable always unsubscribes. Without this, "investigates recently opened
            // doors" would silently stop working after the first disable/enable cycle.
            if (_hasBeenEnabledBefore)
            {
                SubscribeToDoors();
            }

            _hasBeenEnabledBefore = true;
        }

        private void OnDisable()
        {
            if (_levelBuilder != null)
            {
                _levelBuilder.LevelBuilt -= OnLevelBuilt;
            }

            UnsubscribeFromDoors();
        }

        /// <summary>
        /// Fires once the level is actually built (initial build, or any later rebuild
        /// — e.g. <c>RespawnController</c>'s no-save fallback). Places the Attendant in
        /// a real, generated room a few hops from the entry — rather than trusting
        /// wherever its Transform happened to be positioned at edit time, which has no
        /// guarantee of landing inside instantiated geometry — and (re-)subscribes to
        /// the newly-instantiated doors, since the old ones no longer exist.
        /// </summary>
        private void OnLevelBuilt(RoomGraph graph)
        {
            List<Guid> nearEntry = RoomGraphPathfinder.GetNodesWithinHops(graph, graph.EntryNodeId, Mathf.Max(_config.TerritoryRoomRadius, 1));
            _homeNodeId = nearEntry.Count > 1 ? nearEntry[1] : nearEntry[0];
            _currentPatrolNodeId = _homeNodeId;

            ResetToHomePosition();

            UnsubscribeFromDoors();
            SubscribeToDoors();
        }

        /// <summary>
        /// Teleports back to its home node, clears any in-progress path, and starts a
        /// fresh state machine (so it's always back in Patrol, never stuck resuming
        /// whatever state it was in before) — the logic <see cref="OnLevelBuilt"/> always
        /// ran once at level start, now also reusable by
        /// <c>AttendantAppearanceController</c> every time the Attendant reappears after
        /// being hidden, so every hunt starts from the same clean spawn instead of
        /// wherever it physically stopped when it last vanished.
        /// </summary>
        public void ResetToHomePosition()
        {
            if (_levelBuilder != null && _levelBuilder.TryGetRoomWorldPosition(_homeNodeId, out Vector3 homePosition))
            {
                _characterController.enabled = false;
                transform.position = homePosition;
                _characterController.enabled = true;
            }

            _pathWaypoints.Clear();
            _waypointIndex = 0;
            _unstickTimer = 0f;
            ResetStuckTracking();
            _stateMachine = new AttendantStateMachine(_config);
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

        private Vector3 _stuckCheckPosition;
        private float _stuckTimer;
        private float _unstickTimer;
        private int _unstickDirectionSign = 1;
        private const float StuckDetectionThresholdSeconds = 1.2f;
        private const float StuckMinProgressMeters = 0.15f;
        private const float UnstickDurationSeconds = 0.6f;

        private void FollowWaypoints(float speed)
        {
            if (_unstickTimer > 0f)
            {
                PerformUnstickStrafe(speed);
                return;
            }

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

                ResetStuckTracking();
                return;
            }

            Vector3 moveDirection = toWaypoint.normalized;
            transform.forward = moveDirection;
            OpenBlockingDoor(moveDirection);

            Vector3 motion = moveDirection * speed * Time.deltaTime;
            _characterController.Move(motion);

            TrackStuckProgress();
        }

        /// <summary>
        /// Straight-line waypoint following (no navmesh, per the design doc's
        /// deliberate MVP scope) can clip a wall corner near a door opening and get
        /// permanently wedged — confirmed via debug logging: collisionFlags stuck
        /// non-zero with zero net position change for many seconds. Rather than
        /// building real obstacle avoidance, this detects "no progress for over a
        /// second while actively trying to move" and strafes perpendicular to the
        /// blocked direction for a short burst, alternating sides on repeated
        /// failures, then forces a fresh path calculation from wherever it ends up.
        /// </summary>
        private void TrackStuckProgress()
        {
            float distanceSinceLastCheck = Vector3.Distance(transform.position, _stuckCheckPosition);
            if (distanceSinceLastCheck >= StuckMinProgressMeters)
            {
                ResetStuckTracking();
                return;
            }

            _stuckTimer += Time.deltaTime;
            if (_stuckTimer < StuckDetectionThresholdSeconds)
            {
                return;
            }

            _unstickTimer = UnstickDurationSeconds;
            _unstickDirectionSign *= -1;
            ResetStuckTracking();
        }

        private void ResetStuckTracking()
        {
            _stuckCheckPosition = transform.position;
            _stuckTimer = 0f;
        }

        private void PerformUnstickStrafe(float speed)
        {
            _unstickTimer -= Time.deltaTime;

            Vector3 strafeDirection = Vector3.Cross(Vector3.up, transform.forward).normalized * _unstickDirectionSign;
            _characterController.Move(strafeDirection * speed * Time.deltaTime);

            if (_unstickTimer <= 0f)
            {
                _pathWaypoints.Clear();
                _waypointIndex = 0;
                ResetStuckTracking();
            }
        }

        /// <summary>
        /// The Attendant reacts to doors the player opens (<see cref="OnDoorToggled"/>),
        /// but that's not enough on its own — patrol/chase paths cross rooms via doors
        /// that start closed, and a closed door's panel is a solid collider spanning
        /// the full wall height, which fully blocks <see cref="CharacterController.Move"/>
        /// with no way through. So it opens a closed, unlocked door directly ahead of it
        /// the same way a player does: through <see cref="Door.Interact"/>, the same
        /// <see cref="ToggleDoorCommand"/> path — not a special AI-only shortcut.
        /// </summary>
        private void OpenBlockingDoor(Vector3 moveDirection)
        {
            float probeDistance = _config.WaypointArrivalRadius + 1.5f;
            if (!Physics.Raycast(_eyes.position, moveDirection, out RaycastHit hit, probeDistance))
            {
                return;
            }

            Door door = hit.collider.GetComponentInParent<Door>();
            if (door != null && !door.IsOpen && !door.IsLocked)
            {
                door.Interact(new InteractionContext(gameObject));
            }
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
                _targetPlayerController?.SnapLookAt(_eyes != null ? _eyes.position : transform.position);
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
