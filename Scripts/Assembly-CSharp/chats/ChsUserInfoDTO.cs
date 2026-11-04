using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsUserInfoDTO : IMessage<ChsUserInfoDTO>, IMessage, IEquatable<ChsUserInfoDTO>, IDeepCloneable<ChsUserInfoDTO>
	{
		private static readonly MessageParser<ChsUserInfoDTO> _parser = new MessageParser<ChsUserInfoDTO>(() => new ChsUserInfoDTO());

		private UnknownFieldSet _unknownFields;

		public const int IdFieldNumber = 1;

		private ByteString id_ = ByteString.Empty;

		public const int NameFieldNumber = 2;

		private string name_ = "";

		public const int AdditionalDataFieldNumber = 3;

		private string additionalData_ = "";

		[DebuggerNonUserCode]
		public static MessageParser<ChsUserInfoDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsUserInfoDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public ByteString Id
		{
			get
			{
				return id_;
			}
			set
			{
				id_ = ProtoPreconditions.CheckNotNull(value, "value");
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
		public ChsUserInfoDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsUserInfoDTO(ChsUserInfoDTO other)
			: this()
		{
			id_ = other.id_;
			name_ = other.name_;
			additionalData_ = other.additionalData_;
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsUserInfoDTO Clone()
		{
			return new ChsUserInfoDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsUserInfoDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsUserInfoDTO other)
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
			if (Name != other.Name)
			{
				return false;
			}
			if (AdditionalData != other.AdditionalData)
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			if (Id.Length != 0)
			{
				num ^= Id.GetHashCode();
			}
			if (Name.Length != 0)
			{
				num ^= Name.GetHashCode();
			}
			if (AdditionalData.Length != 0)
			{
				num ^= AdditionalData.GetHashCode();
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
			if (Id.Length != 0)
			{
				output.WriteRawTag(10);
				output.WriteBytes(Id);
			}
			if (Name.Length != 0)
			{
				output.WriteRawTag(18);
				output.WriteString(Name);
			}
			if (AdditionalData.Length != 0)
			{
				output.WriteRawTag(26);
				output.WriteString(AdditionalData);
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
			if (Id.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeBytesSize(Id);
			}
			if (Name.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(Name);
			}
			if (AdditionalData.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(AdditionalData);
			}
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsUserInfoDTO other)
		{
			if (other != null)
			{
				if (other.Id.Length != 0)
				{
					Id = other.Id;
				}
				if (other.Name.Length != 0)
				{
					Name = other.Name;
				}
				if (other.AdditionalData.Length != 0)
				{
					AdditionalData = other.AdditionalData;
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
					Id = input.ReadBytes();
					break;
				case 18u:
					Name = input.ReadString();
					break;
				case 26u:
					AdditionalData = input.ReadString();
					break;
				}
			}
		}
	}
}
