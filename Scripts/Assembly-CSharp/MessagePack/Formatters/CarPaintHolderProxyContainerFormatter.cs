using System.Collections.Generic;

namespace MessagePack.Formatters
{
	public sealed class CarPaintHolderProxyContainerFormatter : IMessagePackFormatter<CarPaintHolderProxyContainer>, IMessagePackFormatter
	{
		public void Serialize(ref MessagePackWriter writer, CarPaintHolderProxyContainer value, MessagePackSerializerOptions options)
		{
			if (value == null)
			{
				writer.WriteNil();
				return;
			}
			IFormatterResolver resolver = options.Resolver;
			writer.WriteArrayHeader(1);
			resolver.GetFormatterWithVerify<List<CarPaintHolderProxy>>().Serialize(ref writer, value.carPaintHolderProxyList, options);
		}

		public CarPaintHolderProxyContainer Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			if (reader.TryReadNil())
			{
				return null;
			}
			options.Security.DepthStep(ref reader);
			IFormatterResolver resolver = options.Resolver;
			int num = reader.ReadArrayHeader();
			CarPaintHolderProxyContainer carPaintHolderProxyContainer = new CarPaintHolderProxyContainer();
			for (int i = 0; i < num; i++)
			{
				if (i == 0)
				{
					carPaintHolderProxyContainer.carPaintHolderProxyList = resolver.GetFormatterWithVerify<List<CarPaintHolderProxy>>().Deserialize(ref reader, options);
				}
				else
				{
					reader.Skip();
				}
			}
			reader.Depth--;
			return carPaintHolderProxyContainer;
		}
	}
}
