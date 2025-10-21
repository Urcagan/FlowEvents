using FlowEvents.Services.Interface;
using System;
using System.Diagnostics;
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
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    if (value)
                        Start();
                    else
                        Stop();

                    OnSettingsChanged?.Invoke(); // вызываем событие OnSettingsChanged
                    Debug.WriteLine($"Статус автоообновления {value} ");
                }
            }
        }

        public int RefreshInterval
        {
            get => _refreshInterval;
            set
            {
                if (_refreshInterval != value)
                {
                    _refreshInterval = value;
                    _timer.Interval = TimeSpan.FromSeconds(value);
                    OnSettingsChanged?.Invoke(); // вызываем событие OnSettingsChanged
                    Debug.WriteLine($"Устоновлен интервал времени автоообновления {value} ");
                }
            }
        }

        public event Action RefreshRequested;
        public event Action OnSettingsChanged;  // Событие уведомления об изменении настроек

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
