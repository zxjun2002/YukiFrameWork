namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using EasyLayoutNS.Extensions;
	using UnityEngine;

	/// <summary>
	/// Hex settings.
	/// </summary>
	[Serializable]
	public class EasyLayoutHexSettings : IObservable, INotifyPropertyChanged
	{
		/// <summary>
		/// Content positions.
		/// </summary>
		[Serializable]
		public enum OrientationMode
		{
			/// <summary>
			/// Flat-top orientation.
			/// </summary>
			FlatTop = 0,

			/// <summary>
			/// Pointy-top orientation.
			/// </summary>
			PointyTop = 1,
		}

		/// <summary>
		/// Coordinates modes.
		/// </summary>
		[Serializable]
		public enum CoordinatesMode
		{
			/// <summary>
			/// Read coordinates from the HexCoordinates component.
			/// </summary>
			Read = 0,

			/// <summary>
			/// Write coordinates to the HexCoordinates component.
			/// </summary>
			Write = 1,
		}

		/// <summary>
		/// Hex constraints.
		/// </summary>
		[Serializable]
		public enum HexConstraints
		{
			/// <summary>
			/// Don't constraint the number of rows or columns.
			/// </summary>
			Flexible = 0,

			/// <summary>
			/// Constraint the number of columns to a specified number.
			/// </summary>
			FixedColumnCount = 1,

			/// <summary>
			/// Constraint the number of rows to a specified number.
			/// </summary>
			FixedRowCount = 2,

			/// <summary>
			/// Constraint the cells per row to a specified number.
			/// </summary>
			CellsPerRow = 3,

			/// <summary>
			/// Constraint the cells per column to a specified number.
			/// </summary>
			CellsPerColumn = 4,
		}

		[SerializeField]
		OrientationMode orientation = OrientationMode.FlatTop;

		/// <summary>
		/// Orientation.
		/// </summary>
		public OrientationMode Orientation
		{
			get => orientation;

			set => Change(ref orientation, value, nameof(Orientation));
		}

		[SerializeField]
		[Tooltip("Read coordinates from the HexCoordinates component in the children.\nOr group components and write coordinates to the HexCoordinates component.")]
		CoordinatesMode coordinates = CoordinatesMode.Write;

		/// <summary>
		/// Load coordinates.
		/// </summary>
		public CoordinatesMode Coordinates
		{
			get => coordinates;

			set => Change(ref coordinates, value, nameof(Coordinates));
		}

		[SerializeField]
		bool shovesOdd = true;

		/// <summary>
		/// Shoves odd.
		/// </summary>
		public bool ShovesOdd
		{
			get => shovesOdd;

			set => Change(ref shovesOdd, value, nameof(ShovesOdd));
		}

		[SerializeField]
		HexConstraints constraint = HexConstraints.Flexible;

		/// <summary>
		/// Constraint type.
		/// </summary>
		public HexConstraints Constraint
		{
			get => constraint;

			set => Change(ref constraint, value, nameof(Constraint));
		}

		[SerializeField]
		int constraintCount = 1;

		/// <summary>
		/// How many elements there should be along the constrained axis.
		/// </summary>
		public int ConstraintCount
		{
			get => Mathf.Max(1, constraintCount);

			set => Change(ref constraintCount, value, nameof(ConstraintCount));
		}

		[SerializeField]
		[Tooltip("Shoved rows or columns will have 1 cell less than the specified constraint.")]
		bool decreaseShoved = false;

		/// <summary>
		/// Shoved rows or columns will have 1 cell less than the specified constraint.
		/// </summary>
		public bool DecreaseShoved
		{
			get => decreaseShoved && (ConstraintCount > 1);

			set => Change(ref decreaseShoved, value, nameof(DecreaseShoved));
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
			sb.AppendValueEnum("\tOrientation: ", Orientation);
			sb.AppendValue("\tShovesOdd: ", ShovesOdd);
			sb.AppendValueEnum("\tHexConstraint: ", Constraint);
			sb.AppendValue("\tConstraintCount: ", ConstraintCount);
		}
	}
}