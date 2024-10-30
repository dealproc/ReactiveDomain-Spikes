using ReactiveDomain;
using ReactiveDomain.Foundation;
using ReactiveDomain.Messaging;
using ReactiveDomain.Testing;

// ReSharper disable once CheckNamespace
namespace PowerModels.Persistence.Tests {
	// todo: separate stream connection tests and repo tests
	// ReSharper disable InconsistentNaming
	public class DataStoreTests {
		// ReSharper disable once CollectionNeverQueried.Local
		private readonly IStreamStoreConnection _streamStoreConnection;

		private readonly IRepository _repo;
		private readonly IStreamNameBuilder _streamNameBuilder;
		private readonly Tuple<IStreamStoreConnection, StreamStoreRepository> _mockPair;


		public DataStoreTests() {
			_streamNameBuilder = new PrefixedCamelCaseStreamNameBuilder("UnitTest");

			_streamStoreConnection = new DataStore("MockStore");
			_streamStoreConnection.Connect();

			_repo = new StreamStoreRepository(_streamNameBuilder, _streamStoreConnection, new JsonMessageSerializer());
			_mockPair = new Tuple<IStreamStoreConnection, StreamStoreRepository>(_streamStoreConnection, (StreamStoreRepository)_repo);
		}


		[Fact]
		public void can_save_new_aggregate() {

			var id = Guid.NewGuid();
			var tAgg = new TestAggregate(id);
			_repo.Save(tAgg);
			var rAgg = _repo.GetById<TestAggregate>(id);
			Assert.NotNull(rAgg);
			Assert.Equal(tAgg.Id, rAgg.Id);

		}
		[Fact]
		public void can_try_get_new_aggregate() {

			var id = Guid.NewGuid();
			Assert.False(_repo.TryGetById(id, out TestAggregate tAgg));
			Assert.Null(tAgg);
			tAgg = new TestAggregate(id);
			_repo.Save(tAgg);

			Assert.True(_repo.TryGetById(id, out TestAggregate rAgg));
			Assert.NotNull(rAgg);
			Assert.Equal(tAgg.Id, rAgg.Id);

		}
		[Fact]
		public void can_try_get_new_aggregate_at_version() {
			var id = Guid.NewGuid();
			Assert.False(_repo.TryGetById(id, out TestAggregate tAgg, 1));
			Assert.Null(tAgg);
			tAgg = new TestAggregate(id);
			_repo.Save(tAgg);

			Assert.True(_repo.TryGetById(id, out TestAggregate rAgg, 1));
			Assert.NotNull(rAgg);
			Assert.Equal(tAgg.Id, rAgg.Id);
		}

		[Fact]
		public void can_update_and_save_aggregate() {

			var id = Guid.NewGuid();
			var tAgg = new TestAggregate(id);
			_repo.Save(tAgg);
			// Get v2
			var v2Agg = _repo.GetById<TestAggregate>(id);
			Assert.Equal((uint)0, v2Agg.CurrentAmount());
			// update v2
			v2Agg.RaiseBy(1);
			Assert.Equal((uint)1, v2Agg.CurrentAmount());
			_repo.Save(v2Agg);
			// get v3
			var v3Agg = _repo.GetById<TestAggregate>(id);
			Assert.Equal((uint)1, v3Agg.CurrentAmount());

		}

		[Fact]
		public void throws_on_requesting_specific_version_higher_than_exists() {

			var id = Guid.NewGuid();
			var tAgg = new TestAggregate(id);
			tAgg.RaiseBy(1);
			tAgg.RaiseBy(1);
			tAgg.RaiseBy(1);
			tAgg.RaiseBy(1); //v4
			_repo.Save(tAgg);
			Assert.Throws<AggregateVersionException>(() => _repo.GetById<TestAggregate>(id, 50));

		}

		[Fact]
		public void can_get_aggregate_at_version() {

			var id = Guid.NewGuid();
			Assert.Throws<InvalidOperationException>(() => _repo.GetById<TestAggregate>(id, 0));
			var tAgg = new TestAggregate(id);
			tAgg.RaiseBy(1);
			Assert.Equal((uint)1, tAgg.CurrentAmount());
			tAgg.RaiseBy(2);
			Assert.Equal((uint)3, tAgg.CurrentAmount());
			// get latest version (v3)
			_repo.Save(tAgg);
			var v3Agg = _repo.GetById<TestAggregate>(id);
			Assert.Equal((uint)3, v3Agg.CurrentAmount());

			//get version v2
			var v2Agg = _repo.GetById<TestAggregate>(id, 2);
			Assert.Equal((uint)1, v2Agg.CurrentAmount());


		}

		[Fact]
		public void will_throw_concurrency_exception() {

			var id = Guid.NewGuid();
			var tAgg = new TestAggregate(id);
			tAgg.RaiseBy(1);
			Assert.Equal((uint)1, tAgg.CurrentAmount());

			tAgg.RaiseBy(2);
			Assert.Equal((uint)3, tAgg.CurrentAmount());

			// get latest version (v3) then update & save
			_repo.Save(tAgg);
			var v3Agg = _repo.GetById<TestAggregate>(id);
			v3Agg.RaiseBy(2);
			_repo.Save(v3Agg);

			//Update & save original copy
			tAgg.RaiseBy(6);
			var r = _repo; //copy iteration variable for closure
			Assert.Throws<WrongExpectedVersionException>(() => r.Save(tAgg));

		}

		[Fact]
		public void can_multiple_update_and_save_multiple_aggregates() {

			var id1 = Guid.NewGuid();
			var tAgg = new TestAggregate(id1);
			tAgg.RaiseBy(1);
			tAgg.RaiseBy(2);
			tAgg.RaiseBy(3);
			_repo.Save(tAgg);
			var loadedAgg1 = _repo.GetById<TestAggregate>(id1);
			Assert.True(tAgg.CurrentAmount() == loadedAgg1.CurrentAmount());

			var id2 = Guid.NewGuid();
			var tAgg2 = new TestAggregate(id2);
			tAgg2.RaiseBy(4);
			tAgg2.RaiseBy(5);
			tAgg2.RaiseBy(6);
			_repo.Save(tAgg2);
			var loadedAgg2 = _repo.GetById<TestAggregate>(id2);
			Assert.True(tAgg2.CurrentAmount() == loadedAgg2.CurrentAmount());

		}

		[Fact]
		public void can_save_multiple_aggregate_types() {

			foreach (var repo in new[] { _mockPair }) {
				var events = new List<RecordedEvent>();
				repo.Item1.SubscribeToAll(events.Add);

				var id1 = Guid.NewGuid();
				repo.Item2.Save(new TestAggregate(id1));





				var agg1 = repo.Item2.GetById<TestAggregate>(id1);
				Assert.NotNull(agg1);
				Assert.Equal(id1, agg1.Id);

				AssertEx.IsOrBecomesTrue(() => events.Count == 3);
				Assert.Collection(events,
					re => Assert.Equal(nameof(TestAggregateMessages.NewAggregate), re.EventType),
					re => Assert.Equal(nameof(TestAggregateMessages.NewAggregate), re.EventType),
					re => Assert.Equal(nameof(TestAggregateMessages.NewAggregate), re.EventType));

				Assert.Collection(events,
					re => Assert.Equal(_streamNameBuilder.GenerateForAggregate(typeof(TestAggregate), id1),
						re.EventStreamId),
					re => Assert.Equal(_streamNameBuilder.GenerateForCategory(typeof(TestAggregate)),
						((ProjectedEvent)re).ProjectedStream),
					re => Assert.Equal(_streamNameBuilder.GenerateForEventType(nameof(TestAggregateMessages.NewAggregate)),
						((ProjectedEvent)re).ProjectedStream));
				events.Clear();

				var id2 = Guid.NewGuid();
				repo.Item2.Save(new TestAggregate2(id2));

				var agg2 = repo.Item2.GetById<TestAggregate2>(id2);
				Assert.NotNull(agg2);
				Assert.Equal(id2, agg2.Id);

				AssertEx.IsOrBecomesTrue(() => events.Count == 3);
				Assert.Collection(events,
					re => Assert.Equal(nameof(TestAggregateMessages.NewAggregate2), re.EventType),
					re => Assert.Equal(nameof(TestAggregateMessages.NewAggregate2), re.EventType),
					re => Assert.Equal(nameof(TestAggregateMessages.NewAggregate2), re.EventType));

				Assert.Collection(events,
					re => Assert.Equal(_streamNameBuilder.GenerateForAggregate(typeof(TestAggregate2), id2),
						re.EventStreamId),
					re => Assert.Equal(_streamNameBuilder.GenerateForCategory(typeof(TestAggregate2)),
						((ProjectedEvent)re).ProjectedStream),
					re => Assert.Equal(_streamNameBuilder.GenerateForEventType(nameof(TestAggregateMessages.NewAggregate2)),
						((ProjectedEvent)re).ProjectedStream));
			}
		}


		public class TestEvent : IMessage {
			public Guid MsgId { get; private set; }
			public readonly int MessageNumber;
			public TestEvent(
				int messageNumber) {
				MsgId = Guid.NewGuid();
				MessageNumber = messageNumber;
			}
		}
	}

	// ReSharper restore InconsistentNaming
}
