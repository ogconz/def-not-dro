using System;
using UnityEngine;

namespace MessagePack.Formatters
{
	public sealed class VinylLayerProxyFormatter : IMessagePackFormatter<VinylLayerProxy>, IMessagePackFormatter
	{
		public void Serialize(ref MessagePackWriter writer, VinylLayerProxy value, MessagePackSerializerOptions options)
		{
			IFormatterResolver resolver = options.Resolver;
			writer.WriteArrayHeader(15);
			writer.Write(value.version);
			writer.Write(value.rawId);
			writer.Write(value.groupMask);
			writer.Write(value.isSymmetry);
			writer.Write(value.isHorizontalFlip);
			writer.Write(value.isVerticalFlip);
			writer.Write(value.isPassThrough);
			writer.Write(value.angle);
			resolver.GetFormatterWithVerify<Vector3>().Serialize(ref writer, value.position, options);
			resolver.GetFormatterWithVerify<Quaternion>().Serialize(ref writer, value.rotation, options);
			resolver.GetFormatterWithVerify<Vector2>().Serialize(ref writer, value.scale, options);
			writer.Write(value.clipMaskShift);
			resolver.GetFormatterWithVerify<Matrix4x4>().Serialize(ref writer, value.projectionMatrix, options);
			resolver.GetFormatterWithVerify<int[]>().Serialize(ref writer, value.materialProps, options);
			resolver.GetFormatterWithVerify<Color[]>().Serialize(ref writer, value.colors, options);
		}

		public VinylLayerProxy Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			if (reader.TryReadNil())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			options.Security.DepthStep(ref reader);
			IFormatterResolver resolver = options.Resolver;
			int num = reader.ReadArrayHeader();
			VinylLayerProxy result = default(VinylLayerProxy);
			for (int i = 0; i < num; i++)
			{
				switch (i)
				{
				case 0:
					result.version = reader.ReadByte();
					break;
				case 1:
					result.rawId = reader.ReadInt32();
					break;
				case 2:
					result.groupMask = reader.ReadInt32();
					break;
				case 3:
					result.isSymmetry = reader.ReadBoolean();
					break;
				case 4:
					result.isHorizontalFlip = reader.ReadBoolean();
					break;
				case 5:
					result.isVerticalFlip = reader.ReadBoolean();
					break;
				case 6:
					result.isPassThrough = reader.ReadBoolean();
					break;
				case 7:
					result.angle = reader.ReadSingle();
					break;
				case 8:
					result.position = resolver.GetFormatterWithVerify<Vector3>().Deserialize(ref reader, options);
					break;
				case 9:
					result.rotation = resolver.GetFormatterWithVerify<Quaternion>().Deserialize(ref reader, options);
					break;
				case 10:
					result.scale = resolver.GetFormatterWithVerify<Vector2>().Deserialize(ref reader, options);
					break;
				case 11:
					result.clipMaskShift = reader.ReadSingle();
					break;
				case 12:
					result.projectionMatrix = resolver.GetFormatterWithVerify<Matrix4x4>().Deserialize(ref reader, options);
					break;
				case 13:
					result.materialProps = resolver.GetFormatterWithVerify<int[]>().Deserialize(ref reader, options);
					break;
				case 14:
					result.colors = resolver.GetFormatterWithVerify<Color[]>().Deserialize(ref reader, options);
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
