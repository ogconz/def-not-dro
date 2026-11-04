using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsContentItemDTO : IMessage<ChsContentItemDTO>, IMessage, IEquatable<ChsContentItemDTO>, IDeepCloneable<ChsContentItemDTO>
	{
		private static readonly MessageParser<ChsContentItemDTO> _parser = new MessageParser<ChsContentItemDTO>(() => new ChsContentItemDTO());

		private UnknownFieldSet _unknownFields;

		public const int AuthorFieldNumber = 2;

		private ByteString author_ = ByteString.Empty;

		public const int AuthorNameFieldNumber = 3;

		private string authorName_ = "";

		public const int RawContentFieldNumber = 4;

		private ByteString rawContent_ = ByteString.Empty;

		public const int TimestampFieldNumber = 5;

		private long timestamp_;

		public const int PreviousPlaceFieldNumber = 6;

		private int previousPlace_;

		public const int PremiumTypeFieldNumber = 7;

		private ChsPremiumType premiumType_;

		public const int FrameHeightFieldNumber = 8;

		private int frameHeight_;

		public const int AuthorGlobalIdFieldNumber = 9;

		private string authorGlobalId_ = "";

		public const int ClanNameFieldNumber = 10;

		private string clanName_ = "";

		public const int AdminFieldNumber = 101;

		private bool admin_;

		[DebuggerNonUserCode]
		public static MessageParser<ChsContentItemDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsContentItemDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public ByteString Author
		{
			get
			{
				return author_;
			}
			set
			{
				author_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public string AuthorName
		{
			get
			{
				return authorName_;
			}
			set
			{
				authorName_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public ByteString RawContent
		{
			get
			{
				return rawContent_;
			}
			set
			{
				rawContent_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public long Timestamp
		{
			get
			{
				return timestamp_;
			}
			set
			{
				timestamp_ = value;
			}
		}

		[DebuggerNonUserCode]
		public int PreviousPlace
		{
			get
			{
				return previousPlace_;
			}
			set
			{
				previousPlace_ = value;
			}
		}

		[DebuggerNonUserCode]
		public ChsPremiumType PremiumType
		{
			get
			{
				return premiumType_;
			}
			set
			{
				premiumType_ = value;
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
		public string AuthorGlobalId
		{
			get
			{
				return authorGlobalId_;
			}
			set
			{
				authorGlobalId_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public string ClanName
		{
			get
			{
				return clanName_;
			}
			set
			{
				clanName_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public bool Admin
		{
			get
			{
				return admin_;
			}
			set
			{
				admin_ = value;
			}
		}

		[DebuggerNonUserCode]
		public ChsContentItemDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsContentItemDTO(ChsContentItemDTO other)
			: this()
		{
			author_ = other.author_;
			authorName_ = other.authorName_;
			rawContent_ = other.rawContent_;
			timestamp_ = other.timestamp_;
			previousPlace_ = other.previousPlace_;
			premiumType_ = other.premiumType_;
			frameHeight_ = other.frameHeight_;
			authorGlobalId_ = other.authorGlobalId_;
			clanName_ = other.clanName_;
			admin_ = other.admin_;
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsContentItemDTO Clone()
		{
			return new ChsContentItemDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsContentItemDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsContentItemDTO other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			if (Author != other.Author)
			{
				return false;
			}
			if (AuthorName != other.AuthorName)
			{
				return false;
			}
			if (RawContent != other.RawContent)
			{
				return false;
			}
			if (Timestamp != other.Timestamp)
			{
				return false;
			}
			if (PreviousPlace != other.PreviousPlace)
			{
				return false;
			}
			if (PremiumType != other.PremiumType)
			{
				return false;
			}
			if (FrameHeight != other.FrameHeight)
			{
				return false;
			}
			if (AuthorGlobalId != other.AuthorGlobalId)
			{
				return false;
			}
			if (ClanName != other.ClanName)
			{
				return false;
			}
			if (Admin != other.Admin)
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			if (Author.Length != 0)
			{
				num ^= Author.GetHashCode();
			}
			if (AuthorName.Length != 0)
			{
				num ^= AuthorName.GetHashCode();
			}
			if (RawContent.Length != 0)
			{
				num ^= RawContent.GetHashCode();
			}
			if (Timestamp != 0L)
			{
				num ^= Timestamp.GetHashCode();
			}
			if (PreviousPlace != 0)
			{
				num ^= PreviousPlace.GetHashCode();
			}
			if (PremiumType != ChsPremiumType.UnknownPremiumType)
			{
				num ^= PremiumType.GetHashCode();
			}
			if (FrameHeight != 0)
			{
				num ^= FrameHeight.GetHashCode();
			}
			if (AuthorGlobalId.Length != 0)
			{
				num ^= AuthorGlobalId.GetHashCode();
			}
			if (ClanName.Length != 0)
			{
				num ^= ClanName.GetHashCode();
			}
			if (Admin)
			{
				num ^= Admin.GetHashCode();
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
			if (Author.Length != 0)
			{
				output.WriteRawTag(18);
				output.WriteBytes(Author);
			}
			if (AuthorName.Length != 0)
			{
				output.WriteRawTag(26);
				output.WriteString(AuthorName);
			}
			if (RawContent.Length != 0)
			{
				output.WriteRawTag(34);
				output.WriteBytes(RawContent);
			}
			if (Timestamp != 0L)
			{
				output.WriteRawTag(40);
				output.WriteInt64(Timestamp);
			}
			if (PreviousPlace != 0)
			{
				output.WriteRawTag(48);
				output.WriteInt32(PreviousPlace);
			}
			if (PremiumType != ChsPremiumType.UnknownPremiumType)
			{
				output.WriteRawTag(56);
				output.WriteEnum((int)PremiumType);
			}
			if (FrameHeight != 0)
			{
				output.WriteRawTag(64);
				output.WriteInt32(FrameHeight);
			}
			if (AuthorGlobalId.Length != 0)
			{
				output.WriteRawTag(74);
				output.WriteString(AuthorGlobalId);
			}
			if (ClanName.Length != 0)
			{
				output.WriteRawTag(82);
				output.WriteString(ClanName);
			}
			if (Admin)
			{
				output.WriteRawTag(168, 6);
				output.WriteBool(Admin);
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
			if (Author.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeBytesSize(Author);
			}
			if (AuthorName.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(AuthorName);
			}
			if (RawContent.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeBytesSize(RawContent);
			}
			if (Timestamp != 0L)
			{
				num += 1 + CodedOutputStream.ComputeInt64Size(Timestamp);
			}
			if (PreviousPlace != 0)
			{
				num += 1 + CodedOutputStream.ComputeInt32Size(PreviousPlace);
			}
			if (PremiumType != ChsPremiumType.UnknownPremiumType)
			{
				num += 1 + CodedOutputStream.ComputeEnumSize((int)PremiumType);
			}
			if (FrameHeight != 0)
			{
				num += 1 + CodedOutputStream.ComputeInt32Size(FrameHeight);
			}
			if (AuthorGlobalId.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(AuthorGlobalId);
			}
			if (ClanName.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(ClanName);
			}
			if (Admin)
			{
				num += 3;
			}
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsContentItemDTO other)
		{
			if (other != null)
			{
				if (other.Author.Length != 0)
				{
					Author = other.Author;
				}
				if (other.AuthorName.Length != 0)
				{
					AuthorName = other.AuthorName;
				}
				if (other.RawContent.Length != 0)
				{
					RawContent = other.RawContent;
				}
				if (other.Timestamp != 0L)
				{
					Timestamp = other.Timestamp;
				}
				if (other.PreviousPlace != 0)
				{
					PreviousPlace = other.PreviousPlace;
				}
				if (other.PremiumType != ChsPremiumType.UnknownPremiumType)
				{
					PremiumType = other.PremiumType;
				}
				if (other.FrameHeight != 0)
				{
					FrameHeight = other.FrameHeight;
				}
				if (other.AuthorGlobalId.Length != 0)
				{
					AuthorGlobalId = other.AuthorGlobalId;
				}
				if (other.ClanName.Length != 0)
				{
					ClanName = other.ClanName;
				}
				if (other.Admin)
				{
					Admin = other.Admin;
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
				case 18u:
					Author = input.ReadBytes();
					break;
				case 26u:
					AuthorName = input.ReadString();
					break;
				case 34u:
					RawContent = input.ReadBytes();
					break;
				case 40u:
					Timestamp = input.ReadInt64();
					break;
				case 48u:
					PreviousPlace = input.ReadInt32();
					break;
				case 56u:
					premiumType_ = (ChsPremiumType)input.ReadEnum();
					break;
				case 64u:
					FrameHeight = input.ReadInt32();
					break;
				case 74u:
					AuthorGlobalId = input.ReadString();
					break;
				case 82u:
					ClanName = input.ReadString();
					break;
				case 808u:
					Admin = input.ReadBool();
					break;
				}
			}
		}
	}
}
