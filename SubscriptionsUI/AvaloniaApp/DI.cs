using AvaloniaApp.Services;
using AvaloniaApp.ViewModels;
using EventStore.Embedded;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PowerModels.Persistence;
using ReactiveDomain;
using ReactiveDomain.Foundation;

namespace AvaloniaApp;

public static class DI {
	private static IConfiguration? _configuration;
	private static ServiceProvider? _services;

	public static IConfiguration Configuration {
		get {
			if (_configuration is not null) {
				return _configuration;
			}

			_configuration = new ConfigurationBuilder().Build();
			return _configuration;
		}
	}

	public static ServiceProvider Services {
		get {
			if (_services is not null) {
				return _services!;
			}

			var services = new ServiceCollection();
			services.AddLogging(l => l.AddDebug());
			services.AddSingleton<IStreamStoreConnection>((sp) => {
				var metrics = Configuration.GetSection("metrics");

				//var scc = SingleVNodeClient.CreateInMem(sp.GetRequiredService<ILoggerFactory>().CreateLogger<SingleVNodeClient>(), sp.GetRequiredService<ILoggerFactory>(), metrics);
				var scc = SingleVNodeClient.CreateOnDisk("c:\\Users\\Richard\\Desktop\\AvaloniaApp\\db", sp.GetRequiredService<ILoggerFactory>().CreateLogger<SingleVNodeClient>(), sp.GetRequiredService<ILoggerFactory>(), metrics);

				scc.ConnectAsync().Wait();
				return scc;

				//var lds = new DataStore("...");
				//lds.Connect();
				//return lds;
			});
			services.AddSingleton<IConfiguredConnection>((sp) => new ConfiguredConnection(sp.GetRequiredService<IStreamStoreConnection>(), new PrefixedCamelCaseStreamNameBuilder(), new JsonMessageSerializer()));
			services.AddSingleton((sp) => sp.GetRequiredService<IConfiguredConnection>().GetCorrelatedRepository(caching: true));

			services.AddSingleton<EventProducerService>();

			services.AddTransient<MainWindowViewModel>();
			services.AddTransient<UsingReadModel>();

			_services = services.BuildServiceProvider();

			return _services;
		}
	}
}
