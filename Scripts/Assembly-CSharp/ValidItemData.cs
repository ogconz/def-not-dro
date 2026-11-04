using System;
using System.Collections.Generic;

[Serializable]
public struct ValidItemData : ICloneable
{
	public List<ValidItem> data;

	public readonly float maxSizeInMb;

	public readonly float maxSizeInMbMeta;

	public ValidItemData(float maxSizeInMb = 512f, float maxSizeInMbMeta = 24f, params ValidItem[] data)
	{
		this.maxSizeInMb = maxSizeInMb;
		this.maxSizeInMbMeta = maxSizeInMbMeta;
		this.data = new List<ValidItem>(data);
	}

	public override string ToString()
	{
		string text = string.Empty;
		if (data != null)
		{
			foreach (ValidItem datum in data)
			{
				if (datum.current != 0)
				{
					text = text + datum.ToStat() + "\n";
				}
			}
		}
		return text;
	}

	public object Clone()
	{
		return new ValidItemData(maxSizeInMb, maxSizeInMbMeta, (ValidItem[])data.ToArray().Clone());
	}
}
