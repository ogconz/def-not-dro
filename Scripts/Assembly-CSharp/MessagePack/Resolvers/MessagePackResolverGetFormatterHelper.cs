using System;
using System.Collections.Generic;
using MessagePack.Formatters;
using UnityEngine;

namespace MessagePack.Resolvers
{
	internal static class MessagePackResolverGetFormatterHelper
	{
		private static readonly Dictionary<Type, int> lookup;

		static MessagePackResolverGetFormatterHelper()
		{
			lookup = new Dictionary<Type, int>(11)
			{
				{
					typeof(Dictionary<string, float>),
					0
				},
				{
					typeof(List<CarPaintHolderProxy>),
					1
				},
				{
					typeof(List<VinylLayerProxy>),
					2
				},
				{
					typeof(Color[]),
					3
				},
				{
					typeof(CarVisualManager.CustomizationFlags),
					4
				},
				{
					typeof(CarPaintHolderProxy),
					5
				},
				{
					typeof(CarPaintHolderProxyContainer),
					6
				},
				{
					typeof(ProfilesInfo.Profile),
					7
				},
				{
					typeof(TextureNetworkData),
					8
				},
				{
					typeof(VinylLayerProxy),
					9
				},
				{
					typeof(VinylLayerProxyContainer),
					10
				}
			};
		}

		internal static object GetFormatter(Type t)
		{
			if (!lookup.TryGetValue(t, out var value))
			{
				return null;
			}
			return value switch
			{
				0 => new DictionaryFormatter<string, float>(), 
				1 => new ListFormatter<CarPaintHolderProxy>(), 
				2 => new ListFormatter<VinylLayerProxy>(), 
				3 => new ArrayFormatter<Color>(), 
				4 => new CarVisualManager_CustomizationFlagsFormatter(), 
				5 => new CarPaintHolderProxyFormatter(), 
				6 => new CarPaintHolderProxyContainerFormatter(), 
				7 => new ProfilesInfo_ProfileFormatter(), 
				8 => new TextureNetworkDataFormatter(), 
				9 => new VinylLayerProxyFormatter(), 
				10 => new VinylLayerProxyContainerFormatter(), 
				_ => null, 
			};
		}
	}
}
