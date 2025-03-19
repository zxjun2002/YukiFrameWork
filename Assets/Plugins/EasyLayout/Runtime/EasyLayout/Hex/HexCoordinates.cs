namespace EasyLayoutNS
{
	using UnityEngine;
	using UnityEngine.Events;
	using UnityEngine.UI;

	/// <summary>
	/// Coordinates for the Hex layout.
	/// See more: https://www.redblobgames.com/grids/hexagons/
	/// </summary>
	[HelpURL("https://ilih.name/unity-assets/EasyLayout/docs/hex-coordinates.html")]
	public class HexCoordinates : MonoBehaviour
	{
		EasyLayout layout;

		/// <summary>
		/// Layout.
		/// </summary>
		protected EasyLayout Layout
		{
			get
			{
				if (layout == null)
				{
					transform.parent.TryGetComponent(out layout);
				}

				return layout;
			}
		}

		/// <summary>
		/// Row.
		/// </summary>
		[SerializeField]
		protected int Row;

		/// <summary>
		/// Column.
		/// </summary>
		[SerializeField]
		protected int Column;

		/// <summary>
		/// Offset coordinates.
		/// </summary>
		public OffsetCoordinates Offset => new OffsetCoordinates(Row, Column);

		/// <summary>
		/// Cube coordinates.
		/// </summary>
		public CubeCoordinates Cube => new CubeCoordinates(Row, Column, Layout.HexSettings);

		/// <summary>
		/// Event on coordinates changed.
		/// </summary>
		public UnityEvent OnCoordinatesChanged = new UnityEvent();

		/// <summary>
		/// Set coordinates.
		/// </summary>
		/// <param name="coordinates">Coordinates.</param>
		public void SetCoordinates(OffsetCoordinates coordinates) => SetCoordinates(coordinates.Row, coordinates.Column);

		/// <summary>
		/// Set coordinates.
		/// </summary>
		/// <param name="row">Row.</param>
		/// <param name="column">Column.</param>
		public void SetCoordinates(int row, int column)
		{
			var changed = (Row != row) || (Column != column);

			if (changed)
			{
				Row = row;
				Column = column;

				OnCoordinatesChanged.Invoke();
			}
		}

#if UNITY_EDITOR

		/// <summary>
		/// Process the validate event.
		/// </summary>
		protected virtual void OnValidate()
		{
			LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
		}
#endif
	}
}