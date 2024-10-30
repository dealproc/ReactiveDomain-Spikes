using ReactiveDomain;

namespace PowerModels.Persistence.Tests {
	public class TestWoftamAggregate : EventDrivenStateMachine {
		public TestWoftamAggregate(Guid aggregateId) : this() {
			Raise(new TestWoftamAggregateCreated(aggregateId));
		}

		private TestWoftamAggregate() {
			Register<TestWoftamAggregateCreated>(e => Id = e.AggregateId);
			Register<WoftamEvent>(e => AppliedEventCount++);
		}

		public int AppliedEventCount { get; private set; }

		public void ProduceEvents(int count) {
			for (int i = 0; i < count; i++)
				Raise(new WoftamEvent("Woftam1-" + i, "Woftam2-" + i));
		}
	}
}
