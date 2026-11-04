using System;
using Cysharp.Threading.Tasks;

namespace Services.UGC.Interfaces
{
	public interface IUGCAuthentication
	{
		Action<bool> onAuthChanged { get; set; }

		bool Initialize();

		UniTask<bool> AuthenticateAsync();

		bool Deauthenticate();

		UniTask<bool> IsAuthenticatedAsync();
	}
}
