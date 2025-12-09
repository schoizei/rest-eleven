using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using RestEleven.Client;
using RestEleven.Client.Data;
using RestEleven.Client.Options;
using RestEleven.Client.Services;
using SQLitePCL;

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

		builder.Services.AddHttpClient("PushServer", ConfigureHttpClient)
			.AddPolicyHandler(GetRetryPolicy());

		builder.Services.AddHttpClient("PersonioBridge", ConfigurePersonioClient)
			.AddPolicyHandler(GetRetryPolicy());

		builder.Services.AddScoped<ILocalDbService, SqliteDbService>();
		builder.Services.AddScoped<ILearningService, LearningService>();
		builder.Services.AddScoped<INotificationService, WebPushService>();
		builder.Services.AddScoped<IBackgroundSyncService, BackgroundSyncService>();
		builder.Services.AddScoped<IPersonioBridgeClient, PersonioBridgeClient>();
		builder.Services.AddScoped<ISettingsService, SettingsService>();

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
