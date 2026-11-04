using UnityEngine;

namespace RenderCore.UI
{
	public interface IMaterialParameters
	{
		bool Compare(IMaterialParameters other);

		void Modify(Material m);
	}
}
