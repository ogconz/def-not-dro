using System;
using UnityEngine;

[Serializable]
public class ClippingZoneSettings : ScriptableObject
{
	public const string RESOURCE_PATH = "ClippingZoneSettings";

	public Transform DefaultPoint;

	public Transform DefaultZone;

	public Transform CustomPoint;
}
