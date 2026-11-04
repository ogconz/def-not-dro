using System;
using System.Collections.Generic;
using CarX;
using CodeStage.AntiCheat.ObscuredTypes;
using Cysharp.Threading.Tasks;
using DB.Meta;
using DI;
using GameOverlay;
using SplineLogic;
using UnityEngine;

namespace SyncMultiplayer
{
	public class NetworkPlayer
	{
		public enum TextureLoadState
		{
			NotLoaded = 0,
			Loading = 1,
			Downloaded = 2
		}

		public class PingQualityFlag
		{
			private readonly int m_maxPing;

			private readonly int m_setToUp;

			private readonly int m_setToDown;

			private int m_setCounter;

			private int m_lastPing;

			public bool flag { get; private set; }

			public PingQualityFlag(int maxPing, int setToUp, int setToDown)
			{
				m_maxPing = maxPing;
				m_setToUp = setToUp;
				m_setToDown = setToDown;
			}

			public void SetPing(int ping)
			{
				if (ping > m_maxPing)
				{
					if (!flag)
					{
						m_setCounter++;
						if (m_setCounter > m_setToUp)
						{
							flag = true;
							m_setCounter = 0;
						}
					}
				}
				else if (ping < m_maxPing && flag)
				{
					m_setCounter++;
					if (m_setCounter > m_setToDown)
					{
						flag = false;
						m_setCounter = 0;
					}
				}
			}
		}

		public class Points
		{
			private NetworkPlayer m_owner;

			public int generation;

			private ObscuredInt m_points;

			private ObscuredInt m_maxPoints;

			private ObscuredInt m_driftPoints;

			public int raceProgress;

			private ObscuredFloat m_raceTime;

			private ObscuredInt m_RAWDriftPoints;

			public int points
			{
				get
				{
					return m_points;
				}
				set
				{
					m_points = value;
					if (m_owner.IsLocal)
					{
						m_owner.m_game.stats.pointsAll = value;
					}
				}
			}

			public int maxPoints
			{
				get
				{
					return m_maxPoints;
				}
				set
				{
					m_maxPoints = value;
					if (m_owner.IsLocal && (int)m_maxPoints > m_owner.m_game.stats.pointsPerDrift)
					{
						m_owner.m_game.stats.pointsPerDrift = m_maxPoints;
					}
				}
			}

			public int driftPoints
			{
				get
				{
					return m_driftPoints;
				}
				set
				{
					m_driftPoints = value;
				}
			}

			public float raceTime
			{
				get
				{
					return m_raceTime.Get();
				}
				set
				{
					m_raceTime.Set(value);
				}
			}

			public int RAWDriftPoints
			{
				get
				{
					return m_RAWDriftPoints;
				}
				set
				{
					m_RAWDriftPoints = value;
				}
			}

			public Points(NetworkPlayer owner)
			{
				generation = 0;
				m_owner = owner;
				Clear(allData: true);
			}

			public void Clear(bool allData)
			{
				driftPoints = 0;
				raceProgress = 0;
				raceTime = 0f;
				if (allData)
				{
					RAWDriftPoints = 0;
					points = 0;
					maxPoints = 0;
				}
			}
		}

		public class NetworkPlayerColor
		{
			private int m_color = -1;

			private Color m_selectedColor;

			private static List<byte> m_colorPool = new List<byte>();

			private static List<byte> m_activeColors = new List<byte>();

			private static MinimapIconSettings m_settings = null;

			public Color color => m_selectedColor;

			public NetworkPlayerColor(bool isLocal)
			{
				Initialize();
				if (!isLocal && m_colorPool.Count > 0)
				{
					int index = new System.Random().Next(0, m_colorPool.Count - 1);
					m_color = m_colorPool[index];
					m_colorPool.RemoveAt(index);
					m_activeColors.Add((byte)m_color);
					m_selectedColor = m_settings.networkColors[m_color];
				}
				else
				{
					m_color = -1;
					m_selectedColor = Color.gray;
				}
			}

			public void Release()
			{
				if (m_color != -1)
				{
					m_activeColors.Remove((byte)m_color);
					m_colorPool.Add((byte)m_color);
					m_color = -1;
					m_selectedColor = Color.gray;
				}
			}

			private void Initialize()
			{
				if (m_settings == null)
				{
					m_settings = ScriptableSingleton<MinimapIconSettings>.instance;
					for (byte b = 0; b < m_settings.networkColors.Length; b++)
					{
						m_colorPool.Add(b);
					}
				}
			}
		}

		private const string CONFIG_STYLE_SYNC_NETWORK = "SyncNetwork";

		private const string CONFIG_STYLE_DEFAULT = "default";

		private const string CONFIG_STYLE_DEFAULT_NETWORK = "defaultnetwork";

		public byte SlotID;

		public string collideGroup = "";

		public int NetworkID;

		private SubroomInfo m_attachedRoom;

		public byte spawnGeneration;

		private Dictionary<CarTextureType, Texture2D> m_playerTextures = new Dictionary<CarTextureType, Texture2D>();

		private NetworkPlayerColor m_playerColor;

		public readonly Points points;

		public bool ugcCarReadyForConstruction;

		public Vector3 lastRecivedNetworkPosition;

		public Quaternion lastRecivedNetworkRotation;

		public Action<NetworkPlayer> OnCarAttached;

		public Action onFilteredNicknameUpdated;

		private RaceCar m_ugcCar;

		private int m_ping;

		private NetworkGame m_game;

		private PingQualityFlag m_badQualityPing = new PingQualityFlag(100, 2, 20);

		private PingQualityFlag m_veryBadQualityPing = new PingQualityFlag(150, 10, 10);

		private bool m_dead;

		private string m_originalNickName;

		private string m_filteredName;

		private string m_filteredNameWithPlatformIcon;

		public bool IsMasterClient;

		public PlayerId PlayerId
		{
			get
			{
				return PlayerProperties.PlayerID;
			}
			set
			{
				PlayerProperties.PlayerID = value;
			}
		}

		public SubroomInfo attachedRoom
		{
			get
			{
				return m_attachedRoom;
			}
			set
			{
				if (m_attachedRoom == value)
				{
					return;
				}
				if (m_attachedRoom != null)
				{
					m_attachedRoom.runtimePlayers.Remove(this);
				}
				value?.runtimePlayers.Add(this);
				m_attachedRoom = value;
				if (IsLocal && (bool)PlayerCarControl.instance)
				{
					userCar = PlayerCarControl.instance.car;
					if ((bool)userCar)
					{
						userCar.networkPlayer = this;
					}
				}
			}
		}

		public bool isNetworkCarLoading { get; private set; }

		public Color networkColor => m_playerColor.color;

		public RaceCar userCar { get; set; }

		public PlayerCarFactory carFactory { get; private set; }

		public string OriginaNickName => m_originalNickName;

		public string FilteredNickName => m_filteredName;

		public string FilteredNickNameWithPlatformIcon => m_filteredNameWithPlatformIcon;

		public SmartfoxPlayerProperties PlayerProperties { get; private set; }

		public bool IsLocal { get; private set; }

		public int Ping
		{
			get
			{
				return m_ping;
			}
			set
			{
				m_badQualityPing.SetPing(value);
				m_veryBadQualityPing.SetPing(value);
				m_ping = value;
			}
		}

		public bool BadConnection => m_badQualityPing.flag;

		public bool VeryBadConnection => m_veryBadQualityPing.flag;

		public void SetPlayerTexture(CarTextureType textureType, Texture2D texture)
		{
			if (m_playerTextures.TryGetValue(textureType, out var value))
			{
				UnityEngine.Object.Destroy(value);
				value = null;
			}
			m_playerTextures[textureType] = texture;
		}

		public void ClearTextures(CarTextureType[] carTextureTypes)
		{
			if (IsLocal || carTextureTypes == null)
			{
				return;
			}
			foreach (CarTextureType carTextureType in carTextureTypes)
			{
				if (carTextureType != CarTextureType.Unknown && m_playerTextures.TryGetValue(carTextureType, out var value))
				{
					UnityEngine.Object.Destroy(value);
					m_playerTextures[carTextureType] = null;
				}
			}
			m_playerTextures.Clear();
		}

		public void ApplyTextures()
		{
			m_playerTextures.TryGetValue(CarTextureType.TextureBody, out var value);
			m_playerTextures.TryGetValue(CarTextureType.TextureGlass, out var value2);
			m_playerTextures.TryGetValue(CarTextureType.MaterialBody, out var value3);
			m_playerTextures.TryGetValue(CarTextureType.MaterialGlass, out var value4);
			if ((bool)userCar)
			{
				userCar.carModel?.OverrideVinylWrapTexture(value, value2, value3, value4);
			}
		}

		public NetworkPlayer(NetworkGame game, bool isLocal, SmartfoxPlayerProperties playerProperties)
		{
			m_game = game;
			points = new Points(this);
			carFactory = DependencyInjector.Resolve<PlayerCarFactory>();
			PlayerProperties = playerProperties;
			IsLocal = isLocal;
			m_playerColor = new NetworkPlayerColor(isLocal);
			if (isLocal && (bool)PlayerCarControl.instance)
			{
				userCar = PlayerCarControl.instance.car;
			}
			m_originalNickName = playerProperties.Name;
			if (!string.IsNullOrEmpty(m_originalNickName))
			{
				UpdateFilteredName(Overlay.filterTextManager.GetFilteredCachedText(TextFilterType.Nickname, PlayerProperties.PlayerID.platform, PlayerProperties.Name, UpdateFilteredName));
			}
		}

		private void UpdateFilteredName(string rawFilteredName)
		{
			m_filteredName = UIUtils.ConvertNGUIColorToRich(rawFilteredName);
			m_filteredNameWithPlatformIcon = m_filteredName + UIUtils.GetPlatfomIconTagIfNeed(PlayerId.platform);
			if (userCar != null)
			{
				userCar.UpdateNickName(m_filteredNameWithPlatformIcon);
			}
			if (attachedRoom != null)
			{
				attachedRoom.UpdatePlayerNickName(this);
			}
			onFilteredNicknameUpdated.SafeCall();
		}

		public void NetworkSpawn(Vector3 position, Quaternion rotation, bool respawn)
		{
			lastRecivedNetworkPosition = position;
			lastRecivedNetworkRotation = rotation;
			NetworkSpawnAsync(respawn).Forget();
		}

		public async UniTask NetworkSpawnAsync(bool respawn)
		{
			if (m_ugcCar != null)
			{
				AttachCar(m_ugcCar, respawn);
				m_ugcCar = null;
				return;
			}
			int num = PlayerProperties.CarId;
			_ = PlayerProperties.ProfileId;
			int swapId = PlayerProperties.SwapId;
			CarVisualManager.VisualSettings visualSettings = PlayerProperties.VisualSettings;
			ProfilesInfo.Profile profile = PlayerProperties.Profile;
			PlayerCar byId = NetworkController.InstanceGame.baseModel.GetById<PlayerCar>(num);
			if (byId == null || !byId.enabled || byId.forcedDisable)
			{
				num = 1;
				if (visualSettings != null)
				{
					visualSettings = visualSettings.Copy();
					visualSettings.parts.Clear();
					visualSettings.networkparts.Clear();
				}
				profile = null;
			}
			isNetworkCarLoading = true;
			RaceCar raceCar = await carFactory.CreateNetworkRaceCar(this, num, profile, swapId, visualSettings);
			isNetworkCarLoading = false;
			if ((bool)m_ugcCar)
			{
				if ((bool)raceCar)
				{
					UnityEngine.Object.Destroy(raceCar.gameObject);
				}
				raceCar = m_ugcCar;
			}
			AttachCar(raceCar, respawn);
		}

		private bool AttachCar(RaceCar car, bool respawn)
		{
			if (userCar != null)
			{
				UnityEngine.Object.Destroy(userCar.gameObject);
				userCar = null;
			}
			userCar = car;
			if (userCar != null)
			{
				if (!m_dead || IsLocal)
				{
					userCar.networkColor = networkColor;
					userCar.networkPlayer = this;
					userCar.SpawnNickNameUIPrefab(FilteredNickNameWithPlatformIcon);
					if (respawn)
					{
						Spawn(null);
					}
					userCar.carX.gearShifting = GearShiftType.Manual;
					userCar.carX.autoClutch = false;
					if (m_ugcCar != null)
					{
						foreach (Transform item in userCar.carModel.skeleton.root)
						{
							if (string.Equals(item.name, "skeleton", StringComparison.OrdinalIgnoreCase))
							{
								UnityEngine.Object.Destroy(item.gameObject);
								break;
							}
						}
					}
					OnCarAttached.SafeCall(this);
					return true;
				}
				UnityEngine.Object.Destroy(userCar.gameObject);
				userCar = null;
			}
			OnCarAttached.SafeCall(null);
			return false;
		}

		public async void ConstructUGCCar()
		{
			ugcCarReadyForConstruction = false;
			if (!m_dead)
			{
				int carId = PlayerProperties.CarId;
				int swapId = PlayerProperties.SwapId;
				CarVisualManager.VisualSettings visualSettings = PlayerProperties.VisualSettings;
				ProfilesInfo.Profile profile = PlayerProperties.Profile;
				GrabUGCCar(await carFactory.CreateNetworkRaceCar(this, carId, profile, swapId, visualSettings.Copy()));
			}
		}

		public void GrabUGCCar(RaceCar car)
		{
			if (car == null)
			{
				return;
			}
			if (m_ugcCar != null)
			{
				UnityEngine.Object.Destroy(m_ugcCar.gameObject);
			}
			m_ugcCar = null;
			if (m_dead)
			{
				UnityEngine.Object.Destroy(car.gameObject);
				return;
			}
			if (isNetworkCarLoading)
			{
				m_ugcCar = car;
				return;
			}
			m_ugcCar = car;
			if (userCar != null)
			{
				if (CameraSwitch.instance.GetCar() == userCar)
				{
					CameraSwitch.instance.SetCar(m_ugcCar);
				}
				UnityEngine.Object.Destroy(userCar.gameObject);
				userCar = null;
			}
		}

		public NetworkSubraceDescriptor.Descriptor Restart(string startName, int position, bool center, bool oddShift, bool isLeader = true, bool resetStatsEvents = true)
		{
			NetworkSubraceDescriptor.Descriptor result = Spawn(startName, position, center, oddShift, isLeader);
			if (IsLocal && (bool)PlayerCarControl.instance)
			{
				spawnGeneration++;
				SyncNetFreerideRaceModeState syncNetFreerideRaceModeState = States.mainCurrent as SyncNetFreerideRaceModeState;
				if ((bool)syncNetFreerideRaceModeState)
				{
					syncNetFreerideRaceModeState.Reset(spawnCar: false, resetStatsEvents);
					syncNetFreerideRaceModeState.ResetPoints();
					points.Clear(allData: false);
					syncNetFreerideRaceModeState.driftController.allowRepeatDrift = false;
				}
			}
			return result;
		}

		public void FlushMapToDefault()
		{
			ConfigurationManager instance = ConfigurationManager.instance;
			if ((bool)instance && instance.GetAndActivateData<CheckpointsData>("defaultnetwork") == null)
			{
				instance.GetAndActivateData<CheckpointsData>("default");
			}
		}

		public void ShowMap(string name = "")
		{
			ConfigurationManager instance = ConfigurationManager.instance;
			if (!instance)
			{
				return;
			}
			string text = ((instance.lastNetworkPoint != null) ? instance.lastNetworkPoint.name : null);
			if (attachedRoom != null)
			{
				if (!string.IsNullOrEmpty(text) && (int)attachedRoom.state < 2 && attachedRoom.pointName.Equals(text))
				{
					return;
				}
				if (string.IsNullOrEmpty(name) && (int)attachedRoom.state >= 1)
				{
					name = attachedRoom.pointName;
				}
			}
			if (!string.IsNullOrEmpty(name))
			{
				if ((string.IsNullOrEmpty(text) || !text.Equals(name)) && (attachedRoom == null || (int)attachedRoom.state < 4))
				{
					instance.GetAndActivateNetworkData<CheckpointsData>(name);
				}
			}
			else
			{
				FlushMapToDefault();
			}
		}

		private NetworkSubraceDescriptor.Descriptor Spawn(string startName, int position = 0, bool center = true, bool oddShift = false, bool isLeader = true)
		{
			if (userCar == null)
			{
				if (!IsLocal)
				{
					return null;
				}
				userCar = PlayerCarControl.instance.car;
				if (userCar == null)
				{
					return null;
				}
			}
			ConfigurationManager instance = ConfigurationManager.instance;
			if (!instance)
			{
				return null;
			}
			CheckpointsData checkpointsData = instance.GetAndActivateNetworkData<CheckpointsData>(startName, isLeader);
			if (checkpointsData == null)
			{
				checkpointsData = instance.GetDataByType<CheckpointsData>();
			}
			NetworkSubraceDescriptor networkSubraceDescriptor = (instance.lastNetworkPoint ? instance.lastNetworkPoint.GetComponentInChildren<NetworkSubraceDescriptor>() : null);
			NetworkSubraceDescriptor.Descriptor result = ((networkSubraceDescriptor == null) ? NetworkSubraceDescriptor.GetDefault() : networkSubraceDescriptor.descriptor);
			StartLine startLine = checkpointsData.GetStartLine();
			if ((bool)Track.instance)
			{
				ISpline centerLine = Track.instance.centerLine;
				Vector3 pnt = startLine.transform.position;
				centerLine.ProjectPoint(pnt, out float along, out float _);
				int num = position / 2;
				float num2 = -1f;
				if (oddShift && position % 2 == 0)
				{
					num2 = -5f;
				}
				float num3 = userCar.carX.bounds.max.z + 8f * (float)num - num2;
				float distance = along - num3;
				centerLine.SamplePoint(distance, out var pnt2, out var vel);
				vel = vel.normalized;
				float num4 = Vector3.Dot(pnt2 - pnt, -vel);
				if (Mathf.Abs(num3 - num4) > 0.1f)
				{
					centerLine.SamplePoint(along, out pnt, out var vel2);
					vel2 = vel2.normalized;
					pnt2 = pnt - vel2 * num3;
					centerLine.ProjectPoint(pnt2, out pnt2, out vel);
					vel = vel.normalized;
				}
				if (!center)
				{
					vel = new Vector3(0f - vel.z, vel.y, vel.x) * 1.5f;
					if (position % 2 == 0)
					{
						vel = -vel * 1.5f;
					}
					pnt2 += vel;
				}
				Transform getTransform = userCar.getTransform;
				getTransform.rotation = startLine.transform.rotation;
				getTransform.position = pnt2;
			}
			else
			{
				Transform getTransform2 = userCar.getTransform;
				getTransform2.rotation = startLine.transform.rotation;
				getTransform2.position = startLine.transform.position - getTransform2.forward * (userCar.lengthFront + 1f);
			}
			userCar.AlignCarPositionAtSpawn();
			userCar.getRigidbody.velocity = Vector3.zero;
			userCar.getRigidbody.angularVelocity = Vector3.zero;
			return result;
		}

		public void OnLeave()
		{
			if ((bool)NetworkController.InstanceGame.systems)
			{
				NetworkController.InstanceGame.systems.DoPlayerLeave(this);
			}
			attachedRoom = null;
			if (m_playerColor != null)
			{
				m_playerColor.Release();
			}
			if ((bool)userCar && !IsLocal)
			{
				UnityEngine.Object.Destroy(userCar.gameObject);
				userCar = null;
			}
			m_dead = true;
		}

		public void OnPostLeave()
		{
			if ((bool)NetworkController.InstanceGame.systems)
			{
				NetworkController.InstanceGame.systems.PostPlayerLeave(this);
			}
		}
	}
}
