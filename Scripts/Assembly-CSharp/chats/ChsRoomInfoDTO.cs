using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsRoomInfoDTO : IMessage<ChsRoomInfoDTO>, IMessage, IEquatable<ChsRoomInfoDTO>, IDeepCloneable<ChsRoomInfoDTO>
	{
		private static readonly MessageParser<ChsRoomInfoDTO> _parser = new MessageParser<ChsRoomInfoDTO>(() => new ChsRoomInfoDTO());

		private UnknownFieldSet _unknownFields;

		public const int RoomIdFieldNumber = 1;

		private ByteString roomId_ = ByteString.Empty;

		public const int PublicAttributesFieldNumber = 2;

		private static readonly FieldCodec<ChsKeyValueDTO> _repeated_publicAttributes_codec = FieldCodec.ForMessage(18u, ChsKeyValueDTO.Parser);

		private readonly RepeatedField<ChsKeyValueDTO> publicAttributes_ = new RepeatedField<ChsKeyValueDTO>();

		[DebuggerNonUserCode]
		public static MessageParser<ChsRoomInfoDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsRoomInfoDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public ByteString RoomId
		{
			get
			{
				return roomId_;
			}
			set
			{
				roomId_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public RepeatedField<ChsKeyValueDTO> PublicAttributes => publicAttributes_;

		[DebuggerNonUserCode]
		public ChsRoomInfoDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsRoomInfoDTO(ChsRoomInfoDTO other)
			: this()
		{
			roomId_ = other.roomId_;
			publicAttributes_ = other.publicAttributes_.Clone();
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsRoomInfoDTO Clone()
		{
			return new ChsRoomInfoDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsRoomInfoDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsRoomInfoDTO other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			if (RoomId != other.RoomId)
			{
				return false;
			}
			if (!publicAttributes_.Equals(other.publicAttributes_))
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			if (RoomId.Length != 0)
			{
				num ^= RoomId.GetHashCode();
			}
			num ^= publicAttributes_.GetHashCode();
			if (_unknownFields != null)
			{
				num ^= _unknownFields.GetHashCode();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public override string ToString()
		{
			return JsonFormatter.ToDiagnosticString(this);
		}

		[DebuggerNonUserCode]
		public void WriteTo(CodedOutputStream output)
		{
			if (RoomId.Length != 0)
			{
				output.WriteRawTag(10);
				output.WriteBytes(RoomId);
			}
			publicAttributes_.WriteTo(output, _repeated_publicAttributes_codec);
			if (_unknownFields != null)
			{
				_unknownFields.WriteTo(output);
			}
		}

		[DebuggerNonUserCode]
		public int CalculateSize()
		{
			int num = 0;
			if (RoomId.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeBytesSize(RoomId);
			}
			num += publicAttributes_.CalculateSize(_repeated_publicAttributes_codec);
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsRoomInfoDTO other)
		{
			if (other != null)
			{
				if (other.RoomId.Length != 0)
				{
					RoomId = other.RoomId;
				}
				publicAttributes_.Add(other.publicAttributes_);
				_unknownFields = UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
			}
		}

		[DebuggerNonUserCode]
		public void MergeFrom(CodedInputStream input)
		{
			uint num;
			while ((num = input.ReadTag()) != 0)
			{
				switch (num)
				{
				default:
					_unknownFields = UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
					break;
				case 10u:
					RoomId = input.ReadBytes();
					break;
				case 18u:
					publicAttributes_.AddEntriesFrom(input, _repeated_publicAttributes_codec);
					break;
				}
			}
		}
	}
}
