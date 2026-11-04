using System;
using UnityEngine;

[Serializable]
public class MarkerData
{
	public string head;

	[TextArea]
	public string param;

	[SerializeReference]
	public object value;

	public string GetHead()
	{
		return head.ToLower();
	}
}
