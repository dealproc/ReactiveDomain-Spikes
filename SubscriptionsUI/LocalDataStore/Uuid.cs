using System.Buffers.Binary;

namespace PowerModels.Persistence {

	public partial class Uuid {
		public static implicit operator Uuid(Guid id) => From(id);
		public static implicit operator Guid(Uuid id) => id.ToGuid();

		public static Uuid From(Guid id) {
			Span<byte> data = stackalloc byte[16];

			id.TryWriteBytes(data);

			data[..8].Reverse();
			data[..2].Reverse();
			data.Slice(2, 2).Reverse();
			data.Slice(4, 4).Reverse();
			data[8..].Reverse();
			return new Uuid {
				MostSignificantBits = BinaryPrimitives.ReadInt64LittleEndian(data),
				LeastSignificantBits = BinaryPrimitives.ReadInt64LittleEndian(data[8..])
			};
		}

		public Guid ToGuid() {
			Span<byte> data = stackalloc byte[16];
			BinaryPrimitives.WriteInt64LittleEndian(data, MostSignificantBits);
			BinaryPrimitives.WriteInt64LittleEndian(data[8..], LeastSignificantBits);
			data[..8].Reverse();
			data[..4].Reverse();
			data.Slice(4, 2).Reverse();
			data.Slice(6, 2).Reverse();
			data[8..].Reverse();

			return new Guid(data);
		}
	}
}
