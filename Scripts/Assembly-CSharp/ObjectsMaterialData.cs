using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ObjectsMaterialData : MonoBehaviour
{
	private struct MaterialParams
	{
		private readonly Material m_mat;

		private readonly Vector2 m_initTiling;

		private readonly Vector2 m_initOffset;

		public MaterialParams(Material mat)
		{
			m_mat = mat;
			m_initTiling = mat.mainTextureScale;
			m_initOffset = mat.mainTextureOffset;
		}

		public void Override(Vector2 tiling, Vector2 offset)
		{
			m_mat.mainTextureScale = tiling;
			m_mat.mainTextureOffset = offset;
		}

		public void Restore()
		{
			Override(m_initTiling, m_initOffset);
		}
	}

	[SerializeField]
	public string customTag = "";

	private Transform m_objectListRoot;

	[SerializeField]
	public Vector2 tiling = new Vector2(1f, 1f);

	[SerializeField]
	public Vector2 offset;

	private readonly List<MaterialParams> m_tempParams = new List<MaterialParams>();

	private void RestoreParameters()
	{
		foreach (MaterialParams tempParam in m_tempParams)
		{
			tempParam.Restore();
		}
	}

	private void ApplyParameters()
	{
		foreach (MaterialParams tempParam in m_tempParams)
		{
			tempParam.Override(tiling, offset);
		}
	}

	private void InitParameters()
	{
		m_tempParams.Clear();
		try
		{
			if (!string.IsNullOrEmpty(customTag))
			{
				HashSet<GameObject> hashSet = CustomTagManager.Get(customTag);
				if (hashSet.Count == 1)
				{
					foreach (GameObject item in hashSet)
					{
						m_objectListRoot = item.transform;
					}
				}
				else
				{
					GameLog.Print(AlertLevel.Error, "ObjectsMaterialData", "Can't link by tag " + customTag);
				}
			}
			if (m_objectListRoot == null)
			{
				GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
				foreach (GameObject gameObject in rootGameObjects)
				{
					GameObject gameObject2 = gameObject.FindChildWithName(base.gameObject.name);
					if ((bool)gameObject2 && gameObject2 != base.gameObject)
					{
						m_objectListRoot = gameObject2.transform;
						break;
					}
					if (gameObject.name == base.gameObject.name)
					{
						m_objectListRoot = gameObject.transform;
						break;
					}
				}
			}
			if (m_objectListRoot != null)
			{
				Renderer[] componentsInChildren = m_objectListRoot.GetComponentsInChildren<Renderer>();
				foreach (Renderer renderer in componentsInChildren)
				{
					m_tempParams.Add(new MaterialParams(renderer.sharedMaterial));
				}
			}
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
	}

	private void Awake()
	{
		InitParameters();
	}

	private void OnEnable()
	{
		ApplyParameters();
	}

	private void OnDisable()
	{
		RestoreParameters();
	}

	private void OnDestroy()
	{
		RestoreParameters();
	}
}
