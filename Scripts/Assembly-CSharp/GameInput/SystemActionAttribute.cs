using System;

namespace GameInput
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class SystemActionAttribute : Attribute
	{
	}
}
