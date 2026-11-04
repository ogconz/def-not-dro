using System;

namespace Xbox
{
	[Flags]
	public enum PrivilegeCheckResult : uint
	{
		NoIssue = 0u,
		PurchaseRequired = 1u,
		Aborted = 2u,
		Banned = 4u,
		Restricted = 8u
	}
}
