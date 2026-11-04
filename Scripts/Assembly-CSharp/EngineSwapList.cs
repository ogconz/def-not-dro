using System;
using System.Collections.Generic;
using UnityEngine;

public class EngineSwapList : ListView<EngineSwapListItem, EngineSwapListItemController>
{
	[SerializeField]
	private UIEngineSwapFilter m_engineSwapFilter;

	private List<EngineSwapListItem> m_cachedListItems = new List<EngineSwapListItem>();

	protected override void Awake()
	{
		base.Awake();
		UIEngineSwapFilter engineSwapFilter = m_engineSwapFilter;
		engineSwapFilter.onCategorySelected = (Action<EngineInfo.Type>)Delegate.Combine(engineSwapFilter.onCategorySelected, new Action<EngineInfo.Type>(OnFilterCategorySelectedHandler));
	}

	protected override void OnDestroy()
	{
		UIEngineSwapFilter engineSwapFilter = m_engineSwapFilter;
		engineSwapFilter.onCategorySelected = (Action<EngineInfo.Type>)Delegate.Remove(engineSwapFilter.onCategorySelected, new Action<EngineInfo.Type>(OnFilterCategorySelectedHandler));
		base.OnDestroy();
	}

	public EngineSwapListItem GetEngineById(int id)
	{
		return base.items.Find((EngineSwapListItem engine) => engine.info.id == id);
	}

	public int GetEngineIndexById(int id)
	{
		return IndexOf(GetEngineById(id));
	}

	public void ShowFirstAvailableCategory()
	{
		m_engineSwapFilter.SelectFirstAvailableCategory();
	}

	public override void AddItem(EngineSwapListItem item, bool safeScroll = false)
	{
		base.AddItem(item, safeScroll);
		m_cachedListItems.Add(item);
		m_engineSwapFilter.SetCategoryActive(item.info.type, value: true);
	}

	public override void RemoveItem(EngineSwapListItem item, bool shouldScroll = true)
	{
		base.RemoveItem(item, shouldScroll);
		m_cachedListItems.Remove(item);
		if (!m_cachedListItems.Exists((EngineSwapListItem existedItem) => !existedItem.isStock && existedItem.info.type == item.info.type))
		{
			m_engineSwapFilter.SetCategoryActive(item.info.type, value: false);
		}
	}

	public override void RemoveItem(int index, bool shouldScroll = true)
	{
		RemoveItem(base.items[index]);
	}

	public override void Clear()
	{
		base.Clear();
		m_cachedListItems.Clear();
		m_engineSwapFilter.SetAllCategoriesActive(value: false);
		m_engineSwapFilter.ClearSelection();
	}

	private void OnFilterCategorySelectedHandler(EngineInfo.Type filterCategory)
	{
		UpdateListByCategory(filterCategory);
	}

	private void UpdateListByCategory(EngineInfo.Type filterCategory)
	{
		FillEngineListByCategory(filterCategory);
		InvalidateSelection();
		TrySelectInstalledEngine();
		UpdateList();
		RecalcList();
		ScrollTo(selectedIndex);
	}

	private void FillEngineListByCategory(EngineInfo.Type filterCategory)
	{
		base.items.Clear();
		base.items.AddRange(m_cachedListItems.FindAll((EngineSwapListItem engine) => filterCategory.HasFlag(engine.info.type)));
	}

	private void TrySelectInstalledEngine()
	{
		int num = base.items.FindIndex((EngineSwapListItem engine) => engine.IsInstalled());
		selectedIndex = ((num >= 0) ? num : 0);
	}

	private void InvalidateSelection()
	{
		selectedIndex = -1;
	}
}
