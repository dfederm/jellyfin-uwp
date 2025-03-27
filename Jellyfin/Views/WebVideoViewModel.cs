using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Jellyfin.Views;

public sealed partial class WebVideoViewModel : ObservableObject
{
    [ObservableProperty]
    public partial Uri VideoUri { get; set; }

    public void HandleParameters(WebVideo.Parameters parameters)
    {
        VideoUri = parameters.VideoUri;
    }
}