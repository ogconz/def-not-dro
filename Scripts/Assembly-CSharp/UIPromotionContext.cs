using Binding.Base;
using GameInput;
using UI.PromotionWidget;
using UnityEngine;

public class UIPromotionContext : BaseBindingBehaviourTarget
{
	private static UIPromotionContext s_localInstance;

	[SerializeField]
	private Widget widget;

	private bool m_isAlreadyActivated;

	protected override void OnActivate()
	{
		base.OnActivate();
		Initialize();
	}

	private void Initialize()
	{
		m_isAlreadyActivated = false;
		s_localInstance = this;
	}

	public static void ShowWidgetHandler()
	{
		if (!(s_localInstance == null) && !s_localInstance.m_isAlreadyActivated)
		{
			s_localInstance.m_isAlreadyActivated = true;
			s_localInstance.widget.SetRaycastRestrictionCallback(s_localInstance.RaycastRestrictionCallback);
			s_localInstance.widget.gameObject.SetActive(value: true);
			s_localInstance.widget.StartPlayContent();
		}
	}

	public static void HideWidgetHandler()
	{
		if (!(s_localInstance == null) && s_localInstance.m_isAlreadyActivated)
		{
			s_localInstance.m_isAlreadyActivated = false;
			s_localInstance.widget.StopPlayContent();
			s_localInstance.widget.gameObject.SetActive(value: false);
			s_localInstance.widget.SetRaycastRestrictionCallback(null);
		}
	}

	private bool RaycastRestrictionCallback()
	{
		return InputManager.IsInputLocked();
	}
}
