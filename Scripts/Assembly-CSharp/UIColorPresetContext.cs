using Binding;
using Binding.Base;
using UnityEngine;

public class UIColorPresetContext : BaseBindingBehaviourTarget
{
	public readonly Property<bool> selected = new BoolProperty();

	public readonly Property<Color> mainColor = new ColorProperty();

	public readonly Property<Color> layerOneColor = new ColorProperty();

	public readonly Property<Color> layerTwoColor = new ColorProperty();

	public readonly Property<bool> isEmpty = new BoolProperty(startValue: true);

	private UIPaintCustomizationContext m_context;

	private int m_index;

	private bool m_favoriteColorsCategorySelected;

	public CarColorFavorite color { get; private set; }

	public void SetData(UIPaintCustomizationContext context, int index)
	{
		m_context = context;
		m_index = index;
	}

	public void SetColor(CarColorFavorite newColor)
	{
		isEmpty.value = false;
		color = newColor;
		mainColor.value = color.colorMain;
		layerOneColor.value = color.colorSpecular;
		layerTwoColor.value = color.colorDuo;
	}

	public void OnPresetSelect()
	{
		if (!m_context.lockPartSelection)
		{
			m_favoriteColorsCategorySelected = m_context.GetCategoryNavMode() == UIPaintCustomizationContext.CategoryNavMode.FavoriteColors;
			m_context.SelectItemInCategory(m_index, UIPaintCustomizationContext.CategoryNavMode.FavoriteColors);
		}
	}

	public void OnPresetSubmitted()
	{
		if (!isEmpty.value && m_favoriteColorsCategorySelected)
		{
			m_context.ApplyFavoriteColor(m_index);
		}
	}
}
