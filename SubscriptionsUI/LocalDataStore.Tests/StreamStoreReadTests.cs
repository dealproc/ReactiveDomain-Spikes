using ReactiveDomain;
using ReactiveDomain.Foundation;
using ReactiveDomain.Messaging;

namespace PowerModels.Persistence.Tests {
	public class StreamStoreReadTests {
		private readonly IStreamStoreConnection _store;
		private readonly IEventSerializer _serializer = new JsonMessageSerializer();
		private readonly string _streamName;
		private readonly int _lastEvent;

		public StreamStoreReadTests() {
			IStreamNameBuilder streamNameBuilder = new PrefixedCamelCaseStreamNameBuilder("UnitTest");
			var dataStore = new DataStore("Test");
			dataStore.Connect();

			_store = dataStore;

			_streamName = streamNameBuilder.GenerateForAggregate(typeof(TestAggregate), Guid.NewGuid());
			var eventCount = 10;

			AppendEvents(eventCount, _store, _streamName);

			_lastEvent = eventCount - 1;
		}
		private void AppendEvents(int numEventsToBeSent, IStreamStoreConnection conn, string streamName) {
			for (int evtNumber = 0; evtNumber < numEventsToBeSent; evtNumber++) {
				var evt = new ReadTestTestEvent(evtNumber);
				conn.AppendToStream(streamName, ExpectedVersion.Any, null, _serializer.Serialize(evt));
			}
		}
		[Fact]
		public void connection_name_is_set() {
			var name = "FooConnection";
			var conn = new DataStore(name);
			Assert.Equal(name, conn.ConnectionName);
		}
		[Fact]
		public void can_create_then_delete_stream() {


			var streamName = $"ReadTest-{Guid.NewGuid()}";
			_store.AppendToStream(streamName, ExpectedVersion.Any, null, _serializer.Serialize(new ReadTestTestEvent(1)));
			var slice = _store.ReadStreamForward(streamName, 0, 1);

			// Ensure stream has been created
			Assert.IsNotType<StreamNotFoundSlice>(slice);
			Assert.IsNotType<StreamDeletedSlice>(slice);

			_store.DeleteStream(streamName, ExpectedVersion.Any);

			// Ensure stream has been deleted
			// We do not support soft delete so StreamNotFoundSlice instead of StreamDeletedSlice
			Assert.IsType<StreamNotFoundSlice>(_store.ReadStreamForward(streamName, 0, 1));

		}
		[Fact]
		public void cannot_delete_system_streams() {

			Assert.Throws<AggregateException>(() => _store.DeleteStream("$streams", ExpectedVersion.Any));

		}
		[Fact]
		public void can_delete_missing_streams() {

			var name = $"Missing-{Guid.NewGuid()}";
			_store.DeleteStream(name, ExpectedVersion.Any);
			_store.DeleteStream(name, ExpectedVersion.EmptyStream);
			_store.DeleteStream(name, ExpectedVersion.NoStream);

			Assert.Throws<ArgumentOutOfRangeException>(
				() => _store.DeleteStream(name, ExpectedVersion.StreamExists)
				);

		}

		[Fact]
		public void can_read_stream_forward() {




			//before the beginning 
			var startFrom = -3;
			var count = 4;
			Assert.Throws<ArgumentOutOfRangeException>(() => _store.ReadStreamForward(
																	_streamName,
																	startFrom,
																	count));

			//from the beginning
			startFrom = StreamPosition.Start;  // Start == 0
			var slice = _store.ReadStreamForward(
				_streamName,
				startFrom,
				count);

			Assert.True(count == slice.Events.Length, "Failed to read events forward");
			Assert.Equal(startFrom, slice.FromEventNumber);
			Assert.Equal(_lastEvent, slice.LastEventNumber);
			Assert.Equal(startFrom + count, slice.NextEventNumber);
			Assert.False(slice.IsEndOfStream);
			Assert.Equal(ReadDirection.Forward, slice.ReadDirection);
			Assert.True(string.CompareOrdinal(_streamName, slice.Stream) == 0);
			var j = startFrom;
			for (long i = 0; i < count; i++) {
				var evt = (ReadTestTestEvent)_serializer.Deserialize(slice.Events[i]);
				Assert.True(j == evt.MessageNumber, $"Expected {j} got {evt.MessageNumber}");
				j++;
			}

			//from the middle
			startFrom = 3;
			slice = _store.ReadStreamForward(
				_streamName,
				startFrom,
				count);

			Assert.True(count == slice.Events.Length, "Failed to read events forward");
			Assert.Equal(startFrom, slice.FromEventNumber);
			Assert.Equal(_lastEvent, slice.LastEventNumber);
			Assert.Equal(startFrom + count, slice.NextEventNumber);
			Assert.False(slice.IsEndOfStream);
			Assert.Equal(ReadDirection.Forward, slice.ReadDirection);
			Assert.True(string.CompareOrdinal(_streamName, slice.Stream) == 0);
			j = startFrom;
			for (long i = 0; i < count; i++) {
				var evt = (ReadTestTestEvent)_serializer.Deserialize(slice.Events[i]);
				Assert.True(j == evt.MessageNumber, $"Expected {j} got {evt.MessageNumber}");
				j++;
			}
			//to the end
			startFrom = 6;
			slice = _store.ReadStreamForward(
				_streamName,
				startFrom,
				count);

			Assert.True(count == slice.Events.Length, "Failed to read events forward");
			Assert.Equal(startFrom, slice.FromEventNumber);
			Assert.Equal(_lastEvent, slice.LastEventNumber);
			Assert.Equal(startFrom + count, slice.NextEventNumber);
			Assert.True(slice.IsEndOfStream);
			Assert.Equal(ReadDirection.Forward, slice.ReadDirection);
			Assert.True(string.CompareOrdinal(_streamName, slice.Stream) == 0);
			j = startFrom;
			for (long i = 0; i < count; i++) {
				var evt = (ReadTestTestEvent)_serializer.Deserialize(slice.Events[i]);
				Assert.True(j == evt.MessageNumber, $"Expected {j} got {evt.MessageNumber}");
				j++;
			}
			//read past the end
			startFrom = 8;
			slice = _store.ReadStreamForward(
				_streamName,
				startFrom,
				count);
			var expectedCount = 2;
			Assert.True(expectedCount == slice.Events.Length, "Failed to read events forward");
			Assert.Equal(startFrom, slice.FromEventNumber);
			Assert.Equal(_lastEvent, slice.LastEventNumber);
			Assert.Equal(_lastEvent + 1, slice.NextEventNumber);
			Assert.True(slice.IsEndOfStream);
			Assert.Equal(ReadDirection.Forward, slice.ReadDirection);
			Assert.True(string.CompareOrdinal(_streamName, slice.Stream) == 0);
			j = startFrom;
			for (long i = 0; i < expectedCount; i++) {
				var evt = (ReadTestTestEvent)_serializer.Deserialize(slice.Events[i]);
				Assert.True(j == evt.MessageNumber, $"Expected {j} got {evt.MessageNumber}");
				j++;
			}

			//start past the end
			startFrom = 12;
			slice = _store.ReadStreamForward(
				_streamName,
				startFrom,
				count);
			expectedCount = 0;
			Assert.True(expectedCount == slice.Events.Length, "Failed to read events forward");
			Assert.Equal(startFrom, slice.FromEventNumber);
			Assert.Equal(_lastEvent, slice.LastEventNumber);
			Assert.Equal(_lastEvent + 1, slice.NextEventNumber);
			Assert.True(slice.IsEndOfStream);
			Assert.Equal(ReadDirection.Forward, slice.ReadDirection);
			Assert.True(string.CompareOrdinal(_streamName, slice.Stream) == 0);

		}

		[Fact]
		public void can_read_stream_backward() {

			//before the beginning 
			var startFrom = -3;
			var count = 4;
			Assert.Throws<ArgumentOutOfRangeException>(() => _store.ReadStreamBackward(
																	_streamName,
																	startFrom,
																	count));
			//from start past beginning 
			startFrom = StreamPosition.Start;  // Start == 0
			var slice = _store.ReadStreamBackward(
				_streamName,
				startFrom,
				count);
			var expectedCount = 1;
			Assert.True(expectedCount == slice.Events.Length, "Failed to read events forward");
			Assert.Equal(startFrom, slice.FromEventNumber);
			Assert.Equal(_lastEvent, slice.LastEventNumber);
			Assert.Equal(StreamPosition.End, slice.NextEventNumber);
			Assert.True(slice.IsEndOfStream);
			Assert.Equal(ReadDirection.Backward, slice.ReadDirection);
			Assert.True(string.CompareOrdinal(_streamName, slice.Stream) == 0);
			var j = startFrom;
			for (long i = 0; i < expectedCount; i++) {
				var evt = (ReadTestTestEvent)_serializer.Deserialize(slice.Events[i]);
				Assert.True(j == evt.MessageNumber, $"Expected {j} got {evt.MessageNumber}");
				j--;
			}


			//from the middle to the beginning
			startFrom = 4;
			slice = _store.ReadStreamBackward(
				_streamName,
				startFrom,
				count);

			Assert.True(count == slice.Events.Length, "Failed to read events forward");
			Assert.Equal(startFrom, slice.FromEventNumber);
			Assert.Equal(_lastEvent, slice.LastEventNumber);
			Assert.Equal(StreamPosition.Start, slice.NextEventNumber);
			Assert.False(slice.IsEndOfStream);
			Assert.Equal(ReadDirection.Backward, slice.ReadDirection);
			Assert.True(string.CompareOrdinal(_streamName, slice.Stream) == 0);
			j = startFrom;
			for (long i = 0; i < count; i++) {
				var evt = (ReadTestTestEvent)_serializer.Deserialize(slice.Events[i]);
				Assert.True(j == evt.MessageNumber, $"Expected {j} got {evt.MessageNumber}");
				j--;
			}

			//from the middle
			startFrom = 6;
			slice = _store.ReadStreamBackward(
				_streamName,
				startFrom,
				count);

			Assert.True(count == slice.Events.Length, "Failed to read events forward");
			Assert.Equal(startFrom, slice.FromEventNumber);
			Assert.Equal(_lastEvent, slice.LastEventNumber);
			Assert.Equal(startFrom - count, slice.NextEventNumber);
			Assert.False(slice.IsEndOfStream);
			Assert.Equal(ReadDirection.Backward, slice.ReadDirection);
			Assert.True(string.CompareOrdinal(_streamName, slice.Stream) == 0);
			j = startFrom;
			for (long i = 0; i < count; i++) {
				var evt = (ReadTestTestEvent)_serializer.Deserialize(slice.Events[i]);
				Assert.True(j == evt.MessageNumber, $"Expected {j} got {evt.MessageNumber}");
				j--;
			}
			//start past the end and into events
			startFrom = 11;
			slice = _store.ReadStreamBackward(
				_streamName,
				startFrom,
				count);
			expectedCount = 2;
			Assert.True(expectedCount == slice.Events.Length, "Failed to read events backward");
			Assert.Equal(startFrom, slice.FromEventNumber);
			Assert.Equal(_lastEvent, slice.LastEventNumber);
			Assert.Equal(startFrom - count, slice.NextEventNumber);
			Assert.False(slice.IsEndOfStream);
			Assert.Equal(ReadDirection.Backward, slice.ReadDirection);
			Assert.True(string.CompareOrdinal(_streamName, slice.Stream) == 0);
			j = _lastEvent;
			for (long i = 0; i < expectedCount; i++) {
				var evt = (ReadTestTestEvent)_serializer.Deserialize(slice.Events[i]);
				Assert.True(j == evt.MessageNumber, $"Expected {j} got {evt.MessageNumber}");
				j--;
			}

			//start past the end beyond the events
			startFrom = 16;
			slice = _store.ReadStreamBackward(
				_streamName,
				startFrom,
				count);
			expectedCount = 0;
			Assert.True(expectedCount == slice.Events.Length, "Failed to read events backward");
			Assert.Equal(startFrom, slice.FromEventNumber);
			Assert.Equal(_lastEvent, slice.LastEventNumber);
			Assert.Equal(_lastEvent, slice.NextEventNumber);
			Assert.False(slice.IsEndOfStream);
			Assert.Equal(ReadDirection.Backward, slice.ReadDirection);
			Assert.True(string.CompareOrdinal(_streamName, slice.Stream) == 0);

		}
		public class ReadTestTestEvent : IMessage {
			public Guid MsgId { get; private set; }
			public readonly int MessageNumber;
			public ReadTestTestEvent(
				int messageNumber) {
				MsgId = Guid.NewGuid();
				MessageNumber = messageNumber;
			}
		}
	}

}
