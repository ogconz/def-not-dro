using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsCreateRoomInfoDTO : IMessage<ChsCreateRoomInfoDTO>, IMessage, IEquatable<ChsCreateRoomInfoDTO>, IDeepCloneable<ChsCreateRoomInfoDTO>
	{
		private static readonly MessageParser<ChsCreateRoomInfoDTO> _parser = new MessageParser<ChsCreateRoomInfoDTO>(() => new ChsCreateRoomInfoDTO());

		private UnknownFieldSet _unknownFields;

		public const int CredentialsFieldNumber = 1;

		private RtuAuthenticationCredentialsDTO credentials_;

		public const int NameFieldNumber = 2;

		private string name_ = "";

		public const int PublicAttributesFieldNumber = 3;

		private static readonly FieldCodec<ChsKeyValueDTO> _repeated_publicAttributes_codec = FieldCodec.ForMessage(26u, ChsKeyValueDTO.Parser);

		private readonly RepeatedField<ChsKeyValueDTO> publicAttributes_ = new RepeatedField<ChsKeyValueDTO>();

		public const int PrivateAttributesFieldNumber = 4;

		private static readonly FieldCodec<ChsKeyValueDTO> _repeated_privateAttributes_codec = FieldCodec.ForMessage(34u, ChsKeyValueDTO.Parser);

		private readonly RepeatedField<ChsKeyValueDTO> privateAttributes_ = new RepeatedField<ChsKeyValueDTO>();

		public const int ContentPropertiesFieldNumber = 5;

		private static readonly FieldCodec<ChsKeyValueDTO> _repeated_contentProperties_codec = FieldCodec.ForMessage(42u, ChsKeyValueDTO.Parser);

		private readonly RepeatedField<ChsKeyValueDTO> contentProperties_ = new RepeatedField<ChsKeyValueDTO>();

		[DebuggerNonUserCode]
		public static MessageParser<ChsCreateRoomInfoDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsCreateRoomInfoDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public RtuAuthenticationCredentialsDTO Credentials
		{
			get
			{
				return credentials_;
			}
			set
			{
				credentials_ = value;
			}
		}

		[DebuggerNonUserCode]
		public string Name
		{
			get
			{
				return name_;
			}
			set
			{
				name_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public RepeatedField<ChsKeyValueDTO> PublicAttributes => publicAttributes_;

		[DebuggerNonUserCode]
		public RepeatedField<ChsKeyValueDTO> PrivateAttributes => privateAttributes_;

		[DebuggerNonUserCode]
		public RepeatedField<ChsKeyValueDTO> ContentProperties => contentProperties_;

		[DebuggerNonUserCode]
		public ChsCreateRoomInfoDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsCreateRoomInfoDTO(ChsCreateRoomInfoDTO other)
			: this()
		{
			credentials_ = ((other.credentials_ != null) ? other.credentials_.Clone() : null);
			name_ = other.name_;
			publicAttributes_ = other.publicAttributes_.Clone();
			privateAttributes_ = other.privateAttributes_.Clone();
			contentProperties_ = other.contentProperties_.Clone();
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsCreateRoomInfoDTO Clone()
		{
			return new ChsCreateRoomInfoDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsCreateRoomInfoDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsCreateRoomInfoDTO other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			if (!object.Equals(Credentials, other.Credentials))
			{
				return false;
			}
			if (Name != other.Name)
			{
				return false;
			}
			if (!publicAttributes_.Equals(other.publicAttributes_))
			{
				return false;
			}
			if (!privateAttributes_.Equals(other.privateAttributes_))
			{
				return false;
			}
			if (!contentProperties_.Equals(other.contentProperties_))
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			if (credentials_ != null)
			{
				num ^= Credentials.GetHashCode();
			}
			if (Name.Length != 0)
			{
				num ^= Name.GetHashCode();
			}
			num ^= publicAttributes_.GetHashCode();
			num ^= privateAttributes_.GetHashCode();
			num ^= contentProperties_.GetHashCode();
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
			if (credentials_ != null)
			{
				output.WriteRawTag(10);
				output.WriteMessage(Credentials);
			}
			if (Name.Length != 0)
			{
				output.WriteRawTag(18);
				output.WriteString(Name);
			}
			publicAttributes_.WriteTo(output, _repeated_publicAttributes_codec);
			privateAttributes_.WriteTo(output, _repeated_privateAttributes_codec);
			contentProperties_.WriteTo(output, _repeated_contentProperties_codec);
			if (_unknownFields != null)
			{
				_unknownFields.WriteTo(output);
			}
		}

		[DebuggerNonUserCode]
		public int CalculateSize()
		{
			int num = 0;
			if (credentials_ != null)
			{
				num += 1 + CodedOutputStream.ComputeMessageSize(Credentials);
			}
			if (Name.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(Name);
			}
			num += publicAttributes_.CalculateSize(_repeated_publicAttributes_codec);
			num += privateAttributes_.CalculateSize(_repeated_privateAttributes_codec);
			num += contentProperties_.CalculateSize(_repeated_contentProperties_codec);
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsCreateRoomInfoDTO other)
		{
			if (other == null)
			{
				return;
			}
			if (other.credentials_ != null)
			{
				if (credentials_ == null)
				{
					credentials_ = new RtuAuthenticationCredentialsDTO();
				}
				Credentials.MergeFrom(other.Credentials);
			}
			if (other.Name.Length != 0)
			{
				Name = other.Name;
			}
			publicAttributes_.Add(other.publicAttributes_);
			privateAttributes_.Add(other.privateAttributes_);
			contentProperties_.Add(other.contentProperties_);
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
				case 10u:
					if (credentials_ == null)
					{
						credentials_ = new RtuAuthenticationCredentialsDTO();
					}
					input.ReadMessage(credentials_);
					break;
				case 18u:
					Name = input.ReadString();
					break;
				case 26u:
					publicAttributes_.AddEntriesFrom(input, _repeated_publicAttributes_codec);
					break;
				case 34u:
					privateAttributes_.AddEntriesFrom(input, _repeated_privateAttributes_codec);
					break;
				case 42u:
					contentProperties_.AddEntriesFrom(input, _repeated_contentProperties_codec);
					break;
				}
			}
		}
	}
}
