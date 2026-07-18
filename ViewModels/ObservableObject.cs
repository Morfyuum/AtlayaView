using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AtlayaView.ViewModels;

// ── ObservableObject ──────────────────────────────────────────────────────────
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

// ── RelayCommand ──────────────────────────────────────────────────────────────
public sealed class RelayCommand(Action<object?> execute,
                                 Func<object?, bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add    => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? p)  => canExecute?.Invoke(p) ?? true;
    public void Execute(object? p)     => execute(p);
    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}
