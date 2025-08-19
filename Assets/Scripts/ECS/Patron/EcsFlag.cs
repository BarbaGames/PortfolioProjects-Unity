using System;

namespace ECS.Patron
{
    [Flags]
    public enum FlagType
    {
        None = 0,
        Cart = 1 << 0,
        Gatherer = 1 << 1,
        Builder = 1 << 2,
        Carnivore = 1 << 3,
        Herbivore = 1 << 4
    }

    public class EcsFlag
    {
        public EcsFlag(FlagType flagType)
        {
            Flag = flagType;
        }

        public uint EntityOwnerID { get; set; } = 0;

        public FlagType Flag { get; set; }

        public virtual void Dispose()
        {
        }
    }
}