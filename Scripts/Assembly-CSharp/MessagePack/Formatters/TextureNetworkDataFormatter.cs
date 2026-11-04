using System;
using MessagePack.Internal;

namespace MessagePack.Formatters
{
	public sealed class TextureNetworkDataFormatter : IMessagePackFormatter<TextureNetworkData>, IMessagePackFormatter
	{
		public void Serialize(ref MessagePackWriter writer, TextureNetworkData value, MessagePackSerializerOptions options)
		{
			writer.WriteArrayHeader(8);
			writer.Write(value.bodyTex);
			writer.Write(value.glassTex);
			writer.Write(value.bodyMat);
			writer.Write(value.glassMat);
			writer.Write(value.bodySize);
			writer.Write(value.glassSize);
			writer.Write(value.bodyFormat);
			writer.Write(value.glassFormat);
		}

		public TextureNetworkData Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			if (reader.TryReadNil())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			options.Security.DepthStep(ref reader);
			int num = reader.ReadArrayHeader();
			TextureNetworkData result = default(TextureNetworkData);
			for (int i = 0; i < num; i++)
			{
				switch (i)
				{
				case 0:
					result.bodyTex = CodeGenHelpers.GetArrayFromNullableSequence(reader.ReadBytes());
					break;
				case 1:
					result.glassTex = CodeGenHelpers.GetArrayFromNullableSequence(reader.ReadBytes());
					break;
				case 2:
					result.bodyMat = CodeGenHelpers.GetArrayFromNullableSequence(reader.ReadBytes());
					break;
				case 3:
					result.glassMat = CodeGenHelpers.GetArrayFromNullableSequence(reader.ReadBytes());
					break;
				case 4:
					result.bodySize = reader.ReadInt32();
					break;
				case 5:
					result.glassSize = reader.ReadInt32();
					break;
				case 6:
					result.bodyFormat = reader.ReadInt32();
					break;
				case 7:
					result.glassFormat = reader.ReadInt32();
					break;
				default:
					reader.Skip();
					break;
				}
			}
			reader.Depth--;
			return result;
		}
	}
}
