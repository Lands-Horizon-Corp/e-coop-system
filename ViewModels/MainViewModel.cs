using ECoopSystem.Services;
using ECoopSystem.Stores;

namespace ECoopSystem.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ShellViewModel _shell;
    private readonly AppStateStore _store;
    private readonly AppState _state;
    private readonly SecretKeyStore _secretStore;
    private readonly LicenseService _licenseService;

    public string URL { get; } = "https://e-coop-client-development.up.railway.app/";

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
