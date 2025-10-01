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

    public class RelayCommand : ICommand  // не-generic
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

        // Конструкторы для команд без параметра
        public RelayCommand(Action execute, Func<bool> canExecute = null)
            : this(_ => execute(), _ => canExecute?.Invoke() ?? true)
        {
        }


        // Конструктор для асинхронных команд
        public RelayCommand(Func<object, Task> executeAsync, Func<object, bool> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        // Конструктор для асинхронных команд  без параметра
        public RelayCommand(Func<Task> executeAsync, Func<bool> canExecute = null)
        : this(_ => executeAsync(), _ => canExecute?.Invoke() ?? true)
        {
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

        // это метод, который вручную запускает проверку возможности выполнения команды.
        // Это механизм уведомления WPF о том, что условия выполнения команды изменились.
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    // Универсальная версия RelayCommand
    public class RelayCommand<T> : ICommand     // generic
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;
        private readonly Func<T, Task> _executeAsync;

        // Конструктор для синхронных команд
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Конструктор для асинхронных команд
        public RelayCommand(Func<T, Task> executeAsync, Func<T, bool> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (parameter is T typedParameter)
            {
                return _canExecute == null || _canExecute(typedParameter);
            }
            return _canExecute == null || _canExecute(default(T));
        }

        public void Execute(object parameter)
        {
            if (parameter is T typedParameter)
            {
                if (_execute != null)
                {
                    _execute(typedParameter);
                }
                else if (_executeAsync != null)
                {
                    _ = ExecuteAsync(typedParameter);
                }
            }
        }

        private async Task ExecuteAsync(T parameter)
        {
            await _executeAsync(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

}
