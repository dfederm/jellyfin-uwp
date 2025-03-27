using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Models;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;

namespace Jellyfin.Views;

internal sealed partial class HomeViewModel : ObservableObject
{
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;

    [ObservableProperty]
    public partial ObservableCollection<BaseItemDto> UserViews { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<BaseItemDto> ContinueWatchingItems { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<BaseItemDto> ContinueListeningItems { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<BaseItemDto> ContinueReadingItems { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<BaseItemDto> NextUpItems { get; set; }

    [ObservableProperty]
    public partial List<HomeViewSection> Sections { get; set; }

    public HomeViewModel(JellyfinApiClient jellyfinApiClient, NavigationManager navigationManager)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _navigationManager = navigationManager;
    }

    public async Task InitializeAsync()
    {
        Task[] tasks =
        [
            InitializeUserViewsAsync(),
            InitializeContinueWatchingItemsAsync(),
            InitializeContinueListeningItemsAsync(),
            InitializeContinueReadingItemsAsync(),
            // TODO: LiveTV Section,
            InitializeNextUpItemsAsync(),
            // TODO: LatestMedia Sections
        ];

        await Task.WhenAll(tasks);

        Sections =
        [
            new () { Items = ContinueWatchingItems, Name = "Continue Watching" },
            new () { Items = ContinueListeningItems, Name = "Continue Listening" },
            new () { Items = ContinueReadingItems, Name = "Continue Reading" },
            new () { Items = NextUpItems, Name = "Next Up" }
        ];
    }

    private async Task InitializeUserViewsAsync()
    {
        List<BaseItemDto> items = new();

        BaseItemDtoQueryResult result = await _jellyfinApiClient.UserViews.GetAsync();
        foreach (BaseItemDto item in result.Items)
        {
            if (!item.Id.HasValue)
            {
                continue;
            }

            items.Add(item);
        }

        UserViews = new ObservableCollection<BaseItemDto>(items);
    }

    private async Task InitializeContinueWatchingItemsAsync()
    {
        List<BaseItemDto> items = await GetItemsToResumeAsync(MediaType.Video);
        ContinueWatchingItems = new ObservableCollection<BaseItemDto>(items);
    }

    private async Task InitializeContinueListeningItemsAsync()
    {
        List<BaseItemDto> items = await GetItemsToResumeAsync(MediaType.Audio);
        ContinueListeningItems = new ObservableCollection<BaseItemDto>(items);
    }

    private async Task InitializeContinueReadingItemsAsync()
    {
        List<BaseItemDto> items = await GetItemsToResumeAsync(MediaType.Book);
        ContinueReadingItems = new ObservableCollection<BaseItemDto>(items);
    }

    private async Task<List<BaseItemDto>> GetItemsToResumeAsync(MediaType mediaType)
    {
        List<BaseItemDto> items = new();

        BaseItemDtoQueryResult result = await _jellyfinApiClient.UserItems.Resume.GetAsync(requestConfig =>
        {
            requestConfig.QueryParameters.Limit = 12;
            requestConfig.QueryParameters.Fields = [ItemFields.PrimaryImageAspectRatio];
            requestConfig.QueryParameters.ImageTypeLimit = 1;
            requestConfig.QueryParameters.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Thumb];
            requestConfig.QueryParameters.EnableTotalRecordCount = false;
            requestConfig.QueryParameters.MediaTypes = [mediaType];
        });
        foreach (BaseItemDto item in result.Items)
        {
            if (!item.Id.HasValue)
            {
                continue;
            }

            items.Add(item);
        }

        return items;
    }

    private async Task InitializeNextUpItemsAsync()
    {
        List<BaseItemDto> items = new();

        BaseItemDtoQueryResult result = await _jellyfinApiClient.Shows.NextUp.GetAsync();
        foreach (BaseItemDto item in result.Items)
        {
            if (!item.Id.HasValue)
            {
                continue;
            }

            items.Add(item);
        }

        NextUpItems = new ObservableCollection<BaseItemDto>(items);
    }

    [RelayCommand]
    private void NavigateToItem(BaseItemDto item)
    {
        _navigationManager.NavigateToItem(item);
    }
}