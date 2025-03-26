using System;
using System.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Jellyfin.Common;

internal sealed class EmptyCollectionToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is not ICollection enumerable
            ? throw new InvalidOperationException($"{nameof(EmptyCollectionToVisibilityConverter)} can only be used with collection types")
            : enumerable.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new InvalidOperationException($"{nameof(EmptyCollectionToVisibilityConverter)} cannot be used in a two-way binding");
    }
}
