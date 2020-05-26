using System;
using UnityEngine;

namespace InfinityScroll
{
    public abstract class VirtualHorizontalOrVerticalLayoutGroup : VirtualLayoutGroup
    {
        [SerializeField] protected float m_Spacing = 0.0f;
        [SerializeField] protected bool m_ChildForceExpandWidth = true;
        [SerializeField] protected bool m_ChildForceExpandHeight = true;
        [SerializeField] protected bool m_ChildControlWidth = true;
        [SerializeField] protected bool m_ChildControlHeight = true;

        /// <summary>
        ///   <para>The spacing to use between layout elements in the layout group.</para>
        /// </summary>
        public float spacing
        {
            get { return m_Spacing; }
            set { SetProperty<float>(ref m_Spacing, value); }
        }

        /// <summary>
        ///   <para>Whether to force the children to expand to fill additional available horizontal space.</para>
        /// </summary>
        public bool childForceExpandWidth
        {
            get { return m_ChildForceExpandWidth; }
            set { SetProperty<bool>(ref m_ChildForceExpandWidth, value); }
        }

        /// <summary>
        ///   <para>Whether to force the children to expand to fill additional available vertical space.</para>
        /// </summary>
        public bool childForceExpandHeight
        {
            get { return m_ChildForceExpandHeight; }
            set { SetProperty<bool>(ref m_ChildForceExpandHeight, value); }
        }

        /// <summary>
        ///   <para>Returns true if the Layout Group controls the widths of its children. Returns false if children control their own widths.</para>
        /// </summary>
        public bool childControlWidth
        {
            get { return m_ChildControlWidth; }
            set { SetProperty<bool>(ref m_ChildControlWidth, value); }
        }

        /// <summary>
        ///   <para>Returns true if the Layout Group controls the heights of its children. Returns false if children control their own heights.</para>
        /// </summary>
        public bool childControlHeight
        {
            get { return m_ChildControlHeight; }
            set { SetProperty<bool>(ref m_ChildControlHeight, value); }
        }

        /// <summary>
        ///   <para>Calculate the layout element properties for this layout element along the given axis.</para>
        /// </summary>
        /// <param name="axis">The axis to calculate for. 0 is horizontal and 1 is vertical.</param>
        /// <param name="isVertical">Is this group a vertical group?</param>
        protected void CalcAlongAxis(int axis, bool isVertical)
        {
            float num1 = axis != 0 ? (float) padding.vertical : (float) padding.horizontal;
            bool controlSize = axis != 0 ? m_ChildControlHeight : m_ChildControlWidth;
            bool childForceExpand = axis != 0 ? childForceExpandHeight : childForceExpandWidth;
            float num2 = num1;
            float b = num1;
            float num3 = 0.0f;
            bool flag = isVertical ^ axis == 1;
            for (int index = 0; index < childCount; ++index)
            {
                float min;
                float preferred;
                float flexible;
                GetChildSizes(children[index], axis, controlSize, childForceExpand, out min, out preferred,
                    out flexible);
                if (flag)
                {
                    num2 = Mathf.Max(min + num1, num2);
                    b = Mathf.Max(preferred + num1, b);
                    num3 = Mathf.Max(flexible, num3);
                }
                else
                {
                    num2 += min + spacing;
                    b += preferred + spacing;
                    num3 += flexible;
                }
            }

            if (!flag && childCount > 0)
            {
                num2 -= spacing;
                b -= spacing;
            }

            float totalPreferred = Mathf.Max(num2, b);
            SetLayoutInputForAxis(num2, totalPreferred, num3, axis);
        }

        /// <summary>
        ///   <para>Set the positions and sizes of the child layout elements for the given axis.</para>
        /// </summary>
        /// <param name="axis">The axis to handle. 0 is horizontal and 1 is vertical.</param>
        /// <param name="isVertical">Is this group a vertical group?</param>
        protected void SetChildrenAlongAxis(int axis, bool isVertical)
        {
            float num1 = rectTransform.rect.size[axis];
            bool controlSize = axis != 0 ? m_ChildControlHeight : m_ChildControlWidth;
            bool childForceExpand = axis != 0 ? childForceExpandHeight : childForceExpandWidth;
            float alignmentOnAxis = GetAlignmentOnAxis(axis);
            if (isVertical ^ axis == 1)
            {
                float num2 = num1 - (axis != 0 ? (float) padding.vertical : (float) padding.horizontal);
                for (int index = 0; index < childCount; ++index)
                {
                    VirtualChild virtualChild = children[index];
                    float min;
                    float preferred;
                    float flexible;
                    GetChildSizes(virtualChild, axis, controlSize, childForceExpand, out min, out preferred,
                        out flexible);
                    float num3 = Mathf.Clamp(num2, min, flexible <= 0.0 ? preferred : num1);
                    float startOffset = GetStartOffset(axis, num3);
                    if (controlSize)
                    {
                        SetChildAlongAxis(virtualChild, axis, startOffset, num3);
                    }
                    else
                    {
                        float num4 = (num3 - virtualChild.sizeDelta[axis]) * alignmentOnAxis;
                        SetChildAlongAxis(virtualChild, axis, startOffset + num4);
                    }
                }
            }
            else
            {
                float pos = axis != 0 ? padding.top : padding.left;
                if (Math.Abs((double) GetTotalFlexibleSize(axis)) < 1e-4 &&
                    GetTotalPreferredSize(axis) < num1)
                    pos = GetStartOffset(axis,
                        GetTotalPreferredSize(axis) -
                        (axis != 0 ? padding.vertical : padding.horizontal));
                float t = 0.0f;
                if (Math.Abs((double) GetTotalMinSize(axis) - GetTotalPreferredSize(axis)) > 1e-4)
                    t = Mathf.Clamp01(((num1 - GetTotalMinSize(axis)) /
                                       (GetTotalPreferredSize(axis) -
                                        GetTotalMinSize(axis))));
                float num2 = 0.0f;
                if (num1 > GetTotalPreferredSize(axis) &&
                    GetTotalFlexibleSize(axis) > 0.0)
                    num2 = (num1 - GetTotalPreferredSize(axis)) / GetTotalFlexibleSize(axis);
                for (int index = 0; index < childCount; ++index)
                {
                    VirtualChild virtualChild = children[index];
                    float min;
                    float preferred;
                    float flexible;
                    GetChildSizes(virtualChild, axis, controlSize, childForceExpand, out min, out preferred,
                        out flexible);
                    float size = Mathf.Lerp(min, preferred, t) + flexible * num2;
                    if (controlSize)
                    {
                        SetChildAlongAxis(virtualChild, axis, pos, size);
                    }
                    else
                    {
                        float num3 = (size - virtualChild.sizeDelta[axis]) * alignmentOnAxis;
                        SetChildAlongAxis(virtualChild, axis, pos + num3);
                    }

                    pos += size + spacing;
                }
            }
        }

        private void GetChildSizes(
            VirtualChild child,
            int axis,
            bool controlSize,
            bool childForceExpand,
            out float min,
            out float preferred,
            out float flexible)
        {
            if (!controlSize)
            {
                min = child.sizeDelta[axis];
                preferred = min;
                flexible = 0.0f;
            }
            else
            {
                min = child.GetMinSize(axis);
                preferred = child.GetPreferredSize(axis);
                flexible = child.GetFlexibleSize(axis);
            }

            if (!childForceExpand)
                return;
            flexible = Mathf.Max(flexible, 1f);
        }

        protected override void Reset()
        {
            base.Reset();
            m_ChildControlWidth = false;
            m_ChildControlHeight = false;
        }
    }
}