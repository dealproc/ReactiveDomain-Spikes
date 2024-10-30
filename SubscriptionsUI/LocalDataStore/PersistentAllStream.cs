using System.Collections;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ReactiveDomain;

namespace PowerModels.Persistence {
	public class PersistentAllStream : IStream, IDisposable {
		private readonly FileStream _fs;
		private readonly SinglyLinkedList _allStream;
		public PersistentAllStream(string filePath) {
			_fs = File.Open(filePath, FileMode.OpenOrCreate);
			_allStream = new SinglyLinkedList();
			while (_fs.Position < _fs.Length) {
				var read = StorageMessage.Parser.ParseDelimitedFrom(_fs);
				if (read.TypeCase == StorageMessage.TypeOneofCase.ProjectedEvent) {
					_allStream.Add(new ProjectedEvent(
						read.ProjectedEvent.ProjectedStream,
						read.ProjectedEvent.OriginalEventNumber,
						read.StreamId,
						read.EventId,
						read.EventNumber,
						read.EventType,
						read.Data.ToByteArray(),
						read.Metadata.ToByteArray(),
						read.IsJson,
						read.Created.ToDateTime(),
						read.CreatedEpoch
					));
				} else {
					_allStream.Add(new RecordedEvent(
						read.StreamId,
						read.EventId,
						read.EventNumber,
						read.EventType,
						read.Data.ToByteArray(),
						read.Metadata.ToByteArray(),
						read.IsJson,
						read.Created.ToDateTime(),
						read.CreatedEpoch));
				}
			}
		}

		public RecordedEvent Add(RecordedEvent e) {
			lock (_fs) {
				var sm = new StorageMessage() {
					StreamId = e.EventStreamId,
					EventId = e.EventId,
					EventNumber = e.EventNumber,
					EventType = e.EventType,
					IsJson = e.IsJson,
					Data = ByteString.CopyFrom(e.Data),
					Metadata = ByteString.CopyFrom(e.Metadata),
					CreatedEpoch = _fs.Position,
					Created = Timestamp.FromDateTime(e.Created)
				};
				if (e is ProjectedEvent pe) {
					sm.ProjectedEvent = new ProjectionInfo() {
						ProjectedStream = pe.ProjectedStream,
						OriginalEventNumber = pe.OriginalEventNumber,
					};
				}

				sm.WriteDelimitedTo(_fs);
				_fs.Flush();

				RecordedEvent result = e switch {
					ProjectedEvent p => new ProjectedEvent(
						p.ProjectedStream, p.OriginalEventNumber,
						p.EventStreamId, p.EventId, p.EventNumber,
						p.EventType, p.Data, p.Metadata,
						p.IsJson, p.Created, p.CreatedEpoch),
					_ => new RecordedEvent(
						e.EventStreamId, e.EventId, e.EventNumber,
						e.EventType, e.Data, e.Metadata,
						e.IsJson, e.Created, e.CreatedEpoch)
				};
				_allStream.Add(result);
				return result;
			}
		}

		public int Count => _allStream.Count;

		public RecordedEvent this[int index] => _allStream[index];
		public IEnumerator<RecordedEvent> GetEnumerator() {
			return _allStream.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return ((IEnumerable)_allStream).GetEnumerator();
		}

		public void Dispose() {
			_fs.Dispose();
		}
	}
}
