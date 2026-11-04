using System;

public class RootState : BaseState
{
	public override void OnUpdate()
	{
		throw new Exception("Root not allowed to be current!");
	}
}
