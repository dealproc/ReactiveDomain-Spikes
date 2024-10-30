using AvaloniaApp.Services;
using EventStore.Embedded;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveDomain;

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
				var scc = SingleVNodeClient.CreateInMem(sp.GetRequiredService<ILoggerFactory>().CreateLogger<SingleVNodeClient>(), sp.GetRequiredService<ILoggerFactory>(), metrics);
				return scc;
			});
			services.AddSingleton<EventProducerService>();

			services.AddTransient<ViewModels.MainWindowViewModel>();

			_services = services.BuildServiceProvider();

			return _services;
		}
	}
}
