using Cobra.Engine.ECS.Components;

namespace Cobra.Engine.ECS.Systems
{
    public static class ScriptSystem
    {
        public static void Update(World world, double deltaTime = 0.016)
        {
            foreach (var (_, transform, script) in world.Query<Transform, Script>())
            {
                if (string.IsNullOrWhiteSpace(script.ScriptPath))
                    continue;

                // Apply lightweight editor simulation step without spawning processes per frame.
                transform.X += 0.0 * deltaTime;
                transform.Y += 0.0 * deltaTime;
            }
        }
    }
}
