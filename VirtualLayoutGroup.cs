using System;
using System.Collections.Generic;
using GameUtil;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InfinityScroll
{
    public abstract class VirtualLayoutGroup : UIBehaviour, ILayoutElement, ILayoutGroup
    {
        [SerializeField] protected RectOffset m_Padding = new RectOffset();
        [SerializeField] protected TextAnchor m_ChildAlignment = TextAnchor.UpperLeft;
        private Vector2 m_TotalMinSize = Vector2.zero;
        private Vector2 m_TotalPreferredSize = Vector2.zero;
        private Vector2 m_TotalFlexibleSize = Vector2.zero;
        [NonSerialized] private List<VirtualChild> m_Children = new List<VirtualChild>();
        [NonSerialized] private RectTransform m_Rect;
        protected DrivenRectTransformTracker m_Tracker;


        private Action<int, RectTransform> onChildUnbind;
        protected Action onLayoutEnd;
        private Vector3[] corners;

        //
        // Properties
        //
        public TextAnchor childAlignment
        {
            get { return m_ChildAlignment; }
            set { SetProperty<TextAnchor>(ref m_ChildAlignment, value); }
        }

        public int childCount
        {
            get { return (m_Children != null) ? m_Children.Count : 0; }
        }

        protected List<VirtualChild> children
        {
            get { return m_Children; }
        }

        public virtual float flexibleHeight
        {
            get { return GetTotalFlexibleSize(1); }
        }

        public virtual float flexibleWidth
        {
            get { return GetTotalFlexibleSize(0); }
        }

        private bool isRootLayoutGroup
        {
            get
            {
                Transform parent = base.transform.parent;
                return parent == null || base.transform.parent.GetComponent(typeof(ILayoutGroup)) == null;
            }
        }

        public virtual int layoutPriority
        {
            get { return 0; }
        }

        public virtual float minHeight
        {
            get { return GetTotalMinSize(1); }
        }

        public virtual float minWidth
        {
            get { return GetTotalMinSize(0); }
        }

        public RectOffset padding
        {
            get { return m_Padding; }
            set { SetProperty<RectOffset>(ref m_Padding, value); }
        }

        public virtual float preferredHeight
        {
            get { return GetTotalPreferredSize(1); }
        }

        public virtual float preferredWidth
        {
            get { return GetTotalPreferredSize(0); }
        }

        protected RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                {
                    m_Rect = GetComponent<RectTransform>();
                }

                return m_Rect;
            }
        }

        //
        // Constructors
        //
        protected VirtualLayoutGroup()
        {
            if (m_Padding == null)
            {
                m_Padding = new RectOffset();
            }
        }

        //
        // Methods
        //
        public void BindChild(int idx, RectTransform rect)
        {
            if (idx < 0 || idx >= m_Children.Count)
            {
                Debug.LogError(string.Concat(new object[]
                {
                    "bindchild out of bound idx:",
                    idx,
                    ",len:",
                    m_Children.Count
                }));
                return;
            }

            if (m_Children[idx].rectTransform != null && m_Children[idx].rectTransform != rect && onChildUnbind != null)
            {
                onChildUnbind(idx, m_Children[idx].rectTransform);
            }

            m_Children[idx].rectTransform = rect;
            SetChildAlongAxis(m_Children[idx], 0, m_Children[idx].x, m_Children[idx].width);
            SetChildAlongAxis(m_Children[idx], 1, m_Children[idx].y, m_Children[idx].height);
        }

        //ILayoutElement 接口
        public abstract void CalculateLayoutInputHorizontal();

        public abstract void CalculateLayoutInputVertical();

        //ILayoutController 接口
        public abstract void SetLayoutHorizontal();
        public abstract void SetLayoutVertical();

        public void ChangeChildrenCount(int newCount, float preferredWidth, float preferredHeight)
        {
            int oldCount = childCount;
            if (oldCount == newCount)
            {
                return;
            }

            if (newCount < oldCount)
            {
                for (int i = newCount; i < oldCount; i++)
                {
                    VirtualChild virtualChild = m_Children[i];
                    if (onChildUnbind != null)
                        onChildUnbind(i, virtualChild.rectTransform);

                    if (virtualChild.rectTransform != null)
                        ObjectPool.Instance.DisposeGameObject(virtualChild.rectTransform.gameObject);
                    virtualChild.Clear();
                    CommonPool.Instance.Dispose(virtualChild);
                }
                m_Children.RemoveRange(newCount, oldCount - newCount);
            }
            else
            {
                for (int j = oldCount; j < newCount; j++)
                {
                    VirtualChild virtualChild = CommonPool.Instance.Get<VirtualChild>();
                    virtualChild.preferredHeight = preferredHeight;
                    virtualChild.preferredWidth = preferredWidth;
                    m_Children.Add(virtualChild);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        private void ClearChildren()
        {
            for (int i = 0; i < m_Children.Count; i++)
            {
                VirtualChild virtualChild = m_Children[i];
                if (virtualChild.rectTransform != null)
                    ObjectPool.Instance.DisposeGameObject(virtualChild.rectTransform.gameObject);

                virtualChild.Clear();
                CommonPool.Instance.Dispose(virtualChild);
            }

            m_Children.Clear();
        }

        public Vector3 GetChildCenter(int idx)
        {
            if (idx < 0 || idx >= m_Children.Count)
            {
                Debug.LogError(string.Concat(new object[]
                {
                    "GetChildCenter out of bound idx:",
                    idx,
                    ",len:",
                    m_Children.Count
                }));
                return Vector3.zero;
            }

            if (corners == null)
                corners = new Vector3[4];

            rectTransform.GetLocalCorners(corners);
            VirtualChild virtualChild = m_Children[idx];
            Vector3 position = new Vector3(corners[1].x + virtualChild.x + virtualChild.width / 2,
                corners[1].y - virtualChild.y - virtualChild.height / 2, 0);
            return rectTransform.TransformPoint(position);
        }

        public Rect GetChildRect(int idx)
        {
            if (idx < 0 || idx >= m_Children.Count)
            {
                Debug.LogError(string.Concat(new object[]
                {
                    "bindchild out of bound idx:",
                    idx,
                    ",len:",
                    m_Children.Count
                }));
                return default(Rect);
            }
            return m_Children[idx].rect;
        }

        /// <summary>
        ///   <para>Returns the calculated position of the first child layout element along the given axis.</para>
        /// </summary>
        /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical.</param>
        /// <param name="requiredSpaceWithoutPadding">The total space required on the given axis for all the layout elements including spacing and excluding padding.</param>
        /// <returns>
        ///   <para>The position of the first child along the given axis.</para>
        /// </returns>
        protected float GetStartOffset(int axis, float requiredSpaceWithoutPadding)
        {
            float num1 = requiredSpaceWithoutPadding +
                         (axis != 0 ? (float) padding.vertical : (float) padding.horizontal);
            float num2 = rectTransform.rect.size[axis] - num1;
            float alignmentOnAxis = GetAlignmentOnAxis(axis);
            return (axis != 0 ? (float) padding.top : (float) padding.left) + num2 * alignmentOnAxis;
        }

        /// <summary>
        ///   <para>Returns the alignment on the specified axis as a fraction where 0 is lefttop, 0.5 is middle, and 1 is rightbottom.</para>
        /// </summary>
        /// <param name="axis">The axis to get alignment along. 0 is horizontal and 1 is vertical.</param>
        /// <returns>
        ///   <para>The alignment as a fraction where 0 is lefttop, 0.5 is middle, and 1 is rightbottom.</para>
        /// </returns>
        protected float GetAlignmentOnAxis(int axis)
        {
            return axis == 0 ? (float) ((int) childAlignment % 3) * 0.5f : (float) ((int) childAlignment / 3) * 0.5f;
        }

        protected float GetTotalFlexibleSize(int axis)
        {
            return m_TotalFlexibleSize[axis];
        }

        protected float GetTotalMinSize(int axis)
        {
            return m_TotalMinSize[axis];
        }

        protected float GetTotalPreferredSize(int axis)
        {
            return m_TotalPreferredSize[axis];
        }

        public void InitChildren(int count, float preferredWidth, float preferredHeight)
        {
            ClearChildren();
            for (int i = 0; i < count; i++)
            {
                VirtualChild virtualChild = CommonPool.Instance.Get<VirtualChild>();
                virtualChild.preferredHeight = preferredHeight;
                virtualChild.preferredWidth = preferredWidth;
                virtualChild.minHeight = preferredHeight;
                virtualChild.minWidth = preferredWidth;
                m_Children.Add(virtualChild);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        protected override void OnDestroy()
        {
            ClearChildren();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetDirty();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (isRootLayoutGroup)
            {
                SetDirty();
            }
        }

        protected virtual void OnTransformChildrenChanged()
        {
            SetDirty();
        }

        public void SetCallback(Action<int, RectTransform> onUnbind, Action layoutEnd)
        {
            onChildUnbind = onUnbind;
            onLayoutEnd = layoutEnd;
        }

        protected void SetChildAlongAxis(VirtualChild child, int axis, float pos, float size)
        {
            if (axis == 0)
            {
                child.x = pos;
                child.width = size;
            }
            else
            {
                child.height = size;
                child.y = pos;
            }

            if (child.rectTransform == null)
            {
                return;
            }

            m_Tracker.Add(this, child.rectTransform,
                DrivenTransformProperties.AnchoredPositionX | DrivenTransformProperties.AnchoredPositionY |
                DrivenTransformProperties.AnchorMinX | DrivenTransformProperties.AnchorMinY |
                DrivenTransformProperties.AnchorMaxX | DrivenTransformProperties.AnchorMaxY |
                DrivenTransformProperties.SizeDeltaX | DrivenTransformProperties.SizeDeltaY);
            child.rectTransform.SetInsetAndSizeFromParentEdge((axis != 0) ? RectTransform.Edge.Top : RectTransform.Edge.Left,
                pos, size);
        }

        protected void SetChildAlongAxis(VirtualChild child, int axis, float pos)
        {
            SetChildAlongAxis(child, axis, pos, child.sizeDelta[axis]);
        }

        protected void SetDirty()
        {
            if (!IsActive())
            {
                return;
            }

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        protected void SetLayoutInputForAxis(float totalMin, float totalPreferred, float totalFlexible, int axis)
        {
            m_TotalMinSize[axis] = totalMin;
            m_TotalPreferredSize[axis] = totalPreferred;
            m_TotalFlexibleSize[axis] = totalFlexible;
        }

        protected void SetProperty<T>(ref T currentValue, T newValue)
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
            {
                return;
            }

            currentValue = newValue;
            SetDirty();
        }

        public void UnBindChild(int index)
        {
            if (index < 0 || index >= childCount) return;
            m_Children[index].rectTransform = null;
        }

        //
        // Nested Types
        //
    }

    public class VirtualChild
    {
        public RectTransform rectTransform;

        public float x;

        public float y;

        public float width;

        public float height;

        public float minWidth;

        public float minHeight;

        public float preferredWidth;

        public float preferredHeight;

        public float flexibleWidth;

        public float flexibleHeight;

        public Vector2 sizeDelta
        {
            get { return new Vector2(width, height); }
        }

        public Rect rect
        {
            get { return new Rect(x, -y - height, width, height); }
        }

        public float GetMinSize(int axis)
        {
            if (rectTransform != null)
            {
                float minSize = LayoutElementUtil.GetMinSize(rectTransform.gameObject, axis);
                if (minSize >= 0)
                {
                    if (axis == 0)
                    {
                        minWidth = minSize;
                    }
                    else
                    {
                        minHeight = minSize;
                    }

                    return minSize;
                }
            }

            return (axis != 0) ? minHeight : minWidth;
        }

        public float GetPreferredSize(int axis)
        {
            if (rectTransform != null)
            {
                float preferredSize = LayoutElementUtil.GetPreferredSize(rectTransform.gameObject, axis);
                if (preferredSize >= 0)
                {
                    if (axis == 0)
                    {
                        preferredWidth = preferredSize;
                    }
                    else
                    {
                        preferredHeight = preferredSize;
                    }

                    return preferredSize;
                }
            }

            return (axis != 0) ? Mathf.Max(minHeight, preferredHeight) : Mathf.Max(minWidth, preferredWidth);
        }

        public float GetFlexibleSize(int axis)
        {
            if (rectTransform != null)
            {
                float flexibleSize = LayoutElementUtil.GetFlexibleSize(rectTransform.gameObject, axis);
                if (flexibleSize >= 0)
                {
                    if (axis == 0)
                    {
                        flexibleWidth = flexibleSize;
                    }
                    else
                    {
                        flexibleHeight = flexibleSize;
                    }

                    return flexibleSize;
                }
            }

            return (axis != 0) ? flexibleHeight : flexibleWidth;
        }

        public void Clear()
        {
            rectTransform = null;
            minWidth = 0;
            minHeight = 0;
            preferredWidth = 0;
            preferredHeight = 0;
            flexibleWidth = 0;
            flexibleHeight = 0;
            x = 0;
            y = 0;
            width = 0;
            height = 0;
        }
    }
}