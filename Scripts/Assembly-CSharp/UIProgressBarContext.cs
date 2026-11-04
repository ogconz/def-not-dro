using Binding;
using UnityEngine;
using UnityEngine.UI;

[GameHotkeyBlocker(true)]
[RaycastBlocker(true)]
public class UIProgressBarContext : UIBaseDialogContext
{
	public readonly BoolProperty showBG = new BoolProperty();

	[SerializeField]
	private Image m_progressBar;

	[SerializeField]
	private StringProperty m_percent = new StringProperty("0%");

	private readonly BoolProperty m_showProgressLine = new BoolProperty();

	private readonly BoolProperty m_showSpinner = new BoolProperty();

	private float m_prevProgressValue;

	protected override void OnActivate()
	{
		base.OnActivate();
		m_prevProgressValue = 0f;
		m_showSpinner.value = true;
	}

	public void FillProgressBar(float value)
	{
		if (base.isActive)
		{
			m_showSpinner.value = value <= 0f;
			m_showProgressLine.value = value > 0f;
			float num = value * 100f;
			if (!(value <= m_prevProgressValue))
			{
				m_percent.value = $"{num: 0}%";
				m_prevProgressValue = value;
				m_progressBar.fillAmount = value;
			}
		}
	}
}
