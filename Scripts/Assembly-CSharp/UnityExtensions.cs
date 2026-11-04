using System;
using System.Collections.Generic;
using System.Text;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

public static class UnityExtensions
{
	public class ColorToJSONHelper
	{
		public byte? R;

		public byte? G;

		public byte? B;

		public byte? A;

		public ColorToJSONHelper()
		{
		}

		public ColorToJSONHelper(Color color)
		{
			R = (byte)(color.r * 255f);
			G = (byte)(color.g * 255f);
			B = (byte)(color.b * 255f);
			A = (byte)(color.a * 255f);
		}

		public ColorToJSONHelper(string jsonString)
		{
			ColorToJSONHelper colorToJSONHelper = JSON.Parse<ColorToJSONHelper>(jsonString);
			R = colorToJSONHelper.R;
			G = colorToJSONHelper.G;
			B = colorToJSONHelper.B;
			A = colorToJSONHelper.A;
		}
	}

	private static List<Transform> m_cacheTransforms = new List<Transform>(64);

	public static bool IsSceneObject(this Component comp)
	{
		if (comp != null)
		{
			return comp.gameObject.scene.rootCount != 0;
		}
		return false;
	}

	public static bool IsSceneObject(this Transform tr)
	{
		if (tr != null)
		{
			return tr.gameObject.scene.rootCount != 0;
		}
		return false;
	}

	public static bool IsSceneObject(this GameObject go)
	{
		if (go != null)
		{
			return go.scene.rootCount != 0;
		}
		return false;
	}

	public static void SnapScrollTo(this ScrollRect scrollRect, RectTransform target)
	{
		float num = 0f - scrollRect.content.anchoredPosition.y;
		float num2 = ((target.anchoredPosition.y > num) ? (0f - target.anchoredPosition.y) : (0f - (target.anchoredPosition.y - target.rect.height)));
		float verticalNormalizedPosition = Mathf.Clamp01((scrollRect.content.rect.height - num2) / scrollRect.content.rect.height);
		scrollRect.verticalNormalizedPosition = verticalNormalizedPosition;
		Canvas.ForceUpdateCanvases();
	}

	public static void SnapsScrollToHorisontal(this ScrollRect scrollRect, RectTransform target)
	{
		float num = 0f - scrollRect.content.anchoredPosition.x;
		float horizontalNormalizedPosition = Mathf.Clamp01(((target.anchoredPosition.x > num) ? (target.anchoredPosition.x + target.rect.width) : (target.anchoredPosition.x - target.rect.width)) / scrollRect.content.rect.width);
		scrollRect.horizontalNormalizedPosition = horizontalNormalizedPosition;
		Canvas.ForceUpdateCanvases();
	}

	public static bool IsScrollItemVerticalyVisible(this ScrollRect scrollRect, RectTransform target, float padding = 0f)
	{
		float height = scrollRect.viewport.rect.height;
		Vector2 anchoredPosition = scrollRect.content.anchoredPosition;
		float y = target.anchoredPosition.y;
		float num = y - target.rect.height;
		float num2 = 0f - anchoredPosition.y - padding;
		float num3 = 0f - anchoredPosition.y - height + padding;
		if (y - num2 <= 0.001f || Mathf.Approximately(y, num2))
		{
			if (!(num - num3 >= 0.001f))
			{
				return Mathf.Approximately(num, num3);
			}
			return true;
		}
		return false;
	}

	public static bool IsScrollItemHorisontalyVisible(this ScrollRect scrollRect, RectTransform target, float padding = 0f)
	{
		float width = scrollRect.viewport.rect.width;
		Vector2 anchoredPosition = scrollRect.content.anchoredPosition;
		float x = target.anchoredPosition.x;
		float num = x + target.rect.width;
		float num2 = 0f - anchoredPosition.x - padding;
		float num3 = 0f - anchoredPosition.x + width + padding;
		if (x > num2)
		{
			return num < num3;
		}
		return false;
	}

	public static bool IsScrollItemFloatVerticalyVisible(this ScrollRect scrollRect, float topPos, float elementHeight, float bottomOffset = 0f)
	{
		float height = scrollRect.viewport.rect.height;
		Vector2 anchoredPosition = scrollRect.content.anchoredPosition;
		float num = topPos - elementHeight;
		float num2 = 0f - anchoredPosition.y;
		float num3 = 0f - anchoredPosition.y - height + bottomOffset;
		if (topPos < num2)
		{
			return num > num3;
		}
		return false;
	}

	public static void SnapScrollToWithLerp(this ScrollRect scrollRect, float topPos, float elementHeight, ref bool needUpdate, float bottomOffset = 0f, float findOffset = 0f, float stopOffset = 0f)
	{
		Vector2 anchoredPosition = scrollRect.content.anchoredPosition;
		float num = topPos - elementHeight;
		float height = scrollRect.viewport.rect.height;
		float num2 = 0f - scrollRect.content.anchoredPosition.y;
		float num3 = num2 - height;
		float num4 = 0f;
		if (topPos > num2 && num > num3)
		{
			num4 = 0f - topPos - stopOffset;
		}
		else
		{
			float num5 = height - elementHeight;
			num4 = 0f - (topPos - bottomOffset + num5) + stopOffset;
		}
		Vector2 b = new Vector2(scrollRect.content.anchoredPosition.x, num4);
		scrollRect.content.anchoredPosition = Vector2.Lerp(anchoredPosition, b, Time.unscaledDeltaTime * 6f);
		num2 = 0f - scrollRect.content.anchoredPosition.y;
		num3 = num2 - height;
		float num6 = 0f - num4;
		if (num2 + findOffset >= num6 && num2 - findOffset <= num6)
		{
			needUpdate = false;
		}
	}

	public static void HorizontalSnapScrollTo(this ScrollRect scrollRect, RectTransform target, float padding = 0f)
	{
		float width = scrollRect.viewport.rect.width;
		bool num = target.anchoredPosition.x > 0f - scrollRect.content.anchoredPosition.x;
		Vector2 vector = scrollRect.transform.InverseTransformPoint(scrollRect.content.position);
		Vector2 vector2 = scrollRect.transform.InverseTransformPoint(target.position);
		float x = (num ? (vector.x - (vector2.x + target.rect.width + padding) + width) : (vector.x - (vector2.x - padding)));
		scrollRect.content.anchoredPosition = new Vector2(x, scrollRect.content.anchoredPosition.y);
		Canvas.ForceUpdateCanvases();
	}

	public static bool IsScrollItemHorizontallyVisible(this ScrollRect scrollRect, RectTransform target)
	{
		float width = scrollRect.viewport.rect.width;
		Vector2 anchoredPosition = scrollRect.content.anchoredPosition;
		float num = target.anchoredPosition.x - target.rect.width / 2f;
		float num2 = target.anchoredPosition.x + target.rect.width / 2f;
		float num3 = 0f - anchoredPosition.x;
		float num4 = 0f - anchoredPosition.x + width;
		if (!(num < num3))
		{
			return !(num2 > num4);
		}
		return false;
	}

	public static GameObject RemoveCloneFromName(this GameObject self)
	{
		int num = self.name.IndexOf("(Clone)");
		if (num != -1)
		{
			self.name = self.name.Substring(0, num);
		}
		return self;
	}

	public static MonoBehaviour RemoveCloneFromName(this MonoBehaviour self)
	{
		self.gameObject.RemoveCloneFromName();
		return self;
	}

	public static T FindInParents<T>(this GameObject go) where T : Component
	{
		if (go == null)
		{
			return null;
		}
		if (!go.TryGetComponent<T>(out var component))
		{
			Transform parent = go.transform.parent;
			while (parent != null && component == null)
			{
				parent.gameObject.TryGetComponent<T>(out component);
				parent = parent.parent;
			}
		}
		return component;
	}

	public static T GetOrAddComponent<T>(this GameObject go) where T : Component
	{
		if (go == null)
		{
			return null;
		}
		if (!go.TryGetComponent<T>(out var component))
		{
			return go.AddComponent<T>();
		}
		return component;
	}

	public static GameObject FindChildWithName(this GameObject go, string name)
	{
		if (go == null)
		{
			return null;
		}
		Transform transform = go.transform.FindChildDeph(name);
		if (!transform)
		{
			return null;
		}
		return transform.gameObject;
	}

	public static Transform FindChildDeph(this Transform parent, string name)
	{
		if (parent == null)
		{
			return null;
		}
		if (parent.name == name)
		{
			return parent;
		}
		m_cacheTransforms.Clear();
		parent.GetComponentsInChildren(includeInactive: true, m_cacheTransforms);
		foreach (Transform cacheTransform in m_cacheTransforms)
		{
			if (cacheTransform.name == name)
			{
				return cacheTransform;
			}
		}
		return null;
	}

	public static Transform FindChildDeph(this Transform parent, Func<Transform, bool> match)
	{
		if (parent == null)
		{
			return null;
		}
		if (match(parent))
		{
			return parent;
		}
		m_cacheTransforms.Clear();
		parent.GetComponentsInChildren(includeInactive: true, m_cacheTransforms);
		foreach (Transform cacheTransform in m_cacheTransforms)
		{
			if (match(cacheTransform))
			{
				return cacheTransform;
			}
		}
		return null;
	}

	public static Transform FindChildWithTag(this Transform parent, string tag)
	{
		if (parent == null)
		{
			return null;
		}
		if (parent.CompareTag(tag))
		{
			return parent;
		}
		m_cacheTransforms.Clear();
		parent.GetComponentsInChildren(includeInactive: true, m_cacheTransforms);
		foreach (Transform cacheTransform in m_cacheTransforms)
		{
			if (cacheTransform.CompareTag(tag))
			{
				return cacheTransform;
			}
		}
		return null;
	}

	public static GameObject FindChildWithTag(this GameObject go, string tag)
	{
		if (go == null)
		{
			return null;
		}
		Transform transform = go.transform.FindChildWithTag(tag);
		if (!transform)
		{
			return null;
		}
		return transform.gameObject;
	}

	public static T GetExistingComponent<T>(this Component _this) where T : Component
	{
		return _this.gameObject.GetExistingComponent<T>();
	}

	public static T GetExistingComponent<T>(this GameObject _this) where T : Component
	{
		if (!_this.TryGetComponent<T>(out var component))
		{
			GameLog.Print(AlertLevel.Error, "Debug", _this.name + ": required component not found: " + typeof(T).Name);
		}
		return component;
	}

	public static T GetComponent<T>(this Component _this, string childName) where T : Component
	{
		return _this.gameObject.GetComponent<T>(childName);
	}

	public static T GetComponent<T>(this GameObject _this, string childName) where T : Component
	{
		Transform transform = _this.transform.Find(childName);
		if (transform != null && transform.TryGetComponent<T>(out var component))
		{
			return component;
		}
		return null;
	}

	public static T GetExistingComponent<T>(this Component _this, string childName) where T : Component
	{
		return _this.gameObject.GetExistingComponent<T>(childName);
	}

	public static T GetExistingComponent<T>(this GameObject _this, string childName) where T : Component
	{
		Transform transform = _this.transform.Find(childName);
		T component = null;
		if (transform != null)
		{
			transform.TryGetComponent<T>(out component);
		}
		if (component == null)
		{
			GameLog.Print(AlertLevel.Error, "Debug", _this.name + ": required component or child object not found: '" + childName + "'." + typeof(T).Name);
		}
		return component;
	}

	public static void SetLayerRecursively(this GameObject go, int layer)
	{
		go.layer = layer;
		foreach (Transform item in go.transform)
		{
			item.gameObject.SetLayerRecursively(layer);
		}
	}

	public static void ReplaceLayerRecursively(this GameObject go, int fromLayer, int toLayer)
	{
		Transform[] componentsInChildren = go.GetComponentsInChildren<Transform>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			GameObject gameObject = componentsInChildren[i].gameObject;
			if (gameObject.layer == fromLayer)
			{
				gameObject.layer = toLayer;
			}
		}
	}

	public static void ApplyRecursively(this Transform transform, Action<Transform> applyAction)
	{
		applyAction(transform);
		foreach (Transform item in transform)
		{
			item.ApplyRecursively(applyAction);
		}
	}

	public static void ForEachChildrenComponents<T>(this Transform transform, Action<T> action)
	{
		T[] componentsInChildren = transform.GetComponentsInChildren<T>(includeInactive: true);
		foreach (T param in componentsInChildren)
		{
			action.SafeCall(param);
		}
	}

	public static void ResetPRS(this Transform obj)
	{
		obj.localPosition = Vector3.zero;
		obj.localRotation = Quaternion.identity;
		obj.localScale = Vector3.one;
	}

	public static void ResetPRS(this GameObject obj)
	{
		obj.transform.localPosition = Vector3.zero;
		obj.transform.localRotation = Quaternion.identity;
		obj.transform.localScale = Vector3.one;
	}

	public static void ResetPRS(this Component obj)
	{
		obj.transform.localPosition = Vector3.zero;
		obj.transform.localRotation = Quaternion.identity;
		obj.transform.localScale = Vector3.one;
	}

	public static void AddChildResetPRS(this Transform obj, GameObject child)
	{
		child.transform.parent = obj;
		child.transform.localPosition = Vector3.zero;
		child.transform.localRotation = Quaternion.identity;
		child.transform.localScale = Vector3.one;
	}

	public static void AddChildResetPRS(this GameObject obj, GameObject child)
	{
		child.transform.parent = obj.transform;
		child.transform.localPosition = Vector3.zero;
		child.transform.localRotation = Quaternion.identity;
		child.transform.localScale = Vector3.one;
	}

	public static void AddChildResetPRS(this Transform obj, Component child)
	{
		child.transform.parent = obj;
		child.transform.localPosition = Vector3.zero;
		child.transform.localRotation = Quaternion.identity;
		child.transform.localScale = Vector3.one;
	}

	public static void AddChildResetPRS(this GameObject obj, Component child)
	{
		child.transform.parent = obj.transform;
		child.transform.localPosition = Vector3.zero;
		child.transform.localRotation = Quaternion.identity;
		child.transform.localScale = Vector3.one;
	}

	public static void AddChildResetPRS(this Transform obj, GameObject child, Vector3 localPosition)
	{
		child.transform.parent = obj;
		child.transform.localPosition = localPosition;
		child.transform.localRotation = Quaternion.identity;
		child.transform.localScale = Vector3.one;
	}

	public static void AddChildResetPRS(this GameObject obj, GameObject child, Vector3 localPosition)
	{
		child.transform.parent = obj.transform;
		child.transform.localPosition = localPosition;
		child.transform.localRotation = Quaternion.identity;
		child.transform.localScale = Vector3.one;
	}

	public static void AddChildResetPRS(this Transform obj, Component child, Vector3 localPosition)
	{
		child.transform.parent = obj;
		child.transform.localPosition = localPosition;
		child.transform.localRotation = Quaternion.identity;
		child.transform.localScale = Vector3.one;
	}

	public static void AddChildResetPRS(this GameObject obj, Component child, Vector3 localPosition)
	{
		child.transform.parent = obj.transform;
		child.transform.localPosition = localPosition;
		child.transform.localRotation = Quaternion.identity;
		child.transform.localScale = Vector3.one;
	}

	public static void AlignAs(this GameObject obj, GameObject other)
	{
		obj.transform.position = other.transform.position;
		obj.transform.rotation = other.transform.rotation;
		obj.transform.localScale = other.transform.localScale;
	}

	public static void AlignAs(this GameObject obj, Transform other)
	{
		obj.transform.position = other.position;
		obj.transform.rotation = other.rotation;
		obj.transform.localScale = other.localScale;
	}

	public static string GetFullPath(this GameObject obj)
	{
		return obj.transform.GetFullPath();
	}

	public static string GetFullPath(this Transform obj)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(obj.name);
		Transform parent = obj.parent;
		while (parent != null)
		{
			stringBuilder.Insert(0, parent.name + "/");
			parent = parent.parent;
		}
		return stringBuilder.ToString();
	}

	public static void SetX(this Transform tr, float x)
	{
		Vector3 position = tr.position;
		position.x = x;
		tr.position = position;
	}

	public static void SetY(this Transform tr, float y)
	{
		Vector3 position = tr.position;
		position.y = y;
		tr.position = position;
	}

	public static void SetZ(this Transform tr, float z)
	{
		Vector3 position = tr.position;
		position.z = z;
		tr.position = position;
	}

	public static void ShiftX(this Transform tr, float ox)
	{
		Vector3 position = tr.position;
		position.x += ox;
		tr.position = position;
	}

	public static void ShiftY(this Transform tr, float oy)
	{
		Vector3 position = tr.position;
		position.y += oy;
		tr.position = position;
	}

	public static void ShiftZ(this Transform tr, float oz)
	{
		Vector3 position = tr.position;
		position.z += oz;
		tr.position = position;
	}

	public static void SetLocalX(this Transform tr, float x)
	{
		Vector3 localPosition = tr.localPosition;
		localPosition.x = x;
		tr.localPosition = localPosition;
	}

	public static void SetLocalY(this Transform tr, float y)
	{
		Vector3 localPosition = tr.localPosition;
		localPosition.y = y;
		tr.localPosition = localPosition;
	}

	public static void SetLocalZ(this Transform tr, float z)
	{
		Vector3 localPosition = tr.localPosition;
		localPosition.z = z;
		tr.localPosition = localPosition;
	}

	public static void ShiftLocalX(this Transform tr, float ox)
	{
		Vector3 localPosition = tr.localPosition;
		localPosition.x += ox;
		tr.localPosition = localPosition;
	}

	public static void ShiftLocalY(this Transform tr, float oy)
	{
		Vector3 localPosition = tr.localPosition;
		localPosition.y += oy;
		tr.localPosition = localPosition;
	}

	public static void ShiftLocalZ(this Transform tr, float oz)
	{
		Vector3 localPosition = tr.localPosition;
		localPosition.z += oz;
		tr.localPosition = localPosition;
	}

	public static Vector2 GetFirstTouchOrMousePosition()
	{
		if (Input.touchCount <= 0)
		{
			return new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		}
		return Input.touches[0].position;
	}

	public static string ToJSON(this Color color)
	{
		return JSON.Serialize(new ColorToJSONHelper(color));
	}

	public static Color FromJSON(this Color color, string jsonString)
	{
		ColorToJSONHelper colorToJSONHelper = new ColorToJSONHelper(jsonString);
		color.r = (float)(int)colorToJSONHelper.R.Value / 255f;
		color.g = (float)(int)colorToJSONHelper.G.Value / 255f;
		color.b = (float)(int)colorToJSONHelper.B.Value / 255f;
		color.a = (float)(int)colorToJSONHelper.A.Value / 255f;
		return color;
	}

	private static bool IsIn01(float v)
	{
		if (v >= 0f)
		{
			return v <= 1f;
		}
		return false;
	}

	public static bool IsValid(this Color color)
	{
		if (IsIn01(color.r) && IsIn01(color.g) && IsIn01(color.b))
		{
			return IsIn01(color.a);
		}
		return false;
	}

	public static bool AddOnce<T>(this List<T> obj, T item)
	{
		if (obj.IndexOf(item) < 0)
		{
			obj.Add(item);
			return true;
		}
		return false;
	}

	public static bool RandomBool()
	{
		return UnityEngine.Random.Range(0, 2) == 1;
	}

	public static float RandomFloat(float min, float max)
	{
		return UnityEngine.Random.Range(min, max);
	}

	public static int RandomInt(int min, int max)
	{
		return UnityEngine.Random.Range(min, max + 1);
	}

	public static bool RandomBool(System.Random random)
	{
		return random.Next(0, 2) == 1;
	}

	public static float RandomFloat(float min, float max, System.Random random)
	{
		return (float)(random.NextDouble() * (double)(max - min) + (double)min);
	}

	public static int RandomInt(int min, int max, System.Random random)
	{
		return random.Next(min, max + 1);
	}

	public static int RandomIndex<T>(this List<T> obj)
	{
		int count = obj.Count;
		if (count <= 0)
		{
			return -1;
		}
		return UnityEngine.Random.Range(0, count);
	}

	public static T RandomItem<T>(this List<T> obj)
	{
		int count = obj.Count;
		if (count > 0)
		{
			return obj[UnityEngine.Random.Range(0, count)];
		}
		return default(T);
	}

	public static T RandomItem<T>(this FastList<T> obj)
	{
		int size = obj.size;
		if (size > 0)
		{
			return obj[UnityEngine.Random.Range(0, size)];
		}
		return default(T);
	}

	public static int RandomIndex<T>(this T[] obj)
	{
		int num = obj.Length;
		if (num <= 0)
		{
			return -1;
		}
		return UnityEngine.Random.Range(0, num);
	}

	public static T RandomItem<T>(this T[] obj)
	{
		int num = obj.Length;
		if (num > 0)
		{
			return obj[UnityEngine.Random.Range(0, num)];
		}
		return default(T);
	}

	public static T RandomItem<T>(this List<T> obj, System.Random random)
	{
		int count = obj.Count;
		if (count > 0)
		{
			return obj[random.Next(0, count)];
		}
		return default(T);
	}

	public static int RandomIndex<T>(this T[] obj, System.Random random)
	{
		int num = obj.Length;
		if (num <= 0)
		{
			return -1;
		}
		return random.Next(0, num);
	}

	public static T RandomItem<T>(this T[] obj, System.Random random)
	{
		int num = obj.Length;
		if (num > 0)
		{
			return obj[random.Next(0, num)];
		}
		return default(T);
	}

	public static bool IsPassedThrough(this LogType baseType, LogType? otherType)
	{
		if (!otherType.HasValue)
		{
			return false;
		}
		LogType value = otherType.Value;
		switch (baseType)
		{
		case LogType.Exception:
			return true;
		case LogType.Assert:
			return value != LogType.Exception;
		case LogType.Error:
			if (value != LogType.Exception)
			{
				return value != LogType.Assert;
			}
			return false;
		case LogType.Warning:
			if (value != LogType.Log)
			{
				return value == LogType.Warning;
			}
			return true;
		case LogType.Log:
			return value == LogType.Log;
		default:
			return true;
		}
	}

	public static string ToRoman(this int number)
	{
		return number switch
		{
			1 => "I", 
			2 => "II", 
			3 => "III", 
			4 => "IV", 
			5 => "V", 
			_ => "X", 
		};
	}

	public static DateTime ToDateTimeFromUnixTime(this long unixTimeMillis)
	{
		return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(unixTimeMillis);
	}

	public static void Decompose(this Matrix4x4 m, out Vector3 p, out Quaternion q, out Vector3 s)
	{
		p = new Vector3(m.m03, m.m13, m.m23);
		Vector4 vector = new Vector4(m.m00, m.m10, m.m20, m.m30);
		Vector4 vector2 = new Vector4(m.m01, m.m11, m.m21, m.m31);
		Vector4 vector3 = new Vector4(m.m02, m.m12, m.m22, m.m32);
		q = Quaternion.LookRotation(vector3, vector2);
		s = new Vector3(vector.magnitude, vector2.magnitude, vector3.magnitude);
	}

	public static Vector3 GetPosition(this Matrix4x4 m)
	{
		return new Vector3(m.m03, m.m13, m.m23);
	}

	public static Quaternion GetRotation(this Matrix4x4 m)
	{
		return Quaternion.LookRotation(new Vector4(m.m02, m.m12, m.m22, m.m32), new Vector4(m.m01, m.m11, m.m21, m.m31));
	}

	public static Vector3 GetScale(this Matrix4x4 m)
	{
		return new Vector3(new Vector4(m.m00, m.m10, m.m20, m.m30).magnitude, new Vector4(m.m01, m.m11, m.m21, m.m31).magnitude, new Vector4(m.m02, m.m12, m.m22, m.m32).magnitude);
	}

	public static Vector3 GetSignedEulers(this Vector3 originalEulers)
	{
		if (originalEulers.y > 180f)
		{
			originalEulers.y = 0f - (360f - originalEulers.y);
		}
		if (originalEulers.x > 180f)
		{
			originalEulers.x = 0f - (360f - originalEulers.x);
		}
		if (originalEulers.z > 180f)
		{
			originalEulers.z = 0f - (360f - originalEulers.z);
		}
		return originalEulers;
	}

	public static float Cross(this Vector2 lhs, Vector2 rhs)
	{
		return lhs.x * rhs.y - lhs.y * rhs.x;
	}
}
