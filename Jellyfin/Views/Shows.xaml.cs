using System;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.Views;

internal sealed partial class Shows : Page
{
    public Shows()
    {
        InitializeComponent();

        ViewModel = AppServices.Instance.ServiceProvider.GetRequiredService<ShowsViewModel>();
    }

    internal ShowsViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e) => ViewModel.HandleParameters(e.Parameter as Parameters);

    internal sealed record Parameters(Guid CollectionItemId);
}
