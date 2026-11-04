using CarProfile;
using DI;
using TrackLayout.Data;

public class DynoRaceModeState : FreerideRaceModeState
{
	private CarVisualManager m_carVisualManager;

	private DynoDto m_dynoCache;

	private bool m_tiresLoaded;

	public override RaceMode raceMode => RaceMode.Dyno;

	public DynoDto GetDynoData()
	{
		return m_dynoCache;
	}

	public void SetupDynoData(DynoDto dynoDto)
	{
		m_dynoCache = dynoDto;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if (m_carVisualManager == null)
		{
			m_carVisualManager = DependencyInjector.Resolve<CarVisualManager>();
		}
		ApplyPreviewProfile();
		base.checkpointManager.ClearTimers();
		m_tiresLoaded = false;
		base.userCar.OnCarVisualLoadFinishEvent += delegate
		{
			if (!m_tiresLoaded)
			{
				SetupTires();
			}
		};
	}

	private void SetupTires()
	{
		bool flag = TrackMetaDB.IsWinterTrackWithIce(m_prefs.globals.lastTrackId);
		if (flag)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.tires_changed_to_winter_type.Localized());
		}
		CarVisualManager.VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(base.userCar.metaInfo.id);
		visualForCar.winterTires = flag;
		m_carVisualManager?.ApplyVisual(CarVisualManager.CustomizationFlags.Wheels, base.userCar, visualForCar);
		m_tiresLoaded = true;
	}

	private void ApplyPreviewProfile()
	{
		if (!m_dynoCache.Equals(null))
		{
			if (base.userCar.IsVisualLoaded)
			{
				OnVisualsLoaded(base.userCar);
				return;
			}
			base.userCar.OnCarVisualLoadFinishEvent -= OnVisualsLoaded;
			base.userCar.OnCarVisualLoadFinishEvent += OnVisualsLoaded;
		}
		void OnVisualsLoaded(RaceCar car)
		{
			base.userCar.OnCarVisualLoadFinishEvent -= OnVisualsLoaded;
			base.userCar.ApplyAdvancedProfile(m_model, m_dynoCache.preview, m_prefs.carSettings, reset: false, skipSwapControllers: true);
		}
	}
}
