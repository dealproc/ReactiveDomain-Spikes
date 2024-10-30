using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using AvaloniaApp.Models;
using AvaloniaApp.Services;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReactiveUI;

namespace AvaloniaApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable {
	private readonly ILogger _log;
	private readonly EventProducerService _eps;

	private readonly List<IDisposable> _disposables = [];

	private ReadOnlyObservableCollection<SimpleMessage>? _messages;
	public ReadOnlyObservableCollection<SimpleMessage>? Messages => _messages;

	public ReadOnlyObservableCollection<SimpleMessage>? _subscriberMessages;
	public ReadOnlyObservableCollection<SimpleMessage>? SubscriberMessages => _subscriberMessages;

#pragma warning disable CA1822 // Mark members as static
	public string Greeting => "Welcome to Avalonia!";
#pragma warning restore CA1822 // Mark members as static


	public MainWindowViewModel(ILoggerFactory loggerFactory, EventProducerService eps) {
		_log = loggerFactory.CreateLogger<MainWindowViewModel>();
		_log.LogInformation("Constructed Main Window View Model.");

		_eps = eps;

		ConstructCommonElements();
	}

	public MainWindowViewModel() {
		_log = NullLoggerFactory.Instance.CreateLogger("Testing");
		_eps = null!;

		ConstructCommonElements();
	}

	private void ConstructCommonElements() {
		StartService = ReactiveCommand.Create(() => {
			var x = 0;
			_eps.Start();
		});
		StopService = ReactiveCommand.Create(() => {
			var x = 0;
			_eps.Stop();
		});

		if (_eps is not null) {
			_disposables.AddRange([
				_eps.Messages
					.ToObservableChangeSet(x => x.Message)
					.Bind(out _messages)
					.Subscribe(),
				_eps.SubscriberMessages
					.ToObservableChangeSet(x => x.Message)
					.Bind(out _subscriberMessages)
					.Subscribe()
			]);
		}
	}

	public void Dispose() {
		foreach(var d in _disposables) {
			d?.Dispose();
		}
		_disposables.Clear();
	}

	public ReactiveCommand<Unit, Unit> StartService { get; private set; } = null!;
	public ReactiveCommand<Unit, Unit> StopService { get; private set; } = null!;
}
