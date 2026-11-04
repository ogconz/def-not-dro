using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class RtuAuthenticationCredentialsDTO : IMessage<RtuAuthenticationCredentialsDTO>, IMessage, IEquatable<RtuAuthenticationCredentialsDTO>, IDeepCloneable<RtuAuthenticationCredentialsDTO>
	{
		private static readonly MessageParser<RtuAuthenticationCredentialsDTO> _parser = new MessageParser<RtuAuthenticationCredentialsDTO>(() => new RtuAuthenticationCredentialsDTO());

		private UnknownFieldSet _unknownFields;

		public const int GlobalTokenFieldNumber = 1;

		private ChsGlobalTokenDTO globalToken_;

		public const int RoomIdFieldNumber = 2;

		private string roomId_ = "";

		public const int NameFieldNumber = 3;

		private string name_ = "";

		public const int AttributesFieldNumber = 4;

		private static readonly FieldCodec<ChsKeyValueDTO> _repeated_attributes_codec = FieldCodec.ForMessage(34u, ChsKeyValueDTO.Parser);

		private readonly RepeatedField<ChsKeyValueDTO> attributes_ = new RepeatedField<ChsKeyValueDTO>();

		[DebuggerNonUserCode]
		public static MessageParser<RtuAuthenticationCredentialsDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => RtuAuthenticationCredentialsDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public ChsGlobalTokenDTO GlobalToken
		{
			get
			{
				return globalToken_;
			}
			set
			{
				globalToken_ = value;
			}
		}

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
		public RepeatedField<ChsKeyValueDTO> Attributes => attributes_;

		[DebuggerNonUserCode]
		public RtuAuthenticationCredentialsDTO()
		{
		}

		[DebuggerNonUserCode]
		public RtuAuthenticationCredentialsDTO(RtuAuthenticationCredentialsDTO other)
			: this()
		{
			globalToken_ = ((other.globalToken_ != null) ? other.globalToken_.Clone() : null);
			roomId_ = other.roomId_;
			name_ = other.name_;
			attributes_ = other.attributes_.Clone();
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public RtuAuthenticationCredentialsDTO Clone()
		{
			return new RtuAuthenticationCredentialsDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as RtuAuthenticationCredentialsDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(RtuAuthenticationCredentialsDTO other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			if (!object.Equals(GlobalToken, other.GlobalToken))
			{
				return false;
			}
			if (RoomId != other.RoomId)
			{
				return false;
			}
			if (Name != other.Name)
			{
				return false;
			}
			if (!attributes_.Equals(other.attributes_))
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			if (globalToken_ != null)
			{
				num ^= GlobalToken.GetHashCode();
			}
			if (RoomId.Length != 0)
			{
				num ^= RoomId.GetHashCode();
			}
			if (Name.Length != 0)
			{
				num ^= Name.GetHashCode();
			}
			num ^= attributes_.GetHashCode();
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
			if (globalToken_ != null)
			{
				output.WriteRawTag(10);
				output.WriteMessage(GlobalToken);
			}
			if (RoomId.Length != 0)
			{
				output.WriteRawTag(18);
				output.WriteString(RoomId);
			}
			if (Name.Length != 0)
			{
				output.WriteRawTag(26);
				output.WriteString(Name);
			}
			attributes_.WriteTo(output, _repeated_attributes_codec);
			if (_unknownFields != null)
			{
				_unknownFields.WriteTo(output);
			}
		}

		[DebuggerNonUserCode]
		public int CalculateSize()
		{
			int num = 0;
			if (globalToken_ != null)
			{
				num += 1 + CodedOutputStream.ComputeMessageSize(GlobalToken);
			}
			if (RoomId.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(RoomId);
			}
			if (Name.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(Name);
			}
			num += attributes_.CalculateSize(_repeated_attributes_codec);
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(RtuAuthenticationCredentialsDTO other)
		{
			if (other == null)
			{
				return;
			}
			if (other.globalToken_ != null)
			{
				if (globalToken_ == null)
				{
					globalToken_ = new ChsGlobalTokenDTO();
				}
				GlobalToken.MergeFrom(other.GlobalToken);
			}
			if (other.RoomId.Length != 0)
			{
				RoomId = other.RoomId;
			}
			if (other.Name.Length != 0)
			{
				Name = other.Name;
			}
			attributes_.Add(other.attributes_);
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
					if (globalToken_ == null)
					{
						globalToken_ = new ChsGlobalTokenDTO();
					}
					input.ReadMessage(globalToken_);
					break;
				case 18u:
					RoomId = input.ReadString();
					break;
				case 26u:
					Name = input.ReadString();
					break;
				case 34u:
					attributes_.AddEntriesFrom(input, _repeated_attributes_codec);
					break;
				}
			}
		}
	}
}
