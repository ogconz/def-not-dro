using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DB.Meta;
using DI;
using GameOverlay;
using SharingSetups;
using SyncMultiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

public static class UIUtils
{
	public interface IPreview
	{
		void UpdatePreview();
	}

	public interface IManaged
	{
		void LoadResources();

		void UnloadResources();
	}

	public class MoneyFormatProvider : IFormatProvider
	{
		private NumberFormatInfo _cacheFormat;

		public object GetFormat(Type formatType)
		{
			if (formatType != typeof(NumberFormatInfo))
			{
				return null;
			}
			if (_cacheFormat == null)
			{
				_cacheFormat = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
				_cacheFormat.NumberGroupSeparator = " ";
				_cacheFormat.NumberGroupSizes = new int[1] { 3 };
			}
			return _cacheFormat;
		}
	}

	public class PurchaseResultMessage
	{
		public enum EType
		{
			Default = 0,
			Error = 1,
			Money = 2,
			Fuel = 3
		}

		public EType type;

		public string title;

		public string text;

		public int coins;

		public int premium;

		public static PurchaseResultMessage AsError(string text)
		{
			return new PurchaseResultMessage
			{
				type = EType.Error,
				text = text
			};
		}
	}

	public static readonly string[] ResourcesPathToGuiPrefabs;

	public static readonly string[] SubpathsToGuiPrefabs;

	public const string ResourcesPathToUi = "UI/";

	public const string ResourcesPathToShopUi = "UI/Shop/";

	public const string ResourcesPathToShopActionsUiFolder = "Actions/";

	public const string ResourcesPathToShopActionsUi = "UI/Shop/Actions/";

	public const string ResourcesPathToDialogsUi = "UI/Dialogs/";

	public const string ResourcesPathToCatalogUi = "ui/catalog/";

	public const string ResourcesPathToGarageUi = "UI/Garage/";

	public const string ResourcesPathToLoadTrackIconsUi = "ui/tracks/trackpreviewfullhd/";

	public const string ResourcesPathToSelectTrackIconsUi = "ui/trackicons/selecttrack/";

	public const string ResourcesPathToSelectTrackNewIconsUi = "ui/tracks/";

	public const string ResourcesPathToSelectTrackNewIconsMinimapsUi = "ui/tracks/minimaps/";

	public const string ResourcesPathToSelectTrackMinimapsRoadPreview = "ui/tracks/minimapsonlyroad/";

	public const string ResourcesPathToSelectLobbyTrackNewIconsUi = "ui/lobby/tracks/";

	public const string ResourcesPathToCustomization = "ui/customization/";

	public const string ResourcesPathToCustomizationCategories = "ui/customization/categories/";

	public const string ResourcesPathToCustomizationRimsCategories = "ui/customization/categories/rimscategories/";

	public const string ResourcesPathToCustomizationParts = "ui/customization/prefabs/";

	public const string ResourcesPathToEngines = "ui/engines/";

	public const string ResourcesPathToCustomizationTires = "ui/customization/prefabs/tires/";

	public const string DialogsUiCash = "dialogs_header_cash";

	public const string DialogsUiCoins = "dialogs_header_coins";

	public const string DialogsUiRefuel = "multi_energy_refill";

	public const string DialogsUiIncraseMaxFuel = "multi_energy_increase";

	public const string DialogsUiCoinsCash = "dialogs_header_coins_cash";

	public const string DialogsUiVideo = "dialogs_header_video";

	private const string Texture2XPostfix = "~2x";

	public const string Texture2XWebPostfix = "_2x";

	public const string EmptySceneName = "Empty";

	public const string RaceLevelName = "Race";

	public const string PreviewCarIcon = "dialogs_ico_car";

	public const string PreviewProfileIcon = "dialogs_ico_setup";

	public const string PreviewTrackIcon = "dialogs_ico_track";

	public const string PreviewFuelIcon = "dialogs_ico_fuel";

	public const string DigitalSteeringLeft = "DigitalSteeringLeft";

	public const string DigitalSteeringRight = "DigitalSteeringRight";

	public const string AnalogSteering = "AnalogSteering";

	public const string DigitalThrottle = "DigitalThrottle";

	public const string AnalogThrottle = "AnalogThrottle";

	public const string DigitalBrake = "DigitalBrake";

	public const string AnalogBrake = "AnalogBrake";

	public const string AnalogClutch = "AnalogClutch";

	public const string Handbrake = "Handbrake";

	public const string GearShiftUp = "GearShiftUp";

	public const string GearShiftDn = "GearShiftDn";

	public const string RussianRubleSign = "₽";

	public static readonly MoneyFormatProvider numbersFormatter;

	private const string LINK_TAG = "link";

	private static HashSet<string> m_processImages;

	private static string[] OneNumSigns;

	private static string[] TwoNumSigns;

	private static string[] ThreeNumSigns;

	private const int MaxTwoNumSigns = 99;

	private const int MaxThreeNumSigns = 999;

	private const string SymColon = ":";

	private const string SymDot = ".";

	private const string EmptyTime = "--:--.---";

	private static readonly StringBuilder m_sb;

	private static readonly string[] DriftModeDescription;

	private static readonly string[] TimeModeDescription;

	private static readonly string[] FreeModeDescription;

	private static readonly string[] NetworkFreerideModeDescription;

	private static readonly string[] DynoModeDescription;

	private static readonly string[] XDSModeDescription;

	private static string[] blockedTags;

	private static StringBuilder m_builderHelper;

	private static string[] NGUI_END_TAG;

	public const char ESCAPE_CHAR = '\u001b';

	public const string EMPTY_FORMAT = "{0}";

	static UIUtils()
	{
		ResourcesPathToGuiPrefabs = new string[1] { "Prefabs/UI/Contexts/" };
		SubpathsToGuiPrefabs = new string[10] { "_UGUI_/", "Settings/", "Multiplayer/", "SyncMultiplayer/", "Shop/", "Banners/", "Catalog/", "Tutorial/", "Tutorial/Tutorial2/", "Race/" };
		numbersFormatter = new MoneyFormatProvider();
		m_processImages = new HashSet<string>();
		OneNumSigns = null;
		TwoNumSigns = null;
		ThreeNumSigns = null;
		m_sb = new StringBuilder(15);
		DriftModeDescription = new string[3] { "Drift", "Drift", "Earn maximum drift points and reach the finish line." };
		TimeModeDescription = new string[3] { "Time", "Time attack", "Reach finish line in the shortest time." };
		FreeModeDescription = new string[3] { "Training", "Training", "No limits, no rewards, just hone your skills." };
		NetworkFreerideModeDescription = new string[3] { "Network", "Network", "Be free to realize yourself." };
		DynoModeDescription = new string[3] { "Dyno", "Dyno", "Dyno" };
		XDSModeDescription = new string[3] { "XDS", "XDS", "XDS" };
		blockedTags = new string[20]
		{
			"size", "voffset", "b", "i", "s", "link", "sub", "sup", "tr", "u",
			"cspace", "rotate", "mspace", "pos", "noparse", "sprite", "gradient", "allcaps", "hex", "font"
		};
		m_builderHelper = new StringBuilder(32);
		NGUI_END_TAG = new string[3] { "[/url]", "[T]", "[/T]" };
		InitNumSigns();
	}

	public static Color HexToColor(string hex)
	{
		ColorUtility.TryParseHtmlString("#" + hex, out var color);
		return color;
	}

	public static Color HexToColor(uint val)
	{
		return IntToColor((int)val);
	}

	public static Color IntToColor(int val)
	{
		float num = 0.003921569f;
		Color black = Color.black;
		black.r = num * (float)((val >> 24) & 0xFF);
		black.g = num * (float)((val >> 16) & 0xFF);
		black.b = num * (float)((val >> 8) & 0xFF);
		black.a = num * (float)(val & 0xFF);
		return black;
	}

	public static bool IsPointerOverGameObject()
	{
		EventSystem current = EventSystem.current;
		if (!current)
		{
			return false;
		}
		return current.IsPointerOverGameObject();
	}

	public static Transform FindTransformByIndex(GameObject owner, int index)
	{
		if (owner == null)
		{
			return null;
		}
		GameObject gameObject = owner.FindChildWithName($"{index}");
		if (!(gameObject != null))
		{
			return null;
		}
		return gameObject.transform;
	}

	public static void LoadResources(BaseContext context)
	{
		if (context is IManaged managed)
		{
			managed.LoadResources();
		}
	}

	public static void UnloadResources(BaseContext context)
	{
		if (context is IManaged managed)
		{
			managed.UnloadResources();
		}
	}

	public static void UnloadSprite(Sprite sprite)
	{
		ResourcesWrapper.UnloadAsset(sprite);
	}

	public static void UnloadTex(Texture2D tex)
	{
		ResourcesWrapper.UnloadAsset(tex);
	}

	public static Sprite CreateSprite(Texture2D texture)
	{
		Sprite obj = ((texture != null) ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f)) : Sprite.Create(null, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f)));
		obj.hideFlags = HideFlags.HideAndDontSave;
		return obj;
	}

	public static Texture2D CreateTextureFromPath(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		byte[] data = File.ReadAllBytes(path);
		Texture2D texture2D = new Texture2D(2, 2, TextureFormat.ARGB32, mipChain: false, linear: false);
		texture2D.LoadImage(data);
		texture2D.hideFlags = HideFlags.HideAndDontSave;
		return texture2D;
	}

	public static async Task<Texture2D> CreateTextureFromPathAsync(string path, CancellationToken token, int maxWidth = 1, int maxHeight = 1)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		if (m_processImages.Contains(path))
		{
			return null;
		}
		Texture2D iconTexture = null;
		m_processImages.Add(path);
		Task<byte[]> byteLoad = Task.Run(() => LoadAsyncByteFile(path), token);
		await byteLoad;
		token.ThrowIfCancellationRequested();
		if (byteLoad.Result != null)
		{
			m_processImages.Remove(path);
			iconTexture = new Texture2D(maxWidth, maxHeight, TextureFormat.ARGB32, mipChain: false, linear: false);
			iconTexture.LoadImage(byteLoad.Result, markNonReadable: true);
			iconTexture.hideFlags = HideFlags.HideAndDontSave;
		}
		return iconTexture;
	}

	public static async Task<byte[]> LoadAsyncByteFile(string path)
	{
		using (FileStream sourceStream = File.Open(path, FileMode.Open, FileAccess.Read))
		{
			sourceStream.Seek(0L, SeekOrigin.Begin);
			byte[] buffer = new byte[sourceStream.Length];
			Task<int> readAsync = sourceStream.ReadAsync(buffer, 0, buffer.Length);
			await readAsync;
			sourceStream.Close();
			sourceStream.Dispose();
			if (readAsync.IsCompleted)
			{
				return buffer;
			}
		}
		return null;
	}

	public static IEnumerator DownloadSprite(string url, Action<Sprite, Texture2D> callback)
	{
		using UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
		yield return webRequest.SendWebRequest();
		if (webRequest.result != UnityWebRequest.Result.Success)
		{
			callback.SafeCall(null, null);
			yield break;
		}
		Texture2D content = DownloadHandlerTexture.GetContent(webRequest);
		callback(CreateSprite(content), content);
	}

	public static void DownloadTexture(string url, Action<Texture2D> callback, int width = 1, int height = 1, CancellationTokenSource ctsSource = null)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			callback.SafeCall(null);
			return;
		}
		DownloadByte(url, delegate(byte[] textureData)
		{
			Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, mipChain: false, linear: false);
			texture2D.LoadImage(textureData, markNonReadable: true);
			texture2D.hideFlags = HideFlags.HideAndDontSave;
			callback.SafeCall(texture2D);
		}, ctsSource).Forget();
	}

	public static async UniTask DownloadByte(string url, Action<byte[]> callback, CancellationTokenSource ctsSource = null)
	{
		using UnityWebRequest webRequest = UnityWebRequest.Get(url);
		_ = 1;
		try
		{
			if (ctsSource == null)
			{
				await webRequest.SendWebRequest();
			}
			else
			{
				await webRequest.SendWebRequest().WithCancellation(ctsSource.Token).SuppressCancellationThrow();
			}
		}
		catch
		{
			callback.SafeCall(null);
			return;
		}
		if (webRequest.result != UnityWebRequest.Result.Success || (ctsSource != null && ctsSource.IsCancellationRequested))
		{
			callback.SafeCall(null);
		}
		else
		{
			callback.SafeCall(webRequest.downloadHandler.data);
		}
	}

	public static int IndexOf(this StringBuilder sb, char c, int startIndex = 0)
	{
		int i = startIndex;
		for (int length = sb.Length; i < length; i++)
		{
			if (sb[i] == c)
			{
				return i;
			}
		}
		return -1;
	}

	public static int IndexOf(this StringBuilder sb, string value, int startIndex = 0)
	{
		int length = value.Length;
		int num = sb.Length - length + 1;
		for (int i = startIndex; i < num; i++)
		{
			if (sb[i] == value[0])
			{
				int j;
				for (j = 1; j < length && sb[i + j] == value[j]; j++)
				{
				}
				if (j == length)
				{
					return i;
				}
			}
		}
		return -1;
	}

	public static string Substring(this StringBuilder sb, int startIndex, int lenght)
	{
		m_sb.Clear();
		int i = startIndex;
		for (int num = Mathf.Min(startIndex + lenght, sb.Length); i < num; i++)
		{
			m_sb.Append(sb[i]);
		}
		return m_sb.PopString();
	}

	public static void Clear(this StringBuilder sb)
	{
		sb.Length = 0;
		for (int i = 0; i < sb.Capacity; i++)
		{
			sb.Append(' ');
		}
		sb.Length = 0;
	}

	public static string PopString(this StringBuilder sb)
	{
		string result = sb.ToString();
		sb.Clear();
		return result;
	}

	private static string[] CreateNumSigns(int maxLen, string formatter)
	{
		int num = maxLen + 1;
		string[] array = new string[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = i.ToString(formatter);
		}
		return array;
	}

	private static void InitNumSigns()
	{
		OneNumSigns = CreateNumSigns(9, "0");
		TwoNumSigns = CreateNumSigns(99, "00");
		ThreeNumSigns = CreateNumSigns(999, "000");
	}

	public static string NumToTwoSignString(int value)
	{
		value = Mathf.Clamp(value, 0, 99);
		return TwoNumSigns[value];
	}

	public static string NumToTreeSignString(int value)
	{
		value = Mathf.Clamp(value, 0, 999);
		return ThreeNumSigns[value];
	}

	public static string NumToString(uint value)
	{
		if (value < 10)
		{
			return OneNumSigns[value];
		}
		if (value < 100)
		{
			return TwoNumSigns[value];
		}
		if (value < 1000)
		{
			return ThreeNumSigns[value];
		}
		return value.ToString();
	}

	public static string TimeToHH_MMString(int h, int m)
	{
		m_sb.Clear();
		m_sb.Append(NumToTwoSignString(h));
		m_sb.Append(":");
		m_sb.Append(NumToTwoSignString(m));
		return m_sb.PopString();
	}

	public static string TimeToHMSString(float timeSeconds)
	{
		ParceTime(timeSeconds, out var hour, out var min, out var sec, out var _);
		m_sb.Clear();
		m_sb.Append(NumToTwoSignString(hour));
		m_sb.Append(":");
		m_sb.Append(NumToTwoSignString(min));
		m_sb.Append(":");
		m_sb.Append(NumToTwoSignString(sec));
		return m_sb.PopString();
	}

	public static string TimeToMSString(float timeSeconds)
	{
		ParceTime(timeSeconds, out var _, out var min, out var sec, out var _);
		m_sb.Clear();
		m_sb.Append(NumToTwoSignString(min));
		m_sb.Append(":");
		m_sb.Append(NumToTwoSignString(sec));
		return m_sb.PopString();
	}

	public static string TimeToHMSMString(float? time)
	{
		if (time.HasValue)
		{
			ParceTime(time.Value, out var hour, out var min, out var sec, out var millis);
			m_sb.Clear();
			if (hour > 0)
			{
				m_sb.Append(NumToTwoSignString(hour));
				m_sb.Append(":");
			}
			m_sb.Append(NumToTwoSignString(min));
			m_sb.Append(":");
			m_sb.Append(NumToTwoSignString(sec));
			m_sb.Append(".");
			m_sb.Append(NumToTreeSignString(millis));
			return m_sb.PopString();
		}
		return "--:--.---";
	}

	public static string TimeToMSMString(float? time)
	{
		if (time.HasValue)
		{
			ParceTime(time.Value, out var _, out var min, out var sec, out var millis);
			m_sb.Clear();
			m_sb.Append(NumToTwoSignString(min));
			m_sb.Append(":");
			m_sb.Append(NumToTwoSignString(sec));
			m_sb.Append(".");
			m_sb.Append(NumToTreeSignString(millis));
			return m_sb.PopString();
		}
		return "--:--.---";
	}

	private static void ParceTime(float timeSeconds, out int hour, out int min, out int sec, out int millis)
	{
		hour = Mathf.FloorToInt(timeSeconds);
		sec = hour % 60;
		hour /= 60;
		min = hour % 60;
		hour /= 60;
		millis = Mathf.FloorToInt((timeSeconds - (float)(min * 60) - (float)(hour * 3600) - (float)sec) * 1000f);
	}

	public static bool ParseTime(string time, out float result)
	{
		result = 0f;
		Match match = Regex.Match(time, "(\\d\\d):(\\d\\d)\\.(\\d\\d\\d)");
		if (!match.Success)
		{
			return false;
		}
		int num = int.Parse(match.Groups[1].Value);
		int num2 = int.Parse(match.Groups[2].Value);
		int num3 = int.Parse(match.Groups[3].Value);
		result = (float)(num * 60 + num2) + (float)(num3 - 500) / 1000f;
		return true;
	}

	public static string GetDayString(int days)
	{
		if (days == 0)
		{
			return LocalizationKeys.subs_last_days2.Localized();
		}
		if (days == 1)
		{
			return LocalizationKeys.subs_last_day.Localized();
		}
		if (days < 5)
		{
			return LocalizationKeys.subs_last_days.Localized();
		}
		return LocalizationKeys.subs_last_days2.Localized();
	}

	public static Texture2D LoadUITexture(string path)
	{
		return ResourcesWrapper.Load<Texture2D>(path);
	}

	public static Sprite LoadUISprite(string path)
	{
		return ResourcesWrapper.Load<Sprite>(path);
	}

	public static Color GetColorFromMetaIgnoreAlpha(BaseColorMeta colorMeta)
	{
		Color? color = colorMeta.color;
		if (!color.HasValue)
		{
			foreach (ShaderParameter shaderParam in colorMeta.materialParams.shaderParams)
			{
				if (shaderParam is ShaderParameterColor)
				{
					ShaderParameterColor shaderParameterColor = (ShaderParameterColor)shaderParam;
					color = shaderParameterColor.value;
					break;
				}
			}
		}
		if (!color.HasValue)
		{
			throw new InvalidOperationException(colorMeta.GetType().Name + " " + colorMeta.id + " has no color!");
		}
		Color value = color.Value;
		value.a = 1f;
		return value;
	}

	public static void AddChild(GameObject parent, GameObject child)
	{
		if (child != null && parent != null)
		{
			Transform transform = child.transform;
			transform.parent = parent.transform;
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;
			child.layer = parent.layer;
		}
	}

	public static string DigitsWithSeparate(int value)
	{
		if (value >= 0 && value < 1000)
		{
			return NumToString((uint)value);
		}
		return value.ToString("#,0", numbersFormatter);
	}

	public static string DigitsWithSeparate(float value)
	{
		return value.ToString("F2", numbersFormatter);
	}

	public static ECurrencyType GetMoneyType(bool isPremium)
	{
		if (!isPremium)
		{
			return ECurrencyType.Coins;
		}
		return ECurrencyType.Premium;
	}

	public static ECurrencyType GetMoneyType(this IDBCanCost canCost)
	{
		return GetMoneyType(isPremium: false);
	}

	public static string AddWhiteDollarSignAtStart(string cost)
	{
		return "<color=white>$</color> " + cost;
	}

	public static bool IsCarVisible(GamePrefs prefs, PlayerCar carData)
	{
		if (prefs.locks.GetCarLockState(carData.id) && carData.forcedDisable)
		{
			return false;
		}
		return carData.enabled;
	}

	private static string[] GetRaceModeInfo(RaceMode type)
	{
		return type switch
		{
			RaceMode.Drift => DriftModeDescription, 
			RaceMode.TimeAttack => TimeModeDescription, 
			RaceMode.Freeride => FreeModeDescription, 
			RaceMode.Dyno => DynoModeDescription, 
			RaceMode.NetworkFreeride => NetworkFreerideModeDescription, 
			RaceMode.XDS => XDSModeDescription, 
			_ => throw new InvalidOperationException(), 
		};
	}

	public static string GetRaceModeName(RaceMode type)
	{
		return GetRaceModeInfo(type)[0];
	}

	public static string GetRaceModeLocalizedName(RaceMode type)
	{
		return type switch
		{
			RaceMode.Drift => LocalizationKeys.track_mode_drift_title.Localized(), 
			RaceMode.TimeAttack => LocalizationKeys.track_mode_time_attack_title.Localized(), 
			RaceMode.Freeride => (UISetupsSharingContext.isSetupsSharingActive ? LocalizationKeys.nav_hint_goto_testdrive : LocalizationKeys.singlemode_training).Localized(), 
			RaceMode.Dyno => LocalizationKeys.track_mode_dyno_title.Localized(), 
			RaceMode.XDS => LocalizationKeys.track_mode_xds_title.Localized(), 
			RaceMode.NetworkFreeride => LocalizationKeys.screen_title_multiplayer.Localized(), 
			_ => string.Empty, 
		};
	}

	public static string GetRaceModeTitle(RaceMode type)
	{
		return GetRaceModeInfo(type)[1];
	}

	public static string GetRaceModeDescription(RaceMode type)
	{
		return GetRaceModeInfo(type)[2];
	}

	public static RaceResult GetRaceResult(int track, RaceMode raceMode)
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		int profileIdForCar = gamePrefs.carSettings.GetProfileIdForCar(gamePrefs.globals.lastCarId);
		return gamePrefs.stats.GetRaceResult(track, gamePrefs.globals.lastCarId, profileIdForCar, raceMode);
	}

	public static string MakeViewBestDriftPoints(int track)
	{
		RaceResult raceResult = GetRaceResult(track, RaceMode.Drift);
		if (raceResult == null)
		{
			return "-----";
		}
		return DigitsWithSeparate(raceResult.recordDriftPoints);
	}

	public static string MakeViewBestTimeAttackPoints(int track)
	{
		RaceResult raceResult = GetRaceResult(track, RaceMode.TimeAttack);
		if (raceResult == null)
		{
			return "--:--.---";
		}
		return TimeToMSMString(raceResult.GetBestLapTime());
	}

	public static bool IsMatchRegularExpression(string text, string pattern)
	{
		return Regex.IsMatch(text, pattern);
	}

	public static string FilterInvalidTags(string name, bool skipLink = false)
	{
		if (string.IsNullOrEmpty(name))
		{
			return string.Empty;
		}
		string[] array = blockedTags;
		foreach (string text in array)
		{
			if (skipLink && string.Equals(text, "link", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string value = "<" + text;
			string text2 = ">";
			string text3 = "</" + text + ">";
			string text4 = name.ToLower();
			while (text4.Contains(value))
			{
				int num = text4.IndexOf(value);
				int num2 = text4.IndexOf(text2, num);
				if (num2 < 0)
				{
					break;
				}
				string text5 = name.Substring(0, num);
				string text6 = name.Substring(num2 + text2.Length);
				name = text5 + text6;
				text4 = name.ToLower();
			}
			while (text4.Contains(text3))
			{
				int num3 = text4.IndexOf(text3);
				string text7 = name.Substring(0, num3);
				string text8 = name.Substring(num3 + text3.Length);
				name = text7 + text8;
				text4 = name.ToLower();
			}
		}
		return name;
	}

	public static string FilterInvalidChars(this string input)
	{
		return input.Replace("\\n", string.Empty).Replace("\n", string.Empty).Replace("\\r", string.Empty)
			.Replace("\r", string.Empty)
			.Replace("\\v", string.Empty)
			.Replace("\v", string.Empty)
			.Replace("\\t", string.Empty)
			.Replace("\t", string.Empty);
	}

	public static string FilterText(string input)
	{
		return FilterInvalidTags(input).FilterInvalidChars();
	}

	public static void LogNullReference(string methodName, string paramName)
	{
		UnityEngine.Debug.LogError($"{methodName} ({paramName} == null)");
	}

	public static string MakeGhostExpiredUiInfo(MultiplayerGhost ghost)
	{
		if ((int)ghost.secondsTillOutdate.GetValueOrDefault() > 0)
		{
			return LocalizationKeys.mp_qualify_end_ghost_life_time.LocalizedFormat(TimeToString(ghost.secondsTillOutdate));
		}
		return LocalizationKeys.mp_qualify_end_ghost_expired.Localized();
	}

	public static int ConvertTime(float? value)
	{
		int num = (int)value.GetValueOrDefault();
		if (num < 0)
		{
			num = 0;
		}
		return num;
	}

	public static string TimeToString(float? numberOfSeconds)
	{
		return TimeToHMSString(numberOfSeconds.GetValueOrDefault());
	}

	public static string GetDayAndMonth(DateTime dateTime)
	{
		string key = $"month_{dateTime.Month}";
		return $"{dateTime.Day} {LocalizationService.Localize(key)}";
	}

	public static string GetPlatfomIconTagIfNeed(UserPlatform.Id platformId)
	{
		if (NetworkController.InstanceGame.IsCrossPlatformGloballyEnabled() && UserPlatform.IsNeedShowLocalPlatformIcon(platformId))
		{
			if (platformId == UserPlatform.Id.MyGames || platformId == UserPlatform.Id.EpicGames)
			{
				platformId = UserPlatform.Id.Steam;
			}
			return $"<sprite name=\"{platformId}\"/>";
		}
		return string.Empty;
	}

	public static string ConvertNGUIColorToRich(string inputText)
	{
		bool flag = false;
		m_builderHelper.Clear();
		m_builderHelper.Append(inputText);
		int num = 0;
		while (num < m_builderHelper.Length)
		{
			num = m_builderHelper.IndexOf('[', num);
			if (num < 0)
			{
				break;
			}
			num++;
			int num2 = m_builderHelper.IndexOf(']', num);
			int num3 = num2 - num;
			if (num2 < 0 || (num3 != 6 && num3 != 8))
			{
				continue;
			}
			string text = m_builderHelper.Substring(num, num3).ToUpper();
			Color c = ((num3 == 6) ? ParseColor24(text) : ParseColor32(text, 0));
			if (((num3 == 6) ? EncodeColor24(c) : EncodeColor32(c)) == text)
			{
				if (flag)
				{
					m_builderHelper.Insert(num - 1, "</color>");
					num += "</color>".Length;
					num2 += "</color>".Length;
				}
				string text2 = $"<color=#{text}>";
				m_builderHelper.Remove(num - 1, num2 - num + 2);
				m_builderHelper.Insert(num - 1, text2);
				flag = true;
				num += text2.Length;
			}
		}
		if (flag)
		{
			m_builderHelper.Insert(m_builderHelper.Length, "</color>");
		}
		m_builderHelper.Replace('\u001b'.ToString(), string.Empty);
		m_builderHelper.Replace("[+]", string.Empty);
		m_builderHelper.Replace("[-]", string.Empty);
		m_builderHelper.Replace("[T]", string.Empty);
		m_builderHelper.Replace("[/T]", string.Empty);
		return m_builderHelper.ToString();
	}

	public static string ConvertNGUIToTMPTags(string inputText)
	{
		m_builderHelper.Clear();
		m_builderHelper.Append(inputText);
		bool flag = false;
		int num = 0;
		while (true)
		{
			num = m_builderHelper.IndexOf('[', num);
			if (num < 0)
			{
				break;
			}
			num++;
			int num2 = m_builderHelper.IndexOf(']', num);
			int num3 = num2 - num;
			if (num2 < 0 || (num3 != 6 && num3 != 8))
			{
				continue;
			}
			string text = m_builderHelper.Substring(num, num3).ToUpper();
			Color c = ((num3 == 6) ? ParseColor24(text) : ParseColor32(text, 0));
			if (!(((num3 == 6) ? EncodeColor24(c) : EncodeColor32(c)) == text))
			{
				continue;
			}
			if (flag)
			{
				m_builderHelper.Insert(num - 1, "</color>");
				num += "</color>".Length;
				num2 += "</color>".Length;
			}
			string text2 = $"<color=#{text}>";
			m_builderHelper.Remove(num - 1, num3 + 2);
			m_builderHelper.Insert(num - 1, text2);
			num2 = num + text2.Length;
			int num4 = m_builderHelper.IndexOf("</link>", num2 + 1);
			if (num4 >= 0)
			{
				m_builderHelper.Insert(num4, "</color>");
				num = num4 + "</color>".Length + "</link>".Length;
				continue;
			}
			flag = true;
			string[] nGUI_END_TAG = NGUI_END_TAG;
			foreach (string text3 in nGUI_END_TAG)
			{
				int num5 = m_builderHelper.IndexOf(text3, num2 + 1);
				if (num5 >= 0)
				{
					m_builderHelper.Remove(num5, text3.Length);
					m_builderHelper.Insert(num5, "</color>");
					num = num5 + "</color>".Length;
					flag = false;
					break;
				}
			}
		}
		if (flag)
		{
			m_builderHelper.Insert(m_builderHelper.Length, "</color>");
		}
		int num6 = 0;
		while (true)
		{
			num6 = m_builderHelper.IndexOf("[url", num6);
			if (num6 < 0)
			{
				break;
			}
			m_builderHelper.Remove(num6, "[url".Length);
			m_builderHelper.Insert(num6, "<link");
			int num7 = m_builderHelper.IndexOf(']', num6 + "<link".Length);
			if (num7 >= 0)
			{
				m_builderHelper.Remove(num7, 1);
				m_builderHelper.Insert(num7, ">");
				num6 = num7 + 1;
			}
			num7 = m_builderHelper.IndexOf("[/url]", num6);
			if (num7 >= 0)
			{
				m_builderHelper.Remove(num7, "[/url]".Length);
				m_builderHelper.Insert(num7, "</link>");
				num6 = num7 + "</link>".Length;
			}
		}
		m_builderHelper.Replace('\u001b'.ToString(), string.Empty);
		m_builderHelper.Replace("[+]", string.Empty);
		m_builderHelper.Replace("[-]", string.Empty);
		m_builderHelper.Replace("[T]", string.Empty);
		m_builderHelper.Replace("[/T]", string.Empty);
		return NGUISymbolsHolder.TMP_PreparseString(m_builderHelper.ToString());
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public static int HexToDecimal(char ch)
	{
		switch (ch)
		{
		case '0':
			return 0;
		case '1':
			return 1;
		case '2':
			return 2;
		case '3':
			return 3;
		case '4':
			return 4;
		case '5':
			return 5;
		case '6':
			return 6;
		case '7':
			return 7;
		case '8':
			return 8;
		case '9':
			return 9;
		case 'A':
		case 'a':
			return 10;
		case 'B':
		case 'b':
			return 11;
		case 'C':
		case 'c':
			return 12;
		case 'D':
		case 'd':
			return 13;
		case 'E':
		case 'e':
			return 14;
		case 'F':
		case 'f':
			return 15;
		default:
			return 15;
		}
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public static Color ParseColor24(string text, int offset = 0)
	{
		int num = (HexToDecimal(text[offset]) << 4) | HexToDecimal(text[offset + 1]);
		int num2 = (HexToDecimal(text[offset + 2]) << 4) | HexToDecimal(text[offset + 3]);
		int num3 = (HexToDecimal(text[offset + 4]) << 4) | HexToDecimal(text[offset + 5]);
		float num4 = 0.003921569f;
		return new Color(num4 * (float)num, num4 * (float)num2, num4 * (float)num3);
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public static Color ParseColor32(string text, int offset)
	{
		int num = (HexToDecimal(text[offset]) << 4) | HexToDecimal(text[offset + 1]);
		int num2 = (HexToDecimal(text[offset + 2]) << 4) | HexToDecimal(text[offset + 3]);
		int num3 = (HexToDecimal(text[offset + 4]) << 4) | HexToDecimal(text[offset + 5]);
		int num4 = (HexToDecimal(text[offset + 6]) << 4) | HexToDecimal(text[offset + 7]);
		float num5 = 0.003921569f;
		return new Color(num5 * (float)num, num5 * (float)num2, num5 * (float)num3, num5 * (float)num4);
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public static string EncodeColor24(Color c)
	{
		return DecimalToHex24(0xFFFFFF & (ColorToInt(c) >> 8));
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public static string EncodeColor32(Color c)
	{
		return DecimalToHex32(ColorToInt(c));
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public static int ColorToInt(Color c)
	{
		return 0 | (Mathf.RoundToInt(c.r * 255f) << 24) | (Mathf.RoundToInt(c.g * 255f) << 16) | (Mathf.RoundToInt(c.b * 255f) << 8) | Mathf.RoundToInt(c.a * 255f);
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public static string DecimalToHex24(int num)
	{
		num &= 0xFFFFFF;
		return num.ToString("X6");
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public static string DecimalToHex32(int num)
	{
		return num.ToString("X8");
	}

	public static void SetActive(GameObject go, bool state)
	{
		SetActive(go, state, compatibilityMode: true);
	}

	public static void SetActive(GameObject go, bool state, bool compatibilityMode)
	{
		if ((bool)go)
		{
			if (state)
			{
				Activate(go.transform, compatibilityMode);
			}
			else
			{
				go.SetActive(value: false);
			}
		}
	}

	private static void Activate(Transform t, bool compatibilityMode)
	{
		t.gameObject.SetActive(value: true);
		if (!compatibilityMode)
		{
			return;
		}
		int i = 0;
		for (int childCount = t.childCount; i < childCount; i++)
		{
			if (t.GetChild(i).gameObject.activeSelf)
			{
				return;
			}
		}
		int j = 0;
		for (int childCount2 = t.childCount; j < childCount2; j++)
		{
			Activate(t.GetChild(j), compatibilityMode: true);
		}
	}

	public static void SetActiveChildren(GameObject go, bool state)
	{
		Transform transform = go.transform;
		if (state)
		{
			int i = 0;
			for (int childCount = transform.childCount; i < childCount; i++)
			{
				Activate(transform.GetChild(i), compatibilityMode: false);
			}
		}
		else
		{
			int j = 0;
			for (int childCount2 = transform.childCount; j < childCount2; j++)
			{
				transform.GetChild(j).gameObject.SetActive(value: false);
			}
		}
	}

	public static void DestroyChildren(this Transform t)
	{
		bool isPlaying = Application.isPlaying;
		while (t.childCount != 0)
		{
			Transform child = t.GetChild(0);
			if (isPlaying)
			{
				child.SetParent(null);
				UnityEngine.Object.Destroy(child.gameObject);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(child.gameObject);
			}
		}
	}

	public static void ReparentChildren(this Transform t, Transform newParent)
	{
		_ = Application.isPlaying;
		while (t.childCount != 0)
		{
			t.GetChild(0).SetParent(newParent, worldPositionStays: false);
		}
	}
}
