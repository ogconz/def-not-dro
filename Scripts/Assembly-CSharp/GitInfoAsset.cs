using UnityEngine;

public class GitInfoAsset : ScriptableObject
{
	[SerializeField]
	private string m_branchName = "";

	[SerializeField]
	private string m_SHA = "";

	public string BranchName => m_branchName;

	public string Sha => m_SHA;

	public string ShaShort
	{
		get
		{
			if (string.IsNullOrEmpty(Sha))
			{
				return string.Empty;
			}
			return Sha.Remove(7);
		}
	}
}
