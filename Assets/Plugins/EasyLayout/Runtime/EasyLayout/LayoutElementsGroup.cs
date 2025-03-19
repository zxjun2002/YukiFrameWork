namespace EasyLayoutNS
{
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Layout elements group.
	/// </summary>
	public class LayoutElementsGroup
	{
		readonly List<LayoutElementInfo> rowElements = new List<LayoutElementInfo>();

		readonly List<LayoutElementInfo> columnElements = new List<LayoutElementInfo>();

		List<LayoutElementInfo> elements;

		int rows;

		int columns;

		/// <summary>
		/// Elements.
		/// </summary>
		public List<LayoutElementInfo> Elements => elements;

		/// <summary>
		/// Get element by index.
		/// </summary>
		/// <param name="index">Index.</param>
		/// <returns>Element,</returns>
		public LayoutElementInfo this[int index] => elements[index];

		/// <summary>
		/// Rows.
		/// </summary>
		public int Rows => rows + 1;

		/// <summary>
		/// Columns.
		/// </summary>
		public int Columns => columns + 1;

		/// <summary>
		/// Elements count.
		/// </summary>
		public int Count => elements.Count;

		/// <summary>
		/// Set elements.
		/// </summary>
		/// <param name="newElements">Elements.</param>
		public void SetElements(List<LayoutElementInfo> newElements)
		{
			elements = newElements;

			Clear();
		}

		/// <summary>
		/// Clear.
		/// </summary>
		public void Clear()
		{
			rows = -1;
			columns = -1;
			foreach (var element in elements)
			{
				element.Row = -1;
				element.Column = -1;
			}
		}

		/// <summary>
		/// Set position of the element.
		/// </summary>
		/// <param name="index">Index of the element.</param>
		/// <param name="row">Row.</param>
		/// <param name="column">Column.</param>
		public void SetPosition(int index, int row, int column)
		{
			SetPosition(elements[index], row, column);
		}

		/// <summary>
		/// Set position of the element.
		/// </summary>
		/// <param name="element">Element.</param>
		/// <param name="row">Row.</param>
		/// <param name="column">Column.</param>
		public void SetPosition(LayoutElementInfo element, int row, int column)
		{
			element.Row = row;
			element.Column = column;

			rows = Mathf.Max(rows, row);
			columns = Mathf.Max(columns, column);
		}

		/// <summary>
		/// Get elements at row.
		/// </summary>
		/// <param name="row">Row.</param>
		/// <returns>Elements.</returns>
		public List<LayoutElementInfo> GetRow(int row)
		{
			rowElements.Clear();

			foreach (var elem in elements)
			{
				if (elem.Row == row)
				{
					rowElements.Add(elem);
				}
			}

			return rowElements;
		}

		/// <summary>
		/// Get items count in row.
		/// </summary>
		/// <param name="row">Row.</param>
		/// <returns>Count.</returns>
		public int ItemsInRow(int row)
		{
			var result = 0;

			foreach (var elem in elements)
			{
				if (elem.Row == row)
				{
					result++;
				}
			}

			return result;
		}

		/// <summary>
		/// Get items count in row.
		/// </summary>
		/// <param name="row">Row.</param>
		/// <returns>Count.</returns>
		public int MaxColumnInRow(int row)
		{
			var max_column = 0;

			foreach (var elem in elements)
			{
				if (elem.Row == row)
				{
					max_column = Mathf.Max(elem.Column, max_column);
				}
			}

			return max_column;
		}

		/// <summary>
		/// Get elements at column.
		/// </summary>
		/// <param name="column">Column.</param>
		/// <returns>Elements.</returns>
		public List<LayoutElementInfo> GetColumn(int column)
		{
			columnElements.Clear();

			foreach (var elem in elements)
			{
				if (elem.Column == column)
				{
					columnElements.Add(elem);
				}
			}

			return columnElements;
		}

		/// <summary>
		/// Get items count in column.
		/// </summary>
		/// <param name="column">Column.</param>
		/// <returns>Count.</returns>
		public int ItemsInColumn(int column)
		{
			var result = 0;

			foreach (var elem in elements)
			{
				if (elem.Column == column)
				{
					result++;
				}
			}

			return result;
		}

		/// <summary>
		/// Get items count in column.
		/// </summary>
		/// <param name="column">Column.</param>
		/// <returns>Count.</returns>
		public int MaxRowInColumn(int column)
		{
			var max_row = 0;

			foreach (var elem in elements)
			{
				if (elem.Column == column)
				{
					max_row = Mathf.Max(elem.Row, max_row);
				}
			}

			return max_row;
		}

		/// <summary>
		/// Get target position in the group.
		/// </summary>
		/// <param name="target">Target.</param>
		/// <returns>Position.</returns>
		public EasyLayoutPosition GetElementPosition(RectTransform target)
		{
			var target_id = target.GetInstanceID();
			foreach (var element in elements)
			{
				if (element.Rect.GetInstanceID() == target_id)
				{
					return new EasyLayoutPosition(element.Row, element.Column);
				}
			}

			return new EasyLayoutPosition(-1, -1);
		}

		/// <summary>
		/// Change elements order to bottom to top.
		/// </summary>
		public void BottomToTop()
		{
			foreach (var element in elements)
			{
				element.Row = rows - element.Row;
			}
		}

		/// <summary>
		/// Change elements order to right to left.
		/// </summary>
		public void RightToLeft()
		{
			for (int i = 0; i < Rows; i++)
			{
				var row = GetRow(i);
				foreach (var element in row)
				{
					element.Column = row.Count - element.Column - 1;
				}
			}
		}

		/// <summary>
		/// Get size.
		/// </summary>
		/// <param name="spacing">Spacing.</param>
		/// <param name="padding">Padding.</param>
		/// <returns>Size.</returns>
		public GroupSize Size(Vector2 spacing, Vector2 padding)
		{
			var width = HorizontalSize(spacing.x, padding.x);
			var height = VerticalSize(spacing.y, padding.y);

			return new GroupSize(width, height);
		}

		GroupSize HorizontalSize(float spacing, float padding)
		{
			var size = default(GroupSize);

			for (int i = 0; i < Rows; i++)
			{
				var block = GetRow(i);
				var block_size = new GroupSize(((block.Count - 1) * spacing) + padding, 0f);
				foreach (var element in block)
				{
					block_size.Width += element.Width;
					block_size.MinWidth += element.MinWidth;
					block_size.PreferredWidth += element.PreferredWidth;
				}

				size.Max(block_size);
			}

			return size;
		}

		GroupSize VerticalSize(float spacing, float padding)
		{
			var size = default(GroupSize);

			for (int i = 0; i < Columns; i++)
			{
				var block = GetColumn(i);
				var block_size = new GroupSize(0f, ((block.Count - 1) * spacing) + padding);
				foreach (var element in block)
				{
					block_size.Height += element.Height;
					block_size.MinHeight += element.MinHeight;
					block_size.PreferredHeight += element.PreferredHeight;
				}

				size.Max(block_size);
			}

			return size;
		}

		/// <summary>
		/// Validate elements.
		/// </summary>
		/// <returns>true if element states are in supported range; otherwise false.</returns>
		public bool Validate()
		{
			var error = false;
#if UNITY_EDITOR
			for (var i = 0; i < Rows; i++)
			{
				error |= !IsValidRow(GetRow(i));
			}

			for (var i = 0; i < Columns; i++)
			{
				error |= !IsValidColumn(GetColumn(i));
			}
#endif
			return !error;
		}

		bool IsValidRow(List<LayoutElementInfo> row)
		{
			var total = 0f;
			foreach (var e in row)
			{
				total += e.RelativeWidth;
			}

			if (total > 1f)
			{
				Debug.LogError("The following objects are in the same row and have a total relative width of more than 1. It is not supported");

				foreach (var e in row)
				{
					if (e.IsRelativeWidth)
					{
						Debug.LogError(e.Rect, e.Rect);
					}
				}
			}

			return total <= 1f;
		}

		bool IsValidColumn(List<LayoutElementInfo> column)
		{
			var total = 0f;
			foreach (var e in column)
			{
				total += e.RelativeHeight;
			}

			if (total > 1f)
			{
				Debug.LogError("The following objects are in the same column and have a total relative height of more than 1. It is not supported");

				foreach (var e in column)
				{
					if (e.IsRelativeHeight)
					{
						Debug.LogError(e.Rect, e.Rect);
					}
				}
			}

			return total <= 1f;
		}

		/// <summary>
		/// Compare elements by row and column.
		/// </summary>
		protected static readonly System.Comparison<LayoutElementInfo> LayoutElementInfoComparison = (x, y) =>
		{
			var row_comparison = x.Row.CompareTo(y.Row);

			if (row_comparison == 0)
			{
				return x.Column.CompareTo(y.Column);
			}

			return row_comparison;
		};

		/// <summary>
		/// Sort elements.
		/// </summary>
		public void Sort()
		{
			Elements.Sort(LayoutElementInfoComparison);
		}
	}
}