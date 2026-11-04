using UnityEngine;

namespace Utils.MeshGen.Spline
{
	public static class FloatArrayExtensions
	{
		public static float Sample(this float[] fArr, float t)
		{
			int num = fArr.Length;
			switch (num)
			{
			case 0:
				Debug.LogError("Unable to sample array - it has no elements");
				return 0f;
			case 1:
				return fArr[0];
			default:
			{
				float num2 = t * (float)(num - 1);
				int num3 = Mathf.FloorToInt(num2);
				int num4 = Mathf.FloorToInt(num2 + 1f);
				if (num4 >= num)
				{
					return fArr[num - 1];
				}
				if (num3 < 0)
				{
					return fArr[0];
				}
				return Mathf.Lerp(fArr[num3], fArr[num4], num2 - (float)num3);
			}
			}
		}
	}
}
