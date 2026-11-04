using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Services.Anzu
{
	public class AnzuPropsHolder
	{
		private List<AnzuPropItemBehaviour> m_props = new List<AnzuPropItemBehaviour>();

		private List<AnzuPlacement> m_placementsList = new List<AnzuPlacement>();

		private AnzuAdditiveSceneController m_sceneController;

		public AnzuPropsHolder(AnzuAdditiveSceneController sceneController)
		{
			m_sceneController = sceneController;
		}

		public void FillItems()
		{
			TryCollectAdPropsOnScene();
			CollectPropsPlacements();
		}

		public void Dispose()
		{
			m_props?.Clear();
			m_props = null;
			m_placementsList?.Clear();
			m_placementsList = null;
			m_sceneController = null;
		}

		public List<AnzuPlacement> GetPropsPlacements()
		{
			return m_placementsList;
		}

		private void CollectPropsPlacements()
		{
			m_placementsList.Clear();
			foreach (AnzuPropItemBehaviour prop in m_props)
			{
				m_placementsList.AddRange(prop.GetPropsPlacements());
			}
		}

		private void TryCollectAdPropsOnScene()
		{
			m_props.Clear();
			if (m_sceneController.TryGetActiveScene(out var scene))
			{
				m_props = GetComponentsInScene(scene);
				UpdatePropsAdStatsVisibility();
			}
		}

		private List<AnzuPropItemBehaviour> GetComponentsInScene(Scene scene)
		{
			List<AnzuPropItemBehaviour> list = new List<AnzuPropItemBehaviour>();
			GameObject[] rootGameObjects = scene.GetRootGameObjects();
			foreach (GameObject gameObject in rootGameObjects)
			{
				list.AddRange(gameObject.GetComponentsInChildren<AnzuPropItemBehaviour>(includeInactive: false));
			}
			return list;
		}

		public void UpdatePropsAdStatsVisibility()
		{
			foreach (AnzuPropItemBehaviour prop in m_props)
			{
				prop.UpdatePlacementsAdStatsVisibility();
			}
		}
	}
}
