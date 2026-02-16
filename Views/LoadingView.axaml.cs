using Avalonia.Controls;
using Avalonia.Threading;

namespace ECoopSystem.Views;

public partial class LoadingView : UserControl
{
    public LoadingView()
    {
        InitializeComponent();

        AttachedToVisualTree += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                Opacity = 1;
            }, DispatcherPriority.Render);
        };
    }
}

