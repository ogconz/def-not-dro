using System.Collections.Generic;
using DB.Meta;
using UnityEngine;

public class MaterialSettingsHelper : MonoBehaviour
{
	public class ColorsCollection
	{
		public List<CarColorMeta> carColors = new List<CarColorMeta>();

		public List<WheelSpokeColorMeta> wheelSpokeColors = new List<WheelSpokeColorMeta>();

		public List<WheelRimColorMeta> wheelRimColors = new List<WheelRimColorMeta>();

		public List<GlassColorMeta> glassColors = new List<GlassColorMeta>();

		public List<SmokeColorMeta> smokeColors = new List<SmokeColorMeta>();
	}

	public CarVisualManager.CustomizationType helperType;
}
