using System;
using System.Collections.Generic;
using GameUtil;
using UnityEngine;
using UnityEngine.UI;

namespace InfinityScroll
{
    [RequireComponent(typeof(RectTransform))]
    public class UIList : MonoBehaviour
    {
        public ScrollRect ScrollRect { private set; get; }
        public VirtualLayoutGroup VirtualLayout { private set; get; }
        private RectTransform m_ContentTrans;
        private RectTransform m_RectTransform;
        private Vector3[] mContentCorners = new Vector3[4];
        private Vector3[] mViewportCorners = new Vector3[4];
        private Dictionary<int, RectTransform> mVisibleItems = new Dictionary<int, RectTransform>();
        private Vector2 mPrevPos = Vector2.zero;
        private Vector2 mPrevSize = Vector2.zero;
        private bool mLayoutChange;
        
        public ObjectPool.LoadMode LoadMode;
        public string PrefabBundleName;
        public string PrefabPath;
        public float PreferredWidth;
        public float PreferredHeight;
        public event Action<int, RectTransform> OnItemCreated;
        public event Action<int> OnItemHided;

        private void Awake()
        {
            m_RectTransform = transform as RectTransform;
            ScrollRect = GetComponentInParent<ScrollRect>();
            if (ScrollRect != null)
                m_ContentTrans = ScrollRect.content;

            VirtualLayout = GetComponent<VirtualLayoutGroup>();
            if (VirtualLayout != null)
            {
                VirtualLayout.SetCallback(OnUnbind, OnLayoutChange);
                ScrollRect.onValueChanged.AddListener(OnContentPosChange);
            }
            
            InitSize();
        }
        
        private void LateUpdate()
        {
            if (mLayoutChange)
            {
                mLayoutChange = false;
                UpdateChildVisible();
            }
        }
        
        protected void OnDestroy()
        {
            int childCount = m_RectTransform.childCount;
            if (childCount < 1) return;
            for (int i = childCount - 1; i > -1; i--)
                ObjectPool.Instance.DisposeGameObject(m_RectTransform.GetChild(i).gameObject);
            ScrollRect = null;
        }
        
        public void InitListView(int len)
        {
            mVisibleItems.Clear();
            int childCount = m_RectTransform.childCount;
            while (childCount-- > 0)
                ObjectPool.Instance.DisposeGameObject(m_RectTransform.GetChild(childCount).gameObject);

            VirtualLayout.InitChildren(len, PreferredWidth, PreferredHeight);
        }
        
        public RectTransform GetItem(int index)
        {
            if (VirtualLayout != null)
            {
                return mVisibleItems.TryGetValue(index, out var trans) ? trans : null;
            }

            if (index < 0 || index >= m_RectTransform.childCount)
            {
                Debug.LogWarning(string.Concat(new object[]
                {
                    "index out of bound:",
                    index,
                    ",child count:",
                    m_RectTransform.childCount
                }));
                return null;
            }

            Transform child = m_RectTransform.GetChild(index);
            if (child == null)
            {
                return null;
            }

            return child as RectTransform;
        }
        
        public void Refresh(int len)
        {
            if (m_RectTransform == null)
            {
                return;
            }

            if (VirtualLayout != null)
            {
                VirtualLayout.ChangeChildrenCount(len, PreferredWidth, PreferredHeight);
                UpdateChildVisible();
                return;
            }

            int childCount = m_RectTransform.childCount;
            if (childCount > len)
            {
                for (int i = childCount - 1; i >= len; i--)
                {
                    ObjectPool.Instance.DisposeGameObject(m_RectTransform.GetChild(i).gameObject);
                }
            }
        }

        public void Clean()
        {
            int childCount = m_RectTransform.childCount;
            if (childCount < 1)
            {
                return;
            }

            for (int i = childCount - 1; i > -1; i--)
            {
                ObjectPool.Instance.DisposeGameObject(m_RectTransform.GetChild(i).gameObject);
            }

            ScrollRect = null;
        }

        private GameObject CreateItem()
        {
            if (PrefabPath == null) return null;

            //加载
            GameObject go = ObjectPool.Instance.Get<GameObject>(LoadMode, PrefabBundleName, PrefabPath);
            if (go != null)
            {
                LayoutElement layoutElement = go.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    if (PreferredHeight > 0)
                        layoutElement.preferredHeight = PreferredHeight;

                    if (PreferredWidth > 0)
                        layoutElement.preferredWidth = PreferredWidth;
                }

                RectTransform rectTransform = go.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    if (PreferredHeight > 0 || PreferredWidth > 0)
                        rectTransform.sizeDelta = new Vector2(PreferredWidth, PreferredHeight);
                }
            }

            return go;
        }

        private void InitSize()
        {
            if (PreferredWidth > 0 || PreferredHeight > 0) return;

            GameObject go = CreateItem();
            if (go == null) return;

            RectTransform rect = go.GetComponent<RectTransform>();
            LayoutElement layoutElement = go.GetComponent<LayoutElement>();
            float preferredWidth = -1;
            float preferredHeight = -1;
            if (layoutElement != null)
            {
                preferredWidth = layoutElement.preferredWidth;
                preferredHeight = layoutElement.preferredHeight;
            }

            PreferredWidth = preferredWidth >= 0 ? preferredWidth : rect.sizeDelta.x;
            PreferredHeight = preferredHeight >= 0 ? preferredHeight : rect.sizeDelta.y;
            ObjectPool.Instance.DisposeGameObject(go);
        }

        private void OnContentPosChange(Vector2 pos)
        {
            if (Vector2Dist(m_ContentTrans.anchoredPosition, mPrevPos) > 0.1 ||
                Vector2Dist(m_ContentTrans.sizeDelta, mPrevSize) > 0.1)
            {
                mPrevPos = m_ContentTrans.anchoredPosition;
                mPrevSize = m_ContentTrans.sizeDelta;
                OnLayoutChange();
            }
        }

        private void OnItemHide(int index)
        {
            if (!mVisibleItems.TryGetValue(index, out var rect)) return;
            if (rect != null)
            {
                VirtualLayout.UnBindChild(index);
                ObjectPool.Instance.DisposeGameObject(rect.gameObject);
            }

            if (OnItemHided != null)
                OnItemHided(index);
            mVisibleItems.Remove(index);
        }

        private void OnItemShow(int idx)
        {
            if (mVisibleItems.ContainsKey(idx)) return;

            GameObject go = CreateItem();
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null)
            {
                Debug.LogError("invalid uilist item:" + go, go);
                ObjectPool.Instance.DisposeGameObject(go);
                return;
            }

            rect.SetParent(m_RectTransform);
            rect.transform.localScale = Vector3.one;
            VirtualLayout.BindChild(idx, rect);
            mVisibleItems.Add(idx, rect);
            if (OnItemCreated != null)
            {
                OnItemCreated(idx, rect);
            }
        }

        private void OnLayoutChange()
        {
            mLayoutChange = true;
        }

        private void OnUnbind(int idx, RectTransform rect)
        {
            OnItemHide(idx);
        }

        private void UpdateChildVisible()
        {
            if (ScrollRect == null || ScrollRect.viewport == null || m_ContentTrans == null ||
                VirtualLayout == null)
            {
                return;
            }

            RectTransform viewport = ScrollRect.viewport;
            viewport.GetWorldCorners(mViewportCorners);
            m_RectTransform.GetLocalCorners(mContentCorners);
            Vector3 vector = transform.InverseTransformPoint(mViewportCorners[0]);
            Vector3 vector2 = transform.InverseTransformPoint(mViewportCorners[2]);
            Rect other = new Rect(vector.x - mContentCorners[0].x, vector.y - mContentCorners[2].y,
                vector2.x - vector.x, vector2.y - vector.y);
            int childCount = VirtualLayout.childCount;
            if(childCount <= 0 ) return;
            if (mVisibleItems.Count <= 0)
            {
                //减少遍历次数
                bool hasVisibleItem = false;
                for (int i = 0; i < childCount; i++)
                {
                    if (VirtualLayout.GetChildRect(i).Overlaps(other))
                    {
                        OnItemShow(i);
                        hasVisibleItem = true;
                    }
                    else if(hasVisibleItem)
                        break;
                }
            }
            else
            {
                //减少遍历次数
                int minIndex = int.MaxValue;
                int maxIndex = int.MinValue;
                foreach (var index in mVisibleItems.Keys)
                {
                    if (index < minIndex)
                        minIndex = index;
                    if (index > maxIndex)
                        maxIndex = index;
                }
                int endIndex = maxIndex;
                for (; endIndex >= minIndex; endIndex--)
                {
                    if(VirtualLayout.GetChildRect(endIndex).Overlaps(other)) break;
                    OnItemHide(endIndex);
                }
                for (int i = minIndex; i < endIndex; i++)
                {
                    if(VirtualLayout.GetChildRect(i).Overlaps(other)) break;
                    OnItemHide(i);
                }

                for (int i = maxIndex + 1; i < childCount; i++)
                {
                    if (!VirtualLayout.GetChildRect(i).Overlaps(other)) break;
                    OnItemShow(i);
                }
                for (int i = minIndex - 1; i >= 0; i--)
                {
                    if (!VirtualLayout.GetChildRect(i).Overlaps(other)) break;
                    OnItemShow(i);
                }
            }
        }

        private float Vector2Dist(Vector2 a, Vector2 b)
        {
            Vector2 vector = a - b;
            return Mathf.Abs(vector.x) + Mathf.Abs(vector.y);
        }
    }
}