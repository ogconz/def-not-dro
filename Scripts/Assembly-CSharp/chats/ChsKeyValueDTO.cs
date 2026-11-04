using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsKeyValueDTO : IMessage<ChsKeyValueDTO>, IMessage, IEquatable<ChsKeyValueDTO>, IDeepCloneable<ChsKeyValueDTO>
	{
		private static readonly MessageParser<ChsKeyValueDTO> _parser = new MessageParser<ChsKeyValueDTO>(() => new ChsKeyValueDTO());

		private UnknownFieldSet _unknownFields;

		public const int KeyFieldNumber = 1;

		private string key_ = "";

		public const int ValueFieldNumber = 2;

		private string value_ = "";

		[DebuggerNonUserCode]
		public static MessageParser<ChsKeyValueDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsKeyValueDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public string Key
		{
			get
			{
				return key_;
			}
			set
			{
				key_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public string Value
		{
			get
			{
				return value_;
			}
			set
			{
				value_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public ChsKeyValueDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsKeyValueDTO(ChsKeyValueDTO other)
			: this()
		{
			key_ = other.key_;
			value_ = other.value_;
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsKeyValueDTO Clone()
		{
			return new ChsKeyValueDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsKeyValueDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsKeyValueDTO other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			if (Key != other.Key)
			{
				return false;
			}
			if (Value != other.Value)
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			if (Key.Length != 0)
			{
				num ^= Key.GetHashCode();
			}
			if (Value.Length != 0)
			{
				num ^= Value.GetHashCode();
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
			if (Key.Length != 0)
			{
				output.WriteRawTag(10);
				output.WriteString(Key);
			}
			if (Value.Length != 0)
			{
				output.WriteRawTag(18);
				output.WriteString(Value);
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
			if (Key.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(Key);
			}
			if (Value.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(Value);
			}
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsKeyValueDTO other)
		{
			if (other != null)
			{
				if (other.Key.Length != 0)
				{
					Key = other.Key;
				}
				if (other.Value.Length != 0)
				{
					Value = other.Value;
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
					Key = input.ReadString();
					break;
				case 18u:
					Value = input.ReadString();
					break;
				}
			}
		}
	}
}
