using System;

namespace SimpleJSON
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public sealed class JsonSerializeAttribute : Attribute
	{
	}
}
