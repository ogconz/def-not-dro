using UnityEngine;

public class EOSConfig : SingletonScriptableObject<EOSConfig>
{
	[Header("Epic")]
	public string ProductId;

	public string SandboxIdProd;

	public string DeploymentIdProd;

	public string ApplicationId;

	[Header("Credential")]
	public string ClientId;

	public string ClientSecret;
}
