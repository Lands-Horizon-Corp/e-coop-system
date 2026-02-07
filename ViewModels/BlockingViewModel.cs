namespace ECoopSystem.ViewModels;

public class BlockingViewModel : ViewModelBase
{
    public string Title { get; set; }
    public string Message { get; set; }

    public BlockingViewModel(string title, string message)
    {
        Title = title;
        Message = message;
    }
}
