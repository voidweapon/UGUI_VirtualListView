using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("ExtendUI/Virtual List View", 37)]
[SelectionBase]
//[ExecuteInEditMode]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class VirtualListView : ScrollRect
{
    [Serializable]
    public class VirtualListViewEvent : UnityEvent<int, GameObject> { }

    public enum ListScrollType
    {
        Horizontal,
        Vertical,
    }

    [SerializeField] private GameObject m_templet = null;
    public GameObject Templet { get { return m_templet; } set { m_templet = value; RebuildVisibleItems(); } }

    [SerializeField] private ListScrollType m_scrollType = ListScrollType.Horizontal;
    public ListScrollType ScrollType { get { return m_scrollType; }  set { m_scrollType = value; SetScrollDirection(value); } }

    #region Grid Layout Group
    [SerializeField] protected RectOffset m_Padding = new RectOffset();
    public RectOffset padding { get { return m_Padding; } set { SetProperty(ref m_Padding, value); } }

    [SerializeField] protected GridLayoutGroup.Corner m_StartCorner = GridLayoutGroup.Corner.UpperLeft;
    public GridLayoutGroup.Corner startCorner { get { return m_StartCorner; } set { SetProperty(ref m_StartCorner, value); } }

    [SerializeField] protected GridLayoutGroup.Axis m_StartAxis = GridLayoutGroup.Axis.Horizontal;
    public GridLayoutGroup.Axis startAxis { get { return m_StartAxis; }  }

    [SerializeField] protected Vector2 m_CellSize = new Vector2(100, 100);
    public Vector2 cellSize { get { return m_CellSize; } set { SetProperty(ref m_CellSize, value); } }

    [SerializeField] protected Vector2 m_Spacing = Vector2.zero;
    public Vector2 spacing { get { return m_Spacing; } set { SetProperty(ref m_Spacing, value); } }

    [SerializeField] protected GridLayoutGroup.Constraint m_Constraint = GridLayoutGroup.Constraint.Flexible;
    public GridLayoutGroup.Constraint constraint { get { return m_Constraint; } set { SetProperty(ref m_Constraint, value); } }

    [SerializeField] protected int m_ConstraintCount = 2;
    public int constraintCount { get { return m_ConstraintCount; } set { SetProperty(ref m_ConstraintCount, Mathf.Max(1, value)); } } 
    #endregion

    [SerializeField]
    [HideInInspector]
    private int m_visibleColumn = 1;//列
    [SerializeField]
    [HideInInspector]
    private int m_visibleRow = 1;//行

    [SerializeField]
    [HideInInspector]
    private int m_itemCount;
    public int ItemCount { get { return m_itemCount; } set { SetItemCount(value); } }

    [SerializeField]
    private VirtualListViewEvent m_OnShowNewItem = new VirtualListViewEvent();
    public VirtualListViewEvent onShowNewItem { get { return m_OnShowNewItem; } set { m_OnShowNewItem = value; } }

    [SerializeField]
    private VirtualListViewEvent m_OnHideItem = new VirtualListViewEvent();
    public VirtualListViewEvent onHideItem { get { return m_OnHideItem; } set { m_OnHideItem = value; } }

    [SerializeField]
    private VirtualListBar m_VirtualVerticalScrollbar;
    public VirtualListBar VirtualVerticalScrollbar {
        get
        {
            return m_VirtualVerticalScrollbar;
        }
        set
        {
            if (m_VirtualVerticalScrollbar != null)
            {
                m_VirtualVerticalScrollbar.onValueChanged.RemoveListener(OnVirtualVerticalScrollBarValueChange);
            }
            m_VirtualVerticalScrollbar = value;
            if(m_VirtualVerticalScrollbar != null)
            {
                m_VirtualVerticalScrollbar.onValueChanged.AddListener(OnVirtualVerticalScrollBarValueChange);
            }
            SetDirtyCaching();
        }
    }

    [SerializeField]
    private VirtualListBar m_VirtualHorizontalScrollbar;
    public VirtualListBar VirtualHorizontalScrollbar
    {
        get
        {
            return m_VirtualHorizontalScrollbar;
        }
        set
        {
            if (m_VirtualHorizontalScrollbar != null)
            {
                m_VirtualHorizontalScrollbar.onValueChanged.RemoveListener(OnVirtualHorizontalScrollBarValueChange);
            }
            m_VirtualHorizontalScrollbar = value;
            if (m_VirtualHorizontalScrollbar != null)
            {
                m_VirtualHorizontalScrollbar.onValueChanged.AddListener(OnVirtualHorizontalScrollBarValueChange);
            }
            SetDirtyCaching();
        }
    }

    private List<RectTransform> m_items = new List<RectTransform>();
    private LinkedList<VisibleWindowItem> m_visibleWindow = new LinkedList<VisibleWindowItem>();

    private int m_maxColumn = 1;
    private int m_maxRow = 1;
    private Vector2 m_contentOrignPosition;

    private Vector2 m_moveAccumulated;
    private PointerEventData m_dragData = null;

    protected Bounds m_ViewportBounds;

    public int MinimumVisibleItemCount {
        get
        {
            var info = CalculateVisibleColumnAndRow();
            int col = Mathf.CeilToInt(info.x);
            int row = Mathf.CeilToInt(info.y);
            if (ScrollType == ListScrollType.Horizontal)
            {
                col = col > info.x ? --col : col;
            }
            else
            {
                row = row > info.y ? --row : row;
            }
            row = Mathf.Clamp(row, 1, row);
            col = Mathf.Clamp(col, 1, col);
            return col * row;
        }
    }

    public int perfectVisibleItemCount
    {
        get { var info = CalculateVisibleColumnAndRow(); return Mathf.FloorToInt(info.x) * Mathf.FloorToInt(info.y); }
    }

    public IScrollDistanceController ScrollDistanceController { get; set; }

    protected override void Awake()
    {
        base.Awake();

        if (content != null && viewport != null)
        {
            m_contentOrignPosition = content.anchoredPosition;
            m_moveAccumulated = m_contentOrignPosition;

            Vector2 columnAndRow = CalculateVisibleColumnAndRow();
            m_visibleColumn = (int)columnAndRow.x;
            m_visibleRow = (int)columnAndRow.y;
            if (ScrollType == ListScrollType.Horizontal)
            {
                m_visibleColumn += 1;
            }
            else
            {
                m_visibleRow += 1;
            }

            SetItemCount_Inner();
            if (m_templet != null && Application.isPlaying)
            {
                RebuildVisibleItems();
            }
        }
    }


    protected override void OnEnable()
    {
        if (m_VirtualVerticalScrollbar)
            m_VirtualVerticalScrollbar.onValueChanged.AddListener(OnVirtualVerticalScrollBarValueChange);
        if (m_VirtualHorizontalScrollbar)
            m_VirtualHorizontalScrollbar.onValueChanged.AddListener(OnVirtualHorizontalScrollBarValueChange);
        base.OnEnable();
    }

    protected override void OnDisable()
    {
        if (m_VirtualVerticalScrollbar)
            m_VirtualVerticalScrollbar.onValueChanged.RemoveListener(OnVirtualVerticalScrollBarValueChange);
        if (m_VirtualHorizontalScrollbar)
            m_VirtualHorizontalScrollbar.onValueChanged.RemoveListener(OnVirtualHorizontalScrollBarValueChange);
        base.OnDisable();
    }

    public override void Rebuild(CanvasUpdate executing)
    {
        base.Rebuild(executing);

        if(executing == CanvasUpdate.PostLayout)
        {
            UpdateViewportBounds();
            UpdateVirtualScrollbars(Vector2.zero);
        }
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);
        m_dragData = eventData;
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        m_dragData = null;
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        //UpdateVirtualScollbarSize(content.anchoredPosition);
    }


    private Vector2 UpdateVirtualScollbarSize(Vector2 position)
    {
        float deltaTime = Time.unscaledDeltaTime;
        Vector2 offset = CalculateOffset(Vector2.zero);
        Vector2 Velocity_t = velocity;
        for (int axis = 0; axis < 2; axis++)
        {
            // Apply spring physics if movement is elastic and content has an offset from the view.
            if (movementType == MovementType.Elastic && offset[axis] != 0)
            {
                float speed = velocity[axis];
                position[axis] = Mathf.SmoothDamp(content.anchoredPosition[axis], content.anchoredPosition[axis] + offset[axis], ref speed, elasticity, Mathf.Infinity, deltaTime);
                if (Mathf.Abs(speed) < 1)
                    speed = 0;
                Velocity_t[axis] = speed;
            }
            // Else move content according to velocity with deceleration applied.
            else if (inertia)
            {
                Velocity_t[axis] *= Mathf.Pow(decelerationRate, deltaTime);
                if (Mathf.Abs(Velocity_t[axis]) < 1)
                    Velocity_t[axis] = 0;
                position[axis] += Velocity_t[axis] * deltaTime;
            }
            // If we have neither elaticity or friction, there shouldn't be any velocity.
            else
            {
                Velocity_t[axis] = 0;
            }
        }

        if (movementType == MovementType.Clamped)
        {
            offset = CalculateOffset(position - content.anchoredPosition);
            position += offset;
        }
        UpdateVirtualScrollbars(offset);
        return position;
    }

    private void UpdateVirtualScrollbars(Vector2 offset)
    {
        if (m_VirtualHorizontalScrollbar)
        {
            if (m_ContentBounds.size.x > 0)
                m_VirtualHorizontalScrollbar.size = Mathf.Clamp01((m_ViewportBounds.size.x - Mathf.Abs(offset.x)) / m_ContentBounds.size.x);
            else
                m_VirtualHorizontalScrollbar.size = 1;

        }

        if (m_VirtualVerticalScrollbar)
        {
            if (m_ContentBounds.size.y > 0)
                m_VirtualVerticalScrollbar.size = Mathf.Clamp01((m_ViewportBounds.size.y - Mathf.Abs(offset.y)) / m_ContentBounds.size.y);
            else
                m_VirtualVerticalScrollbar.size = 1;

        }
    }

    public override void SetLayoutVertical()
    {
        base.SetLayoutVertical();
        m_ViewportBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
    }
    public override void SetLayoutHorizontal()
    {
        base.SetLayoutHorizontal();

        m_ViewportBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
    }

    protected void UpdateViewportBounds()
    {
        m_ViewportBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (!IsActive())
            return;

        for (int i = m_items.Count - 1; i >= 0; i--)
        {
            if (m_items[i] == null)
            {
                m_items.RemoveAt(i);
            }
        }

        if (viewport != null)
        {
            Vector2 columnAndRow = CalculateVisibleColumnAndRow();
            m_visibleColumn = (int)columnAndRow.x;
            m_visibleRow = (int)columnAndRow.y;
            if (ScrollType == ListScrollType.Horizontal)
            {
                m_visibleColumn += 1;
            }
            else
            {
                m_visibleRow += 1;
            } 
        }

        if (content != null)
        {
            SetItemCount_Inner(); 
        }

        if (m_templet != null && Application.isPlaying)
        {
            RebuildVisibleItems();
        }
    }
#endif

    //1.calculate visible col and row
    //2.reszie content fill item in view point
    //3.fill item in view point
    private void RebuildVisibleItems()
    {
        int requireItemCount = m_visibleColumn * m_visibleRow;

        if (ItemCount < m_visibleColumn * m_visibleRow)
        {
            requireItemCount = ItemCount;
        }

        int newItemCount = requireItemCount - m_items.Count;
        int orderedCont = m_items.Count;
        m_visibleWindow.Clear();
        for (int i = 0; i < newItemCount; i++)
        {
            GameObject newItem = Instantiate(Templet, content);
            RectTransform itemRectTransform = newItem.GetComponent<RectTransform>();
            itemRectTransform.anchorMin = Vector2.up;
            itemRectTransform.anchorMax = Vector2.up;
            itemRectTransform.sizeDelta = cellSize;
            m_items.Add(itemRectTransform);
        }
        for (int i = 0; i < requireItemCount; i++)
        {
            if (!m_items[i].gameObject.activeSelf)
            {
                m_items[i].gameObject.SetActive(true);
            }
            m_visibleWindow.AddLast(new VisibleWindowItem() {
                dataIndex = i,
                visibleObjIindex = i,
            });
            onShowNewItem.Invoke(i, m_items[i].gameObject);
        }
        for (int i = requireItemCount; i < m_items.Count; i++)
        {
            if (m_items[i].gameObject.activeSelf)
            {
                m_items[i].gameObject.SetActive(false);
            }
        }
        for (int i = 0; i < m_items.Count; i++)
        {
            Vector2 postion = CalculateItemPostion(m_maxColumn, m_maxRow, i);
            m_items[i].localPosition = postion;
            //SetItem Position
            SetItemPosition(m_items[i], postion, cellSize);        
        }
    }

    private void SetItemPosition(RectTransform rect, Vector2 pos, Vector2 size)
    {
        rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, pos.x, size.x);
        rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, pos.y, size.y);
    }

    public void SetItemCount(int count)
    {
        //if(m_itemCount != count)
        //{
        //    UpdateViewportBounds();
        //}

        m_itemCount = count;
        Vector2 columnAndRow = CalculateVisibleColumnAndRow();
        m_visibleColumn = (int)columnAndRow.x;
        m_visibleRow = (int)columnAndRow.y;
        if (ScrollType == ListScrollType.Horizontal)
        {
            m_visibleColumn += 1;
        }
        else
        {
            m_visibleRow += 1;
        }
        Vector2 newCountSize = SetItemCount_Inner();
        if (m_templet != null)
        {
            RebuildVisibleItems();
        }
        //SetNormalizedPosition(0, (int)ScrollType, false);
    }
    private Vector2 SetItemCount_Inner()
    {
        Vector2 contentSize = CalculateContentSize(m_itemCount);
        content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, contentSize.x);
        content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, contentSize.y);
        return contentSize;
    }

    public GameObject GetItem(int index)
    {
        foreach (var item in m_visibleWindow)
        {
            if(item.dataIndex == index)
            {
                return m_items[item.visibleObjIindex].gameObject;
            }
        }
        return null;
    }

    Vector2 CalculateItemPostion(int maxColumn, int maxRow, int index)
    {
        int cellsPerMainAxis;

        if (startAxis == GridLayoutGroup.Axis.Horizontal)
        {
            cellsPerMainAxis = maxColumn;
        }
        else
        {
            cellsPerMainAxis = maxRow;
        }

        int positionX, positionY;
        if(startAxis  == GridLayoutGroup.Axis.Horizontal)
        {
            positionX = index % cellsPerMainAxis;
            positionY = index / cellsPerMainAxis;
        }
        else
        {
            positionX = index / cellsPerMainAxis;
            positionY = index % cellsPerMainAxis;
        }
        float X = (cellSize.x + spacing.x) * positionX + padding.left;
        float Y = (cellSize.y + spacing.y) * positionY + padding.top;
        return new Vector2(X, Y);
    }

    public void SetContentPosition(Vector2 position)
    {
        SetContentAnchoredPosition(position);
    }

    #region Content Size
    private Vector2 CalculateContentSize(int itemCount)
    {
        if (ScrollType == ListScrollType.Horizontal)
        {
            return CalculateContentSizeHorizontal(itemCount);
        }
        else
        {
            return CalculateContentSizeVertical(itemCount);
        }
    }

    private Vector2 CalculateContentSizeHorizontal(int itemCount)
    {
        //Use current Content height and calculate weight
        int maxRows = 1;
        int maxColumns = 1;
        if (m_Constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            maxRows = Mathf.CeilToInt(itemCount / (float)m_ConstraintCount - 0.001f);
            maxColumns = m_ConstraintCount;
        }
        else if (m_Constraint == GridLayoutGroup.Constraint.FixedRowCount)
        {
            maxColumns = Mathf.CeilToInt(itemCount / (float)m_ConstraintCount - 0.001f);
            maxRows = m_ConstraintCount;
        }
        else
        {
            float height = viewport.rect.height;
            int cellCountY = Mathf.Max(1, Mathf.FloorToInt((height - padding.vertical + spacing.x + 0.001f) / (cellSize.y + spacing.y)));
            maxColumns = Mathf.CeilToInt(itemCount / (float)cellCountY);
            maxRows = cellCountY;
        }

        //float weight = padding.horizontal + (cellSize.x + spacing.x) * maxColumns - spacing.x;
       float weight = padding.horizontal + (cellSize.x + spacing.x) * maxColumns ;
        m_maxRow = maxRows;
        m_maxColumn = maxColumns;

        return new Vector2(weight, viewport.rect.height);
    }

    private Vector2 CalculateContentSizeVertical(int itemCount)
    {
        //Use current Content width and calculate height
        int maxRows = 1;
        int maxColumns = 1;
        if (m_Constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            maxRows = Mathf.CeilToInt(itemCount / (float)m_ConstraintCount - 0.001f);
            maxColumns = m_ConstraintCount;
        }
        else if (m_Constraint == GridLayoutGroup.Constraint.FixedRowCount)
        {
            maxRows = m_ConstraintCount;
            maxColumns = Mathf.CeilToInt(itemCount / (float)maxRows);
        }
        else
        {
            float width = viewport.rect.width;
            int cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
            maxRows = Mathf.CeilToInt(itemCount / (float)cellCountX);
            maxColumns = cellCountX;
        }
        //float height = padding.vertical + (cellSize.y + spacing.y) * maxRows - spacing.y;
        float height = padding.vertical + (cellSize.y + spacing.y) * maxRows;

        m_maxRow = maxRows;
        m_maxColumn = maxColumns;

        return new Vector2(viewport.rect.width, height);
    } 
    #endregion

    private Vector2 CalculateVisibleColumnAndRow()
    {
        Vector2 viewPointSize = viewport.rect.size;
        int column;
        int row;
        if(ScrollType == ListScrollType.Horizontal)
        {
            column = Mathf.CeilToInt((viewPointSize.x - padding.left) / (cellSize.x + spacing.x));
            row = Mathf.FloorToInt((viewPointSize.y - padding.top) / (cellSize.y + spacing.y));
        }
        else
        {
            column = Mathf.FloorToInt((viewPointSize.x - padding.left) / (cellSize.x + spacing.x));
            row = Mathf.CeilToInt((viewPointSize.y - padding.top) / (cellSize.y + spacing.y));
        }
        return new Vector2(column, row);
    }

    private void SetScrollDirection(ListScrollType scrollType)
    {
        switch (scrollType)
        {
            case ListScrollType.Horizontal:
                horizontal = true;
                vertical = false;
                m_StartAxis = GridLayoutGroup.Axis.Vertical;
                break;
            case ListScrollType.Vertical:
                horizontal = false;
                vertical = true;
                m_StartAxis = GridLayoutGroup.Axis.Horizontal;
                break;
            default:
                break;
        }
        SetDirty();
    }

    protected void SetProperty<T>(ref T currentValue, T newValue)
    {
        if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
            return;
        currentValue = newValue;
        SetDirty();
    }

    protected override void SetContentAnchoredPosition(Vector2 position)
    {
        if (ScrollDistanceController != null)
        {
            ScrollDistanceController.OnContentPositionChanged(this, position, ApplyContentPosition);
        }
        else
        {
            base.SetContentAnchoredPosition(position);
            OnContentPositionChanged();
        }

    }

    public void SetNormalizedPosition(float value, int axis, bool useController)
    {
        Vector2 oldValue = normalizedPosition;
        base.SetNormalizedPosition(value, axis);

        if (ScrollDistanceController != null && useController)
        {
            Vector2 position = content.anchoredPosition;
            base.SetNormalizedPosition(oldValue[axis], axis);
            ScrollDistanceController.OnContentPositionChanged(this, position, ApplyContentPosition);
        }
        else
        {
            OnContentPositionChanged();
        }
    }
    protected override void SetNormalizedPosition(float value, int axis)
    {
        Vector2 oldValue = normalizedPosition;
        base.SetNormalizedPosition(value, axis);

        if (ScrollDistanceController != null)
        {
            Vector2 position = content.anchoredPosition;
            base.SetNormalizedPosition(oldValue[axis], axis);
            ScrollDistanceController.OnContentPositionChanged(this, position, ApplyContentPosition);
        }
        else
        {
            OnContentPositionChanged();
        }
    }

    private void ApplyContentPosition(Vector2 position)
    {
        base.SetContentAnchoredPosition(position);
        OnContentPositionChanged();
       

        if (m_dragData != null)
        {
            //if in drag update drag start point;
            OnBeginDrag(m_dragData);
        }
    }

    private void OnContentPositionChanged()
    {
        UpdateViewportBounds();
        UpdateVirtualScollbarSize(content.anchoredPosition);

        if (!Application.isPlaying || ItemCount == 0) return;

        Vector2 moveDistance = content.anchoredPosition - m_contentOrignPosition;

        if ((content.anchoredPosition - m_moveAccumulated).sqrMagnitude < 1f) return;
        m_moveAccumulated = content.anchoredPosition;

        if (ScrollDistanceController != null)
        {
            ScrollDistanceController.Clear(this);
        }

        float distance = 0f;
        float padding = 0f;
        float spacing = 0f;
        int passedItemCount = 0;
        if (ScrollType == ListScrollType.Horizontal)
        {
            // Content move left to show next part item and move right to show previous part item.
            distance = -moveDistance.x;
            padding = m_Padding.left;
            spacing = m_Spacing.x;

            int a = Mathf.Clamp(Mathf.FloorToInt((distance - padding) / (cellSize.x + spacing)), 0, m_maxColumn);
            passedItemCount = a * m_maxRow;
        }
        else
        {
            //Content move down to show next part item and move up to show previous part item.
            distance = moveDistance.y;
            padding = m_Padding.top;
            spacing = m_Spacing.y;

            int a = Mathf.Clamp(Mathf.FloorToInt((distance - padding) / (cellSize.y + spacing)), 0, m_maxRow);
            passedItemCount = a * m_maxColumn;
        }

        int lastVisibleItemIndex = Mathf.Min(passedItemCount + m_visibleRow * m_visibleColumn, m_itemCount) - 1;
        passedItemCount = Mathf.Min(passedItemCount, m_itemCount);

        int[] newVisibleWindow = new int[2] { passedItemCount, lastVisibleItemIndex };
        int[] currentVisibleWindow = new int[2] { m_visibleWindow.First.Value.dataIndex, m_visibleWindow.Last.Value.dataIndex };

        if (newVisibleWindow[0] > currentVisibleWindow[0])
        {
            while (m_visibleWindow.First.Value.dataIndex < newVisibleWindow[0] && m_visibleWindow.Last.Value.dataIndex < newVisibleWindow[1])
            {
                var newVisibleWindowItem = new VisibleWindowItem()
                {
                    dataIndex = m_visibleWindow.Last.Value.dataIndex + 1,
                    visibleObjIindex = m_visibleWindow.First.Value.visibleObjIindex,
                };
                onHideItem.Invoke(m_visibleWindow.First.Value.dataIndex, m_items[m_visibleWindow.First.Value.visibleObjIindex].gameObject);
                m_visibleWindow.RemoveFirst();
                m_visibleWindow.AddLast(newVisibleWindowItem);
                onShowNewItem.Invoke(m_visibleWindow.Last.Value.dataIndex, m_items[m_visibleWindow.Last.Value.visibleObjIindex].gameObject);
                Vector2 newPosition = CalculateItemPostion(m_maxColumn, m_maxRow, newVisibleWindowItem.dataIndex);
                SetItemPosition(m_items[newVisibleWindowItem.visibleObjIindex], newPosition, cellSize);
            }
        }
        else if (newVisibleWindow[0] < currentVisibleWindow[0])
        {
            while (m_visibleWindow.Last.Value.dataIndex > newVisibleWindow[1] && m_visibleWindow.First.Value.dataIndex > newVisibleWindow[0])
            {
                var newVisibleWindowItem = new VisibleWindowItem()
                {
                    dataIndex = m_visibleWindow.First.Value.dataIndex - 1,
                    visibleObjIindex = m_visibleWindow.Last.Value.visibleObjIindex,
                };
                onHideItem.Invoke(m_visibleWindow.Last.Value.dataIndex, m_items[m_visibleWindow.Last.Value.visibleObjIindex].gameObject);
                m_visibleWindow.RemoveLast();
                m_visibleWindow.AddFirst(newVisibleWindowItem);
                onShowNewItem.Invoke(m_visibleWindow.First.Value.dataIndex, m_items[m_visibleWindow.First.Value.visibleObjIindex].gameObject);
                Vector2 newPosition = CalculateItemPostion(m_maxColumn, m_maxRow, newVisibleWindowItem.dataIndex);
                SetItemPosition(m_items[newVisibleWindowItem.visibleObjIindex], newPosition, cellSize);
            }
        }
    }

    private void OnVirtualVerticalScrollBarValueChange(float value)
    {
        SetNormalizedPosition(value, 1);
    }
    private void OnVirtualHorizontalScrollBarValueChange(float value)
    {
        SetNormalizedPosition(value, 0);
    }

   

    private Vector2 CalculateOffset(Vector2 delta)
    {
        return InternalCalculateOffset(ref m_ViewportBounds, ref m_ContentBounds, horizontal, vertical, movementType, ref delta);
    }

    internal static Vector2 InternalCalculateOffset(ref Bounds viewBounds, ref Bounds contentBounds, bool horizontal, bool vertical, MovementType movementType, ref Vector2 delta)
    {
        Vector2 offset = Vector2.zero;
        if (movementType == MovementType.Unrestricted)
            return offset;

        Vector2 min = contentBounds.min;
        Vector2 max = contentBounds.max;

        if (horizontal)
        {
            min.x += delta.x;
            max.x += delta.x;
            if (min.x > viewBounds.min.x)
                offset.x = viewBounds.min.x - min.x;
            else if (max.x < viewBounds.max.x)
                offset.x = viewBounds.max.x - max.x;
        }

        if (vertical)
        {
            min.y += delta.y;
            max.y += delta.y;
            if (max.y < viewBounds.max.y)
                offset.y = viewBounds.max.y - max.y;
            else if (min.y > viewBounds.min.y)
                offset.y = viewBounds.min.y - min.y;
        }

        return offset;
    }


    private struct VisibleWindowItem
    {
        public int dataIndex;
        public int visibleObjIindex;
    }
}

public interface IScrollDistanceController
{
    void OnContentPositionChanged(VirtualListView view, Vector2 position, Action<Vector2> ApplyContentPosition);
    void Clear(VirtualListView view);
}