using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Agents.SecurityAgents;

namespace Agents
{
    public class Fsm<TEnumState, TEnumFlag>
        where TEnumState : Enum
        where TEnumFlag : Enum
    {
        private const int UnassignedTransition = -1;
        private readonly Dictionary<int, Func<object[]>> _behaviourOnEnterParameters;
        private readonly Dictionary<int, Func<object[]>> _behaviourOnExitParameters;
        private readonly Dictionary<int, State> _behaviours;
        private readonly Dictionary<int, Func<object[]>> _behaviourTickParameters;

        private readonly object _fsmLock = new();

        // Queue any transition requests that occur while a transition is in progress.
        private readonly Queue<TransitionRequest> _queuedTransitions = new();
        private readonly (int destinationInState, Action onTransition)[,] _transitions;

        private readonly ParallelOptions _parallelOptions = new()
        {
            MaxDegreeOfParallelism = 5
        };

        private int _currentState;
        private bool _isTransitioning;
        public Action<int> OnStateChange;

        public Fsm()
        {
            int states = Enum.GetValues(typeof(TEnumState)).Length;
            int flags = Enum.GetValues(typeof(TEnumFlag)).Length;
            _behaviours = new Dictionary<int, State>();
            _transitions = new (int, Action)[states, flags];

            for (int i = 0; i < states; i++)
            for (int j = 0; j < flags; j++)
                _transitions[i, j] = (UnassignedTransition, null);

            _behaviourTickParameters = new Dictionary<int, Func<object[]>>();
            _behaviourOnEnterParameters = new Dictionary<int, Func<object[]>>();
            _behaviourOnExitParameters = new Dictionary<int, Func<object[]>>();
        }

        private int CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;
                OnStateChange?.Invoke(_currentState);
            }
        }

        private BehaviourActions GetCurrentStateOnEnterBehaviours
        {
            get
            {
                int state;
                lock (_fsmLock)
                {
                    state = CurrentState;
                }

                return _behaviours[state].GetOnEnterBehaviour(_behaviourOnEnterParameters[state]?.Invoke());
            }
        }

        private BehaviourActions GetCurrentStateOnExitBehaviours
        {
            get
            {
                int state;
                lock (_fsmLock)
                {
                    state = CurrentState;
                }

                return _behaviours[state].GetOnExitBehaviour(_behaviourOnExitParameters[state]?.Invoke());
            }
        }

        private BehaviourActions GetCurrentStateTickBehaviours
        {
            get
            {
                int state;
                lock (_fsmLock)
                {
                    state = CurrentState;
                }

                return _behaviours[state].GetTickBehaviour(_behaviourTickParameters[state]?.Invoke());
            }
        }

        /// <summary>
        ///     A forced transition externally invoked. (For example, a user might
        ///     decide that the FSM should immediately go to a specific state.)
        /// </summary>
        public void ForceTransition(Enum state)
        {
            lock (_fsmLock)
            {
                if (_isTransitioning)
                {
                    // If a transition is in progress, queue this forced transition.
                    _queuedTransitions.Enqueue(new TransitionRequest(true, state));
                    return;
                }

                _isTransitioning = true;
            }

            try
            {
                ProcessForceTransition(state);
                ProcessQueuedTransitions();
            }
            finally
            {
                lock (_fsmLock)
                {
                    _isTransitioning = false;
                }
            }
        }

        public void SetTransition(Enum originState, Enum flag, Enum destinationState, Action onTransition = null)
        {
            lock (_fsmLock)
            {
                _transitions[Convert.ToInt32(originState), Convert.ToInt32(flag)] =
                    (Convert.ToInt32(destinationState), onTransition);
            }
        }

        public void AddBehaviour<T>(TEnumState stateIndexEnum,
            Func<object[]> onTickParameters = null,
            Func<object[]> onEnterParameters = null,
            Func<object[]> onExitParameters = null)
            where T : State, new()
        {
            int stateIndex = Convert.ToInt32(stateIndexEnum);
            lock (_fsmLock)
            {
                if (_behaviours.ContainsKey(stateIndex))
                    return;

                State newBehaviour = new T();
                newBehaviour.OnFlag += Transition;
                _behaviours.Add(stateIndex, newBehaviour);
                _behaviourTickParameters.Add(stateIndex, onTickParameters);
                _behaviourOnEnterParameters.Add(stateIndex, onEnterParameters);
                _behaviourOnExitParameters.Add(stateIndex, onExitParameters);
            }
        }

        /// <summary>
        ///     This method is registered as an event handler on each state’s OnFlag event.
        ///     It gets called when a state wants to transition.
        /// </summary>
        private void Transition(Enum flag)
        {
            lock (_fsmLock)
            {
                if (_isTransitioning)
                {
                    // Queue the transition request if we are already in the middle of a transition.
                    _queuedTransitions.Enqueue(new TransitionRequest(false, flag));
                    return;
                }

                _isTransitioning = true;
            }

            try
            {
                ProcessTransition(flag);
                ProcessQueuedTransitions();
            }
            finally
            {
                lock (_fsmLock)
                {
                    _isTransitioning = false;
                }
            }
        }

        /// <summary>
        ///     Process a flag-based transition.
        ///     Looks up the transition in the table, executes exit behaviours, calls any onTransition callback,
        ///     changes state, then executes enter behaviours.
        /// </summary>
        private void ProcessTransition(Enum flag)
        {
            int flagInt = Convert.ToInt32(flag);
            int currentState;
            lock (_fsmLock)
            {
                currentState = CurrentState;
            }

            (int destinationInState, Action onTransition) transition = _transitions[currentState, flagInt];
            if (transition.destinationInState == UnassignedTransition)
                return;

            // Execute exit behaviours for the current state.
            BehaviourActions exitBehaviours;
            lock (_fsmLock)
            {
                exitBehaviours = _behaviours[CurrentState].GetOnExitBehaviour(
                    _behaviourOnExitParameters[CurrentState]?.Invoke());
            }

            ExecuteBehaviour(exitBehaviours);
            ExecuteBehaviour(exitBehaviours, true);

            // Invoke any transition callback.
            transition.onTransition?.Invoke();

            // Change to the new state.
            lock (_fsmLock)
            {
                CurrentState = transition.destinationInState;
            }

            // Execute enter behaviours for the new state.
            BehaviourActions enterBehaviours;
            lock (_fsmLock)
            {
                enterBehaviours = _behaviours[CurrentState].GetOnEnterBehaviour(
                    _behaviourOnEnterParameters[CurrentState]?.Invoke());
            }

            ExecuteBehaviour(enterBehaviours);
            ExecuteBehaviour(enterBehaviours, true);
        }

        /// <summary>
        ///     Process a forced transition.
        /// </summary>
        private void ProcessForceTransition(Enum state)
        {
            // Execute exit behaviours for the current state.
            BehaviourActions exitBehaviours;
            lock (_fsmLock)
            {
                exitBehaviours = _behaviours[CurrentState].GetOnExitBehaviour(
                    _behaviourOnExitParameters[CurrentState]?.Invoke());
            }

            ExecuteBehaviour(exitBehaviours);
            ExecuteBehaviour(exitBehaviours, true);

            int forcedState = Convert.ToInt32(state);
            if (forcedState == UnassignedTransition)
                return;

            // Change state.
            lock (_fsmLock)
            {
                CurrentState = forcedState;
            }

            // Execute enter behaviours for the new state.
            BehaviourActions enterBehaviours;
            lock (_fsmLock)
            {
                enterBehaviours = _behaviours[CurrentState].GetOnEnterBehaviour(
                    _behaviourOnEnterParameters[CurrentState]?.Invoke());
            }

            ExecuteBehaviour(enterBehaviours);
            ExecuteBehaviour(enterBehaviours, true);
        }

        /// <summary>
        ///     After finishing a transition, process any transition requests that were queued.
        /// </summary>
        private void ProcessQueuedTransitions()
        {
            while (true)
            {
                TransitionRequest req;
                lock (_fsmLock)
                {
                    if (_queuedTransitions.Count == 0)
                        break;
                    req = _queuedTransitions.Dequeue();
                }

                if (req.IsForce)
                    ProcessForceTransition(req.Value);
                else
                    ProcessTransition(req.Value);
            }
        }

        public void Tick()
        {
            int state;
            lock (_fsmLock)
            {
                state = CurrentState;
            }

            if (!_behaviours.ContainsKey(state))
                return;

            BehaviourActions tickBehaviours;
            lock (_fsmLock)
            {
                tickBehaviours = _behaviours[state].GetTickBehaviour(
                    _behaviourTickParameters[state]?.Invoke());
            }

            ExecuteBehaviour(tickBehaviours);
            ExecuteBehaviour(tickBehaviours, true);
        }

        public void ExecuteBehaviour(BehaviourActions behaviourActions, bool multi = false)
        {
            if (behaviourActions.Equals(default))
                return;

            int executionOrder = 0;

            if (multi)
                while (behaviourActions.MultiThreadablesBehaviour is { Count: > 0 })
                {
                    ExecuteMultiThreadBehaviours(behaviourActions, executionOrder);
                    executionOrder++;
                }
            else
                while (behaviourActions.MainThreadBehaviour is { Count: > 0 })
                {
                    ExecuteMainThreadBehaviours(behaviourActions, executionOrder);
                    executionOrder++;
                }

            behaviourActions.TransitionBehaviour?.Invoke();
        }

        public void ExecuteBehaviour(BehaviourActions behaviourActions, int executionOrder, bool multi = false)
        {
            if (multi)
                ExecuteMultiThreadBehaviours(behaviourActions, executionOrder);
            else
                ExecuteMainThreadBehaviours(behaviourActions, executionOrder);

            behaviourActions.TransitionBehaviour?.Invoke();
        }

        public void ExecuteMainThreadBehaviours(BehaviourActions behaviourActions, int executionOrder)
        {
            if (behaviourActions.MainThreadBehaviour != null &&
                behaviourActions.MainThreadBehaviour.TryGetValue(executionOrder, out List<Action> actions))
            {
                foreach (Action action in actions) action.Invoke();

                // Remove the executed actions so they can be garbage collected.
                behaviourActions.MainThreadBehaviour.Remove(executionOrder);
            }
        }

        public int GetMainThreadCount()
        {
            int state;
            lock (_fsmLock)
            {
                state = CurrentState;
            }

            BehaviourActions currentStateBehaviours;
            lock (_fsmLock)
            {
                currentStateBehaviours = _behaviours[state].GetTickBehaviour(
                    _behaviourTickParameters[state]?.Invoke());
            }

            if (currentStateBehaviours.MainThreadBehaviour == null)
                return 0;

            return currentStateBehaviours.MainThreadBehaviour.Count;
        }

        public int GetMultiThreadCount()
        {
            int state;
            lock (_fsmLock)
            {
                state = CurrentState;
            }

            BehaviourActions currentStateBehaviours;
            lock (_fsmLock)
            {
                currentStateBehaviours = _behaviours[state].GetTickBehaviour(
                    _behaviourTickParameters[state]?.Invoke());
            }

            return currentStateBehaviours.MultiThreadablesBehaviour == null
                ? 0
                : currentStateBehaviours.MultiThreadablesBehaviour.Count;
        }

        public void ExecuteMultiThreadBehaviours(BehaviourActions behaviourActions, int executionOrder)
        {
            ConcurrentDictionary<int, ConcurrentBag<Action>> multiThreadables;
            multiThreadables = behaviourActions.MultiThreadablesBehaviour;
            if (multiThreadables == null)
                return;

            if (!multiThreadables.TryGetValue(executionOrder, out ConcurrentBag<Action> actions))
                return;

            int count = actions.Count;
            if (count == 0)
            {
                multiThreadables.TryRemove(executionOrder, out _);
                return;
            }

            if (count == 1)
            {
                if (actions.TryPeek(out Action singleAction)) singleAction?.Invoke();
            }
            else
            {
                // For multiple actions, convert to an array and use Parallel.For.
                Action[] actionsArray = actions.ToArray();
                Parallel.For(0, actionsArray.Length, _parallelOptions, i => { actionsArray[i]?.Invoke(); });
            }

            multiThreadables.TryRemove(executionOrder, out _);
        }


        public void MultiThreadTick(int executionOrder)
        {
            int state;
            lock (_fsmLock)
            {
                state = CurrentState;
            }

            if (!_behaviours.ContainsKey(state))
                return;

            BehaviourActions tickBehaviours;
            lock (_fsmLock)
            {
                tickBehaviours = _behaviours[state].GetTickBehaviour(
                    _behaviourTickParameters[state]?.Invoke());
            }

            ExecuteBehaviour(tickBehaviours, executionOrder, true);
        }

        public void MainThreadTick(int executionOrder)
        {
            int state;
            lock (_fsmLock)
            {
                state = CurrentState;
            }

            if (!_behaviours.ContainsKey(state))
                return;

            BehaviourActions tickBehaviours;
            lock (_fsmLock)
            {
                tickBehaviours = _behaviours[state].GetTickBehaviour(
                    _behaviourTickParameters[state]?.Invoke());
            }

            ExecuteBehaviour(tickBehaviours, executionOrder);
        }

        private struct TransitionRequest
        {
            /// <summary>
            ///     If true, this is a forced transition (the parameter is a state).
            ///     If false, it is a flag-based transition.
            /// </summary>
            public readonly bool IsForce;

            public readonly Enum Value;

            public TransitionRequest(bool isForce, Enum value)
            {
                IsForce = isForce;
                Value = value;
            }
        }

        public void SendInput(Flags flag)
        {
            Transition((TEnumFlag)(object)flag);
        }
    }
}