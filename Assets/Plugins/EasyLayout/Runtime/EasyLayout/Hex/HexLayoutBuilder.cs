namespace EasyLayoutNS
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Serialization;

	/// <summary>
	/// Hex layout builder.
	/// </summary>
	[RequireComponent(typeof(EasyLayout))]
	[HelpURL("https://ilih.name/unity-assets/EasyLayout/docs/hex-layout-builder.html")]
	public class HexLayoutBuilder : MonoBehaviour
	{
		/// <summary>
		/// Block.
		/// If flat top then block describe cells in column.
		/// If pointy top then block describe cells in row.
		/// </summary>
		[Serializable]
		public struct Block
		{
			[SerializeField]
			int start;

			/// <summary>
			/// Index of the first cell.
			/// </summary>
			public readonly int Start => start;

			[SerializeField]
			[FormerlySerializedAs("items")]
			int cells;

			/// <summary>
			/// Cells in block.
			/// </summary>
			public readonly int Cells => cells;

			/// <summary>
			/// End index (not inclusive).
			/// </summary>
			public readonly int End => start + cells;

			/// <summary>
			/// Initializes a new instance of the <see cref="Block"/> struct.
			/// </summary>
			/// <param name="start">Start.</param>
			/// <param name="cells">Cells.</param>
			public Block(int start, int cells)
			{
				this.start = start;
				this.cells = cells;
			}
		}

		/// <summary>
		/// Template with FlatTop.
		/// </summary>
		[SerializeField]
		public HexCoordinates TemplateFlatTop;

		/// <summary>
		/// Template with PointyTop.
		/// </summary>
		[SerializeField]
		public HexCoordinates TemplatePointyTop;

		/// <summary>
		/// Blocks.
		/// </summary>
		[SerializeField]
		[Tooltip("If the EasyLayout.Orientation is FlatTop then each block describes a column.\r\nOtherwise, each block describes a row.")]
		public List<Block> Blocks = new List<Block>();

		EasyLayout layout;

		/// <summary>
		/// Layout.
		/// </summary>
		protected EasyLayout Layout
		{
			get
			{
				if (layout == null)
				{
					TryGetComponent<EasyLayout>(out layout);
				}

				return layout;
			}
		}

		/// <summary>
		/// Pool.
		/// </summary>
		protected HexPool Pool;

		/// <summary>
		/// Instances.
		/// </summary>
		public IReadOnlyDictionary<CubeCoordinates, HexCoordinates> Instances => Pool.Instances;

		/// <summary>
		/// Layout orientation is flat top?
		/// </summary>
		public bool IsFlatTop => Layout.HexSettings.Orientation == EasyLayoutHexSettings.OrientationMode.FlatTop;

		bool isInited;

		/// <summary>
		/// Process the start event.
		/// </summary>
		protected virtual void Start()
		{
			Init();
		}

		/// <summary>
		/// Init this instance.
		/// </summary>
		public virtual void Init()
		{
			if (isInited)
			{
				return;
			}

			isInited = true;

			if (TemplateFlatTop != null)
			{
				TemplateFlatTop.gameObject.SetActive(false);
			}

			if (TemplatePointyTop != null)
			{
				TemplatePointyTop.gameObject.SetActive(false);
			}

			Pool = new HexPool(transform);
			CreateGrid();
		}

		/// <summary>
		/// Create grid.
		/// </summary>
		public virtual void CreateGrid()
		{
			CreateGrid(IsFlatTop ? TemplateFlatTop : TemplatePointyTop, Blocks);
		}

		/// <summary>
		/// Create grid.
		/// </summary>
		/// <param name="template">Template.</param>
		/// <param name="blocks">Blocks.</param>
		public virtual void CreateGrid(HexCoordinates template, IList<Block> blocks)
		{
			if (Layout == null)
			{
				Debug.LogError("HexLayout requires EasyLayout component.", this);
				return;
			}

			var is_flat_top = IsFlatTop;
			Pool.SetTemplate(template);

			for (var block_index = 0; block_index < blocks.Count; block_index++)
			{
				var data = blocks[block_index];
				for (var cell_index = data.Start; cell_index < data.End; cell_index++)
				{
					var coordinates = is_flat_top
						? new OffsetCoordinates(cell_index, block_index)
						: new OffsetCoordinates(block_index, cell_index);
					_ = Pool.GetInstance(coordinates);
				}
			}

			Layout.HexSettings.Coordinates = EasyLayoutHexSettings.CoordinatesMode.Read;
			Layout.LayoutType = LayoutTypes.Hex;
			Layout.UpdateLayout();
		}
	}
}