using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Jellyfin.Views;

public sealed partial class Video : Page
{
    public Video()
    {
        InitializeComponent();

        ViewModel = AppServices.Instance.ServiceProvider.GetRequiredService<VideoViewModel>();
    }

    internal VideoViewModel ViewModel { get; }

    protected override async void OnNavigatedTo(NavigationEventArgs e) => await ViewModel.PlayVideoAsync(e.Parameter as Parameters, PlayerElement);

    protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e) => await ViewModel.StopVideoAsync();

    public record Parameters(BaseItemDto Item, string MediaSourceId, int? AudioStreamIndex, int? SubtitleStreamIndex);
}
