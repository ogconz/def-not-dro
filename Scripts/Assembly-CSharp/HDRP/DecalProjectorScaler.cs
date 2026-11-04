using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace HDRP
{
	public class DecalProjectorScaler : MonoBehaviour
	{
		[SerializeField]
		private DecalProjector m_decalProjector;

		[SerializeField]
		private Transform m_sourceTransform;

		private void Start()
		{
			m_decalProjector.affectsTransparency = true;
		}

		private void Update()
		{
			Vector3 size = m_decalProjector.transform.localRotation * m_sourceTransform.lossyScale;
			size.x = Mathf.Abs(size.x);
			size.y = Mathf.Abs(size.y);
			size.z = Mathf.Abs(size.z);
			m_decalProjector.size = size;
		}
	}
}
