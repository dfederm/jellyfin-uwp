using Windows.Storage;

namespace Jellyfin;

internal sealed class AppSettings
{
    public string ServerUrl
    {
        get => GetProperty<string>(nameof(ServerUrl));
        set => SetProperty(nameof(ServerUrl), value);
    }

    public string AccessToken
    {
        get => GetProperty<string>(nameof(AccessToken));
        set => SetProperty(nameof(AccessToken), value);
    }

    private static void SetProperty(string propertyName, object value)
        => ApplicationData.Current.LocalSettings.Values[propertyName] = value;

    private static T GetProperty<T>(string propertyName, T defaultValue = default)
    {
        object value = ApplicationData.Current.LocalSettings.Values[propertyName];
        return value != null ? (T)value : defaultValue;
    }
}
