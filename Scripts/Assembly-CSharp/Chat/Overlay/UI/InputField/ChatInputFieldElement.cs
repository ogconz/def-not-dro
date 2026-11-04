using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Chat.Overlay.UI.InputField
{
	public class ChatInputFieldElement : TMP_InputField
	{
		private const float ShowAlpha = 1f;

		private const float HideAlpha = 0f;

		private const float FadeDuration = 0.1f;

		[SerializeField]
		private CanvasGroup m_canvas;

		private TweenRunner m_showTweenRunner = new TweenRunner();

		private TweenRunner m_hideTweenRunner = new TweenRunner();

		private bool m_currentSelectionState;

		private bool m_targetSelectionState;

		private bool m_isPointerEntered;

		public event Action<string> onTextSubmit;

		public event Action onActivityChanged;

		protected override void OnEnable()
		{
			base.OnEnable();
			base.onValueChanged.AddListener(OnValueChangedHandler);
		}

		protected override void OnDisable()
		{
			m_showTweenRunner.Release();
			m_hideTweenRunner.Release();
			base.onValueChanged.RemoveListener(OnValueChangedHandler);
			base.OnDisable();
		}

		private void Update()
		{
			ProcessSelectionState();
		}

		private void OnValueChangedHandler(string rawText)
		{
			string text = UIUtils.FilterText(rawText);
			if (text != rawText)
			{
				SetTextWithoutNotify(text);
			}
		}

		public bool IsHovered()
		{
			return m_isPointerEntered;
		}

		public bool IsSelected()
		{
			return m_targetSelectionState;
		}

		public override void OnSelect(BaseEventData eventData)
		{
			base.OnSelect(eventData);
			SetSelection(selected: true);
		}

		public override void OnDeselect(BaseEventData eventData)
		{
			base.OnDeselect(eventData);
			SetSelection(selected: false);
		}

		public override void OnPointerEnter(PointerEventData eventData)
		{
			m_isPointerEntered = true;
			InvokeActivityChangedEvent();
		}

		public override void OnPointerExit(PointerEventData eventData)
		{
			m_isPointerEntered = false;
			InvokeActivityChangedEvent();
		}

		public void Show()
		{
			m_hideTweenRunner.Release();
			m_showTweenRunner.Run(m_canvas.DOFade(1f, 0.1f));
		}

		public void Hide()
		{
			m_hideTweenRunner.Run(m_canvas.DOFade(0f, 0.1f));
		}

		private void Clear()
		{
			base.text = string.Empty;
		}

		public void SetSelection(bool selected)
		{
			m_targetSelectionState = selected;
			if (!m_targetSelectionState)
			{
				HotkeyManager.Sleep();
			}
		}

		public void InvokeTextSubmitEvent()
		{
			this.onTextSubmit.SafeCall(base.text);
			Clear();
		}

		private void InvokeActivityChangedEvent()
		{
			this.onActivityChanged.SafeCall();
		}

		public override void OnUpdateSelected(BaseEventData eventData)
		{
			if (base.isFocused)
			{
				base.OnUpdateSelected(eventData);
				if (base.wasCanceled)
				{
					OnDeselect(eventData);
				}
			}
		}

		private void ProcessSelectionState()
		{
			if (m_currentSelectionState == m_targetSelectionState)
			{
				return;
			}
			InvokeActivityChangedEvent();
			EventSystem current = EventSystem.current;
			if (!(current == null) && !current.alreadySelecting)
			{
				bool flag = current.currentSelectedGameObject == base.gameObject;
				if (flag && !m_targetSelectionState)
				{
					current.SetSelectedGameObject(null);
				}
				else if (!flag && m_targetSelectionState)
				{
					current.SetSelectedGameObject(base.gameObject);
				}
				m_currentSelectionState = m_targetSelectionState;
			}
		}
	}
}
