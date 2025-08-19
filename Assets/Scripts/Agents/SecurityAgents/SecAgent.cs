using System;
using System.Collections;
using Agents.States.SecurityStates;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace Agents.SecurityAgents
{
    public enum Flags
    {
        None,
        OnTargetReach,
        OnTargetLost,
        OnTargetFound,
        OnRetreat,
        OnWait,
    }

    public enum Behaviours
    {
        None,
        Wait,
        Walk,
        Chase,
        Attack,
        Retreat,
        Patrol,
        Search
    }

    [Serializable]
    public class PatrolPoint
    {
        public Transform transform;
        public float waitSeconds = 1.5f;
        public bool lookAround;
        public float lookAngle = 60f;
        public float lookSpeed = 120f;
        public Vector3 faceDirection;
    }

    public class SecAgent
    {
        public Fsm<Behaviours, Flags> Fsm;
        public Behaviours CurrentState;
        public Transform Position;
        public Transform Target;
        
        [Header("Patrol Settings")] 
        public float DetectionRange = 10f;
        public float MaxChaseDistance = 20f;
        public float ReachDistance = 2f;
        public float AttackRange = 1.5f;
        public float SafeDistance = 3f;
        public float SearchRadius = 15f;
        public float WaitTime = 2f;
        public Transform[] PatrolPoints;
        public Transform RetreatPoint;
        public Transform LastKnownPosition;
        public bool Retreat = false;

        [Header("Attack Settings")] 
        public float AttackCooldown = 2f;
        public float AttackDamage = 10f;

        [Header("Search Settings")] 
        public float SearchDuration = 10f;
        private readonly bool _shouldMove = false;
        private readonly bool _stopRetreating = false;
        private NavMeshAgent _navMeshAgent;
        private int _currentPatrolIndex;
        private float _lastAttackTime;
        private float _searchStartTime;
        private Vector3 _spawnPosition;
        private Quaternion _originalRotation;
        private float _lookTimer;
        private bool _isLooking;

        protected Action _onMove;
        protected Action _onAttack;

        [Header("Debug Settings")]
        public bool ShowDebugInfo = true;
        public bool ShowPatrolPath = true;
        public bool ShowVisionCone = true;

        private TextMeshPro _stateText;
        private float _stateTextUpdateTimer = 0f;
        private const float StateTextUpdateInterval = 0.25f;

        // Movement optimization
        private Vector3 _lastDestination = Vector3.zero;
        private const float DestinationThreshold = 0.5f;

        // State tracking for debugging
        private Behaviours _previousState;
        private float _stateChangeTime;

        public virtual void Init()
        {
            _navMeshAgent = Position.GetComponent<NavMeshAgent>();
            if (!_navMeshAgent)
                _navMeshAgent = Position.gameObject.AddComponent<NavMeshAgent>();

            _stateText = Position.GetComponentInChildren<TextMeshPro>();
            if (!_stateText)
            {
                Debug.LogWarning($"No TextMeshPro found on {Position.name} - state display will not work");
            }

            _spawnPosition = Position.position;
            _originalRotation = Position.rotation;

            Fsm = new Fsm<Behaviours, Flags>();
            Fsm.OnStateChange += OnStateChanged;

            _onMove += Move;
            _onAttack += Attack;

            AddFsmBehaviours();
            FsmTransitions();
        }
        
        public virtual void Tick()
        {
            Fsm.Tick();
            UpdateStateDisplay();
        }
        
        public void Reset()
        {
            if (Fsm != null)
            {
                Fsm.OnStateChange -= OnStateChanged;
                Fsm.ForceTransition(Behaviours.None);
            }

            if (_navMeshAgent && _navMeshAgent.isActiveAndEnabled)
            {
                _navMeshAgent.ResetPath();
                _navMeshAgent.isStopped = true;
            }

            Target = null;
            LastKnownPosition = null;
            _searchStartTime = 0f;
            _lastAttackTime = 0f;
        }
        
        private void OnStateChanged(int newState)
        {
            _previousState = CurrentState;
            CurrentState = (Behaviours)Math.Clamp(newState, 0, Enum.GetValues(typeof(Behaviours)).Length);
            _stateChangeTime = Time.time;

            if (ShowDebugInfo)
            {
                Debug.Log($"Agent {Position.name}: {_previousState} -> {CurrentState} at {_stateChangeTime:F2}s");
            }
        }

        private void UpdateStateDisplay()
        {
            _stateTextUpdateTimer += Time.deltaTime;
            if (!(_stateTextUpdateTimer >= StateTextUpdateInterval) || !_stateText) return;
            _stateTextUpdateTimer = 0f;

            Color stateColor = CurrentState switch
            {
                Behaviours.Patrol => Color.green,
                Behaviours.Chase => Color.red,
                Behaviours.Search => Color.yellow,
                Behaviours.Attack => Color.magenta,
                Behaviours.Retreat => Color.cyan,
                Behaviours.Wait => Color.blue,
                _ => Color.white
            };

            _stateText.text = $"{CurrentState}\n{GetStateTimer():F1}s";
            _stateText.color = stateColor;
        }

        private float GetStateTimer()
        {
            return Time.time - _stateChangeTime;
        }

        public bool IsValidState()
        {
            return _navMeshAgent && Position && Fsm != null;
        }

        public void EmergencyStop()
        {
            if (_navMeshAgent)
            {
                _navMeshAgent.isStopped = true;
                _navMeshAgent.ResetPath();
            }

            if (ShowDebugInfo)
            {
                Debug.Log($"Emergency stop called for agent {Position.name}");
            }
        }

        public void Resume()
        {
            if (_navMeshAgent)
            {
                _navMeshAgent.isStopped = false;
            }
        }

        protected virtual void AddFsmBehaviours()
        {
            Fsm.AddBehaviour<WaitState>(Behaviours.Wait, WaitTickParameters);
            Fsm.AddBehaviour<WalkState>(Behaviours.Walk, WalkTickParameters);
            Fsm.AddBehaviour<ChaseState>(Behaviours.Chase, ChaseTickParameters);
            Fsm.AddBehaviour<AttackState>(Behaviours.Attack, AttackTickParameters);
            Fsm.AddBehaviour<RetreatState>(Behaviours.Retreat, RetreatTickParameters);
            Fsm.AddBehaviour<PatrolState>(Behaviours.Patrol, PatrolTickParameters);
            Fsm.AddBehaviour<SearchState>(Behaviours.Search, SearchTickParameters);
            Fsm.AddBehaviour<WaitState>(Behaviours.None, WaitTickParameters);
        }

        #region Movement

        protected virtual void Move()
        {
            if (!_navMeshAgent || !Position) return;

            switch (CurrentState)
            {
                case Behaviours.Patrol:
                    HandlePatrolMovement();
                    break;
                case Behaviours.Chase:
                    HandleChaseMovement();
                    break;
                case Behaviours.Retreat:
                    HandleRetreatMovement();
                    break;
                case Behaviours.Search:
                    HandleSearchMovement();
                    break;
                case Behaviours.Walk:
                    HandleWalkMovement();
                    break;
            }
        }

        private void HandlePatrolMovement()
        {
            if (PatrolPoints == null || PatrolPoints.Length == 0)
            {
                Debug.LogWarning($"Agent {Position.name} has no patrol points assigned!");
                return;
            }

            Transform currentPatrolPoint = PatrolPoints[_currentPatrolIndex];
            if (currentPatrolPoint == null)
            {
                Debug.LogError($"Patrol point {_currentPatrolIndex} is null for agent {Position.name}");
                return;
            }

            SetDestinationOptimized(currentPatrolPoint.position);

            if (!_navMeshAgent.pathPending && _navMeshAgent.remainingDistance < ReachDistance)
            {
                if (!_isLooking)
                {
                    _isLooking = true;
                    _lookTimer = 0f;

                    if (ShowDebugInfo)
                    {
                        Debug.Log($"Agent {Position.name} reached patrol point {_currentPatrolIndex}");
                    }
                }

                HandlePatrolPointWait();
            }
        }

        private void SetDestinationOptimized(Vector3 destination)
        {
            if (Vector3.Distance(destination, _lastDestination) > DestinationThreshold)
            {
                _navMeshAgent.SetDestination(destination);
                _lastDestination = destination;
            }
        }

        private void HandlePatrolPointWait()
        {
            PatrolPoint currentPoint = SecurityManager.Instance.GetPatrolPointData(_currentPatrolIndex);
            if (currentPoint == null) return;

            _lookTimer += Time.deltaTime;

            if (currentPoint.lookAround && _lookTimer < currentPoint.waitSeconds)
            {
                float lookProgress = Mathf.PingPong(_lookTimer * currentPoint.lookSpeed, currentPoint.lookAngle * 2f) -
                                     currentPoint.lookAngle;
                Vector3 lookDirection = Quaternion.Euler(0, lookProgress, 0) * _originalRotation * Vector3.forward;
                Position.rotation = Quaternion.LookRotation(lookDirection);
            }

            if (_lookTimer >= currentPoint.waitSeconds)
            {
                _isLooking = false;
                _currentPatrolIndex = (_currentPatrolIndex + 1) % PatrolPoints.Length;
            }
        }

        private void HandleChaseMovement()
        {
            if (!Target) return;
            _navMeshAgent.SetDestination(Target.position);
        }

        private void HandleRetreatMovement()
        {
            if (!RetreatPoint) return;
            _navMeshAgent.SetDestination(RetreatPoint.position);
        }

        private void HandleSearchMovement()
        {
            if (!LastKnownPosition)
            {
                Fsm.SendInput(Flags.OnRetreat);
                return;
            }

            if (_searchStartTime == 0f)
            {
                _searchStartTime = Time.time;
                _navMeshAgent.SetDestination(LastKnownPosition.position);
            }

            if (Time.time - _searchStartTime > SearchDuration)
            {
                _searchStartTime = 0f;
                LastKnownPosition = null;
                Fsm.SendInput(Flags.OnTargetLost);
                return;
            }

            if (!_navMeshAgent.pathPending && _navMeshAgent.remainingDistance < ReachDistance)
            {
                Vector3 randomSearchPoint =
                    LastKnownPosition.position + UnityEngine.Random.insideUnitSphere * SearchRadius;
                if (NavMesh.SamplePosition(randomSearchPoint, out NavMeshHit hit, SearchRadius, NavMesh.AllAreas))
                {
                    _navMeshAgent.SetDestination(hit.position);
                }
            }
        }

        private void HandleWalkMovement()
        {
            // Default walking behavior - can be customized
            if (!_navMeshAgent.hasPath)
            {
                Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * 5f;
                randomDirection += _spawnPosition;
                if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    _navMeshAgent.SetDestination(hit.position);
                }
            }
        }

        #endregion
        
        protected virtual void Attack()
        {
            if (!Target) return;
            if (Time.time - _lastAttackTime < AttackCooldown) return;

            float distanceToTarget = Vector3.Distance(Position.position, Target.position);
            if (distanceToTarget <= AttackRange)
            {
                _lastAttackTime = Time.time;

                // Face the target
                Vector3 directionToTarget = (Target.position - Position.position).normalized;
                Position.rotation = Quaternion.LookRotation(directionToTarget);

                // Perform attack
                Debug.Log($"Agent {Position.name} attacking target for {AttackDamage} damage!");

                // You can add damage dealing logic here
                PlayerHealth playerHealth = Target.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(AttackDamage);
                }
            }
        }

        #region FsmParams

        private object[] WalkTickParameters()
        {
            return new object[] { _onMove };
        }

        private object[] WaitTickParameters()
        {
            return new object[] { WaitTime, _shouldMove, Retreat };
        }

        private object[] ChaseTickParameters()
        {
            object[] parameters = { _onMove, Position, Target, MaxChaseDistance, ReachDistance, Retreat };
            return parameters;
        }

        private object[] AttackTickParameters()
        {
            return new object[] { _onAttack, Position, Target, AttackRange, Retreat };
        }

        private object[] RetreatTickParameters()
        {
            return new object[] { _onMove, Position, RetreatPoint, SafeDistance, _stopRetreating };
        }

        private object[] PatrolTickParameters()
        {
            return new object[] { _onMove, Position, PatrolPoints, Target, DetectionRange, Retreat };
        }

        private object[] SearchTickParameters()
        {
            return new object[] { _onMove, Position, LastKnownPosition, Target, SearchRadius, DetectionRange, Retreat };
        }

        #endregion

        #region FsmTransitions

        protected virtual void FsmTransitions()
        {
            WalkTransitions();
            WaitTransitions();
            ChaseTransitions();
            AttackTransitions();
            RetreatTransitions();
            PatrolTransitions();
            SearchTransitions();
        }

        protected virtual void WalkTransitions()
        {
            Fsm.SetTransition(Behaviours.Walk, Flags.OnRetreat, Behaviours.Retreat);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnWait, Behaviours.Wait);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnTargetFound, Behaviours.Chase);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnTargetLost, Behaviours.Patrol);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnTargetReach, Behaviours.Patrol);
        }

        protected virtual void WaitTransitions()
        {
            Fsm.SetTransition(Behaviours.Wait, Flags.OnRetreat, Behaviours.Retreat);
            Fsm.SetTransition(Behaviours.Wait, Flags.OnTargetFound, Behaviours.Chase);
            Fsm.SetTransition(Behaviours.Wait, Flags.OnTargetLost, Behaviours.Patrol);
            Fsm.SetTransition(Behaviours.Wait, Flags.OnTargetReach, Behaviours.Attack);
        }

        private void ChaseTransitions()
        {
            Fsm.SetTransition(Behaviours.Chase, Flags.OnTargetLost, Behaviours.Search);
            Fsm.SetTransition(Behaviours.Chase, Flags.OnRetreat, Behaviours.Retreat);
            Fsm.SetTransition(Behaviours.Chase, Flags.OnTargetReach, Behaviours.Attack);
            Fsm.SetTransition(Behaviours.Chase, Flags.OnWait, Behaviours.Wait);
        }

        private void AttackTransitions()
        {
            Fsm.SetTransition(Behaviours.Attack, Flags.OnTargetLost, Behaviours.Search);
            Fsm.SetTransition(Behaviours.Attack, Flags.OnRetreat, Behaviours.Retreat);
            Fsm.SetTransition(Behaviours.Attack, Flags.OnTargetReach, Behaviours.Attack);
            Fsm.SetTransition(Behaviours.Attack, Flags.OnTargetFound, Behaviours.Chase);
        }

        private void RetreatTransitions()
        {
            Fsm.SetTransition(Behaviours.Retreat, Flags.OnTargetLost, Behaviours.Patrol);
            Fsm.SetTransition(Behaviours.Retreat, Flags.OnTargetReach, Behaviours.Patrol);
            Fsm.SetTransition(Behaviours.Retreat, Flags.OnWait, Behaviours.Wait);
        }

        private void PatrolTransitions()
        {
            Fsm.SetTransition(Behaviours.Patrol, Flags.OnTargetLost, Behaviours.Search);
            Fsm.SetTransition(Behaviours.Patrol, Flags.OnRetreat, Behaviours.Retreat);
            Fsm.SetTransition(Behaviours.Patrol, Flags.OnTargetFound, Behaviours.Chase);
            Fsm.SetTransition(Behaviours.Patrol, Flags.OnWait, Behaviours.Wait);
        }

        private void SearchTransitions()
        {
            Fsm.SetTransition(Behaviours.Search, Flags.OnTargetLost, Behaviours.Patrol);
            Fsm.SetTransition(Behaviours.Search, Flags.OnRetreat, Behaviours.Retreat);
            Fsm.SetTransition(Behaviours.Search, Flags.OnTargetFound, Behaviours.Chase);
            Fsm.SetTransition(Behaviours.Search, Flags.OnWait, Behaviours.Wait);
        }

        #endregion
    }
}