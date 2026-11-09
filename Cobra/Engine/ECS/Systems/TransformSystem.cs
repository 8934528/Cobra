using Cobra.Engine.ECS.Components;

namespace Cobra.Engine.ECS.Systems
{
    public static class TransformSystem
    {
        public static void Update(World world)
        {
            foreach (var (_, transform, _) in world.Query<Transform, Sprite>())
            {
                transform.Rotation %= 360.0;
            }
        }
    }
}
