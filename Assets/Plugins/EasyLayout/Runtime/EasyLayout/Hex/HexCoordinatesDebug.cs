namespace EasyLayoutNS
{
	using UnityEngine;
	using UnityEngine.UI;

	/// <summary>
	/// Show coordinates of the cell in the hexagonal grid.
	/// </summary>
	[RequireComponent(typeof(HexCoordinates))]
	[HelpURL("https://ilih.name/unity-assets/EasyLayout/docs/hex-coordinates.html")]
	public class HexCoordinatesDebug : MonoBehaviour
	{
		/// <summary>
		/// Label.
		/// </summary>
		[SerializeField]
		protected Text Label;

		/// <summary>
		/// Show offset or cube coordinates.
		/// </summary>
		[SerializeField]
		[Tooltip("Show offset or cube coordinates")]
		protected bool OffsetCoordinates = true;

		HexCoordinates coordinates;

		/// <summary>
		/// Process the start event.
		/// </summary>
		protected void Start()
		{
			TryGetComponent(out coordinates);

			if (coordinates != null)
			{
				coordinates.OnCoordinatesChanged.AddListener(UpdateLabel);
				UpdateLabel();
			}
		}

		/// <summary>
		/// Process the destroy event.
		/// </summary>
		protected void OnDestroy()
		{
			if (coordinates != null)
			{
				coordinates.OnCoordinatesChanged.RemoveListener(UpdateLabel);
			}
		}

		/// <summary>
		/// Update text.
		/// </summary>
		protected virtual void UpdateLabel()
		{
			if (Label != null)
			{
				Label.text = OffsetCoordinates ? coordinates.Offset.ToString() : coordinates.Cube.ToString();
			}
		}

#if UNITY_EDITOR
		/// <summary>
		/// Process the validate event.
		/// </summary>
		protected virtual void OnValidate()
		{
			TryGetComponent(out coordinates);
			UpdateLabel();
		}
#endif
	}
}