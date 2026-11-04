using System.Collections.Generic;
using SimpleJSON;

namespace PrefsModels
{
	public class EngineSwapData
	{
		public class SwapController
		{
			[JsonSerialize]
			public string key;

			[JsonSerialize]
			public float value;

			public SwapController()
			{
			}

			public SwapController(string key, float value)
			{
				this.key = key;
				this.value = value;
			}
		}

		public const string KEY_TURBO_CHARGED = "turbo_charged";

		public const string KEY_TURBO_PRESSURE = "turbo_pressure";

		public const string KEY_REV_LIMITER = "rev_limiter";

		public const string KEY_MAX_TORQUE = "max_torque";

		public const string KEY_MAX_TORQUE_RPM = "max_torque_rpm";

		public const string KEY_FINAL_DRIVE = "final_drive";

		public const string KEY_GEAR_TEMPLATE = "gearbox";

		[JsonSerialize]
		public int id { get; private set; }

		[JsonSerialize]
		public List<SwapController> controllers { get; private set; }

		public EngineSwapData()
		{
		}

		public EngineSwapData(int engineId)
		{
			id = engineId;
			InitControllersIfNeed();
		}

		public void AddController(string ctrlName, float value)
		{
			SwapController swapCtrl = new SwapController
			{
				key = ctrlName,
				value = value
			};
			AddController(swapCtrl);
		}

		public void AddController(SwapController swapCtrl)
		{
			InitControllersIfNeed();
			if (controllers.Contains(swapCtrl))
			{
				controllers.Remove(swapCtrl);
			}
			controllers.Add(swapCtrl);
		}

		private void InitControllersIfNeed()
		{
			if (controllers == null)
			{
				List<SwapController> list = (controllers = new List<SwapController>());
			}
		}

		public EngineSwapData Copy()
		{
			EngineSwapData engineSwapData = new EngineSwapData(id);
			InitControllersIfNeed();
			foreach (SwapController controller in controllers)
			{
				engineSwapData.AddController(new SwapController
				{
					key = controller.key,
					value = controller.value
				});
			}
			return engineSwapData;
		}
	}
}
