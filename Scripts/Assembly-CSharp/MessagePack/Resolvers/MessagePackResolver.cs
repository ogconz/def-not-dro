using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
	public class MessagePackResolver : IFormatterResolver
	{
		private static class FormatterCache<T>
		{
			internal static readonly IMessagePackFormatter<T> Formatter;

			static FormatterCache()
			{
				object formatter = MessagePackResolverGetFormatterHelper.GetFormatter(typeof(T));
				if (formatter != null)
				{
					Formatter = (IMessagePackFormatter<T>)formatter;
				}
			}
		}

		public static readonly IFormatterResolver Instance = new MessagePackResolver();

		private MessagePackResolver()
		{
		}

		public IMessagePackFormatter<T> GetFormatter<T>()
		{
			return FormatterCache<T>.Formatter;
		}
	}
}
