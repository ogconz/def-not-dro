using CarX;
using UnityEngine;

[CreateAssetMenu(fileName = "WheelsParticlesData", menuName = "Particles/WheelsParticlesData")]
public class WheelsParticlesData : ScriptableObject
{
	public SurfaceType surfaceType;

	public ParticleSystem particles;

	public float emissionRate = 15f;

	public float minSize = 0.8f;

	public float maxSize = 1.6f;

	public float minLifeTime = 1f;

	public float maxLifeTime = 3f;

	public float minAngularVelocity = 10f;

	public float maxAngularVelocity = 30f;

	public float maxStartRotationAngle = 360f;

	public float velocityScale = 0.05f;

	public float inheritVelocity = 0.35f;

	public float maxDistance = 0.3f;

	public float velocityNormalFactor = 0.4f;

	public Color color = Color.white;
}
