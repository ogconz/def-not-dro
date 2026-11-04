using System;
using UnityEngine;

namespace MessagePack.Formatters
{
	public sealed class CarPaintHolderProxyFormatter : IMessagePackFormatter<CarPaintHolderProxy>, IMessagePackFormatter
	{
		public void Serialize(ref MessagePackWriter writer, CarPaintHolderProxy value, MessagePackSerializerOptions options)
		{
			IFormatterResolver resolver = options.Resolver;
			writer.WriteArrayHeader(7);
			resolver.GetFormatterWithVerify<CarVisualManager.CustomizationFlags>().Serialize(ref writer, value.type, options);
			writer.Write(value.id);
			resolver.GetFormatterWithVerify<Color>().Serialize(ref writer, value.color, options);
			resolver.GetFormatterWithVerify<Color>().Serialize(ref writer, value.specColor, options);
			resolver.GetFormatterWithVerify<Color>().Serialize(ref writer, value.duoColor, options);
			resolver.GetFormatterWithVerify<Color>().Serialize(ref writer, value.customSecColor, options);
			writer.Write(value.indexConfig);
		}

		public CarPaintHolderProxy Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			if (reader.TryReadNil())
			{
				throw new InvalidOperationException("typecode is null, struct not supported");
			}
			options.Security.DepthStep(ref reader);
			IFormatterResolver resolver = options.Resolver;
			int num = reader.ReadArrayHeader();
			CarPaintHolderProxy result = default(CarPaintHolderProxy);
			for (int i = 0; i < num; i++)
			{
				switch (i)
				{
				case 0:
					result.type = resolver.GetFormatterWithVerify<CarVisualManager.CustomizationFlags>().Deserialize(ref reader, options);
					break;
				case 1:
					result.id = reader.ReadInt32();
					break;
				case 2:
					result.color = resolver.GetFormatterWithVerify<Color>().Deserialize(ref reader, options);
					break;
				case 3:
					result.specColor = resolver.GetFormatterWithVerify<Color>().Deserialize(ref reader, options);
					break;
				case 4:
					result.duoColor = resolver.GetFormatterWithVerify<Color>().Deserialize(ref reader, options);
					break;
				case 5:
					result.customSecColor = resolver.GetFormatterWithVerify<Color>().Deserialize(ref reader, options);
					break;
				case 6:
					result.indexConfig = reader.ReadInt32();
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
