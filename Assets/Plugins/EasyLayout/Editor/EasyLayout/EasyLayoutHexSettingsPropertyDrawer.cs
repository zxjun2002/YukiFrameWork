#if UNITY_EDITOR
namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;
	using UnityEditor;

	/// <summary>
	/// Property drawer for the EasyLayoutHexSettings.
	/// </summary>
	[CustomPropertyDrawer(typeof(EasyLayoutHexSettings))]
	public class EasyLayoutHexSettingsPropertyDrawer : ConditionalPropertyDrawer
	{
		static readonly Func<SerializedProperty, bool> IsWriteCoordinate = x => (EasyLayoutHexSettings.CoordinatesMode)x.intValue == EasyLayoutHexSettings.CoordinatesMode.Write;

		static readonly Dictionary<string, Func<SerializedProperty, bool>> IsWriteCoordinates = new Dictionary<string, Func<SerializedProperty, bool>>()
		{
			{ "coordinates", IsWriteCoordinate },
		};

		static readonly Dictionary<string, Func<SerializedProperty, bool>> IsNotFlexible = new Dictionary<string, Func<SerializedProperty, bool>>()
		{
			{ "coordinates", IsWriteCoordinate },
			{ "constraint", x => (EasyLayoutHexSettings.HexConstraints)x.intValue != EasyLayoutHexSettings.HexConstraints.Flexible },
		};

		static readonly Dictionary<string, Func<SerializedProperty, bool>> IsItemsPerN = new Dictionary<string, Func<SerializedProperty, bool>>()
		{
			{ "coordinates", IsWriteCoordinate },
			{
				"constraint",
				x =>
				{
					var v = (EasyLayoutHexSettings.HexConstraints)x.intValue;
					return (v == EasyLayoutHexSettings.HexConstraints.CellsPerRow) || (v == EasyLayoutHexSettings.HexConstraints.CellsPerColumn);
				}
			},
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
				new ConditionalFieldInfo("orientation"),
				new ConditionalFieldInfo("shovesOdd"),
				new ConditionalFieldInfo("coordinates"),
				new ConditionalFieldInfo("constraint", 1, IsWriteCoordinates),
				new ConditionalFieldInfo("constraintCount", 1, IsNotFlexible),
				new ConditionalFieldInfo("decreaseShoved", 1, IsItemsPerN),
			};
		}
	}
}
#endif