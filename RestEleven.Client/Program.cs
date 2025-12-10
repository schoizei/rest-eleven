using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using RestEleven.Client.Data;
using RestEleven.Client.Options;
using RestEleven.Client.Services;

namespace RestEleven.Client;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        SQLitePCL.Batteries_V2.Init();

        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        builder.Services.Configure<ClientOptions>(builder.Configuration.GetSection("Client"));
        builder.Services.AddScoped(sp => sp.GetRequiredService<IOptionsSnapshot<ClientOptions>>().Value);

        builder.Services.AddDbContextFactory<AppDbContext>(options =>
        {
            options.UseSqlite("Filename=resteleven.db");
        });

        builder.Services.AddScoped<PushServerAuthorizationMessageHandler>();
        builder.Services.AddScoped<PersonioBridgeAuthorizationMessageHandler>();

        builder.Services.AddHttpClient("PushServer", ConfigureHttpClient)
            .AddPolicyHandler(GetRetryPolicy())
            .AddHttpMessageHandler<PushServerAuthorizationMessageHandler>();

        builder.Services.AddHttpClient("PersonioBridge", ConfigurePersonioClient)
            .AddPolicyHandler(GetRetryPolicy())
            .AddHttpMessageHandler<PersonioBridgeAuthorizationMessageHandler>();

        builder.Services.AddScoped<ILocalDbService, SqliteDbService>();
        builder.Services.AddScoped<ILearningService, LearningService>();
        builder.Services.AddScoped<INotificationService, WebPushService>();
        builder.Services.AddScoped<IBackgroundSyncService, BackgroundSyncService>();
        builder.Services.AddScoped<IPersonioBridgeClient, PersonioBridgeClient>();
        builder.Services.AddScoped<ISettingsService, SettingsService>();

        builder.Services.AddMsalAuthentication(options =>
        {
            builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
            var defaultScopes = builder.Configuration.GetSection("AzureAd:DefaultScopes").Get<string[]>() ?? Array.Empty<string>();
            foreach (var scope in defaultScopes)
            {
                options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);
            }
        });

        var host = builder.Build();
        await host.RunAsync();

        static void ConfigureHttpClient(IServiceProvider serviceProvider, HttpClient client)
        {
            var options = serviceProvider.GetRequiredService<IOptionsSnapshot<ClientOptions>>().Value;
            client.BaseAddress = options.PushServerBaseUrl;
        }

        static void ConfigurePersonioClient(IServiceProvider serviceProvider, HttpClient client)
        {
            var options = serviceProvider.GetRequiredService<IOptionsSnapshot<ClientOptions>>().Value;
            client.BaseAddress = options.PersonioBridgeBaseUrl;
        }
    }

    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy() => HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 250));
}
