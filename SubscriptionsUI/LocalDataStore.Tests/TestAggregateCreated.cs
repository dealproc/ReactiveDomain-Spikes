using ReactiveDomain.Messaging;

namespace PowerModels.Persistence.Tests {
	public class TestWoftamAggregateCreated : IEvent {
		public Guid MsgId { get; private set; }
		public Guid AggregateId { get; private set; }
		public TestWoftamAggregateCreated(Guid aggregateId) {
			MsgId = Guid.NewGuid();
			AggregateId = aggregateId;
		}
	}
}
