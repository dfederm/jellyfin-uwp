using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using System.Collections.Generic;

namespace Jellyfin.Common;

internal sealed class EmptyCollectionToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is not IEnumerable<object> enumerable
            ? Visibility.Collapsed
            : enumerable.Count() == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new InvalidOperationException($"{nameof(EmptyCollectionToVisibilityConverter)} cannot be used in a two-way binding");
    }
}
