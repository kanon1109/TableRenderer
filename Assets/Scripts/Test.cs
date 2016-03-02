using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Test : MonoBehaviour
{
    public GameObject list;
    private List<TestVo> datalist;
	// Use this for initialization
	void Start () 
    {
        this.datalist = new List<TestVo>();
        for (int i = 0; i < 60; ++i)
        {
            TestVo tVo = new TestVo();
            tVo.name = "name" + i;
            this.datalist.Add(tVo);
        }
        TableRenderer lr = this.list.GetComponent<TableRenderer>();
        lr.init(false, datalist.Count, 4, 10, 10, updateTableItem);
	}

    private void updateTableItem(GameObject item, int index, bool isReload)
    {

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