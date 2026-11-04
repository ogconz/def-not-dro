using System.Collections.Generic;
using MessagePack;

[MessagePackObject(false)]
public class VinylLayerProxyContainer
{
	[Key(0)]
	public List<VinylLayerProxy> vinylsProxyList;
}
