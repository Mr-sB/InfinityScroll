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
        public ObjectPool.LoadMode LoadMode;
        public string PrefabPath;
        public float PreferredWidth;
        public float PreferredHeight;
        public event Action<int, RectTransform> OnItemCreated;
        public event Action<int> OnItemHided;

        private ScrollRect m_ScrollRect;
        private VirtualLayoutGroup m_VirtualLayout;
        private RectTransform m_ContentTrans;
        private RectTransform m_RectTransform;
        private Vector3[] mContentCorners = new Vector3[4];
        private Vector3[] mViewportCorners = new Vector3[4];
        private Dictionary<int, RectTransform> mVisibleItems = new Dictionary<int, RectTransform>();
        private Vector2 mPrevPos = Vector2.zero;
        private Vector2 mPrevSize = Vector2.zero;
        private bool mLayoutChange;

        private void Awake()
        {
            m_RectTransform = transform as RectTransform;
            m_ScrollRect = GetComponentInParent<ScrollRect>();
            if (m_ScrollRect != null)
                m_ContentTrans = m_ScrollRect.content;

            m_VirtualLayout = GetComponent<VirtualLayoutGroup>();
            if (m_VirtualLayout != null)
            {
                m_VirtualLayout.SetCallback(OnUnbind, OnLayoutChange);
                m_ScrollRect.onValueChanged.AddListener(OnContentPosChange);
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
            m_ScrollRect = null;
        }
        
        public void InitListView(int len)
        {
            mVisibleItems.Clear();
            int childCount = m_RectTransform.childCount;
            while (childCount-- > 0)
                ObjectPool.Instance.DisposeGameObject(m_RectTransform.GetChild(childCount).gameObject);

            m_VirtualLayout.InitChildren(len, PreferredWidth, PreferredHeight);
        }
        
        public RectTransform GetItem(int index)
        {
            if (m_VirtualLayout != null)
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

            if (m_VirtualLayout != null)
            {
                m_VirtualLayout.ChangeChildrenCount(len, PreferredWidth, PreferredHeight);
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

            m_ScrollRect = null;
        }

        private GameObject CreateItem()
        {
            if (PrefabPath == null) return null;

            //加载
            GameObject go = ObjectPool.Instance.Get<GameObject>(PrefabPath, LoadMode);
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
                preferredWidth = layoutElement.flexibleWidth;
                preferredHeight = layoutElement.flexibleHeight;
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
                m_VirtualLayout.UnBindChild(index);
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
            m_VirtualLayout.BindChild(idx, rect);
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
            if (m_ScrollRect == null || m_ScrollRect.viewport == null || m_ContentTrans == null ||
                m_VirtualLayout == null)
            {
                return;
            }

            RectTransform viewport = m_ScrollRect.viewport;
            viewport.GetWorldCorners(mViewportCorners);
            m_RectTransform.GetLocalCorners(mContentCorners);
            Vector3 vector = base.transform.InverseTransformPoint(mViewportCorners[0]);
            Vector3 vector2 = base.transform.InverseTransformPoint(mViewportCorners[2]);
            Rect other = new Rect(vector.x - mContentCorners[0].x, vector.y - mContentCorners[2].y,
                vector2.x - vector.x, vector2.y - vector.y);
            int childCount = m_VirtualLayout.childCount;
            for (int i = 0; i < childCount; i++)
            {
                if (m_VirtualLayout.GetChildRect(i).Overlaps(other))
                {
                    OnItemShow(i);
                }
                else
                {
                    OnItemHide(i);
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