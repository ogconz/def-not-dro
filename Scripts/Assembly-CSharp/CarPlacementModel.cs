using CarX;
using Cysharp.Threading.Tasks;
using SyncMultiplayer;
using UnityEngine;

public class CarPlacementModel
{
	private const float TeleportCooldown = 5f;

	private const float RayCastMaxDistance = 30f;

	private readonly RaceCar m_raceCar;

	private readonly Car m_carX;

	private float m_remainingCooldownTime;

	public int remainingCooldownTime => Mathf.CeilToInt(m_remainingCooldownTime);

	public CarPlacementModel(RaceCar raceCar)
	{
		m_raceCar = raceCar;
		m_carX = raceCar.carX;
	}

	public bool TryResetToWaypoint()
	{
		var (vector, rotation) = PlayerCarControl.instance.currentWayPoint;
		if (vector == Vector3.zero)
		{
			return false;
		}
		ResetToPoint(vector, rotation);
		return true;
	}

	public bool TryResetToPlayer(RaceCar raceCar)
	{
		if (raceCar == null || raceCar.isLocalPlayer || m_remainingCooldownTime != 0f)
		{
			return false;
		}
		ResetToPoint(raceCar.transform.position, raceCar.transform.rotation);
		ResetCooldownAsync().Forget();
		return true;
	}

	private async UniTaskVoid ResetCooldownAsync()
	{
		for (m_remainingCooldownTime = 5f; m_remainingCooldownTime > 0f; m_remainingCooldownTime -= Time.deltaTime)
		{
			await UniTask.Yield();
		}
		m_remainingCooldownTime = 0f;
	}

	public void ResetToPoint(Vector3 position, Quaternion rotation)
	{
		if (IsPlayerCarValid())
		{
			m_carX.getTransform.SetPositionAndRotation(position, rotation);
			FlipCar();
		}
	}

	private bool IsPlayerCarValid()
	{
		if (m_carX != null)
		{
			return m_carX.getTransform != null;
		}
		return false;
	}

	private void ResetPlayerCarSettings()
	{
		Rigidbody getRigidbody = m_carX.getRigidbody;
		getRigidbody.velocity = Vector3.zero;
		getRigidbody.angularVelocity = Vector3.zero;
		m_carX.gear = 0;
		m_carX.ResetTransmission();
	}

	public void FlipCar(bool rotateToForward = false)
	{
		AlignCarPositionAtSpawn(adjustRotation: true, rotateToForward);
		ResetPlayerCarSettings();
		SetNetworkCarPlacementIfNeed();
	}

	private void SetNetworkCarPlacementIfNeed()
	{
		if (!m_raceCar.isNetworkCar)
		{
			NetworkController.InstanceGame.OnLocalPlayerPlacedOnTrack();
		}
	}

	public void HoldGaragePosition(bool hold)
	{
		Rigidbody getRigidbody = m_carX.getRigidbody;
		if (hold)
		{
			getRigidbody.constraints = (RigidbodyConstraints)42;
		}
		else
		{
			getRigidbody.constraints = RigidbodyConstraints.None;
		}
	}

	public void AlignCarPositionAtSpawn(bool adjustRotation = true, bool rotateToForward = false)
	{
		if (Physics.Raycast(new Ray(m_raceCar.getTransform.position + Vector3.up * 2f, Vector3.down), out var hitInfo, 30f, 2048) && TryGetDiscDiamCoeff(out var discDiam))
		{
			Vector3 normal = hitInfo.normal;
			m_raceCar.getTransform.position = hitInfo.point + Vector3.Normalize(normal) * discDiam;
			if (adjustRotation || m_raceCar.carId == 108)
			{
				Vector3 forward = m_raceCar.getTransform.forward;
				Vector3 normalized = (forward - Vector3.Dot(forward, normal) * normal).normalized;
				m_raceCar.getTransform.rotation = Quaternion.LookRotation(rotateToForward ? m_raceCar.getTransform.forward : normalized, normal);
			}
		}
	}

	public bool TryGetDiscDiamCoeff(out float discDiam, bool checkHeight = true)
	{
		discDiam = 0f;
		CarDesc desc = null;
		m_carX.GetCarDesc(ref desc);
		if (desc == null)
		{
			return false;
		}
		float num = Mathf.Max(desc.frontTyre.profile, desc.rearTyre.profile);
		float num2 = Mathf.Max(desc.frontTyre.discDiam, desc.rearTyre.discDiam);
		if (checkHeight)
		{
			num = ((num > 50f) ? (num - 50f) : 0f);
			num2 = ((num2 > 18f) ? num2 : 0f) / 200f;
		}
		discDiam = num / 100f + num2;
		return true;
	}
}
