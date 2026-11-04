using System;

[Flags]
public enum ButtonDisplayMode : byte
{
	ALL = 0,
	EXCLUDE_POSITIVE = 2,
	EXCLUDE_NEGATIVE = 4
}
