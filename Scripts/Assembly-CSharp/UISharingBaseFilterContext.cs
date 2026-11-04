using System;
using Binding;
using Binding.Base;
using GameInput;
using UnityEngine;

public class UISharingBaseFilterContext : BaseBindingBehaviourTarget
{
	protected const int STAR_COUNT = 5;

	protected const string STAR_CODE = "✫";

	protected const char FILLED_STAR_CODE = '⭐';

	protected const string MATH_SIGN_MORE = "> ";

	public System.Action onApplyFilter;

	protected readonly IntProperty m_selectedIndex = new IntProperty();

	[SerializeField]
	protected CreateRoomParamsList m_paramsList;

	protected SharingFilterController m_sharingController;

	protected ISharingFilter m_sharingFilterSnap;

	public virtual void InitFilter(SharingFilterController filterController)
	{
		m_paramsList.RecalcList();
		m_paramsList.selectedIndex = 0;
		SubscribeItems(ResetSelectedIndex);
	}

	protected T GetInitFilterValue<K, T>(K filterType)
	{
		return m_sharingController.GetFilterValue<K, T>(filterType);
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		BringForwardByDepth();
		m_paramsList.Clear();
		ResetSelectedIndex();
	}

	protected override void OnDeactivate()
	{
		SubscribeItems(null);
		base.OnDeactivate();
	}

	protected void UpdateParamList()
	{
		m_paramsList.UpdateList();
	}

	public void OnApplyFilter()
	{
		SaveAndApplyFilter(resetFilter: false);
	}

	public void OnResetFilter()
	{
		SaveAndApplyFilter(resetFilter: true);
	}

	private void SaveAndApplyFilter(bool resetFilter)
	{
		if (resetFilter)
		{
			m_sharingController.ResetFilterToDefault();
		}
		m_sharingController.SaveFilter<ISharingFilter>();
		onApplyFilter.SafeCall();
		Hide();
	}

	protected void InitListItem<T>(ref T item, string title, int startIndex, System.Action onChangeValueHandler, System.Action onClick = null, params object[] items) where T : BaseListSettingsItem
	{
		if (item == null)
		{
			item = Activator.CreateInstance<T>();
		}
		item.title = title;
		T val = item;
		if (!(val is SwitchListSettingsItem switchItem))
		{
			if (!(val is ToggleListSettingsItem toggleItem))
			{
				if (val is HeaderListSettingsItem headerItem)
				{
					SetHeaderItem(headerItem);
				}
			}
			else
			{
				SetToggleItem(toggleItem);
			}
		}
		else
		{
			SetSwitchItem(switchItem);
		}
		T val2 = item;
		val2.onClick = (System.Action)Delegate.Combine(val2.onClick, onClick);
		T val3 = item;
		val3.onChangedValue = (System.Action)Delegate.Combine(val3.onChangedValue, onChangeValueHandler);
		item.OnChangeValue();
		m_paramsList.AddItem(item);
		void SetHeaderItem(HeaderListSettingsItem headerListSettingsItem)
		{
			if (items != null && items.Length != 0)
			{
				headerListSettingsItem.description = items[0].ToString();
			}
		}
		void SetSwitchItem(SwitchListSettingsItem switchListSettingsItem)
		{
			switchListSettingsItem.list.Clear();
			for (int i = 0; i < items?.Length; i++)
			{
				switchListSettingsItem.list.Add(items[i]);
			}
			switchListSettingsItem.index = startIndex;
		}
		void SetToggleItem(ToggleListSettingsItem toggleListSettingsItem)
		{
			toggleListSettingsItem.value = startIndex == 0;
		}
	}

	protected void ResetSelectedIndex()
	{
		m_selectedIndex.value = -1;
	}

	protected void SubscribeItems(System.Action handler)
	{
		if (m_paramsList == null || m_paramsList.items == null)
		{
			return;
		}
		foreach (BaseListSettingsItem item in m_paramsList.items)
		{
			if (handler != null)
			{
				item.onClick = (System.Action)Delegate.Combine(item.onClick, handler);
				continue;
			}
			item.onClick = null;
			item.onChangedValue = null;
		}
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavigationUp()
	{
		int count = m_paramsList.GetCount();
		if (m_paramsList.selectedIndex < 0)
		{
			m_paramsList.selectedIndex = count - 1;
			ResetSelectedIndex();
			return;
		}
		m_paramsList.selectedIndex--;
		if (m_paramsList.selectedIndex < 0)
		{
			SelectApplyButton();
		}
		else
		{
			UpdateParamList();
		}
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavigationDown()
	{
		int selectedIndex = m_paramsList.selectedIndex;
		if (selectedIndex < 0)
		{
			m_paramsList.selectedIndex = 0;
			ResetSelectedIndex();
			return;
		}
		int count = m_paramsList.GetCount();
		if (selectedIndex == count - 1)
		{
			SelectApplyButton();
			return;
		}
		m_paramsList.selectedIndex++;
		UpdateParamList();
	}

	private void SelectApplyButton()
	{
		m_selectedIndex.value = 0;
		m_paramsList.selectedIndex = -1;
	}

	[GameHotkey(ActionType.NavRight, false, false)]
	public void NavigationRight()
	{
		if (m_selectedIndex.value > -1)
		{
			m_selectedIndex.value++;
			m_selectedIndex.value = (int)Mathf.Clamp01(m_selectedIndex.value);
		}
		else
		{
			m_paramsList.GetSelectedItem()?.NextValue();
		}
	}

	[GameHotkey(ActionType.NavLeft, false, false)]
	public void NavigationLeft()
	{
		if (m_selectedIndex.value > -1)
		{
			m_selectedIndex.value--;
			m_selectedIndex.value = (int)Mathf.Clamp01(m_selectedIndex.value);
		}
		else
		{
			m_paramsList.GetSelectedItem()?.PrevValue();
		}
	}

	[GameHotkey(ActionType.NavSelectBtnA, false, false)]
	public void NavigationSubmit()
	{
		switch (m_selectedIndex.value)
		{
		case 0:
			OnApplyFilter();
			break;
		case 1:
			OnResetFilter();
			break;
		default:
			m_paramsList.GetSelectedItem()?.onClick.SafeCall();
			break;
		}
	}

	[GameHotkey(ActionType.NavCancelBtnB, false, false)]
	public void NavigationClose()
	{
		m_sharingController.ResetFilterTo(m_sharingFilterSnap);
		m_sharingController.SaveFilter<ISharingFilter>();
		Hide();
	}
}
