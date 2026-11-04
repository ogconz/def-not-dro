using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsNoPermissionErrorDTO : IMessage<ChsNoPermissionErrorDTO>, IMessage, IEquatable<ChsNoPermissionErrorDTO>, IDeepCloneable<ChsNoPermissionErrorDTO>
	{
		private static readonly MessageParser<ChsNoPermissionErrorDTO> _parser = new MessageParser<ChsNoPermissionErrorDTO>(() => new ChsNoPermissionErrorDTO());

		private UnknownFieldSet _unknownFields;

		public const int IsTemporalFieldNumber = 1;

		private bool isTemporal_;

		public const int DeniedUntilFieldNumber = 2;

		private long deniedUntil_;

		public const int PermissionFieldNumber = 3;

		private ChsUserPermissionsChat permission_;

		[DebuggerNonUserCode]
		public static MessageParser<ChsNoPermissionErrorDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsNoPermissionErrorDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public bool IsTemporal
		{
			get
			{
				return isTemporal_;
			}
			set
			{
				isTemporal_ = value;
			}
		}

		[DebuggerNonUserCode]
		public long DeniedUntil
		{
			get
			{
				return deniedUntil_;
			}
			set
			{
				deniedUntil_ = value;
			}
		}

		[DebuggerNonUserCode]
		public ChsUserPermissionsChat Permission
		{
			get
			{
				return permission_;
			}
			set
			{
				permission_ = value;
			}
		}

		[DebuggerNonUserCode]
		public ChsNoPermissionErrorDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsNoPermissionErrorDTO(ChsNoPermissionErrorDTO other)
			: this()
		{
			isTemporal_ = other.isTemporal_;
			deniedUntil_ = other.deniedUntil_;
			permission_ = other.permission_;
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsNoPermissionErrorDTO Clone()
		{
			return new ChsNoPermissionErrorDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsNoPermissionErrorDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsNoPermissionErrorDTO other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			if (IsTemporal != other.IsTemporal)
			{
				return false;
			}
			if (DeniedUntil != other.DeniedUntil)
			{
				return false;
			}
			if (Permission != other.Permission)
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			if (IsTemporal)
			{
				num ^= IsTemporal.GetHashCode();
			}
			if (DeniedUntil != 0L)
			{
				num ^= DeniedUntil.GetHashCode();
			}
			if (Permission != ChsUserPermissionsChat.UnknownPermission)
			{
				num ^= Permission.GetHashCode();
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
			if (IsTemporal)
			{
				output.WriteRawTag(8);
				output.WriteBool(IsTemporal);
			}
			if (DeniedUntil != 0L)
			{
				output.WriteRawTag(16);
				output.WriteInt64(DeniedUntil);
			}
			if (Permission != ChsUserPermissionsChat.UnknownPermission)
			{
				output.WriteRawTag(24);
				output.WriteEnum((int)Permission);
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
			if (IsTemporal)
			{
				num += 2;
			}
			if (DeniedUntil != 0L)
			{
				num += 1 + CodedOutputStream.ComputeInt64Size(DeniedUntil);
			}
			if (Permission != ChsUserPermissionsChat.UnknownPermission)
			{
				num += 1 + CodedOutputStream.ComputeEnumSize((int)Permission);
			}
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsNoPermissionErrorDTO other)
		{
			if (other != null)
			{
				if (other.IsTemporal)
				{
					IsTemporal = other.IsTemporal;
				}
				if (other.DeniedUntil != 0L)
				{
					DeniedUntil = other.DeniedUntil;
				}
				if (other.Permission != ChsUserPermissionsChat.UnknownPermission)
				{
					Permission = other.Permission;
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
					IsTemporal = input.ReadBool();
					break;
				case 16u:
					DeniedUntil = input.ReadInt64();
					break;
				case 24u:
					permission_ = (ChsUserPermissionsChat)input.ReadEnum();
					break;
				}
			}
		}
	}
}
