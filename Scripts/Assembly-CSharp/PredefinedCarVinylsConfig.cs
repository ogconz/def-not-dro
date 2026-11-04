using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PredefinedCarVinylsConfig", menuName = "Other/PredefinedCarVinylsConfig")]
public class PredefinedCarVinylsConfig : SingletonScriptableObject<PredefinedCarVinylsConfig>
{
	public List<PredefinedVinylData> carPredefinedVinylsList;
}
