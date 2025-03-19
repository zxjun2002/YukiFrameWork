#if UNITY_EDITOR
namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;
	using UnityEditor;

	/// <summary>
	/// Property drawer for the EasyLayoutEllipseSettings.
	/// </summary>
	[CustomPropertyDrawer(typeof(EasyLayoutEllipseSettings))]
	public class EasyLayoutEllipseSettingsPropertyDrawer : ConditionalPropertyDrawer
	{
		static readonly Dictionary<string, Func<SerializedProperty, bool>> IsNotWidthAuto = new Dictionary<string, Func<SerializedProperty, bool>>()
		{
			{ "widthAuto", x => !x.boolValue },
		};

		static readonly Dictionary<string, Func<SerializedProperty, bool>> IsNotHeightAuto = new Dictionary<string, Func<SerializedProperty, bool>>()
		{
			{ "heightAuto", x => !x.boolValue },
		};

		static readonly Dictionary<string, Func<SerializedProperty, bool>> IsAngleAuto = new Dictionary<string, Func<SerializedProperty, bool>>()
		{
			{ "angleStepAuto", x => x.boolValue },
		};

		static readonly Dictionary<string, Func<SerializedProperty, bool>> IsNotAngleAuto = new Dictionary<string, Func<SerializedProperty, bool>>()
		{
			{ "angleStepAuto", x => !x.boolValue },
		};

		static readonly Dictionary<string, Func<SerializedProperty, bool>> IsRotate = new Dictionary<string, Func<SerializedProperty, bool>>()
		{
			{ "elementsRotate", x => x.boolValue },
		};

		/// <summary>
		/// Init this instance.
		/// </summary>
		protected override void Init()
		{
			if (Fields != null)
			{
				return;
			}

			Fields = new List<ConditionalFieldInfo>()
			{
				new ConditionalFieldInfo("widthAuto"),
				new ConditionalFieldInfo("width", 1, IsNotWidthAuto),
				new ConditionalFieldInfo("heightAuto"),
				new ConditionalFieldInfo("height", 1, IsNotHeightAuto),
				new ConditionalFieldInfo("angleStart"),
				new ConditionalFieldInfo("angleStepAuto"),
				new ConditionalFieldInfo("angleStep", 1, IsNotAngleAuto),
				new ConditionalFieldInfo("fill", 1, IsAngleAuto),
				new ConditionalFieldInfo("arcLength", 1),
				new ConditionalFieldInfo("align"),
				new ConditionalFieldInfo("elementsRotate"),
				new ConditionalFieldInfo("elementsRotationStart", 1, IsRotate),
			};
		}
	}
}
#endif