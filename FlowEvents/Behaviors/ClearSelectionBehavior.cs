using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace FlowEvents.Behaviors
{
    public class ClearSelectionBehavior : Behavior<ListView>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseDown += OnPreviewMouseDown;    // Подписываемся на события ListView
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseDown -= OnPreviewMouseDown;    // Отписываемся от событий при удалении
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var listView = AssociatedObject;
            var hit = VisualTreeHelper.HitTest(listView, e.GetPosition(listView));

            if (hit == null || !IsClickOnItem(hit.VisualHit))
            {
                listView.SelectedItem = null;
            }
        }

        private bool IsClickOnItem(DependencyObject source)
        {
            while (source != null && source != AssociatedObject)
            {
                if (source is ListViewItem)
                {
                    return true;
                }
                source = VisualTreeHelper.GetParent(source);
            }
            return false;
        }
    }
}
