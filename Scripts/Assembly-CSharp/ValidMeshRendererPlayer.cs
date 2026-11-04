using ArrayExtension;
using Services.UGC.Utils;
using UnityEngine;
using UnityEngine.Rendering;

public class ValidMeshRendererPlayer : IValidComponentProcess
{
	private const string HDRP_HIDDEN_ERROR = "Hidden/InternalErrorShader";

	private const string HDRP_DEFAULT = "HDRP/Lit";

	private const string HDRP_SHADERGRAPH = "Shader Graphs";

	private const string HDRP_FOLIAGE_SHADERGRAPH = "Shader Graphs/FoliageSimple";

	private const string HDRP_SHADERGRAPH_FOLIAGE_ASSET = "ShaderGraphs/FoliageSimple.shadergraph";

	private const string HDRP_SHADERGRAPH_FOLIAGE_MATERIAL = "ShaderGraphs/FoliageSimple.mat";

	private const string HDRP_DEFAULT_MATERIAL = "ShaderGraphs/LItDefault.mat";

	private const string ALPHATEST_PROPERTY = "_ALPHATEST_ON";

	private Shader m_foliageShader;

	private Material m_hdrpMaterialFoliage;

	private Material m_hdrpMaterial;

	public bool isSuccess { get; private set; } = true;

	public string processMessage { get; private set; }

	public void Reset()
	{
		processMessage = string.Empty;
		isSuccess = true;
	}

	public void ValidProcess(Component comp)
	{
		if (!(comp.GetType().Name == "MeshRenderer"))
		{
			return;
		}
		MeshRenderer component = comp.gameObject.GetComponent<MeshRenderer>();
		Material[] materials = component.materials;
		if (materials != null)
		{
			for (int i = 0; i < component.materials.Length; i++)
			{
				Material material = materials[i];
				ValidMaterial(ref material);
				materials[i] = material;
			}
			component.materials = materials;
		}
	}

	public void ValidMaterial(ref Material material)
	{
		if (m_hdrpMaterial == null)
		{
			m_hdrpMaterial = ResourcesWrapper.Load<Material>("ShaderGraphs/LItDefault.mat");
		}
		if (m_hdrpMaterialFoliage == null)
		{
			m_hdrpMaterialFoliage = ResourcesWrapper.Load<Material>("ShaderGraphs/FoliageSimple.mat");
		}
		if (m_foliageShader == null)
		{
			m_foliageShader = ResourcesWrapper.Load<Shader>("ShaderGraphs/FoliageSimple.shadergraph");
		}
		if (UGCSceneBundleManager.loadedSceneBundle.Item1?.targetClientVersion != "2.21.0")
		{
			return;
		}
		if (material.shader.name.Contains("Shader Graphs/FoliageSimple"))
		{
			material.shader = m_foliageShader;
			Texture texture = material.GetTexture(Shader.PropertyToID("_BaseColorMap"));
			material.CopyPropertiesFromMaterial(m_hdrpMaterialFoliage);
			material.SetTexture(Shader.PropertyToID("_BaseColorMap"), texture);
			return;
		}
		Shader shader = Shader.Find(material.shader.name);
		if (material.shader.name.Contains("Hidden/InternalErrorShader"))
		{
			material = CopyMaterialProperties(material, m_hdrpMaterial, m_hdrpMaterial.shader);
		}
		else if (shader == null || material.shader.name == "HDRP/Lit" || material.shader.name.Contains("Shader Graphs"))
		{
			LocalKeyword localKeyword = material.enabledKeywords.Find((LocalKeyword keyword) => keyword.name == "_ALPHATEST_ON");
			material = CopyMaterialProperties(material, localKeyword.isValid ? m_hdrpMaterial : material, m_hdrpMaterial.shader);
		}
		else
		{
			material.shader = shader;
		}
	}

	private Material CopyMaterialProperties(Material source, Material settings, Shader dest)
	{
		Material material = new Material(dest);
		int[] texturePropertyNameIDs = source.GetTexturePropertyNameIDs();
		Texture[] array = new Texture[texturePropertyNameIDs.Length];
		for (int i = 0; i < texturePropertyNameIDs.Length; i++)
		{
			array[i] = source.GetTexture(texturePropertyNameIDs[i]);
		}
		material.CopyPropertiesFromMaterial(settings);
		material.CopyMatchingPropertiesFromMaterial(settings);
		for (int j = 0; j < texturePropertyNameIDs.Length; j++)
		{
			if (material.HasProperty(texturePropertyNameIDs[j]))
			{
				material.SetTexture(texturePropertyNameIDs[j], array[j]);
			}
			else
			{
				material.SetTexture(texturePropertyNameIDs[j], Texture2D.linearGrayTexture);
			}
		}
		return material;
	}
}
