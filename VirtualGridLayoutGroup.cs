using UnityEngine;
using UnityEngine.UI;

namespace InfinityScroll
{
    public class VirtualGridLayoutGroup : VirtualLayoutGroup
    {
        [SerializeField] protected GridLayoutGroup.Corner m_StartCorner = GridLayoutGroup.Corner.UpperLeft;
        [SerializeField] protected GridLayoutGroup.Axis m_StartAxis = GridLayoutGroup.Axis.Horizontal;
        [SerializeField] protected Vector2 m_CellSize = new Vector2(100f, 100f);
        [SerializeField] protected Vector2 m_Spacing = Vector2.zero;
        [SerializeField] protected GridLayoutGroup.Constraint m_Constraint = GridLayoutGroup.Constraint.Flexible;
        [SerializeField] protected int m_ConstraintCount = 2;

        /// <summary>
        ///   <para>Which corner should the first cell be placed in?</para>
        /// </summary>
        public GridLayoutGroup.Corner startCorner
        {
            get { return m_StartCorner; }
            set { SetProperty(ref m_StartCorner, value); }
        }

        /// <summary>
        ///   <para>Which axis should cells be placed along first?</para>
        /// </summary>
        public GridLayoutGroup.Axis startAxis
        {
            get { return m_StartAxis; }
            set { SetProperty(ref m_StartAxis, value); }
        }

        /// <summary>
        ///   <para>The size to use for each cell in the grid.</para>
        /// </summary>
        public Vector2 cellSize
        {
            get { return m_CellSize; }
            set { SetProperty(ref m_CellSize, value); }
        }

        /// <summary>
        ///   <para>The spacing to use between layout elements in the grid.</para>
        /// </summary>
        public Vector2 spacing
        {
            get { return m_Spacing; }
            set { SetProperty(ref m_Spacing, value); }
        }

        /// <summary>
        ///   <para>Which constraint to use for the GridLayoutGroup.</para>
        /// </summary>
        public GridLayoutGroup.Constraint constraint
        {
            get { return m_Constraint; }
            set { SetProperty(ref m_Constraint, value); }
        }

        /// <summary>
        ///   <para>How many cells there should be along the constrained axis.</para>
        /// </summary>
        public int constraintCount
        {
            get { return m_ConstraintCount; }
            set { SetProperty(ref m_ConstraintCount, Mathf.Max(1, value)); }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            constraintCount = constraintCount;
        }
#endif
        
        /// <summary>
        ///   <para>Called by the layout system.</para>
        /// </summary>
        public override void CalculateLayoutInputHorizontal()
        {
            int constraintCount;
            int num;
            if (m_Constraint == GridLayoutGroup.Constraint.FixedColumnCount)
                num = constraintCount = m_ConstraintCount;
            else if (m_Constraint == GridLayoutGroup.Constraint.FixedRowCount)
            {
                num = constraintCount =
                    Mathf.CeilToInt((float) ((double) childCount / (double) m_ConstraintCount -
                                             1.0 / 1000.0));
            }
            else
            {
                num = 1;
                constraintCount = Mathf.CeilToInt(Mathf.Sqrt((float) childCount));
            }

            SetLayoutInputForAxis(
                (float) padding.horizontal + (cellSize.x + spacing.x) * (float) num - spacing.x,
                (float) padding.horizontal + (cellSize.x + spacing.x) * (float) constraintCount -
                spacing.x, -1f, 0);
        }

        /// <summary>
        ///   <para>Called by the layout system.</para>
        /// </summary>
        public override void CalculateLayoutInputVertical()
        {
            float num = padding.vertical + (cellSize.y + spacing.y) *
                (m_Constraint != GridLayoutGroup.Constraint.FixedColumnCount
                    ? (m_Constraint != GridLayoutGroup.Constraint.FixedRowCount
                        ? Mathf.CeilToInt((float) childCount / Mathf.Max(1,
                            Mathf.FloorToInt(
                                ((rectTransform.rect.size.x -
                                  padding.horizontal +
                                  spacing.x + 1f / 1000f) /
                                 (cellSize.x +
                                  spacing.x)))))
                        : (float) m_ConstraintCount)
                    : Mathf.CeilToInt(
                        ((float) childCount / m_ConstraintCount -
                         1f / 1000f))) - spacing.y;
            SetLayoutInputForAxis(num, num, -1f, 1);
        }

        /// <summary>
        ///   <para>Called by the layout system.</para>
        /// </summary>
        public override void SetLayoutHorizontal()
        {
            SetCellsAlongAxis(0);
        }

        /// <summary>
        ///   <para>Called by the layout system.</para>
        /// </summary>
        public override void SetLayoutVertical()
        {
            SetCellsAlongAxis(1);
            if (onLayoutEnd != null)
            {
                onLayoutEnd();
            }
        }

        private void SetCellsAlongAxis(int axis)
        {
            if (axis == 0)
            {
                for (int index = 0; index < childCount; ++index)
                {
                    RectTransform rectChild = children[index].rectTransform;
                    if (rectChild)
                    {
                        m_Tracker.Add((Object) this, rectChild,
                            DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition |
                            DrivenTransformProperties.SizeDelta);
                        rectChild.anchorMin = Vector2.up;
                        rectChild.anchorMax = Vector2.up;
                        rectChild.sizeDelta = cellSize;
                    }
                }
            }
            else
            {
                float x = rectTransform.rect.size.x;
                float y = rectTransform.rect.size.y;
                int num1;
                int num2;
                if (m_Constraint == GridLayoutGroup.Constraint.FixedColumnCount)
                {
                    num1 = m_ConstraintCount;
                    num2 = Mathf.CeilToInt((float) ((double) childCount / (double) num1 - 1.0 / 1000.0));
                }
                else if (m_Constraint == GridLayoutGroup.Constraint.FixedRowCount)
                {
                    num2 = m_ConstraintCount;
                    num1 = Mathf.CeilToInt((float) ((double) childCount / (double) num2 - 1.0 / 1000.0));
                }
                else
                {
                    num1 = (double) cellSize.x + (double) spacing.x > 0.0
                        ? Mathf.Max(1,
                            Mathf.FloorToInt(
                                (float) (((double) x - (double) padding.horizontal + (double) spacing.x +
                                          1.0 / 1000.0) / ((double) cellSize.x + (double) spacing.x))))
                        : int.MaxValue;
                    num2 = (double) cellSize.y + (double) spacing.y > 0.0
                        ? Mathf.Max(1,
                            Mathf.FloorToInt(
                                (float) (((double) y - (double) padding.vertical + (double) spacing.y +
                                          1.0 / 1000.0) / ((double) cellSize.y + (double) spacing.y))))
                        : int.MaxValue;
                }

                int num3 = (int) startCorner % 2;
                int num4 = (int) startCorner / 2;
                int num5;
                int num6;
                int num7;
                if (startAxis == GridLayoutGroup.Axis.Horizontal)
                {
                    num5 = num1;
                    num6 = Mathf.Clamp(num1, 1, childCount);
                    num7 = Mathf.Clamp(num2, 1, Mathf.CeilToInt((float) childCount / (float) num5));
                }
                else
                {
                    num5 = num2;
                    num7 = Mathf.Clamp(num2, 1, childCount);
                    num6 = Mathf.Clamp(num1, 1, Mathf.CeilToInt((float) childCount / (float) num5));
                }

                Vector2 vector2_1 =
                    new Vector2(
                        (float) ((double) num6 * (double) cellSize.x +
                                 (double) (num6 - 1) * (double) spacing.x),
                        (float) ((double) num7 * (double) cellSize.y +
                                 (double) (num7 - 1) * (double) spacing.y));
                Vector2 vector2_2 =
                    new Vector2(GetStartOffset(0, vector2_1.x), GetStartOffset(1, vector2_1.y));
                for (int index = 0; index < childCount; ++index)
                {
                    int num8;
                    int num9;
                    if (startAxis == GridLayoutGroup.Axis.Horizontal)
                    {
                        num8 = index % num5;
                        num9 = index / num5;
                    }
                    else
                    {
                        num8 = index / num5;
                        num9 = index % num5;
                    }

                    if (num3 == 1)
                        num8 = num6 - 1 - num8;
                    if (num4 == 1)
                        num9 = num7 - 1 - num9;
                    SetChildAlongAxis(children[index], 0,
                        vector2_2.x + (cellSize[0] + spacing[0]) * (float) num8, cellSize[0]);
                    SetChildAlongAxis(children[index], 1,
                        vector2_2.y + (cellSize[1] + spacing[1]) * (float) num9, cellSize[1]);
                }
            }
        }
    }
}