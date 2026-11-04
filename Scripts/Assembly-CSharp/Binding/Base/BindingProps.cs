using System;
using System.Collections.Generic;
using System.Reflection;

namespace Binding.Base
{
	public class BindingProps<T> where T : IBindingTarget
	{
		private const string INSTANCE_FIELD_NAME = "m_instanceName";

		private const int DEEP_FIND_PROPERTY = 50;

		private static Type m_basePropertyType = typeof(BaseProperty);

		private List<FieldInfo> m_bindingFieldInfos = new List<FieldInfo>(8);

		private List<BaseProperty> m_properties = new List<BaseProperty>(32);

		private T m_target;

		public BindingProps(T targetObject)
		{
			m_target = targetObject;
			CollectProperties();
		}

		public BaseProperty FindProperty(string propertyName, int deep)
		{
			if (deep > 50)
			{
				GameLog.Print(AlertLevel.Error, "UI", "Search too deep: " + propertyName + " in " + m_target.GetType().Name);
				return null;
			}
			BaseProperty propertyByName = GetPropertyByName(propertyName);
			if (propertyByName != null)
			{
				return propertyByName;
			}
			deep++;
			return FindPropertyFromBindingFields(propertyName, deep--);
		}

		private BaseProperty GetPropertyByName(string propertyName)
		{
			for (int i = 0; i < m_properties.Count; i++)
			{
				BaseProperty baseProperty = m_properties[i];
				if (baseProperty.instanceName == propertyName)
				{
					return baseProperty;
				}
			}
			return null;
		}

		private BaseProperty FindPropertyFromBindingFields(string propertyName, int deep)
		{
			for (int i = 0; i < m_bindingFieldInfos.Count; i++)
			{
				object value = m_bindingFieldInfos[i].GetValue(m_target);
				if ((!(value is BaseBindingBehaviourTarget comp) || comp.IsSceneObject()) && value is IBindingTarget bindingTarget)
				{
					BaseProperty baseProperty = bindingTarget.FindProperty(propertyName, deep);
					if (baseProperty != null)
					{
						return baseProperty;
					}
				}
			}
			return null;
		}

		private void CollectProperties()
		{
			Type type = m_target.GetType();
			Type typeFromHandle = typeof(T);
			Type typeFromHandle2 = typeof(object);
			if (type.BaseType != null)
			{
				while (type != typeFromHandle && type != typeFromHandle2)
				{
					CollectProperties(type);
					type = type.BaseType;
				}
			}
		}

		private void CollectProperties(Type type)
		{
			FieldInfo[] cachedFields = type.GetCachedFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in cachedFields)
			{
				if (fieldInfo.IsDefined(typeof(NonSerializedAttribute), inherit: false))
				{
					continue;
				}
				Type fieldType = fieldInfo.FieldType;
				if (BaseProperty.IsHaveIBindingTargetInterface(fieldType))
				{
					string fieldName = fieldInfo.Name;
					if (m_bindingFieldInfos.FindIndex((FieldInfo fi) => fi.Name == fieldName && fi.FieldType == fieldType) == -1)
					{
						m_bindingFieldInfos.Add(fieldInfo);
					}
					continue;
				}
				fieldType = fieldType.GetInitialType();
				if (fieldType == m_basePropertyType && fieldInfo.GetValue(m_target) is BaseProperty baseProperty && GetPropertyByName(baseProperty.instanceName) == null)
				{
					SetPropertyInstanceName(fieldType, fieldInfo, baseProperty);
					m_properties.Add(baseProperty);
				}
			}
		}

		private static void SetPropertyInstanceName(Type propertyType, FieldInfo propertyField, BaseProperty property)
		{
			FieldInfo[] cachedFields = propertyType.GetCachedFields(BindingFlags.Instance | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in cachedFields)
			{
				if (fieldInfo.Name == "m_instanceName")
				{
					fieldInfo.SetValue(property, propertyField.Name);
				}
			}
		}
	}
}
