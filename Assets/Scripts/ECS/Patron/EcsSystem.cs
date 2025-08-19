using System.Threading.Tasks;

namespace ECS.Patron
{
    public abstract class EcsSystem
    {
        public void Run(float deltaTime)
        {
            PreExecute(deltaTime);
            Execute(deltaTime);
            PostExecute(deltaTime);
        }

        public abstract void Initialize();

        public virtual void Deinitialize()
        {
        }

        protected ParallelOptions GetParallelOptions()
        {
            return EcsManager.GetNestedParallelOptions();
        }

        protected abstract void PreExecute(float deltaTime);

        protected abstract void Execute(float deltaTime);

        protected abstract void PostExecute(float deltaTime);
    }
}