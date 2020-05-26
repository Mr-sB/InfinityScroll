using System;
using UnityEngine;
using UnityEngine.UI;

namespace InfinityScroll
{
    public static class LayoutElementUtil
    {
        public static float GetMinSize(GameObject go, int axis)
        {
            return GetLayoutElementSize(go, element => axis == 0 ? element.minWidth : element.minHeight);
        }
    
        public static float GetPreferredSize(GameObject go, int axis)
        {
            return GetLayoutElementSize(go, element => axis == 0 ? element.preferredWidth : element.preferredHeight);
        }
    
        public static float GetFlexibleSize(GameObject go, int axis)
        {
            return GetLayoutElementSize(go, element => axis == 0 ? element.flexibleWidth : element.flexibleHeight);
        }

        private static float GetLayoutElementSize(GameObject go, Func<LayoutElement, float> func)
        {
            if (!go) return -1;
            var layoutElement = go.GetComponent<LayoutElement>();
            if (!layoutElement) return -1;
            return func(layoutElement);
        }
    }
}