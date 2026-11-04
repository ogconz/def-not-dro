using System.Collections.Generic;

namespace MessagePack.Formatters
{
	public sealed class ProfilesInfo_ProfileFormatter : IMessagePackFormatter<ProfilesInfo.Profile>, IMessagePackFormatter
	{
		public void Serialize(ref MessagePackWriter writer, ProfilesInfo.Profile value, MessagePackSerializerOptions options)
		{
			if (value == null)
			{
				writer.WriteNil();
				return;
			}
			IFormatterResolver resolver = options.Resolver;
			writer.WriteArrayHeader(4);
			writer.Write(value.id);
			writer.Write(value.forceDefault);
			resolver.GetFormatterWithVerify<Dictionary<string, float>>().Serialize(ref writer, value.controllers, options);
			resolver.GetFormatterWithVerify<Dictionary<string, float>>().Serialize(ref writer, value.values, options);
		}

		public ProfilesInfo.Profile Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
		{
			if (reader.TryReadNil())
			{
				return null;
			}
			options.Security.DepthStep(ref reader);
			IFormatterResolver resolver = options.Resolver;
			int num = reader.ReadArrayHeader();
			int profileId = 0;
			bool forceDefault = false;
			Dictionary<string, float> controllers = null;
			Dictionary<string, float> values = null;
			for (int i = 0; i < num; i++)
			{
				switch (i)
				{
				case 0:
					profileId = reader.ReadInt32();
					break;
				case 1:
					forceDefault = reader.ReadBoolean();
					break;
				case 2:
					controllers = resolver.GetFormatterWithVerify<Dictionary<string, float>>().Deserialize(ref reader, options);
					break;
				case 3:
					values = resolver.GetFormatterWithVerify<Dictionary<string, float>>().Deserialize(ref reader, options);
					break;
				default:
					reader.Skip();
					break;
				}
			}
			ProfilesInfo.Profile profile = new ProfilesInfo.Profile(profileId);
			if (num > 1)
			{
				profile.forceDefault = forceDefault;
				if (num > 2)
				{
					profile.controllers = controllers;
					if (num > 3)
					{
						profile.values = values;
					}
				}
			}
			reader.Depth--;
			return profile;
		}
	}
}
