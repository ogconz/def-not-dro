using System;
using Binding.Base;
using UnityEngine;

public abstract class BaseHintPanelContext : BaseBindingBehaviourTarget
{
	public GameObject prefabHint;

	protected ActionElementBindingPool m_aebPool = new ActionElementBindingPool();

	protected void UpdateCanvasOffsetInXrMode()
	{
		XRModeManager instance = XRModeManager.instance;
		if (XRModeManager.IsXRActive())
		{
			Canvas component = GetComponent<Canvas>();
			if (component != null && component.renderMode == RenderMode.WorldSpace)
			{
				RectTransform component2 = base.gameObject.GetComponent<RectTransform>();
				component2.offsetMin = new Vector2(component2.offsetMin.x, 180f);
				instance.OnXRSubsystemDeactivated = (Action)Delegate.Remove(instance.OnXRSubsystemDeactivated, new Action(ResetRectTransformBottomOffset));
				instance.OnXRSubsystemDeactivated = (Action)Delegate.Combine(instance.OnXRSubsystemDeactivated, new Action(ResetRectTransformBottomOffset));
			}
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_aebPool.CleanUp();
		m_aebPool = null;
	}

	private static void ResetRectTransformBottomOffset()
	{
		BaseGameState baseGameState = States.current as BaseGameState;
		if (baseGameState == null)
		{
			return;
		}
		ContextOwner contextOwner = baseGameState.contextOwner;
		if (contextOwner != null)
		{
			UIBottomPanelContext uIBottomPanelContext = contextOwner.Get<UIBottomPanelContext>();
			if (!(uIBottomPanelContext == null))
			{
				RectTransform component = uIBottomPanelContext.gameObject.GetComponent<RectTransform>();
				component.offsetMin = new Vector2(component.offsetMin.x, 0f);
			}
		}
	}
}
