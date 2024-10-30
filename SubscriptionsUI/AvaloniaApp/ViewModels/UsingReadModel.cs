using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ReactiveDomain;
using ReactiveDomain.Foundation;
using ReactiveDomain.Messaging;
using ReactiveDomain.Messaging.Bus;

namespace AvaloniaApp.ViewModels;

public class UsingReadModel : ReadModelBase, IHandle<SimpleAggregateMsgs.Notification> {
	private readonly ICorrelatedRepository _repository;
	private CancellationTokenSource _cts = new();

	public ObservableCollection<string> Notifications { get; } = new();

	public UsingReadModel(ICorrelatedRepository repository, IConfiguredConnection connection) : base(nameof(UsingReadModel), connection) {
		_repository = repository;

		EventStream.Subscribe<SimpleAggregateMsgs.Notification>(this);

		Start<SimpleAggregate>();
	}

	public void Start() {
		Notifications.Clear();

		if (_cts is null) {
			_cts = new();
		}

		Task.Yield();

		IssueNotifications();
	}

	public void Stop() {
		_cts?.Cancel();
		_cts = null!;
	}

	public void Handle(SimpleAggregateMsgs.Notification msg) {
		Notifications.Add(msg.Message);
	}

	private async void IssueNotifications() {
		var id = Guid.NewGuid();
		while (true) {
			var cmd = new Dummy();
			if (!_repository.TryGetById<SimpleAggregate>(id, out var aggregate, cmd)) {
				aggregate = new SimpleAggregate(id, cmd);
			}

			aggregate.Notify($"It's {DateTime.Now:G} and all's well.");
			_repository.Save(aggregate);

			try {
				await Task.Delay(500, _cts.Token);
			} catch (TaskCanceledException) {
				break;
			}
		}
	}

	protected override void Dispose(bool disposing) {
		base.Dispose(disposing);
		if (!disposing) {
			return;
		}

		_cts?.Dispose();
	}
}

public class SimpleAggregate : AggregateRoot {
	public SimpleAggregate(Guid id, ICorrelatedMessage msg) : base(msg) {
		RegisterEvents();
		Raise(new SimpleAggregateMsgs.Initialized(id));
	}

	public SimpleAggregate() {
		RegisterEvents();
	}

	private void RegisterEvents() {
		Register<SimpleAggregateMsgs.Initialized>(Apply);
	}

	public void Notify(string message) {
		Raise(new SimpleAggregateMsgs.Notification(Id, message));
	}

	private void Apply(SimpleAggregateMsgs.Initialized msg) {
		Id = msg.SimpleAggregateId;
	}
}

public class SimpleAggregateMsgs {
	public class Initialized : Event {
		public readonly Guid SimpleAggregateId;

		public Initialized(Guid simpleAggregateId) {
			SimpleAggregateId = simpleAggregateId;
		}
	}
	public class Notification : Event {
		public readonly Guid SimpleAggregateId;
		public readonly string Message;

		public Notification(Guid simpleAggregateId, string message) {
			SimpleAggregateId = simpleAggregateId;
			Message = message;
		}
	}
}

class Dummy : Command {

}
