using System.Collections.Generic;

namespace MessagePack.Formatters
{
	public sealed class VinylLayerProxyContainerFormatter : IMessagePackFormatter<VinylLayerProxyContainer>, IMessagePackFormatter
	{
		public void Serialize(ref MessagePackWriter writer, VinylLayerProxyContainer value, MessagePackSerializerOptions options)
		{
			if (value == null)
			{
				writer.WriteNil();
				return;
			}
			IFormatterResolver resolver = options.Resolver;
			writer.WriteArrayHeader(1);
			resolver.GetFormatterWithVerify<List<VinylLayerProxy>>().Serialize(ref writer, value.vinylsProxyList, options);
		}

		public VinylLayerProxyContainer Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			if (reader.TryReadNil())
			{
				return null;
			}
			options.Security.DepthStep(ref reader);
			IFormatterResolver resolver = options.Resolver;
			int num = reader.ReadArrayHeader();
			VinylLayerProxyContainer vinylLayerProxyContainer = new VinylLayerProxyContainer();
			for (int i = 0; i < num; i++)
			{
				if (i == 0)
				{
					vinylLayerProxyContainer.vinylsProxyList = resolver.GetFormatterWithVerify<List<VinylLayerProxy>>().Deserialize(ref reader, options);
				}
				else
				{
					reader.Skip();
				}
			}
			reader.Depth--;
			return vinylLayerProxyContainer;
		}
	}
}
