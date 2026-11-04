using System;

namespace DI
{
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class DependencyAttribute : Attribute
	{
	}
}
