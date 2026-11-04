using UnityEngine;

namespace Generic.Attributes
{
	public class ArrayElementTitleAttribute : PropertyAttribute
	{
		public string varName;

		public ArrayElementTitleAttribute(string elementTitleVar)
		{
			varName = elementTitleVar;
		}
	}
}
