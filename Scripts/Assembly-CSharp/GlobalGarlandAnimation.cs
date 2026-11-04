using UnityEngine;

public sealed class GlobalGarlandAnimation : GarlandAnimation
{
	public static class ShaderProperty
	{
		public static int GlobalFillAmount = Shader.PropertyToID("_Global_FillAmount");
	}

	protected override void ApplyFillAmount(float fillAmount)
	{
		Shader.SetGlobalFloat(ShaderProperty.GlobalFillAmount, fillAmount);
	}
}
