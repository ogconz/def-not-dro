using System;
using Binding;
using Binding.Base;
using Binding.Components;
using DG.Tweening;
using GameInput;
using UnityEngine;

public class UIRadialAction : BaseBindingBehaviourTarget
{
	private const float PixelsToMove = 20f;

	private const float MoveDuration = 0.25f;

	private const float IconAlphaOnSelect = 1f;

	private const float IconAlphaOnDeselect = 0.5f;

	[SerializeField]
	private ActionElementBinding m_actionElementBinding;

	[SerializeField]
	private UIRadialMenuContext m_radialMenuContext;

	private System.Action m_radialAction;

	private string m_text;

	private ActionType m_actionType;

	private Vector3 m_originalLocalPosition;

	private readonly SpriteProperty m_sprite = new SpriteProperty();

	private readonly BoolProperty m_isSelected = new BoolProperty();

	private readonly BoolProperty m_isNeedShowImage = new BoolProperty();

	private readonly BoolProperty m_isKeyBindVisibility = new BoolProperty();

	private readonly AlphaFadeProperty m_iconAlpha = new AlphaFadeProperty();

	private readonly IntProperty m_countNotify = new IntProperty();

	private readonly ColorProperty m_colorNotify = new ColorProperty(Color.white);

	public bool isSelected => m_isSelected.value;

	public ActionType actionType => m_actionType;

	public void SetData(System.Action radialAction, ActionType actionType, string text, Sprite sprite)
	{
		m_radialAction = radialAction;
		m_text = text;
		m_sprite.value = sprite;
		m_actionType = actionType;
		m_actionElementBinding.SetData(string.Empty, actionType, OnClickHint);
	}

	public void SetNotificationCount(int count)
	{
		m_countNotify.value = count;
	}

	public void SetNotificationColor(Color color)
	{
		m_colorNotify.value = color;
	}

	public void OnClickHint()
	{
		if (!m_radialMenuContext.IsAnyActionSelected())
		{
			PerformAction(forceHide: true);
		}
	}

	public void UpdateHintVisibility()
	{
		m_actionElementBinding.gameObject.SetActive(m_radialAction != null && !m_actionElementBinding.isEmpty);
	}

	public void UpdateSelection(bool isSelected)
	{
		m_isSelected.value = isSelected;
		if (m_text != null)
		{
			m_radialMenuContext.actionText.value = (isSelected ? m_text : string.Empty);
		}
		m_iconAlpha.value = (isSelected ? 1f : 0.5f);
		Vector3 vector = base.transform.up * 20f;
		Vector3 vector2 = m_originalLocalPosition + vector;
		TweenItemOnSelection(isSelected ? vector2 : m_originalLocalPosition, 0.25f);
	}

	public void PerformAction(bool forceHide = false)
	{
		m_radialAction.SafeCall();
		if (forceHide)
		{
			m_radialMenuContext.Hide();
		}
	}

	private void Awake()
	{
		m_originalLocalPosition = base.transform.localPosition;
	}

	private void OnEnable()
	{
		ResetPosition();
		m_isNeedShowImage.value = m_sprite.value != null;
		m_isKeyBindVisibility.value = !InputManager.instance.GetActionBinds(actionType).Equals("NONE");
		m_iconAlpha.value = 0.5f;
		m_iconAlpha.fadeTime = 0.25f;
	}

	private void ResetPosition()
	{
		base.transform.localPosition = m_originalLocalPosition;
	}

	private void TweenItemOnSelection(Vector3 targetPosition, float duration)
	{
		base.transform.DOLocalMove(targetPosition, duration).SetEase(Ease.OutQuad).SetUpdate(isIndependentUpdate: true);
	}
}
