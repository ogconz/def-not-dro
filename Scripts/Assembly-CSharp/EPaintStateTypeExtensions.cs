using DB.Meta;

internal static class EPaintStateTypeExtensions
{
	public static GameGlobalFeature AsGameFeature(this UIPaintCustomizationContext.StateType stateType)
	{
		return stateType switch
		{
			UIPaintCustomizationContext.StateType.Body => GameGlobalFeature.PaintCar, 
			UIPaintCustomizationContext.StateType.Interior => GameGlobalFeature.PaintInterior, 
			UIPaintCustomizationContext.StateType.Suspension => GameGlobalFeature.PaintSuspension, 
			UIPaintCustomizationContext.StateType.Glass => GameGlobalFeature.PaintGlass, 
			UIPaintCustomizationContext.StateType.Smoke => GameGlobalFeature.PaintSmoke, 
			_ => GameGlobalFeature.Unknown, 
		};
	}
}
