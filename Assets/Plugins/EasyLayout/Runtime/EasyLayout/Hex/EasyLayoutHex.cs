namespace EasyLayoutNS
{
	using System;
	using EasyLayoutNS.Extensions;
	using UnityEngine;

	/// <summary>
	/// Hex layout group.
	/// </summary>
	public class EasyLayoutHex : EasyLayoutBaseType
	{
		/// <summary>
		/// Dimensions info.
		/// </summary>
		protected struct DimensionsInfo
		{
			/// <summary>
			/// Width.
			/// </summary>
			public float Width;

			/// <summary>
			/// Height.
			/// </summary>
			public float Height;

			/// <summary>
			/// Horizontal spacing.
			/// </summary>
			public float SpacingHorizontal;

			/// <summary>
			/// Vertical spacing.
			/// </summary>
			public float SpacingVertical;

			/// <summary>
			/// Items per row.
			/// </summary>
			public float PerRow;

			/// <summary>
			/// Items per column.
			/// </summary>
			public float PerColumn;

			/// <summary>
			/// Update horizontal dimensions.
			/// </summary>
			/// <param name="width">Width.</param>
			/// <param name="spacing">Spacing.</param>
			/// <param name="items">Items.</param>
			public void Horizontal(float width, float spacing, float items)
			{
				Width = Mathf.Max(Width, width);
				SpacingHorizontal = Mathf.Max(SpacingHorizontal, spacing);
				PerRow = Mathf.Max(PerRow, items);
			}

			/// <summary>
			/// Update vertical dimensions.
			/// </summary>
			/// <param name="height">Height.</param>
			/// <param name="spacing">Spacing.</param>
			/// <param name="items">Items.</param>
			public void Vertical(float height, float spacing, float items)
			{
				Height = Mathf.Max(Height, height);
				SpacingVertical = Mathf.Max(SpacingVertical, spacing);
				PerColumn = Mathf.Max(PerColumn, items);
			}
		}

		/// <summary>
		/// Settings.
		/// </summary>
		protected EasyLayoutHexSettings Settings;

		/// <summary>
		/// Group position.
		/// </summary>
		protected Anchors GroupPosition;

		/// <summary>
		/// Base cell width.
		/// </summary>
		protected float BaseCellWidth => (ElementsGroup.Count == 0) ? 0f : ElementsGroup[0].Width;

		/// <summary>
		/// Cell width.
		/// </summary>
		protected float CellWidth => IsFlatTop ? BaseCellWidth * 1.5f : BaseCellWidth;

		/// <summary>
		/// Base cell height.
		/// </summary>
		protected float BaseCellHeight => (ElementsGroup.Count == 0) ? 0f : ElementsGroup[0].Height;

		/// <summary>
		/// Cell height.
		/// </summary>
		protected float CellHeight => IsPointyTop ? BaseCellHeight * 1.5f : BaseCellHeight;

		/// <summary>
		/// Base cell axis size.
		/// </summary>
		protected float BaseAxisSize => IsHorizontal ? BaseCellWidth : BaseCellHeight;

		/// <summary>
		/// Cell axis size.
		/// </summary>
		protected float AxisSize => IsHorizontal ? CellWidth : CellHeight;

		/// <summary>
		/// Cell sub axis size.
		/// </summary>
		protected float SubSize => !IsHorizontal ? CellWidth : CellHeight;

		/// <summary>
		/// Shove.
		/// </summary>
		protected int Shove => Settings.ShovesOdd ? 1 : 0;

		/// <summary>
		/// Is flat top?
		/// </summary>
		protected bool IsFlatTop => Settings.Orientation == EasyLayoutHexSettings.OrientationMode.FlatTop;

		/// <summary>
		/// Is pointy top?
		/// </summary>
		protected bool IsPointyTop => Settings.Orientation == EasyLayoutHexSettings.OrientationMode.PointyTop;

		/// <summary>
		/// Dimensions info.
		/// </summary>
		protected DimensionsInfo Dimensions;

		/// <inheritdoc/>
		public override void LoadSettings(EasyLayout layout)
		{
			base.LoadSettings(layout);

			Settings = layout.HexSettings;

			GroupPosition = layout.GroupPosition;
		}

		bool ShouldShove(int index) => (index % 2) == Shove;

		/// <inheritdoc/>
		protected override bool ShouldValidate() => Settings.Constraint != EasyLayoutHexSettings.HexConstraints.Flexible;

		/// <summary>
		/// Group elements.
		/// </summary>
		protected override void Group()
		{
			// flat top
			// - w size = width / 2
			// - h size = height / sqrt 3
			// - w distance = 3/4 * width
			// - h distance = height

			// pointy top
			// - w size = width / sqrt 3
			// - h size = height / 2
			// - w distance = width
			// - h distance = 3/4 * height
			if (Settings.Coordinates == EasyLayoutHexSettings.CoordinatesMode.Read)
			{
				for (var i = 0; i < ElementsGroup.Count; i++)
				{
					var element = ElementsGroup[i];
					var offset = EasyLayoutUtilities.RequireComponent<HexCoordinates>(element.Rect).Offset;
					ElementsGroup.SetPosition(element, offset.Row, offset.Column);
				}
			}
			else
			{
				switch (Settings.Constraint)
				{
					case EasyLayoutHexSettings.HexConstraints.Flexible:
						GroupFlexible();
						break;
					case EasyLayoutHexSettings.HexConstraints.FixedColumnCount:
						GroupByColumns(ConstraintCount);
						break;
					case EasyLayoutHexSettings.HexConstraints.FixedRowCount:
						GroupByRows(ConstraintCount);
						break;
					case EasyLayoutHexSettings.HexConstraints.CellsPerColumn:
						GroupPerColumn(ConstraintCount, Settings.DecreaseShoved);
						break;
					case EasyLayoutHexSettings.HexConstraints.CellsPerRow:
						GroupPerRow(ConstraintCount, Settings.DecreaseShoved);
						break;
					default:
						throw new NotSupportedException(string.Format("Unknown HexConstraints: {0}", EnumHelper<EasyLayoutHexSettings.HexConstraints>.Instance.ToString(Settings.Constraint)));
				}
			}

			if (!TopToBottom)
			{
				ElementsGroup.BottomToTop();
			}

			if (RightToLeft)
			{
				ElementsGroup.RightToLeft();
			}

			if (Settings.Coordinates == EasyLayoutHexSettings.CoordinatesMode.Write)
			{
				foreach (var element in ElementsGroup.Elements)
				{
					var c = EasyLayoutUtilities.RequireComponent<HexCoordinates>(element.Rect);
					c.SetCoordinates(element.Row, element.Column);
				}
			}
		}

		/// <summary>
		/// Group the specified uiElements.
		/// </summary>
		void GroupFlexible()
		{
			if (ElementsGroup.Count == 0)
			{
				return;
			}

			var base_length = MainAxisSize;
			var flat_top = IsFlatTop;
			var is_horizontal = IsHorizontal;

			var cell_size = is_horizontal ? BaseCellWidth : BaseCellHeight;
			var spacing = is_horizontal ? Spacing.x : Spacing.y;
			var shove = (cell_size / 2) + spacing;

			var main_axis = 0;
			for (var i = 0; i < ElementsGroup.Count;)
			{
				var length = base_length;
				var should_shove = (is_horizontal == !flat_top) && ShouldShove(main_axis);
				if (should_shove)
				{
					length -= shove;
				}

				var per_sub_axis = is_horizontal == flat_top
					? (length + spacing - (cell_size / 4)) / ((cell_size * 0.75f) + spacing)
					: (length + (spacing * 2)) / (cell_size + (spacing * 2));
				per_sub_axis = Mathf.Max(1, Mathf.Floor(per_sub_axis));

				for (var sub_axis = 0; sub_axis < per_sub_axis; sub_axis++)
				{
					if (is_horizontal)
					{
						ElementsGroup.SetPosition(i, main_axis, sub_axis);
					}
					else
					{
						ElementsGroup.SetPosition(i, sub_axis, main_axis);
					}

					i++;

					if (i == ElementsGroup.Count)
					{
						break;
					}
				}

				main_axis++;
			}
		}

		void GroupPerRow(int perRow, bool decreaseShoved)
		{
			if (ElementsGroup.Count == 0)
			{
				return;
			}

			if (decreaseShoved)
			{
				var row = 0;
				var column = 0;
				var count = ShouldShove(row) ? perRow - 1 : perRow;
				for (int i = 0; i < ElementsGroup.Count; i++)
				{
					ElementsGroup.SetPosition(i, row, column);

					column++;
					if (column == count)
					{
						row++;
						column = 0;
						count = ShouldShove(row) ? perRow - 1 : perRow;
					}
				}
			}
			else
			{
				for (int i = 0; i < ElementsGroup.Count; i++)
				{
					ElementsGroup.SetPosition(i, i / perRow, i % perRow);
				}
			}
		}

		void GroupPerColumn(int perColumn, bool decreaseShoved)
		{
			if (ElementsGroup.Count == 0)
			{
				return;
			}

			if (decreaseShoved)
			{
				var row = 0;
				var column = 0;
				var count = ShouldShove(column) ? perColumn - 1 : perColumn;
				for (int i = 0; i < ElementsGroup.Count; i++)
				{
					ElementsGroup.SetPosition(i, row, column);

					row++;
					if (row == count)
					{
						column++;
						row = 0;
						count = ShouldShove(column) ? perColumn - 1 : perColumn;
					}
				}
			}
			else
			{
				for (int i = 0; i < ElementsGroup.Count; i++)
				{
					ElementsGroup.SetPosition(i, i % perColumn, i / perColumn);
				}
			}
		}

		/// <summary>
		/// Calculate dimensions.
		/// </summary>
		protected void CalculateDimensions()
		{
			Dimensions = default;

			if (ElementsGroup.Count == 0)
			{
				return;
			}

			var pointy_top = IsPointyTop;
			var flat_top = IsFlatTop;
			var cell_base_width = BaseCellWidth;
			var cell_base_height = BaseCellHeight;

			var shove = flat_top ? cell_base_height / 2 : cell_base_width / 2;

			var spacing_x = Spacing.x;
			var spacing_y = Spacing.y;
			for (var row = 0; row < ElementsGroup.Rows; row++)
			{
				var cells = (float)ElementsGroup.MaxColumnInRow(row) + 1;
				var per_row = flat_top
					? (0.75f * cells) + 0.25f
					: cells;

				var spacing = flat_top
					? spacing_x * (cells - 1)
					: spacing_x * 2 * (cells - 1);

				var width = cell_base_width * per_row;

				if (!flat_top && ShouldShove(row))
				{
					width += shove;
					spacing += spacing_x;
					per_row += 0.5f;
				}

				Dimensions.Horizontal(width + spacing, spacing, per_row);
			}

			for (var column = 0; column < ElementsGroup.Columns; column++)
			{
				var cells = (float)ElementsGroup.MaxRowInColumn(column) + 1;
				var per_column = flat_top
					? cells
					: (0.75f * cells) + 0.25f;

				var spacing = flat_top
					? spacing_y * 2 * (cells - 1)
					: spacing_y * (cells - 1);

				var height = cell_base_height * per_column;

				if (flat_top && ShouldShove(column))
				{
					height += shove;
					spacing += spacing_y;
					per_column += 0.5f;
				}

				Dimensions.Vertical(height + spacing, spacing, per_column);
			}
		}

		/// <inheritdoc/>
		protected override void CalculateSizes()
		{
			CalculateDimensions();

			if (ElementsGroup.Count == 0)
			{
				return;
			}

			var max_preferred = Vector2.zero;
			if ((ChildrenWidth == ChildrenSize.SetMaxFromPreferred)
				|| (ChildrenHeight == ChildrenSize.SetMaxFromPreferred))
			{
				foreach (var element in ElementsGroup.Elements)
				{
					max_preferred.x = Mathf.Max(max_preferred.x, element.PreferredWidth);
					max_preferred.y = Mathf.Max(max_preferred.y, element.PreferredHeight);
				}
			}

			var width = InternalSize.x - Dimensions.SpacingHorizontal;
			var max_width = width / Dimensions.PerRow;

			var height = InternalSize.y - Dimensions.SpacingVertical;
			var max_height = height / Dimensions.PerColumn;
			var size = ElementsGroup[0].Size;

			switch (ChildrenWidth)
			{
				case ChildrenSize.DoNothing:
					break;
				case ChildrenSize.SetPreferred:
					size.x = Mathf.Max(ElementsGroup[0].MinWidth, Mathf.Min(ElementsGroup[0].PreferredWidth, max_width));
					break;
				case ChildrenSize.SetMaxFromPreferred:
					size.x = max_preferred.x;
					break;
				case ChildrenSize.FitContainer:
				case ChildrenSize.SetPreferredAndFitContainer:
					size.x = max_width;
					break;
				case ChildrenSize.ShrinkOnOverflow:
					if (Dimensions.Width > width)
					{
						size.x = max_width;
					}

					break;
			}

			switch (ChildrenHeight)
			{
				case ChildrenSize.DoNothing:
					break;
				case ChildrenSize.SetPreferred:
					size.y = Mathf.Max(ElementsGroup[0].MinHeight, Mathf.Min(ElementsGroup[0].PreferredHeight, max_height));
					break;
				case ChildrenSize.SetMaxFromPreferred:
					size.y = max_preferred.y;
					break;
				case ChildrenSize.FitContainer:
				case ChildrenSize.SetPreferredAndFitContainer:
					size.y = max_height;
					break;
				case ChildrenSize.ShrinkOnOverflow:
					if (Dimensions.Height > height)
					{
						size.y = max_height;
					}

					break;
			}

			foreach (var element in ElementsGroup.Elements)
			{
				element.NewSize = size;
			}
		}

		/// <inheritdoc/>
		protected override GroupSize CalculateGroupSize() => new GroupSize(Dimensions.Width, Dimensions.Height);

		/// <inheritdoc/>
		protected override void CalculatePositions(Vector2 size)
		{
#if UNITY_EDITOR
			if (size.IsValid())
			{
				throw new ArgumentException("Size value has NaN component: " + size);
			}
#endif

			var offset = CalculateOffset(GroupPosition, size);
#if UNITY_EDITOR
			if (offset.IsValid())
			{
				throw new ArgumentException("Offset value has NaN component: " + offset);
			}
#endif

			var cell_width = CellWidth;
			var cell_height = CellHeight;

			var flat_top = IsFlatTop;

			var step_x = cell_width + (Spacing.x * 2);
			var step_y = cell_height + (Spacing.y * 2);
			if (flat_top)
			{
				step_x /= 2;
			}
			else
			{
				step_y /= 2;
			}

			var step_shove_x = step_x / 2;
			var step_shove_y = -step_y / 2;
			foreach (var element in ElementsGroup.Elements)
			{
				var shove_x = !flat_top && ShouldShove(element.Row) ? step_shove_x : 0;
				var shove_y = flat_top && ShouldShove(element.Column) ? step_shove_y : 0;
				var position = new Vector2(
					(element.Column * step_x) + shove_x,
					(-element.Row * step_y) + shove_y);

				element.PositionTopLeft = offset + position;
			}
		}
	}
}