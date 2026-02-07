using ECoopSystem.Stores;

namespace ECoopSystem.ViewModels;

public class ActivationViewModel
{
    private readonly ShellViewModel _shell;
    private readonly AppStateStore _store;
    private readonly AppState _state;

    public ActivationViewModel(ShellViewModel shell, AppStateStore store, AppState state)
    {
        _shell = shell;
        _store = store;
        _state = state;
    }
    public void GoNext()
    {
        if (_state.WelcomeShown)
        {
            _shell.Navigate(
                new MainViewModel(_shell, _store, _state),
                WindowMode.Normal
            );
        }
        else
        {
            _shell.Navigate(
                new WelcomeViewModel(_shell, _store, _state),
                WindowMode.Locked
            );
        }
    }
}
