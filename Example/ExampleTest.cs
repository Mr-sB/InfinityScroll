using UnityEngine;

namespace InfinityScroll.Example
{
    [RequireComponent(typeof(UIList))]
    public class ExampleTest : MonoBehaviour
    {
        public int InitCount = 20;
        private void Start()
        {
            var uiList = GetComponent<UIList>();
            uiList.InitListView(InitCount);
        }
    }
}