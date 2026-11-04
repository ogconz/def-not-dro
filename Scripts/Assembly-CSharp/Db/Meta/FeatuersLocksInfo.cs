using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class FeatuersLocksInfo
	{
		private Dictionary<GameGlobalFeature, int> m_dispatchedLocks = new Dictionary<GameGlobalFeature, int>();

		[SerializeField]
		private List<LockInfo> m_xmlLocks = new List<LockInfo>();

		[XmlArray("locks")]
		public List<LockInfo> xmlLocks
		{
			get
			{
				return m_xmlLocks;
			}
			set
			{
				m_xmlLocks = value;
			}
		}

		public Dictionary<GameGlobalFeature, int> GetDispatchedLocks()
		{
			DispatchLocks();
			return m_dispatchedLocks;
		}

		private void DispatchLocks()
		{
			if (xmlLocks == null)
			{
				return;
			}
			m_dispatchedLocks.Clear();
			foreach (LockInfo xmlLock in xmlLocks)
			{
				m_dispatchedLocks.Add(xmlLock.feature, xmlLock.level);
			}
			m_dispatchedLocks.Add(GameGlobalFeature.Unknown, 0);
			xmlLocks = null;
		}
	}
}
