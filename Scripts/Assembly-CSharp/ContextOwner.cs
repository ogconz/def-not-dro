using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using DI;
using UnityEngine;

public class ContextOwner
{
	private abstract class BaseShownOrderStackItem
	{
	}

	private class VisibilityBreakerStackItem : BaseShownOrderStackItem
	{
	}

	private class ContextStackItem : BaseShownOrderStackItem
	{
		public BaseContext context;

		public override string ToString()
		{
			return $"[ContextStackItem] {context}";
		}
	}

	private readonly Transform m_contextsRoot;

	private readonly Stack<Type> m_creationStack = new Stack<Type>();

	private readonly Dictionary<Type, BaseContext> m_ownedContexts = new Dictionary<Type, BaseContext>();

	private readonly List<BaseShownOrderStackItem> m_shownOrderStack = new List<BaseShownOrderStackItem>();

	private static StringBuilder m_pathBuilder = new StringBuilder(300);

	private List<GameObject> m_loadedContextPrefabs = new List<GameObject>();

	public Action<BaseContext> onContextActiveChanged;

	public Action<BaseContext> onContextUiChanged;

	public Action<BaseContext> onTopContextChanged;

	public BaseContext topContext { get; private set; }

	public bool IsEndContext()
	{
		return m_shownOrderStack.Count < 2;
	}

	public ContextOwner(Transform contextsRoot)
	{
		m_contextsRoot = contextsRoot;
	}

	private int FindLastVisibilityBreakerIndex()
	{
		for (int num = m_shownOrderStack.Count - 1; num >= 0; num--)
		{
			if (m_shownOrderStack[num] is VisibilityBreakerStackItem)
			{
				return num;
			}
		}
		return -1;
	}

	private void SetActiveTopStackGroup(bool isActive)
	{
		for (int i = FindLastVisibilityBreakerIndex() + 1; i < m_shownOrderStack.Count; i++)
		{
			if (!(m_shownOrderStack[i] is ContextStackItem contextStackItem))
			{
				continue;
			}
			if (isActive)
			{
				if (!contextStackItem.context.isActive)
				{
					DoActivateContext(contextStackItem.context);
				}
			}
			else
			{
				DoDeactivateContext(contextStackItem.context);
			}
		}
	}

	private void UpdateTopContext()
	{
		for (int num = m_shownOrderStack.Count - 1; num >= 0; num--)
		{
			if (m_shownOrderStack[num] is ContextStackItem contextStackItem)
			{
				topContext = contextStackItem.context;
				return;
			}
		}
		topContext = null;
	}

	private void RemoveContextFromShownStack(BaseContext context)
	{
		m_shownOrderStack.RemoveAll((BaseShownOrderStackItem item) => item is ContextStackItem && ((ContextStackItem)item).context == context);
	}

	private void TopContextDidChanged()
	{
		BaseContext baseContext = topContext;
		UpdateTopContext();
		if (baseContext != topContext)
		{
			onTopContextChanged.Call(baseContext);
		}
		NavigationInputModule.instance.ForceUpdateMousePosition();
	}

	private GameObject GetResource(string prefabName)
	{
		BaseContext context = ContextPreloader.GetContext(prefabName);
		if (context != null)
		{
			return context.gameObject;
		}
		return LoadResource(prefabName);
	}

	private GameObject FindInCache(string prefabName)
	{
		return m_loadedContextPrefabs.Find((GameObject go) => go.name == prefabName);
	}

	public void ClearFromCache(string prefabName)
	{
		GameObject gameObject = FindInCache(prefabName);
		if (gameObject != null)
		{
			m_loadedContextPrefabs.Remove(gameObject);
			ResourcesWrapper.UnloadAsset(gameObject);
		}
	}

	private GameObject LoadResource(string prefabName)
	{
		GameObject gameObject = FindInCache(prefabName);
		if (gameObject != null)
		{
			return gameObject;
		}
		string[] resourcesPathToGuiPrefabs = UIUtils.ResourcesPathToGuiPrefabs;
		foreach (string value in resourcesPathToGuiPrefabs)
		{
			m_pathBuilder.Clear();
			m_pathBuilder.Append(value);
			m_pathBuilder.Append(prefabName);
			GameObject gameObject2 = ResourcesWrapper.Load<GameObject>(m_pathBuilder.ToString());
			if (gameObject2 != null)
			{
				m_loadedContextPrefabs.Add(gameObject2);
				return gameObject2;
			}
		}
		return null;
	}

	private GameObject FindContextInRootByType(Type contextType)
	{
		string text = contextType.Name;
		if (text.EndsWith("Context"))
		{
			text = text.Substring(0, text.Length - "Context".Length);
		}
		Transform transform = m_contextsRoot.FindChildDeph(text);
		if (transform == null || transform.gameObject == null)
		{
			return null;
		}
		BaseContext component = transform.gameObject.GetComponent<BaseContext>();
		if (component == null || (!component.dontDestroyByStateChange && !component.dontDestroyAlways))
		{
			return null;
		}
		return transform.gameObject;
	}

	private GameObject InstantiateContextByType(Type contextType)
	{
		string text = contextType.Name;
		if (text.EndsWith("Context"))
		{
			text = text.Substring(0, text.Length - "Context".Length);
		}
		GameObject resource = GetResource(text);
		if (resource == null)
		{
			throw new Exception("Could not find UI prefab - " + text);
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(resource, m_contextsRoot);
		if (gameObject == null)
		{
			throw new Exception("Could not instantiate prefab - " + text);
		}
		gameObject.name = text;
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localScale = Vector3.one;
		return gameObject;
	}

	public BaseContext GetContextByType(Type type)
	{
		m_ownedContexts.TryGetValue(type, out var value);
		return value;
	}

	public Type FindTypeByName(string name)
	{
		foreach (Type key in m_ownedContexts.Keys)
		{
			if (string.Equals(key.Name, name))
			{
				return key;
			}
		}
		return null;
	}

	public BaseContext GetContextByTypeName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		Type type = FindTypeByName(name);
		if (type != null)
		{
			return GetContextByType(type);
		}
		type = DynamicTypesManager.GetFirstDerivedType<BaseContext>(name);
		if (type == null)
		{
			return null;
		}
		return InitializeContext(type);
	}

	public T Get<T>() where T : BaseContext
	{
		return (T)GetContextByType(typeof(T));
	}

	private void DoActivateContext(BaseContext context)
	{
		if (context.gameObject.activeSelf)
		{
			throw new InvalidOperationException("Context - " + context.GetType().Name + " already activated");
		}
		UIUtils.SetActive(context.gameObject, state: true);
		context.Activate();
		onContextActiveChanged.SafeCall(context);
		context.HandleAudioFX();
	}

	private void DoDeactivateContext(BaseContext context, bool stateChanged = false)
	{
		if (context.gameObject.activeSelf)
		{
			context.Deactivate();
			if (context.dontDestroyAlways || !context.dontDestroyByStateChange || !stateChanged)
			{
				UIUtils.SetActive(context.gameObject, state: false);
			}
			onContextActiveChanged.SafeCall(context);
		}
	}

	private BaseContext InitializeContext(Type type)
	{
		if (m_creationStack.Contains(type))
		{
			string text = "Circular dependency in the UI contexts! Creation stack:\n";
			foreach (Type item in m_creationStack)
			{
				text = text + item.Name + "\n";
			}
			throw new Exception(text);
		}
		m_creationStack.Push(type);
		GameObject gameObject = FindContextInRootByType(type);
		if (gameObject == null)
		{
			gameObject = InstantiateContextByType(type);
		}
		foreach (Type dependencyContextType in GetDependencyContextTypes(type))
		{
			if (!(dependencyContextType == type) && GetContextByType(dependencyContextType) == null)
			{
				GameLog.Print(AlertLevel.Notify, "Debug", "DC spawn " + type.Name + " -> " + dependencyContextType.Name);
				InitializeContext(dependencyContextType);
			}
		}
		BaseContext baseContext = gameObject.GetComponent<BaseContext>();
		if (baseContext == null)
		{
			baseContext = (BaseContext)gameObject.AddComponent(type);
		}
		if (baseContext.GetType() != type)
		{
			throw new Exception("Prefab for " + type.Name + " contains wrong context of type " + baseContext.GetType().Name);
		}
		SetDependencyContextsTo(baseContext);
		baseContext.MakeDependenciesInjected();
		baseContext.Initialize(this);
		m_ownedContexts.Add(type, baseContext);
		baseContext.gameObject.SetActive(value: false);
		m_creationStack.Pop();
		return baseContext;
	}

	private BaseContext GetOrInitContext(Type type)
	{
		BaseContext baseContext = GetContextByType(type);
		if (baseContext == null)
		{
			baseContext = InitializeContext(type);
		}
		return baseContext;
	}

	public T Preload<T>() where T : BaseContext
	{
		Type typeFromHandle = typeof(T);
		return Preload(typeFromHandle) as T;
	}

	public BaseContext Preload(Type type)
	{
		if (!m_ownedContexts.ContainsKey(type))
		{
			BaseContext baseContext = InitializeContext(type);
			UIUtils.SetActive(baseContext.gameObject, state: true);
			UIUtils.SetActive(baseContext.gameObject, state: false);
			return baseContext;
		}
		return null;
	}

	public BaseContext Add(Type type)
	{
		CheckContextType(type);
		BaseContext orInitContext = GetOrInitContext(type);
		DoActivateContext(orInitContext);
		return orInitContext;
	}

	public T Add<T>() where T : BaseContext
	{
		Type typeFromHandle = typeof(T);
		return (T)Add(typeFromHandle);
	}

	public bool IsActive(Type type)
	{
		BaseContext contextByType = GetContextByType(type);
		if (contextByType != null)
		{
			return contextByType.gameObject.activeSelf;
		}
		return false;
	}

	public bool IsActive<T>() where T : BaseContext
	{
		return IsActive(typeof(T));
	}

	private static void CheckContextType(Type type)
	{
		if (!(type != null) || !typeof(BaseContext).IsAssignableFrom(type))
		{
			throw new InvalidOperationException("Wrong context type: it is not BaseContext's successor: " + ((type == null) ? "null" : type.Name));
		}
	}

	private void CheckStackContext(Type type)
	{
		CheckContextType(type);
		if (m_creationStack.Contains(type))
		{
			throw new InvalidOperationException("Context already added");
		}
	}

	public BaseContext Push(Type type)
	{
		CheckStackContext(type);
		BaseContext orInitContext = GetOrInitContext(type);
		if (orInitContext != null)
		{
			RemoveContextFromShownStack(orInitContext);
			m_shownOrderStack.Add(new ContextStackItem
			{
				context = orInitContext
			});
			DoActivateContext(orInitContext);
		}
		TopContextDidChanged();
		return orInitContext;
	}

	public T Push<T>() where T : BaseContext
	{
		return (T)Push(typeof(T));
	}

	public BaseContext HideStackAndPush(Type contextType)
	{
		SetActiveTopStackGroup(isActive: false);
		m_shownOrderStack.Add(new VisibilityBreakerStackItem());
		return Push(contextType);
	}

	public T HideStackAndPush<T>() where T : BaseContext
	{
		return (T)HideStackAndPush(typeof(T));
	}

	public BaseContext HideAllAndPush(Type type)
	{
		CheckContextType(type);
		if (m_shownOrderStack.Count > 0)
		{
			PopContext();
		}
		return HideStackAndPush(type);
	}

	public T HideAllAndPush<T>() where T : BaseContext
	{
		return (T)HideAllAndPush(typeof(T));
	}

	private void RemoveContext(BaseContext context)
	{
		if (context != null)
		{
			DoDeactivateContext(context);
			RemoveContextFromShownStack(context);
		}
	}

	private void PopContext()
	{
		if (m_shownOrderStack.Count == 0)
		{
			GameLog.Print(AlertLevel.Warning, "Debug", "Popping context from the empty stack");
			return;
		}
		BaseContext context = (m_shownOrderStack.PopLast() as ContextStackItem)?.context;
		RemoveContext(context);
		while (m_shownOrderStack.Last() is VisibilityBreakerStackItem)
		{
			m_shownOrderStack.PopLast();
			SetActiveTopStackGroup(isActive: true);
		}
	}

	public void Pop()
	{
		PopContext();
		TopContextDidChanged();
	}

	public void Hide(Type type)
	{
		BaseContext contextByType = GetContextByType(type);
		if (contextByType != null)
		{
			if (contextByType == topContext)
			{
				Pop();
			}
			else
			{
				RemoveContext(contextByType);
			}
		}
	}

	public void Hide<T>() where T : BaseContext
	{
		Hide(typeof(T));
	}

	public T HideAllExcept<T>() where T : BaseContext
	{
		return (T)HideAllExcept(typeof(T));
	}

	public BaseContext HideAllExcept(Type type)
	{
		BaseContext contextByType = GetContextByType(type);
		for (int num = m_shownOrderStack.Count - 1; num >= 0; num--)
		{
			if (m_shownOrderStack[num] is ContextStackItem contextStackItem)
			{
				if (contextStackItem.context == contextByType)
				{
					break;
				}
				Pop();
			}
		}
		return Push(type);
	}

	public BaseContext ReplaceTop<T>() where T : BaseContext
	{
		return ReplaceTop(typeof(T));
	}

	public BaseContext ReplaceTop(Type type)
	{
		CheckStackContext(type);
		PopContext();
		return HideStackAndPush(type);
	}

	public void ClearAll(bool stateChanged = false)
	{
		foreach (BaseContext value in m_ownedContexts.Values)
		{
			DoDeactivateContext(value, stateChanged);
			RemoveContextFromShownStack(value);
		}
		foreach (BaseContext value2 in m_ownedContexts.Values)
		{
			if (!value2.dontDestroyAlways && (!value2.dontDestroyByStateChange || !stateChanged))
			{
				ClearFromCache(value2.gameObject.name);
				UnityEngine.Object.Destroy(value2.gameObject);
			}
		}
		m_ownedContexts.Clear();
		TopContextDidChanged();
	}

	public bool HandleEscape()
	{
		BaseContext baseContext = topContext;
		if (baseContext != null)
		{
			return baseContext.HandleEscape();
		}
		return false;
	}

	public void HandlePause()
	{
		if (topContext != null)
		{
			topContext.OnPause();
		}
	}

	public void HandleResume()
	{
		if (topContext != null)
		{
			topContext.OnResume();
		}
	}

	public List<BaseContext> GetInternalOwnedList()
	{
		return new List<BaseContext>(m_ownedContexts.Values);
	}

	internal void ContextUiChanged(BaseContext context)
	{
		onContextUiChanged.SafeCall(context);
	}

	private void ForEachDependencyContextFieldDo(Action<FieldInfo> actionWithField, Type fromContextType)
	{
		Type typeFromHandle = typeof(ContextDependencyAttribute);
		FieldInfo[] cachedFields = fromContextType.GetCachedFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in cachedFields)
		{
			if (fieldInfo.IsDefined(typeFromHandle, inherit: true))
			{
				actionWithField.Call(fieldInfo);
			}
		}
	}

	private List<Type> GetDependencyContextTypes(Type fromContextType)
	{
		List<Type> result = new List<Type>();
		ForEachDependencyContextFieldDo(delegate(FieldInfo f)
		{
			result.Add(f.FieldType);
		}, fromContextType);
		return result;
	}

	private void SetDependencyContextsTo(BaseContext context)
	{
		ForEachDependencyContextFieldDo(delegate(FieldInfo f)
		{
			f.SetValue(context, GetContextByType(f.FieldType));
		}, context.GetType());
	}
}
