using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Button btn;
    public Button addBtn;
    public GameObject list;
    private List<TestVo> datalist;
	// Use this for initialization
	void Start () 
    {
        this.addBtn.onClick.AddListener(addBtnHandler);
        this.btn.onClick.AddListener(btnHandler);

        this.datalist = new List<TestVo>();
        for (int i = 0; i < 47; ++i)
        {
            TestVo tVo = new TestVo();
            tVo.name = "name" + i;
            this.datalist.Add(tVo);
        }
        TableRenderer tr = this.list.GetComponent<TableRenderer>();

        tr.init(false, datalist.Count, 4, 10, 10, updateTableItem);
	}

    private void btnHandler()
    {
        int index = Random.Range(0, this.datalist.Count - 1);
        print("跳转到index : " + index);
        this.list.GetComponent<TableRenderer>().rollPosByIndex(index);
    }

    private void updateTableItem(GameObject item, int index, bool isReload)
    {
        TestVo tVo = this.datalist[index];
        if (!isReload && item.GetComponent<TableItem>().index == index) return;
        item.GetComponent<TableItem>().index = index;
        item.GetComponent<TableItem>().txt.text = tVo.name;
    }

    private void addBtnHandler()
    {
        this.datalist = new List<TestVo>();
        int count = Random.Range(0, 90);
        //count = 45;
        print("新列表数量count = " + count);
        for (int i = 1; i <= count; i++)
        {
            TestVo tVo = new TestVo();
            tVo.name = "name" + this.datalist.Count;
            this.datalist.Add(tVo);
        }
        TableRenderer tr = this.list.GetComponent<TableRenderer>();
        tr.reloadData(this.datalist.Count);
    }
	
	// Update is called once per frame
	void Update ()
    {
	
	}
}

public class TestVo : Object
{
    public string name;
    public TestVo()
    {

    }
}