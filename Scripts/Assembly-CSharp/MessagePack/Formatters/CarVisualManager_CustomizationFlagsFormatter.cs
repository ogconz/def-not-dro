namespace MessagePack.Formatters
{
	public sealed class CarVisualManager_CustomizationFlagsFormatter : IMessagePackFormatter<CarVisualManager.CustomizationFlags>, IMessagePackFormatter
	{
		public void Serialize(ref MessagePackWriter writer, CarVisualManager.CustomizationFlags value, MessagePackSerializerOptions options)
		{
			writer.Write((long)value);
		}

		public CarVisualManager.CustomizationFlags Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			return (CarVisualManager.CustomizationFlags)reader.ReadInt64();
		}
	}
}
