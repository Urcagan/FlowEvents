using System;
using System.Windows;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для EventView.xaml
    /// </summary>
    public partial class EventWindow : Window
    {
        public EventWindow()
        {
            InitializeComponent();

            //if (eventViewModel._originalEvent != null)
            //{
            //    Loaded += async (s, e) => await eventViewModel.InitializeAsync(); //Когда это событие происходит, вызывается асинхронный метод InitializeAsync() у переданного в конструктор объекта eventViewModel.
            //}
        }

        private void EventWindow_Loaded(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
