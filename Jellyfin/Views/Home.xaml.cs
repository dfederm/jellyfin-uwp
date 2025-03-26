using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.Views;

internal sealed partial class Home : Page
{
    public Home()
    {
        InitializeComponent();

        ViewModel = AppServices.Instance.ServiceProvider.GetRequiredService<HomeViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    public HomeViewModel ViewModel { get; }
}
