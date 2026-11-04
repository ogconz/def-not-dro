using System;
using Binding;
using Binding.Base;
using GameInput;

namespace SharingSetups
{
	[GameHotkeyBlocker(true)]
	[RaycastBlocker(true)]
	public class UISharingSetupsOptionsContext : BaseBindingBehaviourTarget
	{
		private class ActionOptionsItem : BaseListElementData
		{
			private StringProperty m_optionName = new StringProperty();

			private BoolProperty m_isSelected = new BoolProperty();

			public System.Action callback { get; private set; }

			public ActionOptionsItem(LocalizationKeys.Key optionKey, System.Action callback)
			{
				this.callback = callback;
				m_optionName.value = optionKey.Localized();
			}

			public void SetSelected(bool selected)
			{
				m_isSelected.value = selected;
			}
		}

		public System.Action onDeleteCustomSetup;

		public System.Action onRate;

		public System.Action onGoTestdrive;

		private readonly ListProperty m_actionsList = new ListProperty();

		protected override void OnActivate()
		{
			base.OnActivate();
			InitList();
			m_actionsList.OnSelectionChanged += OnListSelectionChangedHandler;
			m_actionsList.OnClick += OnClickHandler;
		}

		protected override void OnDeactivate()
		{
			m_actionsList.OnSelectionChanged -= OnListSelectionChangedHandler;
			m_actionsList.OnClick -= OnClickHandler;
			base.OnDeactivate();
		}

		private void InitList()
		{
			if (m_actionsList.count == 0)
			{
				m_actionsList.Add(new ActionOptionsItem(LocalizationKeys.nav_hint_input_settings_remove_bind, OnClickDelete));
				m_actionsList.Add(new ActionOptionsItem(LocalizationKeys.nav_hint_goto_testdrive, OnGoTestDrive));
				m_actionsList.Add(new ActionOptionsItem(LocalizationKeys.rate, OnRate));
			}
			m_actionsList.Selection(0, selected: true);
			OnListSelectionChangedHandler(m_actionsList[0]);
		}

		private void OnClickDelete()
		{
			onDeleteCustomSetup.SafeCall();
		}

		private void OnGoTestDrive()
		{
			onGoTestdrive.SafeCall();
		}

		private void OnRate()
		{
			onRate.SafeCall();
		}

		private void OnListSelectionChangedHandler(BaseListElementData baseListElement)
		{
			for (int i = 0; i < m_actionsList.count; i++)
			{
				if (m_actionsList[i] is ActionOptionsItem actionOptionsItem)
				{
					actionOptionsItem.SetSelected(m_actionsList[i] == baseListElement);
				}
			}
		}

		private void OnClickHandler(BaseListElementData baseListElement)
		{
			if (baseListElement is ActionOptionsItem actionOptionsItem)
			{
				actionOptionsItem.callback.SafeCall();
			}
			Hide();
		}

		[GameHotkey(ActionType.NavSelectBtnA, false, false)]
		public void NavigationSubmit()
		{
			OnClickHandler(m_actionsList.GetSelected());
		}

		[GameHotkey(ActionType.NavCancelBtnB, false, false)]
		public void NavigationClose()
		{
			Hide();
		}

		[GameHotkey(ActionType.NavDown, false, true)]
		public void NavDown()
		{
			m_actionsList.Selection(m_actionsList.GetSelectedIndex() + 1, selected: true);
		}

		[GameHotkey(ActionType.NavUp, false, true)]
		public void NavUp()
		{
			m_actionsList.Selection(m_actionsList.GetSelectedIndex() - 1, selected: true);
		}

		public void OnDeleteCustomSetup()
		{
			onDeleteCustomSetup.SafeCall();
		}
	}
}
