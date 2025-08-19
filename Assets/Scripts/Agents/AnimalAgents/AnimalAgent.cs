using System;
using System.Collections.Generic;
using Agents.States.AnimalStates;
using Utils;

namespace Agents.AnimalAgents
{
    public enum Flags
    {
        OnEscape,
        OnEat,
        OnSearchFood,
        OnAttack
    }

    public class AnimalAgent<TVector, TTransform>
        where TVector : IVector, IEquatable<TVector>
        where TTransform : ITransform<IVector>, new()
    {
        public enum Behaviours
        {
            Walk,
            Escape,
            Eat,
            Attack
        }

        protected const int NoTarget = -1;
        public static Action<AnimalAgent<TVector, TTransform>> OnDeath;

        public static float Time = 0;

        protected NodeTerrain foodTarget;
        public FSM<Behaviours, Flags> Fsm;
        public float[][] input;
        private float maxX;
        private float maxY;
        private float minX;
        private float minY;
        protected Action OnEat;
        protected Action OnMove;
        public float[][] output;
        protected int speed = 3;
        protected float timer;
        protected TTransform transform = new();

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

                IVector oldPosition = transform.position;
                transform = value;
                transform.forward = (value.position - oldPosition).Normalized();
            }
        }

        public INode<IVector> CurrentNode;
        public Behaviours CurrentState { get; private set; }
        public virtual bool CanReproduce => Food >= FoodLimit;
        public int FoodLimit { get; protected set; } = 5;
        public int Food { get; protected set; }

        public virtual void Init()
        {
            OnMove += Move;
            OnEat += Eat;

            FsmBehaviours();
            Fsm.OnStateChange += OnStateChange;
            FsmTransitions();
            Fsm.ForceTransition(Behaviours.Walk);
        }

        private void OnStateChange(int state)
        {
            CurrentState = (Behaviours)Math.Clamp(state, 0, Enum.GetValues(typeof(Behaviours)).Length);
        }

        public virtual void Reset()
        {
            Fsm.OnStateChange -= OnStateChange;
            Food = 0;
            Fsm.ForceTransition(Behaviours.Walk);
            CalculateInputs();
        }

        protected void CalculateInputs()
        {

        }

        public virtual void Uninit()
        {
            OnMove -= Move;
            OnEat -= Eat;
        }

        public virtual void UpdateInputs()
        {
            FindFoodInputs();
            MovementInputs();
            ExtraInputs();
        }


        protected virtual void FindFoodInputs()
        {
        }

        protected virtual void MovementInputs()
        {
        }

        protected virtual void ExtraInputs()
        {
        }

        protected virtual void FsmTransitions()
        {
            WalkTransitions();
            EatTransitions();
            ExtraTransitions();
        }

        protected virtual void WalkTransitions()
        {
        }

        protected virtual void EatTransitions()
        {
        }

        protected virtual void ExtraTransitions()
        {
        }

        protected virtual void FsmBehaviours()
        {
            Fsm.AddBehaviour<AnimalWalkState>(Behaviours.Walk, WalkTickParameters);
            ExtraBehaviours();
        }

        protected virtual void ExtraBehaviours()
        {
        }

        protected virtual object[] WalkTickParameters()
        {
            object[] objects =
            {
                CurrentNode, foodTarget, OnMove, output[0], output[1]
            };
            return objects;
        }

        protected virtual object[] EatTickParameters()
        {
           

            object[] objects =
                { CurrentNode, foodTarget, OnEat, output[0], output[1] };
            return objects;
        }

        protected virtual void Eat()
        {
            INode<IVector> currNode = CurrentNode;
            lock (currNode)
            {
                if (currNode.Resource <= 0) return;
                Food++;
                currNode.Resource--;
            }
        }


        protected virtual void Move()
        {
            int movementBrainIndex = 1;

            timer += Time;

            if (speed * timer < 1f)
                return;

            IVector currentCoord = Transform.position;
            MyVector currentPos = new MyVector(currentCoord.X, currentCoord.Y);

            float[] brainOutput = output[movementBrainIndex];
            if (brainOutput.Length < 2)
                return;

            currentPos.X += speed * timer * brainOutput[0];
            currentPos.Y += speed * timer * brainOutput[1];

            if (currentPos.X < minX)
                currentPos.X = maxX - 1;
            else if (currentPos.X >= maxX)
                currentPos.X = minX + 1;

            if (currentPos.Y < minY)
                currentPos.Y = maxY - 1;
            else if (currentPos.Y >= maxY)
                currentPos.Y = minY + 1;

            INode<IVector> newPosNode = CurrentNode;
            if (newPosNode != null) SetPosition(currentPos);

            timer = 0;
        }

        public virtual void SetPosition(IVector position)
        {
            Transform = (TTransform)new ITransform<IVector>(position);
        }
    }
}