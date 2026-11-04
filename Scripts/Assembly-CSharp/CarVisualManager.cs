using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using CarModelSystem;
using CarProfile;
using Customization;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DB.XML;
using DI;
using GameOverlay;
using MessagePack;
using PrefsModels;
using Services.UGC.Interfaces;
using Services.UGC.Utils;
using SharingSetups;
using SimpleJSON;
using SyncMultiplayer;
using TrackLayout.Data;
using UnityEngine;
using Utils.ColorExtensions;
using Utils.VinylLayerExtensions;
using VinylSystem;

public class CarVisualManager
{
	[Flags]
	public enum CustomizationFlags : long
	{
		None = 0L,
		ChassisColor = 1L,
		RampColor = 2L,
		RollCageColor = 4L,
		FrontBumperColor = 8L,
		RearBumperColor = 0x10L,
		SkirtsColor = 0x20L,
		DoorsColor = 0x40L,
		RoofColor = 0x80L,
		SideMirrorsColor = 0x100L,
		HoodColor = 0x200L,
		TrunkColor = 0x400L,
		SpoilerColor = 0x800L,
		FendersColor = 0x1000L,
		FrontLightsColor = 0x2000L,
		RearLightsColor = 0x4000L,
		SteeringWheelSpokesColor = 0x8000L,
		SteeringWheelCoverColor = 0x10000L,
		ShifterColor = 0x20000L,
		HandbrakeColor = 0x40000L,
		RightSeatBaseColor = 0x80000L,
		RightSeatCushionColor = 0x100000L,
		LeftSeatBaseColor = 0x200000L,
		LeftSeatCushionColor = 0x400000L,
		FrontWheelRimsColor = 0x800000L,
		FrontWheelSpokesColor = 0x1000000L,
		RearWheelRimsColor = 0x2000000L,
		RearWheelSpokesColor = 0x4000000L,
		TireSticker1 = 0x8000000L,
		TireSticker2 = 0x10000000L,
		TireSticker3 = 0x20000000L,
		ArmColor = 0x40000000L,
		SpringColor = 0x80000000L,
		StrutColor = 0x100000000L,
		CalliperColor = 0x200000000L,
		FrontLightsGlassColor = 0x400000000L,
		RearLightsGlassColor = 0x800000000L,
		WindowsColor = 0x1000000000L,
		SmokeColor = 0x2000000000L,
		BodyParts = 0x4000000000L,
		Vinyls = 0x8000000000L,
		Wheels = 0x10000000000L,
		AllBodyColors = 0x7FFFL,
		AllRimColors = 0x2800000L,
		AllSpokeColors = 0x5000000L,
		AllTireStickers = 0x18000000L,
		AllSuspensionColors = 0x3C0000000L,
		AllSuspensionAndWheelsColors = 0x3DF800000L,
		AllGlassColors = 0x1C00000000L,
		AllSeatsBaseColors = 0x280000L,
		AllSeatsCushionColors = 0x500000L,
		AllInteriorColors = 0x7F8000L,
		AllColors = 0x3FDFFFFFFFL,
		AllBody = 0xFFDFFFFFFFL,
		All = 0x1FFDFFFFFFFL
	}

	public enum TyreProtectorGripType
	{
		Winter = -1,
		Worn = 0,
		LowGrip = 1,
		NormalGrip = 2,
		HighestGrip = 3
	}

	public enum TireLogos
	{
		None = -1,
		DriftHunters = 0,
		EasyMade = 1,
		TireEnemy = 2,
		Tomiris = 3,
		YoloTires = 4,
		Archer = 5,
		Faster = 6,
		Kanagawa = 7,
		Nextgen = 8,
		Nitro = 9,
		ProximaB = 10,
		Shinemoon = 11,
		Warpo = 12,
		WestWind = 13,
		Radical = 14,
		Corrupted = 15,
		QR = 16,
		Caution = 17,
		NeonKnights = 18,
		WhiteWall = 19
	}

	public enum TireLogoStripes
	{
		None = -1,
		Line = 0,
		ThinLine = 1,
		DottedLine = 2,
		DottedCircleLine = 3,
		StarsLine = 4
	}

	public class TireVisualSettings
	{
		public TyreProtectorGripType ProtectorGrip = TyreProtectorGripType.LowGrip;

		public TireLogos logo = TireLogos.None;

		public TireLogoStripes stripes = TireLogoStripes.None;

		public float visibleLogo;

		public float visibleStripes;

		public TireVisualSettings Copy()
		{
			return MemberwiseClone() as TireVisualSettings;
		}
	}

	public class TirePartMaterial : ICarPartMaterial, IDisposable
	{
		private const string CustomizationShaderName = "CarTireCustomization";

		private const string CustomizationMaterialPath = "CustomTires/CarTireCustomization";

		private static Material s_customizationMatSource;

		private static Dictionary<string, Texture> m_customizationTexCache = new Dictionary<string, Texture>();

		private TireVisualSettings m_settings;

		public TirePartMaterial(TireVisualSettings visualSettings)
		{
			m_settings = visualSettings;
		}

		public void Dispose()
		{
			m_settings = null;
		}

		private void SetTextureAsync(string path, string name, Material material)
		{
			if (!m_customizationTexCache.TryGetValue(path, out var value))
			{
				value = ResourcesWrapper.Load<Texture>(path);
				m_customizationTexCache[path] = value;
			}
			material.SetTexture(name, value);
		}

		public void ApplyCustomize(ICarPart part)
		{
			if (!(part is CarWheelTirePart { material: TirePartMaterial material } carWheelTirePart))
			{
				return;
			}
			foreach (Renderer renderer in carWheelTirePart.renderers)
			{
				Material orCreateCustomizableMaterial = GetOrCreateCustomizableMaterial(renderer);
				string text = string.Format("{0}{1}_tire", "CustomTires/TiresTextures/", (int)material.m_settings.ProtectorGrip);
				SetTextureAsync(text + "_baseColor", "_TireBaseMap", orCreateCustomizableMaterial);
				SetTextureAsync(text + "_rmo", "_TirePackMap", orCreateCustomizableMaterial);
				SetTextureAsync(text + "_normal", "_TireNormalMap", orCreateCustomizableMaterial);
				if (material.m_settings.logo != TireLogos.None)
				{
					text = string.Format("{0}{1}", "CustomTires/Logo/", (int)material.m_settings.logo);
					SetTextureAsync(text + "_baseColor", "_LogoBasemap", orCreateCustomizableMaterial);
					SetTextureAsync(text + "_normal", "_LogoNormal", orCreateCustomizableMaterial);
				}
				else
				{
					orCreateCustomizableMaterial.SetTexture("_LogoColormaskMap", null);
					orCreateCustomizableMaterial.SetTexture("_LogoBasemap", null);
					orCreateCustomizableMaterial.SetTexture("_LogoNormal", null);
				}
				if (material.m_settings.stripes != TireLogoStripes.None)
				{
					text = string.Format("{0}{1}", "CustomTires/Logo/Stripes/", (int)material.m_settings.stripes);
					SetTextureAsync(text + "_stripeBase", "_StripeBasemap", orCreateCustomizableMaterial);
					SetTextureAsync(text + "_stripeNormal", "_StripeNormal", orCreateCustomizableMaterial);
				}
				else
				{
					orCreateCustomizableMaterial.SetTexture("_StripeBasemap", null);
					orCreateCustomizableMaterial.SetTexture("_StripeNormal", null);
				}
			}
		}

		private static Material GetOrCreateCustomizableMaterial(Renderer renderer)
		{
			string text = renderer.sharedMaterial.shader.name;
			int num = text.IndexOf('/');
			if (num != -1)
			{
				text = text.Substring(num + 1);
			}
			if (text != "CarTireCustomization")
			{
				if (s_customizationMatSource == null)
				{
					s_customizationMatSource = ResourcesWrapper.Load<Material>("CustomTires/CarTireCustomization");
				}
				Material material = UnityEngine.Object.Instantiate(s_customizationMatSource);
				material.name += "(Instance)";
				renderer.sharedMaterial = material;
			}
			return renderer.sharedMaterial;
		}

		public void ApplyColor(ICarPart part, CarPaintColor color, TireStickerIndex index)
		{
			if (!(part is CarWheelTirePart carWheelTirePart))
			{
				return;
			}
			foreach (Renderer renderer in carWheelTirePart.renderers)
			{
				Material orCreateCustomizableMaterial = GetOrCreateCustomizableMaterial(renderer);
				switch (index)
				{
				case TireStickerIndex.One:
					orCreateCustomizableMaterial.SetColor("_BaseColor", color.ToColor());
					break;
				case TireStickerIndex.Two:
					orCreateCustomizableMaterial.SetColor("_SecondaryColor", color.ToColor());
					break;
				case TireStickerIndex.Stripes:
					orCreateCustomizableMaterial.SetColor("_ThirdColor", color.ToColor());
					break;
				}
			}
		}
	}

	[Serializable]
	public class CarPaintHolder : IEquatable<CarPaintHolder>
	{
		public CustomizationFlags type;

		public int? id;

		public int? indexConfig;

		public Color? color;

		public Color? specColor;

		public Color? duoColor;

		public Color? customSecColor;

		public Color? CustomSecColor
		{
			get
			{
				return customSecColor.HasValue ? new Color(customSecColor.Value.r - 1f, customSecColor.Value.g - 1f, customSecColor.Value.b - 1f) : new Color(-1f, -1f, -1f, -1f);
			}
			set
			{
				customSecColor = new Color(value.Value.r + 1f, value.Value.g + 1f, value.Value.b + 1f);
			}
		}

		public CarPaintHolder()
		{
		}

		public CarPaintHolder(CarPaintHolder other)
		{
			type = other.type;
			id = other.id;
			indexConfig = other.indexConfig;
			this.color = other.color;
			specColor = other.specColor;
			duoColor = other.duoColor;
			Color? color = other.CustomSecColor;
			Color color2 = new Color(-1f, -1f, -1f, -1f);
			CustomSecColor = ((color.HasValue && (!color.HasValue || color.GetValueOrDefault() == color2) && this.color.HasValue) ? new Color?(ColorExtensions.GetOldSecondaryChameleonColor(this.color.Value)) : other.CustomSecColor);
		}

		public CarPaintHolder(CustomizationFlags type, int? id, int? indexConfig, Color? color, Color? customSecColor = null, Color? specColor = null, Color? duoColor = null)
		{
			this.type = type;
			this.id = id;
			this.indexConfig = indexConfig;
			this.color = color;
			this.specColor = specColor;
			this.duoColor = duoColor;
			CustomSecColor = ((customSecColor.HasValue || !color.HasValue) ? customSecColor : new Color?(ColorExtensions.GetOldSecondaryChameleonColor(color.Value)));
		}

		public CarPaintHolder(CustomizationFlags type, DefaultColorsMeta meta)
		{
			this.type = type;
			id = meta.defaultMaterial;
			indexConfig = meta.defaultIndexConfig;
			color = meta.defaultColor;
			specColor = meta.defaultSpecularColor;
			duoColor = meta.defaultDuoColor;
			CustomSecColor = Color.clear;
		}

		public bool Equals(CarPaintHolder other)
		{
			if (other == null)
			{
				return false;
			}
			if (id == other.id && indexConfig == other.indexConfig && type == other.type && this.color == other.color)
			{
				Color? color = specColor;
				Color? color2 = other.specColor;
				if (color.HasValue == color2.HasValue && (!color.HasValue || color.GetValueOrDefault() == color2.GetValueOrDefault()) && duoColor == other.duoColor)
				{
					color = CustomSecColor;
					color2 = other.CustomSecColor;
					if (color.HasValue != color2.HasValue)
					{
						return false;
					}
					if (!color.HasValue)
					{
						return true;
					}
					return color.GetValueOrDefault() == color2.GetValueOrDefault();
				}
			}
			return false;
		}

		public bool EqualsId(CarPaintHolder other)
		{
			if (other == null)
			{
				return false;
			}
			if (id == other.id)
			{
				return indexConfig == other.indexConfig;
			}
			return false;
		}
	}

	public class VisualSettings
	{
		public int vinylPresetId = -1;

		public string vinylPresetLiveryId;

		public List<VinylLayer> vinylLayers = new List<VinylLayer>();

		public HashSet<int> parts = new HashSet<int>();

		public Dictionary<ulong, HashSet<int>> networkparts = new Dictionary<ulong, HashSet<int>>();

		public int rearWheels;

		public int frontWheels;

		public bool lockChangeWheels;

		[JsonIgnore]
		public int kitRearWheels;

		[JsonIgnore]
		public int kitFrontWheels;

		public bool winterTires;

		public TireVisualSettings frontTire = new TireVisualSettings
		{
			logo = TireLogos.None,
			ProtectorGrip = TyreProtectorGripType.LowGrip,
			visibleLogo = 0f,
			visibleStripes = 0f
		};

		public TireVisualSettings rearTire = new TireVisualSettings
		{
			logo = TireLogos.None,
			ProtectorGrip = TyreProtectorGripType.LowGrip,
			visibleLogo = 0f,
			visibleStripes = 0f
		};

		public List<CarPaintHolder> colors = new List<CarPaintHolder>();

		[JsonIgnore]
		public bool oldColorsFormat;

		[JsonIgnore]
		public Color? bodyColor;

		[JsonIgnore]
		public Color? bodySpecularColor;

		[JsonIgnore]
		public Color? bodyDuoColor;

		[JsonIgnore]
		public int? wheelRimColorFrontId;

		[JsonIgnore]
		public Color? wheelRimFrontColor;

		[JsonIgnore]
		public Color? wheelRimFrontSpecColor;

		[JsonIgnore]
		public Color? wheelRimFrontDuoColor;

		[JsonIgnore]
		public int? wheelRimColorRearId;

		[JsonIgnore]
		public Color? wheelRimRearColor;

		[JsonIgnore]
		public Color? wheelRimRearSpecColor;

		[JsonIgnore]
		public Color? wheelRimRearDuoColor;

		[JsonIgnore]
		public int? wheelSpokeColorFrontId;

		[JsonIgnore]
		public Color? wheelSpokeFrontColor;

		[JsonIgnore]
		public Color? wheelSpokeFrontSpecColor;

		[JsonIgnore]
		public Color? wheelSpokeFrontDuoColor;

		[JsonIgnore]
		public int? wheelSpokeColorRearId;

		[JsonIgnore]
		public Color? wheelSpokeRearColor;

		[JsonIgnore]
		public Color? wheelSpokeRearSpecColor;

		[JsonIgnore]
		public Color? wheelSpokeRearDuoColor;

		[JsonIgnore]
		public Color? smokeColor;

		[JsonIgnore]
		public Color? glassColor;

		[JsonIgnore]
		public int? colorId;

		[JsonIgnore]
		public int? smokeColorId;

		[JsonIgnore]
		public int? glassColorId;

		private GamePrefs m_prefs;

		public void Reset(PlayerCar car = null)
		{
			vinylLayers.Clear();
			networkparts.Clear();
			ResetColors(car);
			vinylPresetLiveryId = null;
			vinylPresetId = -1;
		}

		public void RemoveExternalParts(RaceCar car)
		{
			CarPartDatabase.TryResetIfExistExternalParts(car.carId, parts);
			CarPart partById = CarPartDatabase.GetPartById(frontWheels);
			CarPart partById2 = CarPartDatabase.GetPartById(rearWheels);
			if (partById.external)
			{
				frontWheels = car.metaInfo.defaultWheelId;
			}
			if (partById2.external)
			{
				rearWheels = car.metaInfo.defaultWheelId;
			}
		}

		public override bool Equals(object obj)
		{
			if (!(obj is VisualSettings visualSettings))
			{
				return false;
			}
			int num;
			if (visualSettings.colors.Count <= 0 && colors.Count <= 0)
			{
				Color? color = bodyColor;
				Color? color2 = visualSettings.bodyColor;
				if (color.HasValue == color2.HasValue && (!color.HasValue || color.GetValueOrDefault() == color2.GetValueOrDefault()) && bodyDuoColor == visualSettings.bodyDuoColor)
				{
					color = bodySpecularColor;
					color2 = visualSettings.bodySpecularColor;
					if (color.HasValue == color2.HasValue && (!color.HasValue || color.GetValueOrDefault() == color2.GetValueOrDefault()) && colorId == visualSettings.colorId)
					{
						color2 = glassColor;
						color = visualSettings.glassColor;
						if (color2.HasValue == color.HasValue && (!color2.HasValue || color2.GetValueOrDefault() == color.GetValueOrDefault()) && wheelRimColorFrontId == visualSettings.wheelRimColorFrontId)
						{
							color = wheelRimFrontColor;
							color2 = visualSettings.wheelRimFrontColor;
							if (color.HasValue == color2.HasValue && (!color.HasValue || color.GetValueOrDefault() == color2.GetValueOrDefault()) && wheelRimFrontSpecColor == visualSettings.wheelRimFrontSpecColor)
							{
								color = wheelRimFrontDuoColor;
								color2 = visualSettings.wheelRimFrontDuoColor;
								if (color.HasValue == color2.HasValue && (!color.HasValue || color.GetValueOrDefault() == color2.GetValueOrDefault()) && wheelRimColorRearId == visualSettings.wheelRimColorRearId)
								{
									color2 = wheelRimRearColor;
									color = visualSettings.wheelRimRearColor;
									if (color2.HasValue == color.HasValue && (!color2.HasValue || color2.GetValueOrDefault() == color.GetValueOrDefault()) && wheelRimRearSpecColor == visualSettings.wheelRimRearSpecColor)
									{
										color2 = wheelRimRearDuoColor;
										color = visualSettings.wheelRimRearDuoColor;
										if (color2.HasValue == color.HasValue && (!color2.HasValue || color2.GetValueOrDefault() == color.GetValueOrDefault()) && wheelSpokeColorFrontId == visualSettings.wheelSpokeColorFrontId)
										{
											color = wheelSpokeFrontColor;
											color2 = visualSettings.wheelSpokeFrontColor;
											if (color.HasValue == color2.HasValue && (!color.HasValue || color.GetValueOrDefault() == color2.GetValueOrDefault()) && wheelSpokeFrontSpecColor == visualSettings.wheelSpokeFrontSpecColor)
											{
												color = wheelSpokeFrontDuoColor;
												color2 = visualSettings.wheelSpokeFrontDuoColor;
												if (color.HasValue == color2.HasValue && (!color.HasValue || color.GetValueOrDefault() == color2.GetValueOrDefault()) && wheelSpokeColorRearId == visualSettings.wheelSpokeColorRearId)
												{
													color2 = wheelSpokeRearColor;
													color = visualSettings.wheelSpokeRearColor;
													if (color2.HasValue == color.HasValue && (!color2.HasValue || color2.GetValueOrDefault() == color.GetValueOrDefault()) && wheelSpokeRearSpecColor == visualSettings.wheelSpokeRearSpecColor)
													{
														color2 = wheelSpokeRearDuoColor;
														color = visualSettings.wheelSpokeRearDuoColor;
														if (color2.HasValue == color.HasValue && (!color2.HasValue || color2.GetValueOrDefault() == color.GetValueOrDefault()) && smokeColor == visualSettings.smokeColor)
														{
															num = ((vinylPresetLiveryId == visualSettings.vinylPresetLiveryId) ? 1 : 0);
															goto IL_0601;
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
				num = 0;
				goto IL_0601;
			}
			bool flag = colors.Count == visualSettings.colors.Count;
			if (flag)
			{
				foreach (CarPaintHolder color3 in colors)
				{
					if (!visualSettings.colors.Contains(color3))
					{
						flag = false;
						break;
					}
				}
			}
			goto IL_0664;
			IL_0601:
			flag = (byte)num != 0;
			goto IL_0664;
			IL_0664:
			bool num2 = vinylLayers.Count == visualSettings.vinylLayers.Count;
			bool flag2 = parts.Count == visualSettings.parts.Count;
			bool flag3 = networkparts.Count == visualSettings.networkparts.Count;
			if (!num2 || !flag2 || !flag3 || !flag)
			{
				return false;
			}
			for (int i = 0; i < vinylLayers.Count; i++)
			{
				if (!vinylLayers[i].LayerEquals(visualSettings.vinylLayers[i]))
				{
					return false;
				}
			}
			foreach (int part in parts)
			{
				if (!visualSettings.parts.Contains(part))
				{
					return false;
				}
			}
			foreach (KeyValuePair<ulong, HashSet<int>> networkpart in networkparts)
			{
				if (!visualSettings.networkparts.ContainsKey(networkpart.Key))
				{
					return false;
				}
				if (!visualSettings.networkparts.ContainsValue(networkpart.Value))
				{
					return false;
				}
			}
			if (rearWheels == visualSettings.rearWheels && frontWheels == visualSettings.frontWheels && lockChangeWheels == visualSettings.lockChangeWheels && winterTires == visualSettings.winterTires && frontTire == visualSettings.frontTire)
			{
				return rearTire == visualSettings.rearTire;
			}
			return false;
		}

		public override int GetHashCode()
		{
			HashCode hashCode = default(HashCode);
			hashCode.Add(colors.Count);
			if (colors.Count <= 0)
			{
				hashCode.Add(bodyColor);
				hashCode.Add(bodyDuoColor);
				hashCode.Add(bodySpecularColor);
				hashCode.Add(colorId);
				hashCode.Add(glassColor);
				hashCode.Add(wheelRimColorFrontId);
				hashCode.Add(wheelRimFrontColor);
				hashCode.Add(wheelRimFrontSpecColor);
				hashCode.Add(wheelRimFrontDuoColor);
				hashCode.Add(wheelRimColorRearId);
				hashCode.Add(wheelRimRearColor);
				hashCode.Add(wheelRimRearSpecColor);
				hashCode.Add(wheelRimRearDuoColor);
				hashCode.Add(wheelSpokeColorFrontId);
				hashCode.Add(wheelSpokeFrontColor);
				hashCode.Add(wheelSpokeFrontSpecColor);
				hashCode.Add(wheelSpokeFrontDuoColor);
				hashCode.Add(wheelSpokeColorRearId);
				hashCode.Add(wheelSpokeRearColor);
				hashCode.Add(wheelSpokeRearSpecColor);
				hashCode.Add(wheelSpokeRearDuoColor);
				hashCode.Add(smokeColor);
				hashCode.Add(vinylPresetLiveryId);
			}
			else
			{
				foreach (CarPaintHolder color in colors)
				{
					hashCode.Add(color);
				}
			}
			hashCode.Add(vinylLayers.Count);
			foreach (VinylLayer vinylLayer in vinylLayers)
			{
				hashCode.Add(vinylLayer.GetHashCode());
			}
			hashCode.Add(parts.Count);
			foreach (int part in parts)
			{
				hashCode.Add(part);
			}
			hashCode.Add(networkparts.Count);
			foreach (var (value, hashSet2) in networkparts)
			{
				hashCode.Add(value);
				foreach (int item in hashSet2)
				{
					hashCode.Add(item);
				}
			}
			hashCode.Add(rearWheels);
			hashCode.Add(frontWheels);
			hashCode.Add(lockChangeWheels);
			hashCode.Add(winterTires);
			hashCode.Add(frontTire);
			hashCode.Add(rearTire);
			return hashCode.ToHashCode();
		}

		public void ResetColors(PlayerCar car = null, bool oldFormat = false)
		{
			if (car != null)
			{
				colors.Clear();
				if (!oldFormat)
				{
					colors.Add(new CarPaintHolder(CustomizationFlags.ChassisColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.RampColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.RollCageColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.FrontBumperColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.RearBumperColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.SkirtsColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.DoorsColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.RoofColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.SideMirrorsColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.HoodColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.TrunkColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.SpoilerColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.FendersColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.FrontLightsColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.RearLightsColor, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.FrontWheelRimsColor, car.defaultWheelsColors.defaultFrontRimColors));
					colors.Add(new CarPaintHolder(CustomizationFlags.FrontWheelSpokesColor, car.defaultWheelsColors.defaultFrontSpokeColors));
					colors.Add(new CarPaintHolder(CustomizationFlags.RearWheelRimsColor, car.defaultWheelsColors.defaultRearRimColors));
					colors.Add(new CarPaintHolder(CustomizationFlags.RearWheelSpokesColor, car.defaultWheelsColors.defaultRearSpokeColors));
					colors.Add(new CarPaintHolder(CustomizationFlags.WindowsColor, car.defaultGlassColorId, car.defaultIndexConfig, DEFAULT_GLASS_COLOR, car.defaultCustomSecColor));
					colors.Add(new CarPaintHolder(CustomizationFlags.SmokeColor, car.defaultSmokeColorId, car.defaultIndexConfig, DEFAULT_SMOKE_COLOR, car.defaultCustomSecColor));
				}
				colors.Add(new CarPaintHolder(CustomizationFlags.SteeringWheelSpokesColor, 6, car.defaultIndexConfig, DEFAULT_GENERIC_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.SteeringWheelCoverColor, 6, car.defaultIndexConfig, DEFAULT_INTERIOR_ALCANTAR_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.ShifterColor, 6, car.defaultIndexConfig, DEFAULT_GENERIC_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.HandbrakeColor, 6, car.defaultIndexConfig, DEFAULT_GENERIC_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.RightSeatBaseColor, 6, car.defaultIndexConfig, DEFAULT_INTERIOR_ALCANTAR_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.RightSeatCushionColor, 6, car.defaultIndexConfig, DEFAULT_INTERIOR_ALCANTAR_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.LeftSeatBaseColor, 6, car.defaultIndexConfig, DEFAULT_INTERIOR_ALCANTAR_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.LeftSeatCushionColor, 6, car.defaultIndexConfig, DEFAULT_INTERIOR_ALCANTAR_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.TireSticker1, 1, car.defaultIndexConfig, DEFAULT_TIRE_STICKER_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.TireSticker2, 1, car.defaultIndexConfig, DEFAULT_TIRE_STICKER_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.ArmColor, 1, car.defaultIndexConfig, DEFAULT_GENERIC_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.SpringColor, 1, car.defaultIndexConfig, DEFAULT_GENERIC_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.StrutColor, 1, car.defaultIndexConfig, DEFAULT_GENERIC_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.CalliperColor, 1, car.defaultIndexConfig, DEFAULT_GENERIC_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.FrontLightsGlassColor, car.defaultGlassColorId, car.defaultIndexConfig, DEFAULT_GLASS_COLOR, car.defaultCustomSecColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.RearLightsGlassColor, car.defaultGlassColorId, car.defaultIndexConfig, DEFAULT_GLASS_COLOR, car.defaultCustomSecColor));
			}
			else
			{
				colors.Clear();
				parts.Clear();
			}
		}

		public void TransformToNewFormat(PlayerCar car)
		{
			if (oldColorsFormat)
			{
				ResetColors(car, oldFormat: true);
				colors.Add(new CarPaintHolder(CustomizationFlags.ChassisColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.RampColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.RollCageColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.FrontBumperColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.RearBumperColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.SkirtsColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.DoorsColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.RoofColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.SideMirrorsColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.HoodColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.TrunkColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.SpoilerColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.FendersColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.FrontLightsColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.RearLightsColor, colorId, car.defaultIndexConfig, bodyColor, car.defaultCustomSecColor, bodySpecularColor, bodyDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.FrontWheelRimsColor, wheelRimColorFrontId, car.defaultIndexConfig, wheelRimFrontColor, car.defaultCustomSecColor, wheelRimFrontSpecColor, wheelRimFrontDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.FrontWheelSpokesColor, wheelSpokeColorFrontId, car.defaultIndexConfig, wheelSpokeFrontColor, car.defaultCustomSecColor, wheelSpokeFrontSpecColor, wheelSpokeFrontDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.RearWheelRimsColor, wheelRimColorRearId, car.defaultIndexConfig, wheelRimRearColor, car.defaultCustomSecColor, wheelRimRearSpecColor, wheelRimRearDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.RearWheelSpokesColor, wheelSpokeColorRearId, car.defaultIndexConfig, wheelSpokeRearColor, car.defaultCustomSecColor, wheelSpokeRearSpecColor, wheelSpokeRearDuoColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.WindowsColor, glassColorId, car.defaultIndexConfig, car.defaultCustomSecColor, glassColor));
				colors.Add(new CarPaintHolder(CustomizationFlags.SmokeColor, smokeColorId, car.defaultIndexConfig, car.defaultCustomSecColor, smokeColor));
			}
		}

		public VisualSettings Copy()
		{
			VisualSettings visualSettings = MemberwiseClone() as VisualSettings;
			visualSettings.parts = new HashSet<int>(parts);
			visualSettings.vinylLayers = new List<VinylLayer>();
			if (vinylLayers != null)
			{
				visualSettings.vinylLayers.AddRange(vinylLayers);
			}
			visualSettings.colors = new List<CarPaintHolder>(colors.Count);
			foreach (CarPaintHolder color in colors)
			{
				visualSettings.colors.Add(new CarPaintHolder(color));
			}
			return visualSettings;
		}

		public VisualSettings MutateToNetworkParts()
		{
			networkparts.Clear();
			List<CarPart> list = CarPartDatabase.ConvertToPartList(parts);
			HashSet<int> value = null;
			foreach (CarPart item in list)
			{
				if (item.external)
				{
					parts.Remove(item.id);
					if (!networkparts.TryGetValue(item.packetid, out value))
					{
						value = new HashSet<int>();
						networkparts.Add(item.packetid, value);
					}
					value.Add(item.nativeid);
				}
			}
			if (Overlay.instance.IsUserGeneratedContentRestricted || Overlay.instance.IsUserShareContentRestricted)
			{
				vinylLayers.Clear();
			}
			return this;
		}

		public VisualSettings MutateToLocalParts()
		{
			List<ulong> list = new List<ulong>();
			foreach (KeyValuePair<ulong, HashSet<int>> networkpart in networkparts)
			{
				if (CarPartDatabase.externalDatabases.TryGetValue(networkpart.Key, out var value))
				{
					if (!CanApplyWorkshopParts() && !value.isLocal)
					{
						continue;
					}
					list.Add(networkpart.Key);
					foreach (int item in networkpart.Value)
					{
						if (value.nativeParts.TryGetValue(item, out var value2))
						{
							parts.Add(value2.id);
						}
					}
				}
				else if (CanApplyWorkshopParts())
				{
					UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Cars, IUGCManager.ServiceProvider.Steamworkshop).RequestPackage(networkpart.Key, null, null, needSubscribe: false);
				}
			}
			foreach (ulong item2 in list)
			{
				networkparts.Remove(item2);
			}
			return this;
		}

		public CarPaintHolder GetPaint(CustomizationFlags type)
		{
			foreach (CarPart item in CarPartDatabase.ConvertToPartList(parts))
			{
				if (item == null || item.colorOverrides == null || item.colorOverrides.Count <= 0)
				{
					continue;
				}
				foreach (CarPartColorOverride colorOverride in item.colorOverrides)
				{
					if (colorOverride.cachedPaint != null && colorOverride.cachedPaint.type == type)
					{
						return colorOverride.cachedPaint;
					}
				}
			}
			foreach (CarPaintHolder color in colors)
			{
				if (color.type == type)
				{
					return color;
				}
			}
			return null;
		}

		public CarPaintHolder GetDefaultPaint(PlayerCar car, CustomizationFlags type)
		{
			switch (type)
			{
			case CustomizationFlags.ChassisColor:
			case CustomizationFlags.RampColor:
			case CustomizationFlags.RollCageColor:
			case CustomizationFlags.FrontBumperColor:
			case CustomizationFlags.RearBumperColor:
			case CustomizationFlags.SkirtsColor:
			case CustomizationFlags.DoorsColor:
			case CustomizationFlags.RoofColor:
			case CustomizationFlags.SideMirrorsColor:
			case CustomizationFlags.HoodColor:
			case CustomizationFlags.TrunkColor:
			case CustomizationFlags.SpoilerColor:
			case CustomizationFlags.FendersColor:
			case CustomizationFlags.FrontLightsColor:
			case CustomizationFlags.RearLightsColor:
				return new CarPaintHolder(CustomizationFlags.None, car.defaultColorId, car.defaultIndexConfig, car.defaultColor, car.defaultCustomSecColor, car.defaultSpecularColor, car.defaultDuoColor);
			case CustomizationFlags.SteeringWheelCoverColor:
			case CustomizationFlags.RightSeatBaseColor:
			case CustomizationFlags.RightSeatCushionColor:
			case CustomizationFlags.LeftSeatBaseColor:
			case CustomizationFlags.LeftSeatCushionColor:
				return new CarPaintHolder(CustomizationFlags.None, 6, car.defaultIndexConfig, DEFAULT_INTERIOR_ALCANTAR_COLOR);
			case CustomizationFlags.FrontWheelRimsColor:
				return new CarPaintHolder(CustomizationFlags.None, car.defaultWheelsColors.defaultFrontRimColors);
			case CustomizationFlags.FrontWheelSpokesColor:
				return new CarPaintHolder(CustomizationFlags.None, car.defaultWheelsColors.defaultFrontSpokeColors);
			case CustomizationFlags.RearWheelRimsColor:
				return new CarPaintHolder(CustomizationFlags.None, car.defaultWheelsColors.defaultRearRimColors);
			case CustomizationFlags.RearWheelSpokesColor:
				return new CarPaintHolder(CustomizationFlags.None, car.defaultWheelsColors.defaultRearSpokeColors);
			case CustomizationFlags.TireSticker1:
			case CustomizationFlags.TireSticker2:
			case CustomizationFlags.TireSticker3:
				return new CarPaintHolder(CustomizationFlags.None, 1, car.defaultIndexConfig, DEFAULT_TIRE_STICKER_COLOR, car.defaultCustomSecColor);
			case CustomizationFlags.FrontLightsGlassColor:
			case CustomizationFlags.RearLightsGlassColor:
			case CustomizationFlags.WindowsColor:
				return new CarPaintHolder(CustomizationFlags.None, car.defaultGlassColorId, car.defaultIndexConfig, DEFAULT_GLASS_COLOR);
			case CustomizationFlags.SmokeColor:
				return new CarPaintHolder(CustomizationFlags.None, car.defaultSmokeColorId, car.defaultIndexConfig, DEFAULT_SMOKE_COLOR);
			case CustomizationFlags.SteeringWheelSpokesColor:
			case CustomizationFlags.ShifterColor:
			case CustomizationFlags.HandbrakeColor:
			case CustomizationFlags.ArmColor:
			case CustomizationFlags.SpringColor:
			case CustomizationFlags.StrutColor:
			case CustomizationFlags.CalliperColor:
				return new CarPaintHolder(CustomizationFlags.None, 6, car.defaultIndexConfig, DEFAULT_GENERIC_COLOR);
			default:
				return new CarPaintHolder(CustomizationFlags.None, 6, car.defaultIndexConfig, DEFAULT_GENERIC_COLOR);
			}
		}

		private void CheckColors(PlayerCar car, BaseModel model)
		{
			CustomizationFlags[] array = (CustomizationFlags[])Enum.GetValues(typeof(CustomizationFlags));
			foreach (CustomizationFlags customizationFlags in array)
			{
				if (customizationFlags == CustomizationFlags.None)
				{
					continue;
				}
				if (customizationFlags > CustomizationFlags.SmokeColor)
				{
					break;
				}
				CarPaintHolder paint = GetPaint(customizationFlags);
				if (paint == null)
				{
					continue;
				}
				if (customizationFlags >= CustomizationFlags.ChassisColor && customizationFlags <= CustomizationFlags.RearLightsColor)
				{
					if (paint.id.HasValue && model.GetById<CarColorMeta>(paint.id.Value) == null)
					{
						paint.id = car.defaultColorId;
					}
					if (paint.duoColor.HasValue && !paint.duoColor.Value.IsValid())
					{
						paint.duoColor = car.defaultDuoColor;
					}
					if (paint.specColor.HasValue && !paint.specColor.Value.IsValid())
					{
						paint.specColor = car.defaultSpecularColor;
					}
				}
				else if (customizationFlags == CustomizationFlags.SteeringWheelSpokesColor || customizationFlags == CustomizationFlags.ShifterColor || customizationFlags == CustomizationFlags.HandbrakeColor)
				{
					if (paint.color.HasValue && !paint.color.Value.IsValid())
					{
						paint.color = DEFAULT_GENERIC_COLOR;
					}
				}
				else if (customizationFlags == CustomizationFlags.SteeringWheelCoverColor || customizationFlags == CustomizationFlags.RightSeatBaseColor || customizationFlags == CustomizationFlags.RightSeatCushionColor || customizationFlags == CustomizationFlags.LeftSeatBaseColor || customizationFlags == CustomizationFlags.LeftSeatCushionColor)
				{
					if (paint.color.HasValue && !paint.color.Value.IsValid())
					{
						paint.color = DEFAULT_INTERIOR_ALCANTAR_COLOR;
					}
				}
				else if (customizationFlags >= CustomizationFlags.FrontWheelRimsColor && customizationFlags <= CustomizationFlags.RearWheelSpokesColor)
				{
					if (customizationFlags == CustomizationFlags.FrontWheelRimsColor || customizationFlags == CustomizationFlags.RearWheelRimsColor)
					{
						if (paint.id.HasValue && model.GetById<WheelRimColorMeta>(paint.id.Value) == null)
						{
							paint.id = 1;
						}
					}
					else if (paint.id.HasValue && model.GetById<WheelSpokeColorMeta>(paint.id.Value) == null)
					{
						paint.id = 1;
					}
				}
				else if (customizationFlags >= CustomizationFlags.TireSticker1 && customizationFlags <= CustomizationFlags.TireSticker3)
				{
					if (paint.color.HasValue && !paint.color.Value.IsValid())
					{
						paint.color = DEFAULT_TIRE_STICKER_COLOR;
					}
				}
				else if (customizationFlags >= CustomizationFlags.ArmColor && customizationFlags <= CustomizationFlags.CalliperColor)
				{
					if (paint.color.HasValue && !paint.color.Value.IsValid())
					{
						paint.color = DEFAULT_GENERIC_COLOR;
					}
				}
				else if (customizationFlags >= CustomizationFlags.FrontLightsGlassColor && customizationFlags <= CustomizationFlags.WindowsColor)
				{
					if (paint.id.HasValue && model.GetById<GlassColorMeta>(paint.id.Value) == null)
					{
						paint.id = 1;
					}
				}
				else if (customizationFlags == CustomizationFlags.SmokeColor && paint.id.HasValue && model.GetById<SmokeColorMeta>(paint.id.Value) == null)
				{
					paint.id = 1;
				}
			}
		}

		public bool IsCorrectForCar(PlayerCar car, BaseModel model)
		{
			if (car == null || model == null)
			{
				return false;
			}
			CheckColors(car, model);
			if (!CarPartDatabase.IsValid(car.id, parts))
			{
				parts.Clear();
				return true;
			}
			return true;
		}

		private bool CanApplyWorkshopParts()
		{
			if (!m_prefs)
			{
				m_prefs = DependencyInjector.Resolve<GamePrefs>();
			}
			return m_prefs.globals.workshopNetworkCars;
		}
	}

	[Serializable]
	public class MaterialParameters : ICloneable, ISerializationCallbackReceiver
	{
		[SerializeField]
		private Color32NullableSupport m_color = new Color32NullableSupport();

		[SerializeField]
		private UniversalShaderParameter[] m_shaderParams;

		[XmlIgnore]
		public Color32? color;

		[XmlIgnore]
		public Texture texture;

		[XmlAttribute]
		[DefaultValue("")]
		public string mainTexturePath;

		public List<ShaderParameter> shaderParams = new List<ShaderParameter>();

		[XmlAttribute]
		public string[] shaderKeywords = new string[0];

		[XmlAttribute("color")]
		public string xmlColor
		{
			get
			{
				return XmlTools.ColorToString(color);
			}
			set
			{
				color = XmlTools.ParseColor(value);
			}
		}

		public object Clone()
		{
			MaterialParameters materialParameters = new MaterialParameters();
			if (color.HasValue)
			{
				materialParameters.color = color.Value;
			}
			if (!string.IsNullOrEmpty(mainTexturePath))
			{
				materialParameters.mainTexturePath = string.Copy(mainTexturePath);
			}
			materialParameters.shaderParams = new List<ShaderParameter>();
			foreach (ShaderParameter shaderParam in shaderParams)
			{
				materialParameters.shaderParams.Add(shaderParam.MemberwiseClone() as ShaderParameter);
			}
			materialParameters.shaderKeywords = new string[shaderKeywords.Length];
			shaderKeywords.CopyTo(materialParameters.shaderKeywords, 0);
			return materialParameters;
		}

		public void OnBeforeSerialize()
		{
			m_color.InitFrom(color);
			Array.Resize(ref m_shaderParams, shaderParams.Count);
			for (int i = 0; i < shaderParams.Count; i++)
			{
				if (m_shaderParams[i] == null)
				{
					m_shaderParams[i] = new UniversalShaderParameter();
				}
				m_shaderParams[i].InitFrom(shaderParams[i]);
			}
		}

		public void OnAfterDeserialize()
		{
			color = m_color.GetNullable();
			shaderParams.Clear();
			if (m_shaderParams != null)
			{
				for (int i = 0; i < m_shaderParams.Length; i++)
				{
					shaderParams.Add(m_shaderParams[i].GetTypedShaderParameter());
				}
			}
		}
	}

	public enum CustomizationType
	{
		BodyColor = 0,
		RimColor = 1,
		SpokesColor = 2,
		SmokeColor = 3,
		GlassColor = 4
	}

	public static readonly TypeID BlobShadowTypeId = new TypeID("blobshadow");

	public static readonly TypeID WingTypeId = new TypeID("wing");

	public static readonly TypeID CamerasTypeId = new TypeID("cameras");

	public static readonly TypeID ChassisId = new TypeID("chassis");

	public static readonly TypeID CageId = new TypeID("cage");

	public static readonly TypeID BumperFrontId = new TypeID("bumperfront");

	public static readonly TypeID BumperRearId = new TypeID("bumperrear");

	public static readonly TypeID SkirtsId = new TypeID("skirts");

	public static readonly TypeID DoorsId = new TypeID("doors");

	public static readonly TypeID RoofId = new TypeID("roof");

	public static readonly TypeID MirrorsId = new TypeID("mirrors");

	public static readonly TypeID BonnetId = new TypeID("bonnet");

	public static readonly TypeID TrunkId = new TypeID("trunk");

	public static readonly TypeID SpoilerId = new TypeID("spoiler");

	public static readonly TypeID LightsFrontId = new TypeID("lightsfront");

	public static readonly TypeID LightsRearId = new TypeID("lightsrear");

	public static readonly TypeID TireId = new TypeID("tire");

	public static readonly TypeID RimId = new TypeID("rim");

	public static readonly TypeID SpokeId = new TypeID("spoke");

	public static readonly TypeID BrakeRotorId = new TypeID("brakerotor");

	public static readonly TypeID BrakeCaliperId = new TypeID("brakecaliper");

	public static readonly TypeID SuspensionId = new TypeID("suspension");

	public static readonly TypeID SeatLeftId = new TypeID("seatleft");

	public static readonly TypeID SeatRightId = new TypeID("seatright");

	public static readonly TypeID SteeringWheelId = new TypeID("steeringwheel");

	public static readonly TypeID HandbrakeId = new TypeID("handbrake");

	public static readonly TypeID ShifterId = new TypeID("shifter");

	public static readonly TypeID InteriorId = new TypeID("interior");

	public static readonly TypeID GlassId = new TypeID("glass");

	public static readonly TypeID RampId = new TypeID("ramp");

	public static readonly TypeID GlassLightsId = new TypeID("glass_lights");

	public static readonly TypeID Alcantar1 = new TypeID("alcantar1");

	public static readonly TypeID Alcantar2 = new TypeID("alcantar2");

	public static readonly TypeID InterorParts = new TypeID("interior_parts");

	public const string BodyColorMaterialNamePart = " - Paint";

	public const string GlassColorMaterialNamePart = " - Glass ";

	private const string ProfilesFolder = "Profiles/";

	public static readonly Color DEFAULT_GENERIC_COLOR = Color.white;

	public static readonly Color DEFAULT_SMOKE_COLOR = Color.white;

	public static readonly Color DEFAULT_GLASS_COLOR = Color.white;

	public static readonly Color DEFAULT_INTERIOR_ALCANTAR_COLOR = new Color32(55, 55, 55, byte.MaxValue);

	public static readonly Color DEFAULT_TIRE_STICKER_COLOR = Color.white;

	private static int[] m_carIdsUnableToPaint = new int[2] { 108, 125 };

	[Dependency]
	private BaseModel m_model;

	[Dependency]
	private GamePrefs m_prefs;

	private static CarExternalPartsPostProcessor m_carExternalPartsPostProcessor = new CarExternalPartsPostProcessor();

	public static string CrookedPartType(CarPart part)
	{
		if (part.external || !part.type.Equals("wing", StringComparison.OrdinalIgnoreCase))
		{
			return part.type;
		}
		return "chassis";
	}

	public static string FixPartPath(string path, string type)
	{
		if (type.Equals("shifter", StringComparison.OrdinalIgnoreCase) || !path.StartsWith(type, StringComparison.OrdinalIgnoreCase))
		{
			return path;
		}
		return path.Remove(0, type.Length + 1);
	}

	private static string FixRimPath(string path)
	{
		if (!path.StartsWith("Prefabs/Rims/", StringComparison.OrdinalIgnoreCase))
		{
			return path;
		}
		return path.Remove(0, "Prefabs/Rims/".Length);
	}

	public static CarPaintType ConvertToCarPaintType(int id)
	{
		return id switch
		{
			1 => CarPaintType.Metallic, 
			2 => CarPaintType.Matte, 
			3 => CarPaintType.Chrome, 
			4 => CarPaintType.Pearlescent, 
			5 => CarPaintType.Candy, 
			6 => CarPaintType.Solid, 
			7 => CarPaintType.Satin, 
			_ => CarPaintType.Solid, 
		};
	}

	public static CarPaintType MaterialIdToCarPaintType(int id)
	{
		return id switch
		{
			1 => CarPaintType.Metallic, 
			2 => CarPaintType.Matte, 
			3 => CarPaintType.Chrome, 
			4 => CarPaintType.Pearlescent, 
			5 => CarPaintType.Candy, 
			6 => CarPaintType.Solid, 
			7 => CarPaintType.Satin, 
			_ => CarPaintType.Solid, 
		};
	}

	public CarVisualManager()
	{
		DependencyInjector.InjectObjectsInto(this);
	}

	public void SaveVisualSettingsForCar(int carId, VisualSettings settings)
	{
		m_prefs.carSettings.SetVisualForCar(carId, settings);
	}

	public void SetVisualSettingsForCar(int carId, VisualSettings settings)
	{
		m_prefs.carSettings.SetVisualWithoutSave(carId, settings);
	}

	public void ApplyVisual(CustomizationFlags applyFlags, RaceCar car, VisualSettings visualSettings, ProfilesInfo.Profile profile = null, bool isLocalPlayerCar = true, bool forceProfileApply = false)
	{
		ApplyVisualInternal(applyFlags, car, visualSettings, profile, isLocalPlayerCar, forceProfileApply).GetAwaiter().GetResult();
	}

	public async UniTask ApplyVisualAsync(CustomizationFlags applyFlags, RaceCar car, VisualSettings visualSettings, ProfilesInfo.Profile profile = null, bool isLocalPlayerCar = true, bool forceProfileApply = false)
	{
		await ApplyVisualInternal(applyFlags, car, visualSettings, profile, isLocalPlayerCar, forceProfileApply, isAsync: true);
	}

	private async UniTask ApplyVisualInternal(CustomizationFlags applyFlags, RaceCar car, VisualSettings visualSettings, ProfilesInfo.Profile profile = null, bool isLocalPlayerCar = true, bool forceProfileApply = false, bool isAsync = false)
	{
		if (applyFlags == CustomizationFlags.None)
		{
			return;
		}
		if (car == null || visualSettings == null)
		{
			GameLog.Print(AlertLevel.Error, "CarVisualManager", "Null car or settings");
			return;
		}
		bool needAsyncLoad = (applyFlags & ~CustomizationFlags.AllColors) != 0;
		if (needAsyncLoad && car.IsLoadingVisualInProgress)
		{
			Debug.LogWarning($"Car '{car.name}'[{(int)applyFlags}] is loading visual[{car.LastLoadVisualHash}]. Call skipped!");
			return;
		}
		if (needAsyncLoad)
		{
			car.StartLoadingVisual((int)applyFlags);
		}
		CustomizationFlags curFlag = CustomizationFlags.None;
		try
		{
			if (applyFlags.HasFlag(CustomizationFlags.BodyParts))
			{
				curFlag = CustomizationFlags.BodyParts;
				ApplyBodyParts(car, visualSettings, profile, isLocalPlayerCar, forceProfileApply);
				await WaitFramesDelayIfAsync(1);
			}
			if (applyFlags.HasFlag(CustomizationFlags.Wheels))
			{
				curFlag = CustomizationFlags.Wheels;
				ApplyWheelParts(car, visualSettings);
				await WaitFramesDelayIfAsync(1);
			}
			if (applyFlags.HasFlag(CustomizationFlags.Vinyls))
			{
				curFlag = CustomizationFlags.Vinyls;
				ApplyVinyls(car, visualSettings);
				await WaitFramesDelayIfAsync(1);
			}
			ApplyColorSettings(car, visualSettings, applyFlags);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "CarVisualManager", $"Error apply visual settings: {ex.Message} ({curFlag})");
		}
		if (needAsyncLoad)
		{
			car.EndLoadingVisual();
		}
		async UniTask WaitFramesDelayIfAsync(int skipFrameCount)
		{
			int i = 0;
			while (isAsync)
			{
				int num = i + 1;
				i = num;
				if (num > skipFrameCount)
				{
					break;
				}
				await UniTask.Yield();
			}
		}
	}

	private void ApplyColorSettings(RaceCar car, VisualSettings settings, CustomizationFlags flags)
	{
		CustomizationFlags customizationFlags = CustomizationFlags.None;
		try
		{
			CustomizationFlags[] array = (CustomizationFlags[])Enum.GetValues(typeof(CustomizationFlags));
			foreach (CustomizationFlags customizationFlags2 in array)
			{
				if (customizationFlags2 == CustomizationFlags.None)
				{
					continue;
				}
				if (customizationFlags2 > CustomizationFlags.SmokeColor)
				{
					break;
				}
				customizationFlags = customizationFlags2;
				if (customizationFlags2 >= CustomizationFlags.ChassisColor && customizationFlags2 <= CustomizationFlags.RearLightsColor && customizationFlags2 != CustomizationFlags.AllBodyColors)
				{
					if (flags.HasFlag(customizationFlags2))
					{
						ApplyBodyPaint(car, settings, customizationFlags2);
					}
				}
				else if (customizationFlags2 >= CustomizationFlags.SteeringWheelSpokesColor && customizationFlags2 <= CustomizationFlags.LeftSeatCushionColor && customizationFlags2 != CustomizationFlags.AllSeatsBaseColors && customizationFlags2 != CustomizationFlags.AllSeatsCushionColors)
				{
					if (flags.HasFlag(customizationFlags2))
					{
						ApplyInteriorPaint(car, settings, customizationFlags2);
					}
				}
				else if (customizationFlags2 >= CustomizationFlags.FrontWheelRimsColor && customizationFlags2 <= CustomizationFlags.RearWheelSpokesColor && customizationFlags2 != CustomizationFlags.AllRimColors && customizationFlags2 != CustomizationFlags.AllSpokeColors)
				{
					if (flags.HasFlag(customizationFlags2))
					{
						ApplyWheelPaint(car, settings, customizationFlags2);
					}
				}
				else if (customizationFlags2 >= CustomizationFlags.TireSticker1 && customizationFlags2 <= CustomizationFlags.TireSticker3 && customizationFlags2 != CustomizationFlags.AllTireStickers)
				{
					if (flags.HasFlag(customizationFlags2))
					{
						ApplyTireStickerPaint(car, settings, customizationFlags2);
					}
				}
				else if (customizationFlags2 >= CustomizationFlags.ArmColor && customizationFlags2 <= CustomizationFlags.CalliperColor && customizationFlags2 != CustomizationFlags.AllSuspensionColors)
				{
					if (flags.HasFlag(customizationFlags2))
					{
						ApplySuspensionPaint(car, settings, customizationFlags2);
					}
				}
				else if (customizationFlags2 >= CustomizationFlags.FrontLightsGlassColor && customizationFlags2 <= CustomizationFlags.WindowsColor && customizationFlags2 != CustomizationFlags.AllGlassColors)
				{
					if (flags.HasFlag(customizationFlags2))
					{
						ApplyGlassPaint(car, settings, customizationFlags2);
					}
				}
				else if (customizationFlags2 == CustomizationFlags.SmokeColor && flags.HasFlag(customizationFlags2))
				{
					ApplySmokePaint(car, settings);
				}
			}
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "CarVisualManager", $"An error occured while applying body part color: {ex.Message} ({customizationFlags})");
		}
	}

	private void ApplyBodyParts(RaceCar car, VisualSettings settings, ProfilesInfo.Profile profile = null, bool isLocalPlayerCar = true, bool forceProfileApply = false)
	{
		car.carModel.ApplyVinylTextures();
		Dictionary<TypeID, string> dictionary = new Dictionary<TypeID, string>();
		using (Dictionary<TypeID, ICarPart>.KeyCollection.Enumerator enumerator = car.carModel.GetPartTypes())
		{
			while (enumerator.MoveNext())
			{
				dictionary.Add(enumerator.Current, null);
			}
		}
		settings.kitFrontWheels = 0;
		settings.kitRearWheels = 0;
		bool flag = settings.parts.Count == 0;
		flag |= car.isNetworkCar && settings.networkparts.Count > 0;
		if (flag)
		{
			CarPartDatabase.EquipDefaults(car.metaInfo.id, settings.parts);
		}
		else
		{
			CarPartDatabase.EquipStockInterior(car.metaInfo.id, settings.parts);
		}
		List<CarPart> list = CarPartDatabase.ConvertToPartList(settings.parts, keepNull: true);
		if (car.profileMetaInfo != null)
		{
			ApplyProfile(car, car.profileMetaInfo.id, profile, resetCarDesc: true, isLocalPlayerCar, forceProfileApply);
		}
		BaseCar.Desc desc = car.GetDesc();
		car.vinylsNotSupport = false;
		foreach (CarPart item in list)
		{
			if (item == null)
			{
				if (!flag)
				{
					settings.parts.Clear();
					ApplyBodyParts(car, settings, profile, isLocalPlayerCar);
					return;
				}
				continue;
			}
			string text = CrookedPartType(item);
			if (item.vinylsNotSupport)
			{
				car.vinylsNotSupport = true;
			}
			string empty = string.Empty;
			if (!string.IsNullOrEmpty(item.prefab))
			{
				empty = FixPartPath(item.prefab, text);
				if (!string.IsNullOrEmpty(empty))
				{
					dictionary[new TypeID(text)] = empty;
				}
			}
			foreach (CarPart.SubPart submodel in item.submodels)
			{
				if (!string.IsNullOrEmpty(submodel.prefab))
				{
					empty = FixPartPath(submodel.prefab, submodel.type);
					if (!string.IsNullOrEmpty(empty))
					{
						dictionary[new TypeID(submodel.type)] = empty;
					}
				}
			}
			if (item.cmds != null)
			{
				foreach (CarPartCommand cmd in item.cmds)
				{
					cmd.Apply(car, settings, desc);
				}
			}
			if (item.colorOverrides == null)
			{
				continue;
			}
			foreach (CarPartColorOverride colorOverride in item.colorOverrides)
			{
				colorOverride.Apply();
			}
		}
		car.SetDesc(desc);
		_ = car.gameObject.layer;
		_ = 29;
		bool flag2 = false;
		foreach (string value2 in dictionary.Values)
		{
			flag2 = Path.IsPathRooted(value2);
			if (flag2)
			{
				break;
			}
		}
		if (dictionary.ContainsKey(SuspensionId))
		{
			dictionary.Remove(SuspensionId);
		}
		if (dictionary.ContainsKey(BlobShadowTypeId))
		{
			dictionary.Remove(BlobShadowTypeId);
		}
		if (!flag2)
		{
			if (!dictionary.ContainsKey(InteriorId) || dictionary[InteriorId] == null)
			{
				dictionary[InteriorId] = "stock";
			}
			if (dictionary.TryGetValue(WingTypeId, out var value) && value != null)
			{
				dictionary.Remove(WingTypeId);
				dictionary[ChassisId] = value;
			}
		}
		foreach (KeyValuePair<TypeID, string> item2 in dictionary)
		{
			car.carModel.LoadPart(item2.Key, item2.Value, Path.IsPathRooted(item2.Value) || item2.Key == InteriorId);
		}
		car.carModel.LoadPart(SuspensionId, flag2 ? null : "suspensionmacpherson", useModelRoot: true);
		car.carModel.LoadPart(BlobShadowTypeId, "blobshadowcar", useModelRoot: true);
		car.carModel.LoadPart(CamerasTypeId, "stock", useModelRoot: true);
		if (!flag2)
		{
			return;
		}
		UniTask.Post(async delegate
		{
			await UniTask.WaitWhile(() => car.carModel != null && car.carModel.IsCarPartsLoading);
			m_carExternalPartsPostProcessor.ProcessExternalPartsOnRaceCar(car);
		});
	}

	public bool ClearVisualIfNeed(int carId)
	{
		VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(carId);
		if (visualForCar == null)
		{
			return false;
		}
		bool flag = false;
		if (visualForCar.winterTires)
		{
			if (States.mainCurrent is BaseRaceState)
			{
				visualForCar.winterTires = TrackMetaDB.IsWinterTrackWithIce(m_prefs.globals.lastTrackId);
			}
			else
			{
				visualForCar.winterTires = false;
			}
			flag = true;
		}
		if (CarPartDatabase.IsCarHaveLockedParts(carId))
		{
			VisualSettings visualSettings = visualForCar;
			if (visualSettings.parts == null)
			{
				visualSettings.parts = new HashSet<int>();
			}
			visualForCar.parts.Clear();
			flag = true;
		}
		if (CarPartDatabase.IsCarHaveLockedWheels(carId))
		{
			visualForCar.frontWheels = visualForCar.kitFrontWheels;
			visualForCar.rearWheels = visualForCar.kitRearWheels;
			flag = true;
		}
		if (flag)
		{
			m_prefs.carSettings.SetVisualForCar(carId, visualForCar);
		}
		return flag;
	}

	public bool ForceUpdateAndGetKitWheelVisibleFromSettings(VisualSettings settings)
	{
		settings.kitFrontWheels = 0;
		settings.kitRearWheels = 0;
		foreach (CarPart item in CarPartDatabase.ConvertToPartList(settings.parts, keepNull: true))
		{
			List<CarPartCommand> list = item?.cmds;
			if (list == null)
			{
				continue;
			}
			foreach (CarPartCommand item2 in list)
			{
				item2?.ApplyOnlyKitWheel(settings);
			}
		}
		if (settings.kitFrontWheels == 0)
		{
			return settings.kitRearWheels != 0;
		}
		return true;
	}

	private void ApplyWheelParts(RaceCar car, VisualSettings settings)
	{
		bool flag = false;
		if (!TryLoadWheel(car, CarWheelID.FrontWheels, ref settings.frontWheels))
		{
			flag = true;
		}
		if (!TryLoadWheel(car, CarWheelID.RearWheels, ref settings.rearWheels))
		{
			flag = true;
		}
		if (flag & !car.isNetworkCar)
		{
			SaveVisualSettingsForCar(car.carId, settings);
		}
		car.carModel.LoadWheel(CarWheelID.AllWheels, BrakeRotorId, "rotor_" + 0);
		car.carModel.LoadWheel(CarWheelID.AllWheels, BrakeCaliperId, "caliper_" + 0);
		LoadTires(car, CarWheelID.FrontWheels, settings);
		LoadTires(car, CarWheelID.RearWheels, settings);
		ApplyTireStickerPaint(car, settings, CustomizationFlags.TireSticker1);
		ApplyTireStickerPaint(car, settings, CustomizationFlags.TireSticker2);
	}

	private bool TryLoadWheel(RaceCar car, CarWheelID wheelType, ref int wheelId)
	{
		if (wheelId == 0)
		{
			wheelId = car.metaInfo.defaultWheelId;
		}
		CarPart partById = CarPartDatabase.GetPartById(wheelId);
		if (partById == null || string.IsNullOrEmpty(partById.prefab))
		{
			return false;
		}
		car.carModel.LoadWheel(wheelType, RimId, FixRimPath(partById.prefab));
		return true;
	}

	public static byte[] SerializeVinyls(VisualSettings visual)
	{
		List<VinylLayerProxy> list = new List<VinylLayerProxy>();
		List<VinylLayer> vinylLayers = visual.vinylLayers;
		if (vinylLayers != null)
		{
			list.Capacity = vinylLayers.Count;
			int count = vinylLayers.Count;
			for (int i = 0; i < count; i++)
			{
				VinylLayer vinylLayer = vinylLayers[i];
				if (vinylLayer != null)
				{
					list.Add(new VinylLayerProxy
					{
						version = vinylLayer.version,
						rawId = vinylLayer.rawId,
						groupMask = vinylLayer.groupMask,
						isSymmetry = vinylLayer.isSymmetry,
						isHorizontalFlip = vinylLayer.isHorizontalFlip,
						isVerticalFlip = vinylLayer.isVerticalFlip,
						isPassThrough = vinylLayer.isPassThrough,
						angle = vinylLayer.angle,
						position = vinylLayer.position,
						rotation = vinylLayer.rotation,
						scale = vinylLayer.scale,
						clipMaskShift = vinylLayer.clipMaskShift,
						projectionMatrix = vinylLayer.projectionMatrix,
						materialProps = vinylLayer.materialProps,
						colors = vinylLayer.colors
					});
				}
			}
		}
		return MessagePackSerializer.Serialize(list);
	}

	public static List<VinylLayer> DeserializeVinyls(byte[] data)
	{
		List<VinylLayer> list = new List<VinylLayer>();
		List<VinylLayerProxy> list2 = MessagePackSerializer.Deserialize<List<VinylLayerProxy>>(data);
		if (list2 == null)
		{
			return list;
		}
		foreach (VinylLayerProxy item2 in list2)
		{
			VinylLayer item = new VinylLayer
			{
				version = item2.version,
				rawId = item2.rawId,
				groupMask = item2.groupMask,
				isSymmetry = item2.isSymmetry,
				isHorizontalFlip = item2.isHorizontalFlip,
				isVerticalFlip = item2.isVerticalFlip,
				isPassThrough = item2.isPassThrough,
				angle = item2.angle,
				position = item2.position,
				rotation = item2.rotation,
				scale = item2.scale,
				clipMaskShift = item2.clipMaskShift,
				projectionMatrix = item2.projectionMatrix,
				materialProps = item2.materialProps,
				colors = item2.colors
			};
			list.Add(item);
		}
		return list;
	}

	public static byte[] SerializeCarPaint(VisualSettings visual)
	{
		List<CarPaintHolderProxy> list = new List<CarPaintHolderProxy>();
		List<CarPaintHolder> colors = visual.colors;
		if (colors != null)
		{
			list.Capacity = colors.Count;
			int count = colors.Count;
			for (int i = 0; i < count; i++)
			{
				CarPaintHolder carPaintHolder = colors[i];
				if (carPaintHolder != null)
				{
					CarPaintHolderProxy item = new CarPaintHolderProxy
					{
						type = carPaintHolder.type,
						id = carPaintHolder.id.GetValueOrDefault(),
						color = carPaintHolder.color.GetValueOrDefault()
					};
					if (carPaintHolder.specColor.HasValue)
					{
						item.specColor = carPaintHolder.specColor.GetValueOrDefault();
					}
					if (carPaintHolder.duoColor.HasValue)
					{
						item.duoColor = carPaintHolder.duoColor.GetValueOrDefault();
					}
					if (carPaintHolder.customSecColor.HasValue)
					{
						item.customSecColor = carPaintHolder.customSecColor.GetValueOrDefault();
					}
					if (carPaintHolder.indexConfig.HasValue)
					{
						item.indexConfig = carPaintHolder.indexConfig.GetValueOrDefault();
					}
					list.Add(item);
				}
			}
		}
		return MessagePackSerializer.Serialize(list);
	}

	public static List<CarPaintHolder> DeserializeCarPaint(byte[] data)
	{
		List<CarPaintHolder> list = new List<CarPaintHolder>();
		List<CarPaintHolderProxy> list2 = MessagePackSerializer.Deserialize<List<CarPaintHolderProxy>>(data);
		if (list2 == null)
		{
			return list;
		}
		foreach (CarPaintHolderProxy item2 in list2)
		{
			CarPaintHolder item = new CarPaintHolder
			{
				type = item2.type,
				id = item2.id,
				color = item2.color,
				specColor = item2.specColor,
				duoColor = item2.duoColor,
				customSecColor = item2.customSecColor,
				indexConfig = item2.indexConfig
			};
			list.Add(item);
		}
		return list;
	}

	public void ApplyWheelSizeUpdate(RaceCar car, ProfilesInfo.Profile profile)
	{
		if (car != null && car.profileMetaInfo != null)
		{
			ApplyProfile(car, car.profileMetaInfo.id, profile, resetCarDesc: false);
		}
	}

	public void ApplyAllTires(RaceCar car, VisualSettings overrideSettings = null)
	{
		if (car == null)
		{
			return;
		}
		VisualSettings visualSettings = ((overrideSettings == null) ? m_prefs.carSettings.GetVisualForCar(car.metaInfo.id) : overrideSettings);
		if (visualSettings == null)
		{
			return;
		}
		CarWheelModel[] carWheels = car.carModel.carWheels;
		foreach (CarWheelModel carWheelModel in carWheels)
		{
			CarWheelTirePart tirePart = carWheelModel.tirePart;
			if (tirePart != null)
			{
				tirePart.material = (((CarWheelID.FrontWheels & carWheelModel.id) == carWheelModel.id) ? new TirePartMaterial(visualSettings.frontTire) : new TirePartMaterial(visualSettings.rearTire));
			}
		}
		ApplyTireStickerPaint(car, visualSettings, CustomizationFlags.TireSticker1);
		ApplyTireStickerPaint(car, visualSettings, CustomizationFlags.TireSticker2);
	}

	private void LoadTires(RaceCar car, CarWheelID wheelIds, VisualSettings settings)
	{
		if (settings.winterTires)
		{
			switch (wheelIds)
			{
			case CarWheelID.FrontAxle:
			case CarWheelID.FrontWheels:
			{
				TirePartMaterial customMaterial2 = null;
				if (settings.frontTire != null)
				{
					TireVisualSettings tireVisualSettings2 = settings.frontTire.Copy();
					tireVisualSettings2.ProtectorGrip = TyreProtectorGripType.Winter;
					customMaterial2 = GetTireMaterial(tireVisualSettings2);
				}
				car.carModel.LoadWheel(CarWheelID.FrontLeft, TireId, "tire__l", customMaterial2);
				car.carModel.LoadWheel(CarWheelID.FrontRight, TireId, "tire__r", customMaterial2);
				break;
			}
			case CarWheelID.RearAxle:
			case CarWheelID.RearWheels:
			{
				TirePartMaterial customMaterial = null;
				if (settings.rearTire != null)
				{
					TireVisualSettings tireVisualSettings = settings.rearTire.Copy();
					tireVisualSettings.ProtectorGrip = TyreProtectorGripType.Winter;
					customMaterial = GetTireMaterial(tireVisualSettings);
				}
				car.carModel.LoadWheel(CarWheelID.RearLeft, TireId, "tire__l", customMaterial);
				car.carModel.LoadWheel(CarWheelID.RearRight, TireId, "tire__r", customMaterial);
				break;
			}
			}
		}
		else
		{
			switch (wheelIds)
			{
			case CarWheelID.FrontAxle:
			case CarWheelID.FrontWheels:
			{
				TirePartMaterial tireMaterial2 = GetTireMaterial(settings.frontTire);
				car.carModel.LoadWheel(CarWheelID.FrontLeft, TireId, "tire__l", tireMaterial2);
				car.carModel.LoadWheel(CarWheelID.FrontRight, TireId, "tire__r", tireMaterial2);
				break;
			}
			case CarWheelID.RearAxle:
			case CarWheelID.RearWheels:
			{
				TirePartMaterial tireMaterial = GetTireMaterial(settings.rearTire);
				car.carModel.LoadWheel(CarWheelID.RearLeft, TireId, "tire__l", tireMaterial);
				car.carModel.LoadWheel(CarWheelID.RearRight, TireId, "tire__r", tireMaterial);
				break;
			}
			}
		}
	}

	private TirePartMaterial GetTireMaterial(TireVisualSettings tireVisualSettings)
	{
		return new TirePartMaterial(tireVisualSettings);
	}

	public void SetFrictionParams(RaceCar car, CarWheelID axle, float value, float minValue, float maxValue)
	{
		SetTyreGripType(car, axle, (TyreProtectorGripType)Convert());
		int Convert()
		{
			int num = 0;
			int num2 = 3;
			return Mathf.Clamp(Mathf.RoundToInt((value - minValue) / (maxValue - minValue) * (float)(num2 - num) + (float)num), num, num2);
		}
	}

	public void SetTyreGripType(RaceCar car, CarWheelID axle, TyreProtectorGripType gripType)
	{
		VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(car.metaInfo.id);
		switch (axle)
		{
		case CarWheelID.FrontAxle:
			visualForCar.frontTire.ProtectorGrip = gripType;
			break;
		case CarWheelID.RearAxle:
			visualForCar.rearTire.ProtectorGrip = gripType;
			break;
		case CarWheelID.AllWheels:
			visualForCar.rearTire.ProtectorGrip = gripType;
			visualForCar.frontTire.ProtectorGrip = gripType;
			break;
		}
		m_prefs.carSettings.SetVisualWithoutSave(car.metaInfo.id, visualForCar);
	}

	public static int GetTyreIndex(float frictionMultiplier)
	{
		return Mathf.Clamp(Mathf.RoundToInt(frictionMultiplier * 100f) / ScriptableSingleton<TiresConfig>.instance.stepFriction * 10, ScriptableSingleton<TiresConfig>.instance.minFriction, ScriptableSingleton<TiresConfig>.instance.maxFriction);
	}

	public void ApplyVinyls(RaceCar car, VisualSettings visualSettings)
	{
		if (car == null || car.carModel?.livery == null || visualSettings == null || visualSettings.vinylLayers == null || !CanApplyVinylsForNetworkCar(car))
		{
			return;
		}
		car.carModel.livery.RemoveAllLayers();
		int maxLayersCount = VinylSystemConfig.config.maxLayersCount;
		int count = visualSettings.vinylLayers.Count;
		int num = Mathf.Min(count, maxLayersCount);
		if (car.vinylsNotSupport)
		{
			return;
		}
		for (int i = 0; i < num; i++)
		{
			VinylLayer vinylLayer = visualSettings.vinylLayers[i];
			if (vinylLayer != null && Singleton<VinylMetaProvider>.instance.meta.GetVinylById(vinylLayer.rawId) != null)
			{
				car.carModel.livery.AddLayer(vinylLayer);
			}
		}
		if (count > maxLayersCount)
		{
			GameLog.Print(AlertLevel.Warning, "System", $"Livery has more than {maxLayersCount} layers and will not be fully loaded");
		}
	}

	public bool CanApplyVinylsForNetworkCar(RaceCar car)
	{
		if (car == null)
		{
			return false;
		}
		if (!car.isNetworkCar)
		{
			return true;
		}
		NetworkPlayer networkPlayer = car.networkPlayer;
		if (networkPlayer == null || networkPlayer.PlayerProperties == null || networkPlayer.PlayerProperties.IsShowVinylsDisabled)
		{
			return false;
		}
		if (networkPlayer.IsLocal)
		{
			return true;
		}
		PlayerId playerId = networkPlayer.PlayerId;
		if (NetworkGame.reportedByVinylsPlayers.Contains(playerId.ToString()))
		{
			return false;
		}
		IGameOverlay instance = Overlay.instance;
		GlobalModel.VinylsShowOptions vinylsShowOption = m_prefs.globals.vinylsShowOption;
		if (IsOverlayDisableVinyls(playerId, instance))
		{
			bool num = instance.IsFriend(playerId);
			bool isFriendShowUGCAllowed = instance.IsFriendShowUGCAllowed;
			return num && isFriendShowUGCAllowed;
		}
		return vinylsShowOption switch
		{
			GlobalModel.VinylsShowOptions.NotShow => false, 
			GlobalModel.VinylsShowOptions.CurrentPlatform => playerId.platform == instance.GetPlatform(), 
			GlobalModel.VinylsShowOptions.All => true, 
			_ => false, 
		};
	}

	private bool IsOverlayDisableVinyls(PlayerId playerId, IGameOverlay overlay)
	{
		if (overlay.IsBlockedUser(playerId))
		{
			return true;
		}
		if (States.current is SyncNetFreerideRaceModeState)
		{
			return overlay.IsUserGeneratedContentRestricted;
		}
		return false;
	}

	private void ApplyBodyPaint(RaceCar car, VisualSettings settings, CustomizationFlags flag)
	{
		CarPaintHolder paint = settings.GetPaint(flag);
		Color obj = paint?.CustomSecColor ?? car.metaInfo.defaultCustomSecColor;
		CarPaintType carPaintType = ConvertToCarPaintType(paint?.id ?? 1);
		Color col = paint?.color ?? car.metaInfo.defaultColor;
		Color col2 = obj;
		HSBColor hSBColor = new HSBColor(col);
		HSBColor hSBColor2 = new HSBColor(col2);
		try
		{
			if (carPaintType == CarPaintType.Pearlescent && !Array.Exists(m_carIdsUnableToPaint, (int carId) => carId == car.carId))
			{
				car.carModel.SetBodyPartPaint(hSBColor, carPaintType, FlagToMaterialType(flag), hSBColor2, (paint?.indexConfig).GetValueOrDefault());
			}
			else if (flag == CustomizationFlags.RampColor)
			{
				car.carModel.SetGenericPaintColor(hSBColor);
			}
			else
			{
				car.carModel.SetBodyPartPaint(hSBColor, carPaintType, FlagToMaterialType(flag), (paint?.indexConfig).GetValueOrDefault());
			}
		}
		catch (Exception e)
		{
			GameLog.LogException(e);
		}
	}

	private void ApplyInteriorPaint(RaceCar car, VisualSettings settings, CustomizationFlags flag)
	{
		TypeID partType = FlagToMaterialType(flag);
		TypeID materialType = InterorParts;
		Color color = DEFAULT_GENERIC_COLOR;
		switch (flag)
		{
		case CustomizationFlags.SteeringWheelSpokesColor:
		case CustomizationFlags.ShifterColor:
		case CustomizationFlags.HandbrakeColor:
			materialType = InterorParts;
			break;
		case CustomizationFlags.SteeringWheelCoverColor:
		case CustomizationFlags.RightSeatBaseColor:
		case CustomizationFlags.LeftSeatBaseColor:
			materialType = Alcantar1;
			color = DEFAULT_INTERIOR_ALCANTAR_COLOR;
			break;
		case CustomizationFlags.RightSeatCushionColor:
		case CustomizationFlags.LeftSeatCushionColor:
			materialType = Alcantar2;
			color = DEFAULT_INTERIOR_ALCANTAR_COLOR;
			break;
		}
		Color col = settings.GetPaint(flag)?.color ?? color;
		HSBColor hSBColor = new HSBColor(col);
		car.carModel.SetInteriorPaint(hSBColor, CarPaintType.Solid, partType, materialType, 0);
	}

	private void ApplyWheelPaint(RaceCar car, VisualSettings settings, CustomizationFlags flag)
	{
		CarPaintHolder paint = settings.GetPaint(CustomizationFlags.FrontWheelRimsColor);
		CarPaintHolder paint2 = settings.GetPaint(CustomizationFlags.RearWheelRimsColor);
		CarPaintHolder paint3 = settings.GetPaint(CustomizationFlags.FrontWheelSpokesColor);
		CarPaintHolder paint4 = settings.GetPaint(CustomizationFlags.RearWheelSpokesColor);
		DefaultColorsMeta defaultFrontRimColors = car.metaInfo.defaultWheelsColors.defaultFrontRimColors;
		DefaultColorsMeta defaultFrontSpokeColors = car.metaInfo.defaultWheelsColors.defaultFrontSpokeColors;
		CarPaintType carPaintType = ConvertToCarPaintType(paint?.id ?? defaultFrontRimColors.defaultMaterial);
		CarPaintType carPaintType2 = ConvertToCarPaintType(paint3?.id ?? defaultFrontRimColors.defaultMaterial);
		CarPaintType carPaintType3 = ConvertToCarPaintType(paint2?.id ?? defaultFrontRimColors.defaultMaterial);
		CarPaintType carPaintType4 = ConvertToCarPaintType(paint4?.id ?? defaultFrontRimColors.defaultMaterial);
		HSBColor hSBColor = new HSBColor(paint?.color ?? defaultFrontRimColors.defaultColor);
		HSBColor hSBColor2 = new HSBColor(paint3?.color ?? defaultFrontSpokeColors.defaultColor);
		HSBColor hSBColor3 = new HSBColor(paint2?.color ?? defaultFrontRimColors.defaultColor);
		HSBColor hSBColor4 = new HSBColor(paint4?.color ?? defaultFrontSpokeColors.defaultColor);
		HSBColor hSBColor5 = new HSBColor(paint?.CustomSecColor ?? defaultFrontRimColors.defaultCustomSecColor);
		HSBColor hSBColor6 = new HSBColor(paint3?.CustomSecColor ?? defaultFrontSpokeColors.defaultCustomSecColor);
		HSBColor hSBColor7 = new HSBColor(paint2?.CustomSecColor ?? defaultFrontRimColors.defaultCustomSecColor);
		HSBColor hSBColor8 = new HSBColor(paint4?.CustomSecColor ?? defaultFrontSpokeColors.defaultCustomSecColor);
		int indexConfig = paint?.indexConfig ?? defaultFrontRimColors.defaultIndexConfig;
		int indexConfig2 = paint3?.indexConfig ?? defaultFrontSpokeColors.defaultIndexConfig;
		int indexConfig3 = paint2?.indexConfig ?? defaultFrontRimColors.defaultIndexConfig;
		int indexConfig4 = paint4?.indexConfig ?? defaultFrontSpokeColors.defaultIndexConfig;
		if (carPaintType == CarPaintType.Pearlescent)
		{
			car.carModel.SetWheelPaint(hSBColor, hSBColor5, carPaintType, RimId, CarWheelID.FrontWheels, indexConfig);
		}
		else
		{
			car.carModel.SetWheelPaint(hSBColor, carPaintType, RimId, CarWheelID.FrontWheels, indexConfig);
		}
		if (carPaintType2 == CarPaintType.Pearlescent)
		{
			car.carModel.SetWheelPaint(hSBColor2, hSBColor6, carPaintType2, SpokeId, CarWheelID.FrontWheels, indexConfig2);
		}
		else
		{
			car.carModel.SetWheelPaint(hSBColor2, carPaintType2, SpokeId, CarWheelID.FrontWheels, indexConfig2);
		}
		if (carPaintType3 == CarPaintType.Pearlescent)
		{
			car.carModel.SetWheelPaint(hSBColor3, hSBColor7, carPaintType3, RimId, CarWheelID.RearWheels, indexConfig3);
		}
		else
		{
			car.carModel.SetWheelPaint(hSBColor3, carPaintType3, RimId, CarWheelID.RearWheels, indexConfig3);
		}
		if (carPaintType4 == CarPaintType.Pearlescent)
		{
			car.carModel.SetWheelPaint(hSBColor4, hSBColor8, carPaintType4, SpokeId, CarWheelID.RearWheels, indexConfig4);
		}
		else
		{
			car.carModel.SetWheelPaint(hSBColor4, carPaintType4, SpokeId, CarWheelID.RearWheels, indexConfig4);
		}
	}

	private void ApplyTireStickerPaint(RaceCar car, VisualSettings settings, CustomizationFlags flag)
	{
		HSBColor hSBColor = new HSBColor(settings.GetPaint(flag)?.color ?? DEFAULT_TIRE_STICKER_COLOR);
		car.carModel.SetTireStickerPaint(hSBColor, (flag != CustomizationFlags.TireSticker1) ? TireStickerIndex.Two : TireStickerIndex.One);
	}

	private void ApplySuspensionPaint(RaceCar car, VisualSettings settings, CustomizationFlags flag)
	{
		Color col = settings.GetPaint(flag)?.color ?? DEFAULT_GENERIC_COLOR;
		HSBColor hSBColor = new HSBColor(col);
		if (flag == CustomizationFlags.CalliperColor)
		{
			car.carModel.SetSuspensionPaint(hSBColor, 0, BrakeCaliperId);
			return;
		}
		int colorIndex = 0;
		switch (flag)
		{
		case CustomizationFlags.StrutColor:
			colorIndex = 1;
			break;
		case CustomizationFlags.SpringColor:
			colorIndex = 2;
			break;
		}
		car.carModel.SetSuspensionPaint(hSBColor, colorIndex, SuspensionId);
	}

	private void ApplyGlassPaint(RaceCar car, VisualSettings settings, CustomizationFlags flag)
	{
		Color col = settings.GetPaint(flag)?.color ?? DEFAULT_GLASS_COLOR;
		HSBColor hSBColor = new HSBColor(col);
		TypeID partType = FlagToMaterialType(flag);
		CarModel.GlassType glassType = FlagToGlassType(flag);
		car.carModel.SetGlassPaint(hSBColor, CarPaintType.Normal, partType, glassType, 0);
	}

	private void ApplySmokePaint(RaceCar car, VisualSettings settings)
	{
		if (!(m_model.GetById<SmokeColorMeta>(car.metaInfo.defaultSmokeColorId).materialParams.Clone() is MaterialParameters materialParameters))
		{
			GameLog.Print(AlertLevel.Error, "CarVisualManager", "Could not load smoke color " + car.metaInfo.defaultSmokeColorId);
			return;
		}
		materialParameters.color = settings.GetPaint(CustomizationFlags.SmokeColor)?.color;
		if (!materialParameters.color.HasValue)
		{
			GameLog.Print(AlertLevel.Warning, "CarVisualManager", "Smoke color does not contain color");
			materialParameters.color = DEFAULT_SMOKE_COLOR;
		}
		car.SetSmokeColor(materialParameters.color.Value, delegate(BaseWheelSmoke smoke)
		{
			AddMaterialHelperIfNeeded(smoke.gameObject, CustomizationType.SmokeColor);
		});
	}

	private CarModel.GlassType FlagToGlassType(CustomizationFlags flag)
	{
		return flag switch
		{
			CustomizationFlags.FrontLightsGlassColor => CarModel.GlassType.LightsFront, 
			CustomizationFlags.RearLightsGlassColor => CarModel.GlassType.LightsRear, 
			_ => CarModel.GlassType.Windows, 
		};
	}

	private TypeID FlagToMaterialType(CustomizationFlags flag)
	{
		if (flag <= CustomizationFlags.FrontLightsColor)
		{
			if (flag <= CustomizationFlags.RoofColor)
			{
				if (flag <= CustomizationFlags.RearBumperColor)
				{
					CustomizationFlags num = flag - 1;
					if ((ulong)num <= 3uL)
					{
						switch ((int)num)
						{
						case 0:
							return ChassisId;
						case 1:
							return RampId;
						case 3:
							return CageId;
						case 2:
							goto IL_0235;
						}
					}
					switch (flag)
					{
					case CustomizationFlags.FrontBumperColor:
						return BumperFrontId;
					case CustomizationFlags.RearBumperColor:
						return BumperRearId;
					}
				}
				else
				{
					switch (flag)
					{
					case CustomizationFlags.SkirtsColor:
						return SkirtsId;
					case CustomizationFlags.DoorsColor:
						return DoorsId;
					case CustomizationFlags.RoofColor:
						return RoofId;
					}
				}
			}
			else
			{
				switch (flag)
				{
				case CustomizationFlags.SideMirrorsColor:
					return MirrorsId;
				case CustomizationFlags.HoodColor:
					return BonnetId;
				case CustomizationFlags.TrunkColor:
					return TrunkId;
				case CustomizationFlags.SpoilerColor:
					return SpoilerId;
				case CustomizationFlags.FendersColor:
					return ChassisId;
				case CustomizationFlags.FrontLightsColor:
					return LightsFrontId;
				}
			}
		}
		else
		{
			switch (flag)
			{
			case CustomizationFlags.RearLightsColor:
				return LightsRearId;
			case CustomizationFlags.SteeringWheelSpokesColor:
				return SteeringWheelId;
			case CustomizationFlags.SteeringWheelCoverColor:
				return SteeringWheelId;
			case CustomizationFlags.ShifterColor:
				return ShifterId;
			case CustomizationFlags.HandbrakeColor:
				return HandbrakeId;
			case CustomizationFlags.RightSeatBaseColor:
				return SeatRightId;
			case CustomizationFlags.RightSeatCushionColor:
				return SeatRightId;
			case CustomizationFlags.LeftSeatBaseColor:
				return SeatLeftId;
			case CustomizationFlags.LeftSeatCushionColor:
				return SeatLeftId;
			case CustomizationFlags.FrontLightsGlassColor:
				return GlassLightsId;
			case CustomizationFlags.RearLightsGlassColor:
				return GlassLightsId;
			case CustomizationFlags.WindowsColor:
				return GlassId;
			}
		}
		goto IL_0235;
		IL_0235:
		Debug.LogError($"[CarVisualManager] Unable to get material type for '{flag}'");
		return null;
	}

	public void ApplyProfile(RaceCar car, int profileId, ProfilesInfo.Profile profile, bool resetCarDesc = true, bool isLocalPlayerCar = true, bool forceProfileApply = false)
	{
		if (SkipProfileApply() && !forceProfileApply)
		{
			return;
		}
		PlayerCarProfile byId = m_model.GetById<PlayerCarProfile>(profileId);
		if (byId != null)
		{
			car.profileMetaInfo = byId;
			if (resetCarDesc)
			{
				car.InitDesc(byId.asset);
			}
			CustomProfile customProfile = null;
			if (isLocalPlayerCar)
			{
				customProfile = m_prefs.carSettings.GetInstalledCustomProfile(car.carId);
			}
			bool flag = customProfile != null;
			ProfilesInfo.Profile profile2 = profile ?? m_prefs.carSettings.GetProfileInfo(profileId, car.carId, customProfile?.id);
			if (profile2 != null)
			{
				car.ApplyAdvancedProfile(m_model, profile2, m_prefs.carSettings, reset: false, flag, null, customProfile?.id, !flag);
			}
		}
		static bool SkipProfileApply()
		{
			if (States.mainCurrent is BaseGameState { contextOwner: { } contextOwner })
			{
				UISetupsContext uISetupsContext = contextOwner.Get<UISetupsContext>();
				UISetupsSharingContext uISetupsSharingContext = contextOwner.Get<UISetupsSharingContext>();
				bool num = uISetupsContext != null && uISetupsContext.isActive;
				bool flag2 = uISetupsSharingContext != null && uISetupsSharingContext.isActive;
				return num || flag2;
			}
			return false;
		}
	}

	public static string GetProfileData(string assetName)
	{
		string result = "";
		TextAsset textAsset = Resources.Load("Profiles/" + assetName, typeof(TextAsset)) as TextAsset;
		if ((bool)textAsset)
		{
			result = textAsset.text;
			Resources.UnloadAsset(textAsset);
		}
		return result;
	}

	public static List<VinylLayer> GenerateVinylLayers(int numOfLayers)
	{
		int maxLayersCount = VinylSystemConfig.config.maxLayersCount;
		if (numOfLayers > maxLayersCount)
		{
			numOfLayers = maxLayersCount;
		}
		List<VinylLayer> list = new List<VinylLayer>(numOfLayers);
		for (int i = 0; i < numOfLayers; i++)
		{
			list.Add(GenerateVinylLayer());
		}
		return list;
	}

	public static VinylLayer GenerateVinylLayer()
	{
		VinylLayer vinylLayer = new VinylLayer();
		VinylMeta vinylMeta = Singleton<VinylMetaProvider>.instance.meta.vinyls[UnityEngine.Random.Range(0, Singleton<VinylMetaProvider>.instance.meta.vinyls.Count - 1)];
		vinylLayer.rawId = vinylMeta.id;
		vinylLayer.position = UnityEngine.Random.insideUnitSphere * 0.5f;
		vinylLayer.rotation = Quaternion.Euler(UnityEngine.Random.Range(15, 180), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(15, 180));
		vinylLayer.angle = UnityEngine.Random.Range(0, 360);
		vinylLayer.scale = Vector2.one * UnityEngine.Random.Range(0.2f, 1.4f);
		vinylLayer.groupMask = UnityEngine.Random.Range(1, 4);
		vinylLayer.colors = vinylMeta.defaultColors;
		vinylLayer.materialProps = vinylMeta.defaultMaterialProps;
		for (int i = 0; i < vinylLayer.colors.Length; i++)
		{
			vinylLayer.colors[i] = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
		}
		for (int j = 0; j < vinylLayer.materialProps.Length; j++)
		{
			vinylLayer.materialProps[j] = UnityEngine.Random.Range(0, VinylSystemConfig.Materials.Length);
		}
		return vinylLayer;
	}

	private GameObject GetWheelPrefabByID(int id, int defaultId)
	{
		CarPart partById = CarPartDatabase.GetPartById((id == 0) ? defaultId : id);
		GameObject gameObject = ((partById != null && partById.external) ? CarPartExternalLoader.LoadFromFile(partById.prefab) : Resources.Load<GameObject>(partById.prefab));
		if (gameObject == null)
		{
			partById = CarPartDatabase.GetPartById(defaultId);
			gameObject = ((partById != null && partById.external) ? CarPartExternalLoader.LoadFromFile(partById.prefab) : Resources.Load<GameObject>(partById.prefab));
		}
		return gameObject;
	}

	private bool ApplyMatParamsByNamePart(string namePart, Renderer renderer, MaterialParameters parameters, List<Material> materials = null)
	{
		if (string.IsNullOrEmpty(namePart) || renderer == null)
		{
			return false;
		}
		MaterialAccessor orAddComponent = renderer.gameObject.GetOrAddComponent<MaterialAccessor>();
		bool result = false;
		int i = 0;
		for (int materialsCount = orAddComponent.materialsCount; i < materialsCount; i++)
		{
			Material material = orAddComponent.GetMaterial(i);
			if (material.name.Contains(namePart))
			{
				ApplyMaterialParameters(material, parameters);
				result = true;
				materials?.Add(material);
			}
		}
		return result;
	}

	private void ApplyMaterialParameters(Material mat, MaterialParameters parameters)
	{
		if (mat == null)
		{
			GameLog.Print(AlertLevel.Error, "CarVisualManager", "Null material");
			return;
		}
		if (parameters.texture != null)
		{
			mat.mainTexture = parameters.texture;
			if (!string.IsNullOrEmpty(parameters.mainTexturePath))
			{
				return;
			}
		}
		else if (!string.IsNullOrEmpty(parameters.mainTexturePath))
		{
			Texture texture = Resources.Load(parameters.mainTexturePath, typeof(Texture)) as Texture;
			if (texture == null)
			{
				GameLog.Print(AlertLevel.Warning, "CarVisualManager", "Could not load texture from '" + parameters.mainTexturePath + "' while applying material parameters");
			}
			else
			{
				mat.mainTexture = texture;
			}
		}
		if (parameters.color.HasValue)
		{
			mat.color = parameters.color.Value;
		}
		if (parameters.shaderParams != null)
		{
			foreach (ShaderParameter shaderParam in parameters.shaderParams)
			{
				if (shaderParam is ShaderParameterInt)
				{
					mat.SetInt(shaderParam.name, ((ShaderParameterInt)shaderParam).value);
				}
				else if (shaderParam is ShaderParameterFloat)
				{
					mat.SetFloat(shaderParam.name, ((ShaderParameterFloat)shaderParam).value);
				}
				else if (shaderParam is ShaderParameterColor)
				{
					mat.SetColor(shaderParam.name, ((ShaderParameterColor)shaderParam).value);
				}
				else
				{
					GameLog.Print(AlertLevel.Error, "CarVisualManager", "Unsupported shader parameter type: " + shaderParam.GetType().Name);
				}
			}
		}
		if (parameters.shaderKeywords != null)
		{
			mat.shaderKeywords = (string[])parameters.shaderKeywords.Clone();
		}
	}

	private void AddMaterialHelperIfNeeded(GameObject toObj, CustomizationType type)
	{
	}

	public static TransactionManager.UnlockType GetUnlockTypeFrom(CustomizationType type)
	{
		return type switch
		{
			CustomizationType.BodyColor => TransactionManager.UnlockType.CarColor, 
			CustomizationType.RimColor => TransactionManager.UnlockType.WheelRimColor, 
			CustomizationType.SpokesColor => TransactionManager.UnlockType.WheelSpokeColor, 
			CustomizationType.SmokeColor => TransactionManager.UnlockType.SmokeColor, 
			CustomizationType.GlassColor => TransactionManager.UnlockType.GlassColor, 
			_ => throw new Exception("Unsupported customization type: " + type), 
		};
	}

	public static CustomizationFlags CustomizationTypeToFlag(CustomizationType type)
	{
		return type switch
		{
			CustomizationType.BodyColor => CustomizationFlags.ChassisColor, 
			CustomizationType.RimColor => CustomizationFlags.RearWheelRimsColor, 
			CustomizationType.SpokesColor => CustomizationFlags.FrontWheelSpokesColor, 
			CustomizationType.SmokeColor => CustomizationFlags.SmokeColor, 
			CustomizationType.GlassColor => CustomizationFlags.WindowsColor, 
			_ => CustomizationFlags.ChassisColor, 
		};
	}
}
