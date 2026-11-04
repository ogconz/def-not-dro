using UnityEngine;

[RequireComponent(typeof(Renderer))]
public sealed class RendererGarlandAnimation : GarlandAnimation
{
	public static class ShaderProperty
	{
		public static int FillAmount = Shader.PropertyToID("_FillAmount");
	}

	private Renderer m_renderer;

	private MaterialPropertyBlock m_materialPropertyBlock;

	protected override void OnEnable()
	{
		base.OnEnable();
		m_renderer = GetComponent<Renderer>();
		m_materialPropertyBlock = new MaterialPropertyBlock();
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		m_renderer.SetPropertyBlock(null);
	}

	protected override void ApplyFillAmount(float fillAmount)
	{
		m_materialPropertyBlock.SetFloat(ShaderProperty.FillAmount, fillAmount);
		m_renderer.SetPropertyBlock(m_materialPropertyBlock);
	}
}
