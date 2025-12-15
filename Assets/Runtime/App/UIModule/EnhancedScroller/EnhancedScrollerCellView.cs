using UnityEngine;
using System;
using MIKUFramework.IOC;

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
        public virtual float CalculateSize()
        {
            return -1f; // -1 代表让 EnhancedScrollerController 读取预制体尺寸
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

        // ========== 新增：生命周期状态 ==========
        private bool _inited;
        private bool _isShowing;

        /// <summary>
        /// 对外：保证 Init 只调用一次
        /// </summary>
        public void EnsureInit()
        {
            if (_inited) return;
            _inited = true;
            OnInit();
        }

        /// <summary>
        /// 对外：进入显示阶段（对应 OnShow）
        /// </summary>
        public void EnterShow()
        {
            if (_isShowing) return;
            _isShowing = true;
            OnShow();
        }

        /// <summary>
        /// 对外：退出显示阶段（对应 OnHide）
        /// </summary>
        public void ExitShow()
        {
            if (!_isShowing) return;
            _isShowing = false;
            OnHide();
        }

        /// <summary>
        /// 回收后重置显示状态
        /// </summary>
        public void ResetShowState()
        {
            _isShowing = false;
        }

        // ========== 生命周期虚方法，子类重写 ==========

        /// <summary>
        /// 只会被调用一次：做 IoC 注入、找组件、缓存引用等
        /// </summary>
        protected virtual void OnInit()
        {
            IoCHelper.Instance.Inject(this);
        }

        /// <summary>
        /// 每次从池子里“显示出来”时调用：注册事件、开启动画等
        /// </summary>
        protected virtual void OnShow() { }

        /// <summary>
        /// 每次被回收到池子前调用：解绑事件、停止动画等
        /// </summary>
        protected virtual void OnHide() { }

        /// <summary>
        /// This method is called by the scroller when the RefreshActiveCellViews is called on the scroller
        /// You can override it to update your cell's view UID
        /// </summary>
        public virtual void RefreshCellView() { }

        /// <summary>
        /// 设置数据（具体显示内容）
        /// </summary>
        public virtual void SetData(BaseCellData data) { }

        /// <summary>
        /// 计算单元格高度，子类可覆盖
        /// 返回 `-1` 代表使用默认高度（由预制体决定）
        /// </summary>
        public virtual float CalculateSize(BaseCellData data)
        {
            return -1f; // -1 代表让 EnhancedScrollerController 读取预制体尺寸
        }
    }
}
