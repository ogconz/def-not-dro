using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AudioFXBlockerAttribute : Attribute
{
	public readonly bool isBlocker;

	public AudioFXBlockerAttribute(bool isBlocker)
	{
		this.isBlocker = isBlocker;
	}
}
