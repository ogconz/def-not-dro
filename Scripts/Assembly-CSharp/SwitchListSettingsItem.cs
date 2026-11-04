using System.Collections.Generic;
using UnityEngine;

public class SwitchListSettingsItem : BaseListSettingsItem
{
	public readonly List<object> list = new List<object>();

	private int m_index;

	private bool m_ignoreCheck;

	private int m_initialIndex;

	public bool isEnableToEditing { get; private set; } = true;

	public int index
	{
		get
		{
			return m_index;
		}
		set
		{
			if (value >= 0 && value < list.Count)
			{
				m_index = value;
			}
		}
	}

	public int forceIndex
	{
		get
		{
			return m_index;
		}
		set
		{
			m_index = value;
		}
	}

	public object selectedItem
	{
		get
		{
			if (list.Count != 0 && m_index >= 0 && m_index < list.Count)
			{
				return list[m_index];
			}
			return null;
		}
	}

	private void CheckSwitcher()
	{
		base.canNext = true;
		base.canPrev = true;
		if (list.Count == 0 || list.Count == 1)
		{
			index = 0;
			base.canNext = false;
			base.canPrev = false;
		}
		else if (index >= list.Count - 1)
		{
			index = list.Count - 1;
			base.canNext = false;
			base.canPrev = true;
		}
		else if (index <= 0)
		{
			index = 0;
			base.canNext = true;
			base.canPrev = false;
		}
	}

	public override float GetTrackValue()
	{
		if (index <= 0 || list.Count == 0)
		{
			return 0f;
		}
		return (float)Mathf.Clamp(index, 0, list.Count) / (float)list.Count;
	}

	public void ForceEnableNextElement()
	{
		base.canNext = true;
		base.canPrev = true;
		m_ignoreCheck = true;
	}

	public override string GetCurrentValueTitle()
	{
		if (!m_ignoreCheck && isEnableToEditing)
		{
			CheckSwitcher();
		}
		if (list.Count <= 0)
		{
			return "N/A";
		}
		return list[index].ToString();
	}

	public override void NextValue()
	{
		if (isEnableToEditing)
		{
			index++;
			CheckSwitcher();
			base.NextValue();
		}
	}

	public override void PrevValue()
	{
		if (isEnableToEditing)
		{
			index--;
			CheckSwitcher();
			base.PrevValue();
		}
	}

	public override void StoreInitialValue()
	{
		m_initialIndex = m_index;
	}

	public override void ReturnToInitialValue()
	{
		index = m_initialIndex;
	}

	public void SetBlockToEditing(bool value)
	{
		isEnableToEditing = !value;
		if (!isEnableToEditing)
		{
			base.canNext = false;
			base.canPrev = false;
		}
		else
		{
			CheckSwitcher();
		}
	}
}
