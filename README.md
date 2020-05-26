# InfinityScroll
Infinity ScrollView for UGUI. It helps the UGUI ScrollRect to support any count items.

# Feature
* Support any count items.
* Support Vertical/Horizontal/Gride Layout Group.
* Auto hide invisible items.
* Use [ObjectPool](https://github.com/Mr-sB/ObjectPool) to cache items.

# Usage
* Add `Virtual(Vertical/Horizontal/Gride)LayoutGroup` and `UIList` to the ScrollRect's content gameobject.
* Get `UIList` component and call `InitListView(int len)` method to generate items.
* There are two event you can listen in `UIList`:
```c#
public event Action<int, RectTransform> OnItemCreated;
public event Action<int> OnItemHided;
```

# Note
You must import [ObjectPool](https://github.com/Mr-sB/ObjectPool) module.
