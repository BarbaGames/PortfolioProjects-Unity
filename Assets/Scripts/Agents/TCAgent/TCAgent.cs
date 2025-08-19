using System;
using System.Collections.Generic;
using Agents.States.SecurityStates;
using ECS.PathfinderECS;
using ECS.Patron;
using Utils;

namespace Agents.TCAgent
{
    public enum Flags
    {
        OnTargetReach,
        OnTargetLost,
        OnHunger,
        OnRetreat,
        OnFull,
        OnGather,
        OnBuild,
        OnWait,
        OnReturnResource
    }

    public enum Behaviours
    {
        Wait,
        Walk,
        GatherResources,
        ReturnResources,
        Build,
        Deliver
    }

    public enum ResourceType
    {
        None,
        Gold,
        Wood,
        Food
    }

    public class TcAgent<TVector, TTransform>
        where TVector : IVector, IEquatable<TVector>
        where TTransform : ITransform<IVector>, new()
    {
        public static float Time;
        public TVector AcsVector;
        protected INode<IVector> adjacentNode;
        public int CurrentFood = 3;
        public int CurrentGold = 0;
        public SimNode<IVector> CurrentNode;

        public Behaviours CurrentState;
        public Fsm<Behaviours, Flags> Fsm;
        protected Action OnMove;
        protected Action OnWait;
        public List<SimNode<IVector>> Path;
        protected int? PathNodeId;
        protected int ResourceLimit = 15;
        public bool Retreat;
        protected int speed = 6;

        private SimNode<IVector> targetNode;

        protected float timer;
        protected TTransform transform = new();

        public TcAgent(uint id)
        {
            Id = id;
        }

        public virtual TTransform Transform
        {
            get => transform;
            set
            {
                transform ??= new TTransform();
                transform.position ??= new MyVector(0, 0);

                if (value == null) throw new ArgumentNullException(nameof(value), "Transform value cannot be null");

                if (transform.position == null || value.position == null)
                    throw new InvalidOperationException("Transform positions cannot be null");

                transform.forward = (transform.position - value.position).Normalized();
                transform = value;
            }
        }

        public uint Id { get; }

        public SimNode<IVector> TargetNode
        {
            get => targetNode;
            protected set
            {
                targetNode = value;
                if (targetNode == null || targetNode.GetCoordinate() == null) return;
                PathRequestComponent<SimNode<IVector>> requestComponent =
                    EcsManager.GetComponent<PathRequestComponent<SimNode<IVector>>>(Id);

                requestComponent.StartNode = CurrentNode;
                requestComponent.DestinationNode = value;
                requestComponent.IsProcessed = false;

                PathNodeId = 0;
            }
        }

        public virtual void Init()
        {
            Fsm = new Fsm<Behaviours, Flags>();
            Fsm.OnStateChange += state =>
                CurrentState = (Behaviours)Math.Clamp(state, 0, Enum.GetValues(typeof(Behaviours)).Length);
            Time = 0;

            OnMove += Move;
            OnWait += Wait;

            FsmBehaviours();

            FsmTransitions();
        }

        protected virtual void FsmBehaviours()
        {
            Fsm.AddBehaviour<WaitState>(Behaviours.Wait, WaitTickParameters);
            Fsm.AddBehaviour<WalkState>(Behaviours.Walk, WalkTickParameters);
        }

        protected virtual void FsmTransitions()
        {
            WalkTransitions();
            WaitTransitions();
            GatherTransitions();
            GetResourcesTransitions();
            DeliverTransitions();
        }

        protected virtual void Move()
        {
            if (CurrentNode == null || TargetNode == null || Path == null) return;

            if (CurrentNode.GetCoordinate().Adyacent(TargetNode.GetCoordinate()) ||
                Approximately(CurrentNode.GetCoordinate(), TargetNode.GetCoordinate(), 0.001f)) return;

            if (Path.Count <= 0) return;
            //if (PathNodeId >= Path.Count) PathNodeId = 0;

            timer += Time;

            float relativeSpeed = speed * timer;
            if (relativeSpeed < 1) return;


            int distanceToMove = (int)relativeSpeed;

            PathNodeId += distanceToMove;
            PathNodeId = Math.Clamp((int)PathNodeId, 0, Path.Count - 1);

            CurrentNode = Path[(int)PathNodeId];
            Transform.position = CurrentNode.GetCoordinate();
            Transform.position += AcsVector;
            timer = (float)(relativeSpeed - Math.Truncate(relativeSpeed)) / speed;
        }

        protected SimNode<IVector> GetRetreatNode()
        {
            return null;
        }

        protected virtual void Wait()
        {
        }

        private bool Approximately(IVector coord1, IVector coord2, float tolerance)
        {
            return Math.Abs(coord1.X - coord2.X) <= tolerance && Math.Abs(coord1.Y - coord2.Y) <= tolerance;
        }

        #region Transitions

        protected virtual void GatherTransitions()
        {
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetRetreatNode();
                });
        }


        protected virtual object[] GatherTickParameters()
        {
            object[] objects = { Retreat, CurrentFood, CurrentGold, ResourceLimit };
            return objects;
        }

        protected virtual void WalkTransitions()
        {
            Fsm.SetTransition(Behaviours.Walk, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetRetreatNode();
                });

            Fsm.SetTransition(Behaviours.Walk, Flags.OnWait, Behaviours.Wait);
        }

        protected virtual void WaitTransitions()
        {
            Fsm.SetTransition(Behaviours.Wait, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    if (adjacentNode != null) adjacentNode.IsOccupied = false;
                    TargetNode = GetRetreatNode();
                });
        }

        protected virtual void DeliverTransitions()
        {
        }

        protected virtual void GetResourcesTransitions()
        {
        }

        #endregion

        #region Params

        protected virtual object[] WalkTickParameters()
        {
            object[] objects = { CurrentNode, TargetNode, Retreat, OnMove, Path };
            return objects;
        }

        protected virtual object[] WaitTickParameters()
        {
            object[] objects = { Retreat, CurrentFood, CurrentGold, CurrentNode, OnWait };
            return objects;
        }

        #endregion
    }
}