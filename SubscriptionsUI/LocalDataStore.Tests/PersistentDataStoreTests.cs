using ReactiveDomain;
using Xunit.Abstractions;

namespace PowerModels.Persistence.Tests {
	public class PersistentDataStoreTests : IDisposable {
		private readonly ITestOutputHelper _output;
		private readonly string _path;
		private readonly DataStore _store;

		public PersistentDataStoreTests(ITestOutputHelper output) {
			_output = output;
			_path = Path.GetTempFileName();
			_store = new DataStore("persistent", _path);
		}

		[Fact]
		public void CanAppendData() {
			_store.Connect();
			var result = _store.AppendToStream("Foo", ExpectedVersion.NoStream, null,
				new EventData(
					Guid.NewGuid(),
					"Bar",
					false,
					new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 },
					Array.Empty<byte>()
				));
			Assert.Equal(0, result.NextExpectedVersion);
		}

		[Fact]
		public void CanReadAppendedData() {
			_store.Connect();
			var id = Guid.NewGuid();
			var type = "Bar";
			var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
			var result = _store.AppendToStream("Foo", ExpectedVersion.NoStream, null,
				new EventData(
					id,
					type,
					false,
					data,
					Array.Empty<byte>()
				));

			var read = _store.ReadStreamForward("Foo", 0, 100);
			Assert.Single(read.Events);
			Assert.Equal(id, read.Events[0].EventId);
			Assert.Equal(type, read.Events[0].EventType);
			Assert.Equal(data, read.Events[0].Data);
		}

		[Fact]
		public void CanPersistDataBetweenInstances() {
			_store.Connect();
			var id = Guid.NewGuid();
			var type = "Bar";
			var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
			var result = _store.AppendToStream("Foo", ExpectedVersion.NoStream, null,
				new EventData(
					id,
					type,
					false,
					data,
					Array.Empty<byte>()
				));

			_store.Dispose();
			using (var secondStore = new DataStore("second", _path)) {
				secondStore.Connect();
				var read = secondStore.ReadStreamForward("Foo", 0, 100);
				Assert.Single(read.Events);
				Assert.Equal(id, read.Events[0].EventId);
				Assert.Equal(type, read.Events[0].EventType);
				Assert.Equal(data, read.Events[0].Data);
			}
		}

		[Fact]
		public void CanPersistLargeAmountsOfData() {
			_store.Connect();
			var data = new EventData[50000];
			for (int i = 0; i < 50000; i++) {
				data[i] = new EventData(
					Guid.NewGuid(),
					"Bar",
					false,
					new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 },
					Array.Empty<byte>());
			}
			var result = _store.AppendToStream("Foo", ExpectedVersion.NoStream, null,
				data
				);
			Assert.Equal(49999, result.NextExpectedVersion);
		}


		public void Dispose() {
			_store.Dispose();
			for (int i = 0; i < 5; i++) {
				try {
					if (File.Exists(_path)) {
						File.Delete(_path);
					}
					return;
				} catch (Exception ex) {
					if (i == 4) {
						_output.WriteLine($"Error cleaning up: {ex}");
					}
					Thread.Sleep(100);
				}
			}
		}
	}
}
