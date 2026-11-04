using System;
using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;

namespace chats
{
	public sealed class ChsIncomingPrivateMessageDTO : IMessage<ChsIncomingPrivateMessageDTO>, IMessage, IEquatable<ChsIncomingPrivateMessageDTO>, IDeepCloneable<ChsIncomingPrivateMessageDTO>
	{
		private static readonly MessageParser<ChsIncomingPrivateMessageDTO> _parser = new MessageParser<ChsIncomingPrivateMessageDTO>(() => new ChsIncomingPrivateMessageDTO());

		private UnknownFieldSet _unknownFields;

		public const int ContentFieldNumber = 1;

		private ByteString content_ = ByteString.Empty;

		public const int ReceiversFieldNumber = 2;

		private static readonly FieldCodec<string> _repeated_receivers_codec = FieldCodec.ForString(18u);

		private readonly RepeatedField<string> receivers_ = new RepeatedField<string>();

		[DebuggerNonUserCode]
		public static MessageParser<ChsIncomingPrivateMessageDTO> Parser => _parser;

		[DebuggerNonUserCode]
		public static MessageDescriptor Descriptor => ChsIncomingPrivateMessageDTOReflection.Descriptor.MessageTypes[0];

		[DebuggerNonUserCode]
		MessageDescriptor IMessage.Descriptor => Descriptor;

		[DebuggerNonUserCode]
		public ByteString Content
		{
			get
			{
				return content_;
			}
			set
			{
				content_ = ProtoPreconditions.CheckNotNull(value, "value");
			}
		}

		[DebuggerNonUserCode]
		public RepeatedField<string> Receivers => receivers_;

		[DebuggerNonUserCode]
		public ChsIncomingPrivateMessageDTO()
		{
		}

		[DebuggerNonUserCode]
		public ChsIncomingPrivateMessageDTO(ChsIncomingPrivateMessageDTO other)
			: this()
		{
			content_ = other.content_;
			receivers_ = other.receivers_.Clone();
			_unknownFields = UnknownFieldSet.Clone(other._unknownFields);
		}

		[DebuggerNonUserCode]
		public ChsIncomingPrivateMessageDTO Clone()
		{
			return new ChsIncomingPrivateMessageDTO(this);
		}

		[DebuggerNonUserCode]
		public override bool Equals(object other)
		{
			return Equals(other as ChsIncomingPrivateMessageDTO);
		}

		[DebuggerNonUserCode]
		public bool Equals(ChsIncomingPrivateMessageDTO other)
		{
			if (other == null)
			{
				return false;
			}
			if (other == this)
			{
				return true;
			}
			if (Content != other.Content)
			{
				return false;
			}
			if (!receivers_.Equals(other.receivers_))
			{
				return false;
			}
			return object.Equals(_unknownFields, other._unknownFields);
		}

		[DebuggerNonUserCode]
		public override int GetHashCode()
		{
			int num = 1;
			if (Content.Length != 0)
			{
				num ^= Content.GetHashCode();
			}
			num ^= receivers_.GetHashCode();
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
			if (Content.Length != 0)
			{
				output.WriteRawTag(10);
				output.WriteBytes(Content);
			}
			receivers_.WriteTo(output, _repeated_receivers_codec);
			if (_unknownFields != null)
			{
				_unknownFields.WriteTo(output);
			}
		}

		[DebuggerNonUserCode]
		public int CalculateSize()
		{
			int num = 0;
			if (Content.Length != 0)
			{
				num += 1 + CodedOutputStream.ComputeBytesSize(Content);
			}
			num += receivers_.CalculateSize(_repeated_receivers_codec);
			if (_unknownFields != null)
			{
				num += _unknownFields.CalculateSize();
			}
			return num;
		}

		[DebuggerNonUserCode]
		public void MergeFrom(ChsIncomingPrivateMessageDTO other)
		{
			if (other != null)
			{
				if (other.Content.Length != 0)
				{
					Content = other.Content;
				}
				receivers_.Add(other.receivers_);
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
					Content = input.ReadBytes();
					break;
				case 18u:
					receivers_.AddEntriesFrom(input, _repeated_receivers_codec);
					break;
				}
			}
		}
	}
}
