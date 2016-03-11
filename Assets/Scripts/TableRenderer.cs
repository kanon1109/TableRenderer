using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 无线滚动table 
/// TODO 
/// 已完成
/// 【创建补全item】
/// 【删除item时在同一排的item 判断隐藏】
/// 【上滚动时判断最后一行需要隐藏的item】
/// 【下滚动时判断第一行需要显示的item】
/// 【根据item的index 跳转滚动容器的位置】
/// 【滚动至最后一排 reloadData 新增数量时 不显示增加新的item （无论content 是否大于一屏）】
/// 【如果content内容大于一屏 滚动至最后一排后 reloadData 减少数量时 没隐藏多余的item （ curLastLineItemIndex 未更新）】
/// 【定位后滚动没隐藏多余的item】
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
    //当前最后一行的索引(范围 0 - (showLineCount - 1))
    private int curLastLineIndex = -1;
    //当前最后一行最后一个item位置的索引(范围 0 - (lineItemCount - 1))
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
    private RectTransform contentRectTf;
    //滚动组件
    private ScrollRect sr;
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
        this.sr = this.scroll.GetComponent<ScrollRect>();
        this.sr.horizontal = this.isHorizontal;
        this.sr.vertical = !this.isHorizontal;
        this.gapH = gapH;
        this.gapV = gapV;
        this.listWidth = this.scroll.GetComponent<RectTransform>().sizeDelta.x;
        this.listHeight = this.scroll.GetComponent<RectTransform>().sizeDelta.y;
        this.itemWidth = this.itemPrefab.GetComponent<RectTransform>().sizeDelta.x;
        this.itemHeight = this.itemPrefab.GetComponent<RectTransform>().sizeDelta.y;

        Vector2 v2;
        if(!this.isHorizontal)
            v2 = new Vector2((this.itemWidth + gapV) * lineItemCount, this.scroll.GetComponent<RectTransform>().sizeDelta.y);
        else
            v2 = new Vector2(this.scroll.GetComponent<RectTransform>().sizeDelta.x, (this.itemHeight + gapH) * lineItemCount);

        this.scroll.GetComponent<RectTransform>().sizeDelta = v2;
        
        this.contentRectTf = this.content.GetComponent<RectTransform>();
        this.contentRectTf.sizeDelta = new Vector2(listWidth, listHeight);
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
        List<GameObject> itemList = this.itemLineList[this.curLastLineIndex];
        int length = itemList.Count;
        //计算补全的数量
        int supplementCount = length - (this.curLastLineItemIndex + 1);
        if (supplementCount > showCreateCount) supplementCount = showCreateCount;
        int createdCount = 0;

        //找到补全的数量
        for (int i = 0; i < supplementCount; ++i)
        {
            this.curLastLineItemIndex++;
            GameObject item = itemList[this.curLastLineItemIndex];
            item.SetActive(true);
            createdCount++;
        }

        //判断补全后是否满一排，如果满了 curLastLineItemIndex 归零，curLastLineIndex累加
        if (this.curLastLineItemIndex >= this.lineItemCount - 1)
            this.curLastLineIndex++;

        //创建剩余的数量
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
            GameObject item = itemList[0];
            int itemListLength = itemList.Count;
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
                        Transform lastItemTf = lastItem.transform;
                        for (int j = 0; j < itemListLength; j++)
                        {
                            //当前第i排的所有item
                            GameObject curLineItem = itemList[j];
                            Transform curLineItemTf = curLineItem.transform;
                            curLineItemTf.localPosition = new Vector3(curLineItemTf.localPosition.x,
                                                                      lastItemTf.localPosition.y - this.itemHeight - this.gapV);
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
                            Transform curLineItemTf = curLineItem.transform;
                            curLineItemTf.localPosition = new Vector3(curLineItemTf.localPosition.x,
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
                        Transform firstItemTf = firstItem.transform;
                        for (int j = 0; j < itemListLength; j++)
                        {
                            //当前第i排的所有item
                            GameObject curLineItem = itemList[j];
                            Transform curLineItemTf = curLineItem.transform;
                            curLineItem.SetActive(true);
                            curLineItemTf.localPosition = new Vector3(curLineItemTf.localPosition.x,
                                                                      firstItemTf.localPosition.y + this.itemHeight + this.gapV);
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
                            Transform curLineItemTf = curLineItem.transform;
                            curLineItem.SetActive(true);
                            curLineItemTf.localPosition = new Vector3(curLineItemTf.localPosition.x,
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
                        Transform lastItemTf = lastItem.transform;
                        for (int j = 0; j < itemListLength; j++)
                        {
                            //当前第i排的所有item
                            GameObject curLineItem = itemList[j];
                            Transform curLineItemTf = curLineItem.transform;
                            curLineItemTf.localPosition = new Vector3(lastItemTf.localPosition.x + this.itemWidth + this.gapH,
                                                                      curLineItemTf.localPosition.y);
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
                            Transform curLineItemTf = curLineItem.transform;
                            curLineItemTf.localPosition = new Vector3(this.itemWidth + this.gapH,
                                                                      curLineItemTf.localPosition.y);
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
                        Transform firstItemTf = firstItem.transform;
                        for (int j = 0; j < itemListLength; j++)
                        {
                            //当前第i排的所有item
                            GameObject curLineItem = itemList[j];
                            Transform curLineItemTf = curLineItem.transform;
                            curLineItem.SetActive(true);
                            curLineItemTf.localPosition = new Vector3(firstItemTf.localPosition.x - this.itemWidth - this.gapH,
                                                                      curLineItemTf.localPosition.y);
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
                            Transform curLineItemTf = curLineItem.transform;
                            curLineItem.SetActive(true);
                            curLineItemTf.localPosition = new Vector3(this.itemWidth + this.gapH,
                                                                      curLineItemTf.localPosition.y);
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
        //先更新lineIndex
        this.updateCurLineItemIndex(count);
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
        int createLineCount = this.showLineCount - prevShowLineCount;
        //根据显示数量创建item
        this.createItem(this.itemPrefab, createLineCount, count);
        this.totalCount = count;
        //更新itemIndex
        this.updateCurLineItemIndex(count);
        //更新item的显示
        this.updateLineItemActive();
        //防止item不显示bug
        this.fixContentItemActive();
        //更新滚动范围
        this.updateBorder();
        //总数更新content大小
        this.updateContentSize();
        //修正content的位置
        this.fixContentPos();
        //布局
        this.layoutItem();
        //重新调用回调
        this.reloadItem(true);
        this.isReload = true;
    }

    /// <summary>
    /// 根据创建的总数更新content的大小
    /// </summary>
    private void updateContentSize()
    {
        if (!this.isHorizontal)
            this.contentRectTf.sizeDelta = new Vector2(this.contentRectTf.sizeDelta.x, this.totalLineCount * (this.itemHeight + this.gapV));
        else
            this.contentRectTf.sizeDelta = new Vector2(this.totalLineCount * (this.itemWidth + this.gapH), this.contentRectTf.sizeDelta.y);
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
            //print("范围:" + this.curIndex + "--------" + (this.curIndex + this.showCount));
            for (int i = this.curLineIndex; i < this.curLineIndex + this.showLineCount; ++i)
            {
                if (this.itemLineList[index] != null)
                {
                    List<GameObject> itemList = this.itemLineList[index];
                    int length = itemList.Count;
                    for (int j = 0; j < length; j++)
                    {
                        int itemIndex = i * this.lineItemCount + j;
                        if (itemIndex <= this.totalCount - 1)
                        {
                            GameObject item = itemList[j];
                            if (this.m_updateItem != null)
                                this.m_updateItem.Invoke(item, itemIndex, isReload);
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
                Transform prevItemTf = prevItem.transform;
                List<GameObject> itemList = this.itemLineList[i];
                int length = itemList.Count;
                for (int j = 0; j < length; j++)
                {
                    GameObject item = itemList[j];
                    Transform itemTf = item.transform;
                    if (!this.isHorizontal)
                    {
                        itemTf.localPosition = new Vector3(itemTf.localPosition.x,
                                                            prevItemTf.localPosition.y - this.itemHeight - this.gapV);
                    }
                    else
                    {
                        itemTf.localPosition = new Vector3(prevItemTf.localPosition.x + this.itemWidth + this.gapH,
                                                           itemTf.localPosition.y);
                    }
                }
                prevItemList = this.itemLineList[i];
            }
        }
    }

    /// <summary>
    /// 修正content的位置
    /// </summary>
    private void fixContentPos()
    {
        if (!this.isHorizontal)
        {
            //防止数量减少后content的位置在遮罩上面
            if (this.contentRectTf.sizeDelta.y <= this.listHeight)
            {
                //如果高度不够但content顶部超过scroll的顶部则content顶部归零对齐
                if (this.contentRectTf.localPosition.y > 0)
                    this.contentRectTf.localPosition = new Vector3(this.contentRectTf.localPosition.x, 0);
            }
            else
            {
                //如果高度足够但content底部超过scroll的底部则content底部对齐scroll的底部
                if (this.contentRectTf.localPosition.y - this.contentRectTf.sizeDelta.y > -this.listHeight)
                    this.contentRectTf.localPosition = new Vector3(this.contentRectTf.localPosition.x,
                                                                    -this.listHeight + this.contentRectTf.sizeDelta.y);
            }
        }
        else
        {
            //防止数量减少后content的位置在遮罩左面
            if (this.contentRectTf.sizeDelta.x <= this.listWidth)
            {
                if (this.contentRectTf.localPosition.x < 0)
                    this.contentRectTf.localPosition = new Vector3(0, this.contentRectTf.localPosition.y);
            }
            else
            {
                if (this.contentRectTf.localPosition.x + this.contentRectTf.sizeDelta.x < this.listWidth)
                    this.contentRectTf.localPosition = new Vector3(this.listWidth - this.contentRectTf.sizeDelta.x,
                                                                    this.contentRectTf.localPosition.y);
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
            if (totalLineCount < this.showLineCount)
            {
                //删除 this.showCount - count 个 item
                for (int i = this.showLineCount - 1; i >= totalLineCount; --i)
                {
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
    /// 根据索引滚动到相应位置
    /// </summary>
    /// <param name=index>item索引</param>
    /// <returns></returns>
    public void rollPosByIndex(int targetIndex)
    {
        if (this.itemLineList == null ||
            this.itemLineList.Count == 0) return;
        this.isReload = false;
        this.sr.StopMovement();
        if (targetIndex < 0) targetIndex = 0;
        if (targetIndex > this.totalCount - 1) targetIndex = this.totalCount - 1;
        Vector3 contentPos = this.contentRectTf.localPosition;
        //根据targetIndex计算出当前排的索引
        int targetLineIndex = Mathf.CeilToInt((float)(targetIndex + 1) / (float)lineItemCount) - 1;
        this.curLineIndex = targetLineIndex;
        //计算出第一个索引是多少， 因为第一个curLineIndex不一定是targetLineIndex 
        if (targetLineIndex + this.showLineCount > this.totalLineCount)
            this.curLineIndex -= targetLineIndex + this.showLineCount - this.totalLineCount;
        float gap;
        if (!this.isHorizontal)
        {
            gap = this.itemHeight + this.gapV;
            this.prevItemPos.y = -gap * this.curLineIndex; //算出移动的距离
            contentPos.y = gap * targetLineIndex;
        }
        else
        {
            gap = this.itemWidth + this.gapH;
            this.prevItemPos.x = gap * this.curLineIndex; //算出移动的距离
            contentPos.x = -gap * targetLineIndex;
        }
        this.contentRectTf.localPosition = contentPos;
        this.layoutItem();
        this.reloadItem(true);
        this.updateLineItemActive();
        this.fixContentPos();
        this.isReload = true;
    }

    /// <summary>
    /// 这是一个防止设置content子对象的active时 因为content不动的情况下item还是不显示
    /// </summary>
    private void fixContentItemActive()
    {
        //奇淫技巧 防止因为 content 静止不动时 item.SetActive(true); 还是不显示的bug
        Vector3 lp = this.contentRectTf.localPosition;
        this.contentRectTf.localPosition = new Vector3(lp.x + .001f, lp.y + .001f);
        this.contentRectTf.localPosition = lp;
    }

    /// <summary>
    /// 更新item的显示状态
    /// </summary>
    private void updateLineItemActive()
    {
        if (this.itemLineList == null) return;
        if (this.itemLineList.Count == 0) return;
        for (int i = 0; i < this.showLineCount; i++)
        {
            List<GameObject> itemList = this.itemLineList[i];
            for (int j = 0; j < this.lineItemCount; ++j)
            {
                GameObject item = itemList[j];
                if (this.totalLineCount <= this.showLineCount) //排数在一屏以内
                {
                    if (i < this.showLineCount - 1) //前几排全部显示
                    {
                        item.SetActive(true);
                    }
                    else
                    {
                        if (j <= this.curLastLineItemIndex)  //最后一排判断可显示的item
                            item.SetActive(true);
                        else 
                            item.SetActive(false);
                    }
                }
                else //排数超过一屏
                {
                    //判断最后一排是否在显示范围内
                    if (this.curLineIndex < this.totalLineCount - this.showLineCount)
                    {
                        item.SetActive(true);
                    }
                    else
                    {
                        if (i < this.showLineCount - 1) //前几排全部显示
                        {
                            item.SetActive(true);
                        }
                        else
                        {
                            if (j <= this.curLastLineItemIndex)  //最后一排判断可显示的item
                                item.SetActive(true);
                            else
                                item.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 更新当前总数更新最后一排的最后一个item的索引位置
    /// </summary>
    /// <param name="count">当前应该显示的item总数</param>
    private void updateCurLineItemIndex(int count)
    {
        if (this.itemLineList == null || 
            this.itemLineList.Count == 0)
        {
            this.curLastLineIndex = -1;
            this.curLastLineItemIndex = -1;
            return;
        }
        //标记最后line的Index
        this.curLastLineIndex = this.itemLineList.Count - 1;
        this.curLastLineItemIndex = count % this.lineItemCount;
        if (this.curLastLineItemIndex == 0)
            this.curLastLineItemIndex = this.lineItemCount - 1;
        else
            this.curLastLineItemIndex--;
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
