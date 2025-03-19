namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using EasyLayoutNS.Extensions;
	using UnityEngine;
	using UnityEngine.Events;
	using UnityEngine.Serialization;
	using UnityEngine.UI;

	/// <summary>
	/// EasyLayout.
	/// Warning: using RectTransform relative size with positive size delta (like 100% + 10) with ContentSizeFitter can lead to infinite increased size.
	/// </summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(RectTransform))]
	[AddComponentMenu("UI/Easy Layout/Easy Layout")]
	[HelpURL("https://ilih.name/unity-assets/EasyLayout/docs/easylayout.html")]
	public class EasyLayout : LayoutGroup, INotifyPropertyChanged, IObservable
	{
		readonly List<LayoutElementInfo> elements = new List<LayoutElementInfo>();

		readonly Stack<LayoutElementInfo> elementsCache = new Stack<LayoutElementInfo>();

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event OnChange OnChange;

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Occurs when a properties values changed except PaddingInner property.
		/// </summary>
		[SerializeField]
		public UnityEvent SettingsChanged = new UnityEvent();

		[SerializeField]
		[FormerlySerializedAs("GroupPosition")]
		Anchors groupPosition = Anchors.UpperLeft;

		/// <summary>
		/// The group position.
		/// </summary>
		public Anchors GroupPosition
		{
			get => groupPosition;

			set => Change(ref groupPosition, value, nameof(GroupPosition));
		}

		[SerializeField]
		[FormerlySerializedAs("Stacking")]
		Axis mainAxis = Axis.Horizontal;

		/// <summary>
		/// The stacking type.
		/// </summary>
		[Obsolete("Replaced with MainAxis.")]
		public Stackings Stacking
		{
			get => (MainAxis == Axis.Horizontal) ? Stackings.Horizontal : Stackings.Vertical;

			set => MainAxis = (value == Stackings.Horizontal) ? Axis.Horizontal : Axis.Vertical;
		}

		/// <summary>
		/// Main axis.
		/// </summary>
		public Axis MainAxis
		{
			get => mainAxis;

			set => Change(ref mainAxis, value, nameof(MainAxis));
		}

		[SerializeField]
		[FormerlySerializedAs("LayoutType")]
		LayoutTypes layoutType = LayoutTypes.Compact;

		/// <summary>
		/// The type of the layout.
		/// </summary>
		public LayoutTypes LayoutType
		{
			get => layoutType;

			set
			{
				if (layoutType != value)
				{
					LayoutGroup = null;
					layoutType = value;
					NotifyPropertyChanged(nameof(LayoutType));
				}
			}
		}

		EasyLayoutBaseType layoutGroup;

		/// <summary>
		/// Layout group.
		/// </summary>
		protected EasyLayoutBaseType LayoutGroup
		{
			get
			{
				if (layoutGroup == null)
				{
					layoutGroup = GetLayoutGroup();
					if (layoutGroup == null)
					{
						Debug.LogWarning(string.Format("Unsupported LayoutType: {0}", EnumHelper<LayoutTypes>.Instance.ToString(LayoutType)));
					}

					layoutGroup.OnElementChanged += ElementChanged;
				}

				return layoutGroup;
			}

			set
			{
				if (layoutGroup != null)
				{
					layoutGroup.OnElementChanged -= ElementChanged;
				}

				layoutGroup = value;

				if (layoutGroup != null)
				{
					layoutGroup.OnElementChanged += ElementChanged;
				}
			}
		}

		[SerializeField]
		[FormerlySerializedAs("CompactConstraint")]
		CompactConstraints compactConstraint = CompactConstraints.Flexible;

		/// <summary>
		/// Which constraint to use for the Grid layout.
		/// </summary>
		public CompactConstraints CompactConstraint
		{
			get => compactConstraint;

			set => Change(ref compactConstraint, value, nameof(CompactConstraint));
		}

		[SerializeField]
		[FormerlySerializedAs("CompactConstraintCount")]
		int compactConstraintCount = 1;

		/// <summary>
		/// How many elements there should be along the constrained axis.
		/// </summary>
		public int CompactConstraintCount
		{
			get => Mathf.Max(1, compactConstraintCount);

			set => Change(ref compactConstraintCount, value, nameof(CompactConstraintCount));
		}

		[SerializeField]
		[FormerlySerializedAs("GridConstraint")]
		GridConstraints gridConstraint = GridConstraints.Flexible;

		/// <summary>
		/// Which constraint to use for the Grid layout.
		/// </summary>
		public GridConstraints GridConstraint
		{
			get => gridConstraint;

			set => Change(ref gridConstraint, value, nameof(GridConstraint));
		}

		[SerializeField]
		[FormerlySerializedAs("GridConstraintCount")]
		int gridConstraintCount = 1;

		/// <summary>
		/// How many cells there should be along the constrained axis.
		/// </summary>
		public int GridConstraintCount
		{
			get => Mathf.Max(1, gridConstraintCount);

			set => Change(ref gridConstraintCount, value, nameof(GridConstraintCount));
		}

		/// <summary>
		/// Constraint count.
		/// </summary>
		public int ConstraintCount
		{
			get
			{
				return LayoutType switch
				{
					LayoutTypes.Compact => CompactConstraintCount,
					LayoutTypes.Grid => GridConstraintCount,
					LayoutTypes.Hex => HexSettings.ConstraintCount,
					_ => 0,
				};
			}
		}

		[SerializeField]
		[FormerlySerializedAs("RowAlign")]
		HorizontalAligns rowAlign = HorizontalAligns.Left;

		/// <summary>
		/// The row align.
		/// </summary>
		public HorizontalAligns RowAlign
		{
			get => rowAlign;

			set => Change(ref rowAlign, value, nameof(RowAlign));
		}

		[SerializeField]
		[FormerlySerializedAs("InnerAlign")]
		InnerAligns innerAlign = InnerAligns.Top;

		/// <summary>
		/// The inner align.
		/// </summary>
		public InnerAligns InnerAlign
		{
			get => innerAlign;

			set => Change(ref innerAlign, value, nameof(InnerAlign));
		}

		[SerializeField]
		[FormerlySerializedAs("CellAlign")]
		Anchors cellAlign = Anchors.UpperLeft;

		/// <summary>
		/// The cell align.
		/// </summary>
		public Anchors CellAlign
		{
			get => cellAlign;

			set => Change(ref cellAlign, value, nameof(CellAlign));
		}

		[SerializeField]
		[FormerlySerializedAs("Spacing")]
		Vector2 spacing = new Vector2(5, 5);

		/// <summary>
		/// The spacing.
		/// </summary>
		public Vector2 Spacing
		{
			get => spacing;

			set => Change(ref spacing, value, nameof(Spacing));
		}

		[SerializeField]
		[FormerlySerializedAs("Symmetric")]
		bool symmetric = true;

		/// <summary>
		/// Symmetric margin.
		/// </summary>
		public bool Symmetric
		{
			get => symmetric;

			set => Change(ref symmetric, value, nameof(Symmetric));
		}

		[SerializeField]
		[FormerlySerializedAs("Margin")]
		Vector2 margin = new Vector2(5, 5);

		/// <summary>
		/// The margin.
		/// </summary>
		public Vector2 Margin
		{
			get => margin;

			set => Change(ref margin, value, nameof(Margin));
		}

		[SerializeField]
		[HideInInspector]
		Padding marginInner;

		/// <summary>
		/// The margin.
		/// Should be used by ListView related scripts.
		/// </summary>
		public Padding MarginInner
		{
			get => marginInner;

			set => Change(ref marginInner, value, nameof(MarginInner));
		}

		[SerializeField]
		[FormerlySerializedAs("PaddingInner")]
		[HideInInspector]
		Padding paddingInner;

		/// <summary>
		/// The padding.
		/// Should be used by ListView related scripts.
		/// </summary>
		public Padding PaddingInner
		{
			get => paddingInner;

			set => Change(ref paddingInner, value, nameof(PaddingInner), false);
		}

		[SerializeField]
		[FormerlySerializedAs("MarginTop")]
		float marginTop = 5f;

		/// <summary>
		/// The margin top.
		/// </summary>
		public float MarginTop
		{
			get => marginTop;

			set => Change(ref marginTop, value, nameof(MarginTop));
		}

		[SerializeField]
		[FormerlySerializedAs("MarginBottom")]
		float marginBottom = 5f;

		/// <summary>
		/// The margin bottom.
		/// </summary>
		public float MarginBottom
		{
			get => marginBottom;

			set => Change(ref marginBottom, value, nameof(MarginBottom));
		}

		[SerializeField]
		[FormerlySerializedAs("MarginLeft")]
		float marginLeft = 5f;

		/// <summary>
		/// The margin left.
		/// </summary>
		public float MarginLeft
		{
			get => marginLeft;

			set => Change(ref marginLeft, value, nameof(MarginLeft));
		}

		[SerializeField]
		[FormerlySerializedAs("MarginRight")]
		float marginRight = 5f;

		/// <summary>
		/// The margin right.
		/// </summary>
		public float MarginRight
		{
			get => marginRight;

			set => Change(ref marginRight, value, nameof(MarginRight));
		}

		[SerializeField]
		[FormerlySerializedAs("RightToLeft")]
		bool rightToLeft = false;

		/// <summary>
		/// The right to left stacking.
		/// </summary>
		public bool RightToLeft
		{
			get => rightToLeft;

			set => Change(ref rightToLeft, value, nameof(RightToLeft));
		}

		[SerializeField]
		[FormerlySerializedAs("TopToBottom")]
		bool topToBottom = true;

		/// <summary>
		/// The top to bottom stacking.
		/// </summary>
		public bool TopToBottom
		{
			get => topToBottom;

			set => Change(ref topToBottom, value, nameof(TopToBottom));
		}

		[SerializeField]
		[FormerlySerializedAs("SkipInactive")]
		bool skipInactive = true;

		/// <summary>
		/// The skip inactive.
		/// </summary>
		public bool SkipInactive
		{
			get => skipInactive;

			set => Change(ref skipInactive, value, nameof(SkipInactive));
		}

		[SerializeField]
		bool resetRotation;

		/// <summary>
		/// Reset rotation for the controlled elements.
		/// </summary>
		public bool ResetRotation
		{
			get => resetRotation;

			set => Change(ref resetRotation, value, nameof(ResetRotation));
		}

		/// <summary>
		/// The filter.
		/// </summary>
		[Obsolete("Replaced with ShouldIgnore")]
		public Func<IEnumerable<GameObject>, IEnumerable<GameObject>> Filter
		{
			get => throw new NotSupportedException("Obsolete.");

			set => throw new NotSupportedException("Obsolete.");
		}

		Func<RectTransform, bool> shouldIgnore;

		/// <summary>
		/// The filter.
		/// </summary>
		public Func<RectTransform, bool> ShouldIgnore
		{
			get => shouldIgnore;

			set => Change(ref shouldIgnore, value, nameof(ShouldIgnore));
		}

		[SerializeField]
		[Tooltip("ILayoutElement options will be ignored. Increases performance without side effects if ChildrenWidth and ChildrenHeight are not controlled.")]
		bool ignoreLayoutElementSizes = false;

		/// <summary>
		/// ILayoutElement options will be ignored. Increases performance without side effects if ChildrenWidth and ChildrenHeight are not controlled.
		/// </summary>
		public bool IgnoreLayoutElementSizes
		{
			get => ignoreLayoutElementSizes;

			set => Change(ref ignoreLayoutElementSizes, value, nameof(IgnoreLayoutElementSizes));
		}

		[SerializeField]
		[FormerlySerializedAs("ChildrenWidth")]
		ChildrenSize childrenWidth;

		/// <summary>
		/// How to control width of the children.
		/// </summary>
		public ChildrenSize ChildrenWidth
		{
			get => childrenWidth;

			set => Change(ref childrenWidth, value, nameof(ChildrenWidth));
		}

		[SerializeField]
		[FormerlySerializedAs("ChildrenHeight")]
		ChildrenSize childrenHeight;

		/// <summary>
		/// How to control height of the children.
		/// </summary>
		public ChildrenSize ChildrenHeight
		{
			get => childrenHeight;

			set => Change(ref childrenHeight, value, nameof(ChildrenHeight));
		}

		[SerializeField]
		EasyLayoutFlexSettings flexSettings = new EasyLayoutFlexSettings();

		/// <summary>
		/// Settings for the Flex layout type.
		/// </summary>
		public EasyLayoutFlexSettings FlexSettings
		{
			get => flexSettings;

			set
			{
				if (flexSettings != value)
				{
					flexSettings.OnChange -= FlexSettingsChanged;
					flexSettings = value;
					flexSettings.OnChange += FlexSettingsChanged;
					NotifyPropertyChanged(nameof(FlexSettings));
				}
			}
		}

		[SerializeField]
		EasyLayoutStaggeredSettings staggeredSettings = new EasyLayoutStaggeredSettings();

		/// <summary>
		/// Settings for the Staggered layout type.
		/// </summary>
		public EasyLayoutStaggeredSettings StaggeredSettings
		{
			get => staggeredSettings;

			set
			{
				if (staggeredSettings != value)
				{
					staggeredSettings.OnChange -= StaggeredSettingsChanged;
					staggeredSettings = value;
					staggeredSettings.OnChange += StaggeredSettingsChanged;
					NotifyPropertyChanged(nameof(StaggeredSettings));
				}
			}
		}

		[SerializeField]
		EasyLayoutEllipseSettings ellipseSettings = new EasyLayoutEllipseSettings();

		/// <summary>
		/// Settings for the Ellipse layout type.
		/// </summary>
		public EasyLayoutEllipseSettings EllipseSettings
		{
			get => ellipseSettings;

			set
			{
				if (ellipseSettings != value)
				{
					ellipseSettings.OnChange -= EllipseSettingsChanged;
					ellipseSettings = value;
					ellipseSettings.OnChange += EllipseSettingsChanged;
					NotifyPropertyChanged(nameof(EllipseSettings));
				}
			}
		}

		[SerializeField]
		EasyLayoutHexSettings hexSettings = new EasyLayoutHexSettings();

		/// <summary>
		/// Settings for the Hex layout type.
		/// </summary>
		public EasyLayoutHexSettings HexSettings
		{
			get => hexSettings;

			set
			{
				if (hexSettings != value)
				{
					hexSettings.OnChange -= HexSettingsChanged;
					hexSettings = value;
					hexSettings.OnChange += HexSettingsChanged;
					NotifyPropertyChanged(nameof(HexSettings));
				}
			}
		}

		/// <summary>
		/// Control width of children.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		[Obsolete("Use ChildrenWidth with ChildrenSize.SetPreferred instead.")]
		public bool ControlWidth;

		/// <summary>
		/// Control height of children.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		[Obsolete("Use ChildrenHeight with ChildrenSize.SetPreferred instead.")]
		[FormerlySerializedAs("ControlHeight")]
		public bool ControlHeight;

		/// <summary>
		/// Sets width of the children to maximum width from them.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		[Obsolete("Use ChildrenWidth with ChildrenSize.SetMaxFromPreferred instead.")]
		[FormerlySerializedAs("MaxWidth")]
		public bool MaxWidth;

		/// <summary>
		/// Sets height of the children to maximum height from them.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		[Obsolete("Use ChildrenHeight with ChildrenSize.SetMaxFromPreferred instead.")]
		[FormerlySerializedAs("MaxHeight")]
		public bool MaxHeight;

		[SerializeField]
		[Tooltip("Animate GameObjects reposition. Warning: can decrease performance.")]
		bool movementAnimation = false;

		/// <summary>
		/// Movement animation.
		/// </summary>
		public bool MovementAnimation
		{
			get => movementAnimation;

			set => Change(ref movementAnimation, value, nameof(MovementAnimation));
		}

		[SerializeField]
		[Tooltip("Animate all elements if enabled; otherwise new elements will not be animated.")]
		bool movementAnimateAll = true;

		/// <summary>
		/// Animate all elements if enabled; otherwise new elements will not be animated.
		/// </summary>
		public bool MovementAnimateAll
		{
			get => movementAnimateAll;

			set => Change(ref movementAnimateAll, value, nameof(MovementAnimateAll));
		}

		/// <summary>
		/// ID of existing elements.
		/// </summary>
		public readonly HashSet<int> ExistingElementsID = new HashSet<int>();

		[SerializeField]
		[Tooltip("Animate GameObjects resize. Warning: can decrease performance.")]
		bool resizeAnimation = false;

		[SerializeField]
		AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 0.3f, 1f);

		/// <summary>
		/// Movement animation curve.
		/// </summary>
		public AnimationCurve MovementCurve
		{
			get => movementCurve;

			set => Change(ref movementCurve, value, nameof(MovementCurve));
		}

		/// <summary>
		/// Resize animation.
		/// </summary>
		public bool ResizeAnimation
		{
			get => resizeAnimation;

			set => Change(ref resizeAnimation, value, nameof(ResizeAnimation));
		}

		[SerializeField]
		[Tooltip("Animate all elements if enabled; otherwise new elements will not be animated.")]
		bool resizeAnimateAll = true;

		/// <summary>
		/// Animate all elements if enabled; otherwise new elements will not be animated.
		/// </summary>
		public bool ResizeAnimateAll
		{
			get => resizeAnimateAll;

			set => Change(ref resizeAnimateAll, value, nameof(ResizeAnimateAll));
		}

		[SerializeField]
		AnimationCurve resizeCurve = AnimationCurve.EaseInOut(0f, 0f, 0.3f, 1f);

		/// <summary>
		/// Resize animation curve.
		/// </summary>
		public AnimationCurve ResizeCurve
		{
			get => resizeCurve;

			set => Change(ref resizeCurve, value, nameof(ResizeCurve));
		}

		[SerializeField]
		bool unscaledTime = true;

		/// <summary>
		/// Unscaled time.
		/// </summary>
		public bool UnscaledTime
		{
			get => unscaledTime;

			set => Change(ref unscaledTime, value, nameof(UnscaledTime));
		}

		/// <summary>
		/// Internal size.
		/// </summary>
		public Vector2 InternalSize
		{
			get
			{
				var size = rectTransform.rect.size;
				var padding = PaddingInner;
				size.x -= MarginFullHorizontal + padding.Horizontal;
				size.y -= MarginFullVertical + padding.Vertical;

				return size;
			}
		}

		/// <summary>
		/// Current sizes info.
		/// </summary>
		public GroupSize CurrentSize
		{
			get;
			protected set;
		}

		/// <summary>
		/// Gets or sets the size of the inner block.
		/// </summary>
		/// <value>The size of the inner block.</value>
		public Vector2 BlockSize
		{
			get;
			protected set;
		}

		/// <summary>
		/// Gets or sets the UI size.
		/// </summary>
		/// <value>The UI size.</value>
		public Vector2 UISize
		{
			get;
			protected set;
		}

		/// <summary>
		/// Size in elements.
		/// </summary>
		public Vector2 Size
		{
			get;
			protected set;
		}

		/// <summary>
		/// Gets the minimum width.
		/// </summary>
		/// <value>The minimum width.</value>
		public override float minWidth => (ChildrenWidth == ChildrenSize.DoNothing) ? CurrentSize.Width : CurrentSize.MinWidth;

		/// <summary>
		/// Gets the minimum height.
		/// </summary>
		/// <value>The minimum height.</value>
		public override float minHeight => (ChildrenHeight == ChildrenSize.DoNothing) ? CurrentSize.Height : CurrentSize.MinHeight;

		/// <summary>
		/// Gets the preferred width.
		/// </summary>
		/// <value>The preferred width.</value>
		public override float preferredWidth => (ChildrenWidth == ChildrenSize.DoNothing) ? CurrentSize.Width : CurrentSize.PreferredWidth;

		/// <summary>
		/// Gets the preferred height.
		/// </summary>
		/// <value>The preferred height.</value>
		public override float preferredHeight => (ChildrenHeight == ChildrenSize.DoNothing) ? CurrentSize.Height : CurrentSize.PreferredHeight;

		/// <summary>
		/// Summary horizontal margin.
		/// </summary>
		public float MarginHorizontal => Symmetric ? (Margin.x + Margin.x) : (MarginLeft + MarginRight);

		/// <summary>
		/// Summary vertical margin.
		/// </summary>
		public float MarginVertical => Symmetric ? (Margin.y + Margin.y) : (MarginTop + MarginBottom);

		/// <summary>
		/// Summary horizontal margin with MarginInner.
		/// </summary>
		public float MarginFullHorizontal => (Symmetric ? (Margin.x + Margin.x) : (MarginLeft + MarginRight)) + MarginInner.Horizontal;

		/// <summary>
		/// Summary vertical margin with MarginInner.
		/// </summary>
		public float MarginFullVertical => (Symmetric ? (Margin.y + Margin.y) : (MarginTop + MarginBottom)) + MarginInner.Vertical;

		/// <summary>
		/// Is horizontal stacking?
		/// </summary>
		public bool IsHorizontal => MainAxis == Axis.Horizontal;

		/// <summary>
		/// Size of the main axis.
		/// </summary>
		public float MainAxisSize
		{
			get
			{
				return IsHorizontal
					? rectTransform.rect.width - MarginFullHorizontal
					: rectTransform.rect.height - MarginFullVertical;
			}
		}

		/// <summary>
		/// Size of the sub axis.
		/// </summary>
		public float SubAxisSize
		{
			get
			{
				return !IsHorizontal
					? rectTransform.rect.width - MarginFullHorizontal
					: rectTransform.rect.height - MarginFullVertical;
			}
		}

		/// <summary>
		/// Properties tracker.
		/// </summary>
		protected DrivenRectTransformTracker PropertiesTracker;

		/// <summary>
		/// Children list.
		/// Used if SkipInactive disabled.
		/// </summary>
		protected List<RectTransform> Children = new List<RectTransform>();

		/// <summary>
		/// Change value.
		/// </summary>
		/// <typeparam name="T">Type of field.</typeparam>
		/// <param name="field">Field value.</param>
		/// <param name="value">New value.</param>
		/// <param name="propertyName">Property name.</param>
		/// <param name="invokeSettingsChanged">Invoke settings changed event.</param>
		protected void Change<T>(ref T field, T value, string propertyName, bool invokeSettingsChanged = true)
		{
			if (!EqualityComparer<T>.Default.Equals(field, value))
			{
				field = value;
				NotifyPropertyChanged(propertyName, invokeSettingsChanged);
			}
		}

		/// <summary>
		/// Property changed.
		/// </summary>
		/// <param name="propertyName">Property name.</param>
		/// <param name="invokeSettingsChanged">Should invoke SettingsChanged event?</param>
		protected void NotifyPropertyChanged(string propertyName, bool invokeSettingsChanged = true)
		{
			SetDirty();

			OnChange?.Invoke();

			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

			if (invokeSettingsChanged)
			{
				SettingsChanged.Invoke();
			}
		}

		void FlexSettingsChanged() => NotifyPropertyChanged(nameof(FlexSettings));

		void StaggeredSettingsChanged() => NotifyPropertyChanged(nameof(StaggeredSettings));

		void EllipseSettingsChanged() => NotifyPropertyChanged(nameof(EllipseSettings));

		void HexSettingsChanged() => NotifyPropertyChanged(nameof(HexSettings));

		/// <summary>
		/// Start this instance.
		/// </summary>
		protected override void Start()
		{
			flexSettings.OnChange += FlexSettingsChanged;
			staggeredSettings.OnChange += StaggeredSettingsChanged;
			ellipseSettings.OnChange += EllipseSettingsChanged;
			hexSettings.OnChange += HexSettingsChanged;
		}

		/// <summary>
		/// Process the disable event.
		/// </summary>
		protected override void OnDisable()
		{
			PropertiesTracker.Clear();

			base.OnDisable();
		}

		/// <summary>
		/// Process the destroy event.
		/// </summary>
		protected override void OnDestroy()
		{
			if (flexSettings != null)
			{
				flexSettings.OnChange -= FlexSettingsChanged;
			}

			if (staggeredSettings != null)
			{
				staggeredSettings.OnChange -= StaggeredSettingsChanged;
			}

			if (ellipseSettings != null)
			{
				ellipseSettings.OnChange -= EllipseSettingsChanged;
			}

			if (hexSettings != null)
			{
				hexSettings.OnChange -= HexSettingsChanged;
			}

			LayoutGroup = null;

			base.OnDestroy();
		}

		/// <summary>
		/// Process the RectTransform removed event.
		/// </summary>
		protected virtual void OnRectTransformRemoved() => SetDirty();

		/// <summary>
		/// Sets the layout horizontal.
		/// </summary>
		public override void SetLayoutHorizontal()
		{
			UpdateElements();

			PerformLayout(true, ResizeType.Horizontal);
		}

		/// <summary>
		/// Sets the layout vertical.
		/// </summary>
		public override void SetLayoutVertical()
		{
			UpdateElements();

			PerformLayout(true, ResizeType.Vertical);
		}

		/// <summary>
		/// Calculates the layout input horizontal.
		/// </summary>
		public override void CalculateLayoutInputHorizontal()
		{
			base.CalculateLayoutInputHorizontal();
			UpdateElements();

			PerformLayout(false, ResizeType.None);
		}

		/// <summary>
		/// Calculates the layout input vertical.
		/// </summary>
		public override void CalculateLayoutInputVertical()
		{
			UpdateElements();

			PerformLayout(false, ResizeType.None);
		}

		/// <summary>
		/// Marks layout to update.
		/// </summary>
		public void NeedUpdateLayout()
		{
			SetDirty();
		}

		/// <summary>
		/// Calculates the size of the layout.
		/// </summary>
		public void CalculateLayoutSize()
		{
			UpdateElements();

			PerformLayout(false);
		}

		/// <summary>
		/// Updates the layout.
		/// </summary>
		public void UpdateLayout() => LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

		/// <summary>
		/// Get children.
		/// </summary>
		/// <returns>Children.</returns>
		protected List<RectTransform> GetChildren()
		{
			if (SkipInactive)
			{
				return rectChildren;
			}

			Children.Clear();
			for (int i = 0; i < rectTransform.childCount; i++)
			{
				Children.Add(rectTransform.GetChild(i) as RectTransform);
			}

			return Children;
		}

		/// <summary>
		/// Update LayoutElements.
		/// </summary>
		protected void UpdateElements()
		{
			ClearElements();

			var children = GetChildren();
			var ignore = ShouldIgnore != null;
			foreach (var rt in children)
			{
				if (ignore && ShouldIgnore(rt))
				{
					continue;
				}

				elements.Add(CreateElement(rt));
			}
		}

		/// <summary>
		/// Reset layout elements list.
		/// </summary>
		protected void ClearElements()
		{
			foreach (var e in elements)
			{
				elementsCache.Push(e);
			}

			elements.Clear();
		}

		/// <summary>
		/// Create layout element.
		/// </summary>
		/// <param name="elem">Element.</param>
		/// <returns>Element data.</returns>
		protected LayoutElementInfo CreateElement(RectTransform elem)
		{
			var info = (elementsCache.Count > 0) ? elementsCache.Pop() : new LayoutElementInfo();
			var active = SkipInactive || elem.gameObject.activeInHierarchy;
			info.SetElement(elem, active, this, false, IgnoreLayoutElementSizes);
			if (ResetRotation && (LayoutType != LayoutTypes.Ellipse))
			{
				info.NewEulerAngles = Vector3.zero;
			}

			return info;
		}

		/// <summary>
		/// Gets the margin top.
		/// </summary>
		/// <returns>Top margin.</returns>
		public float GetMarginTop() => Symmetric ? Margin.y : MarginTop;

		/// <summary>
		/// Gets the margin bottom.
		/// </summary>
		/// <returns>Bottom margin.</returns>
		public float GetMarginBottom() => Symmetric ? Margin.y : MarginBottom;

		/// <summary>
		/// Gets the margin left.
		/// </summary>
		/// <returns>Left margin.</returns>
		public float GetMarginLeft() => Symmetric ? Margin.x : MarginLeft;

		/// <summary>
		/// Gets the margin right.
		/// </summary>
		/// <returns>Right margin.</returns>
		public float GetMarginRight() => Symmetric ? Margin.x : MarginRight;

		/// <summary>
		/// Get layout group.
		/// </summary>
		/// <returns>Layout group.</returns>
		protected EasyLayoutBaseType GetLayoutGroup()
		{
			return LayoutType switch
			{
				LayoutTypes.Compact => new EasyLayoutCompact(),
				LayoutTypes.Grid => new EasyLayoutGrid(),
				LayoutTypes.Flex => new EasyLayoutFlex(),
				LayoutTypes.Staggered => new EasyLayoutStaggered(),
				LayoutTypes.Ellipse => new EasyLayoutEllipse(),
				LayoutTypes.Hex => new EasyLayoutHex(),
				_ => null,
			};
		}

		/// <summary>
		/// Perform layout.
		/// </summary>
		/// <param name="setValues">Set element positions and sizes.</param>
		/// <param name="resizeType">Resize type.</param>
		protected void PerformLayout(bool setValues, ResizeType resizeType = ResizeType.Horizontal | ResizeType.Vertical)
		{
			if (!gameObject.activeInHierarchy)
			{
				return;
			}

			if (LayoutGroup == null)
			{
				Debug.LogWarning(string.Format("Layout group not found: {0}", EnumHelper<LayoutTypes>.Instance.ToString(LayoutType)));
				return;
			}

			PropertiesTracker.Clear();

			LayoutGroup.LoadSettings(this);
			CurrentSize = LayoutGroup.PerformLayout(elements, setValues, resizeType);

			BlockSize = elements.Count > 0 ? new Vector2(CurrentSize.Width, CurrentSize.Height) : Vector2.zero;

			CurrentSize += new Vector2(MarginFullHorizontal, MarginFullVertical);
			UISize = new Vector2(CurrentSize.Width, CurrentSize.Height);

			if (setValues)
			{
				ExistingElementsID.Clear();
				foreach (var el in elements)
				{
					ExistingElementsID.Add(el.Rect.GetInstanceID());
				}
			}
		}

		/// <summary>
		/// Update.
		/// </summary>
		public virtual void Update()
		{
			if (LayoutGroup == null)
			{
				return;
			}

			LayoutGroup.Animate();
		}

		/// <summary>
		/// Process element changed event.
		/// </summary>
		/// <param name="element">Element.</param>
		/// <param name="properties">Properties.</param>
		protected virtual void ElementChanged(RectTransform element, DrivenTransformProperties properties)
		{
			PropertiesTracker.Add(this, element, properties);
		}

		/// <summary>
		/// Set element size.
		/// </summary>
		/// <param name="element">Element.</param>
		[Obsolete("No more used.")]
		public void SetElementSize(LayoutElementInfo element)
		{
			var driven_properties = DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.AnchoredPositionZ;

			if (ChildrenWidth != ChildrenSize.DoNothing)
			{
				driven_properties |= DrivenTransformProperties.SizeDeltaX;
				element.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, element.NewWidth);
			}

			if (ChildrenHeight != ChildrenSize.DoNothing)
			{
				driven_properties |= DrivenTransformProperties.SizeDeltaY;
				element.Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, element.NewHeight);
			}

			if (LayoutType == LayoutTypes.Ellipse || ResetRotation)
			{
				driven_properties |= DrivenTransformProperties.Rotation;
				element.Rect.localEulerAngles = element.NewEulerAngles;
			}

			if (LayoutType == LayoutTypes.Ellipse)
			{
				driven_properties |= DrivenTransformProperties.Pivot;
				element.Rect.pivot = element.NewPivot;
			}

			PropertiesTracker.Add(this, element.Rect, driven_properties);
		}

		/// <summary>
		/// Get element position in the group.
		/// </summary>
		/// <param name="element">Element.</param>
		/// <returns>Position.</returns>
		public EasyLayoutPosition GetElementPosition(RectTransform element)
		{
			return LayoutGroup.GetElementPosition(element);
		}

		/// <summary>
		/// Awake this instance.
		/// </summary>
		protected override void Awake()
		{
			base.Awake();
			Upgrade();
		}

		#if UNITY_EDITOR
		/// <summary>
		/// Update layout when parameters changed.
		/// </summary>
		protected override void OnValidate()
		{
			LayoutGroup = null;

			SetDirty();
			SettingsChanged.Invoke();
		}
		#endif

		[SerializeField]
		[HideInInspector]
		int version = 0;

		/// <summary>
		/// Upgrade to keep compatibility between versions.
		/// </summary>
		public virtual void Upgrade()
		{
			#pragma warning disable 0618
			if (version == 0)
			{
				if (ControlWidth)
				{
					ChildrenWidth = MaxWidth ? ChildrenSize.SetMaxFromPreferred : ChildrenSize.SetPreferred;
				}

				if (ControlHeight)
				{
					ChildrenHeight = MaxHeight ? ChildrenSize.SetMaxFromPreferred : ChildrenSize.SetPreferred;
				}

				version = 1;
			}
			#pragma warning restore 0618
		}

		/// <summary>
		/// Get debug information.
		/// </summary>
		/// <returns>Debug information.</returns>
		public virtual string GetDebugInfo()
		{
			var sb = new System.Text.StringBuilder();
			GetDebugInfo(sb);

			return sb.ToString();
		}

		/// <summary>
		/// Get time.
		/// </summary>
		/// <param name="unscaled">Unscaled time.</param>
		/// <returns>Time.</returns>
		public static float GetTime(bool unscaled)
		{
			return unscaled ? Time.unscaledTime : Time.time;
		}

		/// <summary>
		/// Get delta time.
		/// </summary>
		/// <param name="unscaled">Unscaled time.</param>
		/// <returns>Delta time.</returns>
		public static float GetDeltaTime(bool unscaled)
		{
			return unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
		}

		/// <summary>
		/// Get debug information.
		/// </summary>
		/// <param name="sb">String builder.</param>
		public virtual void GetDebugInfo(System.Text.StringBuilder sb)
		{
			sb.AppendValue("Unity Version: ", Application.unityVersion);
			sb.AppendValue("RectTransform.size: ", rectTransform.rect.size);
			sb.AppendValue("localScale: ", rectTransform.localScale);
			sb.AppendValueEnum("Main Axis: ", MainAxis);
			sb.AppendValueEnum("Type: ", LayoutType);

			switch (LayoutType)
			{
				case LayoutTypes.Compact:
					sb.AppendValueEnum("\tGroup Position: ", GroupPosition);
					sb.AppendValueEnum("\tRow Align: ", RowAlign);
					sb.AppendValueEnum("\tInner Align: ", InnerAlign);
					sb.AppendValueEnum("\tCompact Constraint: ", CompactConstraint);
					sb.AppendValue("\tCompact Constraint Count: ", CompactConstraintCount);
					break;
				case LayoutTypes.Grid:
					sb.AppendValueEnum("\tGroup Position: ", GroupPosition);
					sb.AppendValueEnum("\tCell Align: ", CellAlign);
					sb.AppendValueEnum("\tGrid Constraint: ", GridConstraint);
					sb.AppendValue("\tGrid Constraint Count: ", GridConstraintCount);
					break;
				case LayoutTypes.Flex:
					FlexSettings.GetDebugInfo(sb);
					break;
				case LayoutTypes.Staggered:
					StaggeredSettings.GetDebugInfo(sb);
					break;
				case LayoutTypes.Ellipse:
					EllipseSettings.GetDebugInfo(sb);
					break;
				default:
					sb.AppendLine("\tUnknown type: no details");
					break;
			}

			sb.AppendValue("PaddingInner: ", PaddingInner);
			sb.AppendValue("Spacing: ", Spacing);
			sb.AppendValue("Margin Symmetric: ", Symmetric);

			if (Symmetric)
			{
				sb.AppendValue("Margin: ", Margin);
			}
			else
			{
				sb.AppendValue("Margin Left: ", MarginLeft);
				sb.AppendValue("Margin Right: ", MarginRight);
				sb.AppendValue("Margin Top: ", MarginTop);
				sb.AppendValue("Margin Bottom: ", MarginBottom);
			}

			sb.AppendValue("TopToBottom: ", TopToBottom);
			sb.AppendValue("RightToLeft: ", RightToLeft);
			sb.AppendValue("Skip Inactive: ", SkipInactive);
			sb.AppendValue("Reset Rotation: ", ResetRotation);

			sb.AppendValueEnum("Children Width: ", ChildrenWidth);
			sb.AppendValueEnum("Children Height: ", ChildrenHeight);

			sb.Append("Children: ");
			foreach (var el in elements)
			{
				sb.Append(el.Rect.name);
				sb.Append(": ");
				sb.Append(el.Rect.rect.size.ToString());
				sb.AppendLine();
				GetElementInfo(sb, el.Rect);
			}
		}

		List<UnityEngine.Component> components = new List<UnityEngine.Component>();

		void GetElementInfo(System.Text.StringBuilder sb, Transform element, string indent = "\t")
		{
			var next_indent = indent + "\t";
			sb.AppendValue(indent + "GameObject: ", element.name);
			if (element.transform is RectTransform rt)
			{
				sb.AppendValue(indent + " Size: ", rt.rect.size.ToString());
			}

			sb.AppendValue(indent + " Active: ", element.gameObject.activeSelf.ToString());

			GetComponentsInfo(sb, element, next_indent);

			for (var i = 0; i < element.childCount; i++)
			{
				GetElementInfo(sb, element.GetChild(i), next_indent);
			}
		}

		void GetComponentsInfo(System.Text.StringBuilder sb, Transform element, string indent)
		{
			var next_indent = indent + "\t";

			components.Clear();
			element.GetComponents(components);
			for (var i = 0; i < components.Count; i++)
			{
				var c = components[i];
				sb.AppendValue(string.Format("{0}{1}: ", indent, i.ToString()), c.GetType().FullName);
				if (c is ILayoutElement || c is ILayoutController || c is ILayoutIgnorer || c is RectTransform)
				{
					LayoutComponentInfo(sb, c, next_indent);
					LayoutElementInfo(sb, c, next_indent);
				}
			}

			components.Clear();
		}

		void LayoutElementInfo(System.Text.StringBuilder sb, UnityEngine.Component element, string indent)
		{
			if (element is ILayoutElement le)
			{
				sb.AppendLine(indent + "ILayoutElement:");
				sb.AppendValue(string.Format("{0}\tminWidth: ", indent), le.minWidth);
				sb.AppendValue(string.Format("{0}\tpreferredWidth: ", indent), le.preferredWidth);
				sb.AppendValue(string.Format("{0}\tflexibleWidth: ", indent), le.flexibleWidth);
				sb.AppendValue(string.Format("{0}\tminHeight: ", indent), le.minHeight);
				sb.AppendValue(string.Format("{0}\tpreferredHeight: ", indent), le.preferredHeight);
				sb.AppendValue(string.Format("{0}\tflexibleHeight: ", indent), le.flexibleHeight);
				sb.AppendValue(string.Format("{0}\tlayoutPriority: ", indent), le.layoutPriority);
			}

			if (element is ILayoutIgnorer li)
			{
				sb.AppendLine(indent + "ILayoutIgnorer:");
				sb.AppendValue(string.Format("{0}\tignoreLayout: ", indent), li.ignoreLayout);
			}
		}

		void LayoutComponentInfo(System.Text.StringBuilder sb, UnityEngine.Component element, string indent)
		{
			#if UNITY_EDITOR
			static string PropertyValue2String(UnityEditor.SerializedProperty property)
			{
				return property.propertyType switch
				{
					UnityEditor.SerializedPropertyType.Generic => "Unsupported property type: " + property.type,
					UnityEditor.SerializedPropertyType.Integer => property.intValue.ToString(),
					UnityEditor.SerializedPropertyType.Boolean => property.boolValue.ToString(),
					UnityEditor.SerializedPropertyType.Float => property.floatValue.ToString(),
					UnityEditor.SerializedPropertyType.String => property.stringValue,
					UnityEditor.SerializedPropertyType.Color => property.colorValue.ToString(),
					UnityEditor.SerializedPropertyType.ObjectReference => property.objectReferenceValue == null ? "null" : property.objectReferenceValue.ToString(),
					UnityEditor.SerializedPropertyType.LayerMask => ((LayerMask)property.intValue).ToString(),
					UnityEditor.SerializedPropertyType.Enum => property.intValue.ToString(),
					UnityEditor.SerializedPropertyType.Vector2 => property.vector2Value.ToString(),
					UnityEditor.SerializedPropertyType.Vector3 => property.vector3Value.ToString(),
					UnityEditor.SerializedPropertyType.Vector4 => property.vector4Value.ToString(),
					UnityEditor.SerializedPropertyType.Rect => property.rectValue.ToString(),
					UnityEditor.SerializedPropertyType.ArraySize => property.isArray ? property.arraySize.ToString() : "none",
					UnityEditor.SerializedPropertyType.Character => property.intValue.ToString(),
					UnityEditor.SerializedPropertyType.AnimationCurve => property.animationCurveValue == null ? "null" : property.animationCurveValue.ToString(),
					UnityEditor.SerializedPropertyType.Bounds => property.boundsValue.ToString(),
					UnityEditor.SerializedPropertyType.Gradient => "Gradient is unsupported",
					UnityEditor.SerializedPropertyType.Quaternion => property.quaternionValue.ToString(),
					UnityEditor.SerializedPropertyType.ExposedReference => property.exposedReferenceValue == null ? "null" : property.exposedReferenceValue.ToString(),
					UnityEditor.SerializedPropertyType.FixedBufferSize => property.fixedBufferSize.ToString(),
					UnityEditor.SerializedPropertyType.Vector2Int => property.vector2IntValue.ToString(),
					UnityEditor.SerializedPropertyType.Vector3Int => property.vector3IntValue.ToString(),
					UnityEditor.SerializedPropertyType.RectInt => property.rectIntValue.ToString(),
					UnityEditor.SerializedPropertyType.BoundsInt => property.boundsIntValue.ToString(),
					UnityEditor.SerializedPropertyType.ManagedReference => property.managedReferenceFullTypename,
					_ => "Unsupported property type",
				};
			}

			var so = new UnityEditor.SerializedObject(element);
			var property = so.GetIterator();
			property.NextVisible(true);
			while (property.NextVisible(true))
			{
				sb.AppendValue(string.Format("{0}{1} {2}: ", indent, property.propertyType, property.propertyPath), PropertyValue2String(property));
			}
			#else
			sb.AppendValue(indent + "Serialized data is not available in runtime.", string.Empty);
			#endif
		}
	}
}