using UnityEngine;

[CreateAssetMenu(fileName = "NickNameDistanceConfig", menuName = "Other/NickNameDistanceConfig")]
public class NickNameDistanceConfig : ScriptableObject
{
	[SerializeField]
	private Vector3[] m_nicknameDistance = new Vector3[9]
	{
		new Vector3(10f, 1f, 1f),
		new Vector3(20f, 0.9f, 1f),
		new Vector3(30f, 0.8f, 1f),
		new Vector3(40f, 0.7f, 1f),
		new Vector3(50f, 0.6f, 1f),
		new Vector3(60f, 0.5f, 1f),
		new Vector3(70f, 0.4f, 0.8f),
		new Vector3(80f, 0.3f, 0.4f),
		new Vector3(90f, 0.2f, 0f)
	};

	[SerializeField]
	private Vector3[] m_nicknameDistanceVR = new Vector3[9]
	{
		new Vector3(1f, 1f, 1f),
		new Vector3(2f, 0.9f, 1f),
		new Vector3(3f, 0.8f, 1f),
		new Vector3(4f, 0.7f, 1f),
		new Vector3(5f, 0.6f, 1f),
		new Vector3(6f, 0.5f, 1f),
		new Vector3(7f, 0.4f, 0.8f),
		new Vector3(8f, 0.3f, 0.4f),
		new Vector3(9f, 0.2f, 0f)
	};

	public float GetAlphaFromDistance(float sqrDistance)
	{
		Vector3[] targetParams = GetTargetParams();
		float result = targetParams[0].z;
		int index = GetIndex(sqrDistance);
		if (index > 0)
		{
			result = Mathf.Lerp(targetParams[index - 1].z, targetParams[index].z, GetValueFromDistance(sqrDistance));
		}
		return result;
	}

	private Vector3[] GetTargetParams()
	{
		if (XRModeManager.IsXRActive())
		{
			return m_nicknameDistanceVR;
		}
		return m_nicknameDistance;
	}

	public float GetScaleFromDistance(float sqrDistance)
	{
		Vector3[] targetParams = GetTargetParams();
		float result = targetParams[0].y;
		int index = GetIndex(sqrDistance);
		if (index > 0)
		{
			result = Mathf.Lerp(targetParams[index - 1].y, targetParams[index].y, GetValueFromDistance(sqrDistance));
		}
		return result;
	}

	private int GetIndex(float sqrDistance)
	{
		Vector3[] targetParams = GetTargetParams();
		int num = 0;
		int num2 = targetParams.Length - 1;
		for (int i = 1; i <= num2; i++)
		{
			if (sqrDistance <= Mathf.Pow(targetParams[i].x, 2f) && sqrDistance > Mathf.Pow(targetParams[i - 1].x, 2f))
			{
				num = i;
			}
		}
		if (num == 0 && sqrDistance > Mathf.Pow(targetParams[num2].x, 2f))
		{
			num = num2;
		}
		return num;
	}

	private float GetValueFromDistance(float sqrDistance)
	{
		Vector3[] targetParams = GetTargetParams();
		int index = GetIndex(sqrDistance);
		float num = Mathf.Pow(targetParams[index - 1].x, 2f);
		float num2 = Mathf.Pow(targetParams[index].x, 2f);
		if (num2 == 0f)
		{
			Debug.LogError("Wrong distance in NickNameDistanceConfig table");
			return 0f;
		}
		num2 -= num;
		sqrDistance -= num;
		return sqrDistance / num2;
	}
}
