using System;
using Binding;
using Binding.Base;

public class UIEngineSwapFilterCategory : BaseBindingBehaviourTarget
{
	public Action<UIEngineSwapFilterCategory> onClick;

	private StringProperty m_text = new StringProperty();

	private BoolProperty m_selected = new BoolProperty();

	private BoolProperty m_visible = new BoolProperty();

	public EngineInfo.Type type { get; private set; }

	public void Initialize(EngineInfo.Type type)
	{
		this.type = type;
		Localize();
	}

	private void Localize()
	{
		if (type == UIEngineSwapFilter.allCategory)
		{
			m_text.value = LocalizationKeys.engine_swap_category_all.Localized();
			return;
		}
		StringProperty text = m_text;
		text.value = type switch
		{
			EngineInfo.Type.Opposite => LocalizationKeys.engine_swap_category_opposite.Localized(), 
			EngineInfo.Type.Rotary => LocalizationKeys.engine_swap_category_rotary.Localized(), 
			EngineInfo.Type.Other => LocalizationKeys.engine_swap_category_other.Localized(), 
			_ => Enum.GetName(typeof(EngineInfo.Type), type), 
		};
	}

	public void SetActive(bool value)
	{
		m_visible.value = value;
	}

	public void SetSelected(bool value)
	{
		m_selected.value = value;
	}

	public void OnClick()
	{
		onClick?.SafeCall(this);
	}
}
