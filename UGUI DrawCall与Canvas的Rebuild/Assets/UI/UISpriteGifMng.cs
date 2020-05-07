using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using UnityEngine;

///////////////////////////////////////////////////////////
//
//  Written by              ：laishikai
//  Copyright(C)            ：成都博瑞梦工厂
//  ------------------------------------------------------
//  功能描述                ：精灵帧动画
//
///////////////////////////////////////////////////////////

[XmlRootAttribute("Root")]
public class UISpriteGifConfig : ISerializable
{
    [XmlElementAttribute("GifList")]
    public List<UIGifXMLList> gifList = new List<UIGifXMLList>();
    public void Serialize(CSerialize ar)
    {
        ar.SerializeStructArray<UIGifXMLList>(ref gifList);
    }
}

[XmlRootAttribute("GifList")]
public class UIGifXMLList : ISerializable
{
    [XmlAttribute("ID")]
    public int ID;
    [XmlAttribute("Width")]
    public int Width;
    [XmlAttribute("Height")]
    public int Height;
    [XmlElementAttribute("Frame")]
    public List<UIGifXMLFrame> frame = new List<UIGifXMLFrame>();

    public void Serialize(CSerialize ar)
    {
        ar.ReadWriteValue(ref ID);
        ar.ReadWriteValue(ref Width);
        ar.ReadWriteValue(ref Height);
        ar.SerializeStructArray<UIGifXMLFrame>(ref frame);
    }
}
[XmlRootAttribute("Frame")]
public class UIGifXMLFrame : ISerializable
{
    [XmlAttribute("SpriteName")]
    public string SpriteName;

    [XmlAttribute("fNextGapTime")]
    public float fNextGapTime;

    [XmlAttribute("OffsetX")]
    public int OffsetX;

    [XmlAttribute("OffsetY")]
    public int OffsetY;

    [XmlAttribute("ScaleWidth")]
    public int ScaleWidth;

    [XmlAttribute("ScaleHeight")]
    public int ScaleHeight;

    public void Serialize(CSerialize ar)
    {
        ar.ReadWriteValue(ref SpriteName);
        ar.ReadWriteValue(ref fNextGapTime);
        ar.ReadWriteValue(ref OffsetX);
        ar.ReadWriteValue(ref OffsetY);
        ar.ReadWriteValue(ref ScaleWidth);
        ar.ReadWriteValue(ref ScaleHeight);
    }
}

public struct UISpriteGifFrame
{
    public float m_fNextGapTime;  // 到下一帧的间隔时间
    public int m_nOffsetX;   // 偏移X
    public int m_nOffsetY;   // 
    public int m_nScaleWidth;  // 以100为单位, 百分比
    public int m_nScaleHeight; // 
    //public int m_nSpriteID;    // 精灵ID

    private int m_nChangeSpriteID;
    private string m_SpriteName;

    public int m_nSpriteID
    {
        get
        {
            if(m_nChangeSpriteID == 0)
            {
                m_nChangeSpriteID = CAtlasMng.instance.SpriteNameToID(m_SpriteName);
                if(m_nChangeSpriteID == 0)
                    m_nChangeSpriteID = IntParse(m_SpriteName);
                m_SpriteName = string.Empty;
            }
            return m_nChangeSpriteID;
        }
    }
    /// <summary>
    /// 将字符串解析为int
    /// </summary>
    /// <param name="szValue"></param>
    /// <returns>解析失败默认返回0(以前韩国研发 返回的int 最大值)</returns>
    public static int IntParse(String szValue)
    {
        int nValue = 0;
        if (String.IsNullOrEmpty(szValue))
            return 0;
        if (!int.TryParse(szValue, out nValue))
        {
            return 0;
        }
        return nValue;
    }

    public void  Set(UIGifXMLFrame frame)
    {
        m_fNextGapTime = frame.fNextGapTime;
        m_nOffsetX = frame.OffsetX;
        m_nOffsetY = frame.OffsetY;
        m_nScaleWidth = frame.ScaleWidth;
        m_nScaleHeight = frame.ScaleHeight;
        m_nChangeSpriteID = 0;
        m_SpriteName = frame.SpriteName;
        //m_nSpriteID = CAtlasMng.instance.SpriteNameToID(frame.SpriteID);
        //if(m_nSpriteID == 0)
        //{
        //    m_nSpriteID = Helper.IntParse(frame.SpriteID);
        //}
    }
}

public class UISpriteGif
{
    public int m_nID;  // 动画ID
    public int m_nWidth;
    public int m_nHeight;
    public UISpriteGifFrame[] m_FrameInfo;
    public void  Set(UIGifXMLList gif)
    {
        m_nID = gif.ID;
        m_nWidth = gif.Width;
        m_nHeight = gif.Height;
        m_FrameInfo = new UISpriteGifFrame[gif.frame.Count];
        for( int i = 0; i<gif.frame.Count; ++i )
        {
            m_FrameInfo[i].Set(gif.frame[i]);
        }
    }
}

public class UISpriteGifManager
{
    public Dictionary<int, UISpriteGif> m_SpriteGif = new Dictionary<int, UISpriteGif>();
    int m_nMaxID = 0;

    static UISpriteGifManager s_pGifManager;

    public static UISpriteGifManager Instance
    {
        get
        {
            if(s_pGifManager == null)
            {
                s_pGifManager = new UISpriteGifManager();
                s_pGifManager.LoadXmlEditorMode();
            }
            return s_pGifManager;
        }
    }

    public void LoadXmlEditorMode()
    {
#if  UNITY_EDITOR
        string szCfgPathName = Application.dataPath + "/Xmls/sprite_gif.xml";
        if(File.Exists(szCfgPathName))
        {
            try
            {
                FileStream stream = new FileStream(szCfgPathName, FileMode.Open, FileAccess.Read);
                XmlSerializer xs = new XmlSerializer(typeof(UISpriteGifConfig));
                UISpriteGifConfig pCfg = (UISpriteGifConfig)xs.Deserialize(stream);
                stream.Close();
                SetSpriteCfgConfig(pCfg);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
#endif
    }
    public void SetSpriteCfgConfig(UISpriteGifConfig pCfg)
    {
        m_SpriteGif.Clear();
        if (pCfg == null)
            return;
        for( int i = 0; i < pCfg.gifList.Count; ++i )
        {
            UIGifXMLList gif = pCfg.gifList[i];
#if  UNITY_EDITOR
            if (gif.ID == 0)
            {
                continue;
                Debug.LogError("动画ID不能为零，请检查配置表");
            }
#endif
            UISpriteGif uiGif = new UISpriteGif();
            uiGif.Set(gif);
            m_SpriteGif[gif.ID] = uiGif;
            if (m_nMaxID == 0)
                m_nMaxID = gif.ID;
            else if (m_nMaxID < gif.ID)
                m_nMaxID = gif.ID;
        }
    }

    // 功能：得到最大的动画ID
    public int  GetMaxGifID()
    {
        return m_nMaxID;
    }

    // 功能：检查动画ID是不是合法
    public bool  IsValidGif(int nGifID)
    {
        return m_SpriteGif.ContainsKey(nGifID);
    }

    // 功能：根据动画ID得到动画数量
    public UISpriteGif  GetSpriteGif(int nGifID)
    {
        UISpriteGif pGif = null;
        if (m_SpriteGif.TryGetValue(nGifID, out pGif))
            return pGif;
        return null;
    }

    // 功能：得到动画的宽度
    public int GetSpriteGifWidth(int nGifID)
    {
        UISpriteGif pGif = null;
        if (m_SpriteGif.TryGetValue(nGifID, out pGif))
            return pGif.m_nWidth;
        return 0;
    }

    // 功能：得到动画的高度
    public int GetSpriteGifHeight(int nGifID)
    {
        UISpriteGif pGif = null;
        if (m_SpriteGif.TryGetValue(nGifID, out pGif))
            return pGif.m_nHeight;
        return 0;
    }
}
