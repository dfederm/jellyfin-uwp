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

    public HomeViewModel(JellyfinApiClient jellyfinApiClient, NavigationManager navigationManager)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _navigationManager = navigationManager;

        InitializeUserViews();
    }

    private async void InitializeUserViews()
    {
        List<BaseItemDto> userViews = new();

        BaseItemDtoQueryResult result = await _jellyfinApiClient.UserViews.GetAsync();
        foreach (BaseItemDto item in result.Items)
        {
            if (!item.Id.HasValue)
            {
                continue;
            }

            userViews.Add(item);
        }

        UserViews = new ObservableCollection<BaseItemDto>(userViews);
    }

    [RelayCommand]
    private void NavigateToUserView(BaseItemDto userView)
    {
        if (userView.Id.HasValue)
        {
            if (userView.CollectionType.HasValue)
            {
                if (userView.CollectionType.Value == BaseItemDto_CollectionType.Movies)
                {
                    _navigationManager.NavigateToMovies(userView.Id.Value);
                }
            }
        }
    }
}