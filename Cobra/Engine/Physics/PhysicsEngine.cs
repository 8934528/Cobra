namespace Cobra.Engine.Physics
{
    public class PhysicsEngine
    {
        public List<Rigidbody> Bodies { get; set; } = [];
        public List<Collider> Colliders { get; set; } = [];

        public void Update(float deltaTime)
        {
            // Update motion
            foreach (var body in Bodies)
                body.Update(deltaTime);

            // Check collisions
            for (int i = 0; i < Colliders.Count; i++)
            {
                for (int j = i + 1; j < Colliders.Count; j++)
                {
                    if (Collision.Check(Colliders[i], Colliders[j]))
                    {
                        // Simple response (stop)
                        Bodies[i].Velocity = new System.Windows.Vector(0, 0);
                        Bodies[j].Velocity = new System.Windows.Vector(0, 0);
                    }
                }
            }
        }
    }
}
