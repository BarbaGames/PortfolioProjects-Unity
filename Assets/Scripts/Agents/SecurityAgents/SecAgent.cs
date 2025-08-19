using System;
using Agents.States.AnimalStates;
using Agents.States.SecurityStates;
using Agents.States.TCStates;

namespace Agents.SecurityAgents
{
    public enum Flags
    {
        None,
        OnTargetReach,
        OnTargetLost,
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
        Patrol
    }

    public class SecAgent
    {
        public Fsm<Behaviours, Flags> Fsm;
        public Behaviours CurrentState;
        protected Action _onMove;

        public virtual void Init()
        {
            Fsm = new Fsm<Behaviours, Flags>();
            Fsm.OnStateChange += state =>
                CurrentState = (Behaviours)Math.Clamp(state, 0, Enum.GetValues(typeof(Behaviours)).Length);

            _onMove += Move;

            AddFsmBehaviours();

            FsmTransitions();
        }

        public virtual void Tick(float deltaTime)
        {
            Fsm.Tick();
        }

        public void Reset()
        {
            Fsm.OnStateChange -= state =>
                CurrentState = (Behaviours)Math.Clamp(state, 0, Enum.GetValues(typeof(Behaviours)).Length);
            Fsm.ForceTransition(Behaviours.None);
        }

        protected virtual void AddFsmBehaviours()
        {
            Fsm.AddBehaviour<WaitState>(Behaviours.Wait, WaitTickParameters);
            Fsm.AddBehaviour<WalkState>(Behaviours.Walk, WalkTickParameters);
            Fsm.AddBehaviour<ChaseState>(Behaviours.Chase, ChaseTickParameters);
            Fsm.AddBehaviour<AttackState>(Behaviours.Attack, AttackTickParameters);
            Fsm.AddBehaviour<RetreatState>(Behaviours.Retreat, RetreatTickParameters);
            Fsm.AddBehaviour<PatrolState>(Behaviours.Patrol, PatrolTickParameters);
        }

        private object[] PatrolTickParameters()
        {
            throw new NotImplementedException();
        }

        private object[] AttackTickParameters()
        {
            throw new NotImplementedException();
        }

        private object[] RetreatTickParameters()
        {
            throw new NotImplementedException();
        }

        private object[] ChaseTickParameters()
        {
            throw new NotImplementedException();
        }

        private object[] WalkTickParameters()
        {
            return default;
        }

        private object[] WaitTickParameters()
        {
            return default;
        }

        protected virtual void FsmTransitions()
        {
            WalkTransitions();
            WaitTransitions();
        }

        protected virtual void Move()
        {
        }

        protected virtual void WalkTransitions()
        {
            Fsm.SetTransition(Behaviours.Walk, Flags.OnRetreat, Behaviours.Walk);

            Fsm.SetTransition(Behaviours.Walk, Flags.OnWait, Behaviours.Wait);
        }

        protected virtual void WaitTransitions()
        {
            Fsm.SetTransition(Behaviours.Wait, Flags.OnRetreat, Behaviours.Walk);
        }
    }
}