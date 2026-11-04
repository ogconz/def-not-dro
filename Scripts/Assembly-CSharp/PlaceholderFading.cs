using Binding;
using Binding.Base;

public class PlaceholderFading : BaseBindingTarget
{
	private readonly BoolProperty m_normalPlaceholder = new BoolProperty(startValue: true);

	private readonly BoolProperty m_hoverPlaceholder = new BoolProperty();

	private readonly BoolProperty m_downPlaceholder = new BoolProperty();

	private bool m_hoverState;

	private bool m_downState;

	public void SetHover(bool value)
	{
		m_hoverState = value;
		UpdatePlaceholders();
	}

	public void SetDown(bool value)
	{
		m_downState = value;
		UpdatePlaceholders();
	}

	private void UpdatePlaceholders()
	{
		m_normalPlaceholder.value = !m_hoverState && !m_downState;
		m_hoverPlaceholder.value = m_hoverState && !m_downState;
		m_downPlaceholder.value = m_downState;
	}
}
