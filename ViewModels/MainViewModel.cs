using ECoopSystem.Services;
using ECoopSystem.Stores;

namespace ECoopSystem.ViewModels;

public class MainViewModel : ViewModelBase
{
    public readonly ShellViewModel _shell;
    public readonly AppStateStore _store;
    public readonly AppState _state;
    public readonly SecretKeyStore _secretStore;
    public readonly LicenseService _licenseService;

    public MainViewModel(ShellViewModel shell, AppStateStore store, AppState state, SecretKeyStore secretStore, LicenseService licenseService)
    {
        _shell = shell;
        _store = store;
        _state = state;
        _secretStore = secretStore;
        _licenseService = licenseService;
    }

    public void Logout()
    {
        _shell.Navigate(
            new ActivationViewModel(_shell, _store, _state, _secretStore, _licenseService),
            WindowMode.Locked
        );
    }
}
