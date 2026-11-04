using System;

namespace SyncMultiplayer.DynamicCollisions.Structs
{
	[Flags]
	public enum ResolveFlag
	{
		None = 0,
		EqualCollideGroup = 1,
		LargeDistance = 2,
		HighVelocityDiff = 4,
		HasIntersection = 8,
		IsRapprochement = 0x10,
		IsMoving = 0x20,
		HasCollision = 0x40,
		HasHighSpeed = 0x80,
		FirstInitialize = 0x100,
		UnstableMovement = 0x200,
		HasCollideGroup = 0x400,
		HasPredictedIntersection = 0x800,
		EqualMovingGroup = 0x1000,
		HasPhysics = 0x2000
	}
}
