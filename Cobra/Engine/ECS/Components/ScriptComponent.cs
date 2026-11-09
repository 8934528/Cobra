using Cobra.Engine.VisualScripting;

namespace Cobra.Engine.ECS.Components
{
    public class ScriptComponent : Component
    {
        public string ScriptPath { get; set; } = string.Empty;
        public NodeGraph? Graph { get; set; }

        public override void Start()
        {
            if (GameObject != null && string.IsNullOrEmpty(Title))
            {
                Title = GameObject.Name;
            }
        }

        public override void Update()
        {
            if (Graph != null && !string.IsNullOrEmpty(ScriptPath))
            {
                _ = Graph.Nodes.Count;
            }
        }
    }
}
