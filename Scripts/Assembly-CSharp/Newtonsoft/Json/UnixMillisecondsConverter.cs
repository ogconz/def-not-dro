using System;

namespace Newtonsoft.Json
{
	public class UnixMillisecondsConverter : JsonConverter<DateTime>
	{
		public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return DateTimeOffset.FromUnixTimeMilliseconds((long)reader.Value).UtcDateTime;
		}

		public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
		{
			long value2 = new DateTimeOffset(value).ToUnixTimeMilliseconds();
			writer.WriteValue(value2);
		}
	}
}
