using System;
using System.Collections.Generic;
using Binding.Base;
using UnityEngine;

public class UIEngineSwapFilter : BaseBindingBehaviourTarget
{
	public Action<EngineInfo.Type> onCategorySelected;

	[SerializeField]
	private UIEngineSwapFilterCategory m_prefab;

	[SerializeField]
	private Transform m_categoriesRoot;

	private List<UIEngineSwapFilterCategory> m_filterCategories = new List<UIEngineSwapFilterCategory>();

	public static EngineInfo.Type allCategory { get; private set; }

	public int? selectedIndex { get; private set; }

	private void Awake()
	{
		BuildCategories();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_filterCategories.ForEach(delegate(UIEngineSwapFilterCategory category)
		{
			category.onClick = (Action<UIEngineSwapFilterCategory>)Delegate.Remove(category.onClick, new Action<UIEngineSwapFilterCategory>(OnClickCategoryHandler));
		});
	}

	private void BuildCategories()
	{
		foreach (EngineInfo.Type value in Enum.GetValues(typeof(EngineInfo.Type)))
		{
			m_filterCategories.Add(InstantiateCategory(value));
			allCategory |= value;
		}
		BuildCategoryWithAllEngines();
	}

	private void BuildCategoryWithAllEngines()
	{
		UIEngineSwapFilterCategory uIEngineSwapFilterCategory = InstantiateCategory(allCategory);
		uIEngineSwapFilterCategory.gameObject.transform.SetAsFirstSibling();
		uIEngineSwapFilterCategory.SetActive(value: true);
		m_filterCategories.Insert(0, uIEngineSwapFilterCategory);
	}

	private UIEngineSwapFilterCategory InstantiateCategory(EngineInfo.Type categoryType)
	{
		UIEngineSwapFilterCategory uIEngineSwapFilterCategory = UnityEngine.Object.Instantiate(m_prefab, m_categoriesRoot);
		uIEngineSwapFilterCategory.Initialize(categoryType);
		uIEngineSwapFilterCategory.onClick = (Action<UIEngineSwapFilterCategory>)Delegate.Combine(uIEngineSwapFilterCategory.onClick, new Action<UIEngineSwapFilterCategory>(OnClickCategoryHandler));
		return uIEngineSwapFilterCategory;
	}

	public void SetCategoryActive(EngineInfo.Type category, bool value)
	{
		UIEngineSwapFilterCategory uIEngineSwapFilterCategory = m_filterCategories.Find((UIEngineSwapFilterCategory item) => item.type == category);
		if (uIEngineSwapFilterCategory != null)
		{
			uIEngineSwapFilterCategory.SetActive(value);
		}
	}

	public void SetAllCategoriesActive(bool value, bool exceptGeneralCategory = true)
	{
		m_filterCategories.ForEach(delegate(UIEngineSwapFilterCategory item)
		{
			if (!exceptGeneralCategory || item.type != allCategory)
			{
				item.SetActive(value);
			}
		});
	}

	public void SelectFirstAvailableCategory()
	{
		for (int i = 0; i < m_filterCategories.Count && !TrySelectAvailableCategory(m_filterCategories[i]); i++)
		{
		}
	}

	public void OnSwitchNextFilterCategory()
	{
		for (int i = selectedIndex.GetValueOrDefault() + 1; i <= m_filterCategories.Count - 1 && !TrySelectAvailableCategory(m_filterCategories[i]); i++)
		{
		}
	}

	public void OnSwitchPrevFilterCategory()
	{
		int num = selectedIndex.GetValueOrDefault() - 1;
		while (num >= 0 && !TrySelectAvailableCategory(m_filterCategories[num]))
		{
			num--;
		}
	}

	private bool TrySelectAvailableCategory(UIEngineSwapFilterCategory targetCategory)
	{
		if (targetCategory.isActiveAndEnabled)
		{
			SelectCategory(targetCategory);
			return true;
		}
		return false;
	}

	public void ClearSelection()
	{
		m_filterCategories.ForEach(delegate(UIEngineSwapFilterCategory category)
		{
			category.SetSelected(value: false);
		});
		selectedIndex = null;
	}

	private void SelectCategory(UIEngineSwapFilterCategory targetCategory)
	{
		int num = m_filterCategories.IndexOf(targetCategory);
		if (selectedIndex != num)
		{
			if (selectedIndex.HasValue)
			{
				m_filterCategories[selectedIndex.Value].SetSelected(value: false);
			}
			selectedIndex = num;
			targetCategory.SetSelected(value: true);
			onCategorySelected(targetCategory.type);
		}
	}

	private void OnClickCategoryHandler(UIEngineSwapFilterCategory category)
	{
		SoundController.Play2DSound(SoundSample.DefaultBtn);
		SelectCategory(category);
	}
}
