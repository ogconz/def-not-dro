using UnityEngine;

[AddComponentMenu("Scripts/Generic/Util/SetRenderQueue")]
[RequireComponent(typeof(Renderer))]
public class SetRenderQueue : MonoBehaviour
{
	[SerializeField]
	private int[] m_queues;

	private void Start()
	{
		MaterialAccessor orAddComponent = base.gameObject.GetOrAddComponent<MaterialAccessor>();
		for (int i = 0; i < m_queues.Length && i < orAddComponent.materialsCount; i++)
		{
			orAddComponent.GetMaterial(i).renderQueue = m_queues[i];
		}
	}
}
