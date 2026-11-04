using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsRoomHistoryItemDTO : IMessage<ChsRoomHistoryItemDTO>, IMessage, IEquatable<ChsRoomHistoryItemDTO>, IDeepCloneable<ChsRoomHistoryItemDTO>
	{
		private static readonly MessageParser<ChsRoomHistoryItemDTO> _parser = new MessageParser<ChsRoomHistoryItemDTO>(() => new ChsRoomHistoryItemDTO());

		private UnknownFieldSet _unknownFields;

		public const int IdFieldNumber = 1;

		private long id_;

		public const int ContentFieldNumber = 2;

		private ChsContentItemDTO content_;

		public const int TypeFieldNumber = 3;

		private ChsOutcomingMessageTypes type_;

		[DebuggerNonUserCode]
		public static MessageParser<ChsRoomHistoryItemDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsRoomHistoryItemDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public long Id
		{
			get
			{
				return id_;
			}
			set
			{
				id_ = value;
			}
		}

		[DebuggerNonUserCode]
		public ChsContentItemDTO Content
		{
			get
			{
				return content_;
			}
			set
			{
				content_ = value;
			}
		}

		[DebuggerNonUserCode]
		public ChsOutcomingMessageTypes Type
		{
			get
			{
				return type_;
			}
			set
			{
				type_ = value;
			}
		}

		[DebuggerNonUserCode]
		public ChsRoomHistoryItemDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsRoomHistoryItemDTO(ChsRoomHistoryItemDTO other)
			: this()
		{
			id_ = other.id_;
			content_ = ((other.content_ != null) ? other.content_.Clone() : null);
			type_ = other.type_;
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsRoomHistoryItemDTO Clone()
		{
			return new ChsRoomHistoryItemDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsRoomHistoryItemDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsRoomHistoryItemDTO other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			if (Id != other.Id)
			{
				return false;
			}
			if (!object.Equals(Content, other.Content))
			{
				return false;
			}
			if (Type != other.Type)
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			if (Id != 0L)
			{
				num ^= Id.GetHashCode();
			}
			if (content_ != null)
			{
				num ^= Content.GetHashCode();
			}
			if (Type != ChsOutcomingMessageTypes.UnknownOutcomingType)
			{
				num ^= Type.GetHashCode();
			}
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
			if (Id != 0L)
			{
				output.WriteRawTag(8);
				output.WriteInt64(Id);
			}
			if (content_ != null)
			{
				output.WriteRawTag(18);
				output.WriteMessage(Content);
			}
			if (Type != ChsOutcomingMessageTypes.UnknownOutcomingType)
			{
				output.WriteRawTag(24);
				output.WriteEnum((int)Type);
			}
			if (_unknownFields != null)
			{
				_unknownFields.WriteTo(output);
			}
		}

		[DebuggerNonUserCode]
		public int CalculateSize()
		{
			int num = 0;
			if (Id != 0L)
			{
				num += 1 + CodedOutputStream.ComputeInt64Size(Id);
			}
			if (content_ != null)
			{
				num += 1 + CodedOutputStream.ComputeMessageSize(Content);
			}
			if (Type != ChsOutcomingMessageTypes.UnknownOutcomingType)
			{
				num += 1 + CodedOutputStream.ComputeEnumSize((int)Type);
			}
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsRoomHistoryItemDTO other)
		{
			if (other == null)
			{
				return;
			}
			if (other.Id != 0L)
			{
				Id = other.Id;
			}
			if (other.content_ != null)
			{
				if (content_ == null)
				{
					content_ = new ChsContentItemDTO();
				}
				Content.MergeFrom(other.Content);
			}
			if (other.Type != ChsOutcomingMessageTypes.UnknownOutcomingType)
			{
				Type = other.Type;
			}
			_unknownFields = UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
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
				case 8u:
					Id = input.ReadInt64();
					break;
				case 18u:
					if (content_ == null)
					{
						content_ = new ChsContentItemDTO();
					}
					input.ReadMessage(content_);
					break;
				case 24u:
					type_ = (ChsOutcomingMessageTypes)input.ReadEnum();
					break;
				}
			}
		}
	}
}
