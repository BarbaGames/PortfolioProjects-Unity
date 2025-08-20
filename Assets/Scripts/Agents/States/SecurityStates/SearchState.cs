using System;
using Agents.SecurityAgents;
using Unity.Mathematics;
using UnityEngine;

namespace Agents.States.SecurityStates
{
    public class SearchState : State
    {
        private float _searchStartTime;
        private float _maxSearchDuration = 10f;
        private Vector3[] _searchPoints;
        private int _currentSearchPointIndex;
        private float _searchPointReachDistance = 2f;

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            Action move = parameters[0] as Action;
            Transform currentNode = parameters[1] as Transform;
            Transform target = parameters[2] as Transform;
            float detectionRange = parameters[3] as float? ?? 0;
            bool retreat = parameters[4] as bool? ?? false;
            bool hasLineOfSight = parameters.Length > 7 && (bool)parameters[5];

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (!currentNode) return;
                move?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                // Found target with line of sight
                if (target && currentNode && hasLineOfSight)
                {
                    float distance = math.distance(currentNode.position, target.position);
                    if (distance <= detectionRange)
                    {
                        OnFlag?.Invoke(Flags.OnTargetFound);
                        return;
                    }
                }

                // Search timeout
                if (Time.time - _searchStartTime > _maxSearchDuration)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }

                // Check if we've reached current search point
                if (_searchPoints == null || _currentSearchPointIndex >= _searchPoints.Length) return;
                Vector3 currentSearchPoint = _searchPoints[_currentSearchPointIndex];
                if (!(Vector3.Distance(currentNode.position, currentSearchPoint) <= _searchPointReachDistance)) return;
                _currentSearchPointIndex++;
                if (_currentSearchPointIndex >= _searchPoints.Length)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost); // Finished searching all points
                }
            });

            return behaviours;
        }

        public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                _searchStartTime = Time.time;
                _currentSearchPointIndex = 0;

                // Generate search pattern around last known position
                if (parameters[0] is Vector3 lastKnownPos)
                {
                    GenerateSearchPattern(lastKnownPos, parameters[1] as float? ?? 15f);
                }
            });

            return behaviours;
        }

        private void GenerateSearchPattern(Vector3 center, float radius)
        {
            _searchPoints = new Vector3[4]; // Search in 4 directions

            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector3 searchPoint = center + new Vector3(
                    Mathf.Cos(angle) * radius * 0.7f,
                    0,
                    Mathf.Sin(angle) * radius * 0.7f
                );
                _searchPoints[i] = searchPoint;
            }
        }

        public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                _searchPoints = null;
                _currentSearchPointIndex = 0;
            });

            return behaviours;
        }
    }
}