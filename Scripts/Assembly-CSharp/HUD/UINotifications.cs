using System;
using System.Collections;
using Binding;
using Binding.Base;
using UnityEngine;

namespace HUD
{
	public class UINotifications : BehaviourBindingTarget
	{
		private class NotificationItem : BaseListElementData
		{
			public AlphaFadeProperty alpha = new AlphaFadeProperty();

			private StringProperty m_text = new StringProperty();

			private ColorProperty m_color = new ColorProperty(Color.white);

			private IEnumerator m_coroutine;

			private float m_startTime;

			public bool isDie { get; private set; }

			public NotificationItem(string text, Color color, Func<bool> func = null)
			{
				m_text.value = text;
				m_color.value = color;
				m_coroutine = TimeController.RunCoroutine(LiveItem(func));
				m_startTime = Time.unscaledTime;
			}

			public void Update()
			{
				if (!isDie)
				{
					isDie = Time.unscaledTime > m_startTime + 100f;
				}
			}

			public void Release()
			{
				if (m_coroutine != null)
				{
					TimeController.RemoveCoroutine(m_coroutine);
					m_coroutine = null;
				}
			}

			private IEnumerator LiveItem(Func<bool> func)
			{
				alpha.SetForce(0f);
				yield return null;
				alpha.fadeTime = 0.2f;
				alpha.value = 1f;
				if (func != null)
				{
					yield return new WaitWhile(func);
				}
				else
				{
					yield return new WaitForSecondsRealtime(6f);
				}
				alpha.fadeTime = 2f;
				alpha.value = 0f;
				yield return new WaitForSecondsRealtime(2f);
				isDie = true;
			}
		}

		[SerializeField]
		private int MAX_COUNT = 10;

		private ListProperty m_list = new ListProperty();

		public static UINotifications instance { get; private set; }

		private void Awake()
		{
			instance = this;
		}

		private void OnDestroy()
		{
			m_list.ForEach(delegate(NotificationItem x)
			{
				x.Release();
			});
		}

		public void Add(string text, Color color, Func<bool> func = null)
		{
			if (m_list.count == MAX_COUNT)
			{
				m_list.GetByIndex<NotificationItem>(0).Release();
				m_list.RemoveAt(0);
			}
			NotificationItem data = new NotificationItem(text, color, func);
			m_list.Add(data);
		}

		private void Update()
		{
			for (int i = 0; i < m_list.count; i++)
			{
				if (m_list.GetByIndex<NotificationItem>(i).isDie)
				{
					m_list.RemoveAt(i--);
				}
			}
		}
	}
}
