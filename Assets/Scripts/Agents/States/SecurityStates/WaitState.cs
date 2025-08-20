using System;
using Agents.SecurityAgents;
using UnityEngine;

namespace Agents.States.SecurityStates
{
    public class WaitState : State
    {
        private float _waitStartTime;
        private bool _lookingAround = false;
        private float _lookDirection = 0f;

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            float waitTime = parameters[0] as float? ?? 0;
            bool shouldMove = parameters[1] as bool? ?? false;
            bool retreat = parameters[2] as bool? ?? false;
            Transform currentNode = parameters.Length > 3 ? parameters[3] as Transform : null;
            bool lookAround = parameters.Length <= 4 || (bool)parameters[4];

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                // Look around behavior during wait
                if (!lookAround || !currentNode) return;
                _lookDirection += 45f * Time.deltaTime;
                Vector3 lookDir = Quaternion.Euler(0, _lookDirection, 0) * Vector3.forward;
                currentNode.rotation = Quaternion.LookRotation(lookDir);
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                if (shouldMove)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }

                // Wait timeout
                if (Time.time - _waitStartTime >= waitTime)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }
            });

            return behaviours;
        }

        public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                _waitStartTime = Time.time;
                _lookDirection = 0f;
            });

            return behaviours;
        }

        public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
        {
            return default;
        }
    }
}