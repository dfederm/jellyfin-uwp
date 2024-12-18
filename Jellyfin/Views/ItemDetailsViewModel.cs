using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Services;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Jellyfin.Views;

internal sealed record MediaInfoItem(string Text);

internal sealed record MediaStreamOption(string DisplayText, int? Index)
{
    public static MediaStreamOption SubtitlesOff { get; } = new("Off", -1);
}

internal sealed partial class ItemDetailsViewModel : ObservableObject
{
    private static readonly SolidColorBrush OnBrush = new SolidColorBrush(Colors.Red);
    private static readonly SolidColorBrush OffBrush = new SolidColorBrush(Colors.White);

    private readonly JellyfinApiClient _jellyfinApiClient;
    private readonly NavigationManager _navigationManager;

    [ObservableProperty]
    private BaseItemDto _item;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private Uri _backdropImageUri;

    [ObservableProperty]
    private ObservableCollection<MediaInfoItem> _mediaInfo;

    [ObservableProperty]
    private ObservableCollection<MediaSourceInfo> _sourceContainers;

    [ObservableProperty]
    private MediaSourceInfo _selectedSourceContainer;

    [ObservableProperty]
    private ObservableCollection<MediaStreamOption> _videoStreams;

    [ObservableProperty]
    private MediaStreamOption _selectedVideoStream;

    [ObservableProperty]
    private ObservableCollection<MediaStreamOption> _audioStreams;

    [ObservableProperty]
    private MediaStreamOption _selectedAudioStream;

    [ObservableProperty]
    private ObservableCollection<MediaStreamOption> _subtitleStreams;

    [ObservableProperty]
    private MediaStreamOption _selectedSubtitleStream;

    [ObservableProperty]
    private string _tagLine;

    [ObservableProperty]
    private string _overview;

    [ObservableProperty]
    private string _tags;

    [ObservableProperty]
    private bool _isPlayed;

    [ObservableProperty]
    private Brush _playStateBrush;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private Brush _favoriteBrush;

    public ItemDetailsViewModel(JellyfinApiClient jellyfinApiClient, NavigationManager navigationManager)
    {
        _jellyfinApiClient = jellyfinApiClient;
        _navigationManager = navigationManager;
    }

    public async void HandleParameters(ItemDetails.Parameters parameters)
    {
        Item = await _jellyfinApiClient.Items[parameters.ItemId].GetAsync();

        Name = Item.Name;
        BackdropImageUri = _jellyfinApiClient.GetItemBackdropImageUrl(Item, 1920);

        List<MediaInfoItem> mediaInfo = new();
        if (Item.ProductionYear.HasValue)
        {
            mediaInfo.Add(new MediaInfoItem(Item.ProductionYear.ToString()));
        }

        if (Item.RunTimeTicks.HasValue)
        {
            mediaInfo.Add(new MediaInfoItem(GetDisplayDuration(Item.RunTimeTicks.Value)));
        }

        if (!string.IsNullOrEmpty(Item.OfficialRating))
        {
            // TODO: Style correctly
            mediaInfo.Add(new MediaInfoItem(Item.OfficialRating));
        }

        if (Item.CommunityRating.HasValue)
        {
            // TODO: Style correctly
            mediaInfo.Add(new MediaInfoItem(Item.CommunityRating.Value.ToString("F1")));
        }

        if (Item.CriticRating.HasValue)
        {
            // TODO: Style correctly
            mediaInfo.Add(new MediaInfoItem(Item.CriticRating.Value.ToString()));
        }

        if (Item.RunTimeTicks.HasValue)
        {
            mediaInfo.Add(new MediaInfoItem(GetEndsAt(Item.RunTimeTicks.Value)));
        }

        MediaInfo = new ObservableCollection<MediaInfoItem>(mediaInfo);

        SourceContainers = new ObservableCollection<MediaSourceInfo>(Item.MediaSources);

        // This will trigger OnSelectedSourceContainerChanged, which populates the video, audio, and subtitle drop-downs.
        SelectedSourceContainer = SourceContainers[0];

        TagLine = Item.Taglines.Count > 0 ? Item.Taglines[0] : null;
        Overview = Item.Overview;
        Tags = $"Tags: {string.Join(", ", Item.Tags)}";

        UpdateUserData();
    }

    partial void OnSelectedSourceContainerChanged(MediaSourceInfo mediaSourceInfo)
    {
        DetermineVideoOptions(mediaSourceInfo);
        DetermineAudioOptions(mediaSourceInfo);
        DetermineSubtitleOptions(mediaSourceInfo);
    }

    private void DetermineVideoOptions(MediaSourceInfo mediaSourceInfo)
    {
        List<MediaStream> videoStreams = mediaSourceInfo.MediaStreams
            .Where(s => s.Type == MediaStream_Type.Video)
            .OrderBy(s => s, MediaStreamComparer.Instance)
            .ToList();
        int? selectedIndex = videoStreams.Count > 0 ? videoStreams[0].Index : -1;

        MediaStreamOption selectedOption = null;
        List<MediaStreamOption> options = new(videoStreams.Count);
        foreach (MediaStream videoStream in videoStreams)
        {
            string displayTitle = videoStream.DisplayTitle;
            if (string.IsNullOrEmpty(displayTitle))
            {
                // DisplayTitle isn't always populated for video
                // TODO: Get the resolution text and codec. See /src/controllers/itemDetails/index.js::renderVideoSelections
                displayTitle = "TODO";
            }

            MediaStreamOption option = new(displayTitle, videoStream.Index);
            options.Add(option);

            if (selectedOption is null || videoStream.Index == selectedIndex)
            {
                selectedOption = option;
            }
        }

        VideoStreams = new ObservableCollection<MediaStreamOption>(options);
        SelectedVideoStream = selectedOption;
    }

    private void DetermineAudioOptions(MediaSourceInfo mediaSourceInfo)
    {
        List<MediaStream> audioStreams = mediaSourceInfo.MediaStreams
            .Where(s => s.Type == MediaStream_Type.Audio)
            .OrderBy(s => s, MediaStreamComparer.Instance)
            .ToList();
        int? selectedIndex = mediaSourceInfo.DefaultAudioStreamIndex;

        MediaStreamOption selectedOption = null;
        List<MediaStreamOption> options = new(audioStreams.Count);
        foreach (MediaStream audioStream in audioStreams)
        {
            MediaStreamOption option = new(audioStream.DisplayTitle, audioStream.Index);
            options.Add(option);

            if (selectedOption is null || audioStream.Index == selectedIndex)
            {
                selectedOption = option;
            }
        }

        AudioStreams = new ObservableCollection<MediaStreamOption>(options);
        SelectedAudioStream = selectedOption;
    }

    private void DetermineSubtitleOptions(MediaSourceInfo mediaSourceInfo)
    {
        List<MediaStream> subtitleStreams = mediaSourceInfo.MediaStreams
            .Where(s => s.Type == MediaStream_Type.Subtitle)
            .OrderBy(s => s, MediaStreamComparer.Instance)
            .ToList();

        MediaStreamOption selectedOption = null;
        List<MediaStreamOption> options = new(subtitleStreams.Count + 1);
        options.Add(MediaStreamOption.SubtitlesOff);

        int selectedIndex;
        if (mediaSourceInfo.DefaultSubtitleStreamIndex.HasValue)
        {
            selectedIndex = mediaSourceInfo.DefaultSubtitleStreamIndex.Value;
        }
        else
        {
            selectedIndex = -1;
            selectedOption = MediaStreamOption.SubtitlesOff;
        }

        foreach (MediaStream subtitleStream in subtitleStreams)
        {
            MediaStreamOption option = new(subtitleStream.DisplayTitle, subtitleStream.Index);
            options.Add(option);

            if (selectedOption is null || subtitleStream.Index == selectedIndex)
            {
                selectedOption = option;
            }
        }

        SubtitleStreams = new ObservableCollection<MediaStreamOption>(options);
        SelectedSubtitleStream = selectedOption;
    }

    public void Play()
    {
        _navigationManager.NavigateToVideo(
            Item,
            SelectedSourceContainer.Id,
            SelectedAudioStream?.Index,
            SelectedSubtitleStream?.Index);
    }

    public async void PlayTrailer()
    {
        if (Item.LocalTrailerCount > 0)
        {
            List<BaseItemDto> localTrailers = await _jellyfinApiClient.Items[Item.Id.Value].LocalTrailers.GetAsync();
            if (localTrailers.Count > 0)
            {
                // TODO play all the trailers instead of just the first?
                _navigationManager.NavigateToVideo(
                    localTrailers[0],
                    mediaSourceId: null,
                    audioStreamIndex: null,
                    subtitleStreamIndex: null);
                return;
            }
        }

        if (Item.RemoteTrailers.Count > 0)
        {
            // TODO play all the trailers instead of just the first?
            Uri videoUri = GetWebVideoUri(Item.RemoteTrailers[0].Url);

            _navigationManager.NavigateToWebVideo(videoUri);
            return;
        }
    }

    public async void TogglePlayed()
    {
        Item.UserData = Item.UserData.Played.GetValueOrDefault()
            ? await _jellyfinApiClient.UserPlayedItems[Item.Id.Value].DeleteAsync()
            : await _jellyfinApiClient.UserPlayedItems[Item.Id.Value].PostAsync();
        UpdateUserData();
    }

    public async void ToggleFavorite()
    {
        Item.UserData = Item.UserData.IsFavorite.GetValueOrDefault()
            ? await _jellyfinApiClient.UserFavoriteItems[Item.Id.Value].DeleteAsync()
            : await _jellyfinApiClient.UserFavoriteItems[Item.Id.Value].PostAsync();
        UpdateUserData();
    }

    // Return a string in '{}h {}m' format for duration.
    private static string GetDisplayDuration(long ticks)
    {
        int totalMinutes = (int)Math.Round(ticks / 600000000d);
        if (totalMinutes == 0)
        {
            totalMinutes = 1;
        }

        double totalHours = totalMinutes / 60;
        double remainderMinutes = totalMinutes % 60;

        StringBuilder sb = new();
        if (totalHours > 0)
        {
            sb.Append(totalHours);
            sb.Append("h ");
        }

        sb.Append(remainderMinutes);
        sb.Append('m');

        return sb.ToString();
    }

    private static string GetEndsAt(long ticks)
    {
        DateTime endDate = DateTime.Now + TimeSpan.FromTicks(ticks);
        return $"Ends at {endDate:t}";
    }

    private void UpdateUserData()
    {
        IsPlayed = Item.UserData.Played.GetValueOrDefault();
        PlayStateBrush = IsPlayed ? OnBrush : OffBrush;

        IsFavorite = Item.UserData.IsFavorite.GetValueOrDefault();
        FavoriteBrush = IsFavorite ? OnBrush : OffBrush;
    }

    private static Uri GetWebVideoUri(string url)
    {
        Match match = YouTubeRegex.Match(url);
        if (match.Success)
        {
            string youtubeBaseUrl = match.Groups["urlBase"].Value;
            string youtubeVideoId = match.Groups["id"].Value;

            // Use the embed url with autoplay enabled.
            return new($"{youtubeBaseUrl}/embed/{youtubeVideoId}?rel=0&autoplay=1");
        }

        // Fallback to the full url.
        return new Uri(url);
    }

    private static readonly Regex YouTubeRegex = new(
        @"(?<urlBase>https://www.youtube.com)/watch\?v=(?<id>[^&]+)",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    private sealed class MediaStreamComparer : IComparer<MediaStream>
    {
        private MediaStreamComparer()
        {
        }

        public static MediaStreamComparer Instance { get; } = new MediaStreamComparer();

        public int Compare(MediaStream x, MediaStream y)
        {
            int cmp = Compare(x.IsExternal.GetValueOrDefault(), y.IsExternal.GetValueOrDefault());
            if (cmp != 0)
            {
                return cmp;
            }

            cmp = Compare(x.IsForced.GetValueOrDefault(), y.IsForced.GetValueOrDefault());
            if (cmp != 0)
            {
                return cmp;
            }

            cmp = Compare(x.IsDefault.GetValueOrDefault(), y.IsDefault.GetValueOrDefault());
            if (cmp != 0)
            {
                return cmp;
            }

            return x.Index.GetValueOrDefault() - y.Index.GetValueOrDefault();

            static int Compare(bool x, bool y) => (x ? 1 : 0) - (y ? 1 : 0);
        }
    }
}