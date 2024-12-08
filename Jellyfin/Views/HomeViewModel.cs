using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;

namespace Jellyfin.Views;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;

    [ObservableProperty]
    private ObservableCollection<BaseItemDto> _userViews;

    [ObservableProperty]
    private ObservableCollection<BaseItemDto> _continueWatchingItems;

    public HomeViewModel(JellyfinApiClient jellyfinApiClient, NavigationManager navigationManager)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _navigationManager = navigationManager;

        InitializeUserViews();
        InitializeContinueWatchingItems();
    }

    private async void InitializeUserViews()
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

    private async void InitializeContinueWatchingItems()
    {
        List<BaseItemDto> items = new();

        BaseItemDtoQueryResult result = await _jellyfinApiClient.UserItems.Resume.GetAsync();
        foreach (BaseItemDto item in result.Items)
        {
            if (!item.Id.HasValue)
            {
                continue;
            }

            items.Add(item);
        }

        ContinueWatchingItems = new ObservableCollection<BaseItemDto>(items);
    }

    [RelayCommand]
    private void NavigateToItem(BaseItemDto item)
    {
        _navigationManager.NavigateToItem(item);
    }
}