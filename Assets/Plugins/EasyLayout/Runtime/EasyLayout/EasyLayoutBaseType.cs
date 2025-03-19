namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;
	using EasyLayoutNS.Extensions;
	using UnityEngine;

	/// <summary>
	/// Base class for EasyLayout groups.
	/// </summary>
	public abstract class EasyLayoutBaseType
	{
		/// <summary>
		/// Element changed delegate.
		/// </summary>
		/// <param name="element">Element.</param>
		/// <param name="properties">Properties.</param>
		public delegate void ElementChanged(RectTransform element, DrivenTransformProperties properties);

		/// <summary>
		/// Element changed event.
		/// </summary>
		public event ElementChanged OnElementChanged;

		/// <summary>
		/// Target.
		/// </summary>
		protected RectTransform Target;

		/// <summary>
		/// Is horizontal layout?
		/// </summary>
		protected bool IsHorizontal;

		/// <summary>
		/// Available size.
		/// </summary>
		protected Vector2 InternalSize;

		/// <summary>
		/// Spacing.
		/// </summary>
		protected Vector2 Spacing;

		/// <summary>
		/// Constraint count.
		/// </summary>
		protected int ConstraintCount;

		/// <summary>
		/// Main axis size.
		/// </summary>
		protected float MainAxisSize;

		/// <summary>
		/// Sub axis size.
		/// </summary>
		protected float SubAxisSize;

		/// <summary>
		/// Children width resize setting.
		/// </summary>
		protected ChildrenSize ChildrenWidth;

		/// <summary>
		/// Children height resize setting.
		/// </summary>
		protected ChildrenSize ChildrenHeight;

		/// <summary>
		/// Inner padding.
		/// </summary>
		protected Padding PaddingInner;

		/// <summary>
		/// Place element from top to bottom.
		/// </summary>
		protected bool TopToBottom;

		/// <summary>
		/// Place element from right to left.
		/// </summary>
		protected bool RightToLeft;

		/// <summary>
		/// Left margin.
		/// </summary>
		protected float MarginLeft;

		/// <summary>
		/// Top margin.
		/// </summary>
		protected float MarginTop;

		/// <summary>
		/// Change elements rotation.
		/// </summary>
		protected bool ChangeRotation;

		/// <summary>
		/// Change elements pivot.
		/// </summary>
		protected bool ChangePivot;

		/// <summary>
		/// Elements group.
		/// </summary>
		protected LayoutElementsGroup ElementsGroup = new LayoutElementsGroup();

		/// <summary>
		/// Driven properties.
		/// </summary>
		protected DrivenTransformProperties DrivenProperties;

		/// <summary>
		/// Movement animation.
		/// </summary>
		protected bool MovementAnimation;

		/// <summary>
		/// Animate movements for all elements if enabled; otherwise new elements will not be animated.
		/// </summary>
		protected bool MovementAnimateAll = true;

		/// <summary>
		/// Movement animation curve.
		/// </summary>
		protected AnimationCurve MovementCurve;

		/// <summary>
		/// Resize animation.
		/// </summary>
		protected bool ResizeAnimation;

		/// <summary>
		/// Animate resize for all elements if enabled; otherwise new elements will not be animated.
		/// </summary>
		protected bool ResizeAnimateAll = true;

		/// <summary>
		/// Resize animation curve.
		/// </summary>
		protected AnimationCurve ResizeCurve;

		/// <summary>
		/// Unscaled time.
		/// </summary>
		protected bool UnscaledTime;

		/// <summary>
		/// Movement targets.
		/// </summary>
		protected Dictionary<int, MovementTarget> MovementTargets = new Dictionary<int, MovementTarget>();

		/// <summary>
		/// Resize targets.
		/// </summary>
		protected Dictionary<int, ResizeTarget> ResizeTargets = new Dictionary<int, ResizeTarget>();

		/// <summary>
		/// ID of existing elements.
		/// </summary>
		protected HashSet<int> ExistingElementsID;

		/// <summary>
		/// Temporary list for animation.
		/// </summary>
		protected List<int> TempAnimate = new List<int>();

		/// <summary>
		/// Animation target.
		/// </summary>
		protected interface IAnimationTarget
		{
			/// <summary>
			/// Animate.
			/// </summary>
			/// <param name="animation">Animation curve.</param>
			/// <returns>true if animated ended; otherwise false.</returns>
			bool Animate(AnimationCurve animation);
		}

		/// <summary>
		/// Movement target.
		/// </summary>
		protected struct MovementTarget : IAnimationTarget
		{
			/// <summary>
			/// Target.
			/// </summary>
			public readonly RectTransform Target;

			/// <summary>
			/// Start position.
			/// </summary>
			public readonly Vector2 StartPosition;

			/// <summary>
			/// Start pivot.
			/// </summary>
			public readonly Vector2 StartPivot;

			/// <summary>
			/// Start rotation.
			/// </summary>
			public readonly Quaternion StartRotation;

			/// <summary>
			/// End position.
			/// </summary>
			public readonly Vector2 EndPosition;

			/// <summary>
			/// End pivot.
			/// </summary>
			public readonly Vector2 EndPivot;

			/// <summary>
			/// End rotation.
			/// </summary>
			public readonly Quaternion EndRotation;

			/// <summary>
			/// Unscaled time.
			/// </summary>
			public readonly bool UnscaledTime;

			/// <summary>
			/// Time.
			/// </summary>
			float time;

			/// <summary>
			/// Initializes a new instance of the <see cref="MovementTarget"/> struct.
			/// </summary>
			/// <param name="target">Target.</param>
			/// <param name="position">Position.</param>
			/// <param name="pivot">Pivot.</param>
			/// <param name="rotation">Rotation.</param>
			/// <param name="unscaledTime">Unscaled time.</param>
			public MovementTarget(RectTransform target, Vector2 position, Vector2 pivot, Quaternion rotation, bool unscaledTime)
			{
				Target = target;

				StartPosition = Target.localPosition;
				StartPivot = Target.pivot;
				StartRotation = Target.localRotation;

				EndPosition = position;
				EndPivot = pivot;
				EndRotation = rotation;

				UnscaledTime = unscaledTime;

				time = 0f;
			}

			/// <summary>
			/// Animate.
			/// </summary>
			/// <param name="animation">Animation curve.</param>
			/// <returns>true if animated ended; otherwise false.</returns>
			public bool Animate(AnimationCurve animation)
			{
				var duration = animation[animation.length - 1].time;
				time += EasyLayout.GetDeltaTime(UnscaledTime);

				var value = animation.Evaluate(time);

				Target.localPosition = Vector2.Lerp(StartPosition, EndPosition, value);
				Target.pivot = Vector2.Lerp(StartPivot, EndPivot, value);
				Target.localRotation = Quaternion.Lerp(StartRotation, EndRotation, value);

				return time >= duration;
			}
		}

		/// <summary>
		/// Resize target.
		/// </summary>
		protected struct ResizeTarget : IAnimationTarget
		{
			/// <summary>
			/// Target.
			/// </summary>
			public readonly RectTransform Target;

			/// <summary>
			/// Start size.
			/// </summary>
			public readonly Vector2 StartSize;

			/// <summary>
			/// End size.
			/// </summary>
			public readonly Vector2 EndSize;

			/// <summary>
			/// Unscaled time.
			/// </summary>
			public readonly bool UnscaledTime;

			/// <summary>
			/// Start time.
			/// </summary>
			float time;

			/// <summary>
			/// Initializes a new instance of the <see cref="ResizeTarget"/> struct.
			/// </summary>
			/// <param name="target">Target.</param>
			/// <param name="size">Size.</param>
			/// <param name="unscaledTime">Unscaled time.</param>
			public ResizeTarget(RectTransform target, Vector2 size, bool unscaledTime)
			{
				Target = target;
				StartSize = Target.rect.size;
				EndSize = size;
				UnscaledTime = unscaledTime;

				time = 0f;
			}

			/// <summary>
			/// Animate.
			/// </summary>
			/// <param name="animation">Animation curve.</param>
			/// <returns>true if animated ended; otherwise false.</returns>
			public bool Animate(AnimationCurve animation)
			{
				var duration = animation[animation.length - 1].time;
				time += EasyLayout.GetDeltaTime(UnscaledTime);

				var size = Vector2.Lerp(StartSize, EndSize, animation.Evaluate(time));

				Target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
				Target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

				return time >= duration;
			}
		}

		/// <summary>
		/// Group position.
		/// </summary>
		protected static readonly List<Vector2> GroupPositions = new List<Vector2>()
		{
			new Vector2(0.0f, 1.0f), // Anchors.UpperLeft
			new Vector2(0.5f, 1.0f), // Anchors.UpperCenter
			new Vector2(1.0f, 1.0f), // Anchors.UpperRight

			new Vector2(0.0f, 0.5f), // Anchors.MiddleLeft
			new Vector2(0.5f, 0.5f), // Anchors.MiddleCenter
			new Vector2(1.0f, 0.5f), // Anchors.MiddleRight

			new Vector2(0.0f, 0.0f), // Anchors.LowerLeft
			new Vector2(0.5f, 0.0f), // Anchors.LowerCenter
			new Vector2(1.0f, 0.0f), // Anchors.LowerRight
		};

		/// <summary>
		/// Load layout settings.
		/// </summary>
		/// <param name="layout">Layout.</param>
		public virtual void LoadSettings(EasyLayout layout)
		{
			Target = layout.transform as RectTransform;

			IsHorizontal = layout.IsHorizontal;

			Spacing = layout.Spacing;
			InternalSize = layout.InternalSize;
			MainAxisSize = layout.MainAxisSize;
			SubAxisSize = layout.SubAxisSize;

			PaddingInner = layout.PaddingInner;

			ConstraintCount = layout.ConstraintCount;
			ChildrenWidth = layout.ChildrenWidth;
			ChildrenHeight = layout.ChildrenHeight;

			TopToBottom = layout.TopToBottom;
			RightToLeft = layout.RightToLeft;

			MarginLeft = layout.GetMarginLeft();
			MarginTop = layout.GetMarginTop();

			ChangeRotation = layout.ResetRotation;

			MovementAnimation = layout.MovementAnimation;
			MovementAnimateAll = layout.MovementAnimateAll;
			MovementCurve = layout.MovementCurve;

			ResizeAnimation = layout.ResizeAnimation;
			ResizeAnimateAll = layout.ResizeAnimateAll;
			ResizeCurve = layout.ResizeCurve;

			ExistingElementsID = layout.ExistingElementsID;

			UnscaledTime = layout.UnscaledTime;

			DrivenProperties = GetDrivenTransformProperties();
		}

		/// <summary>
		/// Get DrivenProperties.
		/// </summary>
		/// <returns>DrivenProperties.</returns>
		protected virtual DrivenTransformProperties GetDrivenTransformProperties()
		{
			var result = DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.AnchoredPositionZ;

			if (ChildrenWidth != ChildrenSize.DoNothing)
			{
				result |= DrivenTransformProperties.SizeDeltaX;
			}

			if (ChildrenHeight != ChildrenSize.DoNothing)
			{
				result |= DrivenTransformProperties.SizeDeltaY;
			}

			if (ChangeRotation)
			{
				result |= DrivenTransformProperties.Rotation;
			}

			return result;
		}

		/// <summary>
		/// Animate.
		/// </summary>
		public virtual void Animate()
		{
			Animate(ResizeTargets, ResizeCurve);
			Animate(MovementTargets, MovementCurve);
		}

		/// <summary>
		/// Animate.
		/// </summary>
		/// <typeparam name="T">Type of animation target.</typeparam>
		/// <param name="targets">Targets.</param>
		/// <param name="animation">Animation curve.</param>
		protected virtual void Animate<T>(Dictionary<int, T> targets, AnimationCurve animation)
			where T : IAnimationTarget
		{
			TempAnimate.AddRange(targets.Keys);

			foreach (var key in TempAnimate)
			{
				var value = targets[key];
				if (value.Animate(animation))
				{
					targets.Remove(key);
				}
				else
				{
					targets[key] = value;
				}
			}

			TempAnimate.Clear();
		}

		/// <summary>
		/// Should validate elements.
		/// </summary>
		/// <returns>true if should validate elements; otherwise false.</returns>
		protected virtual bool ShouldValidate() => true;

		/// <summary>
		/// Perform layout.
		/// </summary>
		/// <param name="elements">Elements.</param>
		/// <param name="setValues">Set element positions and sizes.</param>
		/// <param name="resizeType">Resize type.</param>
		/// <returns>Size of the group.</returns>
		public GroupSize PerformLayout(List<LayoutElementInfo> elements, bool setValues, ResizeType resizeType)
		{
			var size = new GroupSize(Vector2.zero);
			ElementsGroup.SetElements(elements);

			SetInitialSizes();
			Group();
#if UNITY_EDITOR
			if (ShouldValidate() && !ElementsGroup.Validate())
			{
				return size;
			}
#endif

			ElementsGroup.Sort();

			try
			{
				CalculateSizes();
				size = CalculateGroupSize();
				CalculatePositions(new Vector2(size.Width, size.Height));
			}
			catch (ArgumentException e)
			{
				Debug.LogException(e, Target);
				return size;
			}

			if (setValues)
			{
				SetElementsSize(resizeType);
				SetPositions();
			}

			return size;
		}

		/// <summary>
		/// Get target position in the group.
		/// </summary>
		/// <param name="target">Target.</param>
		/// <returns>Position.</returns>
		public virtual EasyLayoutPosition GetElementPosition(RectTransform target)
		{
			return ElementsGroup.GetElementPosition(target);
		}

		/// <summary>
		/// Group elements.
		/// </summary>
		protected abstract void Group();

		/// <summary>
		/// Calculate sizes of the elements.
		/// </summary>
		protected abstract void CalculateSizes();

		/// <summary>
		/// Calculate positions of the elements.
		/// </summary>
		/// <param name="size">Size.</param>
		protected abstract void CalculatePositions(Vector2 size);

		/// <summary>
		/// Calculate group size.
		/// </summary>
		/// <returns>Size.</returns>
		protected abstract GroupSize CalculateGroupSize();

		/// <summary>
		/// Calculate offset.
		/// </summary>
		/// <param name="groupPosition">Group position.</param>
		/// <param name="size">Size.</param>
		/// <returns>Offset.</returns>
		protected Vector2 CalculateOffset(Anchors groupPosition, Vector2 size)
		{
			var target_size = Target.rect.size;
			var target_pivot = Target.pivot;
			var anchor_position = GroupPositions[(int)groupPosition];

#if UNITY_EDITOR
			if (size.IsValid())
			{
				throw new ArgumentException("size value has NaN component: " + size);
			}

			if (target_size.IsValid())
			{
				throw new ArgumentException("target_size value has NaN component: " + target_size);
			}

			if (target_pivot.IsValid())
			{
				throw new ArgumentException("target_pivot value has NaN component: " + target_pivot);
			}

			if (anchor_position.IsValid())
			{
				throw new ArgumentException("anchor_position value has NaN component: " + anchor_position);
			}

			if (float.IsNaN(MarginLeft))
			{
				throw new ArgumentException("MarginLeft cannot be NaN");
			}

			if (float.IsNaN(MarginTop))
			{
				throw new ArgumentException("MarginTop cannot be NaN");
			}

			if (float.IsNaN(PaddingInner.Left))
			{
				throw new ArgumentException("PaddingInner.Left cannot be NaN");
			}

			if (float.IsNaN(PaddingInner.Top))
			{
				throw new ArgumentException("PaddingInner.Top cannot be NaN");
			}
#endif

			var offset = new Vector2(
				target_size.x * (anchor_position.x - target_pivot.x),
				target_size.y * (anchor_position.y - target_pivot.y));

			offset.x -= anchor_position.x * size.x;
			offset.y += (1 - anchor_position.y) * size.y;

			offset.x += MarginLeft * (1 - (anchor_position.x * 2));
			offset.y += MarginTop * (1 - (anchor_position.y * 2));

			offset.x += PaddingInner.Left;
			offset.y -= PaddingInner.Top;

			return offset;
		}

		/// <summary>
		/// Calculate size of the group.
		/// </summary>
		/// <param name="isHorizontal">ElementsGroup are in horizontal order?</param>
		/// <param name="spacing">Spacing.</param>
		/// <param name="padding">Padding,</param>
		/// <returns>Size.</returns>
		protected virtual GroupSize CalculateGroupSize(bool isHorizontal, Vector2 spacing, Vector2 padding)
		{
			var max_width = new GroupSize(0, 0);
			for (int i = 0; i < ElementsGroup.Rows; i++)
			{
				var row = ElementsGroup.GetRow(i);
				var width = new GroupSize(((row.Count - 1) * spacing.x) + padding.x, 0);
				foreach (var element in row)
				{
					width += element;
				}

				max_width = max_width.Max(width);
			}

			var max_height = new GroupSize(0, 0);
			for (int i = 0; i < ElementsGroup.Columns; i++)
			{
				var column = ElementsGroup.GetColumn(i);
				var height = new GroupSize(0, ((column.Count - 1) * spacing.y) + padding.y);
				foreach (var element in ElementsGroup.GetColumn(i))
				{
					height += element;
				}

				max_height = max_height.Max(height);
			}

			return new GroupSize(max_width, max_height);
		}

		/// <summary>
		/// Set elements size.
		/// </summary>
		/// <param name="resizeType">Resize type.</param>
		protected void SetElementsSize(ResizeType resizeType)
		{
			if (ElementsGroup.Elements.Count == 0)
			{
				return;
			}

			var temp =
				#if UNITY_2021_3_OR_NEWER
				UnityEngine.Pool.ListPool<LayoutElementInfo>.Get();
				#else
				new List<LayoutElementInfo>();
				#endif

			foreach (var element in ElementsGroup.Elements)
			{
				temp.Add(element);
			}

			foreach (var element in temp)
			{
				SetElementSize(element, resizeType, Target.rect.size);
				OnElementChangedInvoke(element.Rect, DrivenProperties);
			}

			temp.Clear();

			#if UNITY_2021_3_OR_NEWER
			UnityEngine.Pool.ListPool<LayoutElementInfo>.Release(temp);
			#endif
		}

		/// <summary>
		/// Should animate resize for the specified instance?
		/// </summary>
		/// <param name="instanceId">Instance ID.</param>
		/// <returns>true if should animate; otherwise false.</returns>
		protected bool ShouldAnimateResize(int instanceId) => ResizeAnimation && Application.isPlaying && (ResizeAnimateAll || ExistingElementsID.Contains(instanceId));

		/// <summary>
		/// Set element size.
		/// </summary>
		/// <param name="element">Element.</param>
		/// <param name="resizeType">Resize type.</param>
		/// <param name="parentSize">Parent size.</param>
		protected virtual void SetElementSize(LayoutElementInfo element, ResizeType resizeType, Vector2 parentSize)
		{
			if (element.Rect.rect.size == element.NewSize)
			{
				return;
			}

			var resize_width = (ChildrenWidth != ChildrenSize.DoNothing) && resizeType.IsSet(ResizeType.Horizontal) && element.ChangedWidth;
			var resize_height = (ChildrenHeight != ChildrenSize.DoNothing) && resizeType.IsSet(ResizeType.Vertical) && element.ChangedHeight;

			var changed = resize_width || resize_height;
			if (!changed)
			{
				return;
			}

			var id = element.Rect.GetInstanceID();
			if (ShouldAnimateResize(id))
			{
				if (!ResizeTargets.TryGetValue(id, out var settings) && (settings.EndSize != element.NewSize))
				{
					ResizeTargets[id] = new ResizeTarget(element.Rect, element.NewSize, UnscaledTime);
				}
			}
			else
			{
				var size_delta = element.Rect.sizeDelta;

				if (resize_width)
				{
					size_delta.x = element.NewWidth - (parentSize.x * (element.Rect.anchorMax.x - element.Rect.anchorMin.x));
				}

				if (resize_height)
				{
					size_delta.y = element.NewHeight - (parentSize.y * (element.Rect.anchorMax.y - element.Rect.anchorMin.y));
				}

				element.Rect.sizeDelta = size_delta;
			}
		}

		/// <summary>
		/// Invoke OnElementChanged event.
		/// </summary>
		/// <param name="element">Element.</param>
		/// <param name="properties">Properties.</param>
		protected void OnElementChangedInvoke(RectTransform element, DrivenTransformProperties properties)
		{
			OnElementChanged?.Invoke(element, properties);
		}

		/// <summary>
		/// Should animate movement for the specified instance?
		/// </summary>
		/// <param name="instanceId">Instance ID.</param>
		/// <returns>true if should animate; otherwise false.</returns>
		protected bool ShouldAnimateMovement(int instanceId) => MovementAnimation && Application.isPlaying && (MovementAnimateAll || ExistingElementsID.Contains(instanceId));

		/// <summary>
		/// Set elements positions.
		/// </summary>
		protected virtual void SetPositions()
		{
			foreach (var element in ElementsGroup.Elements)
			{
				var changed = element.IsPositionChanged || (ChangePivot && element.ChangedPivot) || (ChangeRotation && element.ChangedRotation);
				if (!changed)
				{
					continue;
				}

				var id = element.Rect.GetInstanceID();
				if (ShouldAnimateMovement(id))
				{
					var rotation = Quaternion.Euler(element.NewEulerAngles);
					if (!MovementTargets.TryGetValue(id, out var settings) || (settings.EndPosition != element.PositionPivot) || (ChangePivot && (settings.EndPivot != element.NewPivot)) || (ChangeRotation && (settings.EndRotation != rotation)))
					{
						MovementTargets[id] = new MovementTarget(element.Rect, element.PositionPivot, element.NewPivot, rotation, UnscaledTime);
					}
				}
				else
				{
					if (ChangeRotation)
					{
						element.Rect.localEulerAngles = element.NewEulerAngles;
					}

					if (ChangePivot)
					{
						element.Rect.pivot = element.NewPivot;
					}

					element.Rect.localPosition = element.PositionPivot;
				}
			}
		}

		/// <summary>
		/// Sum values of the list.
		/// </summary>
		/// <param name="list">List.</param>
		/// <returns>Sum.</returns>
		protected static GroupSize Sum(List<GroupSize> list)
		{
			var result = default(GroupSize);
			foreach (var item in list)
			{
				result += item;
			}

			return result;
		}

		/// <summary>
		/// Sum values of the list.
		/// </summary>
		/// <param name="list">List.</param>
		/// <returns>Sum.</returns>
		protected static float Sum(List<float> list)
		{
			var result = 0f;
			foreach (var item in list)
			{
				result += item;
			}

			return result;
		}

		#region Group

		/// <summary>
		/// Reverse list.
		/// </summary>
		/// <param name="list">List.</param>
		protected static void ReverseList(List<LayoutElementInfo> list)
		{
			list.Reverse();
		}

		/// <summary>
		/// Group elements by columns in the vertical order.
		/// </summary>
		/// <param name="maxColumns">Maximum columns count.</param>
		protected void GroupByColumnsVertical(int maxColumns)
		{
			int i = 0;
			for (int column = 0; column < maxColumns; column++)
			{
				int max_rows = Mathf.CeilToInt(((float)(ElementsGroup.Count - i)) / (maxColumns - column));
				for (int row = 0; row < max_rows; row++)
				{
					ElementsGroup.SetPosition(i, row, column);

					i += 1;
				}
			}
		}

		/// <summary>
		/// Group elements by columns in the horizontal order.
		/// </summary>
		/// <param name="maxColumns">Maximum columns count.</param>
		protected void GroupByColumnsHorizontal(int maxColumns)
		{
			int row = 0;

			for (int i = 0; i < ElementsGroup.Count; i += maxColumns)
			{
				int column = 0;
				var end = Mathf.Min(i + maxColumns, ElementsGroup.Count);
				for (int j = i; j < end; j++)
				{
					ElementsGroup.SetPosition(j, row, column);
					column += 1;
				}

				row += 1;
			}
		}

		/// <summary>
		/// Group elements by rows in the vertical order.
		/// </summary>
		/// <param name="maxRows">Maximum rows count.</param>
		protected void GroupByRowsVertical(int maxRows)
		{
			int column = 0;
			for (int i = 0; i < ElementsGroup.Count; i += maxRows)
			{
				int row = 0;
				var end = Mathf.Min(i + maxRows, ElementsGroup.Count);
				for (int j = i; j < end; j++)
				{
					ElementsGroup.SetPosition(j, row, column);
					row += 1;
				}

				column += 1;
			}
		}

		/// <summary>
		/// Group elements by rows in the horizontal order.
		/// </summary>
		/// <param name="maxRows">Maximum rows count.</param>
		protected void GroupByRowsHorizontal(int maxRows)
		{
			int i = 0;
			for (int row = 0; row < maxRows; row++)
			{
				int max_columns = Mathf.CeilToInt((float)(ElementsGroup.Count - i) / (maxRows - row));
				for (int column = 0; column < max_columns; column++)
				{
					ElementsGroup.SetPosition(i, row, column);
					i += 1;
				}
			}
		}

		/// <summary>
		/// Group the specified uiElements by columns.
		/// </summary>
		/// <param name="maxColumns">Max allowed columns.</param>
		protected void GroupByColumns(int maxColumns)
		{
			if (IsHorizontal)
			{
				GroupByColumnsHorizontal(maxColumns);
			}
			else
			{
				GroupByColumnsVertical(maxColumns);
			}
		}

		/// <summary>
		/// Group the specified uiElements by rows.
		/// </summary>
		/// <param name="maxRows">Max allowed rows.</param>
		protected void GroupByRows(int maxRows)
		{
			if (IsHorizontal)
			{
				GroupByRowsHorizontal(maxRows);
			}
			else
			{
				GroupByRowsVertical(maxRows);
			}
		}
		#endregion

		#region Sizes

		/// <summary>
		/// Resize elements.
		/// </summary>
		protected virtual void SetInitialSizes()
		{
			if (ChildrenWidth == ChildrenSize.DoNothing && ChildrenHeight == ChildrenSize.DoNothing)
			{
				return;
			}

			if (ElementsGroup.Count == 0)
			{
				return;
			}

			var max_size = FindMaxPreferredSize();

			foreach (var element in ElementsGroup.Elements)
			{
				SetInitialSize(element, max_size);
			}
		}

		Vector2 FindMaxPreferredSize()
		{
			var max_size = new Vector2(-1f, -1f);

			foreach (var element in ElementsGroup.Elements)
			{
				max_size.x = Mathf.Max(max_size.x, element.PreferredWidth);
				max_size.y = Mathf.Max(max_size.y, element.PreferredHeight);
			}

			if (ChildrenWidth != ChildrenSize.SetMaxFromPreferred)
			{
				max_size.x = -1f;
			}

			if (ChildrenHeight != ChildrenSize.SetMaxFromPreferred)
			{
				max_size.y = -1f;
			}

			return max_size;
		}

		void SetInitialSize(LayoutElementInfo element, Vector2 max_size)
		{
			if (ChildrenWidth != ChildrenSize.DoNothing)
			{
				element.NewWidth = (max_size.x != -1f) ? max_size.x : element.PreferredWidth;
			}

			if (ChildrenHeight != ChildrenSize.DoNothing)
			{
				element.NewHeight = (max_size.y != -1f) ? max_size.y : element.PreferredHeight;
			}
		}

		/// <summary>
		/// Resize elements width to fit.
		/// </summary>
		/// <param name="increaseOnly">Size can be only increased.</param>
		protected void ResizeWidthToFit(bool increaseOnly)
		{
			var width = InternalSize.x;
			for (int row = 0; row < ElementsGroup.Rows; row++)
			{
				ResizeToFit(width, ElementsGroup.GetRow(row), Spacing.x, RectTransform.Axis.Horizontal, increaseOnly);
			}
		}

		/// <summary>
		/// Resize specified elements to fit.
		/// </summary>
		/// <param name="size">Size.</param>
		/// <param name="elements">Elements.</param>
		/// <param name="spacing">Spacing.</param>
		/// <param name="axis">Axis to fit.</param>
		/// <param name="increaseOnly">Size can be only increased.</param>
		protected static void ResizeToFit(float size, List<LayoutElementInfo> elements, float spacing, RectTransform.Axis axis, bool increaseOnly)
		{
			var sizes = axis == RectTransform.Axis.Horizontal ? SizesInfo.GetWidths(elements) : SizesInfo.GetHeights(elements);
			var free_space = size - sizes.TotalPreferred - ((elements.Count - 1) * spacing);

			if (increaseOnly)
			{
				free_space = Mathf.Max(0f, free_space);
				size = Mathf.Max(0f, size);
				sizes.TotalMin = sizes.TotalPreferred;
			}

			var per_flexible = free_space > 0f ? free_space / sizes.TotalFlexible : 0f;

			var minPrefLerp = 1f;
			if (sizes.TotalMin != sizes.TotalPreferred)
			{
				minPrefLerp = Mathf.Clamp01((size - sizes.TotalMin - ((elements.Count - 1) * spacing)) / (sizes.TotalPreferred - sizes.TotalMin));
			}

			for (int i = 0; i < elements.Count; i++)
			{
				var element_size = Mathf.Lerp(sizes.Sizes[i].Min, sizes.Sizes[i].Preferred, minPrefLerp) + (per_flexible * sizes.Sizes[i].Flexible);
				elements[i].SetSize(axis, element_size);
			}
		}

		/// <summary>
		/// Shrink elements width to fit.
		/// </summary>
		protected void ShrinkWidthToFit()
		{
			var width = InternalSize.x;
			for (int row = 0; row < ElementsGroup.Rows; row++)
			{
				ShrinkToFit(width, ElementsGroup.GetRow(row), Spacing.x, RectTransform.Axis.Horizontal);
			}
		}

		/// <summary>
		/// Resize row height to fit.
		/// </summary>
		/// <param name="increaseOnly">Size can be only increased.</param>
		protected void ResizeRowHeightToFit(bool increaseOnly)
		{
			ResizeToFit(InternalSize.y, ElementsGroup, Spacing.y, RectTransform.Axis.Vertical, increaseOnly);
		}

		/// <summary>
		/// Shrink row height to fit.
		/// </summary>
		protected void ShrinkRowHeightToFit()
		{
			ShrinkToFit(InternalSize.y, ElementsGroup, Spacing.y, RectTransform.Axis.Vertical);
		}

		/// <summary>
		/// Shrink specified elements size to fit.
		/// </summary>
		/// <param name="size">Size.</param>
		/// <param name="elements">Elements.</param>
		/// <param name="spacing">Spacing.</param>
		/// <param name="axis">Axis to fit.</param>
		protected static void ShrinkToFit(float size, List<LayoutElementInfo> elements, float spacing, RectTransform.Axis axis)
		{
			var sizes = axis == RectTransform.Axis.Horizontal ? SizesInfo.GetWidths(elements) : SizesInfo.GetHeights(elements);

			float free_space = size - sizes.TotalPreferred - ((elements.Count - 1) * spacing);
			if (free_space > 0f)
			{
				return;
			}

			var per_flexible = free_space > 0f ? free_space / sizes.TotalFlexible : 0f;

			var minPrefLerp = 0f;
			if (sizes.TotalMin != sizes.TotalPreferred)
			{
				minPrefLerp = Mathf.Clamp01((size - sizes.TotalMin - ((elements.Count - 1) * spacing)) / (sizes.TotalPreferred - sizes.TotalMin));
			}

			for (int i = 0; i < elements.Count; i++)
			{
				var element_size = Mathf.Lerp(sizes.Sizes[i].Min, sizes.Sizes[i].Preferred, minPrefLerp) + (per_flexible * sizes.Sizes[i].Flexible);
				elements[i].SetSize(axis, element_size);
			}
		}

		/// <summary>
		/// Resize specified elements to fit.
		/// </summary>
		/// <param name="size">Size.</param>
		/// <param name="group">Group.</param>
		/// <param name="spacing">Spacing.</param>
		/// <param name="axis">Axis to fit.</param>
		/// <param name="increaseOnly">Size can be only increased.</param>
		protected static void ResizeToFit(float size, LayoutElementsGroup group, float spacing, RectTransform.Axis axis, bool increaseOnly)
		{
			var is_horizontal = axis == RectTransform.Axis.Horizontal;
			var sizes = is_horizontal ? SizesInfo.GetWidths(group) : SizesInfo.GetHeights(group);
			var n = is_horizontal ? group.Columns : group.Rows;
			var free_space = size - sizes.TotalPreferred - ((n - 1) * spacing);

			if (increaseOnly)
			{
				free_space = Mathf.Max(0f, free_space);
				size = Mathf.Max(0f, size);
				sizes.TotalMin = sizes.TotalPreferred;
			}

			var minPrefLerp = 1f;
			if (sizes.TotalMin != sizes.TotalPreferred)
			{
				minPrefLerp = Mathf.Clamp01((size - sizes.TotalMin - ((n - 1) * spacing)) / (sizes.TotalPreferred - sizes.TotalMin));
			}

			if (is_horizontal)
			{
				var per_flexible = free_space > 0f ? free_space / sizes.TotalFlexible : 0f;

				for (int column = 0; column < group.Columns; column++)
				{
					var element_size = Mathf.Lerp(sizes.Sizes[column].Min, sizes.Sizes[column].Preferred, minPrefLerp) + (per_flexible * sizes.Sizes[column].Flexible);

					foreach (var element in group.GetColumn(column))
					{
						element.SetSize(axis, element_size);
					}
				}
			}
			else
			{
				var per_flexible = free_space > 0f ? free_space / sizes.TotalFlexible : 0f;

				for (int rows = 0; rows < group.Rows; rows++)
				{
					var element_size = Mathf.Lerp(sizes.Sizes[rows].Min, sizes.Sizes[rows].Preferred, minPrefLerp) + (per_flexible * sizes.Sizes[rows].Flexible);
					foreach (var element in group.GetRow(rows))
					{
						element.SetSize(axis, element_size);
					}
				}
			}
		}

		/// <summary>
		/// Shrink specified elements to fit.
		/// </summary>
		/// <param name="size">Size.</param>
		/// <param name="group">Elements.</param>
		/// <param name="spacing">Spacing.</param>
		/// <param name="axis">Axis to fit.</param>
		protected static void ShrinkToFit(float size, LayoutElementsGroup group, float spacing, RectTransform.Axis axis)
		{
			var is_horizontal = axis == RectTransform.Axis.Horizontal;
			var sizes = is_horizontal ? SizesInfo.GetWidths(group) : SizesInfo.GetHeights(group);
			var n = is_horizontal ? group.Columns : group.Rows;

			var free_space = size - sizes.TotalPreferred - ((n - 1) * spacing);
			if (free_space > 0f)
			{
				return;
			}

			var minPrefLerp = 0f;
			if (sizes.TotalMin != sizes.TotalPreferred)
			{
				minPrefLerp = Mathf.Clamp01((size - sizes.TotalMin - ((n - 1) * spacing)) / (sizes.TotalPreferred - sizes.TotalMin));
			}

			if (is_horizontal)
			{
				var per_flexible = free_space > 0f ? free_space / sizes.TotalFlexible : 0f;

				for (int column = 0; column < group.Columns; column++)
				{
					var element_size = Mathf.Lerp(sizes.Sizes[column].Min, sizes.Sizes[column].Preferred, minPrefLerp) + (per_flexible * sizes.Sizes[column].Flexible);

					foreach (var element in group.GetColumn(column))
					{
						element.SetSize(axis, element_size);
					}
				}
			}
			else
			{
				var per_flexible = free_space > 0f ? free_space / sizes.TotalFlexible : 0f;

				for (int rows = 0; rows < group.Rows; rows++)
				{
					var element_size = Mathf.Lerp(sizes.Sizes[rows].Min, sizes.Sizes[rows].Preferred, minPrefLerp) + (per_flexible * sizes.Sizes[rows].Flexible);

					foreach (var element in group.GetRow(rows))
					{
						element.SetSize(axis, element_size);
					}
				}
			}
		}
		#endregion
	}
}