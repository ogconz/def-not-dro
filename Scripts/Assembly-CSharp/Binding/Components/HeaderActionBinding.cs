using System;
using System.Collections.Generic;
using Binding.Base;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	public class HeaderActionBinding : BehaviourBindingTarget
	{
		public enum ViewType
		{
			None = -1,
			AsNormalText = 0,
			AsSettingsIcon = 1,
			AsPatchnoteIcon = 2
		}

		[SerializeField]
		private TextMeshProUGUI m_textLabel;

		[SerializeField]
		private GameObject m_iconsContainer;

		[SerializeField]
		private Color selectedColor = Color.blue;

		[SerializeField]
		private Color unselectedColor = Color.white;

		[SerializeField]
		private float durationColorChange = 0.5f;

		[SerializeField]
		private GameObject m_notificationIcon;

		private List<Image> m_image = new List<Image>();

		public int indexAction;

		private BoolProperty m_visible = new BoolProperty();

		private IntProperty m_showViewType = new IntProperty();

		private Action m_action;

		private SwitcherActions m_actionsSwitcher;

		private void Awake()
		{
			if (m_textLabel == null)
			{
				m_textLabel = GetComponentInChildren<TextMeshProUGUI>();
			}
			if (m_iconsContainer != null)
			{
				m_image.AddRange(m_iconsContainer.GetComponentsInChildren<Image>());
			}
		}

		public void SetData(SwitcherActions actionsSwitcher, string text, Action action, bool hasNotification, ViewType viewType)
		{
			m_actionsSwitcher = actionsSwitcher;
			m_showViewType.value = (int)viewType;
			switch (viewType)
			{
			default:
				SetText(text);
				break;
			case ViewType.AsSettingsIcon:
				SetText(string.Empty);
				break;
			case ViewType.AsPatchnoteIcon:
				SetText(string.Empty);
				break;
			}
			m_action = action;
			UpdateNotificationIcon(hasNotification);
			void SetText(string val)
			{
				if ((bool)m_textLabel)
				{
					m_textLabel.text = val;
				}
			}
		}

		public void OnClick()
		{
			Select();
			m_action.SafeCall();
		}

		public void Select()
		{
			m_actionsSwitcher.ResetSelectionAndSetIndexOnly(indexAction);
			if (m_textLabel != null)
			{
				m_textLabel.DOColor(selectedColor, durationColorChange).SetUpdate(isIndependentUpdate: true).Play();
			}
			if (m_image.Count > 0)
			{
				foreach (Image item in m_image)
				{
					item.DOColor(selectedColor, durationColorChange).SetUpdate(isIndependentUpdate: true).Play();
				}
			}
			m_visible.value = true;
			UpdateNotificationIcon(visible: false);
		}

		public void ResetSelection()
		{
			if ((bool)m_textLabel)
			{
				m_textLabel.DOColor(unselectedColor, durationColorChange).SetUpdate(isIndependentUpdate: true).Play();
			}
			if (m_image.Count > 0)
			{
				foreach (Image item in m_image)
				{
					item.DOColor(unselectedColor, durationColorChange).SetUpdate(isIndependentUpdate: true).Play();
				}
			}
			m_visible.value = false;
		}

		public void UpdateNotificationIcon(bool visible)
		{
			m_notificationIcon.SetActive(visible);
		}
	}
}
