using UnityEngine;
using System;
using System.Collections;

namespace EnhancedUI.EnhancedScroller
{
    /// <summary>
    /// 所有数据类型的基类，提供通用结构
    /// </summary>
    public abstract class BaseCellData
    {
        /// <summary>
        /// 计算单元格高度，子类可覆盖
        /// 返回 `-1` 代表使用默认高度（由预制体决定）
        /// </summary>
        public virtual float CalculateHeight()
        {
            return -1f; // -1 代表让 EnhancedScrollerController 读取预制体高度
        }
    }
    
    /// <summary>
    /// This is the base class that all cell views should derive from
    /// </summary>
    public class EnhancedScrollerCellView : MonoBehaviour
    {
        /// <summary>
        /// The cellIdentifier is a unique string that allows the scroller
        /// to handle different types of cells in a single list. Each type
        /// of cell should have its own identifier
        /// </summary>
        public string cellIdentifier;

        /// <summary>
        /// The cell index of the cell view
        /// This will differ from the dataIndex if the list is looping
        /// </summary>
        [NonSerialized]
        public int cellIndex;

        /// <summary>
        /// The data index of the cell view
        /// </summary>
        [NonSerialized]
        public int dataIndex;

        /// <summary>
        /// Whether the cell is active or recycled
        /// </summary>
        [NonSerialized]
        public bool active;

        /// <summary>
        /// This method is called by the scroller when the RefreshActiveCellViews is called on the scroller
        /// You can override it to update your cell's view UID
        /// </summary>
        public virtual void RefreshCellView() { }
        
        public virtual void SetData(BaseCellData data) { }
    }
}