using System;
using System.Text;
using SyncMultiplayer;
using UnityEngine;
using UnityEngine.UI;

public static class Extensions
{
	public static bool IsNaNOrInfinity(this float val, bool clamp = false)
	{
		if (!float.IsNaN(val) && !float.IsInfinity(val))
		{
			if (clamp)
			{
				if (!(val > 100000f))
				{
					return val < -100000f;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	public static bool IsOneOfNaNOrInfinity(this Vector3 vec, bool clamp = false)
	{
		if (!vec.x.IsNaNOrInfinity(clamp) && !vec.y.IsNaNOrInfinity(clamp))
		{
			return vec.z.IsNaNOrInfinity(clamp);
		}
		return true;
	}

	public static bool IsOneOfNaNOrInfinity(this Quaternion quat, bool clamp = false)
	{
		if (!quat.x.IsNaNOrInfinity(clamp) && !quat.y.IsNaNOrInfinity(clamp) && !quat.z.IsNaNOrInfinity(clamp))
		{
			return quat.w.IsNaNOrInfinity(clamp);
		}
		return true;
	}

	public static bool IsValid(this PosDataFrame data)
	{
		if (!data.angAccel.IsNaNOrInfinity(clamp: true) && !data.longAccel.IsNaNOrInfinity(clamp: true) && !data.sideAccel.IsNaNOrInfinity(clamp: true) && !data.position.IsOneOfNaNOrInfinity() && !data.velocity.IsOneOfNaNOrInfinity(clamp: true) && !data.angularVelocity.IsOneOfNaNOrInfinity(clamp: true))
		{
			return !data.rotation.IsOneOfNaNOrInfinity(clamp: true);
		}
		return false;
	}

	public static void ChangeAlphaChannel(this Image image, float alpha)
	{
		Color color = image.color;
		color.a = alpha;
		image.color = color;
	}

	public static string FormatNumber(this int number)
	{
		return $"{number:#,##0.###}".Replace(",", " ");
	}

	public static bool IsValidDateTime(this DateTime dateTime)
	{
		return dateTime != DateTime.MinValue;
	}

	public static string EncodeBase64(this string plainText)
	{
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
	}

	public static string DecodeBase64(this string encodedText)
	{
		byte[] bytes = Convert.FromBase64String(encodedText);
		return Encoding.UTF8.GetString(bytes);
	}
}
