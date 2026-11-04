using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsIncomingMessageDTO : IMessage<ChsIncomingMessageDTO>, IMessage, IEquatable<ChsIncomingMessageDTO>, IDeepCloneable<ChsIncomingMessageDTO>
	{
		private static readonly MessageParser<ChsIncomingMessageDTO> _parser = new MessageParser<ChsIncomingMessageDTO>(() => new ChsIncomingMessageDTO());

		private UnknownFieldSet _unknownFields;

		public const int TokenFieldNumber = 1;

		private ByteString token_ = ByteString.Empty;

		public const int MessageTokenFieldNumber = 2;

		private ByteString messageToken_ = ByteString.Empty;

		public const int PayloadFieldNumber = 3;

		private ByteString payload_ = ByteString.Empty;

		public const int TypeFieldNumber = 4;

		private ChsIncomingMessageTypes type_;

		public const int FrameHeightFieldNumber = 5;

		private int frameHeight_;

		[DebuggerNonUserCode]
		public static MessageParser<ChsIncomingMessageDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsIncomingMessageDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

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
		public ByteString Payload
		{
			get
			{
				return payload_;
			}
			set
			{
				payload_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public ChsIncomingMessageTypes Type
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
		public int FrameHeight
		{
			get
			{
				return frameHeight_;
			}
			set
			{
				frameHeight_ = value;
			}
		}

		[DebuggerNonUserCode]
		public ChsIncomingMessageDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsIncomingMessageDTO(ChsIncomingMessageDTO other)
			: this()
		{
			token_ = other.token_;
			messageToken_ = other.messageToken_;
			payload_ = other.payload_;
			type_ = other.type_;
			frameHeight_ = other.frameHeight_;
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsIncomingMessageDTO Clone()
		{
			return new ChsIncomingMessageDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsIncomingMessageDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsIncomingMessageDTO other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			if (Token != other.Token)
			{
				return false;
			}
			if (MessageToken != other.MessageToken)
			{
				return false;
			}
			if (Payload != other.Payload)
			{
				return false;
			}
			if (Type != other.Type)
			{
				return false;
			}
			if (FrameHeight != other.FrameHeight)
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			if (Token.Length != 0)
			{
				num ^= Token.GetHashCode();
			}
			if (MessageToken.Length != 0)
			{
				num ^= MessageToken.GetHashCode();
			}
			if (Payload.Length != 0)
			{
				num ^= Payload.GetHashCode();
			}
			if (Type != ChsIncomingMessageTypes.Unknown)
			{
				num ^= Type.GetHashCode();
			}
			if (FrameHeight != 0)
			{
				num ^= FrameHeight.GetHashCode();
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
			if (Token.Length != 0)
			{
				output.WriteRawTag(10);
				output.WriteBytes(Token);
			}
			if (MessageToken.Length != 0)
			{
				output.WriteRawTag(18);
				output.WriteBytes(MessageToken);
			}
			if (Payload.Length != 0)
			{
				output.WriteRawTag(26);
				output.WriteBytes(Payload);
			}
			if (Type != ChsIncomingMessageTypes.Unknown)
			{
				output.WriteRawTag(32);
				output.WriteEnum((int)Type);
			}
			if (FrameHeight != 0)
			{
				output.WriteRawTag(40);
				output.WriteInt32(FrameHeight);
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
			if (Token.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeBytesSize(Token);
			}
			if (MessageToken.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeBytesSize(MessageToken);
			}
			if (Payload.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeBytesSize(Payload);
			}
			if (Type != ChsIncomingMessageTypes.Unknown)
			{
				num += 1 + CodedOutputStream.ComputeEnumSize((int)Type);
			}
			if (FrameHeight != 0)
			{
				num += 1 + CodedOutputStream.ComputeInt32Size(FrameHeight);
			}
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsIncomingMessageDTO other)
		{
			if (other != null)
			{
				if (other.Token.Length != 0)
				{
					Token = other.Token;
				}
				if (other.MessageToken.Length != 0)
				{
					MessageToken = other.MessageToken;
				}
				if (other.Payload.Length != 0)
				{
					Payload = other.Payload;
				}
				if (other.Type != ChsIncomingMessageTypes.Unknown)
				{
					Type = other.Type;
				}
				if (other.FrameHeight != 0)
				{
					FrameHeight = other.FrameHeight;
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
				case 10u:
					Token = input.ReadBytes();
					break;
				case 18u:
					MessageToken = input.ReadBytes();
					break;
				case 26u:
					Payload = input.ReadBytes();
					break;
				case 32u:
					type_ = (ChsIncomingMessageTypes)input.ReadEnum();
					break;
				case 40u:
					FrameHeight = input.ReadInt32();
					break;
				}
			}
		}
	}
}
