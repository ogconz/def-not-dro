using System;
using System.IO;
using System.Xml.Serialization;
using CarX;
using UnityEngine;

public static class CarExtensions
{
	private static CarProfilesAsset m_profilesDB;

	public static float CalcPrediction(this BaseCar car, float time, out Vector3 delta)
	{
		Transform getTransform = car.getTransform;
		Car carX = car.carX;
		Vector3 forward = getTransform.forward;
		Vector3 right = getTransform.right;
		Vector3 vector = carX.velocity + (forward * carX.longAccel + right * carX.sideAccel) * time;
		delta = (carX.velocity + vector) * 0.5f * time;
		return Vector3.Dot(delta, forward);
	}

	public static float CalcPredictionPosDelta(this BaseCar car, Vector3 velocity, float time, out Vector3 delta)
	{
		Transform getTransform = car.getTransform;
		Car carX = car.carX;
		Vector3 forward = getTransform.forward;
		Vector3 right = getTransform.right;
		Vector3 vector = velocity + (forward * carX.longAccel + right * carX.sideAccel) * time;
		delta = (velocity + vector) * 0.5f * time;
		return Vector3.Dot(delta, forward);
	}

	public static BaseCar.Desc InitDesc(this BaseCar car, string descName, bool skipWheelBase = false)
	{
		if (StaticDictionariesHolder.AssetOnlyMode)
		{
			return car.GetDescFromAsset(descName, apply: true, skipWheelBase);
		}
		return car.ParseDesc(CarVisualManager.GetProfileData(descName), apply: true);
	}

	private static BaseCar.Desc GetDescFromAsset(this BaseCar car, string descName, bool apply = false, bool skipWheelBase = false)
	{
		if (m_profilesDB == null)
		{
			m_profilesDB = CarProfilesAsset.LoadDB();
		}
		if (m_profilesDB != null)
		{
			BaseCar.Desc desc = m_profilesDB.Find(descName);
			if (desc == null)
			{
				return null;
			}
			if (apply)
			{
				car.SetDesc(desc, skipWheelBase);
			}
			return desc;
		}
		return null;
	}

	public static float GetCarProfileMass(this BaseCar car, string profileName)
	{
		if (m_profilesDB == null)
		{
			m_profilesDB = CarProfilesAsset.LoadDB();
		}
		if (m_profilesDB == null)
		{
			return 0f;
		}
		return m_profilesDB.Find(profileName)?.carXDesc.weight.mass ?? 0f;
	}

	public static float GetRevLimiterStep(this BaseCar car, string profileName)
	{
		if (m_profilesDB == null)
		{
			m_profilesDB = CarProfilesAsset.LoadDB();
		}
		if (m_profilesDB == null)
		{
			return 0f;
		}
		return m_profilesDB.Find(profileName)?.carXDesc.engine.revLimiterStep ?? 0f;
	}

	public static BaseCar.Desc ParseDesc(this BaseCar car, string data, bool apply = false)
	{
		BaseCar.Desc desc = null;
		if (!string.IsNullOrEmpty(data))
		{
			try
			{
				StringReader textReader = new StringReader(data);
				desc = (BaseCar.Desc)new XmlSerializer(typeof(BaseCar.Desc)).Deserialize(textReader);
			}
			catch (InvalidOperationException)
			{
				StringReader textReader2 = new StringReader(data);
				CarDesc carXDesc = (CarDesc)new XmlSerializer(typeof(CarDesc)).Deserialize(textReader2);
				desc = car.GetDesc(carXDesc);
			}
			catch (Exception ex2)
			{
				GameLog.Print(AlertLevel.FatalError, "System", "ParseDesc failed: " + ex2.Message);
			}
		}
		if (desc != null && apply)
		{
			desc.Check(car?.name);
			car.SetDesc(desc);
		}
		return desc;
	}

	public static void Check(this BaseCar.Desc desc, string sourceName)
	{
		if (desc.carXDesc.engine.revLimiterStep == 0f)
		{
			desc.carXDesc.engine.revLimiterStep = 300f;
		}
	}
}
