namespace EasyLayoutNS
{
	using System.Collections.Generic;
	using UnityEngine;

	/// <summary>
	/// Size.
	/// </summary>
	public struct Size : System.IEquatable<Size>
	{
		/// <summary>
		/// Minimum size.
		/// </summary>
		public float Min;

		/// <summary>
		/// Preferred size.
		/// </summary>
		public float Preferred;

		/// <summary>
		/// Flexible size.
		/// </summary>
		public float Flexible;

		/// <summary>
		/// Initializes a new instance of the <see cref="Size"/> struct.
		/// </summary>
		/// <param name="min">Minimum size.</param>
		/// <param name="preferred">Preferred size.</param>
		/// <param name="flexible">Flexible size.</param>
		public Size(float min, float preferred, float flexible = 0f)
		{
			Min = min;
			Preferred = preferred;
			Flexible = flexible;
		}

		/// <summary>
		/// Serves as a hash function for a EasyLayout.Size object.
		/// </summary>
		/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
		public override int GetHashCode() => Min.GetHashCode() ^ Preferred.GetHashCode() ^ Flexible.GetHashCode();

		/// <summary>
		/// Determines whether the specified System.Object is equal to the current EasyLayout.Size.
		/// </summary>
		/// <param name="obj">The System.Object to compare with the current EasyLayout.Size.</param>
		/// <returns><c>true</c> if the specified System.Object is equal to the current EasyLayout.Size;
		/// otherwise, <c>false</c>.</returns>
		public readonly override bool Equals(object obj) => (obj is Size size) && Equals(size);

		/// <summary>
		/// Determines whether the specified EasyLayout.Size is equal to the current EasyLayout.Size.
		/// </summary>
		/// <param name="other">The EasyLayout.Size to compare with the current EasyLayout.Size.</param>
		/// <returns><c>true</c> if the specified EasyLayout.Size is equal to the current EasyLayout.Size;
		/// otherwise, <c>false</c>.</returns>
		public readonly bool Equals(Size other) => (Min == other.Min) && (Preferred == other.Preferred) && (Flexible == other.Flexible);

		/// <summary>
		/// Compare sizes.
		/// </summary>
		/// <param name="size1">First size.</param>
		/// <param name="size2">Seconds size.</param>
		/// <returns>True if sizes are equals; otherwise false.</returns>
		public static bool operator ==(Size size1, Size size2) => size1.Equals(size2);

		/// <summary>
		/// Compare sizes.
		/// </summary>
		/// <param name="size1">First size.</param>
		/// <param name="size2">Seconds size.</param>
		/// <returns>True if sizes are not equals; otherwise false.</returns>
		public static bool operator !=(Size size1, Size size2) => !size1.Equals(size2);

		/// <summary>
		/// Get the maximum widths.
		/// </summary>
		/// <returns>The maximum widths.</returns>
		/// <param name="elems">Elements.</param>
		public static Size MaxWidths(List<LayoutElementInfo> elems)
		{
			var min = 0f;
			var preferred = 0f;
			var flexible = 0f;

			foreach (var elem in elems)
			{
				min = Mathf.Max(min, elem.MinWidth);
				preferred = Mathf.Max(preferred, elem.PreferredWidth);
				flexible = Mathf.Max(flexible, elem.FlexibleWidth);
			}

			return new Size(min, preferred, flexible);
		}

		/// <summary>
		/// Get the maximum heights.
		/// </summary>
		/// <returns>The maximum heights.</returns>
		/// <param name="elems">Elements.</param>
		public static Size MaxHeights(List<LayoutElementInfo> elems)
		{
			var min = 0f;
			var preferred = 0f;
			var flexible = 0f;

			foreach (var elem in elems)
			{
				min = Mathf.Max(min, elem.MinHeight);
				preferred = Mathf.Max(preferred, elem.PreferredHeight);
				flexible = Mathf.Max(flexible, elem.FlexibleHeight);
			}

			return new Size(min, preferred, flexible);
		}
	}
}