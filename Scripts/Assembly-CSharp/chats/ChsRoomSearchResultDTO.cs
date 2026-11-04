using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsRoomSearchResultDTO : IMessage<ChsRoomSearchResultDTO>, IMessage, IEquatable<ChsRoomSearchResultDTO>, IDeepCloneable<ChsRoomSearchResultDTO>
	{
		private static readonly MessageParser<ChsRoomSearchResultDTO> _parser = new MessageParser<ChsRoomSearchResultDTO>(() => new ChsRoomSearchResultDTO());

		private UnknownFieldSet _unknownFields;

		public const int RoomsFieldNumber = 1;

		private static readonly FieldCodec<ChsRoomInfoDTO> _repeated_rooms_codec = FieldCodec.ForMessage(10u, ChsRoomInfoDTO.Parser);

		private readonly RepeatedField<ChsRoomInfoDTO> rooms_ = new RepeatedField<ChsRoomInfoDTO>();

		[DebuggerNonUserCode]
		public static MessageParser<ChsRoomSearchResultDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsRoomSearchResultDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public RepeatedField<ChsRoomInfoDTO> Rooms => rooms_;

		[DebuggerNonUserCode]
		public ChsRoomSearchResultDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsRoomSearchResultDTO(ChsRoomSearchResultDTO other)
			: this()
		{
			rooms_ = other.rooms_.Clone();
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsRoomSearchResultDTO Clone()
		{
			return new ChsRoomSearchResultDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsRoomSearchResultDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsRoomSearchResultDTO other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			if (!rooms_.Equals(other.rooms_))
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			num ^= rooms_.GetHashCode();
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
			rooms_.WriteTo(output, _repeated_rooms_codec);
			if (_unknownFields != null)
			{
				_unknownFields.WriteTo(output);
			}
		}

		[DebuggerNonUserCode]
		public int CalculateSize()
		{
			int num = 0;
			num += rooms_.CalculateSize(_repeated_rooms_codec);
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsRoomSearchResultDTO other)
		{
			if (other != null)
			{
				rooms_.Add(other.rooms_);
				_unknownFields = UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
			}
		}

		[DebuggerNonUserCode]
		public void MergeFrom(CodedInputStream input)
		{
			uint num;
			while ((num = input.ReadTag()) != 0)
			{
				if (num != 10)
				{
					_unknownFields = UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
				}
				else
				{
					rooms_.AddEntriesFrom(input, _repeated_rooms_codec);
				}
			}
		}
	}
}
