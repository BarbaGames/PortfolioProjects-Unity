using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECS.FlockingECS;

namespace ECS.Patron
{
    public static class EcsManager
    {
        private static ConcurrentDictionary<Type, EcsSystem> systems;
        private static ConcurrentDictionary<Type, EcsSystem> FlockingSystems;
        private static ConcurrentDictionary<Type, ConcurrentDictionary<uint, EcsComponent>> components;
        private static ConcurrentDictionary<uint, EcsEntity> entities;
        private static ConcurrentDictionary<Type, ConcurrentDictionary<uint, EcsFlag>> flags;

        private static readonly ParallelOptions topLevelOptions;
        private static readonly ParallelOptions nestedOptions;

        private static readonly ThreadLocal<Dictionary<Type, (uint[] ids, Array components)>> componentBuffers =
            new(() => new Dictionary<Type, (uint[], Array)>());

        static EcsManager()
        {
            int processorCount = (int)(Environment.ProcessorCount * 0.9);

            // Leave one core free for system operations
            topLevelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, processorCount - 1)
            };

            // Further limit nested parallelism to avoid oversubscription
            nestedOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, processorCount / 2)
            };
        }

        public static void Init()
        {
            entities = new ConcurrentDictionary<uint, EcsEntity>();
            components = new ConcurrentDictionary<Type, ConcurrentDictionary<uint, EcsComponent>>();
            flags = new ConcurrentDictionary<Type, ConcurrentDictionary<uint, EcsFlag>>();
            systems = new ConcurrentDictionary<Type, EcsSystem>();
            FlockingSystems = new ConcurrentDictionary<Type, EcsSystem>();

            //foreach (var classType in typeof(ECSSystem).Assembly.GetTypes())
            //    if (typeof(ECSSystem).IsAssignableFrom(classType) && !classType.IsAbstract)
            //        systems.TryAdd(classType, Activator.CreateInstance(classType) as ECSSystem);

            FlockingSystems.TryAdd(typeof(BoidRadarSystem),
                Activator.CreateInstance(typeof(BoidRadarSystem)) as EcsSystem);
            FlockingSystems.TryAdd(typeof(AlignmentSystem),
                Activator.CreateInstance(typeof(AlignmentSystem)) as EcsSystem);
            FlockingSystems.TryAdd(typeof(CohesionSystem),
                Activator.CreateInstance(typeof(CohesionSystem)) as EcsSystem);
            FlockingSystems.TryAdd(typeof(SeparationSystem),
                Activator.CreateInstance(typeof(SeparationSystem)) as EcsSystem);
            FlockingSystems.TryAdd(typeof(DirectionSystem),
                Activator.CreateInstance(typeof(DirectionSystem)) as EcsSystem);
            systems.TryAdd(typeof(AcsSystem), Activator.CreateInstance(typeof(AcsSystem)) as EcsSystem);
            //systems.TryAdd(typeof(AStarPathfinderSystem<SimNode<IVector>, IVector, CoordinateNode>), Activator.CreateInstance(typeof(AStarPathfinderSystem<SimNode<IVector>, IVector, CoordinateNode>)) as ECSSystem);

            foreach (KeyValuePair<Type, EcsSystem> system in systems) system.Value.Initialize();


            foreach (Type classType in typeof(EcsComponent).Assembly.GetTypes())
                if (typeof(EcsComponent).IsAssignableFrom(classType) && !classType.IsAbstract)
                    components.TryAdd(classType, new ConcurrentDictionary<uint, EcsComponent>());

            //components.TryAdd(typeof(GraphComponent<SimNode<IVector>>), new ConcurrentDictionary<uint, EcsComponent>());
            //components.TryAdd(typeof(PathRequestComponent<SimNode<IVector>>), new ConcurrentDictionary<uint, EcsComponent>());
            //components.TryAdd(typeof(PathResultComponent<SimNode<IVector>>), new ConcurrentDictionary<uint, EcsComponent>());

            foreach (Type classType in typeof(EcsFlag).Assembly.GetTypes())
                if (typeof(EcsFlag).IsAssignableFrom(classType) && !classType.IsAbstract)
                    flags.TryAdd(classType, new ConcurrentDictionary<uint, EcsFlag>());
        }

        public static void Tick(float deltaTime)
        {
            Parallel.ForEach(systems, topLevelOptions, system => { system.Value.Run(deltaTime); });
        }

        public static void RunFlocking(float deltaTime)
        {
            FlockingSystems[typeof(BoidRadarSystem)].Run(deltaTime);
            FlockingSystems[typeof(AlignmentSystem)].Run(deltaTime);
            FlockingSystems[typeof(CohesionSystem)].Run(deltaTime);
            FlockingSystems[typeof(SeparationSystem)].Run(deltaTime);
            FlockingSystems[typeof(DirectionSystem)].Run(deltaTime);
        }

        public static void RunSystem(float deltaTime, Type systemType)
        {
            systems[systemType].Run(deltaTime);
        }

        public static ParallelOptions GetNestedParallelOptions()
        {
            return nestedOptions;
        }

        public static uint CreateEntity()
        {
            entities ??= new ConcurrentDictionary<uint, EcsEntity>();
            EcsEntity ecsEntity = new EcsEntity();
            entities.TryAdd(ecsEntity.GetID(), ecsEntity);
            return ecsEntity.GetID();
        }

        public static void AddSystem(EcsSystem system)
        {
            systems ??= new ConcurrentDictionary<Type, EcsSystem>();

            systems.TryAdd(system.GetType(), system);
        }

        public static void InitSystems()
        {
            foreach (KeyValuePair<Type, EcsSystem> system in systems) system.Value.Initialize();
        }

        public static void AddComponentList(Type component)
        {
            components ??= new ConcurrentDictionary<Type, ConcurrentDictionary<uint, EcsComponent>>();
            components.TryAdd(component, new ConcurrentDictionary<uint, EcsComponent>());
        }

        public static void AddComponent<TComponentType>(uint entityID, TComponentType component)
            where TComponentType : EcsComponent
        {
            component.EntityOwnerID = entityID;
            entities[entityID].AddComponentType(typeof(TComponentType));
            components[typeof(TComponentType)].TryAdd(entityID, component);
        }

        public static bool ContainsComponent<TComponentType>(uint entityID) where TComponentType : EcsComponent
        {
            return entities[entityID].ContainsComponentType<TComponentType>();
        }


        public static IEnumerable<uint> GetEntitiesWithComponentTypes(params Type[] componentTypes)
        {
            ConcurrentBag<uint> matchs = new ConcurrentBag<uint>();
            Parallel.ForEach(entities, nestedOptions, entity =>
            {
                for (int i = 0; i < componentTypes.Length; i++)
                    if (!entity.Value.ContainsComponentType(componentTypes[i]))
                        return;

                matchs.Add(entity.Key);
            });
            return matchs;
        }

        public static ConcurrentDictionary<uint, TComponentType> GetComponents<TComponentType>()
            where TComponentType : EcsComponent
        {
            if (!components.ContainsKey(typeof(TComponentType)))
                return null;

            ConcurrentDictionary<uint, TComponentType> comps = new ConcurrentDictionary<uint, TComponentType>();

            // Use GetComponentsDirect to get arrays of IDs and components
            (uint[] ids, TComponentType[] componentArray) = GetComponentsDirect<TComponentType>();

            // Fill the dictionary from the arrays
            for (int i = 0; i < ids.Length; i++) comps.TryAdd(ids[i], componentArray[i]);

            return comps;
        }

        public static TComponentType GetComponent<TComponentType>(uint entityID) where TComponentType : EcsComponent
        {
            return components[typeof(TComponentType)][entityID] as TComponentType;
        }

        public static (uint[] ids, TComponentType[] components) GetComponentsDirect<TComponentType>()
            where TComponentType : EcsComponent
        {
            if (!components.TryGetValue(typeof(TComponentType), out ConcurrentDictionary<uint, EcsComponent> componentDict))
                return (Array.Empty<uint>(), Array.Empty<TComponentType>());

            Dictionary<Type, (uint[] ids, Array components)> buffers = componentBuffers.Value;
            Type type = typeof(TComponentType);

            if (!buffers.TryGetValue(type, out (uint[] ids, Array components) buffer))
            {
                buffer = (new uint[1024], new TComponentType[1024]);
                buffers[type] = buffer;
            }

            int count = componentDict.Count;

            if (buffer.ids.Length < count)
            {
                Array.Resize(ref buffer.ids, count);
                TComponentType[] tempComponents = (TComponentType[])buffer.components;
                Array.Resize(ref tempComponents, count);
                buffer.components = tempComponents;
            }

            int i = 0;
            TComponentType[] componentsArray = (TComponentType[])buffer.components;
            foreach (KeyValuePair<uint, EcsComponent> kvp in componentDict)
            {
                buffer.ids[i] = kvp.Key;
                componentsArray[i] = (TComponentType)kvp.Value;
                i++;
            }

            return (
                buffer.ids.Take(count).ToArray(),
                componentsArray.Take(count).ToArray()
            );
        }

        public static void RemoveComponent<TComponentType>(uint entityID) where TComponentType : EcsComponent
        {
            components[typeof(TComponentType)].TryRemove(entityID, out _);
        }

        public static IEnumerable<uint> GetEntitiesWhitFlagTypes(params Type[] flagTypes)
        {
            ConcurrentBag<uint> matchs = new ConcurrentBag<uint>();
            Parallel.ForEach(entities, nestedOptions, entity =>
            {
                for (int i = 0; i < flagTypes.Length; i++)
                    if (!entity.Value.ContainsFlagType(flagTypes[i]))
                        return;

                matchs.Add(entity.Key);
            });
            return matchs;
        }

        public static void AddFlag<TFlagType>(uint entityID, TFlagType flag)
            where TFlagType : EcsFlag
        {
            flag.EntityOwnerID = entityID;
            entities[entityID].AddComponentType(typeof(TFlagType));
            flags[typeof(TFlagType)].TryAdd(entityID, flag);
        }

        public static bool ContainsFlag<TFlagType>(uint entityID) where TFlagType : EcsFlag
        {
            return entities[entityID].ContainsFlagType<TFlagType>();
        }

        public static ConcurrentDictionary<uint, TFlagType> GetFlags<TFlagType>() where TFlagType : EcsFlag
        {
            if (!flags.ContainsKey(typeof(TFlagType))) return null;

            ConcurrentDictionary<uint, TFlagType> flgs = new ConcurrentDictionary<uint, TFlagType>();

            Parallel.ForEach(flags[typeof(TFlagType)], nestedOptions,
                flag => { flgs.TryAdd(flag.Key, flag.Value as TFlagType); });

            return flgs;
        }

        public static TFlagType GetFlag<TFlagType>(uint entityID) where TFlagType : EcsFlag
        {
            return flags[typeof(TFlagType)][entityID] as TFlagType;
        }

        public static void RemoveFlag<TFlagType>(uint entityID) where TFlagType : EcsFlag
        {
            flags[typeof(TFlagType)].TryRemove(entityID, out _);
        }

        public static void RemoveEntity(uint agentId)
        {
            entities.TryRemove(agentId, out _);
            foreach (KeyValuePair<Type, ConcurrentDictionary<uint, EcsComponent>> component in components)
                component.Value.TryRemove(agentId, out _);
            foreach (KeyValuePair<Type, ConcurrentDictionary<uint, EcsFlag>> flag in flags)
                flag.Value.TryRemove(agentId, out _);
        }

        public static EcsSystem GetSystem<T>()
        {
            return systems[typeof(T)];
        }
    }
}