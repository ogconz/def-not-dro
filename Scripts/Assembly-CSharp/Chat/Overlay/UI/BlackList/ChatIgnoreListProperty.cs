using System;
using Binding;
using Chat.Features;
using UnityEngine;

namespace Chat.Overlay.UI.BlackList
{
	public class ChatIgnoreListProperty : ListProperty
	{
		private IgnoreUsersProvider m_ignoreUsersProvider;

		public event Action onUpdateCanvasEvent;

		public void Setup(IgnoreUsersProvider ignoreUsersProvider)
		{
			m_ignoreUsersProvider = ignoreUsersProvider;
			BindHandlers();
			FillUsers();
		}

		public void Release()
		{
			ReleaseHandlers();
			m_ignoreUsersProvider = null;
		}

		private void BindHandlers()
		{
			base.OnElementInserted += OnElementChangedHandler;
			base.OnElementRemoved += OnElementRemovedHandler;
			base.OnClick += OnClickHandler;
			m_ignoreUsersProvider.onListChanged += FillUsers;
		}

		private void ReleaseHandlers()
		{
			base.OnElementInserted -= OnElementChangedHandler;
			base.OnElementRemoved -= OnElementRemovedHandler;
			base.OnClick -= OnClickHandler;
			m_ignoreUsersProvider.onListChanged -= FillUsers;
		}

		private void OnElementChangedHandler(int i, BaseListElementData item)
		{
			InvokeUpdateCanvasEvent();
		}

		private void OnElementRemovedHandler(int i, BaseListElementData item)
		{
			InvokeUpdateCanvasEvent();
		}

		private void OnClickHandler(BaseListElementData item)
		{
			RemoveUserFromIgnoreList(item);
		}

		private void RemoveUserFromIgnoreList(BaseListElementData item)
		{
			bool num = base.count > 1 && GetSelectedIndex() != -1;
			int newIndex = -1;
			if (num)
			{
				newIndex = Mathf.Clamp(IndexOf(item) - 1, 0, base.count - 1);
			}
			if (item is ChatIgnoreListElementData chatIgnoreListElementData)
			{
				m_ignoreUsersProvider.Remove(chatIgnoreListElementData.GetUserInfo());
			}
			if (num)
			{
				TimeController.AddCallback(0.01f, delegate
				{
					SelectAndScrollTo(newIndex);
				});
			}
		}

		public void ClickOnCurrentItem()
		{
			OnClickHandler(GetSelected());
		}

		public void SelectItemByOffset(int offset)
		{
			SelectAndScrollTo(GetSelectedIndex() + offset);
		}

		private void SelectAndScrollTo(int index)
		{
			Selection(index, selected: true);
			ScrollTo(index);
		}

		private void FillUsers()
		{
			ClearUsers();
			foreach (UserInfo item in m_ignoreUsersProvider.GetAll())
			{
				Add(new ChatIgnoreListElementData(item));
			}
			InvokeUpdateCanvasEvent();
		}

		private void ClearUsers()
		{
			Clear();
			InvokeUpdateCanvasEvent();
		}

		private void InvokeUpdateCanvasEvent()
		{
			this.onUpdateCanvasEvent.SafeCall();
		}
	}
}
