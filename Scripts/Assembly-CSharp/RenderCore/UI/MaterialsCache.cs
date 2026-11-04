using System.Collections.Generic;
using UnityEngine;

namespace RenderCore.UI
{
	internal class MaterialsCache
	{
		private class Entry
		{
			internal Material baseMaterial;

			internal Material modifiedMaterial;

			internal IMaterialParameters parameters;

			internal int refCount;
		}

		private readonly List<Entry> m_entries = new List<Entry>();

		public bool isUsed => m_entries.Count > 0;

		public Material Add(Material material, IMaterialParameters parameters)
		{
			for (int i = 0; i < m_entries.Count; i++)
			{
				Entry entry = m_entries[i];
				if (entry.baseMaterial == material && entry.parameters.Compare(parameters))
				{
					entry.refCount++;
					return entry.modifiedMaterial;
				}
			}
			Material material2 = new Material(material);
			parameters.Modify(material2);
			material2.hideFlags = HideFlags.HideAndDontSave;
			Entry entry2 = new Entry
			{
				refCount = 1,
				baseMaterial = material,
				modifiedMaterial = material2,
				parameters = parameters
			};
			m_entries.Add(entry2);
			return entry2.modifiedMaterial;
		}

		public void Remove(Material modifiedMaterial)
		{
			if (!(modifiedMaterial != null))
			{
				return;
			}
			for (int i = 0; i < m_entries.Count; i++)
			{
				Entry entry = m_entries[i];
				if (entry.modifiedMaterial == modifiedMaterial)
				{
					if (--entry.refCount == 0)
					{
						Object.DestroyImmediate(entry.modifiedMaterial);
						entry.baseMaterial = null;
						m_entries.RemoveAt(i);
					}
					break;
				}
			}
		}

		public void ClearAll()
		{
			for (int i = 0; i < m_entries.Count; i++)
			{
				Entry entry = m_entries[i];
				Object.DestroyImmediate(entry.modifiedMaterial);
				entry.baseMaterial = null;
			}
			m_entries.Clear();
		}
	}
}
