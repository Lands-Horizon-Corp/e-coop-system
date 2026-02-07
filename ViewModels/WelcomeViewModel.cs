using ECoopSystem.Services;
using ECoopSystem.Stores;

namespace ECoopSystem.ViewModels;

public class WelcomeViewModel : ViewModelBase
{
    private readonly ShellViewModel _shell;
    private readonly AppStateStore _store;
    private readonly AppState _state;
    private readonly SecretKeyStore _secretStore;
    private readonly LicenseService _licenseService;

    public WelcomeViewModel(ShellViewModel shell, AppStateStore store, AppState state, SecretKeyStore secretStore, LicenseService licenseService)
    {
        _shell = shell;
        _store = store;
        _state = state;
        _secretStore = secretStore;
        _licenseService = licenseService;
    }

    public void Continue()
    {
        _state.WelcomeShown = true;
        _store.Save(_state);

        _shell.Navigate(
            new MainViewModel(_shell, _store, _state, _secretStore, _licenseService), 
            WindowMode.Normal
        );
    }
}
