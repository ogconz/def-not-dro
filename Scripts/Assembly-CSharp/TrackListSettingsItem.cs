using UnityEngine;

public class TrackListSettingsItem : BaseListSettingsItem
{
	private float m_initialValue;

	public float normalizedValue { get; private set; }

	public float value
	{
		get
		{
			return min + (max - min) * normalizedValue;
		}
		set
		{
			normalizedValue = Mathf.Clamp01((value - min) / (max - min));
		}
	}

	public float min { get; private set; }

	public float max { get; private set; }

	public float step { get; private set; }

	public string format { get; private set; }

	public TrackListSettingsItem()
	{
		min = 0f;
		max = 1f;
		step = 0.1f;
		format = "{0}";
	}

	public TrackListSettingsItem(float min, float max, float step, string format = "{0:F0}")
	{
		this.min = min;
		this.max = max;
		this.step = step;
		this.format = format;
	}

	public override string GetCurrentValueTitle()
	{
		return string.Format(format, value);
	}

	public override float GetTrackValue()
	{
		return normalizedValue;
	}

	public override void SetTrackValue(float newValue)
	{
		normalizedValue = newValue;
		OnChangeValue();
	}

	public override float GetMax()
	{
		return max;
	}

	public override float GetMin()
	{
		return min;
	}

	public override void OnChangeValue()
	{
		base.OnChangeValue();
	}

	public override void NextValue()
	{
		value += step;
		base.NextValue();
	}

	public override void PrevValue()
	{
		value -= step;
		base.PrevValue();
	}

	public override void StoreInitialValue()
	{
		m_initialValue = value;
	}

	public override void ReturnToInitialValue()
	{
		value = m_initialValue;
	}
}
