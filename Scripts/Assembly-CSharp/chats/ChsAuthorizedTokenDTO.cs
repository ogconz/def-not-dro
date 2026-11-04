using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsAuthorizedTokenDTO : IMessage<ChsAuthorizedTokenDTO>, IMessage, IEquatable<ChsAuthorizedTokenDTO>, IDeepCloneable<ChsAuthorizedTokenDTO>
	{
		private static readonly MessageParser<ChsAuthorizedTokenDTO> _parser = new MessageParser<ChsAuthorizedTokenDTO>(() => new ChsAuthorizedTokenDTO());

		private UnknownFieldSet _unknownFields;

		public const int TokenFieldNumber = 4;

		private ByteString token_ = ByteString.Empty;

		public const int GrantedPermissionsFieldNumber = 5;

		private static readonly FieldCodec<ChsUserPermissionsChat> _repeated_grantedPermissions_codec = FieldCodec.ForEnum(42u, (ChsUserPermissionsChat x) => (int)x, (int x) => (ChsUserPermissionsChat)x);

		private readonly RepeatedField<ChsUserPermissionsChat> grantedPermissions_ = new RepeatedField<ChsUserPermissionsChat>();

		public const int DeniedPermissionsFieldNumber = 6;

		private static readonly FieldCodec<ChsUserPermissionsChat> _repeated_deniedPermissions_codec = FieldCodec.ForEnum(50u, (ChsUserPermissionsChat x) => (int)x, (int x) => (ChsUserPermissionsChat)x);

		private readonly RepeatedField<ChsUserPermissionsChat> deniedPermissions_ = new RepeatedField<ChsUserPermissionsChat>();

		public const int AdditionalDataFieldNumber = 7;

		private string additionalData_ = "";

		public const int AdditionalDataCollectionsFieldNumber = 8;

		private static readonly FieldCodec<ChsUserDataDTO> _repeated_additionalDataCollections_codec = FieldCodec.ForMessage(66u, ChsUserDataDTO.Parser);

		private readonly RepeatedField<ChsUserDataDTO> additionalDataCollections_ = new RepeatedField<ChsUserDataDTO>();

		public const int UserIdFieldNumber = 9;

		private ByteString userId_ = ByteString.Empty;

		[DebuggerNonUserCode]
		public static MessageParser<ChsAuthorizedTokenDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsAuthorizedTokenDTOReflection.Descriptor.MessageTypes[0];

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
		public RepeatedField<ChsUserPermissionsChat> GrantedPermissions => grantedPermissions_;

		[DebuggerNonUserCode]
		public RepeatedField<ChsUserPermissionsChat> DeniedPermissions => deniedPermissions_;

		[DebuggerNonUserCode]
		public string AdditionalData
		{
			get
			{
				return additionalData_;
			}
			set
			{
				additionalData_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public RepeatedField<ChsUserDataDTO> AdditionalDataCollections => additionalDataCollections_;

		[DebuggerNonUserCode]
		public ByteString UserId
		{
			get
			{
				return userId_;
			}
			set
			{
				userId_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public ChsAuthorizedTokenDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsAuthorizedTokenDTO(ChsAuthorizedTokenDTO other)
			: this()
		{
			token_ = other.token_;
			grantedPermissions_ = other.grantedPermissions_.Clone();
			deniedPermissions_ = other.deniedPermissions_.Clone();
			additionalData_ = other.additionalData_;
			additionalDataCollections_ = other.additionalDataCollections_.Clone();
			userId_ = other.userId_;
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsAuthorizedTokenDTO Clone()
		{
			return new ChsAuthorizedTokenDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsAuthorizedTokenDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsAuthorizedTokenDTO other)
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
			if (!grantedPermissions_.Equals(other.grantedPermissions_))
			{
				return false;
			}
			if (!deniedPermissions_.Equals(other.deniedPermissions_))
			{
				return false;
			}
			if (AdditionalData != other.AdditionalData)
			{
				return false;
			}
			if (!additionalDataCollections_.Equals(other.additionalDataCollections_))
			{
				return false;
			}
			if (UserId != other.UserId)
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
			num ^= grantedPermissions_.GetHashCode();
			num ^= deniedPermissions_.GetHashCode();
			if (AdditionalData.Length != 0)
			{
				num ^= AdditionalData.GetHashCode();
			}
			num ^= additionalDataCollections_.GetHashCode();
			if (UserId.Length != 0)
			{
				num ^= UserId.GetHashCode();
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
				output.WriteRawTag(34);
				output.WriteBytes(Token);
			}
			grantedPermissions_.WriteTo(output, _repeated_grantedPermissions_codec);
			deniedPermissions_.WriteTo(output, _repeated_deniedPermissions_codec);
			if (AdditionalData.Length != 0)
			{
				output.WriteRawTag(58);
				output.WriteString(AdditionalData);
			}
			additionalDataCollections_.WriteTo(output, _repeated_additionalDataCollections_codec);
			if (UserId.Length != 0)
			{
				output.WriteRawTag(74);
				output.WriteBytes(UserId);
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
			num += grantedPermissions_.CalculateSize(_repeated_grantedPermissions_codec);
			num += deniedPermissions_.CalculateSize(_repeated_deniedPermissions_codec);
			if (AdditionalData.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(AdditionalData);
			}
			num += additionalDataCollections_.CalculateSize(_repeated_additionalDataCollections_codec);
			if (UserId.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeBytesSize(UserId);
			}
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsAuthorizedTokenDTO other)
		{
			if (other != null)
			{
				if (other.Token.Length != 0)
				{
					Token = other.Token;
				}
				grantedPermissions_.Add(other.grantedPermissions_);
				deniedPermissions_.Add(other.deniedPermissions_);
				if (other.AdditionalData.Length != 0)
				{
					AdditionalData = other.AdditionalData;
				}
				additionalDataCollections_.Add(other.additionalDataCollections_);
				if (other.UserId.Length != 0)
				{
					UserId = other.UserId;
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
				case 34u:
					Token = input.ReadBytes();
					break;
				case 40u:
				case 42u:
					grantedPermissions_.AddEntriesFrom(input, _repeated_grantedPermissions_codec);
					break;
				case 48u:
				case 50u:
					deniedPermissions_.AddEntriesFrom(input, _repeated_deniedPermissions_codec);
					break;
				case 58u:
					AdditionalData = input.ReadString();
					break;
				case 66u:
					additionalDataCollections_.AddEntriesFrom(input, _repeated_additionalDataCollections_codec);
					break;
				case 74u:
					UserId = input.ReadBytes();
					break;
				}
			}
		}
	}
}
