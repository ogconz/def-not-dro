using System.Collections;
using UnityEngine;

public sealed class InterpolatedTransform : MonoBehaviour
{
	private struct TransformData
	{
		public Vector3 position;

		public Quaternion rotation;

		public TransformData(Vector3 position, Quaternion rotation)
		{
			this.position = position;
			this.rotation = rotation;
		}
	}

	private TransformData[] m_lastTransforms;

	private int m_newTransformIndex;

	private WaitForFixedUpdate m_waitForFixedUpdate = new WaitForFixedUpdate();

	private Transform m_transform;

	private void Awake()
	{
		m_transform = base.transform;
	}

	private void OnEnable()
	{
		ForgetPreviousTransforms();
	}

	public void ForgetPreviousTransforms()
	{
		m_lastTransforms = new TransformData[2];
		TransformData transformData = new TransformData(m_transform.position, m_transform.rotation);
		m_lastTransforms[0] = transformData;
		m_lastTransforms[1] = transformData;
		m_newTransformIndex = 0;
	}

	private void FixedUpdate()
	{
		TransformData transformData = m_lastTransforms[m_newTransformIndex];
		m_transform.SetPositionAndRotation(transformData.position, transformData.rotation);
		StartCoroutine(LateFixedUpdate());
	}

	private IEnumerator LateFixedUpdate()
	{
		yield return m_waitForFixedUpdate;
		m_newTransformIndex = OldTransformIndex();
		m_lastTransforms[m_newTransformIndex] = new TransformData(m_transform.position, m_transform.rotation);
	}

	private void Update()
	{
		TransformData transformData = m_lastTransforms[m_newTransformIndex];
		TransformData transformData2 = m_lastTransforms[OldTransformIndex()];
		float t = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
		m_transform.SetPositionAndRotation(Vector3.Lerp(transformData2.position, transformData.position, t), Quaternion.Slerp(transformData2.rotation, transformData.rotation, t));
	}

	private int OldTransformIndex()
	{
		if (m_newTransformIndex != 0)
		{
			return 0;
		}
		return 1;
	}
}
