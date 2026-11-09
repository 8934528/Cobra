namespace Cobra.Engine.Physics
{
    public static class Collision
    {
        public static bool Check(Collider a, Collider b)
        {
            return a.Bounds.IntersectsWith(b.Bounds);
        }
    }
}
