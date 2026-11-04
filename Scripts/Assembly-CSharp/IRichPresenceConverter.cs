public interface IRichPresenceConverter<T> : IRichPresenceConverter
{
	bool TryConvertFrom(T format, out string result);
}
public interface IRichPresenceConverter
{
}
