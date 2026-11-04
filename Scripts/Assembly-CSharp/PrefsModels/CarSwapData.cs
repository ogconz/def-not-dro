using System.Collections.Generic;
using CarProfile;
using SimpleJSON;

namespace PrefsModels
{
	public class CarSwapData
	{
		[JsonSerialize]
		public int currentId;

		[JsonSerialize]
		public Dictionary<int, EngineSwapData> savedEngines = new Dictionary<int, EngineSwapData>();

		public CarSwapData()
		{
		}

		public CarSwapData(CarSwapData data)
		{
			currentId = data.currentId;
			savedEngines = new Dictionary<int, EngineSwapData>(data.savedEngines);
		}

		public CarSwapData(int engineId)
		{
			currentId = engineId;
			EngineSwapData value = ((engineId == 0) ? new EngineSwapData(engineId) : GetDefaultMeta(engineId));
			savedEngines[engineId] = value;
		}

		public CarSwapData(EngineSwapData swap)
		{
			currentId = swap.id;
			savedEngines[swap.id] = swap.Copy();
		}

		public static EngineSwapData GetDefaultMeta(int engineId)
		{
			if (engineId == 0)
			{
				return new EngineSwapData(0);
			}
			EngineInfo engineInfo = SwapsAsset.LoadAndInitDB().Find(engineId);
			if (engineInfo == null)
			{
				return null;
			}
			EngineSwapData data = new EngineSwapData(engineId);
			TryAddController(engineInfo.turboCharged);
			TryAddController(engineInfo.turboPressure);
			TryAddController(engineInfo.revLimiter);
			TryAddController(engineInfo.maxTorque);
			TryAddController(engineInfo.maxTorqueRpm);
			return data;
			void TryAddController(EngineParamInfo param)
			{
				if (param != null)
				{
					data.AddController(GetController(param));
				}
			}
		}

		public static EngineSwapData.SwapController GetController(EngineParamInfo param)
		{
			float mult = 1f;
			float num = 0f;
			if (param is EngineParamShiftInfo engineParamShiftInfo)
			{
				mult = engineParamShiftInfo.multiplier;
				num = 0f - engineParamShiftInfo.shift;
			}
			float value = new CarProfileChanger(param.min, param.max, param.value, 0, mult, "", "", num).EvaluatePhase(param.value + num);
			return new EngineSwapData.SwapController
			{
				key = param.name,
				value = value
			};
		}
	}
}
