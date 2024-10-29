using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Jellyfin.Sdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace Jellyfin;

internal sealed class AppServices
{
    private AppServices()
    {
        PackageId packageId = Package.Current.Id;
        PackageVersion packageVersion = packageId.Version;
        string packageVersionStr = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
        EasClientDeviceInformation deviceInformation = new();

        JellyfinSdkSettings sdkClientSettings = new();
        sdkClientSettings.Initialize(
            packageId.Name,
            packageVersionStr,
            deviceInformation.FriendlyName,
            deviceInformation.Id.ToString());

        ServiceCollection serviceCollection = new();

        serviceCollection.AddLogging(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
        });

        serviceCollection.AddHttpClient(
            "Jellyfin",
            httpClient =>
            {
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(packageId.Name, packageVersionStr));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 1.0));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));
            });

        // Add Jellyfin SDK services.
        serviceCollection.AddSingleton(sdkClientSettings);
        serviceCollection.AddSingleton<IAuthenticationProvider, JellyfinAuthenticationProvider>();
        serviceCollection.AddScoped<IRequestAdapter, JellyfinRequestAdapter>(s => new JellyfinRequestAdapter(
            s.GetRequiredService<IAuthenticationProvider>(),
            s.GetRequiredService<JellyfinSdkSettings>(),
            s.GetRequiredService<IHttpClientFactory>().CreateClient("Jellyfin")));
        serviceCollection.AddScoped<JellyfinApiClient>();

        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    public IServiceProvider ServiceProvider { get; }

    private static AppServices _instance;

    private static readonly object InstanceLock = new();

    private static AppServices GetInstance()
    {
        lock (InstanceLock)
        {
            return _instance ??= new AppServices();
        }
    }

    public static AppServices Instance => _instance ?? GetInstance();
}