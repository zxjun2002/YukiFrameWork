namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using EasyLayoutNS.Extensions;
	using UnityEngine;

	/// <summary>
	/// Settings for the staggered layout.
	/// </summary>
	[Serializable]
	public class EasyLayoutEllipseSettings : IObservable, INotifyPropertyChanged
	{
		[SerializeField]
		private bool widthAuto = true;

		/// <summary>
		/// Calculate with or not.
		/// </summary>
		public bool WidthAuto
		{
			get => widthAuto;

			set => Change(ref widthAuto, value, nameof(WidthAuto));
		}

		[SerializeField]
		private float width;

		/// <summary>
		/// Width.
		/// </summary>
		public float Width
		{
			get => width;

			set => Change(ref width, value, nameof(Width));
		}

		[SerializeField]
		private bool heightAuto = true;

		/// <summary>
		/// Calculate height or not.
		/// </summary>
		public bool HeightAuto
		{
			get => heightAuto;

			set => Change(ref heightAuto, value, nameof(HeightAuto));
		}

		[SerializeField]
		private float height;

		/// <summary>
		/// Height.
		/// </summary>
		public float Height
		{
			get => height;

			set => Change(ref height, value, nameof(Height));
		}

		[SerializeField]
		private float angleStart;

		/// <summary>
		/// Angle for the display first element.
		/// </summary>
		public float AngleStart
		{
			get => angleStart;

			set => Change(ref angleStart, value, nameof(AngleStart));
		}

		[SerializeField]
		private bool angleStepAuto;

		/// <summary>
		/// Calculate or not AngleStep.
		/// </summary>
		public bool AngleStepAuto
		{
			get => angleStepAuto;

			set => Change(ref angleStepAuto, value, nameof(AngleStepAuto));
		}

		[SerializeField]
		private float angleStep = 20f;

		/// <summary>
		/// Angle distance between elements.
		/// </summary>
		public float AngleStep
		{
			get => angleStep;

			set => Change(ref angleStep, value, nameof(AngleStep));
		}

		[SerializeField]
		private EllipseFill fill = EllipseFill.Closed;

		/// <summary>
		/// Fill type.
		/// </summary>
		public EllipseFill Fill
		{
			get => fill;

			set => Change(ref fill, value, nameof(Fill));
		}

		[SerializeField]
		private float arcLength = 360f;

		/// <summary>
		/// Arc length.
		/// </summary>
		public float ArcLength
		{
			get => arcLength;

			set => Change(ref arcLength, value, nameof(ArcLength));
		}

		[SerializeField]
		private EllipseAlign align;

		/// <summary>
		/// Align.
		/// </summary>
		public EllipseAlign Align
		{
			get => align;

			set => Change(ref align, value, nameof(Align));
		}

		[SerializeField]
		[HideInInspector]
		private float angleScroll;

		/// <summary>
		/// Angle padding.
		/// </summary>
		public float AngleScroll
		{
			get => angleScroll;

			set => Change(ref angleScroll, value, nameof(AngleScroll));
		}

		[SerializeField]
		[HideInInspector]
		private float angleFiller;

		/// <summary>
		/// Angle filler.
		/// </summary>
		public float AngleFiller
		{
			get => angleFiller;

			set => Change(ref angleFiller, value, nameof(AngleFiller));
		}

		[SerializeField]
		private bool elementsRotate = true;

		/// <summary>
		/// Rotate elements.
		/// </summary>
		public bool ElementsRotate
		{
			get => elementsRotate;

			set => Change(ref elementsRotate, value, nameof(ElementsRotate));
		}

		[SerializeField]
		private float elementsRotationStart;

		/// <summary>
		/// Start rotation for elements.
		/// </summary>
		public float ElementsRotationStart
		{
			get => elementsRotationStart;

			set => Change(ref elementsRotationStart, value, nameof(ElementsRotationStart));
		}

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event OnChange OnChange;

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Change value.
		/// </summary>
		/// <typeparam name="T">Type of field.</typeparam>
		/// <param name="field">Field value.</param>
		/// <param name="value">New value.</param>
		/// <param name="propertyName">Property name.</param>
		protected void Change<T>(ref T field, T value, string propertyName)
		{
			if (!EqualityComparer<T>.Default.Equals(field, value))
			{
				field = value;
				NotifyPropertyChanged(propertyName);
			}
		}

		/// <summary>
		/// Property changed.
		/// </summary>
		/// <param name="propertyName">Property name.</param>
		protected void NotifyPropertyChanged(string propertyName)
		{
			OnChange?.Invoke();

			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		/// Get debug information.
		/// </summary>
		/// <param name="sb">String builder.</param>
		public virtual void GetDebugInfo(System.Text.StringBuilder sb)
		{
			sb.AppendValue("\tWidth Auto: ", WidthAuto);
			sb.AppendValue("\tWidth: ", Width);
			sb.AppendValue("\tHeight Auto: ", HeightAuto);
			sb.AppendValue("\tHeight: ", Height);

			sb.AppendValue("\tAngle Start: ", AngleStart);
			sb.AppendValue("\tAngle Step Auto: ", AngleStepAuto);
			sb.AppendValue("\tAngle Step: ", AngleStep);
			sb.AppendValueEnum("\tAlign: ", Align);
			sb.AppendValue("\tElements Rotate: ", ElementsRotate);
			sb.AppendValue("\tElements Rotation Start: ", ElementsRotationStart);

			sb.AppendLine("\t#####");

			sb.AppendValueEnum("\tFill: ", Fill);
			sb.AppendValue("\tAngle Filler: ", AngleFiller);
			sb.AppendValue("\tAngle Scroll: ", AngleScroll);
			sb.AppendValue("\tArc Length: ", ArcLength);
		}
	}
}