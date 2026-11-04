using SplineLogic;
using UnityEngine;

public class XDSDriftSpeedRule : MonoBehaviour
{
	public float deltaV;

	public bool collTracking;

	[HideInInspector]
	public GameObject beginGO;

	[HideInInspector]
	public GameObject endGO;

	public float distance;

	public float length;

	private ISpline m_spline;

	private float? m_prevDistance;

	private float? m_prevLength;

	private void OnEnable()
	{
		ResetGateGOs();
	}

	public void OnZoneLineCross(Collider other, bool contact)
	{
		if (other == null || other.gameObject == null || other.gameObject.layer != 29)
		{
			return;
		}
		if ((bool)UIXDSInfoContext.instance)
		{
			DriftControllerXDS controller = UIXDSInfoContext.instance.controller;
			if ((bool)controller)
			{
				controller.SetSpeedControl(contact, collTracking && contact, contact ? deltaV : 0f);
			}
		}
		GameObject gameObject = (contact ? beginGO : endGO);
		if (gameObject.activeSelf)
		{
			gameObject.SetActive(value: false);
		}
	}

	private void ResetGateGOs()
	{
		beginGO.SetActive(value: true);
		endGO.SetActive(value: true);
	}
}
