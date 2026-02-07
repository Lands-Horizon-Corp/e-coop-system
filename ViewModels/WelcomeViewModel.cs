using ECoopSystem.Stores;

namespace ECoopSystem.ViewModels;

public class WelcomeViewModel
{
    private readonly ShellViewModel _shell;
    private readonly AppStateStore _store;
    private readonly AppState _state;

    public WelcomeViewModel(ShellViewModel shell, AppStateStore store, AppState state)
    {
        _shell = shell;
        _store = store;
        _state = state;
    }

    public void Continue()
    {
        _state.WelcomeShown = true;
        _store.Save(_state);

        _shell.Navigate(
            new MainViewModel(_shell, _store, _state), 
            WindowMode.Normal
        );
    }
}
