using System;
using System.Runtime.InteropServices;
using Blurhash;
using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Kiota.Abstractions.Serialization;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.UI.Core;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Jellyfin.Controls;

public sealed partial class LazyLoadedImageViewModel : ObservableObject
{
    private readonly JellyfinApiClient _jellyfinApiClient;

    [ObservableProperty]
    private BaseItemDto _item;

    [ObservableProperty]
    private ImageType? _imageType;

    [ObservableProperty]
    private int _width;

    [ObservableProperty]
    private int _height;

    [ObservableProperty]
    private bool _enableBlurHash = true;

    [ObservableProperty]
    private ImageSource _imageSource;

    [ObservableProperty]
    private ImageSource _placeholderSource;

    public LazyLoadedImageViewModel(JellyfinApiClient jellyfinApiClient)
    {
        _jellyfinApiClient = jellyfinApiClient;
    }

    partial void OnItemChanged(BaseItemDto value) => OnPropertyChanged();

    partial void OnImageTypeChanged(ImageType? value) => OnPropertyChanged();

    partial void OnWidthChanged(int value) => OnPropertyChanged();

    partial void OnHeightChanged(int value) => OnPropertyChanged();

    private async void OnPropertyChanged()
    {
        if (Item is null
            || ImageType is null
            || Width == 0
            || Height == 0)
        {
            return;
        }

        string imageTypeStr = ImageType.Value.ToString();
        if (!Item.ImageTags.AdditionalData.TryGetValue(imageTypeStr, out object imageTagObj))
        {
            // TODO: Is there some kind of placeholder we can use?
            return;
        }

        string imageTag = imageTagObj.ToString();

        if (EnableBlurHash)
        {
            // This is a little gross, but there doesn't seem to be a better way to do it.
            IAdditionalDataHolder blurHashesForType = ImageType.Value switch
            {
                Sdk.Generated.Models.ImageType.Art => Item.ImageBlurHashes.Art,
                Sdk.Generated.Models.ImageType.Backdrop => Item.ImageBlurHashes.Backdrop,
                Sdk.Generated.Models.ImageType.Banner => Item.ImageBlurHashes.Banner,
                Sdk.Generated.Models.ImageType.Box => Item.ImageBlurHashes.Box,
                Sdk.Generated.Models.ImageType.BoxRear => Item.ImageBlurHashes.BoxRear,
                Sdk.Generated.Models.ImageType.Chapter => Item.ImageBlurHashes.Chapter,
                Sdk.Generated.Models.ImageType.Disc => Item.ImageBlurHashes.Disc,
                Sdk.Generated.Models.ImageType.Logo => Item.ImageBlurHashes.Logo,
                Sdk.Generated.Models.ImageType.Menu => Item.ImageBlurHashes.Menu,
                Sdk.Generated.Models.ImageType.Primary => Item.ImageBlurHashes.Primary,
                Sdk.Generated.Models.ImageType.Profile => Item.ImageBlurHashes.Profile,
                Sdk.Generated.Models.ImageType.Screenshot => Item.ImageBlurHashes.Screenshot,
                Sdk.Generated.Models.ImageType.Thumb => Item.ImageBlurHashes.Thumb,
                _ => null,
            };
            string blurHash = null;
            if (blurHashesForType is not null
                && blurHashesForType.AdditionalData.TryGetValue(imageTag, out object blurHashObj))
            {
                blurHash = blurHashObj.ToString();
            }

            if (blurHash is not null)
            {
                SoftwareBitmap blurHashBitmap = CreateBlurHashImage(blurHash);

                SoftwareBitmapSource blurHashSource = new();
                await blurHashSource.SetBitmapAsync(blurHashBitmap);

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () => PlaceholderSource = blurHashSource);
            }
        }

        Uri imageUri = _jellyfinApiClient.GetImageUri(Item, ImageType.Value, Width, Height);
        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
            CoreDispatcherPriority.Low,
            () => ImageSource = new BitmapImage(imageUri));
    }

    private static unsafe SoftwareBitmap CreateBlurHashImage(string blurhash)
    {
        const int width = 20;
        const int height = 20;

        var pixelData = new Pixel[width, height];
        Core.Decode(blurhash, pixelData, 1);

        SoftwareBitmap softwareBitmap = new(BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);

        using (BitmapBuffer buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
        using (var reference = buffer.CreateReference())
        {
            ((IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out uint capacity);
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    Pixel pixel = pixelData[row, col];
                    *(dataInBytes++) = (byte)MathUtils.LinearTosRgb(pixel.Blue);
                    *(dataInBytes++) = (byte)MathUtils.LinearTosRgb(pixel.Green);
                    *(dataInBytes++) = (byte)MathUtils.LinearTosRgb(pixel.Red);
                    *(dataInBytes++) = 255;
                }
            }
        }

        return softwareBitmap;
    }

    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}
