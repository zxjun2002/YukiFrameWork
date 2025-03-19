#if UNITY_EDITOR
namespace EasyLayoutNS
{
	using UnityEditor;
	using UnityEngine;

	/// <summary>
	/// HexLayoutBuilder editor.
	/// </summary>
	[CustomEditor(typeof(HexLayoutBuilder), true)]
	public class HexLayoutBuilderEditor : Editor
	{
		/// <summary>
		/// Create inspector GUI.
		/// </summary>
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (Application.isPlaying && (targets.Length == 1))
			{
				var script = (HexLayoutBuilder)target;

				if (GUILayout.Button("Update Grid"))
				{
					script.CreateGrid();
				}
			}
		}
	}
}
#endif