using System;
using GameOverlay;

namespace Chat.Overlay.UI.InputField.InputStrategy
{
	public class VirtualInputStrategy : IInputStrategy
	{
		private ChatInputFieldElement m_inputField;

		private bool m_isLocked;

		public VirtualInputStrategy(ChatInputFieldElement inputField)
		{
			m_inputField = inputField;
		}

		~VirtualInputStrategy()
		{
			m_inputField = null;
		}

		public void Setup()
		{
			m_inputField.onActivityChanged += OnActivityChangedHandler;
			InvokeReceiveTextIfInputFieldSelected();
		}

		public void Release()
		{
			m_inputField.onActivityChanged -= OnActivityChangedHandler;
		}

		public bool IsLocked()
		{
			return m_isLocked;
		}

		private void OnActivityChangedHandler()
		{
			InvokeReceiveTextIfInputFieldSelected();
		}

		private void InvokeReceiveTextIfInputFieldSelected()
		{
			if (m_inputField.IsSelected())
			{
				InvokeReceiveText();
			}
		}

		private void InvokeReceiveText()
		{
			ReceiveText(LocalizationKeys.settings_game_chat.Localized(), m_inputField.text, m_inputField.characterLimit, ReceiveTextCallback);
		}

		private void ReceiveTextCallback(string text)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				m_inputField.text = text;
				m_inputField.InvokeTextSubmitEvent();
			}
			TimeController.AddCallback(0.1f, DelayedSelectionCallback);
		}

		private void DelayedSelectionCallback()
		{
			m_inputField.SetSelection(selected: false);
		}

		private void ReceiveText(string title, string defaultText, int maxLength, Action<string> callback)
		{
			if (!m_isLocked)
			{
				m_isLocked = true;
				GameOverlay.Overlay.instance.virtualKeyboard.ReceiveText(title, defaultText, maxLength, ProcessCallback);
			}
			void ProcessCallback(string result)
			{
				m_isLocked = false;
				callback.SafeCall(result);
			}
		}
	}
}
