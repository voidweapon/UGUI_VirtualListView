using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("ExtendUI/Virtual List View", 37)]
[SelectionBase]
//[ExecuteInEditMode]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class VirtualListView : ScrollRect
{
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
    private int ItemCount { get { return m_itemCount; } set { SetItemCount(value); } }

    private List<RectTransform> m_items = new List<RectTransform>();
    private LinkedList<VisibleWindowItem> m_visibleWindow = new LinkedList<VisibleWindowItem>();

    private int m_maxColumn = 1;
    private int m_maxRow = 1;
    private Vector2 m_contentOrignPosition;

    protected override void Start()
    {
        base.Start();

        m_contentOrignPosition = content.anchoredPosition;

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
            Debug.Log("Awake");
            RebuildVisibleItems();
        }
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
        Debug.LogFormat("Col:{0},Row:{1}", m_visibleColumn, m_visibleRow);

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

        Debug.Log(requireItemCount);

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

    private void OnViewPointChange()
    {
        RebuildVisibleItems();
    }

    public void SetItemCount(int count)
    {
        m_itemCount = count;
        SetItemCount_Inner();
        if (m_templet != null)
        {
            RebuildVisibleItems();
        }
    }
    private void SetItemCount_Inner()
    {
        Vector2 contentSize = CalculateContentSize(m_itemCount);
        content.sizeDelta = contentSize;
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

        float weight = padding.horizontal + (cellSize.x + spacing.x) * maxColumns - spacing.x;
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
            Debug.LogFormat("width:{0}, {1}", width, cellCountX);
            maxRows = Mathf.CeilToInt(itemCount / (float)cellCountX);
            maxColumns = cellCountX;
        }
        float height = padding.vertical + (cellSize.y + spacing.y) * maxRows - spacing.y;

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
        base.SetContentAnchoredPosition(position);

        OnContentPositionChanged();
    }
    protected override void SetNormalizedPosition(float value, int axis)
    {
        base.SetNormalizedPosition(value, axis);

        OnContentPositionChanged();
    }

    private void OnContentPositionChanged()
    {
        if (!Application.isPlaying) return;

        Vector2 moveDistance = content.anchoredPosition - m_contentOrignPosition;

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

                m_visibleWindow.RemoveFirst();
                m_visibleWindow.AddLast(newVisibleWindowItem);
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
                m_visibleWindow.RemoveLast();
                m_visibleWindow.AddFirst(newVisibleWindowItem);
                Vector2 newPosition = CalculateItemPostion(m_maxColumn, m_maxRow, newVisibleWindowItem.dataIndex);
                SetItemPosition(m_items[newVisibleWindowItem.visibleObjIindex], newPosition, cellSize);
            }
        }
        Debug.LogFormat("passed:{0}, showEnd:{1}", Mathf.Min(passedItemCount, m_itemCount),
            Mathf.Min(passedItemCount + m_visibleRow * m_visibleColumn, m_itemCount));
    }

    private struct VisibleWindowItem
    {
        public int dataIndex;
        public int visibleObjIindex;
    }
}