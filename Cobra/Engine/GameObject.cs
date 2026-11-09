using Cobra.Engine.ECS.Components;
using Cobra.Engine.Physics;

namespace Cobra.Engine
{
    public class GameObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "GameObject";

        public Rigidbody? Rigidbody { get; set; }
        public Collider? Collider { get; set; }

        private readonly List<Component> _components = [];

        public T AddComponent<T>() where T : Component, new()
        {
            var comp = new T
            {
                GameObject = this
            };
            _components.Add(comp);
            return comp;
        }

        public T? GetComponent<T>() where T : Component
        {
            return _components.OfType<T>().FirstOrDefault();
        }

        public List<Component> GetAllComponents() => _components;
    }
}
