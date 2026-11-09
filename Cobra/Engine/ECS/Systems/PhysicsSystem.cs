using Cobra.Engine.Physics;

namespace Cobra.Engine.ECS.Systems
{
    public class PhysicsSystem(PhysicsEngine physics)
    {
        public void Update(float deltaTime)
        {
            physics.Update(deltaTime);
        }
    }
}
