using System;
using UnityEngine;

[ExecuteInEditMode]
public class GateLaserScript : MonoBehaviour
{
	public Transform startPoint;

	public Transform endPoint;

	[NonSerialized]
	public LineRenderer laserLine;

	private void Start()
	{
		laserLine = GetComponentInChildren<LineRenderer>();
		laserLine.startWidth = 0.2f;
		laserLine.endWidth = 0.2f;
		laserLine.SetPosition(0, startPoint.position);
		laserLine.SetPosition(1, endPoint.position);
		laserLine.enabled = false;
	}
}
