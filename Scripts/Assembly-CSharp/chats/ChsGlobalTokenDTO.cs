using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsGlobalTokenDTO : IMessage<ChsGlobalTokenDTO>, IMessage, IEquatable<ChsGlobalTokenDTO>, IDeepCloneable<ChsGlobalTokenDTO>
	{
		private static readonly MessageParser<ChsGlobalTokenDTO> _parser = new MessageParser<ChsGlobalTokenDTO>(() => new ChsGlobalTokenDTO());

		private UnknownFieldSet _unknownFields;

		public const int TokenFieldNumber = 1;

		private string token_ = "";

		public const int AuthorityFieldNumber = 2;

		private string authority_ = "";

		[DebuggerNonUserCode]
		public static MessageParser<ChsGlobalTokenDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsGlobalTokenDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public string Token
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
		public string Authority
		{
			get
			{
				return authority_;
			}
			set
			{
				authority_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public ChsGlobalTokenDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsGlobalTokenDTO(ChsGlobalTokenDTO other)
			: this()
		{
			token_ = other.token_;
			authority_ = other.authority_;
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsGlobalTokenDTO Clone()
		{
			return new ChsGlobalTokenDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsGlobalTokenDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsGlobalTokenDTO other)
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
			if (Authority != other.Authority)
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
			if (Authority.Length != 0)
			{
				num ^= Authority.GetHashCode();
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
				output.WriteString(Token);
			}
			if (Authority.Length != 0)
			{
				output.WriteRawTag(18);
				output.WriteString(Authority);
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
				num += 1 + CodedOutputStream.ComputeStringSize(Token);
			}
			if (Authority.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(Authority);
			}
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsGlobalTokenDTO other)
		{
			if (other != null)
			{
				if (other.Token.Length != 0)
				{
					Token = other.Token;
				}
				if (other.Authority.Length != 0)
				{
					Authority = other.Authority;
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
					Token = input.ReadString();
					break;
				case 18u:
					Authority = input.ReadString();
					break;
				}
			}
		}
	}
}
