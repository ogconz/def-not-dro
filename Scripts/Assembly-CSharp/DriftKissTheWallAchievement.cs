using System.Collections;
using SyncMultiplayer;
using UnityEngine;

public class DriftKissTheWallAchievement : BaseDriftAchievement
{
	private const string KTW_CONFIG_PATH = "KissTheWall/KissTheWallCarConfig";

	private readonly Vector2[] DataTable;

	private float m_timer;

	private float m_carRearWheelsOffset;

	private RaceCar m_raceCar;

	private KissTheWallChecker m_checker;

	private KissTheWallCarConfig m_config;

	private Coroutine m_pauseCoroutine;

	private NetGameSubroomsSystem m_roomsController;

	public DriftKissTheWallAchievement()
	{
		Reset();
	}

	public DriftKissTheWallAchievement(DriftController driftController, Vector2[] data)
	{
		m_config = ResourcesWrapper.Load<KissTheWallCarConfig>("KissTheWall/KissTheWallCarConfig", "race_state");
		Init(driftController);
		DataTable = data;
		base.AchieveType = AchievementType.KissTheWall;
		m_driftController.driftFailedEvent += Reset;
		m_raceCar.OnCollisionEnterEvent += CollisionHandler;
		m_carRearWheelsOffset = m_raceCar.GetDesc().rearBaseZ;
	}

	public override void Update(float dt)
	{
		if (m_driftController == null || (m_roomsController != null && m_roomsController.myRoom != null && !IsAllowToShowInSubroom(m_roomsController.myRoom)))
		{
			return;
		}
		if (m_driftController.driftState != DriftController.DriftState.Drift)
		{
			m_timer = 0f;
			m_isTracking = false;
			return;
		}
		m_isTracking = (m_pauseCoroutine == null && m_checker.CheckAllEntryConditions()) || m_isTracking;
		if (m_isTracking)
		{
			int num = FindIndex(m_timer, DataTable);
			if (num > -1)
			{
				onAchievementReceive(AchievementType.KissTheWall, 1, DataTable[num].y);
				m_timer = 0f;
				m_pauseCoroutine = m_driftController.StartCoroutine(PauseAchievementCoroutine(m_config.delayAfterAchieve));
				m_isTracking = false;
			}
			else
			{
				m_timer += dt;
			}
		}
	}

	public override void Reset()
	{
		if ((bool)m_driftController)
		{
			m_timer = 0f;
			if (m_pauseCoroutine != null)
			{
				m_driftController.StopCoroutine(m_pauseCoroutine);
			}
			m_pauseCoroutine = null;
			base.Reset();
		}
	}

	public override void Init(DriftController driftController)
	{
		base.Init(driftController);
		m_raceCar = m_driftController.raceCar;
		BaseRaceModeState baseRaceModeState = States.mainCurrent as BaseRaceModeState;
		NetworkGame instanceGame = NetworkController.InstanceGame;
		if (baseRaceModeState != null && baseRaceModeState.raceMode == RaceMode.NetworkFreeride && instanceGame != null)
		{
			m_roomsController = instanceGame.systems.Get<NetGameSubroomsSystem>();
		}
		m_checker = m_raceCar.gameObject.AddComponent<KissTheWallChecker>();
		m_checker.Init(m_driftController, m_config);
		m_checker.SubscribeWallDetection(ShowWallDetectionUI);
	}

	public void CollisionHandler(Collision collisionInfo)
	{
		if ((collisionInfo.impulse / (1000f * Time.fixedDeltaTime)).sqrMagnitude > m_config.maxImpactImpulseSqr)
		{
			Reset();
		}
		else
		{
			if (collisionInfo.collider.gameObject.layer == 8)
			{
				return;
			}
			for (int i = 0; i < collisionInfo.contactCount; i++)
			{
				if (m_raceCar.transform.InverseTransformPoint(collisionInfo.GetContact(i).point).z - m_carRearWheelsOffset > 0f)
				{
					Reset();
				}
			}
		}
	}

	private void ShowWallDetectionUI(Vector3 carPoint, Vector3 wallPoint, float distance)
	{
		UIKissTheWallContext.ShowDistanceToTheWall(carPoint, wallPoint, distance);
	}

	private IEnumerator PauseAchievementCoroutine(float pauseTime)
	{
		float timer = pauseTime;
		while (timer >= 0f)
		{
			timer -= Time.deltaTime;
			yield return null;
		}
		m_pauseCoroutine = null;
	}

	private bool IsAllowToShowInSubroom(SubroomInfo subroom)
	{
		if (subroom == null)
		{
			return false;
		}
		if (subroom.state != SubroomState.WaitingPlayers)
		{
			return subroom.subroomMode == SubroomMode.Challenge;
		}
		return true;
	}
}
