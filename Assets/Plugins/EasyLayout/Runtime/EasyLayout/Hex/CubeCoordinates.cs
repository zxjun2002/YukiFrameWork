namespace EasyLayoutNS
{
	using System;

	/// <summary>
	/// Cube coordinates.
	/// Algorithms are more easier than offset coordinates.
	/// </summary>
	public struct CubeCoordinates : IEquatable<CubeCoordinates>
	{
		/// <summary>
		/// S axis coordinate.
		/// </summary>
		public int S;

		/// <summary>
		/// Q axis coordinate.
		/// </summary>
		public int Q;

		/// <summary>
		/// R axis coordinate.
		/// </summary>
		public int R;

		/// <summary>
		/// Initializes a new instance of the <see cref="CubeCoordinates"/> struct.
		/// </summary>
		/// <param name="row">Row.</param>
		/// <param name="column">Column.</param>
		/// <param name="settings">Settings.</param>
		public CubeCoordinates(int row, int column, EasyLayoutHexSettings settings)
		{
			if (settings.Orientation == EasyLayoutHexSettings.OrientationMode.PointyTop)
			{
				Q = settings.ShovesOdd
					? column - ((row - (row & 1)) / 2)
					: column - ((row + (row & 1)) / 2);
				R = row;
			}
			else
			{
				Q = column;
				R = settings.ShovesOdd
					? row - ((column - (column & 1)) / 2)
					: row - ((column + (column & 1)) / 2);
			}

			S = -Q - R;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CubeCoordinates"/> struct.
		/// </summary>
		/// <param name="offset">Offset coordinates.</param>
		/// <param name="settings">Layout settings.</param>
		public CubeCoordinates(OffsetCoordinates offset, EasyLayoutHexSettings settings)
			: this(offset.Row, offset.Column, settings)
		{
		}

		/// <summary>
		/// Convert this instance to string.
		/// </summary>
		/// <returns>String representation.</returns>
		public override string ToString() => string.Format("S: {0}; Q: {1}; R: {2}", S, Q, R);

		/// <summary>
		/// Convert to offset coordinates.
		/// </summary>
		/// <param name="settings">Layout settings.</param>
		/// <returns>Offset coordinates.</returns>
		public OffsetCoordinates ToOffset(EasyLayoutHexSettings settings) => new OffsetCoordinates(this, settings);

		/// <summary>
		/// Create instance from offset coordinates.
		/// </summary>
		/// <param name="offset">Offset coordinates.</param>
		/// <param name="settings">Layout settings.</param>
		/// <returns>Cube coordinates.</returns>
		public static CubeCoordinates FromOffset(OffsetCoordinates offset, EasyLayoutHexSettings settings) => new CubeCoordinates(offset, settings);

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
		public readonly override bool Equals(object obj) => (obj is CubeCoordinates c) && Equals(c);

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="other">The object to compare with the current object.</param>
		/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
		public readonly bool Equals(CubeCoordinates other) => S == other.S && R == other.R && Q == other.Q;

		/// <summary>
		/// Hash function.
		/// </summary>
		/// <returns>A hash code for the current object.</returns>
		public readonly override int GetHashCode() => S ^ Q ^ R;

		/// <summary>
		/// Compare specified objects.
		/// </summary>
		/// <param name="a">First object.</param>
		/// <param name="b">Second object.</param>
		/// <returns>true if the objects are equal; otherwise, false.</returns>
		public static bool operator ==(CubeCoordinates a, CubeCoordinates b) => a.Equals(b);

		/// <summary>
		/// Compare specified objects.
		/// </summary>
		/// <param name="a">First object.</param>
		/// <param name="b">Second object.</param>
		/// <returns>true if the objects are not equal; otherwise, false.</returns>
		public static bool operator !=(CubeCoordinates a, CubeCoordinates b) => !a.Equals(b);
	}
}