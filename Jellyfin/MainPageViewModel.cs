using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Windows.UI.Xaml.Controls;

namespace Jellyfin;

public sealed partial class MainPageViewModel : ObservableObject
{
    private readonly AppSettings _appSettings;
    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;

    [ObservableProperty]
    private bool _isMenuOpen;

    [ObservableProperty]
    private ObservableCollection<NavigationViewItemBase> _navigationItems;

    public MainPageViewModel(
        AppSettings appSettings,
        JellyfinApiClient jellyfinApiClient,
        NavigationManager navigationManager)
    {
        _appSettings = appSettings;
        _jellyfinApiClient = jellyfinApiClient;
        _navigationManager = navigationManager;

        InitializeNavigationItems();
    }

    public void HandleParameters(MainPage.Parameters parameters, Frame contentFrame)
    {
        if (parameters is not null)
        {
            parameters.DeferredNavigationAction();
        }
        else if (contentFrame.CurrentSourcePageType is null)
        {
            // Default to home
            _navigationManager.NavigateToHome();
        }
    }

    public void NavigationItemSelected(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.Tag is NavigationViewItemContext context)
        {
            context.NavigateAction();
            IsMenuOpen = false;
        }
    }

    public void UpdateSelectedMenuItem()
    {
        Guid? currentItem = _navigationManager.CurrentItem;
        if (!currentItem.HasValue)
        {
            return;
        }

        if (NavigationItems is not null)
        {
            foreach (NavigationViewItemBase item in NavigationItems)
            {
                if (item.Tag is NavigationViewItemContext context)
                {
                    if (context.ItemId == currentItem)
                    {
                        item.IsSelected = true;
                        break;
                    }
                }
            }
        }
    }

    private async void InitializeNavigationItems()
    {
        List<NavigationViewItemBase> navigationItems = new();

        navigationItems.Add(new NavigationViewItem
        {
            Content = "Home",
            Icon = new SymbolIcon(Symbol.Home),
            Tag = new NavigationViewItemContext(() => _navigationManager.NavigateToHome(), ItemId: NavigationManager.HomeId),
        });

        BaseItemDtoQueryResult result = await _jellyfinApiClient.UserViews.GetAsync();
        if (result.Items.Count > 0)
        {
            navigationItems.Add(new NavigationViewItemHeader { Content = "Media" });
        }

        foreach (BaseItemDto item in result.Items)
        {
            if (!item.Id.HasValue)
            {
                continue;
            }

            Guid itemId = item.Id.Value;

            // TODO: Create a better abstraction for this!
            if (item.CollectionType.HasValue && item.CollectionType.Value == BaseItemDto_CollectionType.Movies)
            {
                navigationItems.Add(new NavigationViewItem
                {
                    Content = item.Name,
                    Icon = new SymbolIcon(Symbol.Library),
                    Tag = new NavigationViewItemContext(() => _navigationManager.NavigateToItem(item), itemId),
                });
            }
            else
            {
                // TODO: Need to handle other library types. Display disabled for now.
                navigationItems.Add(new NavigationViewItem
                {
                    Content = item.Name,
                    Icon = new SymbolIcon(Symbol.Library),
                    IsEnabled = false,
                });
            }
        }

        navigationItems.Add(new NavigationViewItemHeader { Content = "User" });

        navigationItems.Add(new NavigationViewItem
        {
            Content = "Select Server",
            Icon = new SymbolIcon(Symbol.Switch),
            Tag = new NavigationViewItemContext(() => _navigationManager.NavigateToServerSelection(), ItemId: null),
        });

        navigationItems.Add(new NavigationViewItem
        {
            Content = "Sign Out",
            Icon = new SymbolIcon(Symbol.BlockContact),
            Tag = new NavigationViewItemContext(
                () =>
                {
                    _appSettings.AccessToken = null;
                    _navigationManager.NavigateToLogin();

                    // After signing out, disallow going back to a logged-in page.
                    _navigationManager.ClearHistory();
                },
                ItemId: null),
        });

        NavigationItems = new ObservableCollection<NavigationViewItemBase>(navigationItems);
    }

    public record NavigationViewItemContext(Action NavigateAction, Guid? ItemId);
}