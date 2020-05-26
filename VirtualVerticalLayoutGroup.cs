namespace InfinityScroll
{
    public class VirtualVerticalLayoutGroup : VirtualHorizontalOrVerticalLayoutGroup
    {
        public override void CalculateLayoutInputHorizontal()
        {
            CalcAlongAxis(0, true);
        }

        public override void CalculateLayoutInputVertical()
        {
            CalcAlongAxis(1, true);
        }

        public override void SetLayoutHorizontal()
        {
            SetChildrenAlongAxis(0, true);
        }

        public override void SetLayoutVertical()
        {
            SetChildrenAlongAxis(1, true);
            if (onLayoutEnd != null)
            {
                onLayoutEnd();
            }
        }
    }
}
