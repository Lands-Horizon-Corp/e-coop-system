namespace ECoopSystem.ViewModels;

public class LoadingViewModel : ViewModelBase
{
    public string _message;

    public string Message
    {
        get => _message;
        set
        {
            _message = value;
            OnPropertyChanged();
        }
    }

    public LoadingViewModel(string message = "Verifying License...")
    {
        _message = message;
    }
}
