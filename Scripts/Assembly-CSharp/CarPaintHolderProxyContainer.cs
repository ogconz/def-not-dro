using System.Collections.Generic;
using MessagePack;

[MessagePackObject(false)]
public class CarPaintHolderProxyContainer
{
	[Key(0)]
	public List<CarPaintHolderProxy> carPaintHolderProxyList;
}
