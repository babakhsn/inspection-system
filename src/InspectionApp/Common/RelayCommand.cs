﻿using System;
using System.Windows.Input;

namespace InspectionApp.Common
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        //implement of ICommand
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        //implement of ICommand
        public void Execute(object? parameter) => _execute();
        //implement of ICommand
        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
