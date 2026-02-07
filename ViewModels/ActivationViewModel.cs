namespace ECoopSystem.ViewModels;

public class ActivationViewModel
{
    private readonly ShellViewModel _shell;

    public ActivationViewModel(ShellViewModel shell) => _shell = shell;
    public void GoNext() => _shell.Navigate(new WelcomeViewModel(_shell), WindowMode.Locked);
}
