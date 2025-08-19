using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Agents
{
    public struct BehaviourActions : IEquatable<BehaviourActions>
    {
        public void AddMainThreadBehaviours(int executionOrder, Action behaviour)
        {
            if (MainThreadBehaviour == null)
                MainThreadBehaviour = new Dictionary<int, List<Action>>();

            if (MainThreadBehaviour.ContainsKey(executionOrder))
                MainThreadBehaviour[executionOrder].Add(behaviour);
            else
                MainThreadBehaviour.Add(executionOrder, new List<Action> { behaviour });
        }

        public void AddMultiThreadableBehaviours(int executionOrder, Action behaviour)
        {
            if (MultiThreadablesBehaviour == null)
                MultiThreadablesBehaviour = new ConcurrentDictionary<int, ConcurrentBag<Action>>();

            if (MultiThreadablesBehaviour.ContainsKey(executionOrder))
                MultiThreadablesBehaviour[executionOrder].Add(behaviour);
            else
                MultiThreadablesBehaviour.TryAdd(executionOrder, new ConcurrentBag<Action> { behaviour });
        }

        public void SetTransitionBehaviour(Action behaviour)
        {
            TransitionBehaviour = behaviour;
        }

        public Dictionary<int, List<Action>> MainThreadBehaviour { get; private set; }

        public ConcurrentDictionary<int, ConcurrentBag<Action>> MultiThreadablesBehaviour { get; private set; }

        public Action TransitionBehaviour { get; private set; }

        public bool Equals(BehaviourActions other)
        {
            return Equals(MainThreadBehaviour, other.MainThreadBehaviour) && Equals(MultiThreadablesBehaviour, other.MultiThreadablesBehaviour) && Equals(TransitionBehaviour, other.TransitionBehaviour);
        }

        public override bool Equals(object obj)
        {
            return obj is BehaviourActions other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MainThreadBehaviour, MultiThreadablesBehaviour, TransitionBehaviour);
        }
    }

    public abstract class State
    {
        public Action<Enum> OnFlag;
        public abstract BehaviourActions GetTickBehaviour(params object[] parameters);
        public abstract BehaviourActions GetOnEnterBehaviour(params object[] parameters);
        public abstract BehaviourActions GetOnExitBehaviour(params object[] parameters);
    }
}