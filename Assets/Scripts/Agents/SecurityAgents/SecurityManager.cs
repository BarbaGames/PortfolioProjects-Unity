using System;
using System.Collections.Generic;
using System.Linq;
using Player;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Agents.SecurityAgents
{
    public class SecurityManager : MonoBehaviour
    {
        public static SecurityManager Instance { get; private set; }

        [Header("Security Configuration")]
        [SerializeField] private GameObject secAgentPrefab;
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private Transform retreatPoint;
        [SerializeField] private int maxAgents = 5;
        [SerializeField] private float spawnRadius = 10f;

        [Header("Detection Settings")] 
        [SerializeField] private float globalDetectionRange = 10f;
        [SerializeField] private float globalAttackRange = 1.5f;
        [SerializeField] private float globalSearchRadius = 15f;
        [SerializeField] private float visionConeAngle = 120f;
        [SerializeField] private LayerMask obstacleLayerMask = -1;

        [Header("Patrol Settings")]
        [SerializeField] private float patrolPointRadius = 15f;
        [SerializeField] private int patrolPointsPerAgent = 4;
        [SerializeField] private float minPatrolPointDistance = 5f;

        [Header("Debug & Quality of Life")] 
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showAgentsDebugInfo = true;
        [SerializeField] private bool autoSpawnAgents = true;
        [SerializeField] private bool pauseAllAgents = false;
        [SerializeField] private KeyCode emergencyStopKey = KeyCode.P;
        [SerializeField] private KeyCode resumeKey = KeyCode.R;

        [Header("Performance")] 
        [SerializeField] private int maxAgentsUpdatedPerFrame = 2;
        [SerializeField] private float detectionCheckInterval = 0.1f;

        private readonly Dictionary<SecAgent, float> _agentPerformanceMetrics = new();
        private readonly Dictionary<SecAgent, GameObject> _agentGameObjects = new();
        private readonly Dictionary<SecAgent, PatrolPoint[]> _agentPatrolPoints = new();
        private readonly List<SecAgent> _activeAgents = new();
        private PlayerController _player;
        private bool _allAgentsPaused = false;
        private int _currentAgentUpdateIndex = 0;
        private float _detectionTimer = 0f;


        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _player = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault();
            InitializeAgents();
        }

        private void Update()
        {
            HandleDebugInput();
            UpdateAgentsOptimized();
            CheckForTargets();
            UpdatePerformanceMetrics();
        }

        private void OnValidate()
        {
            SecAgent.ShowDebugInfo = showAgentsDebugInfo;
        }

        private void InitializeAgents()
        {
            for (int i = 0; i < maxAgents; i++)
            {
                CreateAgent(i);
            }
        }

        private void CreateAgent(int index, Vector3? customPosition = null)
        {
            Vector3 spawnPosition = customPosition ?? GetValidSpawnPosition();

            if (!IsValidSpawnPosition(spawnPosition))
            {
                Debug.LogError($"Invalid spawn position for agent {index}: {spawnPosition}");
                return;
            }

            try
            {
                GameObject agentObject = Instantiate(secAgentPrefab, spawnPosition, Quaternion.identity, transform);
                agentObject.name = $"SecurityAgent_{index}";

                // Validate NavMeshAgent
                NavMeshAgent navAgent = agentObject.GetComponent<NavMeshAgent>();
                if (!navAgent)
                {
                    navAgent = agentObject.AddComponent<NavMeshAgent>();
                }

                SecAgent agent = new SecAgent
                {
                    Position = agentObject.transform,
                    DetectionRange = globalDetectionRange,
                    AttackRange = globalAttackRange,
                    SearchRadius = globalSearchRadius,
                    RetreatPoint = retreatPoint,
                };

                // Generate and assign patrol points
                PatrolPoint[] agentPatrolPoints = GeneratePatrolPoints(spawnPosition);
                Transform[] patrolTransforms = agentPatrolPoints.Select(pp => pp.transform).ToArray();

                agent.PatrolPoints = patrolTransforms;
                _agentPatrolPoints[agent] = agentPatrolPoints;

                agent.Init();
                agent.Fsm.ForceTransition(Behaviours.Patrol);

                _agentGameObjects[agent] = agentObject;
                _activeAgents.Add(agent);
                _agentPerformanceMetrics[agent] = 0f;

                if (showDebugInfo)
                {
                    Debug.Log($"Successfully created agent {index} at {spawnPosition}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create agent {index}: {e.Message}");
            }
        }

        private bool IsValidSpawnPosition(Vector3 position)
        {
            return NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas);
        }

        // Enhanced noise detection with distance falloff
        public void OnPlayerNoise(Vector3 noisePosition, float noiseRadius)
        {
            int agentsAlerted = 0;

            foreach (SecAgent agent in _activeAgents)
            {
                if (!agent.Position) continue;

                float distanceToNoise = Vector3.Distance(agent.Position.position, noisePosition);

                if (distanceToNoise <= noiseRadius)
                {
                    // Add distance-based alertness (closer = more urgent)
                    float alertLevel = 1f - (distanceToNoise / noiseRadius);

                    agent.LastKnownPosition = CreateTempTransform(noisePosition);
                    agent.noiseHeard = true;
                    if (agent.CurrentState == Behaviours.Patrol)
                    {
                        agent.Fsm.SendInput(Flags.OnTargetLost);
                        agentsAlerted++;
                    }
                }
            }

            if (showDebugInfo && agentsAlerted > 0)
            {
                Debug.Log($"Player noise alerted {agentsAlerted} agents at {noisePosition}");
            }
        }

        // Add agent statistics
        public void LogAgentStatistics()
        {
            var stateCount = new Dictionary<Behaviours, int>();

            foreach (SecAgent agent in _activeAgents)
            {
                if (stateCount.ContainsKey(agent.CurrentState))
                    stateCount[agent.CurrentState]++;
                else
                    stateCount[agent.CurrentState] = 1;
            }

            Debug.Log("=== Agent Statistics ===");
            foreach (var kvp in stateCount)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value} agents");
            }
        }
        
        private Vector3 GetValidSpawnPosition()
        {
            for (int attempts = 0; attempts < 10; attempts++)
            {
                Vector3 randomPoint = transform.position + Random.insideUnitSphere * spawnRadius;
                randomPoint.y = transform.position.y;

                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }

            return transform.position;
        }

        private PatrolPoint[] GeneratePatrolPoints(Vector3 centerPosition)
        {
            List<PatrolPoint> points = new List<PatrolPoint>();
            List<Vector3> usedPositions = new List<Vector3>();

            for (int i = 0; i < patrolPointsPerAgent; i++)
            {
                Vector3 patrolPosition = FindValidPatrolPoint(centerPosition, usedPositions);

                GameObject pointObject = new GameObject($"PatrolPoint_{i}")
                {
                    transform =
                    {
                        position = patrolPosition
                    }
                };

                pointObject.transform.SetParent(transform);

                PatrolPoint patrolPoint = new PatrolPoint
                {
                    transform = pointObject.transform,
                    waitSeconds = Random.Range(1f, 3f),
                    lookAround = Random.value > 0.5f,
                    lookAngle = Random.Range(30f, 90f),
                    lookSpeed = Random.Range(60f, 120f)
                };

                points.Add(patrolPoint);
                usedPositions.Add(patrolPosition);
            }

            return points.ToArray();
        }

        private Vector3 FindValidPatrolPoint(Vector3 centerPosition, List<Vector3> usedPositions)
        {
            for (int attempts = 0; attempts < 20; attempts++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(minPatrolPointDistance, patrolPointRadius);

                Vector3 candidate = centerPosition + new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance
                );

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    bool validPosition = true;
                    foreach (Vector3 usedPos in usedPositions)
                    {
                        if (Vector3.Distance(hit.position, usedPos) < minPatrolPointDistance)
                        {
                            validPosition = false;
                            break;
                        }
                    }

                    if (validPosition)
                    {
                        return hit.position;
                    }
                }
            }

            return centerPosition;
        }

        public PatrolPoint GetPatrolPointData(int index)
        {
            foreach (KeyValuePair<SecAgent, PatrolPoint[]> kvp in _agentPatrolPoints)
            {
                if (kvp.Value.Length > index)
                {
                    return kvp.Value[index];
                }
            }

            return null;
        }

        private void HandleDebugInput()
        {
            if (Input.GetKeyDown(emergencyStopKey))
            {
                TogglePauseAllAgents();
            }

            if (Input.GetKeyDown(resumeKey))
            {
                ResumeAllAgents();
            }

            // Quick agent count adjustment
            if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                if (_activeAgents.Count < maxAgents)
                {
                    Vector3 spawnPos = GetValidSpawnPosition();
                    CreateAgent(_activeAgents.Count, spawnPos);
                }
            }

            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                if (_activeAgents.Count > 0)
                {
                    RemoveAgent(_activeAgents[^1]);
                }
            }
        }

        private void UpdateAgentsOptimized()
        {
            if (_activeAgents.Count == 0 || _allAgentsPaused) return;

            int agentsToUpdate = Mathf.Min(maxAgentsUpdatedPerFrame, _activeAgents.Count);

            for (int i = 0; i < agentsToUpdate; i++)
            {
                if (_currentAgentUpdateIndex >= _activeAgents.Count)
                    _currentAgentUpdateIndex = 0;

                SecAgent agent = _activeAgents[_currentAgentUpdateIndex];
                if (agent.IsValidState())
                {
                    float startTime = Time.realtimeSinceStartup;
                    agent.Tick();
                    _agentPerformanceMetrics[agent] = Time.realtimeSinceStartup - startTime;
                }

                _currentAgentUpdateIndex++;
            }

            _detectionTimer += Time.deltaTime;
            if (_detectionTimer >= detectionCheckInterval)
            {
                _detectionTimer = 0f;
                foreach (SecAgent agent in _activeAgents)
                {
                    CheckVisualDetection(agent);
                }
            }
        }

        private void UpdatePerformanceMetrics()
        {
            if (!showDebugInfo) return;

            // Show performance info in console every 5 seconds
            if (Time.time % 5f < Time.deltaTime)
            {
                float avgPerformance = _agentPerformanceMetrics.Values.Average();
                Debug.Log(
                    $"Security Manager: {_activeAgents.Count} agents, avg update time: {avgPerformance * 1000f:F2}ms");
            }
        }

        public void TogglePauseAllAgents()
        {
            _allAgentsPaused = !_allAgentsPaused;

            foreach (SecAgent agent in _activeAgents)
            {
                if (_allAgentsPaused)
                    agent.EmergencyStop();
                else
                    agent.Resume();
            }

            if (showDebugInfo)
            {
                Debug.Log($"All agents {(_allAgentsPaused ? "paused" : "resumed")}");
            }
        }

        public void ResumeAllAgents()
        {
            _allAgentsPaused = false;
            foreach (SecAgent agent in _activeAgents)
            {
                agent.Resume();
            }

            if (showDebugInfo)
            {
                Debug.Log("All agents resumed");
            }
        }

        private void CheckForTargets()
        {
            if (_player)
            {
            }
        }

        private void CheckVisualDetection(SecAgent agent)
        {
            if (!_player || !agent.Position) return;

            float distanceToPlayer = Vector3.Distance(agent.Position.position, _player.Position);

            if (distanceToPlayer <= agent.DetectionRange)
            {
                Vector3 directionToPlayer = (_player.Position - agent.Position.position).normalized;
                Vector3 agentForward = agent.Position.forward;

                float angle = Vector3.Angle(agentForward, directionToPlayer);

                if (angle <= visionConeAngle / 2f)
                {
                    if (HasLineOfSight(agent.Position.position, _player.Position))
                    {
                        agent.Target = _player.transform;
                        agent.Fsm.SendInput(Flags.OnTargetFound);
                    }
                }
            }
        }

        private bool HasLineOfSight(Vector3 agentPosition, Vector3 playerPosition)
        {
            Vector3 direction = playerPosition - agentPosition;
            float distance = direction.magnitude;

            Ray ray = new Ray(agentPosition + Vector3.up * 0.5f, direction.normalized);

            return !Physics.Raycast(ray, distance, obstacleLayerMask);
        }
        
        private Transform CreateTempTransform(Vector3 position)
        {
            GameObject tempObject = new GameObject("TempSearchTarget")
            {
                transform =
                {
                    position = position
                }
            };
            return tempObject.transform;
        }

        public void RemoveAgent(SecAgent agent)
        {
            if (!_agentGameObjects.TryGetValue(agent, out GameObject agentObject)) return;
            agent.Reset();
            _activeAgents.Remove(agent);
            _agentGameObjects.Remove(agent);
            Destroy(agentObject);
        }

        public void SetAllAgentsRetreat(bool retreat)
        {
            foreach (SecAgent agent in _activeAgents)
            {
                if (retreat)
                {
                    agent.Retreat = true;
                }
            }
        }

        public List<SecAgent> GetActiveAgents()
        {
            return new List<SecAgent>(_activeAgents);
        }

        public GameObject GetAgentGameObject(SecAgent agent)
        {
            return _agentGameObjects.GetValueOrDefault(agent);
        }

        public Vector3[] GetAgentPositions()
        {
            return _activeAgents.Select(agent => agent.Position.position).ToArray();
        }
        
        public bool CheckLineOfSight(Vector3 fromPosition, Vector3 toPosition)
        {
            Vector3 direction = toPosition - fromPosition;
            float distance = direction.magnitude;
            
            Ray ray = new Ray(fromPosition + Vector3.up * 0.5f, direction.normalized);
            
            return !Physics.Raycast(ray, distance, obstacleLayerMask);
        }

        private void OnDestroy()
        {
            foreach (SecAgent agent in _activeAgents)
            {
                agent.Reset();
            }

            _activeAgents.Clear();
            _agentGameObjects.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);

            if (patrolPoints is { Length: > 1 })
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (!patrolPoints[i]) continue;
                    int nextIndex = (i + 1) % patrolPoints.Length;
                    if (patrolPoints[nextIndex])
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                    }
                }
            }

            if (retreatPoint)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(retreatPoint.position, Vector3.one * 2f);
            }

            // Draw vision cones for agents
            Gizmos.color = Color.green;
            foreach (SecAgent agent in _activeAgents)
            {
                if (agent.Position)
                {
                    DrawVisionCone(agent.Position.position, agent.Position.forward, agent.DetectionRange,
                        visionConeAngle);
                }
            }
        }

        private void DrawVisionCone(Vector3 position, Vector3 forward, float range, float angle)
        {
            float halfAngle = angle / 2f;
            Vector3 leftBoundary = Quaternion.Euler(0, -halfAngle, 0) * forward;
            Vector3 rightBoundary = Quaternion.Euler(0, halfAngle, 0) * forward;

            Gizmos.DrawRay(position, leftBoundary * range);
            Gizmos.DrawRay(position, rightBoundary * range);
            Gizmos.DrawRay(position, forward * range);
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Active Agents: {_activeAgents.Count}/{maxAgents}");
            GUILayout.Label($"Agents Paused: {_allAgentsPaused}");

            if (GUILayout.Button("Toggle Pause All"))
                TogglePauseAllAgents();

            if (GUILayout.Button("Log Statistics"))
                LogAgentStatistics();

            GUILayout.Label("Controls:");
            GUILayout.Label($"Pause/Resume: {emergencyStopKey}");
            GUILayout.Label("Add Agent: +");
            GUILayout.Label("Remove Agent: -");

            GUILayout.EndArea();
        }
    }
}