using System;
using System.Collections.Generic;

public class ContextPriorityList
{
	public class Item
	{
		public ShowContextDelegate showDelegate;

		public int priority;
	}

	public delegate BaseContext ShowContextDelegate();

	private readonly List<Item> m_items = new List<Item>();

	private BaseContext m_baseContext;

	public void Add(Item item)
	{
		m_items.Add(item);
		m_items.Sort((Item item2, Item item3) => item2.priority.CompareTo(item3.priority));
	}

	public void Clear()
	{
		m_items.Clear();
		if (m_baseContext != null)
		{
			m_baseContext.Hide();
			m_baseContext = null;
		}
	}

	public void Process()
	{
		if (m_baseContext != null || m_items.Count == 0)
		{
			return;
		}
		Item item = m_items[m_items.Count - 1];
		m_items.RemoveAt(m_items.Count - 1);
		m_baseContext = item.showDelegate();
		Action<BaseContext> activeAction = null;
		activeAction = delegate(BaseContext context)
		{
			if (!context.isActive)
			{
				BaseContext baseContext2 = m_baseContext;
				baseContext2.onActiveChanged = (Action<BaseContext>)Delegate.Remove(baseContext2.onActiveChanged, activeAction);
				m_baseContext = null;
			}
		};
		BaseContext baseContext = m_baseContext;
		baseContext.onActiveChanged = (Action<BaseContext>)Delegate.Combine(baseContext.onActiveChanged, activeAction);
	}
}
