using CarX;
using DB.Meta;
using DI;
using LocalCaching.Manager;
using PrefsModels;

namespace LocalCaching.Model
{
	public class CarProfileProxy
	{
		public enum InternationalUnits
		{
			KmH = 0,
			MpH = 1,
			Both = 2
		}

		private readonly PlayerCarProfile m_carProfile;

		private CarProfileCache m_cachedProfileInfo;

		public int carId => m_carProfile.carId;

		public int id => m_carProfile.id;

		public string name => m_carProfile.name;

		public PlayerCarProfile.DynoInfo dyno => m_carProfile.dynoInfo;

		public int cost => m_carProfile.cost;

		public int sort => m_carProfile.sort;

		public float topSpeedMpH => m_cachedProfileInfo.topSpeedMpH;

		public float topSpeedKmH => m_cachedProfileInfo.topSpeedKmH;

		public float accelMpH => m_cachedProfileInfo.accelMpH;

		public float accelKmH => m_cachedProfileInfo.accelKmH;

		public int maxPower => m_cachedProfileInfo.maxPower;

		public int maxPowerRPM => m_cachedProfileInfo.maxPowerRPM;

		public int maxTorque => m_cachedProfileInfo.maxTorque;

		public int maxTorqueRPM => m_cachedProfileInfo.maxTorqueRPM;

		public int weight => m_cachedProfileInfo.weight;

		public float powerDensity => m_cachedProfileInfo.powerDensity;

		public string transmission => m_cachedProfileInfo.transmission;

		public bool traction_control => m_cachedProfileInfo.traction_control;

		public string tyres => m_cachedProfileInfo.tyres;

		public CarProfileProxy(PlayerCarProfile carProfile, CarProfileCache cachedProfileInfo)
		{
			m_carProfile = carProfile;
			m_cachedProfileInfo = cachedProfileInfo;
		}

		public PlayerCarProfile GetProfile()
		{
			return m_carProfile;
		}

		public void RecalculateAll(Car car, InternationalUnits units)
		{
			CarProfileCacheManager.Recalculate(ref m_cachedProfileInfo, car, units);
		}

		public GameGlobalFeature AsGameFeature()
		{
			return name switch
			{
				"Stock" => GameGlobalFeature.SetupStock, 
				"Turbo" => GameGlobalFeature.SetupTurbo, 
				"Drift" => GameGlobalFeature.SetupDrift, 
				"Racing" => GameGlobalFeature.SetupRace, 
				"Ultimate" => GameGlobalFeature.SetupUltimate, 
				"UltimatePro" => GameGlobalFeature.SetupUltimatePro, 
				_ => GameGlobalFeature.Unknown, 
			};
		}

		public int GetCurrentEngineId()
		{
			if (!GetProfile().CanSwap())
			{
				return 0;
			}
			PrefsCarSlotsModel carSettings = DependencyInjector.Resolve<GamePrefs>().carSettings;
			var (setupId, flag, engineId) = carSettings.GetHaveCustomSetupsPerCar(carId);
			if (flag)
			{
				carSettings.SetHaveCustomSetupsPerCar(carId, engineId, setupId, isHaveCustomSetup: false);
			}
			int profileIdForCar = carSettings.GetProfileIdForCar(carId);
			carSettings.SetProfilePerCar(carId, id);
			int engineIdForCar = carSettings.GetEngineIdForCar(carId);
			carSettings.SetProfilePerCar(carId, profileIdForCar);
			if (flag)
			{
				carSettings.SetHaveCustomSetupsPerCar(carId, engineId, setupId, isHaveCustomSetup: true);
			}
			return engineIdForCar;
		}
	}
}
