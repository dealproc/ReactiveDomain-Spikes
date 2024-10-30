using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaApp.Models;
using ReactiveDomain;

namespace AvaloniaApp.Services;

public sealed class EventProducerService : IDisposable {
	private readonly IStreamStoreConnection _streamStoreConnection;
	private readonly List<IDisposable> _disposables = [];

	CancellationTokenSource _cts = new();

	public ObservableCollection<SimpleMessage> Messages { get; } = [];
	public ObservableCollection<SimpleMessage> SubscriberMessages { get; } = [];

	public EventProducerService(IStreamStoreConnection streamStoreConnection) {
		_streamStoreConnection = streamStoreConnection;
		_disposables.Add(_streamStoreConnection.SubscribeToStream(SimpleMessage.StreamName, (e) => {
			var msg = JsonSerializer.Deserialize<SimpleMessage>(new MemoryStream(e.Data))!;
			SubscriberMessages.Add(msg);
		}));
	}

	public void Start() {
		if (_cts is not null) {
			_cts.Cancel();
			_cts = new();
		}

		SubscriberMessages?.Clear();
		Task.Factory.StartNew(DoWork, _cts!.Token);
	}

	public void Stop() {
		_cts?.Cancel(false);
		_cts = null!;
	}

	private async void DoWork() {
		while (!_cts!.Token.IsCancellationRequested) {
			var msg = new SimpleMessage { Message = $"It's {DateTime.Now:G} and all's well." };
			using (var ms = new MemoryStream()) {
				JsonSerializer.Serialize(ms, msg);
				ms.Seek(0, SeekOrigin.Begin);

				_streamStoreConnection.AppendToStream(SimpleMessage.StreamName, ExpectedVersion.Any, events: [
					new EventData(Guid.NewGuid(), nameof(SimpleMessage).ToLowerInvariant(), true, ms.ToArray(), [])
				]);
				Messages.Add(msg);
			}

			try {
				await Task.Delay(500, _cts.Token);
			} catch (TaskCanceledException _) {
				break;
			}
		}
	}

	public void Dispose() {
		foreach (var d in _disposables ?? []) {
			d.Dispose();
		}
		_disposables?.Clear();
	}
}
