using System;
using CarX;
using UnityEngine;

[AddComponentMenu("")]
public class SetupCarDescUI : MonoBehaviour
{
	private class ProfileInfo
	{
		private GUIStyle m_debugTextStyle;

		private Rect m_debugTextRect;

		private string m_topSpeed;

		private string m_accelTo100kmh;

		private string m_maxPower;

		public ProfileInfo()
		{
			m_debugTextStyle = new GUIStyle();
			m_debugTextStyle.alignment = TextAnchor.MiddleRight;
			m_debugTextStyle.fontSize = 20;
		}

		public void RecalcParams(BaseCar car)
		{
			if (car != null)
			{
				car.carX.CalcMaxPowerMaxTorque(out var max_hp, out var _, out var _, out var _);
				m_topSpeed = Mathf.RoundToInt(car.carX.CalcTopSpeed() * 3.6f).ToString();
				m_accelTo100kmh = car.carX.CalcAccelTime(0f, 27.777779f).ToString("F2");
				m_maxPower = Mathf.RoundToInt(max_hp).ToString("F2");
			}
		}

		public void Draw()
		{
			int depth = GUI.depth;
			GUI.depth = 1000;
			float x = (float)Screen.width * 0.5f - 150f;
			float num = (float)Screen.height * 0.5f;
			m_debugTextRect.Set(x, num, 300f, 30f);
			GUI.Label(m_debugTextRect, "Top speed - " + m_topSpeed, m_debugTextStyle);
			num += 30f;
			m_debugTextRect.Set(x, num, 300f, 30f);
			GUI.Label(m_debugTextRect, "To100Kmh - " + m_accelTo100kmh, m_debugTextStyle);
			num += 30f;
			m_debugTextRect.Set(x, num, 300f, 30f);
			GUI.Label(m_debugTextRect, $"Power - {m_maxPower}", m_debugTextStyle);
			GUI.depth = depth;
		}
	}

	public RaceCar car;

	private bool m_show;

	private bool m_changed;

	private string[] m_toolbarStrings = new string[6] { "Suspension", "Transmission", "Engine", "Wheels", "Brakes", "Body" };

	private int m_toolbarSelected;

	private string[] m_gearTypeList;

	private string[] m_numGearsList;

	private string[] m_tyreTypeList;

	private string[] m_tyreProfileNames = new string[12]
	{
		"30", "35", "40", "45", "50", "55", "60", "65", "70", "75",
		"80", "85"
	};

	private int[] m_tyreProfileValues = new int[12]
	{
		30, 35, 40, 45, 50, 55, 60, 65, 70, 75,
		80, 85
	};

	private string[] m_tyreWidthNames = new string[18]
	{
		"165", "175", "185", "195", "215", "225", "235", "245", "255", "265",
		"285", "325", "335", "345", "385", "425", "600", "800"
	};

	private int[] m_tyreWidthValues = new int[18]
	{
		165, 175, 185, 195, 215, 225, 235, 245, 255, 265,
		285, 325, 335, 345, 385, 425, 600, 800
	};

	private string[] m_suspTypeList;

	private ProfileInfo m_profileInfo;

	private const float sw = 200f;

	private const float sh = 60f;

	private const float ph = 8f;

	private const float pv = 8f;

	private void ProfileRecalcParams()
	{
		m_profileInfo.RecalcParams(car);
	}

	private void OnShowChanged()
	{
		if (m_show)
		{
			ProfileRecalcParams();
		}
	}

	private void Awake()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void BodyUI(BaseCar.Desc desc)
	{
		desc.carXDesc.weight.mass = FloatField(0, 0, leftAlign: true, "Mass", "Kg", desc.carXDesc.weight.mass, 100f, 10000f);
		desc.carXDesc.weight.frontPercent = FloatField(0, 1, leftAlign: true, "Front Percent", "%", desc.carXDesc.weight.frontPercent, 0f, 100f);
		desc.carXDesc.weight.upPercent = FloatField(0, 2, leftAlign: true, "Top Percent", "%", desc.carXDesc.weight.upPercent, 0f, 100f);
		desc.carXDesc.weight.rightPercent = FloatField(0, 3, leftAlign: true, "Right Percent", "%", desc.carXDesc.weight.rightPercent, 0f, 100f);
		desc.carXDesc.aero.cx = FloatField(1, 0, leftAlign: true, "Air Resistance Cx", "K", desc.carXDesc.aero.cx, 0f, 5f);
		desc.carXDesc.aero.sx = FloatField(1, 1, leftAlign: true, "Car Sx", "K", desc.carXDesc.aero.sx, 0f, 5f);
		desc.carXDesc.aero.frontDownforce = FloatField(1, 2, leftAlign: true, "Front Downforce", "Kg", desc.carXDesc.aero.frontDownforce, -100f, 100f);
		desc.carXDesc.aero.rearDownforce = FloatField(1, 3, leftAlign: true, "Rear Downforce", "Kg", desc.carXDesc.aero.rearDownforce, -100f, 100f);
	}

	private void TransmissionUI(BaseCar.Desc desc)
	{
		desc.carXDesc.transmission.gearType = (GearType)List(0, 0, leftAlign: true, "Gear Type", "", (int)desc.carXDesc.transmission.gearType, m_gearTypeList);
		desc.carXDesc.transmission.frontDiffViscoCoef = FloatField(0, 1, leftAlign: true, "Diff Lock Coef", "K", desc.carXDesc.transmission.frontDiffViscoCoef, 0f, 1000f);
		desc.carXDesc.transmission.rearDiffViscoCoef = desc.carXDesc.transmission.frontDiffViscoCoef;
		desc.carXDesc.transmission.clutchMaxTorque = FloatField(0, 2, leftAlign: true, "Cluth Max Torque", "N*m", desc.carXDesc.transmission.clutchMaxTorque, 0f, 10000f);
		desc.carXDesc.gearBox.finalDrive = FloatField(0, 5, leftAlign: true, "Final Drive", "", desc.carXDesc.gearBox.finalDrive, 0.1f, 20f);
		desc.carXDesc.gearBox.gearUpRPM = FloatField(0, 3, leftAlign: true, "Gear Up RPM", "RPM", desc.carXDesc.gearBox.gearUpRPM, 0f, 20000f);
		desc.carXDesc.gearBox.gearDownRPM = FloatField(0, 4, leftAlign: true, "Gear Down RPM", "RPM", desc.carXDesc.gearBox.gearDownRPM, 0f, 20000f);
		desc.carXDesc.gearBox.gearR = FloatField(0, 6, leftAlign: true, "Gear Rear", "", desc.carXDesc.gearBox.gearR, 0.1f, 20f);
		desc.carXDesc.gearBox.standingStartRPMUp = FloatField(0, 7, leftAlign: true, "Standing Start RPM Up", "", desc.carXDesc.gearBox.standingStartRPMUp, 150f, 8000f);
		desc.carXDesc.gearBox.standingStartRPMDown = FloatField(0, 8, leftAlign: true, "Standing Start RPM Down", "", desc.carXDesc.gearBox.standingStartRPMDown, 100f, 5000f);
		int num = 1 + List(1, 0, leftAlign: true, "Num Gears", "", desc.carXDesc.gearBox.gears.Length - 1, m_numGearsList);
		if (num != desc.carXDesc.gearBox.gears.Length)
		{
			Array.Resize(ref desc.carXDesc.gearBox.gears, num);
		}
		for (int i = 0; i < desc.carXDesc.gearBox.gears.Length; i++)
		{
			desc.carXDesc.gearBox.gears[i] = FloatField(1 + (1 + i) / 7, (1 + i) % 7, leftAlign: true, $"Gear {i + 1}", "", desc.carXDesc.gearBox.gears[i], 0.1f, 20f);
		}
	}

	private void EngineUI(BaseCar.Desc desc)
	{
		desc.carXDesc.engine.maxTorque = FloatField(0, 0, leftAlign: true, "Max Torque", "N*m", desc.carXDesc.engine.maxTorque, 1f, 1000f);
		desc.carXDesc.engine.maxTorqueRPM = FloatField(0, 1, leftAlign: true, "Max Torque RPM", "rev/min", desc.carXDesc.engine.maxTorqueRPM, 1000f, 10000f);
		desc.carXDesc.engine.brakeTorqueRatio = FloatField(0, 2, leftAlign: true, "Brake Torque Ratio", "", desc.carXDesc.engine.brakeTorqueRatio, 0f, 0.2f);
		desc.carXDesc.engine.inertiaRatio = FloatField(0, 3, leftAlign: true, "Inertia Ratio", "", desc.carXDesc.engine.inertiaRatio, 0.1f, 10f);
		desc.carXDesc.engine.idleRPM = FloatField(0, 4, leftAlign: true, "Idle RPM", "rev/min", desc.carXDesc.engine.idleRPM, 400f, 3000f);
		desc.carXDesc.engine.cutRPM = FloatField(0, 5, leftAlign: true, "Cut RPM", "rev/min", desc.carXDesc.engine.cutRPM, 200f, 2000f);
		desc.carXDesc.engine.revLimiter = FloatField(0, 6, leftAlign: true, "Rev Limiter", "rev/min", desc.carXDesc.engine.revLimiter, 2000f, 12000f);
		desc.carXDesc.engine.turboCharged = Toggle(1, 0, leftAlign: true, "Turbo Charged", desc.carXDesc.engine.turboCharged);
		desc.carXDesc.engine.turboPressure = FloatField(1, 1, leftAlign: true, "Turbo Pressure", "", desc.carXDesc.engine.turboPressure, 0f, 2f);
		desc.carXDesc.engine.useTC = Toggle(1, 2, leftAlign: true, "Traction Control", desc.carXDesc.engine.useTC);
	}

	private void BrakesUI(BaseCar.Desc desc)
	{
		desc.carXDesc.brakes.maxTorque = FloatField(0, 0, leftAlign: true, "Max Brake Torque", "N*m", desc.carXDesc.brakes.maxTorque, 0f, 100000f);
		desc.carXDesc.brakes.frontPercent = FloatField(0, 1, leftAlign: true, "Front Percent", "%", desc.carXDesc.brakes.frontPercent, 0f, 100f);
		desc.carXDesc.brakes.useABS = Toggle(0, 2, leftAlign: true, "ABS", desc.carXDesc.brakes.useABS);
	}

	private void WheelsUI(BaseCar.Desc desc)
	{
		desc.carXDesc.frontTyre.tyreType = (TyreType)List(0, 0, leftAlign: true, "Tyre Type", "", (int)desc.carXDesc.frontTyre.tyreType, m_tyreTypeList);
		desc.carXDesc.frontTyre.profile = m_tyreProfileValues[List(0, 1, leftAlign: true, "Tyre Profile", "%", Index(m_tyreProfileValues, (int)desc.carXDesc.frontTyre.profile), m_tyreProfileNames)];
		desc.carXDesc.frontTyre.width = m_tyreWidthValues[List(0, 2, leftAlign: true, "Tyre Width", "mm", Index(m_tyreWidthValues, (int)desc.carXDesc.frontTyre.width), m_tyreWidthNames)];
		desc.carXDesc.frontTyre.pressure = FloatField(0, 3, leftAlign: true, "Tyre Pressure", "Pa", desc.carXDesc.frontTyre.pressure, 50000f, 1000000f);
		desc.carXDesc.frontTyre.discDiam = FloatField(0, 4, leftAlign: true, "Disk Radius", "inch", desc.carXDesc.frontTyre.discDiam, 1f, 50f);
		desc.frontBaseWidth = 0.001f * (float)IntField(0, 5, leftAlign: true, "BaseWidth", "mm", Mathf.RoundToInt(desc.frontBaseWidth * 1000f), 1000, 2500);
		desc.frontBaseZ = 0.001f * (float)IntField(0, 6, leftAlign: true, "BaseZ", "mm", Mathf.RoundToInt(desc.frontBaseZ * 1000f), 1000, 2000);
		desc.baseY = 0.001f * (float)IntField(1, 0, leftAlign: true, "BaseY", "mm", Mathf.RoundToInt(desc.baseY * 1000f), -1000, 1000);
		desc.carXDesc.rearTyre.tyreType = (TyreType)List(0, 0, leftAlign: false, "Tyre Type", "", (int)desc.carXDesc.rearTyre.tyreType, m_tyreTypeList);
		desc.carXDesc.rearTyre.profile = m_tyreProfileValues[List(0, 1, leftAlign: false, "Tyre Profile", "%", Index(m_tyreProfileValues, (int)desc.carXDesc.rearTyre.profile), m_tyreProfileNames)];
		desc.carXDesc.rearTyre.width = m_tyreWidthValues[List(0, 2, leftAlign: false, "Tyre Width", "mm", Index(m_tyreWidthValues, (int)desc.carXDesc.rearTyre.width), m_tyreWidthNames)];
		desc.carXDesc.rearTyre.pressure = FloatField(0, 3, leftAlign: false, "Tyre Pressure", "Pa", desc.carXDesc.rearTyre.pressure, 50000f, 1000000f);
		desc.carXDesc.rearTyre.discDiam = FloatField(0, 4, leftAlign: false, "Disk Radius", "inch", desc.carXDesc.rearTyre.discDiam, 1f, 50f);
		desc.rearBaseWidth = 0.001f * (float)IntField(0, 5, leftAlign: false, "BaseWidth", "mm", Mathf.RoundToInt(desc.rearBaseWidth * 1000f), 1000, 2500);
		desc.rearBaseZ = 0.001f * (float)IntField(0, 6, leftAlign: false, "BaseZ", "mm", Mathf.RoundToInt(desc.rearBaseZ * 1000f), 1000, 2000);
	}

	private void SuspensionUI(BaseCar.Desc desc)
	{
		desc.carXDesc.frontSuspension.suspType = (SuspensionType)List(0, 0, leftAlign: true, "Type", "", (int)desc.carXDesc.frontSuspension.suspType, m_suspTypeList);
		desc.carXDesc.frontSuspension.springLength = 0.001f * (float)IntField(0, 1, leftAlign: true, "Spring Way", "mm", Mathf.RoundToInt(desc.carXDesc.frontSuspension.springLength * 1000f), 50, 1000);
		desc.carXDesc.frontSuspension.stiffness = 1000f * FloatField(0, 2, leftAlign: true, "Stiffness", "kN/m", desc.carXDesc.frontSuspension.stiffness * 0.001f, 0f, 1000f);
		desc.carXDesc.frontSuspension.stabilizer = 1000f * FloatField(0, 3, leftAlign: true, "Stabilizer", "kN/m", desc.carXDesc.frontSuspension.stabilizer * 0.001f, 0f, 1000f);
		desc.carXDesc.frontSuspension.absorbCoef = 1000f * FloatField(0, 4, leftAlign: true, "AbsorbCoef", "Ns/mm", desc.carXDesc.frontSuspension.absorbCoef * 0.001f, 0f, 100f);
		desc.carXDesc.frontSuspension.slowBump = 1000f * FloatField(0, 5, leftAlign: true, "Slow Bump", "Ns/mm", desc.carXDesc.frontSuspension.slowBump * 0.001f, 0f, 100f);
		desc.carXDesc.frontSuspension.fastBump = 1000f * FloatField(0, 6, leftAlign: true, "Fast Bump", "Ns/mm", desc.carXDesc.frontSuspension.fastBump * 0.001f, 0f, 100f);
		desc.carXDesc.frontSuspension.slowRebound = 1000f * FloatField(0, 7, leftAlign: true, "Slow Rebound", "Ns/mm", desc.carXDesc.frontSuspension.slowRebound * 0.001f, 0f, 100f);
		desc.carXDesc.frontSuspension.fastRebound = 1000f * FloatField(0, 8, leftAlign: true, "Fast Rebound", "Ns/mm", desc.carXDesc.frontSuspension.fastRebound * 0.001f, 0f, 100f);
		desc.carXDesc.frontSuspension.toeIn = FloatField(1, 0, leftAlign: true, "Toe", "deg", desc.carXDesc.frontSuspension.toeIn, -15f, 15f);
		desc.carXDesc.frontSuspension.camber = FloatField(1, 1, leftAlign: true, "Camber", "deg", desc.carXDesc.frontSuspension.camber, -15f, 15f);
		desc.carXDesc.frontSuspension.caster = FloatField(1, 2, leftAlign: true, "Caster", "deg", desc.carXDesc.frontSuspension.caster, -15f, 15f);
		desc.carXDesc.frontSuspension.ackerman = FloatField(1, 3, leftAlign: true, "Ackerman", "%", desc.carXDesc.frontSuspension.ackerman, 0f, 100f);
		desc.carXDesc.frontSuspension.frontLock = FloatField(1, 4, leftAlign: true, "Front lock", "deg", desc.carXDesc.frontSuspension.frontLock, 5f, 60f);
		desc.carXDesc.rearSuspension.suspType = (SuspensionType)List(0, 0, leftAlign: false, "Type", "", (int)desc.carXDesc.rearSuspension.suspType, m_suspTypeList);
		desc.carXDesc.rearSuspension.springLength = 0.001f * (float)IntField(0, 1, leftAlign: false, "Spring Way", "mm", Mathf.RoundToInt(desc.carXDesc.rearSuspension.springLength * 1000f), 50, 1000);
		desc.carXDesc.rearSuspension.stiffness = 1000f * FloatField(0, 2, leftAlign: false, "Stiffness", "kN/m", desc.carXDesc.rearSuspension.stiffness * 0.001f, 0f, 1000f);
		desc.carXDesc.rearSuspension.stabilizer = 1000f * FloatField(0, 3, leftAlign: false, "Stabilizer", "kN/m", desc.carXDesc.rearSuspension.stabilizer * 0.001f, 0f, 1000f);
		desc.carXDesc.rearSuspension.absorbCoef = 1000f * FloatField(0, 4, leftAlign: false, "AbsorbCoef", "Ns/mm", desc.carXDesc.rearSuspension.absorbCoef * 0.001f, 0f, 100f);
		desc.carXDesc.rearSuspension.slowBump = 1000f * FloatField(0, 5, leftAlign: false, "Slow Bump", "Ns/mm", desc.carXDesc.rearSuspension.slowBump * 0.001f, 0f, 100f);
		desc.carXDesc.rearSuspension.fastBump = 1000f * FloatField(0, 6, leftAlign: false, "Fast Bump", "Ns/mm", desc.carXDesc.rearSuspension.fastBump * 0.001f, 0f, 100f);
		desc.carXDesc.rearSuspension.slowRebound = 1000f * FloatField(0, 7, leftAlign: false, "Slow Rebound", "Ns/mm", desc.carXDesc.rearSuspension.slowRebound * 0.001f, 0f, 100f);
		desc.carXDesc.rearSuspension.fastRebound = 1000f * FloatField(0, 8, leftAlign: false, "Fast Rebound", "Ns/mm", desc.carXDesc.rearSuspension.fastRebound * 0.001f, 0f, 100f);
		desc.carXDesc.rearSuspension.toeIn = FloatField(1, 0, leftAlign: false, "Toe", "deg", desc.carXDesc.rearSuspension.toeIn, -10f, 10f);
		desc.carXDesc.rearSuspension.camber = FloatField(1, 1, leftAlign: false, "Camber", "deg", desc.carXDesc.rearSuspension.camber, -10f, 10f);
	}

	private bool Toggle(int col, int row, bool leftAlign, string name, bool val)
	{
		string title = "";
		BeginField(col, row, leftAlign, title);
		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		bool flag = GUILayout.Toggle(val, name);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.FlexibleSpace();
		if (flag != val)
		{
			val = flag;
			m_changed = true;
		}
		EndField();
		return val;
	}

	private float FloatField(int col, int row, bool leftAlign, string name, string unit, float val, float min, float max)
	{
		string title = (string.IsNullOrEmpty(unit) ? name : $"{name} ({unit})");
		BeginField(col, row, leftAlign, title);
		GUILayout.Space(20f);
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label(val.ToString("F2"));
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		float num = GUILayout.HorizontalSlider(val, min, max, GUILayout.Width(190f));
		if (num != val)
		{
			val = num;
			m_changed = true;
		}
		EndField();
		return val;
	}

	private int List(int col, int row, bool leftAlign, string name, string unit, int val, string[] list)
	{
		string title = (string.IsNullOrEmpty(unit) ? name : $"{name} ({unit})");
		BeginField(col, row, leftAlign, title);
		GUILayout.Space(20f);
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		val = Mathf.Clamp(val, 0, list.Length - 1);
		GUILayout.Label(list[val]);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		int num = Mathf.RoundToInt(GUILayout.HorizontalSlider(val, 0f, list.Length - 1, GUILayout.Width(190f)));
		if (num != val)
		{
			val = num;
			m_changed = true;
		}
		EndField();
		return val;
	}

	private int IntField(int col, int row, bool leftAlign, string name, string unit, int val, int min, int max)
	{
		string title = (string.IsNullOrEmpty(unit) ? name : $"{name} ({unit})");
		BeginField(col, row, leftAlign, title);
		GUILayout.Space(20f);
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label(val.ToString());
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		int num = Mathf.RoundToInt(GUILayout.HorizontalSlider(val, min, max, GUILayout.Width(190f)));
		if (num != val)
		{
			val = num;
			m_changed = true;
		}
		EndField();
		return val;
	}

	private void BeginField(int col, int row, bool leftAlign, string title)
	{
		Rect rect = new Rect(0f, 8f + (float)row * 8f + (float)row * 60f, 200f, 60f);
		rect.x = (leftAlign ? (8f + (float)col * 8f + (float)col * 200f) : ((float)Screen.width - (208f + (float)col * 8f + (float)col * 200f)));
		GUI.Box(rect, "");
		GUI.Box(rect, "");
		GUI.Box(rect, title);
		GUILayout.BeginArea(rect);
		GUILayout.BeginVertical();
	}

	private void EndField()
	{
		GUILayout.EndVertical();
		GUILayout.EndArea();
	}

	private int Index(int[] array, int val)
	{
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == val)
			{
				return i;
			}
		}
		return 0;
	}
}
