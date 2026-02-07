namespace ECoopSystem.ViewModels;

public class WelcomeViewModel
{
    private readonly ShellViewModel _shell;

    public WelcomeViewModel(ShellViewModel shell) => _shell = shell;

    public void Continue() => _shell.Navigate(new MainViewModel(_shell), WindowMode.Normal);
}
