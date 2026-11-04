using System;
using Cysharp.Threading.Tasks;
using Services.UGC.Interfaces;

public class VoidUGCManagerAuthentication : IUGCAuthentication
{
	public Action<bool> onAuthChanged { get; set; }

	public bool Initialize()
	{
		return true;
	}

	public UniTask<bool> AuthenticateAsync()
	{
		return UniTask.FromResult(value: true);
	}

	public bool Deauthenticate()
	{
		return true;
	}

	public UniTask<bool> IsAuthenticatedAsync()
	{
		return UniTask.FromResult(value: true);
	}
}
