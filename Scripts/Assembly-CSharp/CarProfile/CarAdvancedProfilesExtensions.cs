using CarModelSystem;
using CarX;
using DB;
using DB.Meta;
using DI;
using Db.Meta.CarProfile;
using PrefsModels;
using UnityEngine;

namespace CarProfile
{
	internal static class CarAdvancedProfilesExtensions
	{
		private const string CAR_ID_KEY = "carId";

		private static bool TryGetStockSavedPhase(EngineSwapData savedSwap, string controllerName, out float value)
		{
			value = 0f;
			if (savedSwap.id > 0 || savedSwap.controllers == null)
			{
				return false;
			}
			foreach (EngineSwapData.SwapController controller in savedSwap.controllers)
			{
				if (controller.key == controllerName)
				{
					value = controller.value;
					return true;
				}
			}
			return false;
		}

		private static float GetChangerValue(EngineParamInfo param, float phase)
		{
			if (param == null)
			{
				return 1f;
			}
			float mult = 1f;
			float shift = 0f;
			if (param is EngineParamShiftInfo engineParamShiftInfo)
			{
				mult = engineParamShiftInfo.multiplier;
				shift = 0f - engineParamShiftInfo.shift;
			}
			return new CarProfileChanger(param.min, param.max, param.value, 0, mult, string.Empty, string.Empty, shift).EvaluateValue(phase);
		}

		public static void ApplyAdvancedProfile(this RaceCar car, BaseModel db, ProfilesInfo.Profile profile, PrefsCarSlotsModel carSettings, bool reset = false, bool skipSwapControllers = false, EngineSwapData swapOverride = null, string customSetupId = "", bool updateFromCarSpecsTable = false)
		{
			int carid = car.metaInfo.id;
			PlayerCarProfile playerCarProfile = db.QueryRecordsByColumnValue("carId", carid, (PlayerCarProfile x) => x.carId == carid).Find((PlayerCarProfile x) => x.id == profile.id);
			PlayerCarFactory playerCarFactory = DependencyInjector.Resolve<PlayerCarFactory>();
			EngineSwapData engineSwapData = null;
			if (car.isNetworkCar)
			{
				engineSwapData = CarSwapData.GetDefaultMeta(car.swap.engineId);
			}
			else
			{
				engineSwapData = carSettings.GetEngineSwapDataForCar(carid, car.swap.engineId, customSetupId);
			}
			if (playerCarProfile == null)
			{
				return;
			}
			if (reset)
			{
				car.profileMetaInfo = playerCarProfile;
				car.InitDesc(playerCarProfile.asset);
			}
			BaseCar.Desc desc = car.GetDesc();
			CarDesc.GearBox gearBox = desc.carXDesc.gearBox;
			EngineInfo meta = car.swap.meta;
			if (playerCarProfile.dynoInfo.controllers.Count > 0)
			{
				CarProfileChanger changer;
				foreach (CarProfileController controller2 in playerCarProfile.dynoInfo.controllers)
				{
					CarProfileController controller = controller2;
					bool flag = true;
					if (!profile.controllers.TryGetValue(controller.name, out var value))
					{
						if (controller.changers.Count == 1)
						{
							CarProfileChanger carProfileChanger = controller.changers[0];
							value = carProfileChanger.EvaluatePhase(carProfileChanger.defaultValue);
						}
						else
						{
							flag = false;
						}
					}
					if (carSettings != null && meta != null)
					{
						EngineSwapData engineSwapData2 = swapOverride ?? engineSwapData;
						if (playerCarProfile.CanSwap() && updateFromCarSpecsTable && engineSwapData2 != null && car.swap.isStockEngine && TryGetStockSavedPhase(engineSwapData2, controller.name, out var value2))
						{
							value = value2;
						}
					}
					if (!flag)
					{
						continue;
					}
					foreach (CarProfileChanger changer2 in controller.changers)
					{
						changer = changer2;
						CarProfileParameterMeta byId = db.GetById<CarProfileParameterMeta>(changer.parameterId);
						switch (byId.type)
						{
						case CarProfileParameterType.FrontSpringWay:
							desc.carXDesc.frontSuspension.springLength = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontSpringStiffness:
							desc.carXDesc.frontSuspension.stiffness = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontFastBump:
							desc.carXDesc.frontSuspension.fastBump = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontFastRebound:
							desc.carXDesc.frontSuspension.fastRebound = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontSlowBump:
							desc.carXDesc.frontSuspension.slowBump = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontSlowRebound:
							desc.carXDesc.frontSuspension.slowRebound = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontToe:
							desc.carXDesc.frontSuspension.toeIn = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontCamber:
							desc.carXDesc.frontSuspension.camber = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontCaster:
							desc.carXDesc.frontSuspension.caster = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontKingpin:
							desc.carXDesc.frontSuspension.kingpin = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontWheelCenterTrail:
							desc.carXDesc.frontSuspension.wheelCenterTrail = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontAckerman:
							desc.carXDesc.frontSuspension.ackerman = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontLock:
							desc.carXDesc.frontSuspension.frontLock = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontStabilizer:
							desc.carXDesc.frontSuspension.stabilizer = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.RearSpringWay:
							desc.carXDesc.rearSuspension.springLength = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.RearSpringStiffness:
							desc.carXDesc.rearSuspension.stiffness = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.RearFastBump:
							desc.carXDesc.rearSuspension.fastBump = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.RearFastRebound:
							desc.carXDesc.rearSuspension.fastRebound = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.RearSlowBump:
							desc.carXDesc.rearSuspension.slowBump = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.RearSlowRebound:
							desc.carXDesc.rearSuspension.slowRebound = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.RearToe:
							desc.carXDesc.rearSuspension.toeIn = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.RearCamber:
							desc.carXDesc.rearSuspension.camber = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.RearStabilizer:
							desc.carXDesc.rearSuspension.stabilizer = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontDiscDiam:
							desc.carXDesc.frontTyre.discDiam = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontTyreWidth:
							desc.carXDesc.frontTyre.width = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontTyrePressure:
							desc.carXDesc.frontTyre.pressure = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.RearDiscDiam:
							desc.carXDesc.rearTyre.discDiam = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.RearTyreWidth:
							desc.carXDesc.rearTyre.width = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.RearTyrePressure:
							desc.carXDesc.rearTyre.pressure = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontTireStretch:
							desc.carXDesc.frontTyre.stretch = ScriptableSingleton<TireStretchConfig>.instance.stretchCurve.Evaluate(changer.EvaluateOriginalValue(value));
							break;
						case CarProfileParameterType.RearTireStretch:
							desc.carXDesc.rearTyre.stretch = ScriptableSingleton<TireStretchConfig>.instance.stretchCurve.Evaluate(changer.EvaluateOriginalValue(value));
							break;
						case CarProfileParameterType.MaxTorque:
						{
							float maxTorque = (car.swap.isStockEngine ? changer.EvaluateOriginalValue(value) : GetChangerValue(meta.maxTorque, value));
							desc.carXDesc.engine.maxTorque = maxTorque;
							break;
						}
						case CarProfileParameterType.MaxTorqueRPM:
						{
							float maxTorqueRPM = (car.swap.isStockEngine ? changer.EvaluateOriginalValue(value) : GetChangerValue(meta.maxTorqueRpm, value));
							desc.carXDesc.engine.maxTorqueRPM = maxTorqueRPM;
							break;
						}
						case CarProfileParameterType.RevLimiter:
						{
							float revLimiter = (car.swap.isStockEngine ? changer.EvaluateOriginalValue(value) : GetChangerValue(meta.revLimiter, value));
							desc.carXDesc.engine.revLimiter = revLimiter;
							break;
						}
						case CarProfileParameterType.TurboCharged:
						{
							float num3 = (car.swap.isStockEngine ? changer.EvaluateOriginalValue(value) : GetChangerValue(meta.turboCharged, value));
							desc.carXDesc.engine.turboCharged = num3 > 0.5f;
							break;
						}
						case CarProfileParameterType.TurboPressure:
						{
							float turboPressure = (car.swap.isStockEngine ? changer.EvaluateOriginalValue(value) : GetChangerValue(meta.turboPressure, value));
							desc.carXDesc.engine.turboPressure = turboPressure;
							break;
						}
						case CarProfileParameterType.DiffType:
							desc.carXDesc.transmission.rearDiffType = (DiffType)Mathf.RoundToInt(changer.EvaluateOriginalValue(value));
							desc.carXDesc.transmission.frontDiffType = desc.carXDesc.transmission.rearDiffType;
							break;
						case CarProfileParameterType.DiffLockCoef:
							desc.carXDesc.transmission.frontDiffViscoCoef = changer.EvaluateOriginalValue(value);
							desc.carXDesc.transmission.rearDiffViscoCoef = desc.carXDesc.transmission.frontDiffViscoCoef;
							break;
						case CarProfileParameterType.DiffPreload:
							desc.carXDesc.transmission.rearDiffPreload = changer.EvaluateOriginalValue(value);
							desc.carXDesc.transmission.frontDiffPreload = desc.carXDesc.transmission.rearDiffPreload;
							break;
						case CarProfileParameterType.DiffLockCoast:
							desc.carXDesc.transmission.rearDiffLockCoast = changer.EvaluateOriginalValue(value);
							desc.carXDesc.transmission.frontDiffLockCoast = desc.carXDesc.transmission.rearDiffLockCoast;
							break;
						case CarProfileParameterType.DiffLockPower:
							desc.carXDesc.transmission.rearDiffLockPower = changer.EvaluateOriginalValue(value);
							desc.carXDesc.transmission.frontDiffLockPower = desc.carXDesc.transmission.rearDiffLockPower;
							break;
						case CarProfileParameterType.FinalDrive:
						{
							GetSwapVal(out var val2);
							desc.carXDesc.gearBox.finalDrive = val2;
							break;
						}
						case CarProfileParameterType.BrakeMaxTorque:
							desc.carXDesc.brakes.maxTorque = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.BrakeFrontPercent:
							desc.carXDesc.brakes.frontPercent = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.BrakeABS:
							desc.carXDesc.brakes.useABS = changer.EvaluateOriginalValue(value) > 0.5f;
							break;
						case CarProfileParameterType.TractionControl:
							desc.carXDesc.engine.useTC = (desc.carXDesc.engine.hasTC = changer.EvaluateOriginalValue(value) > 0.5f);
							break;
						case CarProfileParameterType.LaunchControl:
							desc.carXDesc.engine.hasLaunchControl = changer.EvaluateOriginalValue(value) > 0.5f;
							break;
						case CarProfileParameterType.SequentialTransmissionSound:
							desc.carXDesc.transmission.sequentialTransmission = changer.EvaluateOriginalValue(value) > 0.5f;
							break;
						case CarProfileParameterType.WastegateSound:
							desc.carXDesc.engine.wastegate = changer.EvaluateOriginalValue(value) > 0.5f;
							break;
						case CarProfileParameterType.FrontTireType:
						{
							float minValue2 = changer.values[0];
							float maxValue2 = changer.values[changer.values.Count - 1];
							desc.carXDesc.frontTyre.frictionMultiplier = changer.EvaluateOriginalValue(value);
							if (!car.isNetworkCar)
							{
								playerCarFactory?.visualManager?.SetFrictionParams(car, CarWheelID.FrontAxle, desc.carXDesc.frontTyre.frictionMultiplier, minValue2, maxValue2);
							}
							break;
						}
						case CarProfileParameterType.RearTireType:
						{
							float minValue = changer.values[0];
							float maxValue = changer.values[changer.values.Count - 1];
							desc.carXDesc.rearTyre.frictionMultiplier = changer.EvaluateOriginalValue(value);
							if (!car.isNetworkCar)
							{
								playerCarFactory?.visualManager?.SetFrictionParams(car, CarWheelID.RearAxle, desc.carXDesc.rearTyre.frictionMultiplier, minValue, maxValue);
							}
							break;
						}
						case CarProfileParameterType.FrontBaseOffset:
							car.frontBaseOffset = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.RearBaseOffset:
							car.rearBaseOffset = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.FrontTyreProfile:
							desc.carXDesc.frontTyre.profile = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.RearTyreProfile:
							desc.carXDesc.rearTyre.profile = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.DrivetrainType:
						{
							GearType drivetrainType = DrivetrainSwitchDynoParamViewModel.GetDrivetrainType(Mathf.RoundToInt(changer.EvaluateOriginalValue(value)));
							desc.carXDesc.transmission.gearType = drivetrainType;
							CarDesc.Aero aero = desc.carXDesc.aero;
							if (drivetrainType == GearType.GEAR_RWD)
							{
								aero.frontDownforce = 70f;
								aero.rearDownforce = 100f;
							}
							else
							{
								aero.frontDownforce = 100f;
								aero.rearDownforce = 100f;
							}
							break;
						}
						case CarProfileParameterType.Mass:
						{
							float num2 = changer.EvaluateOriginalValue(value);
							if (!car.swap.isStockEngine)
							{
								num2 = num2 - car.metaInfo.stockEngineWeight + car.swap.meta.weight;
							}
							desc.carXDesc.weight.mass = num2;
							break;
						}
						case CarProfileParameterType.WeightFrontPercent:
							desc.carXDesc.weight.frontPercent = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.WeightUpPercent:
							desc.carXDesc.weight.upPercent = changer.EvaluateOriginalValue(value);
							break;
						case CarProfileParameterType.Gear1:
						case CarProfileParameterType.Gear2:
						case CarProfileParameterType.Gear3:
						case CarProfileParameterType.Gear4:
						case CarProfileParameterType.Gear5:
						case CarProfileParameterType.Gear6:
						case CarProfileParameterType.Gear7:
						case CarProfileParameterType.Gear8:
						case CarProfileParameterType.Gear9:
						case CarProfileParameterType.Gear10:
						case CarProfileParameterType.Gear11:
						case CarProfileParameterType.Gear12:
						{
							int num = (int)(byId.type - 43);
							if (gearBox != null && gearBox.gears.Length > num)
							{
								GetSwapVal(out var val);
								gearBox.gears[num] = val;
							}
							break;
						}
						}
					}
					void GetSwapVal(out float reference)
					{
						reference = changer.EvaluateOriginalValue(value);
						if (!car.swap.isStockEngine && !skipSwapControllers)
						{
							EngineSwapData.SwapController swapController = engineSwapData.controllers.Find((EngineSwapData.SwapController c) => c.key == controller.name);
							if (swapController != null)
							{
								reference = changer.EvaluateOriginalValue(swapController.value);
							}
						}
					}
				}
			}
			else
			{
				car.frontBaseOffset = 0f;
				car.rearBaseOffset = 0f;
				if (!car.isNetworkCar)
				{
					playerCarFactory?.visualManager?.SetTyreGripType(car, CarWheelID.AllWheels, CarVisualManager.TyreProtectorGripType.LowGrip);
				}
			}
			if (desc.frontWheelDiam.HasValue)
			{
				CarDesc.Tyre frontTyre = desc.carXDesc.frontTyre;
				float num4 = 44f - 2f * frontTyre.discDiam;
				if (frontTyre.profile < num4)
				{
					frontTyre.profile = num4;
				}
			}
			if (desc.rearWheelDiam.HasValue)
			{
				CarDesc.Tyre rearTyre = desc.carXDesc.rearTyre;
				float num5 = 44f - 2f * rearTyre.discDiam;
				if (rearTyre.profile < num5)
				{
					rearTyre.profile = num5;
				}
			}
			car.swap.SetupEngineParams(desc, playerCarProfile, customSetupId, car.isNetworkCar, skipSwapControllers, swapOverride ?? engineSwapData);
			car.SetDesc(desc);
			if (!car.isNetworkCar)
			{
				playerCarFactory?.visualManager?.ApplyAllTires(car);
			}
		}
	}
}
