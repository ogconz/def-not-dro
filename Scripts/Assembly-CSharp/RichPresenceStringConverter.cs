public class RichPresenceStringConverter : IRichPresenceConverter<string>, IRichPresenceConverter
{
	public bool TryConvertFrom(string obj, out string result)
	{
		result = obj;
		return true;
	}
}
