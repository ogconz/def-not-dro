using Rewired;

namespace GameInput
{
	public static class ControllerMetaInfoManager
	{
		private const int MAX_DEVICES = 32;

		private static ControllerMetaInfo[] slots = new ControllerMetaInfo[32];

		public static void Clear()
		{
			for (int i = 0; i < 32; i++)
			{
				slots[i] = null;
			}
		}

		public static ControllerMetaInfo TryGetMetaInfo(this Controller controller)
		{
			int deviceSlot = GetDeviceSlot(controller);
			if (deviceSlot == -1)
			{
				return null;
			}
			return slots[deviceSlot];
		}

		public static ControllerMetaInfo GetMetaInfo(this Controller controller)
		{
			int deviceSlot = GetDeviceSlot(controller);
			if (deviceSlot == -1)
			{
				return RegisterDevice(controller, controller.name);
			}
			return slots[deviceSlot];
		}

		public static string Name(this Controller controller)
		{
			return controller.GetMetaInfo().name;
		}

		public static string ToStringInfo(this Controller controller)
		{
			if (controller == null)
			{
				return "Null";
			}
			return controller.GetMetaInfo().ToString() ?? "";
		}

		public static ControllerMetaInfo RegisterDevice(Controller controller, string controllerName)
		{
			int num = GetDeviceSlot(controller);
			if (num != -1)
			{
				return slots[num];
			}
			for (int i = 0; i < 32; i++)
			{
				if (slots[i] == null)
				{
					num = i;
					break;
				}
			}
			if (num == -1)
			{
				return null;
			}
			controller.tag = num.ToString();
			slots[num] = new ControllerMetaInfo(controller, controllerName);
			return slots[num];
		}

		public static void UnregisterDevice(Controller controller)
		{
			int deviceSlot = GetDeviceSlot(controller);
			if (deviceSlot != -1)
			{
				slots[deviceSlot] = null;
			}
		}

		private static int GetDeviceSlot(Controller controller)
		{
			if (string.IsNullOrEmpty(controller.tag))
			{
				return -1;
			}
			string tag = controller.tag;
			int num = ((tag.Length == 1) ? (controller.tag[0] - 48) : int.Parse(tag));
			if (num >= 32 || slots[num] == null || slots[num].rewiredController != controller)
			{
				return -1;
			}
			return num;
		}
	}
}
