using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace GpuCulling
{
	public class GpuCullingServiceDebug : GpuCullingService
	{
		private enum CullResultType
		{
			Skipped = 0,
			Distance = 1,
			Frustum = 2,
			Occlusion = 3
		}

		private enum CullingDataType
		{
			CarUVMinX = 0,
			CarUVMinY = 1,
			CarUVMaxX = 2,
			CarUVMaxY = 3,
			Hiz = 4,
			Depth = 5,
			CullResult = 6,
			Count = 7
		}

		private const string DebugOutputKeyword = "DEBUG_OUTPUT";

		private const float TimeToShowChanges = 2f;

		private static Material m_glMaterial;

		private bool m_debugOutput;

		private bool m_enableService = true;

		private float[] m_debugOutputData;

		private ComputeBuffer m_debugOutputDataBuffer;

		private NativeArray<float> m_debugOutputDataTransferBuffer;

		private AsyncGPUReadbackRequest m_debugOutputDataReadRequest;

		private RenderTexture m_depthResult;

		private Dictionary<int, Object> m_entityToObjects = new Dictionary<int, Object>();

		private Dictionary<int, float> m_entitiesWithChanges = new Dictionary<int, float>();

		public static GpuCullingServiceDebug debugInstance => (GpuCullingServiceDebug)GpuCullingService.instance;

		public bool IsDebugOutputEnabled()
		{
			return m_debugOutput;
		}

		public bool IsServiceEnabled()
		{
			return m_enableService;
		}

		private int OutputDataStride()
		{
			return 7;
		}

		private CullResultType GetEntityCullResult(int handle)
		{
			return (CullResultType)GetDebugData(handle, CullingDataType.CullResult);
		}

		private float GetDebugData(int handle, CullingDataType type)
		{
			return m_debugOutputData[(int)(handle * OutputDataStride() + type)];
		}

		private bool IsEntityHasChanges(int handle)
		{
			return m_entitiesWithChanges.ContainsKey(handle);
		}

		public void SwitchDebugMode(bool enabled)
		{
			m_debugOutput = enabled;
		}

		public void SwitchEnableService(bool enabled)
		{
			m_enableService = enabled;
			for (int i = 0; i < m_activeHandles.Count; i++)
			{
				int handle = m_activeHandles[i];
				if (enabled)
				{
					NotifyVisibilityChange(handle, IsEntityVisible(handle));
				}
				else
				{
					NotifyVisibilityChange(handle, isVisible: true);
				}
			}
		}

		protected override bool DefineScreenSize(ref CustomPassContext ctx)
		{
			bool num = base.DefineScreenSize(ref ctx);
			if (num)
			{
				m_depthResult.Release();
				m_depthResult.width = (int)m_screenSize.x;
				m_depthResult.height = (int)m_screenSize.y;
			}
			return num;
		}

		public void BindObjectForDebug(int handle, Object obj)
		{
			m_entityToObjects[handle] = obj;
		}

		public override void Process(ref CustomPassContext ctx)
		{
			if (m_enableService)
			{
				base.Process(ref ctx);
			}
		}

		protected override void ResetEntity(int handle)
		{
			base.ResetEntity(handle);
			m_entityToObjects.Remove(handle);
			m_entitiesWithChanges.Remove(handle);
		}

		protected override void NotifyVisibilityChange(int handle, bool isVisible)
		{
			base.NotifyVisibilityChange(handle, isVisible);
			m_entitiesWithChanges[handle] = 2f;
		}

		protected override void CreateBuffers()
		{
			base.CreateBuffers();
			int num = m_config.capacity * OutputDataStride();
			m_debugOutputData = new float[num];
			m_debugOutputDataBuffer = new ComputeBuffer(num, Marshal.SizeOf(typeof(float)));
			m_debugOutputDataTransferBuffer = new NativeArray<float>(num, Allocator.Persistent);
			CreateGLMaterial();
			CreateDepthResultTexture();
		}

		protected override void DestroyBuffers()
		{
			base.DestroyBuffers();
			m_debugOutputData = null;
			m_debugOutputDataBuffer.Dispose();
			ReleaseGLMaterial();
			ReleaseDepthResultTexture();
		}

		protected override void BindData()
		{
			base.BindData();
			if (m_debugOutput)
			{
				m_config.cullingProcessShader.EnableKeyword("DEBUG_OUTPUT");
				m_config.cullingProcessShader.SetInt("_DebugOutputDataStride", OutputDataStride());
				m_config.cullingProcessShader.SetBuffer(m_kiCalculateCullResult, "_DebugOutputData", m_debugOutputDataBuffer);
				m_config.cullingProcessShader.SetTexture(m_kiCalculateCullResult, "_DepthResult", m_depthResult);
				m_config.cullingProcessShader.SetTexture(m_kiClearCullResult, "_DepthResult", m_depthResult);
				m_config.cullingProcessShader.SetBool("_CombineResult", m_combineResult);
			}
			else
			{
				m_config.cullingProcessShader.DisableKeyword("DEBUG_OUTPUT");
			}
		}

		protected override void GetData()
		{
			base.GetData();
			if (!m_debugOutput || !m_debugOutputDataReadRequest.done)
			{
				return;
			}
			m_debugOutputDataReadRequest = AsyncGPUReadback.RequestIntoNativeArray(ref m_debugOutputDataTransferBuffer, m_debugOutputDataBuffer, delegate
			{
				if (m_debugOutputDataTransferBuffer.IsCreated)
				{
					if (m_debugOutputData != null)
					{
						m_debugOutputDataTransferBuffer.CopyTo(m_debugOutputData);
					}
					if (!IsInitialized())
					{
						m_debugOutputDataTransferBuffer.Dispose();
					}
				}
			});
		}

		private void UpdateChangedEntities()
		{
			float deltaTime = Time.deltaTime;
			foreach (int item in m_entitiesWithChanges.Keys.ToList())
			{
				m_entitiesWithChanges[item] -= deltaTime;
				if (m_entitiesWithChanges[item] <= 0f)
				{
					m_entitiesWithChanges.Remove(item);
				}
			}
		}

		public void OnGUI()
		{
			if (!m_debugOutput || !IsInitialized())
			{
				return;
			}
			Rect position = new Rect(30f, 30f, 200f, 30f);
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			UpdateChangedEntities();
			Vector2 screenSize = m_screenSize;
			GUI.DrawTexture(new Rect(0f, 0f, m_depthResult.width, m_depthResult.height), m_depthResult, ScaleMode.ScaleToFit);
			foreach (KeyValuePair<int, Object> entityToObject in m_entityToObjects)
			{
				entityToObject.Deconstruct(out var key, out var value);
				int entityHandle = key;
				Object obj = value;
				Color color = GUI.color;
				GUI.color = GetColorFromCullingResult(entityHandle);
				GUI.Label(position, obj.name ?? "");
				position.y += 25f;
				GUI.color = color;
			}
			position = new Rect(Screen.width - 500, 30f, 500f, 30f);
			for (int i = 0; i < m_activeHandles.Count; i++)
			{
				int num = m_activeHandles[i];
				Color colorFromCullingResult = GetColorFromCullingResult(num);
				Vector2 vector = new Vector2(GetDebugData(num, CullingDataType.CarUVMinX) * screenSize.x, (1f - GetDebugData(num, CullingDataType.CarUVMinY)) * screenSize.y);
				Vector2 size = new Vector2(GetDebugData(num, CullingDataType.CarUVMaxX) * screenSize.x, (1f - GetDebugData(num, CullingDataType.CarUVMaxY)) * screenSize.y) - vector;
				Rect area = new Rect(vector, size);
				DrawBorderRect(area, colorFromCullingResult, 5f);
				Color color2 = GUI.color;
				GUI.color = colorFromCullingResult;
				GUI.Label(position, $"ID: {num}, HIZ: {GetDebugData(num, CullingDataType.Hiz)}, DEPTH: {GetDebugData(num, CullingDataType.Depth)}");
				position.y += 25f;
				GUI.color = color2;
			}
		}

		private Color GetColorFromCullingResult(int entityHandle)
		{
			if (IsEntityHasChanges(entityHandle))
			{
				return Color.white;
			}
			return GetEntityCullResult(entityHandle) switch
			{
				CullResultType.Skipped => Color.green, 
				CullResultType.Distance => Color.magenta, 
				CullResultType.Frustum => Color.blue, 
				CullResultType.Occlusion => Color.yellow, 
				_ => Color.black, 
			};
		}

		private void DrawBorderRect(Rect area, Color color, float borderWidth)
		{
			m_glMaterial.SetPass(0);
			GL.PushMatrix();
			GL.LoadPixelMatrix();
			GL.Begin(7);
			GL.Color(color);
			float x = area.x;
			float y = area.y;
			float width = area.width;
			float height = area.height;
			DrawQuad(x, y, width + borderWidth, borderWidth);
			DrawQuad(x + width, y, borderWidth, height + borderWidth);
			DrawQuad(x, y, borderWidth, height);
			DrawQuad(x, y + height, width + borderWidth, borderWidth);
			GL.End();
			GL.PopMatrix();
		}

		private void DrawQuad(float x, float y, float width, float height)
		{
			GL.Vertex3(x, y, 0f);
			GL.Vertex3(x + width, y, 0f);
			GL.Vertex3(x + width, y + height, 0f);
			GL.Vertex3(x, y + height, 0f);
		}

		private void DrawAABBObject(int handle)
		{
			Gizmos.matrix = Matrix4x4.TRS(m_positions[handle], m_rotations[handle], Vector3.one);
			Gizmos.DrawWireCube(Vector3.zero, m_extents[handle] * 2f);
		}

		private void DrawSoundSphere(int handle)
		{
			Gizmos.matrix = Matrix4x4.TRS(m_positions[handle], Quaternion.identity, Vector3.one);
			Gizmos.DrawWireSphere(Vector3.zero, m_sphereRadius[handle]);
		}

		private void CreateGLMaterial()
		{
			m_glMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
			m_glMaterial.hideFlags = HideFlags.HideAndDontSave;
			m_glMaterial.SetInt("_SrcBlend", 5);
			m_glMaterial.SetInt("_DstBlend", 10);
			m_glMaterial.SetInt("_Cull", 0);
			m_glMaterial.SetInt("_ZWrite", 0);
		}

		private void ReleaseGLMaterial()
		{
			Object.DestroyImmediate(m_glMaterial);
			m_glMaterial = null;
		}

		private void CreateDepthResultTexture()
		{
			m_depthResult = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32)
			{
				enableRandomWrite = true,
				useMipMap = false,
				filterMode = FilterMode.Point
			};
			m_depthResult.Create();
		}

		private void ReleaseDepthResultTexture()
		{
			m_depthResult.Release();
			Object.DestroyImmediate(m_depthResult);
			m_depthResult = null;
		}
	}
}
