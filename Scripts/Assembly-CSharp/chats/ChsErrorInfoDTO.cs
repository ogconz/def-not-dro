using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsErrorInfoDTO : IMessage<ChsErrorInfoDTO>, IMessage, IEquatable<ChsErrorInfoDTO>, IDeepCloneable<ChsErrorInfoDTO>
	{
		private static readonly MessageParser<ChsErrorInfoDTO> _parser = new MessageParser<ChsErrorInfoDTO>(() => new ChsErrorInfoDTO());

		private UnknownFieldSet _unknownFields;

		public const int ErrorTypeFieldNumber = 1;

		private ChsErrorTypes errorType_;

		public const int PayloadFieldNumber = 2;

		private ByteString payload_ = ByteString.Empty;

		public const int CodeFieldNumber = 3;

		private int code_;

		[DebuggerNonUserCode]
		public static MessageParser<ChsErrorInfoDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsErrorInfoDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public ChsErrorTypes ErrorType
		{
			get
			{
				return errorType_;
			}
			set
			{
				errorType_ = value;
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
		public int Code
		{
			get
			{
				return code_;
			}
			set
			{
				code_ = value;
			}
		}

		[DebuggerNonUserCode]
		public ChsErrorInfoDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsErrorInfoDTO(ChsErrorInfoDTO other)
			: this()
		{
			errorType_ = other.errorType_;
			payload_ = other.payload_;
			code_ = other.code_;
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsErrorInfoDTO Clone()
		{
			return new ChsErrorInfoDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsErrorInfoDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsErrorInfoDTO other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			if (ErrorType != other.ErrorType)
			{
				return false;
			}
			if (Payload != other.Payload)
			{
				return false;
			}
			if (Code != other.Code)
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			if (ErrorType != ChsErrorTypes.Unknown)
			{
				num ^= ErrorType.GetHashCode();
			}
			if (Payload.Length != 0)
			{
				num ^= Payload.GetHashCode();
			}
			if (Code != 0)
			{
				num ^= Code.GetHashCode();
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
			if (ErrorType != ChsErrorTypes.Unknown)
			{
				output.WriteRawTag(8);
				output.WriteEnum((int)ErrorType);
			}
			if (Payload.Length != 0)
			{
				output.WriteRawTag(18);
				output.WriteBytes(Payload);
			}
			if (Code != 0)
			{
				output.WriteRawTag(24);
				output.WriteInt32(Code);
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
			if (ErrorType != ChsErrorTypes.Unknown)
			{
				num += 1 + CodedOutputStream.ComputeEnumSize((int)ErrorType);
			}
			if (Payload.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeBytesSize(Payload);
			}
			if (Code != 0)
			{
				num += 1 + CodedOutputStream.ComputeInt32Size(Code);
			}
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsErrorInfoDTO other)
		{
			if (other != null)
			{
				if (other.ErrorType != ChsErrorTypes.Unknown)
				{
					ErrorType = other.ErrorType;
				}
				if (other.Payload.Length != 0)
				{
					Payload = other.Payload;
				}
				if (other.Code != 0)
				{
					Code = other.Code;
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
					errorType_ = (ChsErrorTypes)input.ReadEnum();
					break;
				case 18u:
					Payload = input.ReadBytes();
					break;
				case 24u:
					Code = input.ReadInt32();
					break;
				}
			}
		}
	}
}
