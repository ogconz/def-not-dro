using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Binding;
using Binding.Base;
using CarModelSystem;
using CarModelSystem.Data;
using CarX;
using Customization;
using DB;
using DB.Meta;
using DI;
using GameInput;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Utils.ColorExtensions;
using Utlis;

public class UIPaintCustomizationContext : BaseBindingBehaviourTarget
{
	public enum StateType
	{
		Category = -1,
		Body = 0,
		Interior = 1,
		Suspension = 2,
		Glass = 3,
		Smoke = 4,
		Spokes = 5,
		Rims = 6
	}

	public enum CategoryNavMode
	{
		Parts = 0,
		Colors = 1,
		FavoriteColors = 2
	}

	public enum PartType
	{
		Chassis = 0,
		Ramp = 1,
		RollCage = 2,
		FrontBumper = 3,
		RearBumper = 4,
		Skirts = 5,
		Doors = 6,
		Roof = 7,
		SideMirrors = 8,
		Hood = 9,
		Trunk = 10,
		Spoiler = 11,
		Fenders = 12,
		FrontLights = 13,
		RearLights = 14,
		SteeringWheelSpokes = 15,
		SteeringWheelCover = 16,
		Shifter = 17,
		Handbrake = 18,
		RightSeatBase = 19,
		RightSeatCushion = 20,
		LeftSeatBase = 21,
		LeftSeatCushion = 22,
		FrontRims = 23,
		FrontSpokes = 24,
		RearRims = 25,
		RearSpokes = 26,
		TireSticker1 = 27,
		TireSticker2 = 28,
		Arm = 29,
		Spring = 30,
		Strut = 31,
		Calliper = 32,
		FrontLightsGlass = 33,
		RearLightsGlass = 34,
		Windows = 35,
		Smoke = 36
	}

	[Serializable]
	private struct CarPartsToSkip
	{
		public string skipCarName;

		public string[] skipPrefabNames;
	}

	private class HotkeyDelay
	{
		private bool m_firstPress;

		private float m_delayTime;

		public ActionType actionType { get; }

		public HotkeyDelay(ActionType action)
		{
			actionType = action;
			Reset();
		}

		public bool Delay()
		{
			float unscaledTime = Time.unscaledTime;
			if (m_firstPress)
			{
				m_delayTime = unscaledTime + 0.3f;
				m_firstPress = false;
				return false;
			}
			if (m_delayTime > unscaledTime)
			{
				return true;
			}
			m_delayTime = unscaledTime + 0.08f;
			return false;
		}

		public void Reset()
		{
			m_firstPress = true;
		}
	}

	[Serializable]
	public class PartIconPair
	{
		public PartType type;

		public Sprite sprite;
	}

	public abstract class BasePaintState
	{
		public Sprite spriteIcon { get; }

		public UIPaintCustomizationContext context { get; }

		public UIPaintCategory category { get; set; }

		public StateType type { get; }

		public List<UIBodyPartPaintItem> parts { get; } = new List<UIBodyPartPaintItem>();

		protected BasePaintState(UIPaintCustomizationContext context, Sprite spriteIcon, StateType type)
		{
			this.spriteIcon = spriteIcon;
			this.context = context;
			this.type = type;
		}

		protected void InstantiatePart(PartType partType, UIBodyPartPaintItem.PaintMaterial materials, string partDesc)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(context.customizationCategoryPrefab, context.partsContainer, worldPositionStays: false);
			Sprite partIcon = context.GetPartIcon(partType);
			UIBodyPartPaintItem orAddComponent = gameObject.GetOrAddComponent<UIBodyPartPaintItem>();
			orAddComponent.SetData(this, parts.Count, partIcon, partType, materials, partDesc);
			gameObject.SetActive(value: false);
			parts.Add(orAddComponent);
		}

		public void SelectPart(int index)
		{
			if (parts.Count > 0)
			{
				for (int i = 0; i < parts.Count; i++)
				{
					parts[i].selected.value = i == index;
				}
				if (context.MultiselectMode)
				{
					context.UpdateBottomPanel();
				}
			}
		}

		public void TogglePart(int index)
		{
			if (index < 0 || index >= parts.Count)
			{
				return;
			}
			UIBodyPartPaintItem uIBodyPartPaintItem = parts[index];
			uIBodyPartPaintItem.addedToPaint.value = !uIBodyPartPaintItem.addedToPaint.value;
			if (uIBodyPartPaintItem.partType == PartType.Chassis)
			{
				foreach (UIBodyPartPaintItem part in parts)
				{
					part.addedToPaint.value = uIBodyPartPaintItem.addedToPaint.value;
				}
			}
			else if (!uIBodyPartPaintItem.addedToPaint.value)
			{
				foreach (UIBodyPartPaintItem part2 in parts)
				{
					if (part2.partType == PartType.Chassis)
					{
						part2.addedToPaint.value = false;
						break;
					}
				}
			}
			if (context.MultiselectMode)
			{
				context.UpdateBottomPanel();
			}
		}

		public void ResetMultiselect()
		{
			foreach (UIBodyPartPaintItem part in parts)
			{
				part.addedToPaint.value = false;
			}
		}

		public UIBodyPartPaintItem GetSelectedPart(int index)
		{
			if (index >= 0 && index < parts.Count)
			{
				return parts[index];
			}
			return null;
		}

		public List<UIBodyPartPaintItem> GetAddedToPaintParts()
		{
			bool flag = false;
			foreach (UIBodyPartPaintItem part in parts)
			{
				if (part.partType == PartType.Chassis)
				{
					flag = part.selected.value;
					break;
				}
			}
			List<UIBodyPartPaintItem> list = new List<UIBodyPartPaintItem>();
			if (context.MultiselectMode)
			{
				foreach (UIBodyPartPaintItem part2 in parts)
				{
					if (part2.addedToPaint.value)
					{
						list.Add(part2);
					}
				}
			}
			else
			{
				foreach (UIBodyPartPaintItem part3 in parts)
				{
					if (flag || part3.selected.value)
					{
						list.Add(part3);
						if (!flag)
						{
							break;
						}
					}
				}
			}
			return list;
		}

		public virtual void Activate()
		{
		}

		public virtual void Deactivate()
		{
		}

		public virtual void Update()
		{
		}

		public virtual void FixedUpdate()
		{
		}
	}

	public class BodyPaintState : BasePaintState
	{
		private const int PARTS_ID_SHIFT = 20;

		private int m_currentHoodId;

		private int m_emptyHoodId;

		private bool m_prevHoodVisible;

		private readonly HashSet<int> m_prevParts = new HashSet<int>();

		public BodyPaintState(UIPaintCustomizationContext context, Sprite spriteIcon)
			: base(context, spriteIcon, StateType.Body)
		{
			InstantiatePart(PartType.Chassis, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.part_all_body.Localized());
			InstantiatePart(PartType.FrontBumper, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.car_castomize_category_caption.FormatLocalized("bumperfront"));
			InstantiatePart(PartType.RearBumper, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.car_castomize_category_caption.FormatLocalized("bumperrear"));
			InstantiatePart(PartType.Skirts, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.car_castomize_category_caption.FormatLocalized("skirts"));
			InstantiatePart(PartType.Doors, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.car_castomize_category_caption.FormatLocalized("doors"));
			InstantiatePart(PartType.Roof, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.car_castomize_category_caption.FormatLocalized("roof"));
			InstantiatePart(PartType.Hood, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.car_castomize_category_caption.FormatLocalized("bonnet"));
			InstantiatePart(PartType.Trunk, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.car_castomize_category_caption.FormatLocalized("trunk"));
			InstantiatePart(PartType.SideMirrors, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.car_castomize_category_caption.FormatLocalized("mirrors"));
			InstantiatePart(PartType.Spoiler, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.car_castomize_category_caption.FormatLocalized("spoiler"));
			InstantiatePart(PartType.Fenders, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.car_castomize_category_caption.FormatLocalized("chassis"));
			InstantiatePart(PartType.FrontLights, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.car_castomize_category_caption.FormatLocalized("lightsfront"));
			InstantiatePart(PartType.RearLights, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.car_castomize_category_caption.FormatLocalized("lightsrear"));
			InstantiatePart(PartType.Ramp, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.part_ramp.Localized());
			InstantiatePart(PartType.RollCage, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.car_castomize_category_caption.FormatLocalized("cage"));
		}

		public override void Activate()
		{
			base.Activate();
			if (base.context.m_playerCar.emptyBonnetId == -1)
			{
				m_emptyHoodId = -1;
				return;
			}
			m_prevParts.Clear();
			foreach (int part in base.context.m_previewSettings.parts)
			{
				m_prevParts.Add(part);
			}
			int emptyBonnetId = base.context.m_playerCar.emptyBonnetId;
			int num = 20 + base.context.m_playerCar.emptyBonnetId;
			CarPartDatabase.SlotScheme slotScheme = base.context.m_slotScheme;
			List<CarPart> list = CarPartDatabase.GetParts(slotScheme, "bonnet", "", forceAdd: true);
			bool flag = false;
			bool flag2 = false;
			foreach (CarPart item in list)
			{
				if (base.context.m_carId == item.carId)
				{
					if (!flag && (item.nativeid == emptyBonnetId || item.nativeid == num))
					{
						m_emptyHoodId = item.id;
						flag = true;
					}
					if (!flag2 && base.context.m_previewSettings.parts.Contains(item.id))
					{
						m_currentHoodId = item.id;
						flag2 = true;
					}
					if (flag && flag2)
					{
						break;
					}
				}
			}
			CarPart carPart = CarPartDatabase.GetParts(slotScheme, "chassis", "", forceAdd: true).Find((CarPart part) => base.context.m_previewSettings.parts.Contains(part.id));
			foreach (CarPart item2 in list)
			{
				if (carPart == null || carPart.overrideEmptyBonnetId == -1)
				{
					break;
				}
				if (item2 != null && base.context.m_carId == item2.carId && item2.nativeid == carPart.overrideEmptyBonnetId)
				{
					m_emptyHoodId = item2.id;
					break;
				}
			}
			m_prevHoodVisible = m_currentHoodId != m_emptyHoodId;
		}

		public override void Deactivate()
		{
			SetHoodVisible(hoodVisible: true);
			base.Deactivate();
		}

		public bool IsEmptyHood()
		{
			return m_emptyHoodId == m_currentHoodId;
		}

		public void SetHoodVisible(bool hoodVisible)
		{
			if (m_currentHoodId == -1 || IsEmptyHood())
			{
				return;
			}
			if (m_prevHoodVisible != hoodVisible)
			{
				base.context.m_previewSettings.parts.Clear();
				foreach (int prevPart in m_prevParts)
				{
					if (prevPart == m_currentHoodId)
					{
						base.context.m_previewSettings.parts.Add(hoodVisible ? m_currentHoodId : m_emptyHoodId);
					}
					else
					{
						base.context.m_previewSettings.parts.Add(prevPart);
					}
				}
				SetEmptyLightsFrontIfNeed();
				DelayedApplyPreviewSettings();
			}
			m_prevHoodVisible = hoodVisible;
			void SetEmptyLightsFrontIfNeed()
			{
				if (!hoodVisible)
				{
					RaceCar raceCar = base.context?.m_car3dView?.currentCar;
					if (!(raceCar == null) && base.context.m_skipPartsToInstallWithEmptyBonnet.TryGetValue(raceCar.carId, out var value))
					{
						CarPart partById = CarPartDatabase.GetPartById(value);
						if (partById != null)
						{
							base.context.m_previewSettings.parts.Add(partById.id);
						}
					}
				}
			}
		}

		private void DelayedApplyPreviewSettings()
		{
			RaceCar car = base.context?.m_car3dView?.currentCar;
			if (car == null)
			{
				GameLog.LogError("Unable to set delayed ApplyPreviewSettings: current car is NULL");
			}
			else if (car.IsLoadingVisualInProgress)
			{
				Action<RaceCar> delayedApplyVisuals = null;
				delayedApplyVisuals = delegate
				{
					if (base.context == null || car == null)
					{
						GameLog.LogError("Unable to execute delayed ApplyPreviewSettings: context or car is NULL");
					}
					else
					{
						base.context.ApplyPreviewSettings(CarVisualManager.CustomizationFlags.BodyParts);
						car.OnCarVisualLoadFinishEvent -= delayedApplyVisuals;
					}
				};
				car.OnCarVisualLoadFinishEvent += delayedApplyVisuals;
			}
			else
			{
				base.context.ApplyPreviewSettings(CarVisualManager.CustomizationFlags.BodyParts);
			}
		}
	}

	public class InteriorPaintState : BasePaintState
	{
		public InteriorPaintState(UIPaintCustomizationContext context, Sprite spriteIcon)
			: base(context, spriteIcon, StateType.Interior)
		{
			InstantiatePart(PartType.SteeringWheelSpokes, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.part_steering_wheel_spokes.Localized());
			InstantiatePart(PartType.SteeringWheelCover, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.part_steering_wheel_cover.Localized());
			InstantiatePart(PartType.Shifter, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.car_castomize_category_caption.FormatLocalized("shifter"));
			InstantiatePart(PartType.Handbrake, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.car_castomize_category_caption.FormatLocalized("handbrake"));
			InstantiatePart(PartType.RightSeatBase, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.part_right_seat_base.Localized());
			InstantiatePart(PartType.RightSeatCushion, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.part_right_seat_cushion.Localized());
			InstantiatePart(PartType.LeftSeatBase, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.part_left_seat_base.Localized());
			InstantiatePart(PartType.LeftSeatCushion, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.part_left_seat_cushion.Localized());
		}
	}

	public class SuspensionPaintState : BasePaintState
	{
		public SuspensionPaintState(UIPaintCustomizationContext context, Sprite spriteIcon)
			: base(context, spriteIcon, StateType.Suspension)
		{
			InstantiatePart(PartType.FrontRims, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.part_front_rim.Localized());
			InstantiatePart(PartType.FrontSpokes, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.part_front_spokes.Localized());
			InstantiatePart(PartType.RearRims, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.part_rear_rim.Localized());
			InstantiatePart(PartType.RearSpokes, UIBodyPartPaintItem.PaintMaterial.AllBody, LocalizationKeys.part_rear_spokes.Localized());
			InstantiatePart(PartType.Arm, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.part_arm.Localized());
			InstantiatePart(PartType.Spring, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.part_spring.Localized());
			InstantiatePart(PartType.Strut, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.part_strut.Localized());
			InstantiatePart(PartType.Calliper, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.part_calliper.Localized());
			InstantiatePart(PartType.TireSticker1, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.part_tiresticker1.Localized());
			InstantiatePart(PartType.TireSticker2, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.part_tiresticker2.Localized());
		}

		public override void Deactivate()
		{
			base.context.m_targetSteer = 0f;
			base.Deactivate();
		}
	}

	public class GlassBasePaintState : BasePaintState
	{
		public GlassBasePaintState(UIPaintCustomizationContext context, Sprite spriteIcon)
			: base(context, spriteIcon, StateType.Glass)
		{
			InstantiatePart(PartType.FrontLightsGlass, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.car_castomize_category_caption.FormatLocalized("lightsfront"));
			InstantiatePart(PartType.RearLightsGlass, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.car_castomize_category_caption.FormatLocalized("lightsrear"));
			InstantiatePart(PartType.Windows, UIBodyPartPaintItem.PaintMaterial.Gloss, LocalizationKeys.part_glass.Localized());
		}
	}

	public class SmokePaintState : BasePaintState
	{
		private const float RPM_CHANGE_SPEED = 0.064f;

		private const byte ADDITIONAL_RPM_PERCENT = 105;

		private const float RPM_FIRST_STEP = 0.2f;

		private const float MaxTorque = 600f;

		private const float MaxTorqueGain = 10f;

		private const float ExtraMaxTorque = 6000f;

		private float m_targetCarRPM;

		private float m_prmLerpStep;

		private float m_minRPM;

		private float m_brakeFrontBalance;

		private RaceCar m_raceCar;

		private GraphicsQuality m_graphicsQuality;

		private bool m_smokeDisabled;

		private bool m_inState;

		private Coroutine m_switchOffSmokeCoroutine;

		private FMODCarTurbo m_turboComponent;

		private List<int> m_insufficientTorqueCarIds = new List<int> { 73 };

		public SmokePaintState(UIPaintCustomizationContext context, Sprite spriteIcon)
			: base(context, spriteIcon, StateType.Smoke)
		{
			InstantiatePart(PartType.Smoke, UIBodyPartPaintItem.PaintMaterial.Gloss, null);
		}

		public override void Activate()
		{
			base.Activate();
			if (base.context.m_car3dView.IsCarViewCreated())
			{
				ActivateOnCarViewLoaded();
			}
			else
			{
				base.context.m_car3dView.CarViewLoadedEvent += ActivateOnCarViewLoaded;
			}
			m_graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
			m_inState = true;
			if (m_switchOffSmokeCoroutine != null)
			{
				TimeController.StopNow(m_switchOffSmokeCoroutine);
				m_graphicsQuality.smokeEnabled = false;
			}
			m_smokeDisabled = !m_graphicsQuality.smokeEnabled;
			if (m_smokeDisabled)
			{
				m_graphicsQuality.smokeEnabled = true;
			}
		}

		public override void Deactivate()
		{
			base.context.m_car3dView.CarViewLoadedEvent -= ActivateOnCarViewLoaded;
			if (m_smokeDisabled && m_graphicsQuality.smokeEnabled)
			{
				m_switchOffSmokeCoroutine = TimeController.StartNow(SwitchOffSmokeCoroutine());
			}
			if (m_raceCar != null)
			{
				m_raceCar.engineController.SetIgnition(enabled: false);
				m_raceCar.carX.rpm = 0f;
				m_prmLerpStep = 0f;
				m_minRPM = 0f;
				m_raceCar.carX.autoClutch = true;
				m_raceCar.carX.handbrake = 1f;
				m_raceCar.carX.accelerate = 0f;
				m_raceCar.carX.gear = 0;
				m_raceCar.carX.brake = 0f;
				m_raceCar.carX.brakeFrontBalance = m_brakeFrontBalance;
				m_raceCar.carX.starter = 0f;
				if (m_turboComponent != null)
				{
					m_turboComponent.SetTurboSoundsEnable(enable: false);
				}
				m_raceCar = null;
			}
			m_inState = false;
			base.Deactivate();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (!(m_raceCar != null))
			{
				return;
			}
			Car carX = m_raceCar.carX;
			if (carX.isEngineWorks)
			{
				m_prmLerpStep = Mathf.Clamp01(m_prmLerpStep + 0.064f * Time.fixedUnscaledDeltaTime);
				carX.rpm = Mathf.Lerp(m_minRPM, m_targetCarRPM, m_prmLerpStep);
				if (m_raceCar.carX.gear != 1)
				{
					m_raceCar.carX.gear = 1;
				}
				if (m_raceCar.carX.clutch != 0f)
				{
					m_raceCar.carX.clutch = 0f;
				}
			}
			else
			{
				m_raceCar.engineController.SetIgnition(enabled: true);
				m_raceCar.engineController.ForceStartEngineGarage();
			}
		}

		private void ActivateOnCarViewLoaded()
		{
			StartCameraRotationBySmoke();
			m_raceCar = base.context.m_car3dView.GetRaceCar();
			m_turboComponent = m_raceCar.GetComponentInChildren<FMODCarTurbo>();
			m_raceCar.engineController.SetIgnition(enabled: true);
			m_raceCar.engineController.ForceStartEngineGarage();
			m_raceCar.carX.autoClutch = false;
			m_raceCar.carX.brake = 1f;
			m_brakeFrontBalance = m_raceCar.carX.brakeFrontBalance;
			m_raceCar.carX.brakeFrontBalance = 100f;
			m_raceCar.carX.clutch = 1f;
			m_raceCar.carX.viscoDiffCoef = 30f;
			m_raceCar.carX.clutchMaxTorque = 10000f;
			m_raceCar.carX.gear = 1;
			m_raceCar.carX.accelerate = 1f;
			m_targetCarRPM = m_raceCar.carX.engineRPMAtMaxTorque * 1.05f;
			m_prmLerpStep = 0.2f;
			m_minRPM = m_targetCarRPM * m_prmLerpStep;
			if (m_turboComponent != null)
			{
				m_turboComponent.SetTurboSoundsEnable(enable: true);
			}
			CarDesc desc = new CarDesc();
			m_raceCar.carX.GetCarDesc(ref desc);
			desc.engine.maxTorque = (m_insufficientTorqueCarIds.Contains(m_raceCar.carId) ? 6000f : 600f);
			desc.gearBox.finalDrive = 4f;
			desc.transmission.rearDiffType = DiffType.Welded;
			if (desc.gearBox.gears.Length >= 1)
			{
				desc.gearBox.gears[0] = 5f;
			}
			desc.rearTyre.frictionMultiplier = 0.5f;
			m_raceCar.carX.SetCarDesc(desc, apply: true);
		}

		private IEnumerator SwitchOffSmokeCoroutine()
		{
			yield return new WaitForSecondsRealtime(2f);
			if (!m_inState)
			{
				m_graphicsQuality.smokeEnabled = false;
			}
		}

		private void StartCameraRotationBySmoke()
		{
			if (base.context.rotateCameraOnSmokeSelection)
			{
				base.context.m_cameraRotationContext.cameraRotation.RotateToPositionResetCenterAndDistance(new CameraRotation.SphCoord
				{
					direction = base.context.directionToRotateToOnSmokeSelection,
					elevation = base.context.elevationToRotateToOnSmokeSelection
				}, base.context.rotationDurationOnSmokeSelection);
			}
		}
	}

	private const string HEX_PATTERN = "^[#]?([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$";

	private const float STEER_SMOOTH_TIME = 0.05f;

	private const int MAX_CATEGORY_NAV_ITERATIONS = 5;

	private const float PRESS_DELAY_PERIOD = 0.1f;

	[Dependency]
	private BaseModel m_db;

	[ContextDependency]
	private GarageCar3dViewContext m_car3dView;

	[Dependency]
	private CarVisualManager m_carVisualManager;

	[SerializeField]
	private Sprite[] m_categorySprites;

	[SerializeField]
	private GameObject m_prefabCategory;

	[SerializeField]
	private RectTransform m_categoriesContainer;

	[SerializeField]
	private GameObject m_customizationCategoryPrefab;

	[SerializeField]
	private RectTransform m_partsContainer;

	[SerializeField]
	private PartIconPair[] m_partIcons;

	[SerializeField]
	protected ScrollRect m_scrollRect;

	[SerializeField]
	protected List<UIColorChangerContext> m_colorChangers;

	[SerializeField]
	private List<CarPartsToSkip> m_carPartsToSkip;

	public bool rotateCameraOnSmokeSelection = true;

	public float directionToRotateToOnSmokeSelection = 30f;

	public float elevationToRotateToOnSmokeSelection = 5f;

	public float rotationDurationOnSmokeSelection = 1.5f;

	private float m_time;

	private int m_cost;

	public readonly Property<float> colorH = new FloatProperty(0f);

	public readonly Property<float> colorS = new FloatProperty(0f);

	public readonly Property<float> colorB = new FloatProperty(0f);

	public readonly Property<float> customSecColorH = new FloatProperty(0f);

	public readonly Property<float> customSecColorS = new FloatProperty(0f);

	public readonly Property<float> customSecColorB = new FloatProperty(0f);

	public readonly Property<string> hexColor = new StringProperty();

	public readonly Property<string> hexCustomSecColor = new StringProperty();

	public readonly Property<Color> colorGradient = new ColorProperty(Color.white);

	public readonly Property<Color> customSecGradient = new ColorProperty(Color.white);

	public readonly Property<string> formattedCost = new StringProperty();

	public readonly Property<int> modeIndex = new IntProperty();

	public readonly Property<int> navCategoryIndex = new IntProperty();

	public readonly Property<int> navFavColorsIndex = new IntProperty();

	public readonly Property<int> navColorSliderIndex = new IntProperty();

	public readonly Property<bool> materialAvailable = new BoolProperty();

	public readonly Property<bool> materialAdditionalAvailable = new BoolProperty();

	public readonly Property<string> materialSelected = new StringProperty();

	public readonly Property<string> materialAdditionalSelected = new StringProperty();

	public readonly Property<bool> displayPartSelector = new BoolProperty();

	public readonly Property<bool> enabledAdditionalColorPanel = new BoolProperty();

	private BoolProperty m_paintBodyNotSupport = new BoolProperty(false);

	private BasePaintState[] m_states;

	private BasePaintState m_currentState;

	private List<UIPaintCategory> m_categories = new List<UIPaintCategory>();

	private CategoryNavMode m_navMode;

	private int m_categoryNavIteration;

	private UIColorPresetContext[] m_colorPresets = new UIColorPresetContext[0];

	private List<CarColorMeta> m_materials = new List<CarColorMeta>();

	private float m_prevColorH;

	private float m_prevCustomColorH;

	private bool m_dialogActive;

	private UICameraRotationContext m_cameraRotationContext;

	private CameraRotation m_cameraRotation;

	private int m_carId;

	private CarPartDatabase.SlotScheme m_slotScheme;

	private CarVisualManager.VisualSettings m_previewSettings;

	private CarVisualManager.VisualSettings m_currentSettings;

	private PlayerCar m_playerCar;

	private bool m_hasExternalParts;

	private bool m_multiEmptyPaint;

	private bool m_changePartColor = true;

	private bool m_isHoodDetached;

	private readonly Dictionary<int, List<StateType>> m_forbiddenPaintRules = new Dictionary<int, List<StateType>> { 
	{
		108,
		new List<StateType>
		{
			StateType.Body,
			StateType.Interior
		}
	} };

	private readonly Dictionary<int, List<PartType>> m_skipPartsRules = new Dictionary<int, List<PartType>>
	{
		{
			108,
			new List<PartType> { PartType.Windows }
		},
		{
			145,
			new List<PartType> { PartType.Ramp }
		},
		{
			146,
			new List<PartType> { PartType.Ramp }
		}
	};

	private readonly HashSet<PartType> m_skipPartsRulesForUgcCar = new HashSet<PartType>
	{
		PartType.Arm,
		PartType.Spring,
		PartType.Strut,
		PartType.Ramp
	};

	private readonly Dictionary<int, int> m_skipPartsToInstallWithEmptyBonnet = new Dictionary<int, int> { { 129, 129130 } };

	private readonly Dictionary<StateType, int[]> m_costs = new Dictionary<StateType, int[]>
	{
		{
			StateType.Body,
			new int[5] { 3000, 8000, 15000, 50000, 85000 }
		},
		{
			StateType.Suspension,
			new int[5] { 2500, 6500, 13500, 35000, 65000 }
		},
		{
			StateType.Glass,
			new int[5] { 2500, 6500, 13500, 35000, 65000 }
		},
		{
			StateType.Smoke,
			new int[5] { 2500, 6500, 13500, 35000, 65000 }
		}
	};

	private readonly Dictionary<PartType, CarVisualManager.CarPaintHolder> m_cart = new Dictionary<PartType, CarVisualManager.CarPaintHolder>();

	private RaceCar m_raceCar;

	private float m_currentHandBrake;

	private float m_currentSteer;

	private float m_targetSteer;

	private GameObject m_frontLeftWheel;

	private MissController m_missController;

	private Coroutine m_catSelectCoroutine;

	private InputManager m_inputManager;

	private readonly HotkeyDelay m_upDelay = new HotkeyDelay(ActionType.NavUp);

	private readonly HotkeyDelay m_downDelay = new HotkeyDelay(ActionType.NavDown);

	private readonly HotkeyDelay m_rightDelay = new HotkeyDelay(ActionType.NavRight);

	private readonly HotkeyDelay m_leftDelay = new HotkeyDelay(ActionType.NavLeft);

	public GameObject customizationCategoryPrefab => m_customizationCategoryPrefab;

	public PartIconPair[] partIcons => m_partIcons;

	public RectTransform partsContainer => m_partsContainer;

	public int cost
	{
		get
		{
			return m_cost;
		}
		set
		{
			m_cost = value;
			formattedCost.value = UIUtils.DigitsWithSeparate(m_cost);
		}
	}

	public bool lockPartSelection { get; set; }

	public bool MultiselectMode { get; private set; }

	public GamePrefs prefs { get; private set; }

	private void Awake()
	{
		m_db = DependencyInjector.Resolve<BaseModel>();
		prefs = DependencyInjector.Resolve<GamePrefs>();
		m_materials = m_db.QueryAll<CarColorMeta>();
		m_missController = GetComponentInChildren<MissController>();
		m_colorPresets = GetComponentsInChildren<UIColorPresetContext>(includeInactive: true);
		InitStates();
		for (int i = 0; i < m_colorPresets.Length; i++)
		{
			m_colorPresets[i].SetData(this, i);
		}
		for (int j = 0; j < m_colorChangers.Count; j++)
		{
			UIColorChangerContext changer = m_colorChangers[j];
			if (changer != null)
			{
				changer.id = j;
				changer.AddSmoothSlideTrait(() => changer.slider.value, delegate(float value)
				{
					changer.slider.value = value;
					OnColorChanged();
					OnCustomSecColorChanged();
				});
				UIColorChangerContext uIColorChangerContext = changer;
				uIColorChangerContext.clickAction = (Action<int>)Delegate.Combine(uIColorChangerContext.clickAction, (Action<int>)delegate(int id)
				{
					m_navMode = CategoryNavMode.Colors;
					navColorSliderIndex.value = id;
					ResetSelection();
					SelectColorSlider(id);
					UpdateBottomPanel();
				});
			}
		}
		colorH.OnValueChanged += OnColorChanged;
		colorS.OnValueChanged += OnColorChanged;
		colorB.OnValueChanged += OnColorChanged;
		customSecColorH.OnValueChanged += OnCustomSecColorChanged;
		customSecColorS.OnValueChanged += OnCustomSecColorChanged;
		customSecColorB.OnValueChanged += OnCustomSecColorChanged;
	}

	private void OnEnable()
	{
		modeIndex.OnValueChanged += UpdateTopPanel;
		modeIndex.OnValueChanged += UpdateBottomPanel;
		modeIndex.OnValueChanged += OnModeChanged;
	}

	private void OnDisable()
	{
		modeIndex.OnValueChanged -= UpdateTopPanel;
		modeIndex.OnValueChanged -= UpdateBottomPanel;
		modeIndex.OnValueChanged -= OnModeChanged;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		m_car3dView.Set3DModelVisible(value: true);
		m_carId = prefs.globals.lastCarId;
		m_playerCar = m_db.GetById<PlayerCar>(m_carId);
		m_raceCar = m_car3dView.GetRaceCar();
		m_currentSteer = m_raceCar.carX.GetSteer();
		m_raceCar.OnCarVisualLoadFinishEvent += OnCarVisualLoadFinishEventHandler;
		Singleton<CustomCameraPresets>.instance.CameraTo(CustomizationCameraPreset.Garage);
		if (m_inputManager == null)
		{
			m_inputManager = InputManager.instance;
		}
		m_cameraRotation = CameraRotation.instance;
		if (m_cameraRotation != null)
		{
			m_cameraRotation.useZoom = false;
			m_cameraRotation.canIdle = false;
			m_cameraRotation.Stop();
		}
		m_paintBodyNotSupport.value = m_raceCar.vinylsNotSupport;
		navCategoryIndex.value = -1;
		if (m_categories.Count == 0)
		{
			BasePaintState[] states = m_states;
			foreach (BasePaintState basePaintState in states)
			{
				m_categories.Add(InstantiateCategory(basePaintState));
			}
		}
		foreach (UIPaintCategory category in m_categories)
		{
			category.UpdateLockedState();
		}
		m_cameraRotationContext = base.owner.Get<UICameraRotationContext>();
		if (m_cameraRotationContext == null)
		{
			m_cameraRotationContext = base.owner.Add<UICameraRotationContext>();
		}
		LoadFavoriteColors();
		ActivateCategory(StateType.Category);
		UpdateTopPanel();
		UpdateBottomPanel();
		DefineExternalParts();
		DisplayOnlyAvailableParts();
		SelectCategory(StateType.Body);
		navCategoryIndex.value = 0;
		m_car3dView.SetGarageCarStatePhysAndHoldPosition(disablePhysics: false, holdPosition: true);
		ClearCart();
		ApplyVisuals(m_currentSettings);
		m_frontLeftWheel = null;
		if (m_raceCar != null)
		{
			Transform transform = m_raceCar.transform.FindChildDeph("WheelFL_spacer");
			if (transform != null && transform.gameObject != null)
			{
				m_frontLeftWheel = transform.gameObject;
			}
			m_currentHandBrake = m_raceCar.carX.handbrake;
			m_raceCar.carX.handbrake = 1f;
		}
		EnableColorSliders();
		EnableCameraIdleRotate(cameraIdleRotate: false);
		m_topPanelContext.BringForwardByDepth();
	}

	protected override void OnDeactivate()
	{
		Singleton<CustomCameraPresets>.instance.CameraTo(CustomizationCameraPreset.Garage);
		if (m_cameraRotation != null)
		{
			m_cameraRotation.useZoom = false;
			m_cameraRotation.canIdle = true;
			m_cameraRotation.SetIdle();
		}
		if (m_raceCar != null)
		{
			m_raceCar.OnCarVisualLoadFinishEvent -= OnCarVisualLoadFinishEventHandler;
		}
		m_car3dView.SetGarageCarStatePhysAndHoldPosition(disablePhysics: true, holdPosition: false);
		if (m_cameraRotationContext != null)
		{
			m_cameraRotationContext.disabled = false;
		}
		m_cameraRotationContext = null;
		HideTopPanel();
		if (m_raceCar != null)
		{
			m_raceCar.ResetSteer();
			m_raceCar.carX.handbrake = m_currentHandBrake;
		}
		EnableCameraIdleRotate();
		m_car3dView.IconGenerate();
		base.OnDeactivate();
	}

	private void FixedUpdate()
	{
		float currentVelocity = 0f;
		m_currentSteer = Mathf.SmoothDamp(m_raceCar.carX.GetSteer(), m_targetSteer, ref currentVelocity, 0.05f);
		m_raceCar.carX.SetSteer(m_currentSteer, useAssistent: false, Time.deltaTime);
		if (modeIndex.value != -1)
		{
			m_currentState?.FixedUpdate();
		}
	}

	private void Update()
	{
		CustomCameraPresets instance = Singleton<CustomCameraPresets>.instance;
		if (instance != null)
		{
			CustomCameraPreset currentPreset = instance.currentPreset;
			if ((currentPreset == null || currentPreset.stateType != CustomizationCameraPreset.Garage) && m_cameraRotation != null)
			{
				if (InputManager.instance.IsLastActiveJoystickAxis2D(1, out var axis) && axis != Vector2.zero)
				{
					instance.CameraTo(CustomizationCameraPreset.Garage);
				}
				if (!UIUtils.IsPointerOverGameObject() && (Input.touchCount > 0 || Input.GetMouseButton(0)) && !m_cameraRotation.skipToRotateByMouse)
				{
					instance.CameraTo(CustomizationCameraPreset.Garage);
				}
			}
		}
		UpdateHotkeyDelay(m_upDelay);
		UpdateHotkeyDelay(m_downDelay);
		UpdateHotkeyDelay(m_leftDelay);
		UpdateHotkeyDelay(m_rightDelay);
		if (!m_raceCar.carModel.IsLoading())
		{
			lockPartSelection = false;
		}
	}

	private void EnableCameraIdleRotate(bool cameraIdleRotate = true)
	{
		CameraRotation instance = CameraRotation.instance;
		if (!(instance == null))
		{
			instance.disableReturnToIdle = !cameraIdleRotate;
		}
	}

	private void UpdateHotkeyDelay(HotkeyDelay delay)
	{
		if (m_inputManager.IsActionPressed(delay.actionType) || m_inputManager.IsActionReleased(delay.actionType))
		{
			delay.Reset();
		}
	}

	public CategoryNavMode GetCategoryNavMode()
	{
		return m_navMode;
	}

	public void SelectCategory(StateType stateType)
	{
		enabledAdditionalColorPanel.value = false;
		for (int i = 0; i < m_categories.Count; i++)
		{
			UIPaintCategory uIPaintCategory = m_categories[i];
			if (!(uIPaintCategory == null))
			{
				bool flag = uIPaintCategory.stateType == stateType;
				uIPaintCategory.selected.value = flag;
				if (flag)
				{
					navCategoryIndex.value = i;
				}
			}
		}
	}

	private void DefineExternalParts()
	{
		m_hasExternalParts = CarPartDatabase.IsCarBodypartsListContainsUGC(m_carId);
	}

	private void HidePartIfNeed(UIPaintCategory cat)
	{
		if (cat == null)
		{
			return;
		}
		foreach (UIBodyPartPaintItem part in cat.paintState.parts)
		{
			if (!(part == null) && NeedSkipPartByRule(part.partType))
			{
				part.gameObject.SetActive(value: false);
			}
		}
	}

	public bool NeedSkipPartByRule(PartType partType)
	{
		if (m_skipPartsRules.TryGetValue(m_playerCar.id, out var value))
		{
			return value.Exists((PartType el) => el == partType);
		}
		if (m_hasExternalParts)
		{
			return m_skipPartsRulesForUgcCar.Contains(partType);
		}
		return false;
	}

	private bool CheckIsForbiddenToPaint(StateType stateType)
	{
		if (m_forbiddenPaintRules.TryGetValue(m_playerCar.id, out var value))
		{
			return value.Exists((StateType el) => el == stateType);
		}
		return false;
	}

	public void ActivateCategory(StateType stateType)
	{
		ReloadVisualSettings();
		m_categoryNavIteration = 0;
		if ((stateType == StateType.Body && m_paintBodyNotSupport.value) || CheckIsForbiddenToPaint(stateType))
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.paint_not_support.Localized(), 3f);
			return;
		}
		lockPartSelection = false;
		modeIndex.value = (int)stateType;
		m_currentState?.Deactivate();
		SelectCategory(stateType);
		foreach (UIPaintCategory category in m_categories)
		{
			category.SetCategoryActive(active: false);
		}
		navCategoryIndex.value = (int)stateType;
		if (stateType == StateType.Category)
		{
			m_currentState = null;
			return;
		}
		SendAnalytics(stateType);
		displayPartSelector.value = stateType != StateType.Smoke;
		MultiselectMode = false;
		m_categories[(int)stateType].SetCategoryActive(active: true);
		m_currentState = m_states[(int)stateType];
		m_navMode = CategoryNavMode.Parts;
		m_categories[(int)stateType].SelectFirstEnabledPart();
		EnsurePartItemVisibility();
		ResetColorSliders();
		navFavColorsIndex.value = 0;
		SelectFavoriteColor(-1);
		SelectColorSlider(-1);
		UpdateCategoryPartsMaterial();
		UpdateMaterialSelector();
		if (stateType == StateType.Smoke)
		{
			m_navMode = CategoryNavMode.Colors;
			navColorSliderIndex.value = 0;
			SelectColorSlider(0);
		}
		UpdateBottomPanel();
		m_currentState.Activate();
		m_cart.Clear();
		SetWheelVisible(visible: true);
		MarkSelectedPart();
		ResetHotkeysDelay();
		HidePartIfNeed(m_categories[(int)stateType]);
	}

	public Sprite GetPartIcon(PartType type)
	{
		if (type == PartType.Smoke)
		{
			return null;
		}
		PartIconPair[] array = partIcons;
		foreach (PartIconPair partIconPair in array)
		{
			if (partIconPair.type == type)
			{
				return partIconPair.sprite;
			}
		}
		Debug.LogError($"Unable to find part icon with type '{type}'");
		return null;
	}

	private void InitStates()
	{
		if (m_states == null)
		{
			m_states = new BasePaintState[5];
			m_states[0] = new BodyPaintState(this, m_categorySprites[0]);
			m_states[1] = new InteriorPaintState(this, m_categorySprites[1]);
			m_states[2] = new SuspensionPaintState(this, m_categorySprites[2]);
			m_states[3] = new GlassBasePaintState(this, m_categorySprites[3]);
			m_states[4] = new SmokePaintState(this, m_categorySprites[4]);
		}
	}

	private UIPaintCategory InstantiateCategory(BasePaintState basePaintState)
	{
		UIPaintCategory component = UnityEngine.Object.Instantiate(m_prefabCategory, m_categoriesContainer).GetComponent<UIPaintCategory>();
		component.SetData(this, basePaintState);
		return component;
	}

	private void UpdateTopPanel()
	{
		switch ((StateType)modeIndex.value)
		{
		case StateType.Category:
			ShowTopPanel(ETopPanelState.Title, LocalizationKeys.customization_paint.Localized());
			break;
		case StateType.Body:
			ShowTopPanel(ETopPanelState.Title, LocalizationKeys.lvlup_paintcar.Localized());
			break;
		case StateType.Interior:
			ShowTopPanel(ETopPanelState.Title, LocalizationKeys.paint_interior_title.Localized());
			break;
		case StateType.Suspension:
			ShowTopPanel(ETopPanelState.Title, LocalizationKeys.paint_suspension_title.Localized());
			break;
		case StateType.Glass:
			ShowTopPanel(ETopPanelState.Title, LocalizationKeys.lvlup_paintglass.Localized());
			break;
		case StateType.Smoke:
			ShowTopPanel(ETopPanelState.Title, LocalizationKeys.lvlup_paintsmoke.Localized());
			break;
		}
	}

	private void UpdateBottomPanel()
	{
		UIPaintCategory currentCategory = GetCurrentCategory();
		List<ActionHintData> list = new List<ActionHintData>();
		if (modeIndex.value == -1)
		{
			list.AddRange(new ActionHintData[2]
			{
				new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_goto_back.Localized(),
					type = ActionType.NavCancelBtnB,
					action = NavigationCancel
				},
				new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_select.Localized(),
					type = ActionType.NavSelectBtnA,
					action = NavigationSubmit
				}
			});
		}
		else
		{
			list.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = NavigationCancel
			});
			if (currentCategory != null && m_navMode == CategoryNavMode.Parts)
			{
				UIBodyPartPaintItem selectedPart = currentCategory.GetSelectedPart();
				if (selectedPart != null && selectedPart.partType != PartType.Smoke)
				{
					LocalizationKeys.Key key = (MultiselectMode ? LocalizationKeys.part_multiselect_set_off : LocalizationKeys.part_multiselect_set_on);
					list.Add(new ActionHintData
					{
						hint = key.Localized(),
						type = ActionType.NavBtnExpand,
						action = NavigationMultiselectToggle
					});
				}
				if (MultiselectMode)
				{
					LocalizationKeys.Key key2 = (selectedPart.addedToPaint.value ? LocalizationKeys.part_to_paint_remove : LocalizationKeys.part_to_paint_add);
					list.Add(new ActionHintData
					{
						hint = key2.Localized(),
						type = ActionType.NavSelectBtnA,
						action = NavigationTogglePart
					});
				}
			}
			list.Add(new ActionHintData
			{
				hint = LocalizationKeys.part_reset_color.Localized(),
				type = ActionType.NavBtnExpandRight,
				action = ResetSelectPartColor
			});
			if (m_navMode == CategoryNavMode.FavoriteColors)
			{
				list.Add(new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_override.Localized(),
					type = ActionType.NavReport,
					action = SaveSelectFavoriteColor
				});
				if (!m_colorPresets[navFavColorsIndex.value].isEmpty.value)
				{
					list.Add(new ActionHintData
					{
						hint = LocalizationKeys.nav_hint_apply.Localized(),
						type = ActionType.NavSelectBtnA,
						action = ApplySelectFavoriteColor
					});
				}
			}
		}
		ShowBottomPanel(list.ToArray());
		m_topPanelContext.BringForwardByDepth();
	}

	private void OnCarVisualLoadFinishEventHandler(RaceCar loadedCar)
	{
		lockPartSelection = false;
	}

	private void OnModeChanged()
	{
		if (m_missController != null && modeIndex.value == -1 && m_catSelectCoroutine == null)
		{
			m_catSelectCoroutine = TimeController.StartNow(CatButtonCoroutine());
		}
	}

	private IEnumerator CatButtonCoroutine()
	{
		yield return null;
		m_missController.SetLastButton(null);
		m_catSelectCoroutine = null;
	}

	private void EnsurePartItemVisibility()
	{
		if (navCategoryIndex.value < 0 || navCategoryIndex.value >= m_categories.Count)
		{
			return;
		}
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory != null)
		{
			UIBodyPartPaintItem selectedPart = currentCategory.GetSelectedPart();
			if (selectedPart != null && !m_scrollRect.IsScrollItemHorizontallyVisible(selectedPart.rectTransform))
			{
				m_scrollRect.HorizontalSnapScrollTo(selectedPart.rectTransform, 9f);
			}
		}
	}

	public void OnSelectedPartChanged()
	{
		EnsurePartItemVisibility();
		ResetColorSliders();
		UpdateMaterialSelector();
		MarkSelectedPart();
	}

	public void OnSelectedPartChanged(int index)
	{
		m_navMode = CategoryNavMode.Parts;
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory != null)
		{
			currentCategory.SelectPart(index);
		}
		EnsurePartItemVisibility();
		ResetColorSliders();
		UpdateMaterialSelector();
		MarkSelectedPart();
		UpdateBottomPanel();
		IfNavColorsResetSliderSelectIndex();
	}

	private void IfNavColorsResetSliderSelectIndex()
	{
		if (GetCategoryNavMode() == CategoryNavMode.Colors)
		{
			navColorSliderIndex.value = 0;
			SelectColorSlider(-1);
		}
	}

	public void SelectItemInCategory(int index, CategoryNavMode category)
	{
		ResetSelection();
		m_navMode = category;
		if (m_navMode == CategoryNavMode.Parts)
		{
			UIPaintCategory currentCategory = GetCurrentCategory();
			if (currentCategory != null)
			{
				currentCategory.SelectPart(index);
			}
			EnsurePartItemVisibility();
			ResetColorSliders();
			UpdateMaterialSelector();
			SetMaterialToMultiselect();
			UpdateBottomPanel();
		}
		else if (m_navMode == CategoryNavMode.FavoriteColors)
		{
			navFavColorsIndex.value = index;
			SelectFavoriteColor(navFavColorsIndex.value);
		}
		IfNavColorsResetSliderSelectIndex();
		InputManager.PlayClickSound();
	}

	private UIPaintCategory GetCurrentCategory()
	{
		if (navCategoryIndex.value < 0 || navCategoryIndex.value >= m_categories.Count)
		{
			return null;
		}
		return m_categories[navCategoryIndex.value];
	}

	private void ReloadVisualSettings()
	{
		CarVisualManager.VisualSettings visualForCar = prefs.carSettings.GetVisualForCar(m_carId);
		m_previewSettings = visualForCar.Copy();
		m_currentSettings = visualForCar.Copy();
		m_slotScheme = new CarPartDatabase.SlotScheme(m_carId, m_previewSettings.parts);
		SetDefaultVisualIfNeeded();
		if (navCategoryIndex.value >= 0)
		{
			UIPaintCategory currentCategory = GetCurrentCategory();
			if (currentCategory != null)
			{
				ApplyPreviewSettings(GetApplyFlags(currentCategory.stateType));
			}
		}
	}

	private void SetDefaultVisualIfNeeded()
	{
		if (m_currentSettings.colors.Count <= 0)
		{
			m_previewSettings.ResetColors(m_playerCar);
			m_currentSettings = m_previewSettings.Copy();
		}
	}

	private void ApplyVisuals(CarVisualManager.VisualSettings visuals, bool reload = true)
	{
		m_car3dView.ApplyCustomization(CarVisualManager.CustomizationFlags.AllColors, visuals);
		m_carVisualManager.SaveVisualSettingsForCar(m_carId, visuals);
		if (reload)
		{
			ReloadVisualSettings();
		}
	}

	private void ApplyPreviewSettings(CarVisualManager.CustomizationFlags flags)
	{
		m_car3dView.ApplyCustomization(flags, m_previewSettings);
	}

	private void ResetColorSliders()
	{
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return;
		}
		Color? color = null;
		Color? color2 = null;
		if (currentCategory.stateType != StateType.Smoke)
		{
			UIBodyPartPaintItem selectedPart = currentCategory.GetSelectedPart();
			if (selectedPart != null)
			{
				color = GetPartColorByType(selectedPart.partType);
				color2 = GetPartCustomSecColorByType(selectedPart.partType);
			}
		}
		else
		{
			color = GetPartColorByType(PartType.Smoke);
			color2 = GetPartCustomSecColorByType(PartType.Smoke);
		}
		if (color.HasValue)
		{
			m_changePartColor = false;
			HSBColor hSBColor = new HSBColor(color.Value);
			colorH.value = hSBColor.h;
			colorS.value = hSBColor.s;
			colorB.value = hSBColor.b;
			m_changePartColor = true;
			m_prevColorH = colorH.value;
			ColorToHex();
			UpdateCustomSecColorGradient();
		}
		if (color2.HasValue)
		{
			m_changePartColor = false;
			HSBColor hSBColor2 = new HSBColor(color2.Value);
			customSecColorH.value = hSBColor2.h;
			customSecColorS.value = hSBColor2.s;
			customSecColorB.value = hSBColor2.b;
			m_changePartColor = true;
			m_prevCustomColorH = customSecColorH.value;
			CustomSecColorToHex();
			UpdateCustomSecColorGradient();
		}
	}

	private Color GetPartColorByType(PartType type)
	{
		return GetPartPaintByType(type)?.color ?? Color.white;
	}

	private Color GetPartCustomSecColorByType(PartType type)
	{
		return GetPartPaintByType(type)?.CustomSecColor ?? Color.white;
	}

	private CarVisualManager.CarPaintHolder GetPartPaintByType(PartType type)
	{
		if (m_previewSettings.colors.Count < 0)
		{
			GameLog.LogError($"Unable to get paint from settings for type '{type}', colors is empty");
			return null;
		}
		CarVisualManager.CustomizationFlags type2 = PartTypeToFlag(type);
		CarVisualManager.CarPaintHolder paint = m_previewSettings.GetPaint(type2);
		if (paint != null)
		{
			return paint;
		}
		GameLog.LogError($"Unable to get paint from settings for type '{type}'");
		return null;
	}

	private static CarVisualManager.CustomizationFlags GetApplyFlags(StateType stateType)
	{
		return stateType switch
		{
			StateType.Body => CarVisualManager.CustomizationFlags.AllBodyColors, 
			StateType.Interior => CarVisualManager.CustomizationFlags.AllInteriorColors, 
			StateType.Suspension => CarVisualManager.CustomizationFlags.AllSuspensionAndWheelsColors, 
			StateType.Glass => CarVisualManager.CustomizationFlags.AllGlassColors, 
			StateType.Smoke => CarVisualManager.CustomizationFlags.SmokeColor, 
			_ => CarVisualManager.CustomizationFlags.None, 
		};
	}

	private static CarVisualManager.CustomizationFlags PartTypeToFlag(PartType partType)
	{
		switch (partType)
		{
		case PartType.Chassis:
			return CarVisualManager.CustomizationFlags.ChassisColor;
		case PartType.Ramp:
			return CarVisualManager.CustomizationFlags.RampColor;
		case PartType.RollCage:
			return CarVisualManager.CustomizationFlags.RollCageColor;
		case PartType.FrontBumper:
			return CarVisualManager.CustomizationFlags.FrontBumperColor;
		case PartType.RearBumper:
			return CarVisualManager.CustomizationFlags.RearBumperColor;
		case PartType.Skirts:
			return CarVisualManager.CustomizationFlags.SkirtsColor;
		case PartType.Doors:
			return CarVisualManager.CustomizationFlags.DoorsColor;
		case PartType.Roof:
			return CarVisualManager.CustomizationFlags.RoofColor;
		case PartType.SideMirrors:
			return CarVisualManager.CustomizationFlags.SideMirrorsColor;
		case PartType.Hood:
			return CarVisualManager.CustomizationFlags.HoodColor;
		case PartType.Trunk:
			return CarVisualManager.CustomizationFlags.TrunkColor;
		case PartType.Spoiler:
			return CarVisualManager.CustomizationFlags.SpoilerColor;
		case PartType.Fenders:
			return CarVisualManager.CustomizationFlags.FendersColor;
		case PartType.FrontLights:
			return CarVisualManager.CustomizationFlags.FrontLightsColor;
		case PartType.RearLights:
			return CarVisualManager.CustomizationFlags.RearLightsColor;
		case PartType.SteeringWheelSpokes:
			return CarVisualManager.CustomizationFlags.SteeringWheelSpokesColor;
		case PartType.SteeringWheelCover:
			return CarVisualManager.CustomizationFlags.SteeringWheelCoverColor;
		case PartType.Shifter:
			return CarVisualManager.CustomizationFlags.ShifterColor;
		case PartType.Handbrake:
			return CarVisualManager.CustomizationFlags.HandbrakeColor;
		case PartType.RightSeatBase:
			return CarVisualManager.CustomizationFlags.RightSeatBaseColor;
		case PartType.RightSeatCushion:
			return CarVisualManager.CustomizationFlags.RightSeatCushionColor;
		case PartType.LeftSeatBase:
			return CarVisualManager.CustomizationFlags.LeftSeatBaseColor;
		case PartType.LeftSeatCushion:
			return CarVisualManager.CustomizationFlags.LeftSeatCushionColor;
		case PartType.FrontRims:
			return CarVisualManager.CustomizationFlags.FrontWheelRimsColor;
		case PartType.FrontSpokes:
			return CarVisualManager.CustomizationFlags.FrontWheelSpokesColor;
		case PartType.RearRims:
			return CarVisualManager.CustomizationFlags.RearWheelRimsColor;
		case PartType.RearSpokes:
			return CarVisualManager.CustomizationFlags.RearWheelSpokesColor;
		case PartType.TireSticker1:
			return CarVisualManager.CustomizationFlags.TireSticker1;
		case PartType.TireSticker2:
			return CarVisualManager.CustomizationFlags.TireSticker2;
		case PartType.Arm:
			return CarVisualManager.CustomizationFlags.ArmColor;
		case PartType.Spring:
			return CarVisualManager.CustomizationFlags.SpringColor;
		case PartType.Strut:
			return CarVisualManager.CustomizationFlags.StrutColor;
		case PartType.Calliper:
			return CarVisualManager.CustomizationFlags.CalliperColor;
		case PartType.FrontLightsGlass:
			return CarVisualManager.CustomizationFlags.FrontLightsGlassColor;
		case PartType.RearLightsGlass:
			return CarVisualManager.CustomizationFlags.RearLightsGlassColor;
		case PartType.Windows:
			return CarVisualManager.CustomizationFlags.WindowsColor;
		case PartType.Smoke:
			return CarVisualManager.CustomizationFlags.SmokeColor;
		default:
			GameLog.LogError($"Unable to get color for part type '{partType}'");
			return CarVisualManager.CustomizationFlags.None;
		}
	}

	private void ResetSelection()
	{
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory != null)
		{
			currentCategory.paintState.SelectPart(-1);
		}
		SelectFavoriteColor(-1);
		SelectColorSlider(-1);
	}

	private void UpdateCategorySelection(bool navigatedUp)
	{
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return;
		}
		ResetSelection();
		if (m_navMode == CategoryNavMode.Parts)
		{
			currentCategory.SelectPart(currentCategory.selectedPartIndex);
			ResetColorSliders();
		}
		else if (m_navMode == CategoryNavMode.Colors)
		{
			navColorSliderIndex.value = (navigatedUp ? m_colorChangers.FindLastIndex((UIColorChangerContext el) => el.slider.gameObject.activeInHierarchy) : m_colorChangers.FindIndex((UIColorChangerContext el) => el.slider.gameObject.activeInHierarchy));
			SelectColorSlider(navColorSliderIndex.value);
		}
		else if (m_navMode == CategoryNavMode.FavoriteColors)
		{
			SelectFavoriteColor(navFavColorsIndex.value);
		}
		MarkSelectedPart();
		UpdateBottomPanel();
	}

	private void MarkSelectedPart()
	{
		if (MultiselectMode)
		{
			return;
		}
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return;
		}
		UIBodyPartPaintItem selectedPart = currentCategory.GetSelectedPart();
		if (selectedPart == null)
		{
			return;
		}
		foreach (UIBodyPartPaintItem part in currentCategory.paintState.parts)
		{
			if (part != null)
			{
				part.addedToPaint.value = false;
			}
		}
		selectedPart.addedToPaint.value = true;
	}

	private void SelectColorSlider(int index)
	{
		UIColorChangerContext.DetachListener();
		for (int i = 0; i < m_colorChangers.Count; i++)
		{
			m_colorChangers[i].selected.value = i == index;
			if (i == index)
			{
				m_colorChangers[i].StartListenKeys();
			}
		}
	}

	private void OnColorChanged()
	{
		if (!m_dialogActive)
		{
			HSBPaintHelper.ResetColorIfNeed(ref m_prevColorH, colorH, colorS, colorB);
			if (m_changePartColor)
			{
				HSBColor hSBColor = new HSBColor(colorH.value, colorS.value, colorB.value);
				HSBColor hSBColor2 = new HSBColor(customSecColorH.value, customSecColorS.value, customSecColorB.value);
				ChangeColor(hSBColor.ToColor());
				ChangeCustomSecColor(hSBColor2.ToColor());
			}
			UpdateColorGradient();
		}
	}

	private void OnCustomSecColorChanged()
	{
		if (!m_dialogActive)
		{
			HSBPaintHelper.ResetColorIfNeed(ref m_prevCustomColorH, customSecColorH, customSecColorS, customSecColorB);
			if (m_changePartColor)
			{
				ChangeCustomSecColor(new HSBColor(customSecColorH.value, customSecColorS.value, customSecColorB.value).ToColor());
			}
			UpdateCustomSecColorGradient();
		}
	}

	private void ChangeColor(Color color)
	{
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return;
		}
		List<UIBodyPartPaintItem> addedToPaintParts = currentCategory.GetAddedToPaintParts();
		CarVisualManager.CustomizationFlags customizationFlags = CarVisualManager.CustomizationFlags.None;
		foreach (UIBodyPartPaintItem item in addedToPaintParts)
		{
			CarVisualManager.CustomizationFlags customizationFlags2 = PartTypeToFlag(item.partType);
			CarVisualManager.CarPaintHolder paint = m_previewSettings.GetPaint(customizationFlags2);
			if (paint != null && paint.color.HasValue)
			{
				if (paint.color != color)
				{
					paint.color = color;
					AddPartToCart(paint, item.partType);
					customizationFlags |= customizationFlags2;
				}
				paint.color = color;
			}
		}
		if (customizationFlags != CarVisualManager.CustomizationFlags.None)
		{
			ApplyPreviewSettings(customizationFlags);
			CalculatePaintCost();
		}
		ColorToHex();
	}

	private void ChangeCustomSecColor(Color color)
	{
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return;
		}
		if (color == Color.clear)
		{
			color = ColorExtensions.GetOldSecondaryChameleonColor(GetPartColorByType(currentCategory.GetSelectedPart().partType));
		}
		List<UIBodyPartPaintItem> addedToPaintParts = currentCategory.GetAddedToPaintParts();
		CarVisualManager.CustomizationFlags customizationFlags = CarVisualManager.CustomizationFlags.None;
		foreach (UIBodyPartPaintItem item in addedToPaintParts)
		{
			CarVisualManager.CustomizationFlags customizationFlags2 = PartTypeToFlag(item.partType);
			CarVisualManager.CarPaintHolder paint = m_previewSettings.GetPaint(customizationFlags2);
			if (paint != null && paint.CustomSecColor.HasValue)
			{
				if (paint.color != color)
				{
					paint.CustomSecColor = color;
					AddPartToCart(paint, item.partType);
					customizationFlags |= customizationFlags2;
				}
				paint.CustomSecColor = color;
			}
		}
		if (customizationFlags != CarVisualManager.CustomizationFlags.None)
		{
			ApplyPreviewSettings(customizationFlags);
			CalculatePaintCost();
		}
		CustomSecColorToHex();
	}

	private bool IsPartChanged(UIBodyPartPaintItem partItem)
	{
		if (partItem == null)
		{
			return false;
		}
		CarVisualManager.CustomizationFlags type = PartTypeToFlag(partItem.partType);
		CarVisualManager.CarPaintHolder paint = m_previewSettings.GetPaint(type);
		CarVisualManager.CarPaintHolder defaultPaint = m_previewSettings.GetDefaultPaint(m_playerCar, type);
		Color? color = paint.color;
		Color? color2 = defaultPaint.color;
		if (color.HasValue != color2.HasValue || (color.HasValue && color.GetValueOrDefault() != color2.GetValueOrDefault()) || paint.indexConfig != defaultPaint.indexConfig)
		{
			return true;
		}
		if (partItem.partType != PartType.Arm && partItem.partType != PartType.Spring && partItem.partType != PartType.Strut && partItem.partType != PartType.Calliper)
		{
			return paint.id != defaultPaint.id;
		}
		return false;
	}

	private void UpdatePartsMaterial()
	{
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return;
		}
		List<UIBodyPartPaintItem> addedToPaintParts = currentCategory.GetAddedToPaintParts();
		CarVisualManager.CustomizationFlags customizationFlags = CarVisualManager.CustomizationFlags.None;
		foreach (UIBodyPartPaintItem item in addedToPaintParts)
		{
			if (item.partType == PartType.Ramp)
			{
				continue;
			}
			CarVisualManager.CustomizationFlags customizationFlags2 = PartTypeToFlag(item.partType);
			CarVisualManager.CarPaintHolder paint = m_previewSettings.GetPaint(customizationFlags2);
			if (paint != null && paint.id.HasValue)
			{
				int num = item.PaintTypeToMaterialIndex();
				int indexConfig = item.GetIndexConfig();
				if (paint.id.Value != num || paint.indexConfig != indexConfig)
				{
					paint.id = num;
					paint.indexConfig = indexConfig;
					AddPartToCart(paint, item.partType);
					customizationFlags |= customizationFlags2;
				}
			}
		}
		if (customizationFlags != CarVisualManager.CustomizationFlags.None)
		{
			ApplyPreviewSettings(customizationFlags);
			CalculatePaintCost();
		}
	}

	private void UpdateCategoryPartsMaterial()
	{
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return;
		}
		foreach (UIBodyPartPaintItem part in currentCategory.paintState.parts)
		{
			CarVisualManager.CustomizationFlags type = PartTypeToFlag(part.partType);
			CarVisualManager.CarPaintHolder paint = m_previewSettings.GetPaint(type);
			if (paint != null && paint.id.HasValue)
			{
				part.SetPartMaterial(paint.id.Value, paint.indexConfig.GetValueOrDefault());
			}
		}
	}

	private void SetMaterialToMultiselect()
	{
		if (!MultiselectMode)
		{
			return;
		}
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return;
		}
		UIBodyPartPaintItem selectedPart = currentCategory.GetSelectedPart();
		if (selectedPart == null || !selectedPart.canChangeMaterial)
		{
			return;
		}
		foreach (UIBodyPartPaintItem addedToPaintPart in currentCategory.GetAddedToPaintParts())
		{
			if (addedToPaintPart.partType != selectedPart.partType)
			{
				addedToPaintPart.SetPartMaterial(selectedPart.materialId, selectedPart.indexConfig);
			}
		}
		UpdatePartsMaterial();
	}

	private void LoadFavoriteColors()
	{
		foreach (CarColorFavorite favoriteColor in prefs.globals.favoriteColors)
		{
			m_colorPresets[favoriteColor.favoriteColorsIndex].SetColor(favoriteColor);
		}
	}

	private void SelectFavoriteColor(int index)
	{
		for (int i = 0; i < m_colorPresets.Length; i++)
		{
			m_colorPresets[i].selected.value = i == index;
		}
		if (index >= 0 && index < m_colorPresets.Length)
		{
			UpdateBottomPanel();
		}
	}

	private bool IsBashBarPart(string name)
	{
		foreach (CarPart part in CarPartDatabase.GetParts(m_slotScheme, name, "", forceAdd: true))
		{
			if (m_currentSettings.parts.Contains(part.id) && !string.IsNullOrEmpty(part.vendor) && part.vendor.ToLower() == "bashbar")
			{
				return true;
			}
		}
		return false;
	}

	private void UpdateMaterialSelector()
	{
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null || currentCategory.stateType == StateType.Smoke || currentCategory.stateType == StateType.Interior || currentCategory.stateType == StateType.Glass)
		{
			materialAvailable.value = false;
			materialAdditionalAvailable.value = false;
			return;
		}
		UIBodyPartPaintItem part = currentCategory.GetSelectedPart();
		materialAvailable.value = part.canChangeMaterial;
		materialAdditionalAvailable.value = part.canChangeMaterial && part.GetIndexConfigCount() > 1 && currentCategory.stateType == StateType.Body && part.partType != PartType.RollCage;
		if (m_currentState is BodyPaintState bodyPaintState)
		{
			Property<bool> property = materialAdditionalAvailable;
			property.value = part.partType switch
			{
				PartType.Hood => !bodyPaintState.IsEmptyHood(), 
				PartType.FrontBumper => !IsBashBarPart("bumperfront") && IsPartNoLiveryAndExistBody("BumperFront"), 
				PartType.RearBumper => !IsBashBarPart("bumperrear") && IsPartNoLiveryAndExistBody("BumperRear"), 
				_ => materialAdditionalAvailable.value && IsPartNoLiveryAndExistBody(GetCameraPresetByType(part.partType).ToString()), 
			};
		}
		enabledAdditionalColorPanel.value = materialAvailable.value && part.AvailableAdditionalColorToMaterial();
		if (m_navMode == CategoryNavMode.Colors && !m_colorChangers[navColorSliderIndex.value].slider.gameObject.activeInHierarchy)
		{
			NavigationUp();
		}
		if (materialAvailable.value)
		{
			CarColorMeta carColorMeta = m_materials.Find((CarColorMeta x) => x.id == part.selectedMaterialIndex);
			if (carColorMeta != null)
			{
				materialSelected.value = LocalizationService.Localize(carColorMeta.name);
				string text = carColorMeta.name.Substring(0, carColorMeta.name.LastIndexOf("_", StringComparison.Ordinal)) + "_";
				materialAdditionalSelected.value = LocalizationService.Localize((text + GetIndexConfig(carColorMeta.id, part.indexConfig).indexType).ToLower());
			}
		}
		bool IsPartNoLiveryAndExistBody(string typeId)
		{
			return m_car3dView.currentCar.GetRootObject(typeId, "body").Length != 0;
		}
	}

	private CarPaint.CarPaintChild GetIndexConfig(int id, int indexConfig)
	{
		return CarModelConfig.config.carPaintUtility.GetIndexConfigs(CarVisualManager.MaterialIdToCarPaintType(id))[indexConfig];
	}

	private CarColorFavorite MakeFavoriteColor()
	{
		if (m_previewSettings == null)
		{
			return null;
		}
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return null;
		}
		Color main = new HSBColor(colorH.value, colorS.value, colorB.value, 1f).ToColor();
		Color additional = new HSBColor(customSecColorH.value, customSecColorS.value, customSecColorB.value, 1f).ToColor();
		Color color = ((currentCategory.stateType == StateType.Body) ? Color.black : Color.white);
		return new CarColorFavorite(main, additional, color, color);
	}

	private void SaveSelectFavoriteColor()
	{
		List<CarColorFavorite> favoriteColors = prefs.globals.favoriteColors;
		int value = navFavColorsIndex.value;
		CarColorFavorite carColorFavorite = MakeFavoriteColor();
		if (carColorFavorite == null)
		{
			return;
		}
		carColorFavorite.favoriteColorsIndex = value;
		m_colorPresets[value].SetColor(carColorFavorite);
		favoriteColors.Clear();
		UIColorPresetContext[] colorPresets = m_colorPresets;
		foreach (UIColorPresetContext uIColorPresetContext in colorPresets)
		{
			if (!uIColorPresetContext.isEmpty.value)
			{
				favoriteColors.Add(uIColorPresetContext.color);
			}
		}
		prefs.globals.Save();
		UpdateBottomPanel();
	}

	public void ApplyFavoriteColor(int index)
	{
		navFavColorsIndex.value = index;
		ApplySelectFavoriteColor();
	}

	private void ApplySelectFavoriteColor()
	{
		if (navFavColorsIndex.value >= 0 && navFavColorsIndex.value < m_colorPresets.Length)
		{
			UIColorPresetContext uIColorPresetContext = m_colorPresets[navFavColorsIndex.value];
			ChangeColor(uIColorPresetContext.color.colorMain);
			ChangeCustomSecColor(uIColorPresetContext.color.colorAdditional);
			ResetColorSliders();
		}
	}

	private void ResetSelectPartColor()
	{
		if (m_previewSettings == null || m_playerCar == null)
		{
			return;
		}
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null || IsMultiselectOrSelectPartChanged(currentCategory))
		{
			return;
		}
		UIMessageBox.ShowQuestion(LocalizationKeys.dyno_ask_reset_single_to_default.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
		{
			List<UIBodyPartPaintItem> addedToPaintParts = GetCurrentCategory().GetAddedToPaintParts();
			CarVisualManager.CustomizationFlags customizationFlags = CarVisualManager.CustomizationFlags.None;
			foreach (UIBodyPartPaintItem item in addedToPaintParts)
			{
				CarVisualManager.CustomizationFlags customizationFlags2 = PartTypeToFlag(item.partType);
				CarVisualManager.CarPaintHolder paint = m_previewSettings.GetPaint(customizationFlags2);
				CarVisualManager.CarPaintHolder defaultPaint = m_previewSettings.GetDefaultPaint(m_playerCar, customizationFlags2);
				Color? color = paint.color;
				Color? color2 = defaultPaint.color;
				if (color.HasValue != color2.HasValue || (color.HasValue && color.GetValueOrDefault() != color2.GetValueOrDefault()) || paint.id != defaultPaint.id || paint.indexConfig != defaultPaint.indexConfig)
				{
					if (defaultPaint.id.HasValue)
					{
						item.SetPartMaterial(defaultPaint.id.Value, 0);
						RemovePartFromCart(item.partType);
					}
					customizationFlags |= customizationFlags2;
				}
				paint.color = defaultPaint.color;
				paint.CustomSecColor = Color.clear;
				paint.id = defaultPaint.id;
				paint.indexConfig = 0;
			}
			if (customizationFlags != CarVisualManager.CustomizationFlags.None)
			{
				ApplyPreviewSettings(customizationFlags);
				ResetColorSliders();
				SetMaterialToMultiselect();
				UpdateMaterialSelector();
				UpdatePartsMaterial();
				CalculatePaintCost();
			}
		}, LocalizationKeys.Ok.Localized());
	}

	private bool IsMultiselectOrSelectPartChanged(UIPaintCategory cat)
	{
		if (MultiselectMode)
		{
			if (IsCategoryAnyPartChanged(cat))
			{
				return true;
			}
		}
		else if (IsSelectPartChanged(cat))
		{
			return true;
		}
		return false;
	}

	private bool IsSelectPartChanged(UIPaintCategory category)
	{
		UIBodyPartPaintItem selectedPart = category.GetSelectedPart();
		if (selectedPart == null)
		{
			return true;
		}
		if (selectedPart.partType != PartType.Chassis)
		{
			return !IsPartChanged(selectedPart);
		}
		return false;
	}

	private bool IsCategoryAnyPartChanged(UIPaintCategory category)
	{
		foreach (UIBodyPartPaintItem addedToPaintPart in category.GetAddedToPaintParts())
		{
			if (addedToPaintPart != null && (addedToPaintPart.partType == PartType.Chassis || IsPartChanged(addedToPaintPart)))
			{
				return false;
			}
		}
		return true;
	}

	public void FocusCameraOnPart(PartType partType)
	{
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return;
		}
		if (currentCategory.paintState is SuspensionPaintState)
		{
			bool flag = partType == PartType.Arm || partType == PartType.Spring || partType == PartType.Strut;
			m_targetSteer = (flag ? 1f : 0f);
		}
		if (currentCategory.paintState is BodyPaintState bodyPaintState)
		{
			bool flag2 = partType != PartType.Ramp;
			bodyPaintState.SetHoodVisible(flag2);
			if (!flag2)
			{
				m_isHoodDetached = true;
				TimeController.StartNow(DelayedApplyCameraPresetByPart(partType));
				return;
			}
			if (m_isHoodDetached)
			{
				TimeController.StartNow(DelayedApplyCameraPresetByPart(partType));
				m_isHoodDetached = false;
				return;
			}
		}
		CustomizationCameraPreset customizationCameraPreset = ((partType == PartType.TireSticker1 || partType == PartType.TireSticker2) ? GetCameraPresetForTireSticker() : GetCameraPresetByType(partType));
		if (Singleton<CustomCameraPresets>.instance.currentPreset.stateType != customizationCameraPreset)
		{
			Singleton<CustomCameraPresets>.instance.CameraTo(customizationCameraPreset);
			SetWheelVisible(customizationCameraPreset != CustomizationCameraPreset.Suspension && customizationCameraPreset != CustomizationCameraPreset.BrakeCaliper);
		}
	}

	private IEnumerator DelayedApplyCameraPresetByPart(PartType partType)
	{
		RaceCar car = m_car3dView.currentCar;
		if (!(car == null))
		{
			yield return new WaitUntil(() => car.IsVisualLoaded);
			CustomizationCameraPreset cameraPresetByType = GetCameraPresetByType(partType);
			Singleton<CustomCameraPresets>.instance.CameraTo(cameraPresetByType);
		}
	}

	private CustomizationCameraPreset GetCameraPresetByType(PartType partType)
	{
		switch (partType)
		{
		case PartType.Arm:
		case PartType.Spring:
		case PartType.Strut:
			return CustomizationCameraPreset.Suspension;
		case PartType.Ramp:
			return CustomizationCameraPreset.Bonnet;
		case PartType.RollCage:
			return CustomizationCameraPreset.Cage;
		case PartType.FrontBumper:
			return CustomizationCameraPreset.BumperFront;
		case PartType.RearBumper:
			return CustomizationCameraPreset.BumperRear;
		case PartType.Skirts:
			return CustomizationCameraPreset.Skirts;
		case PartType.Doors:
			return CustomizationCameraPreset.Doors;
		case PartType.Roof:
			return CustomizationCameraPreset.Roof;
		case PartType.SideMirrors:
			return CustomizationCameraPreset.Mirrors;
		case PartType.Hood:
			return CustomizationCameraPreset.Bonnet;
		case PartType.Trunk:
			return CustomizationCameraPreset.Trunk;
		case PartType.Spoiler:
			return CustomizationCameraPreset.Spoiler;
		case PartType.FrontLights:
			return CustomizationCameraPreset.LightsFront;
		case PartType.RearLights:
			return CustomizationCameraPreset.LightsRear;
		case PartType.SteeringWheelSpokes:
		case PartType.SteeringWheelCover:
			return CustomizationCameraPreset.SteeringWheel;
		case PartType.Shifter:
			return CustomizationCameraPreset.Shifter;
		case PartType.Handbrake:
			return CustomizationCameraPreset.Handbrake;
		case PartType.RightSeatBase:
		case PartType.RightSeatCushion:
			return CustomizationCameraPreset.SeatRight;
		case PartType.LeftSeatBase:
		case PartType.LeftSeatCushion:
			return CustomizationCameraPreset.SeatLeft;
		case PartType.Calliper:
			return CustomizationCameraPreset.BrakeCaliper;
		case PartType.FrontLightsGlass:
			return CustomizationCameraPreset.LightsFront;
		case PartType.RearLightsGlass:
			return CustomizationCameraPreset.LightsRear;
		case PartType.Smoke:
			return CustomizationCameraPreset.Smoke;
		case PartType.RearRims:
		case PartType.RearSpokes:
			return CustomizationCameraPreset.RearRims;
		case PartType.FrontRims:
		case PartType.FrontSpokes:
		case PartType.TireSticker1:
		case PartType.TireSticker2:
			return CustomizationCameraPreset.FrontRims;
		case PartType.Chassis:
		case PartType.Fenders:
		case PartType.Windows:
			return CustomizationCameraPreset.Chassis;
		default:
			return CustomizationCameraPreset.Chassis;
		}
	}

	private CustomizationCameraPreset GetCameraPresetForTireSticker()
	{
		if (m_previewSettings.frontTire.logo == CarVisualManager.TireLogos.None)
		{
			return CustomizationCameraPreset.RearRims;
		}
		return CustomizationCameraPreset.FrontRims;
	}

	private void DisableBodyAllParts()
	{
		foreach (UIBodyPartPaintItem part in m_states[0].parts)
		{
			if (!(part == null))
			{
				bool value = part.partType == PartType.Chassis || part.partType == PartType.Ramp || part.partType == PartType.Fenders;
				part.ableToPaint.value = value;
			}
		}
	}

	private void DisplayOnlyAvailableParts()
	{
		RaceCar raceCar = m_car3dView.GetRaceCar();
		if (raceCar == null)
		{
			return;
		}
		bool flag = raceCar.carId == 43 || raceCar.carId == 44;
		Transform transform = raceCar.gameObject.transform.Find("Model");
		if (transform != null)
		{
			Transform transform2 = transform.Find("Skeleton");
			if (transform2 != null)
			{
				DisableBodyAllParts();
				if (transform2.transform.childCount == 1)
				{
					Transform child = transform2.GetChild(0);
					if (child.name.ToLower() == "root")
					{
						transform2 = child;
					}
				}
			}
			if (!flag)
			{
				if (transform2 != null)
				{
					DisableBodyAllParts();
					foreach (Transform item in transform2)
					{
						bool partEnabled = item.childCount > 0 && AbleToPaint(StateType.Body, raceCar.name, item);
						switch (item.name.ToLower())
						{
						case "chassis_root":
							EnablePart(StateType.Body, PartType.Fenders, partEnabled: true);
							EnablePart(StateType.Body, PartType.Ramp, partEnabled: true);
							break;
						case "bonnet_root":
							EnablePart(StateType.Body, PartType.Hood, partEnabled);
							break;
						case "bumperfront_root":
							EnablePart(StateType.Body, PartType.FrontBumper, partEnabled);
							break;
						case "bumperrear_root":
							EnablePart(StateType.Body, PartType.RearBumper, partEnabled);
							break;
						case "doors_root":
							EnablePart(StateType.Body, PartType.Doors, partEnabled);
							break;
						case "cage_root":
							EnablePart(StateType.Body, PartType.RollCage, partEnabled);
							break;
						case "lightsfront_root":
							EnablePart(StateType.Body, PartType.FrontLights, partEnabled);
							EnablePart(StateType.Glass, PartType.FrontLightsGlass, AbleToPaintLights(item));
							break;
						case "lightsrear_root":
							EnablePart(StateType.Body, PartType.RearLights, partEnabled);
							EnablePart(StateType.Glass, PartType.RearLightsGlass, AbleToPaintLights(item));
							break;
						case "mirrors_root":
							EnablePart(StateType.Body, PartType.SideMirrors, partEnabled);
							break;
						case "skirts_root":
							EnablePart(StateType.Body, PartType.Skirts, partEnabled);
							break;
						case "spoiler_root":
							EnablePart(StateType.Body, PartType.Spoiler, partEnabled);
							break;
						case "trunk_root":
							EnablePart(StateType.Body, PartType.Trunk, partEnabled);
							break;
						case "roof_root":
							EnablePart(StateType.Body, PartType.Roof, partEnabled);
							break;
						}
					}
				}
			}
			else
			{
				foreach (UIBodyPartPaintItem part in m_states[0].parts)
				{
					if (!(part == null))
					{
						part.ableToPaint.value = part.partType == PartType.Chassis;
					}
				}
				EnablePart(StateType.Glass, PartType.FrontLightsGlass, partEnabled: false);
				EnablePart(StateType.Glass, PartType.RearLightsGlass, partEnabled: false);
				foreach (Transform item2 in transform2)
				{
					string text = item2.name.ToLower();
					if (!(text == "lightsfront_root"))
					{
						if (text == "lightsrear_root")
						{
							EnablePart(StateType.Glass, PartType.RearLightsGlass, AbleToPaintLights(item2));
						}
					}
					else
					{
						EnablePart(StateType.Glass, PartType.FrontLightsGlass, AbleToPaintLights(item2));
					}
				}
			}
			bool value = false;
			if (!flag)
			{
				foreach (Transform item3 in transform)
				{
					if (!item3.name.StartsWith("interior", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					foreach (Transform item4 in item3)
					{
						bool flag2 = AbleToPaint(StateType.Interior, raceCar.name, item4);
						if (flag2)
						{
							value = true;
						}
						switch (item4.name)
						{
						case "steeringWheel_root":
							EnablePart(StateType.Interior, PartType.SteeringWheelCover, flag2);
							EnablePart(StateType.Interior, PartType.SteeringWheelSpokes, flag2);
							break;
						case "seatleft_root":
							EnablePart(StateType.Interior, PartType.LeftSeatBase, flag2);
							EnablePart(StateType.Interior, PartType.LeftSeatCushion, flag2);
							break;
						case "seatright_root":
							EnablePart(StateType.Interior, PartType.RightSeatBase, flag2);
							EnablePart(StateType.Interior, PartType.RightSeatCushion, flag2);
							break;
						case "shifter_root":
							EnablePart(StateType.Interior, PartType.Shifter, flag2);
							break;
						case "handbrake_root":
							EnablePart(StateType.Interior, PartType.Handbrake, flag2);
							break;
						}
					}
					break;
				}
			}
			foreach (UIPaintCategory category in m_categories)
			{
				if (category.stateType == StateType.Interior)
				{
					category.ableToPaint.value = value;
					break;
				}
			}
		}
		bool partEnabled2 = m_previewSettings.frontTire.logo != CarVisualManager.TireLogos.None || m_previewSettings.rearTire.logo != CarVisualManager.TireLogos.None;
		EnablePart(StateType.Suspension, PartType.TireSticker1, partEnabled2);
		EnablePart(StateType.Suspension, PartType.TireSticker2, partEnabled2);
	}

	private void EnablePart(StateType stateType, PartType partType, bool partEnabled)
	{
		foreach (UIBodyPartPaintItem part in m_states[(int)stateType].parts)
		{
			if (!(part == null) && part.partType == partType && !(part == null))
			{
				part.ableToPaint.value = partEnabled;
				break;
			}
		}
	}

	private bool IsCarPartNeedSkipPaint(string carName, string prefabName)
	{
		if (string.IsNullOrEmpty(carName) || string.IsNullOrEmpty(prefabName))
		{
			return true;
		}
		prefabName = prefabName.Replace("(Clone)", string.Empty);
		CarPartsToSkip carPartsToSkip = m_carPartsToSkip.Find((CarPartsToSkip item) => !string.IsNullOrEmpty(item.skipCarName) && item.skipCarName.Equals(carName, StringComparison.OrdinalIgnoreCase));
		if (string.IsNullOrEmpty(carPartsToSkip.skipCarName))
		{
			return false;
		}
		return Array.Exists(carPartsToSkip.skipPrefabNames, (string prName) => !string.IsNullOrEmpty(prName) && prName.Equals(prefabName, StringComparison.OrdinalIgnoreCase));
	}

	private bool AbleToPaint(StateType stateType, string modelName, Transform partRoot)
	{
		if (partRoot == null || partRoot.childCount <= 0 || partRoot.tag == "SkipHideCanvas")
		{
			return false;
		}
		Transform child = partRoot.GetChild(0);
		if (child == null)
		{
			return false;
		}
		if (IsCarPartNeedSkipPaint(modelName, child.name))
		{
			return false;
		}
		string text = child.name.ToLower();
		foreach (Transform item in child)
		{
			if (stateType == StateType.Body && (item.name == "body" || item.name == "body_nolivery"))
			{
				return true;
			}
			if (stateType == StateType.Interior)
			{
				if (text.StartsWith("mx5", StringComparison.OrdinalIgnoreCase) || text.StartsWith("m3e30", StringComparison.OrdinalIgnoreCase) || text.StartsWith("ae86", StringComparison.OrdinalIgnoreCase) || text.StartsWith("bmw", StringComparison.OrdinalIgnoreCase) || text.StartsWith("altezza", StringComparison.OrdinalIgnoreCase) || text.StartsWith("laurel", StringComparison.OrdinalIgnoreCase) || text.StartsWith("vfcommodore", StringComparison.OrdinalIgnoreCase) || text.StartsWith("vipersrt", StringComparison.OrdinalIgnoreCase) || text.StartsWith("nissan", StringComparison.OrdinalIgnoreCase) || text.StartsWith("toyota", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
				if (!text.StartsWith(modelName, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool AbleToPaintLights(Transform partRoot)
	{
		if (partRoot == null || partRoot.childCount <= 0)
		{
			return false;
		}
		foreach (Transform item in partRoot.GetChild(0))
		{
			if (item.gameObject.name == "glass_lights")
			{
				return true;
			}
		}
		return false;
	}

	private bool IsColorsDiffer(List<CarVisualManager.CarPaintHolder> colorsLeft, List<CarVisualManager.CarPaintHolder> colorRight)
	{
		if (colorsLeft.Count != colorRight.Count)
		{
			return true;
		}
		for (int i = 0; i < colorsLeft.Count; i++)
		{
			if (colorRight[i] == null)
			{
				return false;
			}
			Color? color = colorsLeft[i].color;
			Color? color2 = colorRight[i].color;
			Color? customSecColor = colorsLeft[i].CustomSecColor;
			Color? customSecColor2 = colorRight[i].CustomSecColor;
			if (!colorRight[i].EqualsId(colorsLeft[i]) || !color.IsColorHexMatch(color2) || !customSecColor.IsColorHexMatch(customSecColor2))
			{
				return true;
			}
		}
		return false;
	}

	private void BuyPaints(System.Action onDone)
	{
		if (cost <= 0)
		{
			if (IsColorsDiffer(m_currentSettings.colors, m_previewSettings.colors))
			{
				m_dialogActive = true;
				UIMessageBox.ShowComplexDialog(LocalizationKeys.car_profile_window_save_changes_text.Localized(), EnableColorSliders, LocalizationKeys.Cancel.Localized(), delegate
				{
					if (m_isHoodDetached)
					{
						UIPaintCategory currentCategory = GetCurrentCategory();
						if (currentCategory != null && currentCategory.paintState is BodyPaintState bodyPaintState)
						{
							bodyPaintState.SetHoodVisible(hoodVisible: true);
						}
						m_isHoodDetached = false;
					}
					EnableColorSliders();
					ApplyVisuals(m_previewSettings);
					onDone.SafeCall();
				}, LocalizationKeys.Yes.Localized(), delegate
				{
					EnableColorSliders();
					ApplyVisuals(m_currentSettings);
					onDone.SafeCall();
				}, LocalizationKeys.No.Localized());
			}
			else
			{
				EnableColorSliders();
				onDone.SafeCall();
			}
			return;
		}
		if (cost > prefs.money.coins)
		{
			UIMessageBox.ShowHint(LocalizationKeys.not_enough_money.Localized(), "", null, base.owner);
			BuyPaintsAction(buy: false, onDone);
			return;
		}
		m_dialogActive = true;
		List<DialogButtonData> list = new List<DialogButtonData>
		{
			new DialogButtonData
			{
				action = delegate
				{
					EnableColorSliders();
					if (DependencyInjector.Resolve<TransactionManager>().TryToPerformUnlockTransaction(TransactionManager.UnlockType.CarColor, 0, cost, ECurrencyType.Coins))
					{
						BuyPaintsAction(buy: true, onDone);
					}
				},
				text = LocalizationKeys.ShopBuy.Localized()
			},
			new DialogButtonData
			{
				action = delegate
				{
					EnableColorSliders();
					BuyPaintsAction(buy: false, onDone);
				},
				text = LocalizationKeys.No.Localized()
			},
			new DialogButtonData
			{
				action = EnableColorSliders,
				text = LocalizationKeys.Cancel.Localized()
			}
		};
		UIMessageBox.ShowDialog(LocalizationKeys.message_buy.LocalizedFormat(formattedCost.value), list.Last()?.action, list.ToArray());
	}

	private void EnableColorSliders()
	{
		m_dialogActive = false;
		ResetColorSliders();
	}

	private void BuyPaintsAction(bool buy, System.Action onDone)
	{
		if (buy)
		{
			SoundController.Play2DUISound(SoundSample.ColorCustomize);
			ApplyVisuals(m_previewSettings, reload: false);
		}
		else
		{
			ApplyVisuals(m_currentSettings);
		}
		ClearCart();
		onDone.SafeCall();
	}

	private void AddPartToCart(CarVisualManager.CarPaintHolder newPaint, PartType partType)
	{
		if (!m_cart.ContainsKey(partType))
		{
			m_cart.Add(partType, newPaint);
		}
	}

	private void RemovePartFromCart(PartType partType)
	{
		if (m_cart.ContainsKey(partType))
		{
			m_cart.Remove(partType);
		}
	}

	private bool ColorChanged(CarVisualManager.CarPaintHolder newPaint, PartType partType)
	{
		CarVisualManager.CustomizationFlags type = PartTypeToFlag(partType);
		CarVisualManager.CarPaintHolder paint = m_currentSettings.GetPaint(type);
		if (newPaint.id == paint.id && newPaint.indexConfig == paint.indexConfig)
		{
			return !newPaint.color.IsColorHexMatch(paint.color);
		}
		return true;
	}

	private bool IsDefaultColor(CarVisualManager.CarPaintHolder newPaint, PartType partType)
	{
		CarVisualManager.CustomizationFlags type = PartTypeToFlag(partType);
		CarVisualManager.CarPaintHolder defaultPaint = m_currentSettings.GetDefaultPaint(m_raceCar.metaInfo, type);
		if (newPaint.id == defaultPaint.id && newPaint.indexConfig == defaultPaint.indexConfig)
		{
			return newPaint.color.IsColorHexMatch(defaultPaint.color);
		}
		return false;
	}

	private void ClearCart()
	{
		m_cart.Clear();
		CalculatePaintCost();
	}

	private void CalculatePaintCost()
	{
		cost = 0;
		foreach (KeyValuePair<PartType, CarVisualManager.CarPaintHolder> item in m_cart)
		{
			if (!IsDefaultColor(item.Value, item.Key) && ColorChanged(item.Value, item.Key))
			{
				cost += PartPaintCost(item.Key);
			}
		}
	}

	private int PartPaintCost(PartType partType)
	{
		float num = 1f;
		StateType key = StateType.Body;
		switch (partType)
		{
		case PartType.Chassis:
			key = StateType.Body;
			break;
		case PartType.Ramp:
		case PartType.RollCage:
		case PartType.FrontBumper:
		case PartType.RearBumper:
		case PartType.Skirts:
		case PartType.Doors:
		case PartType.Roof:
		case PartType.SideMirrors:
		case PartType.Hood:
		case PartType.Trunk:
		case PartType.Spoiler:
		case PartType.Fenders:
		case PartType.FrontLights:
		case PartType.RearLights:
			key = StateType.Body;
			num = 1f / 15f;
			break;
		case PartType.SteeringWheelSpokes:
		case PartType.SteeringWheelCover:
		case PartType.Shifter:
		case PartType.Handbrake:
		case PartType.RightSeatBase:
		case PartType.RightSeatCushion:
		case PartType.LeftSeatBase:
		case PartType.LeftSeatCushion:
			key = StateType.Body;
			num = 0.125f;
			break;
		case PartType.FrontRims:
		case PartType.FrontSpokes:
		case PartType.RearRims:
		case PartType.RearSpokes:
			key = StateType.Suspension;
			break;
		case PartType.TireSticker1:
		case PartType.TireSticker2:
		case PartType.Arm:
		case PartType.Spring:
		case PartType.Strut:
		case PartType.Calliper:
			key = StateType.Suspension;
			num = 1f / 6f;
			break;
		case PartType.FrontLightsGlass:
		case PartType.RearLightsGlass:
			key = StateType.Glass;
			num = 0.5f;
			break;
		case PartType.Windows:
			key = StateType.Glass;
			break;
		case PartType.Smoke:
			key = StateType.Smoke;
			break;
		}
		int[] array = m_costs[key];
		int num2 = (int)m_playerCar.coinsFactor - 1;
		if (num2 < 0)
		{
			num2 = 0;
		}
		return (int)((float)array[num2] * num);
	}

	private void EnsurePartAddedToPaint()
	{
		m_multiEmptyPaint = false;
		if (!MultiselectMode)
		{
			return;
		}
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return;
		}
		UIBodyPartPaintItem selectedPart = currentCategory.GetSelectedPart();
		if (!(selectedPart == null))
		{
			List<UIBodyPartPaintItem> addedToPaintParts = currentCategory.GetAddedToPaintParts();
			if (addedToPaintParts.Count == 1 && addedToPaintParts[0].partType == selectedPart.partType)
			{
				selectedPart.addedToPaint.value = true;
				m_multiEmptyPaint = true;
			}
		}
	}

	private void UnselectPartIfNeeded()
	{
		if (!m_multiEmptyPaint)
		{
			return;
		}
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (!(currentCategory == null))
		{
			UIBodyPartPaintItem selectedPart = currentCategory.GetSelectedPart();
			if (!(selectedPart == null))
			{
				selectedPart.addedToPaint.value = false;
			}
		}
	}

	private void GoToCategorySelect()
	{
		BuyPaints(delegate
		{
			modeIndex.value = -1;
			m_currentState?.Deactivate();
			ResetCameraOnExitCategory();
			SetWheelVisible(visible: true);
		});
	}

	private void ResetCameraOnExitCategory()
	{
		CustomCameraPresets instance = Singleton<CustomCameraPresets>.instance;
		CustomizationCameraPreset customizationCameraPreset = CustomizationCameraPreset.Chassis;
		if (instance.currentPreset.stateType != customizationCameraPreset)
		{
			instance.CameraTo(customizationCameraPreset);
		}
	}

	private void SetWheelVisible(bool visible)
	{
		if (m_frontLeftWheel != null)
		{
			m_frontLeftWheel.SetActive(visible);
		}
	}

	private void ToggleMultiselect()
	{
		MultiselectMode = !MultiselectMode;
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory != null)
		{
			currentCategory.ResetMultiselect();
			if (!MultiselectMode)
			{
				UIBodyPartPaintItem selectedPart = currentCategory.GetSelectedPart();
				if (selectedPart != null)
				{
					selectedPart.addedToPaint.value = true;
				}
			}
		}
		UpdateBottomPanel();
	}

	private void NavigationSubmit()
	{
		if (modeIndex.value != -1)
		{
			return;
		}
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory != null)
		{
			if (!currentCategory.isLocked)
			{
				StateType stateType = currentCategory.stateType;
				ActivateCategory(stateType);
				UpdateBottomPanel();
			}
			else
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.locked_level_message_track.LocalizedFormat(currentCategory.locklevel.value));
			}
		}
	}

	private void NavigationCancel()
	{
		if (modeIndex.value > -1)
		{
			GoToCategorySelect();
			return;
		}
		SetWheelVisible(visible: true);
		Hide();
		base.owner.Push<UIDesignMenuContext>();
	}

	private void NavigationTogglePart()
	{
		if (MultiselectMode)
		{
			UIPaintCategory currentCategory = GetCurrentCategory();
			if (currentCategory != null)
			{
				currentCategory.TogglePart(currentCategory.selectedPartIndex);
			}
		}
	}

	private void NavigationMultiselectToggle()
	{
		ToggleMultiselect();
		GUICommonGodVoice.ShowText(MultiselectMode ? LocalizationKeys.part_multiselect_on.Localized() : LocalizationKeys.part_multiselect_off.Localized(), 5f);
	}

	private void NavigateToCategory(bool rightDirection)
	{
		if (modeIndex.value != -1)
		{
			return;
		}
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory != null && currentCategory.ableToPaint.value)
		{
			SelectCategory(currentCategory.stateType);
			m_categoryNavIteration = 0;
		}
		else if (++m_categoryNavIteration < 5)
		{
			if (rightDirection)
			{
				NavigationRight();
			}
			else
			{
				NavigationLeft();
			}
		}
	}

	private bool ShouldBeDelayed()
	{
		if (Time.unscaledTime - m_time < 0.1f)
		{
			return true;
		}
		m_time = Time.unscaledTime;
		return false;
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	public void NavigationRight()
	{
		if (modeIndex.value == -1)
		{
			Property<int> property = navCategoryIndex;
			int value = property.value + 1;
			property.value = value;
			if (navCategoryIndex.value > m_states.Length - 1)
			{
				navCategoryIndex.value = m_states.Length - 1;
			}
			NavigateToCategory(rightDirection: true);
		}
		else
		{
			if (ShouldBeDelayed())
			{
				return;
			}
			if (m_navMode == CategoryNavMode.Parts && !m_rightDelay.Delay())
			{
				m_leftDelay.Reset();
				UIPaintCategory currentCategory = GetCurrentCategory();
				if (currentCategory != null)
				{
					currentCategory.NavigateThroughParts(navigateRight: true);
					ResetColorSliders();
				}
				EnsurePartItemVisibility();
				UpdateMaterialSelector();
				MarkSelectedPart();
			}
			else if (m_navMode == CategoryNavMode.FavoriteColors && !m_rightDelay.Delay())
			{
				m_leftDelay.Reset();
				navFavColorsIndex.value++;
				if (navFavColorsIndex.value > m_colorPresets.Length - 1)
				{
					navFavColorsIndex.value = m_colorPresets.Length - 1;
				}
				SelectFavoriteColor(navFavColorsIndex.value);
			}
		}
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	public void NavigationLeft()
	{
		if (modeIndex.value == -1)
		{
			Property<int> property = navCategoryIndex;
			int value = property.value - 1;
			property.value = value;
			if (navCategoryIndex.value < 0)
			{
				navCategoryIndex.value = 0;
			}
			NavigateToCategory(rightDirection: false);
		}
		else
		{
			if (ShouldBeDelayed())
			{
				return;
			}
			if (m_navMode == CategoryNavMode.Parts && !m_leftDelay.Delay())
			{
				m_rightDelay.Reset();
				UIPaintCategory currentCategory = GetCurrentCategory();
				if (currentCategory != null)
				{
					currentCategory.NavigateThroughParts(navigateRight: false);
				}
				OnSelectedPartChanged();
			}
			else if (m_navMode == CategoryNavMode.FavoriteColors && !m_leftDelay.Delay())
			{
				m_rightDelay.Reset();
				navFavColorsIndex.value--;
				if (navFavColorsIndex.value < 0)
				{
					navFavColorsIndex.value = 0;
				}
				SelectFavoriteColor(navFavColorsIndex.value);
			}
		}
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavigationUp()
	{
		if (m_inputManager.IsActionHold(ActionType.NavDown) || modeIndex.value == -1 || m_upDelay.Delay())
		{
			return;
		}
		m_downDelay.Reset();
		if (m_navMode == CategoryNavMode.Parts)
		{
			EnsurePartAddedToPaint();
		}
		if (m_navMode == CategoryNavMode.Colors)
		{
			navColorSliderIndex.value--;
			while (navColorSliderIndex.value >= 0 && !m_colorChangers[navColorSliderIndex.value].slider.gameObject.activeInHierarchy)
			{
				navColorSliderIndex.value--;
			}
			if (navColorSliderIndex.value >= 0)
			{
				SelectColorSlider(navColorSliderIndex.value);
				return;
			}
			navColorSliderIndex.value = 0;
			if (modeIndex.value == 4)
			{
				return;
			}
		}
		int navMode = (int)m_navMode;
		navMode--;
		CategoryNavMode categoryNavMode = CategoryNavMode.Parts;
		if (modeIndex.value == 4)
		{
			categoryNavMode = CategoryNavMode.Colors;
		}
		if (navMode < (int)categoryNavMode)
		{
			navMode = (int)categoryNavMode;
		}
		m_navMode = (CategoryNavMode)navMode;
		if (m_navMode == CategoryNavMode.Parts)
		{
			UnselectPartIfNeeded();
		}
		if (!Input.GetMouseButton(0) && (!InputManager.instance.IsLastActiveJoystickAxis2D(1, out var axis) || !(axis != Vector2.zero)))
		{
			UpdateCategorySelection(navigatedUp: true);
		}
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavigationDown()
	{
		if (m_inputManager.IsActionHold(ActionType.NavUp) || modeIndex.value == -1 || m_downDelay.Delay())
		{
			return;
		}
		m_upDelay.Reset();
		if (m_navMode == CategoryNavMode.Parts)
		{
			EnsurePartAddedToPaint();
		}
		if (m_navMode == CategoryNavMode.Colors)
		{
			navColorSliderIndex.value++;
			while (navColorSliderIndex.value <= m_colorChangers.Count - 1 && !m_colorChangers[navColorSliderIndex.value].slider.gameObject.activeInHierarchy)
			{
				navColorSliderIndex.value++;
			}
			if (navColorSliderIndex.value <= m_colorChangers.Count - 1)
			{
				SelectColorSlider(navColorSliderIndex.value);
				return;
			}
			navColorSliderIndex.value = m_colorChangers.Count - 1;
		}
		int navMode = (int)m_navMode;
		navMode++;
		if (navMode > 2)
		{
			navMode = 2;
		}
		m_navMode = (CategoryNavMode)navMode;
		if (m_navMode == CategoryNavMode.Parts)
		{
			UnselectPartIfNeeded();
		}
		UpdateCategorySelection(navigatedUp: false);
	}

	public void NavigationNextIndexConfig()
	{
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return;
		}
		UIBodyPartPaintItem selectedPart = currentCategory.GetSelectedPart();
		selectedPart.NextIndexConfig();
		foreach (UIBodyPartPaintItem addedToPaintPart in currentCategory.GetAddedToPaintParts())
		{
			if (addedToPaintPart.partType != selectedPart.partType && addedToPaintPart.partType != PartType.RollCage)
			{
				addedToPaintPart.SetPartMaterial(selectedPart.materialId, selectedPart.indexConfig);
			}
		}
		UpdateMaterialSelector();
		UpdatePartsMaterial();
	}

	public void NavigationPrevIndexConfig()
	{
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return;
		}
		UIBodyPartPaintItem selectedPart = currentCategory.GetSelectedPart();
		selectedPart.PrevIndexConfig();
		foreach (UIBodyPartPaintItem addedToPaintPart in currentCategory.GetAddedToPaintParts())
		{
			if (addedToPaintPart.partType != selectedPart.partType && addedToPaintPart.partType != PartType.RollCage)
			{
				addedToPaintPart.SetPartMaterial(selectedPart.materialId, selectedPart.indexConfig);
			}
		}
		UpdateMaterialSelector();
		UpdatePartsMaterial();
	}

	public void NavigationNextMaterial()
	{
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return;
		}
		UIBodyPartPaintItem selectedPart = currentCategory.GetSelectedPart();
		selectedPart.NextMaterial();
		foreach (UIBodyPartPaintItem addedToPaintPart in currentCategory.GetAddedToPaintParts())
		{
			if (addedToPaintPart.partType != selectedPart.partType)
			{
				addedToPaintPart.SetPartMaterial(selectedPart.materialId, selectedPart.indexConfig);
			}
		}
		UpdateMaterialSelector();
		UpdatePartsMaterial();
	}

	public void NavigationPrevMaterial()
	{
		UIPaintCategory currentCategory = GetCurrentCategory();
		if (currentCategory == null)
		{
			return;
		}
		UIBodyPartPaintItem selectedPart = currentCategory.GetSelectedPart();
		selectedPart.PrevMaterial();
		foreach (UIBodyPartPaintItem addedToPaintPart in currentCategory.GetAddedToPaintParts())
		{
			if (addedToPaintPart.partType != selectedPart.partType)
			{
				addedToPaintPart.SetPartMaterial(selectedPart.materialId, selectedPart.indexConfig);
			}
		}
		UpdateMaterialSelector();
		UpdatePartsMaterial();
	}

	private void ResetHotkeysDelay()
	{
		m_upDelay.Reset();
		m_downDelay.Reset();
		m_rightDelay.Reset();
		m_leftDelay.Reset();
	}

	private static bool HexValidator(string hex)
	{
		return Regex.IsMatch(hex, "^[#]?([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$");
	}

	private void ApplyHex(string hex, Action<string> actionOk)
	{
		UIMessageBox.ShowInputDialog(LocalizationKeys.input_hex_code.Localized(), hex, null, null, LocalizationKeys.Cancel.Localized(), delegate(string hexStr)
		{
			if (!string.IsNullOrEmpty(hexStr) && HexValidator(hexStr))
			{
				actionOk.SafeCall(hexStr);
				ColorToHex();
			}
		}, LocalizationKeys.Ok.Localized());
	}

	public void HexApply()
	{
		ApplyHex(hexColor.value, delegate(string hexStr)
		{
			Color colorToSliders = HexToColor(hexStr);
			SetColorToSliders(colorToSliders);
		});
	}

	public void CustomSecColorHexApply()
	{
		ApplyHex(hexCustomSecColor.value, delegate(string hexStr)
		{
			Color customSecColorToSliders = HexToColor(hexStr);
			SetCustomSecColorToSliders(customSecColorToSliders);
		});
	}

	private void SetColorToSliders(Color color)
	{
		HSBColor hSBColor = new HSBColor(color);
		colorH.value = hSBColor.h;
		colorS.value = hSBColor.s;
		colorB.value = hSBColor.b;
	}

	private void SetCustomSecColorToSliders(Color color)
	{
		HSBColor hSBColor = new HSBColor(color);
		customSecColorH.value = hSBColor.h;
		customSecColorS.value = hSBColor.s;
		customSecColorB.value = hSBColor.b;
	}

	private void UpdateColorGradient()
	{
		colorGradient.value = new HSBColor(colorH.value, 1f, 1f, 1f).ToColor();
	}

	private void UpdateCustomSecColorGradient()
	{
		customSecGradient.value = new HSBColor(customSecColorH.value, 1f, 1f, 1f).ToColor();
	}

	private void ColorToHex()
	{
		hexColor.value = ColorToHex(colorH.value, colorS.value, colorB.value);
	}

	private void CustomSecColorToHex()
	{
		hexCustomSecColor.value = ColorToHex(customSecColorH.value, customSecColorS.value, customSecColorB.value);
	}

	private string ColorToHex(float h, float s, float b)
	{
		Color32 color = new HSBColor(h, s, b, 1f).ToColor();
		return color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2") + color.a.ToString("X2");
	}

	public static Color HexToColor(string hex)
	{
		hex = hex.Replace("0x", "");
		hex = hex.Replace("#", "");
		byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
		byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
		byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
		byte a = byte.MaxValue;
		if (hex.Length == 8)
		{
			a = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
		}
		return new Color32(r, g, b, a);
	}

	public void SendAnalytics(StateType state)
	{
		switch (state)
		{
		case StateType.Body:
			AnalyticsService.OnBodyPaintClick();
			break;
		case StateType.Interior:
			AnalyticsService.OnInteriorPaintClick();
			break;
		case StateType.Suspension:
			AnalyticsService.OnSuspensionPaintClick();
			break;
		case StateType.Glass:
			AnalyticsService.OnGlassPaintClick();
			break;
		case StateType.Smoke:
			AnalyticsService.OnSmokePaintClick();
			break;
		}
	}
}
