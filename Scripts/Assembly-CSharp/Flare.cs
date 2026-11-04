using UnityEngine;

[DisallowMultipleComponent]
public class Flare : MonoBehaviour
{
	[SerializeField]
	private FlareData m_flareData;

	private MeshFilter m_meshFilter;

	private MeshRenderer m_meshRenderer;

	public FlareData FlareData
	{
		get
		{
			return m_flareData;
		}
		set
		{
			m_flareData = value;
			if (m_meshFilter != null)
			{
				if (m_flareData != null)
				{
					m_meshFilter.sharedMesh = m_flareData.Mesh;
					m_meshRenderer.sharedMaterial = m_flareData.Material;
				}
				else
				{
					m_meshFilter.sharedMesh = null;
					m_meshRenderer.sharedMaterial = null;
				}
			}
		}
	}

	private void Start()
	{
		if (m_meshFilter == null && m_flareData != null)
		{
			m_meshFilter = base.gameObject.GetOrAddComponent<MeshFilter>();
			m_meshFilter.hideFlags = HideFlags.NotEditable;
			m_meshFilter.sharedMesh = m_flareData.Mesh;
			m_meshRenderer = base.gameObject.GetOrAddComponent<MeshRenderer>();
			m_meshRenderer.hideFlags = HideFlags.NotEditable;
			m_meshRenderer.sharedMaterial = m_flareData.Material;
		}
	}

	private void OnEnable()
	{
		if (m_meshRenderer != null)
		{
			m_meshRenderer.enabled = true;
		}
	}

	private void OnDisable()
	{
		if (m_meshRenderer != null)
		{
			m_meshRenderer.enabled = false;
		}
	}

	private void OnDestroy()
	{
		if (m_meshFilter != null)
		{
			Object.Destroy(m_meshRenderer);
			Object.Destroy(m_meshFilter);
		}
	}
}
