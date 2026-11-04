using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsUserDataDTO : IMessage<ChsUserDataDTO>, IMessage, IEquatable<ChsUserDataDTO>, IDeepCloneable<ChsUserDataDTO>
	{
		private static readonly MessageParser<ChsUserDataDTO> _parser = new MessageParser<ChsUserDataDTO>(() => new ChsUserDataDTO());

		private UnknownFieldSet _unknownFields;

		public const int KeyFieldNumber = 1;

		private string key_ = "";

		public const int ValuesFieldNumber = 2;

		private static readonly FieldCodec<string> _repeated_values_codec = FieldCodec.ForString(18u);

		private readonly RepeatedField<string> values_ = new RepeatedField<string>();

		[DebuggerNonUserCode]
		public static MessageParser<ChsUserDataDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsUserDataDTOReflection.Descriptor.MessageTypes[0];

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
		public RepeatedField<string> Values => values_;

		[DebuggerNonUserCode]
		public ChsUserDataDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsUserDataDTO(ChsUserDataDTO other)
			: this()
		{
			key_ = other.key_;
			values_ = other.values_.Clone();
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsUserDataDTO Clone()
		{
			return new ChsUserDataDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsUserDataDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsUserDataDTO other)
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
			if (!values_.Equals(other.values_))
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
			num ^= values_.GetHashCode();
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
			values_.WriteTo(output, _repeated_values_codec);
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
			num += values_.CalculateSize(_repeated_values_codec);
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsUserDataDTO other)
		{
			if (other != null)
			{
				if (other.Key.Length != 0)
				{
					Key = other.Key;
				}
				values_.Add(other.values_);
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
					values_.AddEntriesFrom(input, _repeated_values_codec);
					break;
				}
			}
		}
	}
}
