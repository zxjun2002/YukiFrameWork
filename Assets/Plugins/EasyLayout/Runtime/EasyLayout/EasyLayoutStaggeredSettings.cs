namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using EasyLayoutNS.Extensions;
	using UnityEngine;
	using UnityEngine.Serialization;

	/// <summary>
	/// Settings for the staggered layout.
	/// </summary>
	[Serializable]
	public class EasyLayoutStaggeredSettings : IObservable, INotifyPropertyChanged
	{
		[SerializeField]
		[FormerlySerializedAs("FixedBlocksCount")]
		[Tooltip("Layout with fixed amount of blocks (row or columns) instead of the flexible.")]
		private bool fixedBlocksCount;

		/// <summary>
		/// Use fixed blocks count.
		/// </summary>
		public bool FixedBlocksCount
		{
			get
			{
				return fixedBlocksCount;
			}

			set
			{
				Change(ref fixedBlocksCount, value, "FixedBlocksCount");
			}
		}

		[SerializeField]
		[FormerlySerializedAs("BlocksCount")]
		private int blocksCount = 1;

		/// <summary>
		/// Block (row or columns) count.
		/// </summary>
		public int BlocksCount
		{
			get
			{
				return blocksCount;
			}

			set
			{
				Change(ref blocksCount, value, "BlocksCount");
			}
		}

		/// <summary>
		/// PaddingInner at the start of the blocks.
		/// Used by ListViewCustom to simulate the space occupied by non-displayable elements.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		public List<float> PaddingInnerStart = new List<float>();

		/// <summary>
		/// PaddingInner at the end of the blocks.
		/// Used by ListViewCustom to simulate the space occupied by non-displayable elements.
		/// </summary>
		[SerializeField]
		[HideInInspector]
		public List<float> PaddingInnerEnd = new List<float>();

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
			sb.AppendValue("\tFixed Blocks Count: ", FixedBlocksCount);
			sb.AppendValue("\tBlocks Count: ", BlocksCount);
			sb.AppendLine("\t#####");
			sb.AppendValue("\tPadding Inner Start: ", EasyLayoutUtilities.List2String(PaddingInnerStart));
			sb.AppendValue("\tPadding Inner End: ", EasyLayoutUtilities.List2String(PaddingInnerEnd));
		}
	}
}