using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class TableRenderer : MonoBehaviour 
{
    //item容器
    public GameObject content;
    //滚动组件
    public GameObject scroll;
    //是否是横向的
    private bool isHorizontal;
    //更新的回调方法
    public delegate void UpdateTableItem(GameObject item, int index, bool isReload);
    //更新列表回调方法
    private UpdateTableItem m_updateItem;
    //横向间隔
    private float gapH;
    //纵向间隔
    private float gapV;
    //item的预设
    public GameObject itemPrefab;
    //元素宽度
    private float itemWidth;
    //元素高度
    private float itemHeight;
    //列表宽度
    private float listWidth;
    //列表高度
    private float listHeight;
    //是否重新加载
    private bool isReload;
    //上一个位置
    private Vector2 prevItemPos;
    //滚动容器初始位置
    private Vector3 contentStartPos;
    //存放item的列表
    private List<List<GameObject>> itemLineList = null;
    //一行的数量或者一列的数量
    private int lineItemCount = 0;
    //多少行或者多少列
    private int lineCount = 0;
    //可显示的行数
    private int showLineCount = 0;
    //当前第一个item的索引
    private int curLineIndex = 0;
    //总的数据数量
    private int totalLineCount;
    //底部位置
    private float bottom;
    //顶部位置
    private float top;
    //左边位置
    private float left;
    //右边位置
    private float right;
    public void init(bool isHorizontal = false,
                     int count = 0,
                     int lineItemCount = 0,
                     float gapH = 5,
                     float gapV = 5,
                     UpdateTableItem updateItem = null)
    {
        //总的数据数量
        this.totalLineCount = -1;
        if (count < 0) count = 0;
        if (this.scroll == null) return;
        if (this.content == null) return;
        this.m_updateItem = updateItem;
        this.isHorizontal = isHorizontal;
        //设置组件横向纵向滚动
        this.scroll.GetComponent<ScrollRect>().horizontal = isHorizontal;
        this.scroll.GetComponent<ScrollRect>().vertical = !isHorizontal;
        this.gapH = gapH;
        this.gapV = gapV;

        this.itemWidth = this.itemPrefab.GetComponent<RectTransform>().sizeDelta.x;
        this.itemHeight = this.itemPrefab.GetComponent<RectTransform>().sizeDelta.y;

        Vector2 v2;
        if(!this.isHorizontal)
            v2 = new Vector2((this.itemWidth + gapV) * lineItemCount, this.scroll.GetComponent<RectTransform>().sizeDelta.y);
        else
            v2 = new Vector2(this.scroll.GetComponent<RectTransform>().sizeDelta.x, (this.itemHeight + gapH) * lineItemCount);

        this.scroll.GetComponent<RectTransform>().sizeDelta = v2;
        this.listWidth = this.scroll.GetComponent<RectTransform>().sizeDelta.x;
        this.listHeight = this.scroll.GetComponent<RectTransform>().sizeDelta.y;

        this.content.GetComponent<RectTransform>().sizeDelta = new Vector2(listWidth, listHeight);
        this.content.transform.localPosition = new Vector3(0, 0);
        this.prevItemPos = new Vector2();
        this.contentStartPos = this.scroll.transform.localPosition;
        //一行或者一列的数量
        this.lineItemCount = lineItemCount;
        //总的行数或者列数
        this.lineCount = Mathf.CeilToInt((float)count / (float)lineItemCount);

        print("一排创建" + this.lineItemCount + "个");
        print("一共" + this.lineCount + "行");

        this.reloadData(count);
        this.isReload = true;
    }

    /// <summary>
    /// 创建item
    /// </summary>
    /// <param name="prefab">item的预设</param>
    /// <returns></returns>
    void createItem(GameObject prefab, int count)
    {
        if (this.itemLineList == null) this.itemLineList = new List<List<GameObject>>();
        if (count <= 0) return;
        int lineShowCount = 0;
        int lineCount = 0;
        for (int i = 0; i < count; ++i)
        {
            GameObject item = MonoBehaviour.Instantiate(prefab, new Vector3(0, 0), new Quaternion()) as GameObject;
            item.transform.SetParent(this.content.gameObject.transform);
            item.transform.localScale = new Vector3(1, 1, 1);
            if (lineShowCount == 0) this.itemLineList.Add(new List<GameObject>());
            List<GameObject> itemList = this.itemLineList[lineCount];
            itemList.Add(item);
            lineShowCount++;
            if (lineShowCount >= this.lineItemCount)
            {
                lineShowCount = 0;
                lineCount++;
            }
        }
    }

    /// <summary>
    /// 更新item
    /// </summary>
    /// <returns></returns>
    void updateItem()
    {
        if (!this.isReload) return;
        //坐标系 上正下负
        for (int i = 0; i < this.itemLineList.Count; ++i)
        {
            List<GameObject> itemList = this.itemLineList[i];
            int itemListLength = itemList.Count;
            GameObject item = itemList[0];
            if(!this.isHorizontal)
            {
                //获取item相对于scroll的坐标
                float posY = scroll.transform.InverseTransformPoint(item.transform.position).y;
                if (posY > this.top && this.curLineIndex < this.totalLineCount - this.showLineCount)
                {
                    //往上拖动时
                    //print("往上拖动时");
                    //如果第一个位置超过顶部范围，并且不是滚动到最后一个，则重新设置位置。 
                    if (this.itemLineList.Count > 1)
                    {
                        this.itemLineList.RemoveAt(i);
                        List<GameObject> lastItemList = this.itemLineList[this.itemLineList.Count - 1];
                        GameObject lastItem = lastItemList[0];
                        for (int j = 0; j < itemListLength; j++)
                        {
                            //当前第i排的所有item
                            GameObject curLineItem = itemList[j];
                            curLineItem.transform.localPosition = new Vector3(curLineItem.transform.localPosition.x,
                                                                              lastItem.transform.localPosition.y - this.itemHeight - this.gapV);
                        }
                        this.itemLineList.Add(itemList);
                        this.curLineIndex++;
                    }
                    else
                    {
                        for (int j = 0; j < itemListLength; j++)
                        {
                            //当前第i排的所有item
                            GameObject curLineItem = itemList[j];
                            curLineItem.transform.localPosition = new Vector3(curLineItem.transform.localPosition.x,
                                                                              -this.itemHeight - this.gapV);
                        }
                        this.curLineIndex = 0;
                    }
                    break;
                }
                else if (posY < this.bottom && this.curLineIndex > 0)
                {
                    //往下拖动时
                    //print("往下拖动时");
                    //如果底部位置超过范围,并且不是滚动到第一个位置，则重新设置位置。
                    if (this.itemLineList.Count > 1)
                    {
                        this.itemLineList.RemoveAt(i);
                        List<GameObject> firstItemList = this.itemLineList[0];
                        GameObject firstItem = firstItemList[0];
                        print("firstItem.transform.localPosition.y　" + firstItem.transform.localPosition.y);
                        for (int j = 0; j < itemListLength; j++)
                        {
                            //当前第i排的所有item
                            GameObject curLineItem = itemList[j];
                            curLineItem.transform.localPosition = new Vector3(curLineItem.transform.localPosition.x,
                                                                              firstItem.transform.localPosition.y + this.itemHeight + this.gapV);
                            print(curLineItem.transform.localPosition.x + " " + curLineItem.transform.localPosition.y);
                        }
                        this.itemLineList.Insert(0, itemList);
                        this.curLineIndex--;
                    }
                    else
                    {
                        for (int j = 0; j < itemListLength; j++)
                        {
                            //当前第i排的所有item
                            GameObject curLineItem = itemList[j];
                            curLineItem.transform.localPosition = new Vector3(curLineItem.transform.localPosition.x,
                                                                              -this.itemHeight - this.gapV);
                        }
                        this.curLineIndex = 0;
                    }
                    break;
                }
            }
            else
            {
                //获取item相对于scroll的坐标
                float posX = scroll.transform.InverseTransformPoint(item.transform.position).x;
                if (posX < this.left && this.curLineIndex < this.totalLineCount - this.showLineCount)
                {
                    //往上拖动时
                    //如果第一个位置超过顶部范围，并且不是滚动到最后一个，则重新设置位置。
                    if (this.itemLineList.Count > 1)
                    {
                        this.itemLineList.RemoveAt(i);
                        List<GameObject> lastItemList = this.itemLineList[this.itemLineList.Count - 1];
                        GameObject lastItem = lastItemList[0];
                        for (int j = 0; j < itemListLength; j++)
                        {
                            //当前第i排的所有item
                            GameObject curLineItem = itemList[j];
                            curLineItem.transform.localPosition = new Vector3(lastItem.transform.localPosition.x + this.itemWidth + this.gapH,
                                                                              curLineItem.transform.localPosition.y);
                        }
                        this.itemLineList.Add(itemList);
                        this.curLineIndex++;
                    }
                    else
                    {
                        for (int j = 0; j < itemListLength; j++)
                        {
                            //当前第i排的所有item
                            GameObject curLineItem = itemList[j];
                            curLineItem.transform.localPosition = new Vector3(this.itemWidth + this.gapH,
                                                                              curLineItem.transform.localPosition.y);
                        }
                        this.curLineIndex = 0;
                    }
                    break;
                }
                else if (posX > this.right && this.curLineIndex > 0)
                {
                    //往下拖动时
                    //如果底部位置超过范围,并且不是滚动到第一个位置，则重新设置位置。
                    if (this.itemLineList.Count > 1)
                    {
                        this.itemLineList.RemoveAt(i);
                        List<GameObject> firstItemList = this.itemLineList[0];
                        GameObject firstItem = firstItemList[0];
                        for (int j = 0; j < itemListLength; j++)
                        {
                            //当前第i排的所有item
                            GameObject curLineItem = itemList[j];
                            curLineItem.transform.localPosition = new Vector3(firstItem.transform.localPosition.x - this.itemWidth - this.gapH,
                                                                              curLineItem.transform.localPosition.y);
                        }
                        this.itemLineList.Insert(0, itemList);
                        this.curLineIndex--;
                    }
                    else
                    {
                        for (int j = 0; j < itemListLength; j++)
                        {
                            //当前第i排的所有item
                            GameObject curLineItem = itemList[j];
                            curLineItem.transform.localPosition = new Vector3(this.itemWidth + this.gapH,
                                                                              curLineItem.transform.localPosition.y);
                        }
                        this.curLineIndex = 0;
                    }
                    break;
                }
            }
        }
        //重新调用item回调

    }


    /// <summary>
    /// 重新设置数据
    /// </summary>
    /// <param name="count">当前数据列表的数量</param>
    /// <returns></returns>
    public void reloadData(int count)
    {
        this.isReload = false;
        //保存上一次显示的数量
        int prevShowLineCount = this.showLineCount;
        //判断当前多出来的数量，并删除。
        this.removeOverItem(count);
        if (!this.isHorizontal) //纵向
            this.showLineCount = (int)(Mathf.Ceil(this.listHeight / (this.itemHeight + this.gapV))); //计算应该显示的数量
        else
            this.showLineCount = (int)(Mathf.Ceil(this.listWidth / (this.itemWidth + this.gapH)));
        //需要创建的数量不大于实际数量
        if (this.showLineCount >= count)
            this.showLineCount = count; //取实际数据的数量
        else
            this.showLineCount += 1; //取计算的数量 + 1
        //创建数量
        print("可显示的行数 " + this.showLineCount);
        int createLineCount = this.showLineCount - prevShowLineCount;
        if (createLineCount < 0) createLineCount = 0;
        int createCount = createLineCount * this.lineItemCount;
        this.totalLineCount = count;
        print("需要创建的行数 " + createLineCount);
        print("需要创建的item数量 " + createCount);
        //根据显示数量创建item
        this.createItem(this.itemPrefab, createCount);
        this.updateBorder();
        if (!this.isHorizontal)
        {
            this.content.GetComponent<RectTransform>().sizeDelta = new Vector2(this.content.GetComponent<RectTransform>().sizeDelta.x,
                                                                               this.totalLineCount * (this.itemHeight + this.gapV));
        }
        else
        {
            this.content.GetComponent<RectTransform>().sizeDelta = new Vector2(this.totalLineCount * (this.itemWidth + this.gapH),
                                                                               this.content.GetComponent<RectTransform>().sizeDelta.y);
        }
        this.layoutItem();
        this.isReload = true;
    }

    /// <summary>
    /// item布局
    /// </summary>
    /// <returns></returns>
    void layoutItem()
    {
        if (this.itemLineList == null) return;
        int count = this.itemLineList.Count;
        for (int i = 0; i < count; ++i)
        {
            List<GameObject> itemList = this.itemLineList[i];
            int length = itemList.Count;
            for (int j = 0; j < length; j++)
			{
                GameObject item = itemList[j];
                if (!this.isHorizontal)
                    item.transform.localPosition = new Vector3(this.prevItemPos.x + (this.itemWidth + this.gapH) * j, 
                                                               this.prevItemPos.y - (this.itemHeight + this.gapV) * i);
                else
                    item.transform.localPosition = new Vector3(this.prevItemPos.x + (this.itemWidth + this.gapH) * i, 
                                                               this.prevItemPos.y - (this.itemHeight + this.gapV) * j);
			}
            
        }
    }

    void Update()
    {
        this.updateItem();
    }

    /// <summary>
    /// 删除多余的item
    /// </summary>
    /// <param name="count">当前应该显示的数量</param>
    /// <returns></returns>
    void removeOverItem(int count)
    {
        if (this.itemLineList != null &&
            this.itemLineList.Count > 0)
        {
            //删除多余的item
            if (count < this.showLineCount)
            {
                //删除 this.showCount - count 个 item
                for (int i = this.showLineCount - 1; i >= count; --i)
                {
                    List<GameObject> itemList = this.itemLineList[i];
                    int length = itemList.Count;
                    for (int j = 0; j < length; ++j)
			        {
                        GameObject item = itemList[j];
                        if (item != null)
                        {
                            GameObject.Destroy(item);
                            itemList.RemoveAt(j);
                        }
			        }
                    this.itemLineList.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// 更新边界
    /// </summary>
    /// <returns></returns>
    void updateBorder()
    {
        //上下
        this.top = this.itemHeight + this.gapV;
        this.bottom = -(this.itemHeight + this.gapV) * (this.showLineCount - 1);
        //左右
        this.left = -this.itemWidth - this.gapH;
        this.right = (this.itemWidth + this.gapH) * (this.showLineCount - 1);
    }

    /// <summary>
    /// 删除所有item
    /// </summary>
    /// <returns></returns>
    public void removeAllItem()
    {
        if (this.itemLineList == null) return;
        int count = this.itemLineList.Count;
        for (int i = 0; i < count; ++i)
        {
            List<GameObject> itemList = this.itemLineList[i];
            int length = itemList.Count;
            for (int j = 0; j < length; ++j)
            {
                GameObject item = itemList[j];
                if (item != null) MonoBehaviour.Destroy(item);
            }
        }
    }
}
