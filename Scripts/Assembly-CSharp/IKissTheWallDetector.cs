using System;
using UnityEngine;

public interface IKissTheWallDetector
{
	event Action<Vector3, Vector3, float> OnWallDetectedEvent;
}
