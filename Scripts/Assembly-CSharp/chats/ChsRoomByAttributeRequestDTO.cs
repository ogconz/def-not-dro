using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsRoomByAttributeRequestDTO : IMessage<ChsRoomByAttributeRequestDTO>, IMessage, IEquatable<ChsRoomByAttributeRequestDTO>, IDeepCloneable<ChsRoomByAttributeRequestDTO>
	{
		private static readonly MessageParser<ChsRoomByAttributeRequestDTO> _parser = new MessageParser<ChsRoomByAttributeRequestDTO>(() => new ChsRoomByAttributeRequestDTO());

		private UnknownFieldSet _unknownFields;

		public const int IndexNameFieldNumber = 1;

		private string indexName_ = "";

		public const int AttributeValueFieldNumber = 2;

		private string attributeValue_ = "";

		[DebuggerNonUserCode]
		public static MessageParser<ChsRoomByAttributeRequestDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsRoomByAttributeRequestDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public string IndexName
		{
			get
			{
				return indexName_;
			}
			set
			{
				indexName_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public string AttributeValue
		{
			get
			{
				return attributeValue_;
			}
			set
			{
				attributeValue_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public ChsRoomByAttributeRequestDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsRoomByAttributeRequestDTO(ChsRoomByAttributeRequestDTO other)
			: this()
		{
			indexName_ = other.indexName_;
			attributeValue_ = other.attributeValue_;
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsRoomByAttributeRequestDTO Clone()
		{
			return new ChsRoomByAttributeRequestDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsRoomByAttributeRequestDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsRoomByAttributeRequestDTO other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			if (IndexName != other.IndexName)
			{
				return false;
			}
			if (AttributeValue != other.AttributeValue)
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			if (IndexName.Length != 0)
			{
				num ^= IndexName.GetHashCode();
			}
			if (AttributeValue.Length != 0)
			{
				num ^= AttributeValue.GetHashCode();
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
			if (IndexName.Length != 0)
			{
				output.WriteRawTag(10);
				output.WriteString(IndexName);
			}
			if (AttributeValue.Length != 0)
			{
				output.WriteRawTag(18);
				output.WriteString(AttributeValue);
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
			if (IndexName.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(IndexName);
			}
			if (AttributeValue.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeStringSize(AttributeValue);
			}
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsRoomByAttributeRequestDTO other)
		{
			if (other != null)
			{
				if (other.IndexName.Length != 0)
				{
					IndexName = other.IndexName;
				}
				if (other.AttributeValue.Length != 0)
				{
					AttributeValue = other.AttributeValue;
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
					IndexName = input.ReadString();
					break;
				case 18u:
					AttributeValue = input.ReadString();
					break;
				}
			}
		}
	}
}
