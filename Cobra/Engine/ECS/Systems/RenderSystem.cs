using System.Windows.Controls;
using System.Windows.Media;
using Cobra.Engine.ECS.Components;
using WpfRectangle = System.Windows.Shapes.Rectangle;
using WpfBrushes = System.Windows.Media.Brushes;
using EcsTransform = Cobra.Engine.ECS.Components.Transform;

namespace Cobra.Engine.ECS.Systems
{
    public static class RenderSystem
    {
        public static void Render(World world, Canvas canvas)
        {
            canvas.Children.Clear();

            foreach (var (_, transform, sprite) in world.Query<EcsTransform, Sprite>())
            {
                var rect = new WpfRectangle
                {
                    Width = sprite.Width,
                    Height = sprite.Height,
                    Fill = WpfBrushes.White,
                    Stroke = WpfBrushes.DarkOrange,
                    StrokeThickness = 1,
                    RenderTransform = new RotateTransform(
                        transform.Rotation,
                        sprite.Width / 2,
                        sprite.Height / 2
                    )
                };

                Canvas.SetLeft(rect, transform.X);
                Canvas.SetTop(rect, transform.Y);

                canvas.Children.Add(rect);
            }
        }
    }
}
