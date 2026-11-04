using UnityEngine;

public class ReplaceDecalMaterial : MonoBehaviour
{
	private static Shader m_HDRPDecalShader;

	private static Shader HDRPDecalShader
	{
		get
		{
			if (m_HDRPDecalShader == null)
			{
				m_HDRPDecalShader = Shader.Find("HDRP/Decal");
			}
			return m_HDRPDecalShader;
		}
	}

	public void Start()
	{
		MeshRenderer component = GetComponent<MeshRenderer>();
		if (!(component != null))
		{
			return;
		}
		Material sharedMaterial = component.sharedMaterial;
		if (sharedMaterial.shader != HDRPDecalShader)
		{
			sharedMaterial.hideFlags = HideFlags.None;
			sharedMaterial.shader = HDRPDecalShader;
			sharedMaterial.SetFloat("_DecalBlend", 1f);
			sharedMaterial.SetFloat("_MaskBlendSrc", 0f);
			sharedMaterial.SetColor("_BaseColor", Color.white);
			sharedMaterial.EnableKeyword("_ALBEDOCONTRIBUTION");
			sharedMaterial.EnableKeyword("_COLORMAP");
			sharedMaterial.EnableKeyword("_NORMALMAP");
			if (sharedMaterial.GetTexture("_MaskMap") != null)
			{
				sharedMaterial.EnableKeyword("_MASKMAP");
			}
			else
			{
				sharedMaterial.DisableKeyword("_MASKMAP");
			}
			sharedMaterial.DisableKeyword("_EMISSIVEMAP");
		}
	}
}
