namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;
	using EasyLayoutNS.Extensions;
	using UnityEngine;
	using UnityEngine.UI;

	/// <summary>
	/// LayoutElementInfo.
	/// Correctly works with multiple resizes during one frame.
	/// </summary>
	public class LayoutElementInfo
	{
		/// <summary>
		/// GameObject active state.
		/// </summary>
		public bool Active;

		/// <summary>
		/// RectTransform.
		/// </summary>
		public RectTransform Rect
		{
			get;
			protected set;
		}

		/// <summary>
		/// RectTransform has relative width.
		/// </summary>
		public bool IsRelativeWidth => Rect.anchorMin.x != Rect.anchorMax.x;

		/// <summary>
		/// Anchors width.
		/// </summary>
		public float RelativeWidth => Mathf.Max(0f, Rect.anchorMax.x - Rect.anchorMin.x);

		/// <summary>
		/// RectTransform has relative height.
		/// </summary>
		public bool IsRelativeHeight => Rect.anchorMin.y != Rect.anchorMax.y;

		/// <summary>
		/// Anchors height.
		/// </summary>
		public float RelativeHeight => Mathf.Max(0f, Rect.anchorMax.y - Rect.anchorMin.y);

		/// <summary>
		/// Width.
		/// </summary>
		public float Width
		{
			get;
			protected set;
		}

		/// <summary>
		/// Height.
		/// </summary>
		public float Height
		{
			get;
			protected set;
		}

		/// <summary>
		/// Size on main axis.
		/// </summary>
		public float AxisSize => Layout.IsHorizontal ? Width : Height;

		/// <summary>
		/// Size on sub axis.
		/// </summary>
		public float SubAxisSize => Layout.IsHorizontal ? Height : Width;

		/// <summary>
		/// Minimal width.
		/// </summary>
		public float MinWidth
		{
			get;
			protected set;
		}

		/// <summary>
		/// Minimal height.
		/// </summary>
		public float MinHeight
		{
			get;
			protected set;
		}

		/// <summary>
		/// Preferred width.
		/// </summary>
		public float PreferredWidth
		{
			get;
			protected set;
		}

		/// <summary>
		/// Preferred height.
		/// </summary>
		public float PreferredHeight
		{
			get;
			protected set;
		}

		/// <summary>
		/// Flexible width.
		/// </summary>
		public float FlexibleWidth
		{
			get;
			protected set;
		}

		/// <summary>
		/// Flexible height.
		/// </summary>
		public float FlexibleHeight
		{
			get;
			protected set;
		}

		/// <summary>
		/// Current layout.
		/// </summary>
		protected EasyLayout Layout;

		/// <summary>
		/// Scale.
		/// </summary>
		protected Vector3 Scale;

		/// <summary>
		/// Is width changed?
		/// </summary>
		public bool ChangedWidth
		{
			get;
			protected set;
		}

		/// <summary>
		/// New width.
		/// </summary>
		protected float newWidth;

		/// <summary>
		/// New width.
		/// </summary>
		public float NewWidth
		{
			get => newWidth;

			set
			{
#if UNITY_EDITOR
				if (float.IsNaN(value) || float.IsInfinity(value))
				{
					throw new ArgumentException("NewWidth cannot be NaN or infinity.");
				}
#endif

				if (ceilValues)
				{
					value = Mathf.Ceil(value);
				}

				if (newWidth != value)
				{
					newWidth = value;
					Width = value * Scale.x;
					ChangedWidth = true;
				}
			}
		}

		/// <summary>
		/// Is height changed?
		/// </summary>
		public bool ChangedHeight
		{
			get;
			protected set;
		}

		/// <summary>
		/// New height.
		/// </summary>
		protected float newHeight;

		/// <summary>
		/// New height.
		/// </summary>
		public float NewHeight
		{
			get => newHeight;

			set
			{
#if UNITY_EDITOR
				if (float.IsNaN(value) || float.IsInfinity(value))
				{
					throw new ArgumentException("NewHeight cannot be NaN or infinity.");
				}
#endif

				if (ceilValues)
				{
					value = Mathf.Ceil(value);
				}

				if (newHeight != value)
				{
					newHeight = value;
					Height = value * Scale.y;
					ChangedHeight = true;
				}
			}
		}

		/// <summary>
		/// New size.
		/// </summary>
		public Vector2 NewSize
		{
			get => new Vector2(NewWidth, NewHeight);

			set
			{
				NewWidth = value.x;
				NewHeight = value.y;
			}
		}

		/// <summary>
		/// Is pivot changed?
		/// </summary>
		public bool ChangedPivot
		{
			get;
			protected set;
		}

		/// <summary>
		/// Pivot.
		/// </summary>
		public Vector2 Pivot
		{
			get;
			protected set;
		}

		/// <summary>
		/// New pivot.
		/// </summary>
		protected Vector2 newPivot;

		/// <summary>
		/// New pivot.
		/// </summary>
		public Vector2 NewPivot
		{
			get => newPivot;

			set
			{
#if UNITY_EDITOR
				if (value.IsValid())
				{
					throw new ArgumentException("NewPivot value has NaN component: " + value);
				}
#endif

				if (newPivot != value)
				{
					newPivot = value;
					Pivot = value;
					ChangedPivot = true;
				}
			}
		}

		/// <summary>
		/// Position of the top left corner.
		/// </summary>
		public Vector2 PositionTopLeft
		{
			get
			{
				return new Vector2(
					PositionPivot.x - (Width * NewPivot.x),
					PositionPivot.y + (Height * (1f - NewPivot.y)));
			}

			set
			{
				PositionPivot = new Vector2(
					value.x + (Width * NewPivot.x),
					value.y - (Height * (1f - NewPivot.y)));
			}
		}

		Vector2 positionPivot;

		/// <summary>
		/// Position with pivot.
		/// </summary>
		public Vector2 PositionPivot
		{
			get => positionPivot;

			set
			{
#if UNITY_EDITOR
				if (value.IsValid())
				{
					throw new ArgumentException("PositionPivot value has NaN component: " + value);
				}
#endif

				positionPivot = value;
			}
		}

		/// <summary>
		/// Is position changed?
		/// </summary>
		public bool IsPositionChanged
		{
			get
			{
				var new_pos = PositionPivot;
				return (Rect.localPosition.x != new_pos.x) || (Rect.localPosition.y != new_pos.y);
			}
		}

		/// <summary>
		/// Is rotation changed?
		/// </summary>
		public bool ChangedRotation
		{
			get;
			protected set;
		}

		/// <summary>
		/// New EulerAngles.
		/// </summary>
		protected Vector3 newEulerAngles = Vector3.zero;

		/// <summary>
		/// New EulerAngles.Z.
		/// </summary>
		public float NewEulerAnglesZ
		{
			get => newEulerAngles.z;

			set
			{
				if (newEulerAngles.z != value)
				{
					newEulerAngles.z = value;
					ChangedRotation = true;
				}
			}
		}

		/// <summary>
		/// New EulerAngles.
		/// </summary>
		public Vector3 NewEulerAngles
		{
			get => newEulerAngles;

			set
			{
				if (newEulerAngles != value)
				{
					newEulerAngles = value;
					ChangedRotation = true;
				}
			}
		}

		/// <summary>
		/// Size.
		/// </summary>
		public Vector2 Size => new Vector2(Width, Height);

		/// <summary>
		/// Row.
		/// </summary>
		public int Row;

		/// <summary>
		/// Column.
		/// </summary>
		public int Column;

		bool ceilValues;

		/// <summary>
		/// Set element.
		/// </summary>
		/// <param name="rectTransform">RectTransform.</param>
		/// <param name="active">Is gameobject active?</param>
		/// <param name="layout">Current layout.</param>
		/// <param name="ceilSize">Ceil size.</param>
		/// <param name="ignoreLayoutElementSizes">Ignore LayoutElement sizes.</param>
		public void SetElement(RectTransform rectTransform, bool active, EasyLayout layout, bool ceilSize, bool ignoreLayoutElementSizes)
		{
			ceilValues = ceilSize;
			Rect = rectTransform;
			Active = active;
			Layout = layout;

			Scale = rectTransform.localScale;
			var size = rectTransform.rect.size;
#if UNITY_EDITOR
			if (size.IsValid())
			{
				throw new ArgumentException("Size has NaN components: " + size);
			}

			if (Scale.IsValid())
			{
				throw new ArgumentException("Scale has NaN components: " + Scale);
			}
#endif

			Width = size.x * Scale.x;
			if (ceilSize)
			{
				Width = Mathf.Ceil(Width);
			}

			Height = size.y * Scale.y;
			if (ceilSize)
			{
				Height = Mathf.Ceil(Height);
			}

			Pivot = rectTransform.pivot;
#if UNITY_EDITOR
			if (Pivot.IsValid())
			{
				throw new ArgumentException("Pivot has NaN components: " + Pivot);
			}
#endif

			newWidth = Width;
			newHeight = Height;
			newPivot = Pivot;

			newEulerAngles = rectTransform.localEulerAngles;

			ChangedWidth = false;
			ChangedHeight = false;

			if (Layout.ChildrenWidth != ChildrenSize.DoNothing || Layout.ChildrenHeight != ChildrenSize.DoNothing)
			{
				RefreshLayoutElements();
			}

			var widths = ignoreLayoutElementSizes ? new Size(0, Width) : GetWidthValues();
			MinWidth = widths.Min;
			PreferredWidth = widths.Preferred;
			FlexibleWidth = widths.Flexible;

			var heights = ignoreLayoutElementSizes ? new Size(0, Height) : GetHeightValues();
			MinHeight = heights.Min;
			PreferredHeight = heights.Preferred;
			FlexibleHeight = heights.Flexible;

			Row = -1;
			Column = -1;
		}

		/// <summary>
		/// Get widths values.
		/// </summary>
		/// <returns>Widths values.</returns>
		protected Size GetWidthValues()
		{
			if (Rect == null)
			{
				return default;
			}

			if (Rect.gameObject.activeInHierarchy)
			{
				return new Size()
				{
					Min = Mathf.Max(0f, LayoutUtility.GetMinWidth(Rect)),
					Preferred = Mathf.Max(0f, LayoutUtility.GetPreferredWidth(Rect)),
					Flexible = Mathf.Max(0f, LayoutUtility.GetFlexibleWidth(Rect)),
				};
			}

			return GetLayoutWidths();
		}

		/// <summary>
		/// Get heights values.
		/// </summary>
		/// <returns>Height values.</returns>
		protected Size GetHeightValues()
		{
			if (Rect == null)
			{
				return default;
			}

			if (Rect.gameObject.activeInHierarchy)
			{
				return new Size()
				{
					Min = Mathf.Max(0f, LayoutUtility.GetMinHeight(Rect)),
					Preferred = Mathf.Max(0f, LayoutUtility.GetPreferredHeight(Rect)),
					Flexible = Mathf.Max(0f, LayoutUtility.GetFlexibleHeight(Rect)),
				};
			}

			return GetLayoutHeights();
		}

		/// <summary>
		/// Set size.
		/// </summary>
		/// <param name="axis">Axis.</param>
		/// <param name="size">New size.</param>
		public void SetSize(RectTransform.Axis axis, float size)
		{
			if (axis == RectTransform.Axis.Horizontal)
			{
				NewWidth = size;
			}
			else
			{
				NewHeight = size;
			}
		}

		/// <summary>
		/// All ILayoutElements Of current gameobject.
		/// </summary>
		protected List<ILayoutElement> LayoutElements = new List<ILayoutElement>();

		/// <summary>
		/// Get widths from LayoutElements.
		/// </summary>
		/// <returns>Widths values.</returns>
		protected Size GetLayoutWidths()
		{
			var max_priority = MaxPriority(LayoutElements);

			var result = default(Size);

			foreach (var elem in LayoutElements)
			{
				if (elem.layoutPriority == max_priority)
				{
					result.Min = Mathf.Max(result.Min, elem.minWidth);
					result.Preferred = Mathf.Max(result.Preferred, Mathf.Max(elem.preferredWidth, elem.minWidth));
					result.Flexible = Mathf.Max(result.Flexible, elem.flexibleWidth);
				}
			}

			return result;
		}

		/// <summary>
		/// Refresh LayoutElements list.
		/// </summary>
		protected void RefreshLayoutElements()
		{
			LayoutElements.Clear();
			Rect.GetComponents(LayoutElements);
		}

		/// <summary>
		/// Get heights from LayoutElements.
		/// </summary>
		/// <returns>Heights values.</returns>
		protected Size GetLayoutHeights()
		{
			var max_priority = MaxPriority(LayoutElements);

			var result = default(Size);

			for (int i = 0; i < LayoutElements.Count; i++)
			{
				var elem = LayoutElements[i];
				if (elem.layoutPriority == max_priority)
				{
					result.Min = Mathf.Max(result.Min, elem.minHeight);
					result.Preferred = Mathf.Max(result.Preferred, Mathf.Max(elem.preferredHeight, elem.minHeight));
					result.Flexible = Mathf.Max(result.Flexible, elem.flexibleHeight);
				}
			}

			return result;
		}

		/// <summary>
		/// Get maximum priority from LayoutElements.
		/// </summary>
		/// <param name="elements">LayoutElements list.</param>
		/// <returns>Maximum priority.</returns>
		protected static int MaxPriority(List<ILayoutElement> elements)
		{
			if (elements.Count == 0)
			{
				return 0;
			}

			int result = elements[0].layoutPriority;
			for (int i = 1; i < elements.Count; i++)
			{
				result = Mathf.Max(result, elements[i].layoutPriority);
			}

			return result;
		}
	}
}