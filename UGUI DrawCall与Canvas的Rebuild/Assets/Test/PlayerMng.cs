using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerMng : MonoBehaviour
{
    Dictionary<int, Player> m_Players = new Dictionary<int, Player>();
    int m_nID = 0;
    int m_nMainID = 0;
    // Use this for initialization
    bool m_bShowHurt = false;
    float m_fLastShowTime = 0.0f;
    void Start ()
    {
        HUDMesh.OnEnterGame();  // 启动游戏场景后调用
    }
    void OnDestory()
    {
        HUDMesh.OnLeaveGame();
    }

    // 添加一个测试玩家
    void  AddPlayer()
    {
        ++m_nID;
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = "Player" + m_nID.ToString();
        obj.transform.position = RandomPos();
        obj.transform.localScale = new Vector3(0.2f, 1.0f, 0.2f);
        Player p = obj.AddComponent<Player>();
        p.m_bMain = m_Players.Count == 0;
        p.m_nID = m_nID;
        m_Players[m_nID] = p;
        if (p.m_bMain)
            m_nMainID = m_nID;
    }
    void  DelPlayer()
    {
        foreach(var p in m_Players)
        {
            GameObject.DestroyObject(p.Value.gameObject);
        }
        m_Players.Clear();
    }

    Nordeus.DataStructures.VaryingIntList mIndics = new Nordeus.DataStructures.VaryingIntList();

    void  TestList()
    {
        mIndics.Add(1);
        mIndics.Add(2);
        mIndics.Add(3);
        int  []buffer1 = mIndics.ToArray();
        int nLen1 = buffer1.Length;
        mIndics.AsArrayOfLength((ulong)mIndics.size);
        int[] buffer2 = mIndics.ToArray();
        int nLen2 = buffer2.Length;
        int iii = 0;
    }

    Vector3 RandomPos()
    {
        Vector3 vPos = Vector3.zero;

        vPos.x = Random.Range(-5, 5);
        vPos.z = Random.Range(-5, 5);

        return vPos;
    }

    void ShowHurt()
    {
        // 先显示主角的伤害
        foreach (var p in m_Players)
        {
            if (p.Value.m_bMain)
            {
                //for(int i = 0; i<5;++i)
                {
                    int nHurtHP = Random.Range(100, 50000);
                    int nExp = Random.Range(1000, 5000);
                    p.Value.ShowHurt(nHurtHP);
                    p.Value.ShowExp(nExp);
                }
            }
            else
            {
                //for (int i = 0; i < 5; ++i)
                {
                    int nHurtHP = Random.Range(100, 50000);
                    p.Value.ShowHurt(nHurtHP);
                }
            }
        }
    }
    void ShowExp()
    {

    }
	
	// Update is called once per frame
	void Update ()
    {
        if(m_bShowHurt)
        {
            float fNow = Time.time;
            if(m_fLastShowTime + 0.3f < fNow)
            {
                m_fLastShowTime = fNow;
                ShowHurt();
                ShowExp();
            }
        }
	}

    void OnGUI()
    {
        float fLeft = 10.0f;
        float fTop = 10.0f;

        if (GUI.Button(new Rect(fLeft, fTop, 100.0f, 20.0f), "添加角色"))
        {
            AddPlayer();
        }
        fLeft += 110;
        if (GUI.Button(new Rect(fLeft, fTop, 100.0f, 20.0f), "删除角色"))
        {
            DelPlayer();
        }
        fLeft += 110;
        if (GUI.Button(new Rect(fLeft, fTop, 100.0f, 20.0f), m_bShowHurt ? "停止显示" : "显示伤害"))
        {
            m_bShowHurt = !m_bShowHurt;
        }
        fLeft += 110;
        if (GUI.Button(new Rect(fLeft, fTop, 100.0f, 20.0f), "测试List"))
        {
            TestList();
        }
    }
}
