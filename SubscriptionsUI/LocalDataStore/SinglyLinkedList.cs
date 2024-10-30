using System.Collections;
using ReactiveDomain;

namespace PowerModels.Persistence {

	class SinglyLinkedList : IReadOnlyList<RecordedEvent> {
		private readonly Node _tail;
		Node _head;

		public SinglyLinkedList() {
			_head = _tail = new Node(0);
		}

		public void Add(RecordedEvent value) {
			if (_head.Index + 1 == _head.Values.Length) {
				_head.Next = new Node(_head.Number + 1);
				_head = _head.Next;
			}

			object? expected = _head.Values[_head.Index + 1];
			Interlocked.MemoryBarrier();
			while (expected != Interlocked.CompareExchange(ref _head.Values[_head.Index + 1], value,
					   expected)) {
				expected = _head.Values[_head.Index + 1];
			}
			Interlocked.Increment(ref _head.Index);
			if (expected is TaskCompletionSource notification) {
				notification.TrySetResult();
			}
		}

		class Node {
			public Node(int number) {
				Number = number;
				Values = new object?[128];
			}
			public readonly int Number;
			public readonly object?[] Values;
			public volatile int Index = -1;
			public Node? Next;

		}
		//TODO: there are thread safe subscription models where this came from too
		public IEnumerator<RecordedEvent> GetEnumerator() {
			return new Enumerator(_tail);
		}

		class Enumerator : IEnumerator<RecordedEvent> {
			private readonly Node _tail;
			private Node _current;
			private int _index;

			public Enumerator(Node tail) {
				_current = _tail = tail;
				_index = -1;
			}

			public bool MoveNext() {
				if (_index++ < _current.Index) {
					return true;
				}

				if (_index > 127 && _current.Next != null) {
					_current = _current.Next;
					_index = 0;
					return _index <= _current.Index;
				}

				return false;
			}

			public void Reset() {
				_current = _tail;
			}

			public RecordedEvent Current => _current.Values[_index] as RecordedEvent ?? throw new InvalidOperationException("Something went very wrong with threading");

			object? IEnumerator.Current => Current;

			public void Dispose() {
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public int Count => _head.Number * 128 + _head.Index + 1;

		public RecordedEvent this[int index] {
			get {
				var (offset, current) = SeekNodeForIndex(index);

				if (current.Values[offset] is RecordedEvent re)
					return re;
				throw new InvalidOperationException(
					"Something went very wrong with threading");

			}
		}

		private (int offset, Node current) SeekNodeForIndex(int index) {
			var nodes = index / 128;
			var offset = index % 128;
			var current = _tail;
			while (nodes > 0) {
				current = current.Next;
				if (current == null || current.Index < offset)
					throw new IndexOutOfRangeException();
				nodes--;
			}
			if (current == null || current.Index < offset)
				throw new IndexOutOfRangeException();
			return (offset, current);
		}
	}
}
