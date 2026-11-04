using System;
using Binding;
using Binding.Base;
using GameInput;
using UnityEngine;

[GameHotkeyBlocker(true)]
[RaycastBlocker(true)]
public class UISharingRateContext : BaseBindingBehaviourTarget
{
	public Action<int> onRateDone;

	private const int MAX_STAR_COUNT = 5;

	private const int RATE_STEP = 1;

	private IntProperty m_selectedStar = new IntProperty();

	private IntProperty m_selectedIndex = new IntProperty();

	private StringProperty m_title = new StringProperty();

	protected override void OnActivate()
	{
		base.OnActivate();
		m_selectedStar.value = 4;
		m_selectedIndex.value = 0;
	}

	public void SetTitle(string title)
	{
		m_title.value = title;
	}

	public void SetStarRating(int rating)
	{
		m_selectedStar.value = rating;
		SetSelectedItem(0);
	}

	public void SetSelectedItem(int idx)
	{
		m_selectedIndex.value = idx;
	}

	[GameHotkey(ActionType.NavLeft, false, false)]
	public void NavigateLeft()
	{
		if (m_selectedIndex.value > 0)
		{
			m_selectedIndex.value--;
			m_selectedIndex.value = Mathf.Clamp(m_selectedIndex.value, 1, 2);
			return;
		}
		m_selectedStar.value--;
		if (m_selectedStar.value < 1)
		{
			m_selectedStar.value = 1;
		}
	}

	[GameHotkey(ActionType.NavRight, false, false)]
	public void NavigateRight()
	{
		if (m_selectedIndex.value > 0)
		{
			m_selectedIndex.value++;
			m_selectedIndex.value = Mathf.Clamp(m_selectedIndex.value, 1, 2);
			return;
		}
		m_selectedStar.value++;
		if (m_selectedStar.value > 5)
		{
			m_selectedStar.value = 5;
		}
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavigationDown()
	{
		if (m_selectedIndex.value <= 0)
		{
			m_selectedIndex.value++;
		}
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavigationUp()
	{
		if (m_selectedIndex.value != 0)
		{
			m_selectedIndex.value = 0;
		}
	}

	[GameHotkey(ActionType.NavSelectBtnA, false, false)]
	public void OnClick()
	{
		switch (m_selectedIndex.value)
		{
		case 0:
			m_selectedIndex.value = 1;
			break;
		case 1:
			Rate();
			break;
		default:
			Hide();
			break;
		}
	}

	[GameHotkey(ActionType.NavCancelBtnB, false, false)]
	public void OnClickCancel()
	{
		Hide();
	}

	public void Rate()
	{
		onRateDone.SafeCall(m_selectedStar.value);
		Hide();
	}
}
