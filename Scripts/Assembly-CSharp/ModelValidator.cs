using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using DB;
using DB.Meta;
using DI;
using UnityEngine;

public static class ModelValidator
{
	private static string m_modelName = "Corrupted";

	private static byte[] key = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };

	private static byte[] iv = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };

	public static string modelName
	{
		get
		{
			if (!isValid)
			{
				return m_modelName;
			}
			return "";
		}
	}

	public static bool isValid { get; private set; }

	public static void FixInvalidLastCarId()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		BaseModel baseModel = DependencyInjector.Resolve<BaseModel>();
		GlobalGameSettings single = baseModel.GetSingle<GlobalGameSettings>();
		List<PlayerCar> list = baseModel.QueryAll<PlayerCar>();
		int num = -1;
		int num2 = -1;
		for (int i = 0; i < list.Count; i++)
		{
			PlayerCar playerCar = list[i];
			if (playerCar.id != -1 && playerCar.id == gamePrefs.globals.lastCarId)
			{
				return;
			}
			if (playerCar.id == single.startFocusedCar)
			{
				num = playerCar.id;
			}
			if (num2 == -1 && playerCar.enabled && !playerCar.forcedDisable)
			{
				num2 = playerCar.id;
			}
		}
		int num3 = ((num != -1) ? num : num2);
		Debug.LogWarning($"Last Car {gamePrefs.globals.lastCarId} not found. Replaced by: {num3}");
		gamePrefs.globals.lastCarId = num3;
	}

	public static void ValidateModel()
	{
		isValid = true;
		FixInvalidLastCarId();
	}

	private static bool GetToken(ref string src, out string path, out string code)
	{
		code = "";
		path = "";
		int num = src.IndexOf('-');
		if (num < 0)
		{
			return false;
		}
		if (num + 32 > src.Length)
		{
			return false;
		}
		string text = src.Substring(0, num + 33);
		path = text.Substring(0, num);
		code = text.Substring(num + 1, 32);
		src = src.Substring(num + 33);
		return true;
	}

	public static string Crypt(this string text)
	{
		ICryptoTransform cryptoTransform = DES.Create().CreateEncryptor(key, iv);
		byte[] bytes = Encoding.Unicode.GetBytes(text);
		return Convert.ToBase64String(cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length));
	}

	public static string Decrypt(this string text)
	{
		ICryptoTransform cryptoTransform = DES.Create().CreateDecryptor(key, iv);
		byte[] array = Convert.FromBase64String(text);
		byte[] bytes = cryptoTransform.TransformFinalBlock(array, 0, array.Length);
		return Encoding.Unicode.GetString(bytes);
	}
}
