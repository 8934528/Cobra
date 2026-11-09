namespace Cobra.Engine.ECS.Components
{
    public class Transform : IComponent
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Rotation { get; set; }
        public double ScaleX { get; set; } = 1;
        public double ScaleY { get; set; } = 1;
    }
}
