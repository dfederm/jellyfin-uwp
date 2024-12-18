using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;

namespace Jellyfin.Views;

internal sealed partial class Home : Page
{
    public Home()
    {
        InitializeComponent();

        ViewModel = AppServices.Instance.ServiceProvider.GetRequiredService<HomeViewModel>();
    }

    internal HomeViewModel ViewModel { get; }
}
