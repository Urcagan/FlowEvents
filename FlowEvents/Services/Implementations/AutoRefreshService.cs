using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FlowEvents.Services.Implementations
{
    public class AutoRefreshService : IAutoRefreshService
    {
        private readonly DispatcherTimer _timer;  // таймер 
        private bool _isEnabled;    // Автообновление включено
        private int _refreshInterval;   // Интервал автообновления

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                if (value) Start();
                else Stop();
            }
        }

        public int RefreshInterval
        {
            get => _refreshInterval;
            set
            {
                _refreshInterval = value;
                _timer.Interval = TimeSpan.FromSeconds(value);
            }
        }

        public event Action RefreshRequested;

        public AutoRefreshService()
        {
            _timer = new DispatcherTimer();
            _timer.Tick += OnTimerTick;
        }

        public void Start()
        {
            if (!_timer.IsEnabled && IsEnabled)
            {
                _timer.Start();
            }
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            RefreshRequested?.Invoke();
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
        }
    }
}
