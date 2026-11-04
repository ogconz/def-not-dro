using System;
using UnityEngine;
using UnityEngine.UI;

public class BaseListSettingsItem : BaseListItem
{
	private const int WIDTH_NARROW = 660;

	public string title;

	public string description;

	public Sprite sprite;

	public Button prevBtn;

	public Button nextBtn;

	public bool expanded;

	public Action onClick;

	public Action onDefaultValue;

	public Action onChangedValue;

	private IListView<BaseListSettingsItem> m_list;

	public bool canNext { get; protected set; }

	public bool canPrev { get; protected set; }

	public virtual string GetCurrentValueTitle()
	{
		return "N/A";
	}

	public virtual float GetTrackValue()
	{
		return 0f;
	}

	public void SetNarrowItem()
	{
		width = 660f;
	}

	public override void OnClick()
	{
		base.OnClick();
		onClick.SafeCall();
	}

	public virtual void NextValue()
	{
		onChangedValue.SafeCall();
	}

	public virtual void PrevValue()
	{
		onChangedValue.SafeCall();
	}

	public virtual void OnChangeValue()
	{
		onChangedValue.SafeCall();
	}

	public virtual float GetMin()
	{
		return 0f;
	}

	public virtual float GetMax()
	{
		return 0f;
	}

	public virtual void SetTrackValue(float newValue)
	{
	}

	public virtual void SelectedMoveLeft()
	{
		PrevValue();
	}

	public virtual void SelectedMoveRight()
	{
		NextValue();
	}

	public virtual void ResetToDefault()
	{
		onDefaultValue.SafeCall();
		OnChangeValue();
	}

	public virtual void StoreInitialValue()
	{
	}

	public virtual void ReturnToInitialValue()
	{
	}
}
