using System.Collections.Generic;
using System.Linq;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

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

        private Dictionary<SecAgent, GameObject> _agentGameObjects = new();
        private Dictionary<SecAgent, TextMeshPro> _agentText = new();
        private Dictionary<SecAgent, PatrolPoint[]> _agentPatrolPoints = new();
        private List<SecAgent> _activeAgents = new();
        private PlayerController _player;
        private float _stateTextUpdateTimer;
        private const float StateTextUpdateInterval = 0.25f;


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
            _player = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            InitializeAgents();
        }

        private void Update()
        {
            UpdateAgents();
            CheckForTargets();
        }

        private void InitializeAgents()
        {
            for (int i = 0; i < maxAgents; i++)
            {
                CreateAgent(i);
            }
        }

        private void CreateAgent(int index)
        {
            Vector3 spawnPosition = GetValidSpawnPosition();

            GameObject agentObject = Instantiate(secAgentPrefab, spawnPosition, Quaternion.identity, transform);
            agentObject.name = $"SecurityAgent_{index}";

            if (!agentObject.GetComponent<NavMeshAgent>())
            {
                agentObject.AddComponent<NavMeshAgent>();
            }

            SecAgent agent = new SecAgent
            {
                Position = agentObject.transform,
                DetectionRange = globalDetectionRange,
                AttackRange = globalAttackRange,
                SearchRadius = globalSearchRadius,
                RetreatPoint = retreatPoint
            };

            PatrolPoint[] agentPatrolPoints = GeneratePatrolPoints(spawnPosition);
            Transform[] patrolTransforms = agentPatrolPoints.Select(pp => pp.transform).ToArray();

            agent.PatrolPoints = patrolTransforms;
            _agentPatrolPoints[agent] = agentPatrolPoints;
            _agentText[agent] = agentObject.GetComponentInChildren<TextMeshPro>();

            agent.Init();
            agent.Fsm.ForceTransition(Behaviours.Retreat);

            _agentGameObjects[agent] = agentObject;
            _activeAgents.Add(agent);
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

        private void UpdateAgents()
        {
            SecAgent.deltaTime = Time.deltaTime;
            foreach (SecAgent agent in _activeAgents)
            {
                agent.Tick(Time.deltaTime);
                CheckVisualDetection(agent);
            }

            _stateTextUpdateTimer += Time.deltaTime;

            if (_stateTextUpdateTimer < StateTextUpdateInterval) return;

            _stateTextUpdateTimer = 0f;
            Color color;
            foreach (KeyValuePair<SecAgent, TextMeshPro> kvp in _agentText)
            {
                if (kvp.Key.Position && kvp.Value)
                {
                    color = kvp.Key.CurrentState switch
                    {
                        Behaviours.Patrol => Color.green,
                        Behaviours.Chase => Color.red,
                        Behaviours.Search => Color.yellow,
                        Behaviours.Attack => Color.magenta,
                        Behaviours.Retreat => Color.cyan,
                        _ => Color.white
                    };
                    kvp.Value.text = kvp.Key.CurrentState.ToString();
                    kvp.Value.color = color;
                }
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

        public void OnPlayerNoise(Vector3 noisePosition, float noiseRadius)
        {
            foreach (SecAgent agent in _activeAgents)
            {
                if (!agent.Position) continue;

                float distanceToNoise = Vector3.Distance(agent.Position.position, noisePosition);

                if (distanceToNoise <= noiseRadius)
                {
                    agent.LastKnownPosition = CreateTempTransform(noisePosition);

                    if (agent.CurrentState == Behaviours.Patrol)
                    {
                        agent.Fsm.SendInput(Flags.OnTargetLost);
                    }
                }
            }
        }

        private Transform CreateTempTransform(Vector3 position)
        {
            GameObject tempObject = new GameObject("TempSearchTarget");
            tempObject.transform.position = position;
            return tempObject.transform;
        }

        public void AddAgent(Vector3 position)
        {
            if (_activeAgents.Count >= maxAgents) return;

            GameObject agentObject = Instantiate(secAgentPrefab, position, Quaternion.identity, transform);
            agentObject.name = $"SecurityAgent_{_activeAgents.Count}";

            SecAgent agent = new SecAgent
            {
                Position = agentObject.transform,
                DetectionRange = globalDetectionRange,
                AttackRange = globalAttackRange,
                SearchRadius = globalSearchRadius,
                PatrolPoints = patrolPoints,
                RetreatPoint = retreatPoint
            };

            agent.Init();
            agent.Fsm.ForceTransition(Behaviours.Patrol);

            _agentGameObjects[agent] = agentObject;
            _activeAgents.Add(agent);
        }

        public void RemoveAgent(SecAgent agent)
        {
            if (_agentGameObjects.TryGetValue(agent, out GameObject agentObject))
            {
                agent.Reset();
                _activeAgents.Remove(agent);
                _agentGameObjects.Remove(agent);
                Destroy(agentObject);
            }
        }

        public void SetTarget(Transform target)
        {
        }

        public void ClearTarget()
        {
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
            return _agentGameObjects.TryGetValue(agent, out GameObject gameObject) ? gameObject : null;
        }

        public Vector3[] GetAgentPositions()
        {
            return _activeAgents.Select(agent => agent.Position.position).ToArray();
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

            if (patrolPoints != null && patrolPoints.Length > 1)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] != null)
                    {
                        int nextIndex = (i + 1) % patrolPoints.Length;
                        if (patrolPoints[nextIndex] != null)
                        {
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                        }
                    }
                }
            }

            if (retreatPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(retreatPoint.position, Vector3.one * 2f);
            }

            // Draw vision cones for agents
            Gizmos.color = Color.green;
            foreach (SecAgent agent in _activeAgents)
            {
                if (agent.Position != null)
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
    }
}