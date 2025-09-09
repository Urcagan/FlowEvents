using System;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace FlowEvents
{
    // реализации класса RelayCommand
    // Он позволяет связывать действия пользователя (например, нажатие кнопки)
    // с методами во ViewModel, сохраняя разделение логики и интерфейса.

    // Зачем нужен RelayCommand?
    // Упрощение кода: Избавляет от необходимости каждый раз вручную реализовывать ICommand.
    // Привязка команд в XAML: Позволяет использовать команды в разметке через Binding.
    // Поддержка MVVM: Помогает отделить UI от бизнес-логики.

    // Зачем нужен RelayCommand?
    // Упрощение кода: Избавляет от необходимости каждый раз вручную реализовывать ICommand.
    // Привязка команд в XAML: Позволяет использовать команды в разметке через Binding.
    // Поддержка MVVM: Помогает отделить UI от бизнес-логики.

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;
        private readonly Func<object, Task> _executeAsync;

        // Конструктор для синхронных команд
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }


        // Конструктор для асинхронных команд
        public RelayCommand(Func<object, Task> executeAsync, Func<object, bool> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        //2 может ли нажатся кнопка true или false
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        //3 метод, который будет выполнятся при нажатии кнопки
        //public void Execute(object parameter)
        //{
        //    _execute(parameter);
        //}

     
        public void Execute(object parameter)
        {
            if (_execute != null)
            {
                _execute(parameter);
            }
            else if (_executeAsync != null)
            {
                // Запускаем асинхронную задачу и "забываем" о ней
                // Это нормально для команд, так как мы не можем ждать завершения
                _ = ExecuteAsync(parameter);
            }
        }


        private async Task ExecuteAsync(object parameter)
        {
            await _executeAsync(parameter);
        }


        //1 событие изменения "2" (может ли нажатся кнопка), его подписчик кнопка
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

    }
}
