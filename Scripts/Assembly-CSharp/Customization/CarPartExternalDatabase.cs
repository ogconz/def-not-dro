using System.Collections.Generic;

namespace Customization
{
	public class CarPartExternalDatabase
	{
		public bool isLocal;

		public Dictionary<int, CarPart> nativeParts = new Dictionary<int, CarPart>();
	}
}
