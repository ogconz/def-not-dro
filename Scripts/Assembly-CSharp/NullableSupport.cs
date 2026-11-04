public class NullableSupport<T> where T : struct
{
	public bool hasValue;

	public T value;

	public void InitFrom(T? v)
	{
		hasValue = v.HasValue;
		if (hasValue)
		{
			value = v.Value;
		}
	}

	public T? GetNullable()
	{
		if (!hasValue)
		{
			return null;
		}
		return value;
	}
}
