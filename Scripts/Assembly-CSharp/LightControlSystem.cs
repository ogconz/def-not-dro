using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering.HighDefinition;
using VLB;

public class LightControlSystem : IDisposable
{
	[Flags]
	private enum LightFlags : byte
	{
		None = 0,
		IsSpot = 1,
		HasVolumetricLightBeam = 2,
		AffectDiffuse = 4,
		AffectSpecular = 8,
		AffectVolumetric = 0x10
	}

	[Flags]
	private enum LightChangesFlags : byte
	{
		None = 0,
		FadeDistance = 1,
		Visibility = 2
	}

	private struct LightData
	{
		public float fadeDistance;

		public float fadeDistanceSq;

		public float defaultSpotAngle;

		public float defaultInnerSpotPercent;

		public float defaultLightRange;

		public LightFlags lightFlags;

		public byte isVisible;

		public float vlbFadeOutBegin;

		public float vlbFadeOutEnd;

		public bool isSpot => (lightFlags & LightFlags.IsSpot) != 0;

		public bool hasVolumetricLightBeam => (lightFlags & LightFlags.HasVolumetricLightBeam) != 0;

		public bool affectDiffuse => (lightFlags & LightFlags.AffectDiffuse) != 0;

		public bool affectSpecular => (lightFlags & LightFlags.AffectSpecular) != 0;

		public bool affectVolumetric => (lightFlags & LightFlags.AffectVolumetric) != 0;
	}

	private struct LightContainer
	{
		public HDAdditionalLightData hdAdditionalLightData;

		public VolumetricLightBeam volumetricLightBeam;
	}

	[BurstCompile]
	private struct RefreshJob : IJobParallelForTransform
	{
		[NativeDisableParallelForRestriction]
		public NativeArray<LightData> lights;

		public NativeParallelHashMap<int, LightChangesFlags>.ParallelWriter lightChangesMap;

		public float3 cameraPosition;

		public float globalFadeDistance;

		public void Execute(int index, TransformAccess transform)
		{
			LightChangesFlags lightChangesFlags = LightChangesFlags.None;
			LightData value = lights[index];
			if (value.fadeDistance - value.defaultLightRange != globalFadeDistance)
			{
				value.fadeDistance = globalFadeDistance + value.defaultLightRange;
				value.fadeDistanceSq = value.fadeDistance * value.fadeDistance;
				if (value.hasVolumetricLightBeam)
				{
					value.vlbFadeOutBegin = math.max(1f, value.fadeDistance + value.defaultLightRange - 100f);
					value.vlbFadeOutEnd = math.max(1f, value.fadeDistance + value.defaultLightRange);
				}
				lightChangesFlags |= LightChangesFlags.FadeDistance;
			}
			byte b = (byte)((math.distancesq(cameraPosition, transform.position) < value.fadeDistanceSq) ? 1u : 0u);
			if (value.isVisible != b)
			{
				value.isVisible = b;
				lightChangesFlags |= LightChangesFlags.Visibility;
			}
			if (lightChangesFlags != LightChangesFlags.None)
			{
				lights[index] = value;
				lightChangesMap.TryAdd(index, lightChangesFlags);
			}
		}
	}

	private List<LightControlAccessor> m_lightControllers;

	private List<LightContainer> m_lightContainers;

	private TransformAccessArray m_transformAccessArray;

	private NativeList<LightData> m_lightList;

	private bool m_updating;

	private NativeParallelHashMap<int, LightChangesFlags> m_lightChangesMap;

	private NativeArray<int> m_lightChangesIndices;

	private JobHandle m_jobHandle;

	public IReadOnlyCollection<LightControlAccessor> lights => m_lightControllers;

	public LightControlSystem()
	{
		m_lightControllers = new List<LightControlAccessor>(2048);
		m_lightContainers = new List<LightContainer>(2048);
		m_transformAccessArray = new TransformAccessArray(2048);
		m_lightList = new NativeList<LightData>(2048, Allocator.Persistent);
	}

	public void Dispose()
	{
		m_lightControllers.Clear();
		m_lightContainers.Clear();
		m_jobHandle.Complete();
		m_transformAccessArray.Dispose();
		m_lightList.Dispose();
		if (m_lightChangesIndices.IsCreated)
		{
			m_lightChangesIndices.Dispose();
		}
		if (m_lightChangesMap.IsCreated)
		{
			m_lightChangesMap.Dispose();
		}
	}

	public void TryAdd(LightControlAccessor l)
	{
		m_jobHandle.Complete();
		HDAdditionalLightData component = l.GetComponent<HDAdditionalLightData>();
		VolumetricLightBeam component2;
		bool flag = l.TryGetComponent<VolumetricLightBeam>(out component2);
		if (component.affectDiffuse || component.affectSpecular || component.affectsVolumetric || flag)
		{
			Light component3 = l.GetComponent<Light>();
			m_lightControllers.Add(l);
			m_lightContainers.Add(new LightContainer
			{
				hdAdditionalLightData = component,
				volumetricLightBeam = component2
			});
			m_transformAccessArray.Add(l.transform);
			LightFlags lightFlags = LightFlags.None;
			if (component3.type == LightType.Spot)
			{
				lightFlags |= LightFlags.IsSpot;
			}
			if (flag)
			{
				lightFlags |= LightFlags.HasVolumetricLightBeam;
			}
			if (component.affectDiffuse)
			{
				lightFlags |= LightFlags.AffectDiffuse;
			}
			if (component.affectSpecular)
			{
				lightFlags |= LightFlags.AffectSpecular;
			}
			if (component.affectsVolumetric)
			{
				lightFlags |= LightFlags.AffectVolumetric;
			}
			LightData value = new LightData
			{
				fadeDistance = component.fadeDistance,
				fadeDistanceSq = component.fadeDistance * component.fadeDistance,
				defaultSpotAngle = component3.spotAngle,
				defaultLightRange = component3.range,
				defaultInnerSpotPercent = component.innerSpotPercent,
				lightFlags = lightFlags,
				vlbFadeOutBegin = (flag ? component2.fadeOutBegin : 0f),
				vlbFadeOutEnd = (flag ? component2.fadeOutEnd : 0f),
				isVisible = 1
			};
			m_lightList.Add(in value);
		}
	}

	public void Remove(LightControlAccessor l)
	{
		if (m_updating)
		{
			EndUpdate();
		}
		m_jobHandle.Complete();
		int num = m_lightControllers.IndexOf(l);
		if (num >= 0)
		{
			LightData lightData = m_lightList[num];
			LightContainer lightContainer = m_lightContainers[num];
			lightContainer.hdAdditionalLightData.affectDiffuse = lightData.affectDiffuse;
			lightContainer.hdAdditionalLightData.affectSpecular = lightData.affectSpecular;
			lightContainer.hdAdditionalLightData.affectsVolumetric = lightData.affectVolumetric;
			lightContainer.hdAdditionalLightData.SetRange(lightData.defaultLightRange);
			if (lightData.isSpot)
			{
				lightContainer.hdAdditionalLightData.SetSpotAngle(lightData.defaultSpotAngle, lightData.defaultInnerSpotPercent);
			}
			m_lightControllers.RemoveAtSwapBack(num);
			m_transformAccessArray.RemoveAtSwapBack(num);
			m_lightList.RemoveAtSwapBack(num);
			m_lightContainers.RemoveAtSwapBack(num);
		}
	}

	public void BeginUpdate(float3 cameraPosition, float globalFadeDistance)
	{
		if (!m_updating)
		{
			m_jobHandle.Complete();
			m_lightChangesMap = new NativeParallelHashMap<int, LightChangesFlags>(m_lightList.Length, Allocator.TempJob);
			RefreshJob jobData = new RefreshJob
			{
				lights = m_lightList,
				lightChangesMap = m_lightChangesMap.AsParallelWriter(),
				cameraPosition = cameraPosition,
				globalFadeDistance = globalFadeDistance
			};
			m_jobHandle = jobData.Schedule(m_transformAccessArray, m_jobHandle);
			m_updating = true;
		}
	}

	public void EndUpdate()
	{
		if (!m_updating)
		{
			return;
		}
		m_jobHandle.Complete();
		m_lightChangesIndices = m_lightChangesMap.GetKeyArray(Allocator.TempJob);
		for (int i = 0; i < m_lightChangesIndices.Length; i++)
		{
			int num = m_lightChangesIndices[i];
			LightChangesFlags num2 = m_lightChangesMap[num];
			LightData lightData = m_lightList[num];
			LightContainer lightContainer = m_lightContainers[num];
			if ((num2 & LightChangesFlags.FadeDistance) != LightChangesFlags.None)
			{
				lightContainer.hdAdditionalLightData.fadeDistance = lightData.fadeDistance;
				if (lightData.hasVolumetricLightBeam)
				{
					lightContainer.volumetricLightBeam.fadeOutBegin = lightData.vlbFadeOutBegin;
					lightContainer.volumetricLightBeam.fadeOutEnd = lightData.vlbFadeOutEnd;
					if (lightData.isVisible == 0 && lightContainer.volumetricLightBeam.hasGeometry)
					{
						lightContainer.volumetricLightBeam.beamGeometry.StopAllCoroutines();
					}
				}
			}
			if ((num2 & LightChangesFlags.Visibility) == 0)
			{
				continue;
			}
			if (lightData.isVisible > 0)
			{
				lightContainer.hdAdditionalLightData.SetRange(lightData.defaultLightRange);
				lightContainer.hdAdditionalLightData.affectDiffuse = lightData.affectDiffuse;
				lightContainer.hdAdditionalLightData.affectSpecular = lightData.affectSpecular;
				lightContainer.hdAdditionalLightData.affectsVolumetric = lightData.affectVolumetric;
				if (lightData.isSpot)
				{
					lightContainer.hdAdditionalLightData.SetSpotAngle(lightData.defaultSpotAngle, lightData.defaultInnerSpotPercent);
				}
				if (lightData.hasVolumetricLightBeam)
				{
					lightContainer.volumetricLightBeam.enabled = true;
					if (lightContainer.volumetricLightBeam.hasGeometry)
					{
						lightContainer.volumetricLightBeam.beamGeometry.RestartFadeOutCoroutine();
					}
				}
				continue;
			}
			lightContainer.hdAdditionalLightData.SetRange(1E-10f);
			lightContainer.hdAdditionalLightData.affectDiffuse = false;
			lightContainer.hdAdditionalLightData.affectSpecular = false;
			lightContainer.hdAdditionalLightData.affectsVolumetric = false;
			if (lightData.isSpot)
			{
				lightContainer.hdAdditionalLightData.SetSpotAngle(0f);
			}
			if (lightData.hasVolumetricLightBeam)
			{
				lightContainer.volumetricLightBeam.enabled = false;
				if (lightContainer.volumetricLightBeam.hasGeometry)
				{
					lightContainer.volumetricLightBeam.beamGeometry.StopAllCoroutines();
				}
			}
		}
		m_jobHandle = JobHandle.CombineDependencies(m_lightChangesMap.Dispose(default(JobHandle)), m_lightChangesIndices.Dispose(default(JobHandle)));
		m_updating = false;
	}
}
