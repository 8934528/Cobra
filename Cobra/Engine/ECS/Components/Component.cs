namespace Cobra.Engine.ECS.Components
{
    public abstract class Component
    {
        public GameObject? GameObject { get; set; }
        public string Title { get; set; } = string.Empty;

        public virtual void Start() { }
        public virtual void Update() { }
    }
}
