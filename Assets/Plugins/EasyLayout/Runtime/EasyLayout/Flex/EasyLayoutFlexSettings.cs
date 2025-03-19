namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using EasyLayoutNS.Extensions;
	using UnityEngine;
	using UnityEngine.Serialization;

	/// <summary>
	/// Flex settings.
	/// </summary>
	[Serializable]
	public class EasyLayoutFlexSettings : IObservable, INotifyPropertyChanged
	{
		/// <summary>
		/// Content positions.
		/// </summary>
		[Serializable]
		public enum Content
		{
			/// <summary>
			/// Position at the start of the block.
			/// </summary>
			Start = 0,

			/// <summary>
			/// Position at the center of the block.
			/// </summary>
			Center = 1,

			/// <summary>
			/// Position at the end of the block.
			/// </summary>
			End = 2,

			/// <summary>
			/// Position with space between.
			/// </summary>
			SpaceBetween = 3,

			/// <summary>
			/// Position with space around.
			/// </summary>
			SpaceAround = 4,

			/// <summary>
			/// Position with space evenly.
			/// </summary>
			SpaceEvenly = 5,
		}

		/// <summary>
		/// Items align.
		/// </summary>
		[Serializable]
		public enum Items
		{
			/// <summary>
			/// Start position.
			/// </summary>
			Start = 0,

			/// <summary>
			/// Center position.
			/// </summary>
			Center = 1,

			/// <summary>
			/// End position.
			/// </summary>
			End = 2,
		}

		[SerializeField]
		[FormerlySerializedAs("Wrap")]
		bool wrap = true;

		/// <summary>
		/// Wrap.
		/// </summary>
		public bool Wrap
		{
			get => wrap;

			set => Change(ref wrap, value, nameof(Wrap));
		}

		[SerializeField]
		[FormerlySerializedAs("JustifyContent")]
		Content justifyContent = Content.Start;

		/// <summary>
		/// Elements positions at the main axis.
		/// </summary>
		public Content JustifyContent
		{
			get => justifyContent;

			set => Change(ref justifyContent, value, nameof(JustifyContent));
		}

		[SerializeField]
		[FormerlySerializedAs("AlignContent")]
		Content alignContent = Content.Start;

		/// <summary>
		/// Elements positions at the sub axis.
		/// </summary>
		public Content AlignContent
		{
			get => alignContent;

			set => Change(ref alignContent, value, nameof(AlignContent));
		}

		[SerializeField]
		[FormerlySerializedAs("AlignItems")]
		Items alignItems = Items.Start;

		/// <summary>
		/// Items align.
		/// </summary>
		public Items AlignItems
		{
			get => alignItems;

			set => Change(ref alignItems, value, nameof(AlignItems));
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
			sb.AppendValue("\tWrap: ", Wrap);
			sb.AppendValueEnum("\tJustify Content: ", JustifyContent);
			sb.AppendValueEnum("\tAlign Content: ", AlignContent);
			sb.AppendValueEnum("\tAlign Items: ", AlignItems);
		}
	}
}