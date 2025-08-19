namespace ECS.Patron
{
    public abstract class EcsComponent
    {
        public uint EntityOwnerID { get; set; } = 0;

        public virtual void Dispose()
        {
        }
    }
}