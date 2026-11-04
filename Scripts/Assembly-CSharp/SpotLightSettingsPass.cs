using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class SpotLightSettingsPass : CustomPass
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	protected struct LightDistanceComparer : IComparer<(int, float)>
	{
		public int Compare((int, float) a, (int, float) b)
		{
			if (a.Item2 > b.Item2)
			{
				return 1;
			}
			if (a.Item2 < b.Item2)
			{
				return -1;
			}
			return 0;
		}
	}

	[Range(0f, 32f)]
	public int maximumSpotShdowsCount = 8;

	private (int, float)[] lightDistance;

	private HDAdditionalLightData[] additionalLightsData;

	private LightDistanceComparer lightDistanceComparer;

	protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
	{
		targetColorBuffer = TargetBuffer.None;
		targetDepthBuffer = TargetBuffer.None;
		lightDistance = new(int, float)[500];
		additionalLightsData = new HDAdditionalLightData[500];
		lightDistanceComparer = default(LightDistanceComparer);
	}

	protected override void Execute(CustomPassContext ctx)
	{
		float3 float5 = ctx.hdCamera.mainViewConstants.worldSpaceCameraPos;
		NativeArray<VisibleLight> visibleLights = ctx.cullingResults.visibleLights;
		int num = 0;
		float3 float6 = default(float3);
		for (int i = 0; i < visibleLights.Length; i++)
		{
			if (visibleLights[i].lightType == LightType.Spot)
			{
				Light light = visibleLights[i].light;
				if (!(light == null) && light.shadows != LightShadows.None)
				{
					Matrix4x4 localToWorldMatrix = visibleLights[i].localToWorldMatrix;
					float6.x = localToWorldMatrix.m03;
					float6.y = localToWorldMatrix.m13;
					float6.z = localToWorldMatrix.m23;
					float3 obj = float6 - float5;
					float item = math.dot(obj, obj);
					lightDistance[num] = (num, item);
					light.TryGetComponent<HDAdditionalLightData>(out var component);
					additionalLightsData[num] = component;
					num++;
				}
			}
		}
		if (num != 0)
		{
			Array.Sort(lightDistance, lightDistanceComparer);
			int num2 = math.min(num, maximumSpotShdowsCount);
			for (int j = 0; j < num2; j++)
			{
				int item2 = lightDistance[j].Item1;
				additionalLightsData[item2].shadowFadeDistance = 10000f;
			}
			for (int k = num2; k < num; k++)
			{
				int item3 = lightDistance[k].Item1;
				additionalLightsData[item3].shadowFadeDistance = 0f;
			}
		}
	}
}
