using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml;

namespace Jellyfin.Common;

/// <summary>
/// Value converter that translates true to <see cref="Visibility.Visible"/> and false to
/// <see cref="Visibility.Collapsed"/>, or the reverse if the parameter is "Reverse".
/// </summary>
internal sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => ((value is bool b && b)
            ^ (parameter as string ?? string.Empty).Equals("Reverse"))
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => (value is Visibility visibility && visibility == Visibility.Visible)
            ^ (parameter as string ?? string.Empty).Equals("Reverse");
}