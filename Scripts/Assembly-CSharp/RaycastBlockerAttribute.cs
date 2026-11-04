using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RaycastBlockerAttribute : Attribute
{
	public readonly bool isBlocker;

	public RaycastBlockerAttribute(bool isBlocker)
	{
		this.isBlocker = isBlocker;
	}
}
