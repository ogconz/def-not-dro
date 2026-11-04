public class CommitableVariable<T>
{
	public T prevVal;

	public T curVal;

	public T value
	{
		get
		{
			return prevVal;
		}
		set
		{
			curVal = value;
		}
	}

	public void Commit()
	{
		prevVal = curVal;
	}

	public void Discard()
	{
		curVal = prevVal;
	}
}
