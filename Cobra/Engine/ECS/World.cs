namespace Cobra.Engine.ECS
{
    public class World
    {
        private int _nextId;

        private readonly Dictionary<int, Dictionary<Type, IComponent>> _entities = [];

        public Entity CreateEntity()
        {
            var entity = new Entity(_nextId++);
            _entities[entity.Id] = [];
            return entity;
        }

        public void AddComponent<T>(Entity entity, T component) where T : IComponent
        {
            if (_entities.TryGetValue(entity.Id, out var components))
            {
                components[typeof(T)] = component;
            }
        }

        public T? GetComponent<T>(Entity entity) where T : class, IComponent
        {
            if (_entities.TryGetValue(entity.Id, out var compDict))
            {
                if (compDict.TryGetValue(typeof(T), out var comp))
                    return comp as T;
            }

            return null;
        }

        public IEnumerable<(Entity, T1, T2)> Query<T1, T2>()
            where T1 : class, IComponent
            where T2 : class, IComponent
        {
            foreach (var e in _entities)
            {
                if (e.Value.ContainsKey(typeof(T1)) &&
                    e.Value.ContainsKey(typeof(T2)))
                {
                    yield return (
                        new Entity(e.Key),
                        (T1)e.Value[typeof(T1)],
                        (T2)e.Value[typeof(T2)]
                    );
                }
            }
        }
    }
}
