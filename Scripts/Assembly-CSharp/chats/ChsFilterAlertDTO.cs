using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsFilterAlertDTO : IMessage<ChsFilterAlertDTO>, IMessage, IEquatable<ChsFilterAlertDTO>, IDeepCloneable<ChsFilterAlertDTO>
	{
		private static readonly MessageParser<ChsFilterAlertDTO> _parser = new MessageParser<ChsFilterAlertDTO>(() => new ChsFilterAlertDTO());

		private UnknownFieldSet _unknownFields;

		public const int FilterNameFieldNumber = 1;

		private ChsFilterNames filterName_;

		public const int PenaltyDurationMillisecondsFieldNumber = 2;

		private long penaltyDurationMilliseconds_;

		public const int DeniedPermissionsFieldNumber = 3;

		private static readonly FieldCodec<ChsUserPermissionsChat> _repeated_deniedPermissions_codec = FieldCodec.ForEnum(26u, (ChsUserPermissionsChat x) => (int)x, (int x) => (ChsUserPermissionsChat)x);

		private readonly RepeatedField<ChsUserPermissionsChat> deniedPermissions_ = new RepeatedField<ChsUserPermissionsChat>();

		[DebuggerNonUserCode]
		public static MessageParser<ChsFilterAlertDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsFilterAlertDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public ChsFilterNames FilterName
		{
			get
			{
				return filterName_;
			}
			set
			{
				filterName_ = value;
			}
		}

		[DebuggerNonUserCode]
		public long PenaltyDurationMilliseconds
		{
			get
			{
				return penaltyDurationMilliseconds_;
			}
			set
			{
				penaltyDurationMilliseconds_ = value;
			}
		}

		[DebuggerNonUserCode]
		public RepeatedField<ChsUserPermissionsChat> DeniedPermissions => deniedPermissions_;

		[DebuggerNonUserCode]
		public ChsFilterAlertDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsFilterAlertDTO(ChsFilterAlertDTO other)
			: this()
		{
			filterName_ = other.filterName_;
			penaltyDurationMilliseconds_ = other.penaltyDurationMilliseconds_;
			deniedPermissions_ = other.deniedPermissions_.Clone();
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsFilterAlertDTO Clone()
		{
			return new ChsFilterAlertDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsFilterAlertDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsFilterAlertDTO other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			if (FilterName != other.FilterName)
			{
				return false;
			}
			if (PenaltyDurationMilliseconds != other.PenaltyDurationMilliseconds)
			{
				return false;
			}
			if (!deniedPermissions_.Equals(other.deniedPermissions_))
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			if (FilterName != ChsFilterNames.Unknown)
			{
				num ^= FilterName.GetHashCode();
			}
			if (PenaltyDurationMilliseconds != 0L)
			{
				num ^= PenaltyDurationMilliseconds.GetHashCode();
			}
			num ^= deniedPermissions_.GetHashCode();
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
			if (FilterName != ChsFilterNames.Unknown)
			{
				output.WriteRawTag(8);
				output.WriteEnum((int)FilterName);
			}
			if (PenaltyDurationMilliseconds != 0L)
			{
				output.WriteRawTag(16);
				output.WriteInt64(PenaltyDurationMilliseconds);
			}
			deniedPermissions_.WriteTo(output, _repeated_deniedPermissions_codec);
			if (_unknownFields != null)
			{
				_unknownFields.WriteTo(output);
			}
		}

		[DebuggerNonUserCode]
		public int CalculateSize()
		{
			int num = 0;
			if (FilterName != ChsFilterNames.Unknown)
			{
				num += 1 + CodedOutputStream.ComputeEnumSize((int)FilterName);
			}
			if (PenaltyDurationMilliseconds != 0L)
			{
				num += 1 + CodedOutputStream.ComputeInt64Size(PenaltyDurationMilliseconds);
			}
			num += deniedPermissions_.CalculateSize(_repeated_deniedPermissions_codec);
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsFilterAlertDTO other)
		{
			if (other != null)
			{
				if (other.FilterName != ChsFilterNames.Unknown)
				{
					FilterName = other.FilterName;
				}
				if (other.PenaltyDurationMilliseconds != 0L)
				{
					PenaltyDurationMilliseconds = other.PenaltyDurationMilliseconds;
				}
				deniedPermissions_.Add(other.deniedPermissions_);
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
					filterName_ = (ChsFilterNames)input.ReadEnum();
					break;
				case 16u:
					PenaltyDurationMilliseconds = input.ReadInt64();
					break;
				case 24u:
				case 26u:
					deniedPermissions_.AddEntriesFrom(input, _repeated_deniedPermissions_codec);
					break;
				}
			}
		}
	}
}
