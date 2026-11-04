using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsOutcomingMessageDTO : IMessage<ChsOutcomingMessageDTO>, IMessage, IEquatable<ChsOutcomingMessageDTO>, IDeepCloneable<ChsOutcomingMessageDTO>
	{
		private static readonly MessageParser<ChsOutcomingMessageDTO> _parser = new MessageParser<ChsOutcomingMessageDTO>(() => new ChsOutcomingMessageDTO());

		private UnknownFieldSet _unknownFields;

		public const int IdFieldNumber = 1;

		private long id_;

		public const int ContentFieldNumber = 2;

		private ByteString content_ = ByteString.Empty;

		public const int TypeFieldNumber = 3;

		private ChsOutcomingMessageTypes type_;

		public const int MessageTokenFieldNumber = 4;

		private ByteString messageToken_ = ByteString.Empty;

		public const int TokenFieldNumber = 5;

		private ByteString token_ = ByteString.Empty;

		[DebuggerNonUserCode]
		public static MessageParser<ChsOutcomingMessageDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsOutcomingMessageDTOReflection.Descriptor.MessageTypes[0];

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
		public ByteString Content
		{
			get
			{
				return content_;
			}
			set
			{
				content_ = ProtoPreconditions.CheckNotNull(value, "value");
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
		public ByteString MessageToken
		{
			get
			{
				return messageToken_;
			}
			set
			{
				messageToken_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public ByteString Token
		{
			get
			{
				return token_;
			}
			set
			{
				token_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public ChsOutcomingMessageDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsOutcomingMessageDTO(ChsOutcomingMessageDTO other)
			: this()
		{
			id_ = other.id_;
			content_ = other.content_;
			type_ = other.type_;
			messageToken_ = other.messageToken_;
			token_ = other.token_;
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsOutcomingMessageDTO Clone()
		{
			return new ChsOutcomingMessageDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsOutcomingMessageDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsOutcomingMessageDTO other)
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
			if (Content != other.Content)
			{
				return false;
			}
			if (Type != other.Type)
			{
				return false;
			}
			if (MessageToken != other.MessageToken)
			{
				return false;
			}
			if (Token != other.Token)
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
			if (Content.Length != 0)
			{
				num ^= Content.GetHashCode();
			}
			if (Type != ChsOutcomingMessageTypes.UnknownOutcomingType)
			{
				num ^= Type.GetHashCode();
			}
			if (MessageToken.Length != 0)
			{
				num ^= MessageToken.GetHashCode();
			}
			if (Token.Length != 0)
			{
				num ^= Token.GetHashCode();
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
			if (Content.Length != 0)
			{
				output.WriteRawTag(18);
				output.WriteBytes(Content);
			}
			if (Type != ChsOutcomingMessageTypes.UnknownOutcomingType)
			{
				output.WriteRawTag(24);
				output.WriteEnum((int)Type);
			}
			if (MessageToken.Length != 0)
			{
				output.WriteRawTag(34);
				output.WriteBytes(MessageToken);
			}
			if (Token.Length != 0)
			{
				output.WriteRawTag(42);
				output.WriteBytes(Token);
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
			if (Content.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeBytesSize(Content);
			}
			if (Type != ChsOutcomingMessageTypes.UnknownOutcomingType)
			{
				num += 1 + CodedOutputStream.ComputeEnumSize((int)Type);
			}
			if (MessageToken.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeBytesSize(MessageToken);
			}
			if (Token.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeBytesSize(Token);
			}
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsOutcomingMessageDTO other)
		{
			if (other != null)
			{
				if (other.Id != 0L)
				{
					Id = other.Id;
				}
				if (other.Content.Length != 0)
				{
					Content = other.Content;
				}
				if (other.Type != ChsOutcomingMessageTypes.UnknownOutcomingType)
				{
					Type = other.Type;
				}
				if (other.MessageToken.Length != 0)
				{
					MessageToken = other.MessageToken;
				}
				if (other.Token.Length != 0)
				{
					Token = other.Token;
				}
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
				case 8u:
					Id = input.ReadInt64();
					break;
				case 18u:
					Content = input.ReadBytes();
					break;
				case 24u:
					type_ = (ChsOutcomingMessageTypes)input.ReadEnum();
					break;
				case 34u:
					MessageToken = input.ReadBytes();
					break;
				case 42u:
					Token = input.ReadBytes();
					break;
				}
			}
		}
	}
}
