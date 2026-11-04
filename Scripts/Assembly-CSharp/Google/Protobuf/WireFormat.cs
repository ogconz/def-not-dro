namespace Google.Protobuf
{
	public static class WireFormat
	{
		public enum WireType : uint
		{
			Varint = 0u,
			Fixed64 = 1u,
			LengthDelimited = 2u,
			StartGroup = 3u,
			EndGroup = 4u,
			Fixed32 = 5u
		}

		private const int TagTypeBits = 3;

		private const uint TagTypeMask = 7u;

		public static WireType GetTagWireType(uint tag)
		{
			return (WireType)(tag & 7);
		}

		public static int GetTagFieldNumber(uint tag)
		{
			return (int)tag >> 3;
		}

		public static uint MakeTag(int fieldNumber, WireType wireType)
		{
			return (uint)(fieldNumber << 3) | (uint)wireType;
		}
	}
}
