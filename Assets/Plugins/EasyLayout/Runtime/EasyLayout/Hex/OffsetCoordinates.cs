namespace EasyLayoutNS
{
	using System;

	/// <summary>
	/// Offset coordinates.
	/// Simple, but algorithms are more difficult.
	/// </summary>
	public struct OffsetCoordinates : IEquatable<OffsetCoordinates>
	{
		/// <summary>
		/// Row.
		/// </summary>
		public int Row;

		/// <summary>
		/// Column.
		/// </summary>
		public int Column;

		/// <summary>
		/// Initializes a new instance of the <see cref="OffsetCoordinates"/> struct.
		/// </summary>
		/// <param name="row">Row.</param>
		/// <param name="column">Column.</param>
		public OffsetCoordinates(int row, int column)
		{
			Row = row;
			Column = column;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OffsetCoordinates"/> struct.
		/// </summary>
		/// <param name="cube">Cube coordinates.</param>
		/// <param name="settings">Layout settings.</param>
		public OffsetCoordinates(CubeCoordinates cube, EasyLayoutHexSettings settings)
		{
			if (settings.Orientation == EasyLayoutHexSettings.OrientationMode.PointyTop)
			{
				Column = settings.ShovesOdd
					? cube.Q + ((cube.R - (cube.R & 1)) / 2)
					: cube.Q + ((cube.R + (cube.R & 1)) / 2);
				Row = cube.R;
			}
			else
			{
				Column = cube.Q;
				Row = settings.ShovesOdd
					? cube.R + ((cube.Q - (cube.Q & 1)) / 2)
					: cube.R + ((cube.Q + (cube.Q & 1)) / 2);
			}
		}

		/// <summary>
		/// Convert this instance to string.
		/// </summary>
		/// <returns>String representation.</returns>
		public override readonly string ToString() => string.Format("{0}x{1}", Column, Row);

		/// <summary>
		/// Convert this instance to cube coordinates.
		/// </summary>
		/// <param name="settings">Layout settings.</param>
		/// <returns>Cube coordinates.</returns>
		public readonly CubeCoordinates ToCube(EasyLayoutHexSettings settings) => new CubeCoordinates(this, settings);

		/// <summary>
		/// Convert cube coordinates to offset.
		/// </summary>
		/// <param name="cube">Cube coordinates.</param>
		/// <param name="settings">Layout settings.</param>
		/// <returns>Offset coordinates.</returns>
		public static OffsetCoordinates FromCube(CubeCoordinates cube, EasyLayoutHexSettings settings) => new OffsetCoordinates(cube, settings);

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
		public readonly override bool Equals(object obj) => (obj is OffsetCoordinates c) && Equals(c);

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="other">The object to compare with the current object.</param>
		/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
		public readonly bool Equals(OffsetCoordinates other) => Row == other.Row && Column == other.Column;

		/// <summary>
		/// Hash function.
		/// </summary>
		/// <returns>A hash code for the current object.</returns>
		public readonly override int GetHashCode() => Row ^ Column;

		/// <summary>
		/// Compare specified objects.
		/// </summary>
		/// <param name="a">First object.</param>
		/// <param name="b">Second object.</param>
		/// <returns>true if the objects are equal; otherwise, false.</returns>
		public static bool operator ==(OffsetCoordinates a, OffsetCoordinates b) => a.Equals(b);

		/// <summary>
		/// Compare specified objects.
		/// </summary>
		/// <param name="a">First object.</param>
		/// <param name="b">Second object.</param>
		/// <returns>true if the objects are not equal; otherwise, false.</returns>
		public static bool operator !=(OffsetCoordinates a, OffsetCoordinates b) => !a.Equals(b);
	}
}