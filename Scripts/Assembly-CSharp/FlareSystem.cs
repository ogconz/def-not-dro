using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class FlareSystem : CustomPass
{
	internal struct LightFlareData
	{
		public Color color;

		public float3 position;

		public float4 dir;

		public float distanceSqr;
	}

	private struct FlareVertex
	{
		public float3 position;

		public half4 color;

		public float2 uvCoord;

		public float4 dir;

		public FlareVertex(float3 position, half4 color, float2 uvCoord, float4 dir)
		{
			this.position = position;
			this.color = color;
			this.uvCoord = uvCoord;
			this.dir = dir;
		}
	}

	[BurstCompile]
	private struct UpdateVertexBufferJob : IJobParallelForDefer
	{
		public float preExposure;

		[ReadOnly]
		public NativeArray<LightFlareData> lights;

		[NativeDisableParallelForRestriction]
		[NativeDisableContainerSafetyRestriction]
		public NativeArray<FlareVertex> vertexBuffer;

		public void Execute(int index)
		{
			LightFlareData lightFlareData = lights[index];
			float3 position = lightFlareData.position;
			half4 color = default(half4);
			color.x = (half)(lightFlareData.color.r * preExposure);
			color.y = (half)(lightFlareData.color.g * preExposure);
			color.z = (half)(lightFlareData.color.b * preExposure);
			color.w = (half)1f;
			int num = index * 4;
			vertexBuffer[num] = new FlareVertex(position, color, new float2(0f, 0f), lightFlareData.dir);
			vertexBuffer[num + 1] = new FlareVertex(position, color, new float2(0f, 1f), lightFlareData.dir);
			vertexBuffer[num + 2] = new FlareVertex(position, color, new float2(1f, 1f), lightFlareData.dir);
			vertexBuffer[num + 3] = new FlareVertex(position, color, new float2(1f, 0f), lightFlareData.dir);
		}
	}

	[BurstCompile]
	private struct LightsPreparingJob : IJob
	{
		[ReadOnly]
		public NativeArray<VisibleLight> visibleLights;

		[ReadOnly]
		public float2 flareMin;

		[ReadOnly]
		public float2 flareMax;

		[ReadOnly]
		public float2 origin;

		public NativeList<LightFlareData> lightFlareList;

		public void Execute()
		{
			int num = ((visibleLights.Length <= 512) ? visibleLights.Length : 512);
			float3 position = default(float3);
			for (int i = 0; i < num; i++)
			{
				VisibleLight visibleLight = visibleLights[i];
				LightType lightType = visibleLight.lightType;
				if (lightType == LightType.Point || lightType == LightType.Spot)
				{
					Matrix4x4 localToWorldMatrix = visibleLight.localToWorldMatrix;
					position.x = localToWorldMatrix.m03;
					position.y = localToWorldMatrix.m13;
					position.z = localToWorldMatrix.m23;
					Color finalColor = visibleLight.finalColor;
					float2 obj = origin - position.xz;
					float distanceSqr = math.dot(obj, obj);
					float num2 = finalColor.a * math.rcp(math.max(finalColor.r, math.max(finalColor.g, finalColor.b)));
					if (!(num2 > 0.25f) || num2 > 0.75f)
					{
						LightFlareData value = new LightFlareData
						{
							color = finalColor.gamma,
							position = position,
							distanceSqr = distanceSqr,
							dir = new float4(-new float3(localToWorldMatrix.m02, localToWorldMatrix.m12, localToWorldMatrix.m22), (lightType == LightType.Spot) ? 1 : 0)
						};
						lightFlareList.Add(in value);
					}
				}
			}
			lightFlareList.AsArray().Sort(default(LightFlareDataComparer));
			if (lightFlareList.Length > 512)
			{
				lightFlareList.ResizeUninitialized(512);
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct LightFlareDataComparer : IComparer<LightFlareData>
	{
		public int Compare(LightFlareData a, LightFlareData b)
		{
			if (a.distanceSqr > b.distanceSqr)
			{
				return 1;
			}
			if (a.distanceSqr < b.distanceSqr)
			{
				return -1;
			}
			return 0;
		}
	}

	public Material flareMaterial;

	private Mesh m_mesh;

	public const int maxLightCount = 512;

	public const int maxVertexCount = 2048;

	public const int maxIndexCount = 3072;

	protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
	{
		VertexAttributeDescriptor[] attributes = new VertexAttributeDescriptor[4]
		{
			new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
			new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float16, 4),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4)
		};
		m_mesh = new Mesh();
		m_mesh.hideFlags = HideFlags.HideAndDontSave;
		m_mesh.MarkDynamic();
		m_mesh.SetVertexBufferParams(2048, attributes);
		NativeArray<ushort> data = new NativeArray<ushort>(3072, Allocator.Temp);
		for (int i = 0; i < 512; i++)
		{
			int num = i * 6;
			int num2 = i * 4;
			data[num] = (ushort)num2;
			data[num + 1] = (ushort)(num2 + 1);
			data[num + 2] = (ushort)(num2 + 2);
			data[num + 3] = (ushort)num2;
			data[num + 4] = (ushort)(num2 + 2);
			data[num + 5] = (ushort)(num2 + 3);
		}
		m_mesh.SetIndexBufferParams(3072, IndexFormat.UInt16);
		m_mesh.SetIndexBufferData(data, 0, 0, data.Length, (MeshUpdateFlags)(-1));
		m_mesh.SetSubMesh(0, new SubMeshDescriptor(0, 3072));
		data.Dispose();
		targetDepthBuffer = TargetBuffer.Camera;
	}

	protected override void Cleanup()
	{
		CoreUtils.Destroy(m_mesh);
	}

	protected override void Execute(CustomPassContext ctx)
	{
		if (!(flareMaterial == null))
		{
			NativeList<LightFlareData> nativeList = new NativeList<LightFlareData>(512, Allocator.TempJob);
			NativeArray<FlareVertex> nativeArray = new NativeArray<FlareVertex>(2048, Allocator.TempJob);
			Vector3 position = ctx.hdCamera.camera.transform.position;
			LightsPreparingJob jobData = new LightsPreparingJob
			{
				visibleLights = ctx.cullingResults.visibleLights,
				origin = new float2(position.x, position.z),
				lightFlareList = nativeList
			};
			new UpdateVertexBufferJob
			{
				preExposure = 1f,
				lights = nativeList.AsDeferredJobArray(),
				vertexBuffer = nativeArray
			}.Schedule(dependsOn: jobData.Schedule(), list: nativeList, innerloopBatchCount: 64).Complete();
			m_mesh.SetVertexBufferData(nativeArray, 0, 0, nativeArray.Length, 0, (MeshUpdateFlags)(-1));
			ctx.cmd.DrawMesh(m_mesh, Matrix4x4.identity, flareMaterial, 0, 0, null);
			nativeList.Dispose();
			nativeArray.Dispose();
		}
	}
}
