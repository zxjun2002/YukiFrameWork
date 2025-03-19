#if UNITY_EDITOR
namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using UnityEditor;
	using UnityEngine;
	using UnityEngine.Events;

	/// <summary>
	/// Conditional editor.
	/// </summary>
	public abstract class ConditionalEditor : Editor
	{
		/// <summary>
		/// Field data.
		/// </summary>
		protected readonly struct FieldData
		{
			/// <summary>
			/// Field.
			/// </summary>
			public readonly SerializedProperty Field;

			/// <summary>
			/// Notification.
			/// </summary>
			public readonly string Notification;

			/// <summary>
			/// Notification type.
			/// </summary>
			public readonly MessageType NotificationType;

			/// <summary>
			/// Show notification.
			/// </summary>
			public readonly bool ShowNotification => !string.IsNullOrEmpty(Notification);

			/// <summary>
			/// Initializes a new instance of the <see cref="FieldData"/> struct.
			/// </summary>
			/// <param name="field">Field.</param>
			/// <param name="notificationAttribute">Notification attribute.</param>
			public FieldData(SerializedProperty field, EditorNotificationAttribute notificationAttribute)
			{
				Field = field;

				if (notificationAttribute != null)
				{
					Notification = notificationAttribute.Notification;
					NotificationType = (MessageType)notificationAttribute.NotificationType;
				}
				else
				{
					Notification = string.Empty;
					NotificationType = MessageType.None;
				}
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="FieldData"/> struct.
			/// </summary>
			/// <param name="field">Field.</param>
			/// <param name="notification">Notification.</param>
			/// <param name="notificationType">Notification type.</param>
			public FieldData(SerializedProperty field, string notification, MessageType notificationType)
			{
				Field = field;

				Notification = notification;
				NotificationType = notificationType;
			}
		}

		/// <summary>
		/// Target type.
		/// </summary>
		protected Type TargetType;

		/// <summary>
		/// Not displayable fields.
		/// </summary>
		protected List<string> IgnoreFields;

		/// <summary>
		/// Fields to display.
		/// </summary>
		protected List<ConditionalFieldInfo> Fields;

		/// <summary>
		/// Serialized properties.
		/// </summary>
		protected Dictionary<string, FieldData> SerializedProperties = new Dictionary<string, FieldData>();

		/// <summary>
		/// Serialized events.
		/// </summary>
		protected Dictionary<string, SerializedProperty> SerializedEvents = new Dictionary<string, SerializedProperty>();

		/// <summary>
		/// Init.
		/// </summary>
		protected virtual void OnEnable()
		{
			Init();

			TargetType = target != null ? target.GetType() : null;

			SerializedProperties.Clear();
			foreach (var field in Fields)
			{
				SerializedProperties[field.Name] = default;
			}

			GetSerializedProperties();
		}

		/// <summary>
		/// Init this instance.
		/// </summary>
		protected abstract void Init();

		/// <summary>
		/// Get serialized properties.
		/// </summary>
		protected void GetSerializedProperties()
		{
			var property = serializedObject.GetIterator();
			property.NextVisible(true);
			while (property.NextVisible(false))
			{
				if (IsEvent(property))
				{
					SerializedEvents[property.name] = serializedObject.FindProperty(property.name);
				}
				else
				{
					if (SerializedProperties.ContainsKey(property.name))
					{
						var p = serializedObject.FindProperty(property.name);
						SerializedProperties[property.name] = new FieldData(p, GetNotification(property.name));
					}
					else if (!IgnoreFields.Contains(property.name))
					{
						Debug.LogWarning("Field info not found: " + property.name);
					}
				}
			}
		}

		/// <summary>
		/// Get field info from the specified type by name.
		/// </summary>
		/// <param name="type">Type.</param>
		/// <param name="name">Field name.</param>
		/// <returns>Field info.</returns>
		protected static FieldInfo GetField(Type type, string name)
		{
			FieldInfo field;

			var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			while ((field = type.GetField(name, flags)) == null && (type = type.BaseType) != null)
			{
				// do nothing
			}

			return field;
		}

		/// <summary>
		/// Get conditions for the specified field.
		/// </summary>
		/// <param name="name">Field name.</param>
		/// <returns>Conditions.</returns>
		protected EditorNotificationAttribute GetNotification(string name)
		{
			var field = GetField(TargetType, name);
			if (field == null)
			{
				return null;
			}

			return field.GetCustomAttribute<EditorNotificationAttribute>();
		}

		/// <summary>
		/// Is property event?
		/// </summary>
		/// <param name="property">Property</param>
		/// <returns>true if property is event; otherwise false.</returns>
		protected virtual bool IsEvent(SerializedProperty property)
		{
			var object_type = property.serializedObject.targetObject.GetType();
			var property_type = object_type.GetField(property.propertyPath, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			if (property_type == null)
			{
				return false;
			}

			return typeof(UnityEventBase).IsAssignableFrom(property_type.FieldType);
		}

		/// <summary>
		/// Check is all displayable fields exists.
		/// </summary>
		/// <returns>true if all displayable fields exists; otherwise false.</returns>
		protected bool AllFieldsExists()
		{
			var result = true;
			foreach (var kv in SerializedProperties)
			{
				if (kv.Value.Field == null)
				{
					Debug.LogWarning("Field with name '" + kv.Key + "' not found");
					result = false;
				}
			}

			return result;
		}

		/// <summary>
		/// Check is field can be displayed.
		/// </summary>
		/// <param name="info">Field info.</param>
		/// <returns>true if field can be displayed; otherwise false.</returns>
		protected bool CanShow(ConditionalFieldInfo info)
		{
			foreach (var condition in info.Conditions)
			{
				var field = SerializedProperties[condition.Key];
				if (!condition.Value(field.Field))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Draw inspector GUI.
		/// </summary>
		public override void OnInspectorGUI()
		{
			if (!AllFieldsExists())
			{
				return;
			}

			serializedObject.Update();

			foreach (var field in Fields)
			{
				if (!CanShow(field))
				{
					continue;
				}

				EditorGUI.indentLevel += field.Indent;
				
				var fd = SerializedProperties[field.Name];
				EditorGUILayout.PropertyField(fd.Field, true);
				if (fd.ShowNotification)
				{
					EditorGUILayout.HelpBox(fd.Notification, fd.NotificationType);
				}

				EditorGUI.indentLevel -= field.Indent;
			}

			foreach (var ev in SerializedEvents)
			{
				EditorGUILayout.PropertyField(ev.Value, true);
			}

			serializedObject.ApplyModifiedProperties();

			AdditionalGUI();
		}

		/// <summary>
		/// Display additional GUI.
		/// </summary>
		protected virtual void AdditionalGUI()
		{
		}
	}
}
#endif