using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DirectInputX
{
	public class InputAPI
	{
		public const string pluginName = "InputWFX";

		[DllImport("InputWFX")]
		public static extern bool Init(bool bWindowMode, IntPtr handler, bool forceAngleRangeSimulation, StringBuilder hardwareIdentifier);

		[DllImport("InputWFX")]
		public static extern void Release();

		[DllImport("InputWFX")]
		public static extern void Update();

		[DllImport("InputWFX")]
		public static extern bool GetJoyName(StringBuilder str);

		[DllImport("InputWFX")]
		public static extern void SetConstantForce(float val);

		public static string GetFriendlyProductName()
		{
			StringBuilder stringBuilder = new StringBuilder(200);
			GetJoyName(stringBuilder);
			return stringBuilder.ToString();
		}

		[DllImport("InputWFX")]
		public static extern bool IsAngleRangeSupported();

		[DllImport("InputWFX")]
		public static extern int GetAngleRangeMin();

		[DllImport("InputWFX")]
		public static extern int GetAngleRangeMax();

		[DllImport("InputWFX")]
		public static extern void SetAngleRange(int range);
	}
}
