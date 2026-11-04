using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Binding;
using Binding.Base;
using DB;
using DB.Meta;
using DI;
using GameInput;
using JukeBox;
using TrackLayout.Behaviours;
using UnityEngine;
using UnityEngine.U2D;

[GameHotkeyBlocker(true)]
[AudioFXBlocker(true)]
public class UILoadingScreenContext : BaseBindingBehaviourTarget
{
	public GameObject minimapNode;

	public bool autoContinue;

	public const int MaxLeague = 5;

	[Dependency]
	private BaseModel m_model;

	[Dependency]
	private GamePrefs m_prefs;

	[Dependency]
	private MultiplayerManager m_multiplayerManager;

	private string m_tmpTrackIconName;

	private bool m_isExternalTrack;

	[SerializeField]
	private int m_maxHintNumber = 20;

	[SerializeField]
	private SpriteAtlas m_setupsAtlas;

	private int m_hintIndex;

	private Coroutine m_waitAndContinuePlayCoroutine;

	public readonly Property<int> modeType = new IntProperty();

	public readonly Property<string> modeTime = new StringProperty();

	public readonly Property<string> yourResult = new StringProperty();

	public readonly Property<string> modeName = new StringProperty();

	public readonly Property<bool> isNetworkRace = new BoolProperty();

	public readonly Property<int> raceMode = new IntProperty();

	public readonly Property<bool> visibleCups = new BoolProperty();

	public readonly Property<bool> visibleXDS = new BoolProperty();

	public readonly Property<string> hintName = new StringProperty();

	public readonly Property<string> pointsTitle = new StringProperty();

	public readonly Property<string> cup1Data = new StringProperty();

	public readonly Property<string> cup1Cost = new StringProperty();

	public readonly Property<string> cup2Data = new StringProperty();

	public readonly Property<string> cup2Cost = new StringProperty();

	public readonly Property<string> cup3Data = new StringProperty();

	public readonly Property<string> cup3Cost = new StringProperty();

	public readonly Property<string> trackName = new StringProperty();

	public readonly TextureProperty trackIcon = new TextureProperty();

	public readonly Property<string> carName = new StringProperty();

	public readonly Property<string> profileName = new StringProperty();

	public readonly Property<bool> loaded = new BoolProperty();

	public readonly Property<string> leagueName = new StringProperty();

	public readonly Property<string> attackerName = new StringProperty();

	public readonly Property<string> defenderName = new StringProperty();

	public readonly Property<string> attackerRating = new StringProperty();

	public readonly Property<string> defenderRating = new StringProperty();

	public readonly Property<string> defenderCarName = new StringProperty();

	public readonly Property<string> defenderProfileName = new StringProperty();

	public readonly BoolProperty isReverse = new BoolProperty();

	private readonly SpriteProperty m_setupIcon = new SpriteProperty();

	public System.Action ContinueClickEvent;

	private string m_trackIconName
	{
		get
		{
			return m_tmpTrackIconName;
		}
		set
		{
			if (m_tmpTrackIconName != value)
			{
				m_tmpTrackIconName = value;
				LoadResources();
			}
		}
	}

	public void OnContinueClick()
	{
		if (loaded.value)
		{
			ContinueClickEvent.Call();
		}
	}

	[GameHotkey(ActionType.NavCancelBtnB, false, false)]
	public void OnCancel()
	{
		if (loaded.value)
		{
			ContinueClickEvent.Call();
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		isReverse.value = false;
		FillInfo();
		loaded.value = false;
		hintName.value = GetHintName();
		SoundController.PlayMusic(MusicSignal.Silence);
		UISpinner.Hide();
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		if (m_waitAndContinuePlayCoroutine != null)
		{
			StopCoroutine(m_waitAndContinuePlayCoroutine);
			m_waitAndContinuePlayCoroutine = null;
		}
		UnloadResources();
		base.owner.ClearFromCache(base.name);
	}

	private void FillInfo()
	{
		isNetworkRace.value = m_multiplayerManager.isInNetworkRace;
		if (m_multiplayerManager.isInNetworkRace)
		{
			visibleCups.value = false;
			switch (m_multiplayerManager.currentState)
			{
			case MultiplayerManager.EMultiplayerState.TrainingRace:
				raceMode.value = 1;
				FillNetworkRaceInfo();
				break;
			case MultiplayerManager.EMultiplayerState.QualificationRace:
				raceMode.value = 2;
				FillNetworkRaceInfo();
				break;
			case MultiplayerManager.EMultiplayerState.BattleRace:
			case MultiplayerManager.EMultiplayerState.Replay:
				raceMode.value = 3;
				FillNetworkRaceInfo();
				break;
			case MultiplayerManager.EMultiplayerState.TopReplay:
				raceMode.value = 3;
				FillNetworkRaceInfo(m_multiplayerManager.GetCurrentTopReplay());
				break;
			}
		}
		else
		{
			visibleCups.value = true;
			raceMode.value = 0;
			FillLocalRaceInfo();
		}
	}

	private void FillNetworkRaceInfo(TopReplay topReplay)
	{
		if (topReplay == null)
		{
			Debug.LogError("UILoadingScreenContext::FillNetworkRaceInfo(): topReplay is null.");
			FillNetworkRaceInfo();
			return;
		}
		int carId = topReplay.userData.carId;
		int profileId = topReplay.userData.profileId;
		int engineIdForCar = m_prefs.carSettings.GetEngineIdForCar(carId);
		string trackScene = topReplay.mapInfo.mapName;
		DB.Meta.Track track = m_model.QueryAll<DB.Meta.Track>().Find((DB.Meta.Track t) => t.scene == trackScene);
		FillTrackInfo(track.id);
		FillCarInfo(carId, profileId, engineIdForCar, carName, profileName);
		int carId2 = topReplay.oppData.carId;
		int profileId2 = topReplay.oppData.profileId;
		FillCarInfo(carId2, profileId2, engineIdForCar, defenderCarName, defenderProfileName);
		if (topReplay.leagueId == 0)
		{
			leagueName.value = LocalizationKeys.multiplayer_top_best.Localized();
		}
		else
		{
			string postfix = (5 - topReplay.leagueId + 1).ToString();
			leagueName.value = LocalizationKeys.league_caps.Localized() + " " + LocalizationKeys.mp_league_.AddLocalized(postfix);
		}
		attackerName.value = topReplay.userName;
		attackerRating.value = (topReplay.userElo.HasValue ? topReplay.userElo.Value.ToString() : LocalizationKeys.mp_no_rating.Localized());
		defenderName.value = topReplay.oppName;
		defenderRating.value = (topReplay.oppElo.HasValue ? topReplay.oppElo.Value.ToString() : LocalizationKeys.mp_no_rating.Localized());
	}

	private void FillNetworkRaceInfo()
	{
		int lastCarId = m_prefs.globals.lastCarId;
		int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(lastCarId);
		int engineIdForCar = m_prefs.carSettings.GetEngineIdForCar(lastCarId);
		string trackScene = m_multiplayerManager.currentLeague.clientSettings.mapName;
		DB.Meta.Track track = m_model.QueryAll<DB.Meta.Track>().Find((DB.Meta.Track t) => t.scene == trackScene);
		FillTrackInfo(track.id);
		FillCarInfo(lastCarId, profileIdForCar, engineIdForCar, carName, profileName);
		MultiplayerGhost opponentGhost = m_multiplayerManager.GetOpponentGhost();
		if (opponentGhost != null)
		{
			int carId = opponentGhost.settings.carId;
			int profileId = opponentGhost.settings.profileId;
			FillCarInfo(carId, profileId, engineIdForCar, defenderCarName, defenderProfileName);
		}
		leagueName.value = LocalizationKeys.league_caps.Localized() + " " + LocalizationService.Localize(m_multiplayerManager.currentLeague.name);
		User currentUser = m_multiplayerManager.currentUser;
		attackerName.value = currentUser.nickname;
		attackerRating.value = (currentUser.elo.HasValue ? currentUser.elo.Value.ToString() : LocalizationKeys.mp_no_rating.Localized());
		Opponent currentOpponent = m_multiplayerManager.currentOpponent;
		if (currentOpponent != null)
		{
			defenderName.value = currentOpponent.nickname;
			defenderRating.value = currentOpponent.elo.ToString();
		}
	}

	private void FillLocalRaceInfo()
	{
		int lastCarId = m_prefs.globals.lastCarId;
		int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(lastCarId);
		int lastTrackId = m_prefs.globals.lastTrackId;
		int engineIdForCar = m_prefs.carSettings.GetEngineIdForCar(lastCarId);
		RaceMode raceMode = RaceMode.Drift;
		LoadRaceState loadRaceState = States.State<LoadRaceState>();
		raceMode = ((loadRaceState != null) ? loadRaceState.currentRaceMode : m_prefs.stats.GetLastRaceModeForTrack(lastTrackId));
		modeType.value = (int)raceMode;
		modeName.value = UIUtils.GetRaceModeLocalizedName(raceMode);
		visibleCups.value = raceMode != RaceMode.Dyno && raceMode != RaceMode.Freeride && raceMode != RaceMode.NetworkFreeride && raceMode != RaceMode.XDS && raceMode != RaceMode.HalloweenEvent && raceMode != RaceMode.Top32 && raceMode != RaceMode.Top32NetRace && raceMode != RaceMode.Top32ViewReplay;
		visibleXDS.value = raceMode == RaceMode.XDS;
		if (raceMode == RaceMode.Dyno)
		{
			this.raceMode.value = 4;
			FillDynoInfo();
			return;
		}
		FillTrackInfo(lastTrackId);
		UIUtils.GetRaceResult(lastTrackId, raceMode);
		switch (raceMode)
		{
		case RaceMode.Drift:
			yourResult.value = UIUtils.MakeViewBestDriftPoints(lastTrackId);
			break;
		case RaceMode.TimeAttack:
			yourResult.value = UIUtils.MakeViewBestTimeAttackPoints(lastTrackId);
			break;
		default:
			yourResult.value = "";
			break;
		case RaceMode.XDS:
			break;
		}
		FillCarInfo(lastCarId, profileIdForCar, engineIdForCar, carName, profileName);
		FillCupsInfo(lastCarId, lastTrackId, raceMode);
	}

	private void FillTrackInfo(int trackId)
	{
		DB.Meta.Track byId = m_model.GetById<DB.Meta.Track>(trackId);
		m_isExternalTrack = byId.external;
		trackName.value = (m_isExternalTrack ? byId.name.Substring(0, Math.Min(byId.name.Length, 45)) : LocalizationService.Localize(byId.name).ToUpper());
		if (byId.name.Length > 45)
		{
			trackName.value += "...";
		}
		m_trackIconName = byId.largeIcon;
		isReverse.value = byId.configuration == "Reversed";
		if (!minimapNode)
		{
			return;
		}
		LoadRaceState loadRaceState = States.State<LoadRaceState>();
		minimapNode.transform.parent.gameObject.SetActive(loadRaceState.currentTrackLayout != null);
		if (loadRaceState.currentTrackLayout != null)
		{
			Singleton<TrackLayoutController>.instance.SetDataRAW(loadRaceState.currentTrackLayout, destroyActive: true);
			Singleton<TrackLayoutController>.instance.BindPreview(minimapNode.transform);
			if (loadRaceState.currentRaceMode == RaceMode.XDS)
			{
				string config = m_prefs.globals.lastMetaConfig.EncodeName(loadRaceState.currentRaceMode, byId.scene);
				yourResult.value = DriftControllerXDS.ConvertScoresToResultString(m_prefs.stats.GetXDSBestResult(config));
			}
		}
	}

	public void HideAndDestroy()
	{
		Hide();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void FillDynoInfo()
	{
		trackName.value = LocalizationKeys.car_profile_dyno_state_caption.Localized();
		m_trackIconName = "dynostate";
		if ((bool)minimapNode)
		{
			minimapNode.transform.parent.gameObject.SetActive(value: false);
		}
	}

	private void FillCarInfo(int carId, int profileId, int engineId, Property<string> outNameProperty, Property<string> outProfileProperty)
	{
		PlayerCar byId = m_model.GetById<PlayerCar>(carId);
		if (byId != null)
		{
			outNameProperty.value = byId.name.ToUpper();
		}
		else
		{
			Debug.LogError("UILoadingScreenContext::FillCarInfo(): carId not found.");
		}
		PlayerCarProfile byId2 = m_model.GetById<PlayerCarProfile>(profileId);
		if (byId2 != null)
		{
			outProfileProperty.value = byId2.name.ToUpper();
			m_setupIcon.value = m_setupsAtlas.GetSprite(byId2.GetProfileIconName());
		}
		else
		{
			m_setupIcon.value = null;
			Debug.LogError("UILoadingScreenContext::FillCarInfo(): profile not found.");
		}
	}

	private string FormatAward(IRaceAward trackAward, RaceCupType cupType)
	{
		int num = BaseRaceModeState.CalculateAward(trackAward.GetAwardByCup(cupType));
		if (num == 0)
		{
			return "---";
		}
		return UIUtils.DigitsWithSeparate(num);
	}

	private void FillCupsInfo(int carId, int trackId, RaceMode mode)
	{
		pointsTitle.value = ((mode == RaceMode.Drift) ? LocalizationKeys.drift_points : LocalizationKeys.Time).Localized();
		IRaceNorm raceNorm = null;
		IRaceAward trackAward = null;
		switch (mode)
		{
		case RaceMode.Drift:
			raceNorm = m_model.Get((TrackDriftRaceNormMeta x) => x.trackId == trackId);
			trackAward = m_model.Get((TrackDriftRaceRewardMeta x) => x.trackId == trackId);
			break;
		case RaceMode.TimeAttack:
			raceNorm = m_model.Get((TrackTimeAttackRaceNormMeta x) => x.trackId == trackId && x.carId == carId);
			trackAward = m_model.Get((TrackTimeAttackRaceRewardMeta x) => x.trackId == trackId);
			break;
		}
		if (raceNorm == null)
		{
			raceNorm = new EmptyRaceNormMeta();
			trackAward = new TrackRewardMeta();
		}
		cup1Data.value = raceNorm.GetNormAsString(RaceCupType.Gold);
		cup1Cost.value = FormatAward(trackAward, RaceCupType.Gold);
		cup2Data.value = raceNorm.GetNormAsString(RaceCupType.Silver);
		cup2Cost.value = FormatAward(trackAward, RaceCupType.Silver);
		cup3Data.value = raceNorm.GetNormAsString(RaceCupType.Bronze);
		cup3Cost.value = FormatAward(trackAward, RaceCupType.Bronze);
	}

	private string GetHintName()
	{
		m_hintIndex = m_prefs.globals.lastHintId;
		m_prefs.globals.lastHintId = m_hintIndex % m_maxHintNumber + 1;
		FieldInfo field = typeof(LocalizationKeys).GetField("HintText" + m_hintIndex, BindingFlags.Static | BindingFlags.Public);
		return LocalizationService.Localize((field != null) ? ((LocalizationKeys.Key)field.GetValue(null)).KeyValue : "");
	}

	private IEnumerator SelfDestroy()
	{
		yield return null;
		yield return null;
		UnloadResources();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void SetLoadedAndShowContinuePlay(Func<bool> waitAction)
	{
		m_waitAndContinuePlayCoroutine = StartCoroutine(WaitAndContinuePlay(waitAction));
	}

	private IEnumerator WaitAndContinuePlay(Func<bool> waitAction)
	{
		if (waitAction != null)
		{
			while (!waitAction())
			{
				yield return null;
			}
		}
		if (autoContinue)
		{
			yield return null;
			ContinueClickEvent.SafeCall();
		}
		else
		{
			loaded.value = true;
		}
		m_waitAndContinuePlayCoroutine = null;
	}

	public void LoadResources()
	{
		UnloadResources();
		if (m_isExternalTrack)
		{
			byte[] data = File.ReadAllBytes(m_trackIconName);
			Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false, linear: false);
			texture2D.LoadImage(data);
			trackIcon.value = texture2D;
		}
		else
		{
			trackIcon.value = ResourcesWrapper.Load<Texture>("ui/tracks/trackpreviewfullhd/" + m_trackIconName);
		}
	}

	public void UnloadResources()
	{
		if (!(trackIcon.value == null))
		{
			if (m_isExternalTrack)
			{
				UnityEngine.Object.Destroy(trackIcon.value);
			}
			else
			{
				ResourcesWrapper.UnloadAsset(trackIcon.value);
			}
			trackIcon.value = null;
		}
	}
}
