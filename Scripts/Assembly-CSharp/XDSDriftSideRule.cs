using SplineLogic;
using UnityEngine;

public class XDSDriftSideRule : MonoBehaviour
{
	public DriftControllerXDS.TargetSide targetSide;

	public float distance;

	private float? m_prevDistance;

	private ISpline m_spline;

	public void OnTriggerEnter(Collider other)
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
				controller.SetTargetSide(targetSide);
			}
		}
		base.enabled = false;
	}
}
