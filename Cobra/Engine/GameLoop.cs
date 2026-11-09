using System.Diagnostics;
using System.Windows.Threading;

namespace Cobra.Engine
{
    public class GameLoop
    {
        private readonly Stopwatch _stopwatch = new();
        private DispatcherTimer? _timer;

        public Action<float>? OnUpdate;

        public void Start()
        {
            _stopwatch.Start();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _timer.Tick += Loop;
            _timer.Start();
        }

        private void Loop(object? sender, EventArgs e)
        {
            float deltaTime = (float)_stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            OnUpdate?.Invoke(deltaTime);
        }

        public void Stop()
        {
            _timer?.Stop();
        }
    }
}
