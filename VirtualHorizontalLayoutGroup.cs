namespace InfinityScroll
{
    public class VirtualHorizontalLayoutGroup : VirtualHorizontalOrVerticalLayoutGroup
    {
        public override void CalculateLayoutInputHorizontal()
        {
            CalcAlongAxis(0, false);
        }

        public override void CalculateLayoutInputVertical()
        {
            CalcAlongAxis(1, false);
        }

        public override void SetLayoutHorizontal()
        {
            SetChildrenAlongAxis(0, false);
        }

        public override void SetLayoutVertical()
        {
            SetChildrenAlongAxis(1, false);
            if (onLayoutEnd != null)
            {
                onLayoutEnd();
            }
        }
    }
}
