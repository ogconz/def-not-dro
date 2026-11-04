using System;
using System.Collections.Generic;
using Binding;

namespace Chat.Overlay.UI.ActionList
{
	public class ChatActionListProperty : ListProperty
	{
		public event Action onCalledAction;

		public void Setup(Dictionary<string, Action> actions)
		{
			FillActions(actions);
			base.OnClick += OnClickHandler;
		}

		public void Release()
		{
			base.OnClick -= OnClickHandler;
			SelectionAll(selected: false);
			Clear();
		}

		private void OnClickHandler(BaseListElementData item)
		{
			if (item is ChatActionElementData chatActionElementData)
			{
				chatActionElementData.GetAction().SafeCall();
			}
			this.onCalledAction.SafeCall();
		}

		public void ClickOnCurrentAction()
		{
			OnClickHandler(GetSelected());
		}

		public void SelectAction(int offset)
		{
			Selection(GetSelectedIndex() + offset, selected: true);
		}

		private void FillActions(Dictionary<string, Action> actions)
		{
			foreach (var (caption, action2) in actions)
			{
				Add(new ChatActionElementData(caption, action2));
			}
		}
	}
}
