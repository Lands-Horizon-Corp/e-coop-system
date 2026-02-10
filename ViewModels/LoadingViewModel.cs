namespace ECoopSystem.ViewModels;

public class LoadingViewModel : ViewModelBase
{
    public string Message { get; }

    public LoadingViewModel(string message = "Verifying License...")
    {
        Message = message;
    }
}
