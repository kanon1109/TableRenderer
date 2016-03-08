using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 无线滚动table 
/// TODO 
/// 【创建补全item】
/// 【删除item时在同一排的item 判断隐藏】
/// 上滚动时判断最后一行需要隐藏的item
/// 下滚动时判断第一行需要显示的item
/// 根据item的index 跳转滚动容器的位置
/// </summary>
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
    //当前显示的排数
    private int showLineCount = 0;
    //当前第一排的索引
    private int curLineIndex = 0;
    //总的多少行或者多少列
    private int totalLineCount;
    //总的数据数量（虚拟数据 并非实际创建的item数量）
    private int totalCount = 0;
    //当前最后一行的索引
    private int curLastLineIndex = -1;
    //当前最后一行最后一个item位置的索引
    private int curLastLineItemIndex = -1;
    //底部位置
    private float bottom;
    //顶部位置
    private float top;
    //左边位置
    private float left;
    //右边位置
    private float right;
    //content的transform组件
    private RectTransform rectTransform;
    //最后一排item的索引位置
    private int lastItemLindex = 0;
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
        this.rectTransform = this.content.GetComponent<RectTransform>();
        this.rectTransform.sizeDelta = new Vector2(listWidth, listHeight);
        this.content.transform.localPosition = new Vector3(0, 0);
        this.prevItemPos = new Vector2();
        this.contentStartPos = this.scroll.transform.localPosition;
        //一行或者一列的数量
        this.lineItemCount = lineItemCount;
        print("一排创建" + this.lineItemCount + "个");
        this.reloadData(count);
        this.isReload = true;
    }

    /// <summary>
    /// 创建item
    /// </summary>
    /// <param name="prefab">需要创建item的预设</param>
    /// <param name="createLineCount">创建的行(列)数</param>
    /// <param name="count">需要创建item的个数</param>
    private void createItem(GameObject prefab, int createLineCount, int count)
    {
        //createLineCount = 0 表示一行也不创建 只补全一排item
        if (createLineCount < 0) return;
        if (count <= 0) return;
        if (this.itemLineList == null)
            this.itemLineList = new List<List<GameObject>>();
        //表示相同一排内 删除或增加 item
        int showCreateCount = count - this.totalCount; //实际显示的数量
        if (showCreateCount <= 0) return;
        //没有创建过
        for (int i = 0; i < createLineCount; ++i)
        {
            this.itemLineList.Add(new List<GameObject>());
        }
        //如果没有创建过
        if(this.curLastLineIndex == -1)
        {
            this.curLastLineIndex = 0;
            this.curLastLineItemIndex = 0;
        }

        print("prev this.curLastLineIndex" + this.curLastLineIndex);

        List<GameObject> itemList = this.itemLineList[this.curLastLineIndex];
        int length = itemList.Count;
        //计算补全的数量
        int supplementCount = length - (this.curLastLineItemIndex + 1);
        if (supplementCount > showCreateCount) supplementCount = showCreateCount;
        int createdCount = 0;
        //TODO 找到补全的数量
        for (int i = 0; i < supplementCount; ++i)
        {
            this.curLastLineItemIndex++;
            GameObject item = itemList[this.curLastLineItemIndex];
            item.SetActive(true);
            createdCount++;
        }

        //TODO判断补全后是否满一排，如果满了 curLastLineItemIndex 归零，curLastLineIndex累加
        if (this.curLastLineItemIndex >= this.lineItemCount - 1)
            this.curLastLineIndex++;

        print("this.curLastLineIndex" + this.curLastLineIndex);
        //TODO创建剩余的数量
        for (int i = 0; i < createLineCount; ++i)
        {
            itemList = this.itemLineList[this.curLastLineIndex];
            this.curLastLineIndex++;
            for (int j = 0; j < this.lineItemCount; ++j)
            {
                GameObject item = MonoBehaviour.Instantiate(prefab, new Vector3(0, 0), new Quaternion()) as GameObject;
                item.transform.SetParent(this.content.gameObject.transform);
                item.transform.localScale = new Vector3(1, 1, 1);
                itemList.Add(item);
                createdCount++;
                //最后一排
                if (createdCount > showCreateCount)
                    item.SetActive(false);
            }
        }

        //TODO标记最后位置
        this.curLastLineItemIndex = count % this.lineItemCount;
        if (this.curLastLineItemIndex == 0) 
            this.curLastLineItemIndex = this.lineItemCount - 1;
        else 
            this.curLastLineItemIndex--;
        print("this.curLastLineItemIndex" + this.curLastLineItemIndex);
        this.curLastLineIndex = this.itemLineList.Count - 1;

    }

    /// <summary>
    /// 更新item
    /// </summary>
    /// <returns></returns>
    private void updateItem()
    {
        if (!this.isReload) return;
        if (this.itemLineList == null) return;
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
                        print("this.curLineIndex" + this.curLineIndex);
                        print("this.totalLineCount - 1 = " + (this.totalLineCount - 1));
                        for (int j = 0; j < itemListLength; j++)
                        {
                            //当前第i排的所有item
                            GameObject curLineItem = itemList[j];
                            curLineItem.transform.localPosition = new Vector3(curLineItem.transform.localPosition.x,
                                                                              lastItem.transform.localPosition.y - this.itemHeight - this.gapV);
                            //隐藏最后一排缺少的item
                            if (this.curLineIndex == this.totalLineCount - this.showLineCount - 1 && 
                                j > this.curLastLineItemIndex) 
                                curLineItem.SetActive(false);
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
                            if (this.curLineIndex == this.totalLineCount - this.showLineCount - 1 && 
                                j > this.curLastLineItemIndex)
                                curLineItem.SetActive(false);
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
                        for (int j = 0; j < itemListLength; j++)
                        {
                            //当前第i排的所有item
                            GameObject curLineItem = itemList[j];
                            curLineItem.SetActive(true);
                            curLineItem.transform.localPosition = new Vector3(curLineItem.transform.localPosition.x,
                                                                              firstItem.transform.localPosition.y + this.itemHeight + this.gapV);
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
                            curLineItem.SetActive(true);
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
                    //往左拖动时
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
                            //隐藏最后一排缺少的item
                            if (this.curLineIndex == this.totalLineCount - this.showLineCount - 1 &&
                                j > this.curLastLineItemIndex)
                                curLineItem.SetActive(false);
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
                            //隐藏最后一排缺少的item
                            if (this.curLineIndex == this.totalLineCount - this.showLineCount - 1 &&
                                j > this.curLastLineItemIndex)
                                curLineItem.SetActive(false);
                        }
                        this.curLineIndex = 0;
                    }
                    break;
                }
                else if (posX > this.right && this.curLineIndex > 0)
                {
                    //往右拖动时
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
                            curLineItem.SetActive(true);
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
                            curLineItem.SetActive(true);
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
        this.reloadItem();
        this.fixItemPos();
    }

    /// <summary>
    /// 重新设置数据
    /// </summary>
    /// <param name="count">当前数据列表的数量</param>
    /// <returns></returns>
    public void reloadData(int count)
    {
        this.isReload = false;
        //保存上一次第一个item的位置
        if (this.itemLineList != null &&
            this.itemLineList.Count > 0)
        {
            List<GameObject> itemList = this.itemLineList[0];
            GameObject item = itemList[0];
            this.prevItemPos.x = item.transform.localPosition.x;
            this.prevItemPos.y = item.transform.localPosition.y;
        }
        //判断 当前删除的index 是否在 this.curIndex , this.curIndex + this.showCount 之间。
        //当前显示出来的最后一个item的index
        int curLastLineIndex = this.curLineIndex + this.showLineCount - 1;
        //总的最大index
        //总的行数或者列数
        int lastLineIndex = Mathf.CeilToInt((float)count / (float)lineItemCount) - 1;
        //防止当前显示的数量溢出
        if (this.curLineIndex > 0 && curLastLineIndex > lastLineIndex)
        {
            //获取溢出数量
            int overLineCount = curLastLineIndex - lastLineIndex;
            this.curLineIndex -= overLineCount;
            //补全位置
            if (!isHorizontal)
                this.prevItemPos.y += (this.itemHeight + this.gapV) * overLineCount;
            else
                this.prevItemPos.x -= (this.itemWidth + this.gapH) * overLineCount;
            //防止去除溢出后 索引为负数。
            if (this.curLineIndex < 0) this.curLineIndex = 0;
        }
        //总的行数或者列数
        this.totalLineCount = Mathf.CeilToInt((float)count / (float)lineItemCount);
        //保存上一次显示的数量
        int prevShowLineCount = this.showLineCount;
        //判断当前多出来的排，并删除。
        this.removeOverItem(this.totalLineCount, count);
        //纵向 计算应该显示的排数
        if (!this.isHorizontal) 
            this.showLineCount = (int)(Mathf.Ceil(this.listHeight / (this.itemHeight + this.gapV))); 
        else
            this.showLineCount = (int)(Mathf.Ceil(this.listWidth / (this.itemWidth + this.gapH)));
        //需要创建的数量不大于实际数量
        if (this.totalLineCount <= this.showLineCount)
            this.showLineCount = this.totalLineCount; //取实际数据的数量
        else
            this.showLineCount += 1; //取计算的数量 + 1
        //创建数量
        print("可显示的行数 " + this.showLineCount);
        int createLineCount = this.showLineCount - prevShowLineCount;
        print("需要创建的行数 " + createLineCount);
        //根据显示数量创建item
        this.createItem(this.itemPrefab, createLineCount, count);
        this.totalCount = count;
        this.updateBorder();
        if (!this.isHorizontal)
            this.rectTransform.sizeDelta = new Vector2(this.rectTransform.sizeDelta.x, this.totalLineCount * (this.itemHeight + this.gapV));
        else
            this.rectTransform.sizeDelta = new Vector2(this.totalLineCount * (this.itemWidth + this.gapH), this.rectTransform.sizeDelta.y);
        this.layoutItem();
        //重新调用回调
        this.reloadItem(true);
        this.fixItemPos();
        this.isReload = true;
    }

    /// <summary>
    /// item布局
    /// </summary>
    /// <returns></returns>
    private void layoutItem()
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

    /// <summary>
    /// 重新调用item的回调
    /// </summary>
    /// <returns></returns>
    private void reloadItem(bool isReload = false)
    {
        if (this.itemLineList == null) return;
        if (this.itemLineList.Count > 0)
        {
            int index = 0;
            int count = 0;
            //print("范围:" + this.curIndex + "--------" + (this.curIndex + this.showCount));
            for (int i = this.curLineIndex; i < this.curLineIndex + this.showLineCount; ++i)
            {
                if (this.itemLineList[index] != null)
                {
                    List<GameObject> itemList = this.itemLineList[index];
                    int length = itemList.Count;
                    for (int j = 0; j < length; j++)
                    {
                        count++;
                        if (count <= this.totalCount)
                        {
                            GameObject item = itemList[j];
                            if (this.m_updateItem != null)
                                this.m_updateItem.Invoke(item, j, isReload);
                        }
                    }
                    index++;
                }
            }
        }
    }

    /// <summary>
    /// 拖动时修正位置
    /// </summary>
    /// <returns></returns>
    private void fixItemPos()
    {
        if (this.itemLineList == null) return;
        //拖动时修正位置
        if (this.itemLineList.Count > 0)
        {
            List<GameObject> prevItemList = this.itemLineList[0];
            if (this.curLineIndex == 0)
            {
                int length = prevItemList.Count;
                for (int j = 0; j < length; j++)
                {
                    GameObject prevItem = prevItemList[j];
                    if (!this.isHorizontal)
                        prevItem.transform.localPosition = new Vector3(prevItem.transform.localPosition.x, 0);
                    else
                        prevItem.transform.localPosition = new Vector3(0, prevItem.transform.localPosition.y);
                }
            }
            for (int i = 1; i < this.itemLineList.Count; ++i)
            {
                //上一排的第一个做为定位
                GameObject prevItem = prevItemList[0];
                List<GameObject> itemList = this.itemLineList[i];
                int length = itemList.Count;
                for (int j = 0; j < length; j++)
                {
                    GameObject item = itemList[j];
                    if (!this.isHorizontal)
                    {
                        item.transform.localPosition = new Vector3(item.transform.localPosition.x,
                                                                   prevItem.transform.localPosition.y - this.itemHeight - this.gapV);
                    }
                    else
                    {
                        item.transform.localPosition = new Vector3(prevItem.transform.localPosition.x + this.itemWidth + this.gapH,
                                                                   item.transform.localPosition.y);
                    }
                }
                prevItemList = this.itemLineList[i];
            }
        }
    }

    void Update()
    {
        this.updateItem();
    }

    /// <summary>
    /// 删除多余的一排item
    /// </summary>
    /// <param name="totalLineCount">当前应该显示的总排数</param>
    /// <param name="count">当前应该显示的数量</param>
    /// <returns></returns>
    private void removeOverItem(int totalLineCount, int count)
    {
        if (this.itemLineList != null &&
            this.itemLineList.Count > 0)
        {
            List<GameObject> itemList;
            int length;
            //删除多余的排
            print("总的行数 = " + totalLineCount);
            print("可以显示的行数 " + showLineCount);
            if (totalLineCount < this.showLineCount)
            {
                //删除 this.showCount - count 个 item
                for (int i = this.showLineCount - 1; i >= totalLineCount; --i)
                {
                    print("删除第" + i + "排");
                    itemList = this.itemLineList[i];
                    length = itemList.Count;
                    for (int j = length - 1; j >= 0; --j)
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

                //标记最后位置
                this.curLastLineIndex = this.itemLineList.Count - 1;
                this.curLastLineItemIndex = this.lineItemCount;
            }

            //隐藏一排内的item
            if (totalLineCount <= this.showLineCount && 
                count < this.totalCount && 
                this.itemLineList.Count > 0)
            {
                //TODO标记最后位置
                this.curLastLineIndex = this.itemLineList.Count - 1;
                //最后一排item的数量
                this.curLastLineItemIndex = count % this.lineItemCount;
                if (this.curLastLineItemIndex == 0)
                    this.curLastLineItemIndex = this.lineItemCount - 1;
                else
                    this.curLastLineItemIndex--;
                print("删除后最好一排索引" + this.curLastLineIndex);
                print("删除后最好一排最后一个索引" + this.curLastLineItemIndex);
                //TODO 隐藏一排内多余的item
                itemList = this.itemLineList[this.curLastLineIndex];
                length = itemList.Count;
                for (int i = this.curLastLineItemIndex + 1; i < length; i++)
                {
                    GameObject item = itemList[i];
                    item.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 更新边界
    /// </summary>
    /// <returns></returns>
    private void updateBorder()
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
