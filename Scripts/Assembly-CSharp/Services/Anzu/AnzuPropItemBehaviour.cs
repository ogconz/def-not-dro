using System.Collections.Generic;
using UnityEngine;

namespace Services.Anzu
{
	public class AnzuPropItemBehaviour : MonoBehaviour
	{
		[SerializeField]
		[HideInInspector]
		private List<AnzuPlacement> m_propsPlacements = new List<AnzuPlacement>();

		public void UpdatePlacementsAdStatsVisibility()
		{
			foreach (AnzuPlacement propsPlacement in m_propsPlacements)
			{
				propsPlacement.SetActiveAdStats(GameConsole.Bool["anzuStats"]);
			}
		}

		private void OnEnable()
		{
			foreach (AnzuPlacement propsPlacement in m_propsPlacements)
			{
				propsPlacement.Initialize();
			}
		}

		private void OnDisable()
		{
			foreach (AnzuPlacement propsPlacement in m_propsPlacements)
			{
				propsPlacement.Dispose();
			}
		}

		public List<AnzuPlacement> GetPropsPlacements()
		{
			return m_propsPlacements;
		}
	}
}
