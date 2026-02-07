namespace ECoopSystem.ViewModels;

public class MainViewModel
{
    public readonly ShellViewModel _shell;

    public MainViewModel(ShellViewModel shell) => _shell = shell;

    public void Logout() => _shell.Navigate(new ActivationViewModel(_shell), WindowMode.Locked);
}
