using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.XR;

namespace GpuCulling
{
	public class GpuCullingService
	{
		protected enum EntityType : byte
		{
			Object = 0,
			Sound = 1
		}

		private const byte VisibilityFlag = 1;

		private static GpuCullingService s_instance;

		private bool m_initialized;

		private bool m_enabled;

		protected GpuCullingConfig m_config;

		private HizMapGenerator m_hizMapGenerator;

		private int m_lastFrameCount;

		protected bool m_combineResult;

		private Dictionary<int, EntityType> m_entityType;

		private Dictionary<int, List<VisibilityChangeDelegate>> m_eventSubscribers;

		private Dictionary<int, Transform> m_dynamicEntityBinds;

		protected int m_kiCalculateCullResult;

		protected int m_kiClearCullResult;

		protected Vector2 m_screenSize;

		private Vector2 m_tempScreenSize;

		private RenderTexture m_hizTexture;

		private int m_maxMipLevel;

		private Vector3 m_cameraPosition;

		private Matrix4x4 m_cameraViewProjectionMatrix;

		private Vector4[] m_frustumPlanes;

		private bool[] m_usedHandles;

		protected List<int> m_activeHandles;

		private ComputeBuffer m_activeHandlesBuffer;

		protected Vector3[] m_positions;

		private ComputeBuffer m_positionsBuffer;

		protected Vector3[] m_offsets;

		private ComputeBuffer m_offsetsBuffer;

		protected Vector3[] m_extents;

		private ComputeBuffer m_extentsBuffer;

		protected Quaternion[] m_rotations;

		private ComputeBuffer m_rotationsBuffer;

		protected float[] m_sphereRadius;

		private ComputeBuffer m_sphereRadiusBuffer;

		private ComputeBuffer m_entityTypeBuffer;

		private byte[] m_oldCullResult;

		private byte[] m_cullResult;

		private ComputeBuffer m_cullResultBuffer;

		private NativeArray<uint> m_cullResultTransferBuffer;

		private AsyncGPUReadbackRequest m_cullResultReadRequest;

		public static GpuCullingService instance => s_instance ?? (s_instance = new GpuCullingServiceDebug());

		public bool IsInitialized()
		{
			return m_initialized;
		}

		private bool IsNeedProcess()
		{
			if (m_enabled)
			{
				return m_activeHandles.Count > 0;
			}
			return false;
		}

		private bool IsHandleValid(in CullingHandle handle)
		{
			if (IsInitialized() && (int)handle >= 0 && (int)handle < m_usedHandles.Length)
			{
				return m_usedHandles[(int)handle];
			}
			return false;
		}

		protected bool IsEntityVisible(int handle)
		{
			if (m_enabled)
			{
				return m_cullResult[handle] == 1;
			}
			return true;
		}

		protected EntityType GetEntityType(in CullingHandle handle)
		{
			return m_entityType.GetValueOrDefault(handle);
		}

		public CullingHandle AddEntity()
		{
			if (!IsInitialized())
			{
				return CullingHandle.Invalid;
			}
			CullingHandle freeHandle = GetFreeHandle();
			if (!freeHandle.IsValid())
			{
				GameLog.LogError("GpuCullingService: Entity pool overflow");
				return freeHandle;
			}
			m_usedHandles[(int)freeHandle] = true;
			m_activeHandles.Add(freeHandle);
			return freeHandle;
		}

		public void RemoveEntity(ref CullingHandle handle)
		{
			if (IsInitialized())
			{
				if (!IsHandleValid(in handle))
				{
					GameLog.LogError("GpuCullingService: Handle is out of bounds");
					return;
				}
				ResetEntity(handle);
				m_usedHandles[(int)handle] = false;
				int index = m_activeHandles.IndexOf(handle);
				m_activeHandles.RemoveAt(index);
				handle = CullingHandle.Invalid;
			}
		}

		public void BindObject(in CullingHandle handle, Bounds bounds)
		{
			if (IsHandleValid(in handle))
			{
				m_positions[(int)handle] = bounds.center;
				m_positionsBuffer.SetData(m_positions);
				m_extents[(int)handle] = bounds.extents;
				m_extentsBuffer.SetData(m_extents);
				SetEntityType(handle, EntityType.Object);
			}
		}

		public void BindSound(in CullingHandle handle, Vector3 position, float radius)
		{
			if (IsHandleValid(in handle))
			{
				m_positions[(int)handle] = position;
				m_positionsBuffer.SetData(m_positions);
				m_sphereRadius[(int)handle] = radius;
				m_sphereRadiusBuffer.SetData(m_sphereRadius);
				SetEntityType(handle, EntityType.Sound);
			}
		}

		public void BindCallback(in CullingHandle handle, VisibilityChangeDelegate callback)
		{
			if (IsHandleValid(in handle) && callback != null)
			{
				if (!m_eventSubscribers.ContainsKey(handle))
				{
					m_eventSubscribers[handle] = new List<VisibilityChangeDelegate>();
				}
				m_eventSubscribers[handle].Add(callback);
				callback(IsEntityVisible(handle));
			}
		}

		public void BindDynamicTransform(in CullingHandle handle, Transform transform)
		{
			if (IsHandleValid(in handle))
			{
				m_dynamicEntityBinds[handle] = transform;
			}
		}

		public void BindDynamicOffset(in CullingHandle handle, Vector3 offset)
		{
			if (IsHandleValid(in handle))
			{
				m_offsets[(int)handle] = offset;
				m_offsetsBuffer.SetData(m_offsets);
			}
		}

		public virtual void Process(ref CustomPassContext ctx)
		{
			UpdateServiceState();
			if (IsInitialized() && IsNeedProcess())
			{
				RecordFrameCount();
				DefineScreenSize(ref ctx);
				ExtractCameraData(ref ctx);
				ExtractFrustumPlanes();
				GenerateHizTexture(ref ctx);
				if (m_combineResult)
				{
					BindData();
					DispatchCalculateCullResult(ctx.cmd);
					return;
				}
				UpdateDynamicEntity();
				BindData();
				DispatchClearCullResult(ctx.cmd);
				DispatchCalculateCullResult(ctx.cmd);
				GetData();
			}
		}

		public void Setup()
		{
			if (!IsInitialized())
			{
				LoadConfig();
				CreateBuffers();
				DefineKernel();
				LoadHizMapGenerator();
				UpdateServiceState();
				m_initialized = true;
			}
		}

		public void Release()
		{
			if (IsInitialized())
			{
				UnloadHizMapGenerator();
				DestroyBuffers();
				UnloadConfig();
				m_initialized = false;
			}
		}

		private void LoadConfig()
		{
			m_config = ResourcesWrapper.Load<GpuCullingConfig>("GpuCullingConfig");
		}

		private void UnloadConfig()
		{
			ResourcesWrapper.UnloadAsset(m_config);
		}

		protected virtual void CreateBuffers()
		{
			m_entityType = new Dictionary<int, EntityType>(m_config.capacity);
			m_eventSubscribers = new Dictionary<int, List<VisibilityChangeDelegate>>(m_config.capacity);
			m_dynamicEntityBinds = new Dictionary<int, Transform>(m_config.capacity);
			m_frustumPlanes = new Vector4[6];
			m_usedHandles = new bool[m_config.capacity];
			m_activeHandles = new List<int>(m_config.capacity);
			m_activeHandlesBuffer = new ComputeBuffer(m_config.capacity, Marshal.SizeOf(typeof(int)));
			m_positions = new Vector3[m_config.capacity];
			m_positionsBuffer = new ComputeBuffer(m_config.capacity, Marshal.SizeOf(typeof(Vector3)));
			m_offsets = new Vector3[m_config.capacity];
			m_offsetsBuffer = new ComputeBuffer(m_config.capacity, Marshal.SizeOf(typeof(Vector3)));
			m_extents = new Vector3[m_config.capacity];
			m_extentsBuffer = new ComputeBuffer(m_config.capacity, Marshal.SizeOf(typeof(Vector3)));
			m_rotations = new Quaternion[m_config.capacity];
			m_rotationsBuffer = new ComputeBuffer(m_config.capacity, Marshal.SizeOf(typeof(Quaternion)));
			m_sphereRadius = new float[m_config.capacity];
			m_sphereRadiusBuffer = new ComputeBuffer(m_config.capacity, Marshal.SizeOf(typeof(float)));
			int num = Marshal.SizeOf(typeof(uint));
			int num2 = num * 8;
			int num3 = (m_config.capacity + (num2 - 1)) / num2;
			m_entityTypeBuffer = new ComputeBuffer(num3, num, ComputeBufferType.Raw);
			uint[] data = new uint[num3];
			m_entityTypeBuffer.SetData(data);
			m_oldCullResult = new byte[m_config.capacity];
			m_cullResult = new byte[m_config.capacity];
			m_cullResultBuffer = new ComputeBuffer(num3, num, ComputeBufferType.Raw);
			m_cullResultTransferBuffer = new NativeArray<uint>(num3, Allocator.Persistent);
		}

		protected virtual void DestroyBuffers()
		{
			m_entityType = null;
			m_eventSubscribers = null;
			m_dynamicEntityBinds = null;
			m_frustumPlanes = null;
			m_usedHandles = null;
			m_activeHandles = null;
			m_activeHandlesBuffer.Release();
			m_positions = null;
			m_positionsBuffer.Release();
			m_offsets = null;
			m_offsetsBuffer.Release();
			m_extents = null;
			m_extentsBuffer.Release();
			m_rotations = null;
			m_rotationsBuffer.Release();
			m_sphereRadius = null;
			m_sphereRadiusBuffer.Release();
			m_entityTypeBuffer.Release();
			m_oldCullResult = null;
			m_cullResult = null;
			m_cullResultBuffer.Release();
		}

		private void DefineKernel()
		{
			m_kiCalculateCullResult = m_config.cullingProcessShader.FindKernel("CalculateCullResult");
			m_kiClearCullResult = m_config.cullingProcessShader.FindKernel("ClearCullResult");
		}

		protected virtual bool DefineScreenSize(ref CustomPassContext ctx)
		{
			m_tempScreenSize = new Vector2(ctx.hdCamera.camera.pixelWidth, ctx.hdCamera.camera.pixelHeight);
			if ((m_screenSize - m_tempScreenSize).magnitude > 1E-05f)
			{
				m_screenSize = m_tempScreenSize;
				ResizeHizTexture();
				return true;
			}
			return false;
		}

		private void LoadHizMapGenerator()
		{
			m_hizMapGenerator = new HizMapGenerator(m_config.copyDepthTextureShader, m_config.mipGeneratorShader);
			m_hizTexture = m_hizMapGenerator.CreateMipMapRenderTexture();
		}

		private void UnloadHizMapGenerator()
		{
			m_hizTexture.Release();
			Object.DestroyImmediate(m_hizTexture);
			m_hizTexture = null;
			m_hizMapGenerator = null;
		}

		private void GenerateHizTexture(ref CustomPassContext ctx)
		{
			m_hizMapGenerator.CopyDepthTexture(ctx.cmd, ctx.cameraDepthBuffer, m_hizTexture);
			m_hizMapGenerator.Build(ctx.cmd, m_hizTexture, out m_maxMipLevel);
		}

		private void ResizeHizTexture()
		{
			m_hizTexture.Release();
			m_hizTexture.width = (int)m_screenSize.x;
			m_hizTexture.height = (int)m_screenSize.y;
		}

		protected virtual void BindData()
		{
			m_config.cullingProcessShader.SetFloat("_MinDistance", m_config.minDistance);
			m_config.cullingProcessShader.SetFloat("_MaxDistance", m_config.maxDistance);
			m_config.cullingProcessShader.SetVector("_ScreenSize", m_screenSize);
			m_config.cullingProcessShader.SetTexture(m_kiCalculateCullResult, "_HiZTexture", m_hizTexture);
			m_config.cullingProcessShader.SetInt("_MaxMipLevel", m_maxMipLevel);
			m_config.cullingProcessShader.SetVector("_CameraPosition", m_cameraPosition);
			m_config.cullingProcessShader.SetMatrix("_CameraViewProjectionMatrix", m_cameraViewProjectionMatrix);
			m_config.cullingProcessShader.SetVectorArray("_FrustumPlanes", m_frustumPlanes);
			m_activeHandlesBuffer.SetData(m_activeHandles);
			m_config.cullingProcessShader.SetBuffer(m_kiCalculateCullResult, "_ActiveHandles", m_activeHandlesBuffer);
			m_config.cullingProcessShader.SetInt("_ActiveHandlesCount", m_activeHandles.Count);
			m_config.cullingProcessShader.SetBuffer(m_kiCalculateCullResult, "_Positions", m_positionsBuffer);
			m_config.cullingProcessShader.SetBuffer(m_kiCalculateCullResult, "_Offsets", m_offsetsBuffer);
			m_config.cullingProcessShader.SetBuffer(m_kiCalculateCullResult, "_Extents", m_extentsBuffer);
			m_config.cullingProcessShader.SetBuffer(m_kiCalculateCullResult, "_Rotations", m_rotationsBuffer);
			m_config.cullingProcessShader.SetBuffer(m_kiCalculateCullResult, "_SphereRadius", m_sphereRadiusBuffer);
			m_config.cullingProcessShader.SetBuffer(m_kiCalculateCullResult, "_EntityType", m_entityTypeBuffer);
			m_config.cullingProcessShader.SetBuffer(m_kiCalculateCullResult, "_CullResult", m_cullResultBuffer);
			m_config.cullingProcessShader.SetBuffer(m_kiClearCullResult, "_CullResult", m_cullResultBuffer);
			m_config.cullingProcessShader.SetInt("_CullResultCount", m_cullResult.Length);
		}

		protected virtual void GetData()
		{
			if (m_cullResultReadRequest.done)
			{
				m_cullResultReadRequest = AsyncGPUReadback.RequestIntoNativeArray(ref m_cullResultTransferBuffer, m_cullResultBuffer, delegate
				{
					ProcessPackedTransferRequest(ref m_cullResultTransferBuffer, m_cullResult);
					NotifyChangedEntities();
				});
			}
		}

		private void ProcessPackedTransferRequest(ref NativeArray<uint> transferBuffer, byte[] dataBuffer)
		{
			if (!transferBuffer.IsCreated)
			{
				return;
			}
			if (dataBuffer != null)
			{
				for (int i = 0; i < dataBuffer.Length; i++)
				{
					int index = i >> 5;
					int num = i & 0x1F;
					uint num2 = (uint)(1 << num);
					bool flag = (transferBuffer[index] & num2) != 0;
					dataBuffer[i] = (byte)(flag ? 1u : 0u);
				}
			}
			if (!IsInitialized())
			{
				transferBuffer.Dispose();
			}
		}

		private void NotifyChangedEntities()
		{
			if (!IsInitialized())
			{
				return;
			}
			for (int i = 0; i < m_activeHandles.Count; i++)
			{
				int num = m_activeHandles[i];
				byte num2 = m_oldCullResult[num];
				byte b = m_cullResult[num];
				if (num2 != b)
				{
					m_oldCullResult[num] = b;
					NotifyVisibilityChange(num, b == 1);
				}
			}
		}

		protected virtual void NotifyVisibilityChange(int handle, bool isVisible)
		{
			if (!m_eventSubscribers.TryGetValue(handle, out var value))
			{
				return;
			}
			foreach (VisibilityChangeDelegate item in value)
			{
				item?.Invoke(isVisible);
			}
		}

		private void DispatchClearCullResult(CommandBuffer cmd)
		{
			int threadGroupsX = Mathf.CeilToInt((float)m_cullResult.Length / 64f);
			cmd.DispatchCompute(m_config.cullingProcessShader, m_kiClearCullResult, threadGroupsX, 1, 1);
		}

		private void DispatchCalculateCullResult(CommandBuffer cmd)
		{
			int threadGroupsX = Mathf.CeilToInt((float)m_activeHandles.Count / 64f);
			cmd.DispatchCompute(m_config.cullingProcessShader, m_kiCalculateCullResult, threadGroupsX, 1, 1);
		}

		private void RecordFrameCount()
		{
			int frameCount = Time.frameCount;
			m_combineResult = m_lastFrameCount == frameCount;
			m_lastFrameCount = frameCount;
		}

		private void ExtractCameraData(ref CustomPassContext ctx)
		{
			Camera camera = ctx.hdCamera.camera;
			if (camera != null)
			{
				m_cameraPosition = camera.transform.position;
				m_cameraViewProjectionMatrix = camera.projectionMatrix * camera.worldToCameraMatrix;
			}
		}

		private void ExtractFrustumPlanes()
		{
			ref Matrix4x4 cameraViewProjectionMatrix = ref m_cameraViewProjectionMatrix;
			m_frustumPlanes[0] = new Vector4(cameraViewProjectionMatrix.m30 + cameraViewProjectionMatrix.m00, cameraViewProjectionMatrix.m31 + cameraViewProjectionMatrix.m01, cameraViewProjectionMatrix.m32 + cameraViewProjectionMatrix.m02, cameraViewProjectionMatrix.m33 + cameraViewProjectionMatrix.m03);
			m_frustumPlanes[1] = new Vector4(cameraViewProjectionMatrix.m30 - cameraViewProjectionMatrix.m00, cameraViewProjectionMatrix.m31 - cameraViewProjectionMatrix.m01, cameraViewProjectionMatrix.m32 - cameraViewProjectionMatrix.m02, cameraViewProjectionMatrix.m33 - cameraViewProjectionMatrix.m03);
			m_frustumPlanes[2] = new Vector4(cameraViewProjectionMatrix.m30 + cameraViewProjectionMatrix.m10, cameraViewProjectionMatrix.m31 + cameraViewProjectionMatrix.m11, cameraViewProjectionMatrix.m32 + cameraViewProjectionMatrix.m12, cameraViewProjectionMatrix.m33 + cameraViewProjectionMatrix.m13);
			m_frustumPlanes[3] = new Vector4(cameraViewProjectionMatrix.m30 - cameraViewProjectionMatrix.m10, cameraViewProjectionMatrix.m31 - cameraViewProjectionMatrix.m11, cameraViewProjectionMatrix.m32 - cameraViewProjectionMatrix.m12, cameraViewProjectionMatrix.m33 - cameraViewProjectionMatrix.m13);
			m_frustumPlanes[4] = new Vector4(cameraViewProjectionMatrix.m30 + cameraViewProjectionMatrix.m20, cameraViewProjectionMatrix.m31 + cameraViewProjectionMatrix.m21, cameraViewProjectionMatrix.m32 + cameraViewProjectionMatrix.m22, cameraViewProjectionMatrix.m33 + cameraViewProjectionMatrix.m23);
			m_frustumPlanes[5] = new Vector4(cameraViewProjectionMatrix.m30 - cameraViewProjectionMatrix.m20, cameraViewProjectionMatrix.m31 - cameraViewProjectionMatrix.m21, cameraViewProjectionMatrix.m32 - cameraViewProjectionMatrix.m22, cameraViewProjectionMatrix.m33 - cameraViewProjectionMatrix.m23);
			for (int i = 0; i < m_frustumPlanes.Length; i++)
			{
				Vector3 vector = new Vector3(m_frustumPlanes[i].x, m_frustumPlanes[i].y, m_frustumPlanes[i].z);
				m_frustumPlanes[i] /= vector.magnitude;
			}
		}

		private void UpdateDynamicEntity()
		{
			for (int i = 0; i < m_activeHandles.Count; i++)
			{
				int num = m_activeHandles[i];
				if (m_dynamicEntityBinds.TryGetValue(num, out var value))
				{
					if (value == null)
					{
						m_dynamicEntityBinds.Remove(num);
						continue;
					}
					m_positions[num] = value.position;
					m_rotations[num] = value.rotation;
				}
			}
			m_positionsBuffer.SetData(m_positions);
			m_rotationsBuffer.SetData(m_rotations);
		}

		protected virtual void ResetEntity(int handle)
		{
			if (m_eventSubscribers.TryGetValue(handle, out var value))
			{
				value.Clear();
				m_eventSubscribers.Remove(handle);
			}
			m_positions[handle] = Vector3.zero;
			m_positionsBuffer.SetData(m_positions);
			m_offsets[handle] = Vector3.zero;
			m_offsetsBuffer.SetData(m_offsets);
			m_extents[handle] = Vector3.zero;
			m_extentsBuffer.SetData(m_extents);
			m_rotations[handle] = Quaternion.identity;
			m_rotationsBuffer.SetData(m_rotations);
			m_sphereRadius[handle] = 0f;
			m_sphereRadiusBuffer.SetData(m_sphereRadius);
			m_entityType.Remove(handle);
			m_dynamicEntityBinds.Remove(handle);
		}

		private void SetEntityType(int handle, EntityType type)
		{
			int computeBufferStartIndex = handle >> 5;
			int num = handle & 0x1F;
			uint num2 = (uint)(1 << num);
			m_entityType[handle] = type;
			uint[] array = new uint[1];
			m_entityTypeBuffer.GetData(array, 0, computeBufferStartIndex, 1);
			if (type == EntityType.Sound)
			{
				array[0] |= num2;
			}
			else
			{
				array[0] &= ~num2;
			}
			m_entityTypeBuffer.SetData(array, 0, computeBufferStartIndex, 1);
		}

		private CullingHandle GetFreeHandle()
		{
			for (int i = 0; i < m_config.capacity; i++)
			{
				if (!m_usedHandles[i])
				{
					return new CullingHandle(i);
				}
			}
			return CullingHandle.Invalid;
		}

		private void UpdateServiceState()
		{
			bool flag = CanEnabled();
			if (m_enabled == flag)
			{
				return;
			}
			m_enabled = flag;
			if (IsInitialized())
			{
				for (int i = 0; i < m_activeHandles.Count; i++)
				{
					int handle = m_activeHandles[i];
					NotifyVisibilityChange(handle, IsEntityVisible(handle));
				}
			}
		}

		private bool CanEnabled()
		{
			if (XRSettings.enabled && XRSettings.isDeviceActive)
			{
				return false;
			}
			if (States.mainCurrent is GarageGUIState)
			{
				return false;
			}
			return true;
		}
	}
}
