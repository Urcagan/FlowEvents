using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для EventView.xaml
    /// </summary>
    public partial class EventWindow : Window
    {
        public EventWindow(EventViewModel eventViewModel)
        {
            InitializeComponent();
            DataContext = eventViewModel;

            if (eventViewModel._originalEvent != null)
            {
                Loaded += async (s, e) => await eventViewModel.InitializeAsync(); //Когда это событие происходит, вызывается асинхронный метод InitializeAsync() у переданного в конструктор объекта eventViewModel.
            }
        }

        private void EventWindow_Loaded(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
