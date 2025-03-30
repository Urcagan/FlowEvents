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
    /// Логика взаимодействия для UnitsView.xaml
    /// </summary>
    public partial class UnitsView : Window
    {
        public UnitsView(UnitViewModel unitViewModel)
        {
            InitializeComponent();

            // настраивает окно для использованя UnitViewModel как DataContext.
            //DataContext = new UnitViewModel(); // Установите DataContext
            DataContext = unitViewModel;
        }

    }
}
