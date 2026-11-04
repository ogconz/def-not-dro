using System.Collections.Generic;

namespace CarProfile
{
	public static class CarProfileControllerExtension
	{
		private static Dictionary<string, CarProfileControllerType> m_controllersForcedToCategory = new Dictionary<string, CarProfileControllerType>
		{
			{
				"front_toe",
				CarProfileControllerType.SuspensionStiffness
			},
			{
				"front_camber",
				CarProfileControllerType.SuspensionStiffness
			},
			{
				"front_caster",
				CarProfileControllerType.SuspensionStiffness
			},
			{
				"front_kingpin",
				CarProfileControllerType.SuspensionStiffness
			},
			{
				"front_wheel_center_trail",
				CarProfileControllerType.SuspensionStiffness
			},
			{
				"front_ackerman",
				CarProfileControllerType.SuspensionStiffness
			},
			{
				"front_lock",
				CarProfileControllerType.SuspensionStiffness
			},
			{
				"rear_toe",
				CarProfileControllerType.SuspensionStiffness
			},
			{
				"rear_camber",
				CarProfileControllerType.SuspensionStiffness
			}
		};

		public static CarProfileControllerType TryGetOverridedCategory(this CarProfileController controller)
		{
			if (!m_controllersForcedToCategory.TryGetValue(controller.name, out var value))
			{
				return controller.type;
			}
			return value;
		}
	}
}
