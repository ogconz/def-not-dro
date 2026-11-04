using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsEnterRoomInfoDTO : IMessage<ChsEnterRoomInfoDTO>, IMessage, IEquatable<ChsEnterRoomInfoDTO>, IDeepCloneable<ChsEnterRoomInfoDTO>
	{
		private static readonly MessageParser<ChsEnterRoomInfoDTO> _parser = new MessageParser<ChsEnterRoomInfoDTO>(() => new ChsEnterRoomInfoDTO());

		private UnknownFieldSet _unknownFields;

		public const int TokenFieldNumber = 1;

		private ChsAuthorizedTokenDTO token_;

		public const int ContentFieldNumber = 2;

		private static readonly FieldCodec<ChsRoomHistoryItemDTO> _repeated_content_codec = FieldCodec.ForMessage(18u, ChsRoomHistoryItemDTO.Parser);

		private readonly RepeatedField<ChsRoomHistoryItemDTO> content_ = new RepeatedField<ChsRoomHistoryItemDTO>();

		public const int UsersInRoomFieldNumber = 3;

		private static readonly FieldCodec<ChsUserInfoDTO> _repeated_usersInRoom_codec = FieldCodec.ForMessage(26u, ChsUserInfoDTO.Parser);

		private readonly RepeatedField<ChsUserInfoDTO> usersInRoom_ = new RepeatedField<ChsUserInfoDTO>();

		public const int PublicAttributesFieldNumber = 4;

		private static readonly FieldCodec<ChsKeyValueDTO> _repeated_publicAttributes_codec = FieldCodec.ForMessage(34u, ChsKeyValueDTO.Parser);

		private readonly RepeatedField<ChsKeyValueDTO> publicAttributes_ = new RepeatedField<ChsKeyValueDTO>();

		public const int RoomIdFieldNumber = 5;

		private string roomId_ = "";

		public const int TemporallyDeniedPermissionsFieldNumber = 6;

		private static readonly FieldCodec<ChsKeyValueDTO> _repeated_temporallyDeniedPermissions_codec = FieldCodec.ForMessage(50u, ChsKeyValueDTO.Parser);

		private readonly RepeatedField<ChsKeyValueDTO> temporallyDeniedPermissions_ = new RepeatedField<ChsKeyValueDTO>();

		[DebuggerNonUserCode]
		public static MessageParser<ChsEnterRoomInfoDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsEnterRoomInfoDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public ChsAuthorizedTokenDTO Token
		{
			get
			{
				return token_;
			}
			set
			{
				token_ = value;
			}
		}

		[DebuggerNonUserCode]
		public RepeatedField<ChsRoomHistoryItemDTO> Content => content_;

		[DebuggerNonUserCode]
		public RepeatedField<ChsUserInfoDTO> UsersInRoom => usersInRoom_;

		[DebuggerNonUserCode]
		public RepeatedField<ChsKeyValueDTO> PublicAttributes => publicAttributes_;

		[DebuggerNonUserCode]
		public string RoomId
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
		public RepeatedField<ChsKeyValueDTO> TemporallyDeniedPermissions => temporallyDeniedPermissions_;

		[DebuggerNonUserCode]
		public ChsEnterRoomInfoDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsEnterRoomInfoDTO(ChsEnterRoomInfoDTO other)
			: this()
		{
			token_ = ((other.token_ != null) ? other.token_.Clone() : null);
			content_ = other.content_.Clone();
			usersInRoom_ = other.usersInRoom_.Clone();
			publicAttributes_ = other.publicAttributes_.Clone();
			roomId_ = other.roomId_;
			temporallyDeniedPermissions_ = other.temporallyDeniedPermissions_.Clone();
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsEnterRoomInfoDTO Clone()
		{
			return new ChsEnterRoomInfoDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsEnterRoomInfoDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsEnterRoomInfoDTO other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			if (!object.Equals(Token, other.Token))
			{
				return false;
			}
			if (!content_.Equals(other.content_))
			{
				return false;
			}
			if (!usersInRoom_.Equals(other.usersInRoom_))
			{
				return false;
			}
			if (!publicAttributes_.Equals(other.publicAttributes_))
			{
				return false;
			}
			if (RoomId != other.RoomId)
			{
				return false;
			}
			if (!temporallyDeniedPermissions_.Equals(other.temporallyDeniedPermissions_))
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			if (token_ != null)
			{
				num ^= Token.GetHashCode();
			}
			num ^= content_.GetHashCode();
			num ^= usersInRoom_.GetHashCode();
			num ^= publicAttributes_.GetHashCode();
			if (RoomId.Length != 0)
			{
				num ^= RoomId.GetHashCode();
			}
			num ^= temporallyDeniedPermissions_.GetHashCode();
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
			if (token_ != null)
			{
				output.WriteRawTag(10);
				output.WriteMessage(Token);
			}
			content_.WriteTo(output, _repeated_content_codec);
			usersInRoom_.WriteTo(output, _repeated_usersInRoom_codec);
			publicAttributes_.WriteTo(output, _repeated_publicAttributes_codec);
			if (RoomId.Length != 0)
			{
				output.WriteRawTag(42);
				output.WriteString(RoomId);
			}
			temporallyDeniedPermissions_.WriteTo(output, _repeated_temporallyDeniedPermissions_codec);
			if (_unknownFields != null)
			{
				_unknownFields.WriteTo(output);
			}
		}

		[DebuggerNonUserCode]
		public int CalculateSize()
		{
			int num = 0;
			if (token_ != null)
			{
				num += 1 + CodedOutputStream.ComputeMessageSize(Token);
			}
			num += content_.CalculateSize(_repeated_content_codec);
			num += usersInRoom_.CalculateSize(_repeated_usersInRoom_codec);
			num += publicAttributes_.CalculateSize(_repeated_publicAttributes_codec);
			if (RoomId.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(RoomId);
			}
			num += temporallyDeniedPermissions_.CalculateSize(_repeated_temporallyDeniedPermissions_codec);
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsEnterRoomInfoDTO other)
		{
			if (other == null)
			{
				return;
			}
			if (other.token_ != null)
			{
				if (token_ == null)
				{
					token_ = new ChsAuthorizedTokenDTO();
				}
				Token.MergeFrom(other.Token);
			}
			content_.Add(other.content_);
			usersInRoom_.Add(other.usersInRoom_);
			publicAttributes_.Add(other.publicAttributes_);
			if (other.RoomId.Length != 0)
			{
				RoomId = other.RoomId;
			}
			temporallyDeniedPermissions_.Add(other.temporallyDeniedPermissions_);
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
					if (token_ == null)
					{
						token_ = new ChsAuthorizedTokenDTO();
					}
					input.ReadMessage(token_);
					break;
				case 18u:
					content_.AddEntriesFrom(input, _repeated_content_codec);
					break;
				case 26u:
					usersInRoom_.AddEntriesFrom(input, _repeated_usersInRoom_codec);
					break;
				case 34u:
					publicAttributes_.AddEntriesFrom(input, _repeated_publicAttributes_codec);
					break;
				case 42u:
					RoomId = input.ReadString();
					break;
				case 50u:
					temporallyDeniedPermissions_.AddEntriesFrom(input, _repeated_temporallyDeniedPermissions_codec);
					break;
				}
			}
		}
	}
}
