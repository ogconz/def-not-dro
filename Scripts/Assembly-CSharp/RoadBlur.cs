using UnityEngine;

[RequireComponent(typeof(Camera))]
[AddComponentMenu("Scripts/Effects/RoadBlur")]
public class RoadBlur : MonoBehaviour
{
	private Camera m_cachedCamera;

	private Transform m_cachedTransform;

	private int m_propertyID;

	private void Awake()
	{
		m_cachedCamera = GetComponent<Camera>();
		m_cachedTransform = base.transform;
		m_propertyID = Shader.PropertyToID("_RoadBlurFactors");
	}

	private void Update()
	{
		float num = Mathf.Clamp01(Vector3.Dot(m_cachedTransform.forward, m_cachedCamera.velocity) * 0.0357f * 0.7f);
		float num2 = Mathf.Clamp01(num * 2.5f);
		Shader.SetGlobalVector(value: new Vector4(num, 1f - num, num2, 1f - num2), nameID: m_propertyID);
	}
}
