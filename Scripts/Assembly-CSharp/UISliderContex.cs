using GameInput;
using UnityEngine;
using UnityEngine.UI;

public class UISliderContex : MonoBehaviour
{
	private UISmoothSlideTrait m_smoothSlideTrait;

	public ActionType actionLeft = ActionType.NavLeft;

	public ActionType actionRight = ActionType.NavRight;

	public Slider slider { get; private set; }

	private void Awake()
	{
		slider = GetComponentInChildren<Slider>(includeInactive: true);
		m_smoothSlideTrait = new UISmoothSlideTrait(() => slider.normalizedValue, delegate(float value)
		{
			slider.normalizedValue = value;
		});
		m_smoothSlideTrait.actionLeft = actionLeft;
		m_smoothSlideTrait.actionRight = actionRight;
		m_smoothSlideTrait.multiInputMode = false;
	}

	private void OnDisable()
	{
		if (UISmoothSlideTrait.activeInstance == m_smoothSlideTrait)
		{
			UISmoothSlideTrait.activeInstance = null;
		}
	}

	public void Select()
	{
		UISmoothSlideTrait.activeInstance = m_smoothSlideTrait;
	}

	public void Update()
	{
		if (m_smoothSlideTrait != null)
		{
			m_smoothSlideTrait.FixedUpdate();
		}
	}
}
