using System.Collections.Generic;
using UnityEngine;

public class CarExternalPartsPostProcessor
{
	private List<IExternalCarPartPostProcess> m_externalCarPartsPostProcessList;

	public void ProcessExternalPartsOnRaceCar(RaceCar raceCar)
	{
		if (m_externalCarPartsPostProcessList == null)
		{
			InitiateRules();
		}
		ParseExternalParts(raceCar, raceCar.gameObject, rootFound: false, null);
	}

	private void InitiateRules()
	{
		m_externalCarPartsPostProcessList = new List<IExternalCarPartPostProcess>
		{
			new ExternalDrivingWheelSetup(),
			new ExternalRpmArrowSetup(),
			new ExternalSpeedArrowSetup(),
			new ExternalPopExhaustSetup(),
			new ExternalBrakeLightsSetup(),
			new ExternalDriverTransformSetup(),
			new ExternalFlameSetup()
		};
	}

	private void ParseExternalParts(RaceCar raceCar, GameObject go, bool rootFound, string format)
	{
		if (go == null)
		{
			return;
		}
		if (go.name.Contains("external_part_root_node"))
		{
			rootFound = true;
			string name = go.name;
			string text = name.Replace("(Clone)", "");
			int num = name.IndexOf(".") + 1;
			format = text.Substring(num, text.Length - num);
		}
		foreach (Transform item in go.transform)
		{
			ParseExternalParts(raceCar, item.gameObject, rootFound, format);
		}
		if (!rootFound)
		{
			return;
		}
		foreach (IExternalCarPartPostProcess externalCarPartsPostProcess in m_externalCarPartsPostProcessList)
		{
			externalCarPartsPostProcess.Process(raceCar, go, format);
		}
	}
}
