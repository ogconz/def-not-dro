using System;
using Binding;
using Binding.Base;
using GameInput;
using UnityEngine;

namespace Xbox
{
	[RaycastBlocker(true)]
	[GameHotkeyBlocker(true)]
	public class UIXboxDialogContext : BehaviourBindingTarget
	{
		private StringProperty m_title = new StringProperty();

		private StringProperty m_text = new StringProperty();

		private StringProperty m_hint = new StringProperty();

		private Action<ulong> m_callback;

		private static UIXboxDialogContext m_instance;

		public static bool isActive
		{
			get
			{
				if (m_instance != null)
				{
					return m_instance.gameObject.activeSelf;
				}
				return false;
			}
		}

		public static bool HideCurrent()
		{
			if ((bool)m_instance && m_instance.gameObject.activeSelf)
			{
				m_instance.gameObject.SetActive(value: false);
				return true;
			}
			return false;
		}

		public static bool Show(string title, string text, string hintText, Action<ulong> action)
		{
			if ((bool)m_instance && m_instance.gameObject.activeSelf)
			{
				return false;
			}
			if (!m_instance)
			{
				UIXboxDialogContext component = ResourcesWrapper.Load<GameObject>("Prefabs/UI/Common/UIXboxDialog").GetComponent<UIXboxDialogContext>();
				if (component == null)
				{
					return false;
				}
				m_instance = UnityEngine.Object.Instantiate(component, BaseGameState.FindUGUIRoot());
			}
			m_instance.gameObject.SetActive(value: true);
			m_instance.SetData(title, text, hintText, action);
			return true;
		}

		private void OnDestroy()
		{
			ResourcesWrapper.UnloadAsset(base.gameObject);
		}

		private void OnEnable()
		{
			InputManager.LockInput(this);
		}

		private void OnDisable()
		{
			if (InputManager.IsInputLocked(this))
			{
				InputManager.LockInput(null);
			}
		}

		public void SetData(string title, string text, string hintText, Action<ulong> action)
		{
			m_text.value = text;
			m_title.value = title;
			m_hint.value = hintText;
			m_callback = action;
		}
	}
}
