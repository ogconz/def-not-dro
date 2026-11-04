using UnityEngine;

[CreateAssetMenu(fileName = "WheelBurnoutData", menuName = "Particles/WheelBurnoutData")]
public class WheelBurnoutData : ScriptableObject
{
	public ParticleSystem particles;

	public float emissionRate = 60f;

	public float minSize = 0.3f;

	public float maxSize = 0.4f;

	public float minLifeTime = 0.8f;

	public float maxLifeTime = 1.6f;

	public float minAngularVelocity;

	public float maxAngularVelocity = 1000f;

	public float maxStartRotationAngle = 360f;

	public Color color = Color.white;
}
