using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

///////////////////////////////////////////////////////////
//
//  Written by              ：laishikai
//  Copyright(C)            ：成都博瑞梦工厂
//  ------------------------------------------------------
//  功能描述                ：角色头顶渲染管理模块
//
///////////////////////////////////////////////////////////

class HUDTilteLine
{
    public string m_szText; // 该行的文本
    public string m_szValidText;
    public float m_fWidth;
    public int m_nHeight;
    public HUDTilteType m_nType;
    public int m_nColorIndex;
    public int m_nSpriteID;
    public int m_nStart;
    public int m_nEnd;
    public int m_nLine;
}

enum HUDTilteType
{
    PlayerName,
    PlayerPrestige,
    PlayerCorp,
    PlayerDesignation,
    MonsterName,
    ItemName,
    PetName,
    Blood, // 血条
    PKFlag, // PK标识
    HeadIcon, // NPC头顶标记或队长图标

    Tilte_Number
}

// 头顶信息
class HUDTitleInfo : HUDTitleBase
{
    public Transform m_tf;
    public float m_fDistToCamera = 0.0f; // 离相机的距离，用来排序
    
    bool m_bInitHUDMesh = false;
    bool m_bNeedHide = false;
    bool m_bDirty = false;
    bool m_bIsMain = false;
    
    HUDTilteLine[] m_TitleLine = new HUDTilteLine[(int)HUDTilteType.Tilte_Number];
    int m_nTitleNumb = 0;
    
    float m_fLineOffsetY;
    float m_fCurLineHeight;
    float m_fCurLineWidth; // 当前行的宽度（只有居中的才统计)
    int m_nStartLineIndex;
    int m_nBloodIndex;
    int m_nBloodSpriteID;
    HUDBloodType m_nBloodType;
    int m_nLines;

    int m_nMeridianIndex; // 头顶充脉Title下标
    int m_nMeridianNumb;  // 头顶充脉显示数字

    HUDTitleInfo.HUDTitleBatcher m_pBatcher;
    int m_nBatcherIndex;
    float m_fLastMoveTime = 0.0f; // 最后移动的时间
    int m_nTitleID = 0;

    void RebuildCharUV(ref CharacterInfo tempCharInfo)
    {
        if (m_aSprite.size == 0)
            return;
        UIFont uiFont = GetHUDTitleFont();
        int nStart = 0, nEnd = 0;
        for(int i = 0; i<m_nTitleNumb; ++i)
        {
            HUDTilteLine title = m_TitleLine[i];
            if (string.IsNullOrEmpty(title.m_szValidText))
                continue;
            uiFont.PrepareQueryText(title.m_szValidText);
            nEnd = title.m_nEnd;
            nStart = title.m_nStart;
            for (; nStart < nEnd; ++nStart)
            {
                HUDVertex v = m_aSprite[nStart];
                if (0 == v.AtlasID)
                {
                    uiFont.GetCharacterInfo(v.ch, ref tempCharInfo);
                    v.RebuildCharUV(tempCharInfo);
                    if (v.hudMesh != null)
                        v.hudMesh.VertexDirty();
                }
            }
        }
    }
    // 功能：重置头顶文本的UV坐标
    // 说明：因为退出游戏时，把UI的模型都给释放了，所以重新登陆时，需要重新设置一下UV坐标
    void RebuildFontUI()
    {
        CharacterInfo tempCharInfo = new CharacterInfo();
        RebuildCharUV(ref tempCharInfo);
    }
    void ApplyMove(bool bCameraDirty, Vector3 vCameraPos)
    {
        Camera caMain = HUDMesh.GetHUDMainCamera();
        Vector3 vPos = m_tf.position;
        Vector3 vScale = m_tf.localScale;
        vPos.y += (m_fOffsetY + HudSetting.Instance.m_fTitleOffsetY) * vScale.y;
        if (!bCameraDirty && !m_bDirty)
        {
            float dx = vPos.x - m_vPos.x;
            float dz = vPos.z - m_vPos.z;
            float dy = vPos.y - m_vPos.y;
            bool bDirty = !m_bInitHUDMesh;
            if (dx * dx + dz * dz + dy * dy > 0.00001f)
            {
                bDirty = true;
            }
            if (!bDirty)
                return;
            if (m_pBatcher != null)
                m_pBatcher.m_bTitleMove = true;
            m_fLastMoveTime = Time.time;
        }
        m_bDirty = false;
        m_vPos = vPos;
        m_vScreenPos = caMain.WorldToScreenPoint(vPos);
                
        m_fDistToCamera = Vector3.Distance(vCameraPos, m_vPos);
        if (m_bIsMain)
            m_fDistToCamera -= 1000.0f;
        CaleCameraScale(vCameraPos);
        OnChangeScreenPos();
    }

    public void EraseSpriteFromMesh()
    {
        if (!m_bInitHUDMesh)
            return;
        m_bInitHUDMesh = false;
        for (int i = m_aSprite.size - 1; i >= 0; --i)
        {
            HUDVertex v = m_aSprite[i];
            if (v.hudMesh != null)
                v.hudMesh.EraseHUDVertex(v);
            v.hudMesh = null;
        }
    }
    
    // 功能：显示或隐藏标题
    public void ShowTitle(bool bShow)
    {
        bool bHide = !bShow;
        if(m_bNeedHide != bHide)
        {
            m_bNeedHide = bHide;
            if(bHide && m_bInitHUDMesh)
            {
                EraseSpriteFromMesh();
            }
        }
    }

    // 功能：目标缩放后的处理
    public void OnScale()
    {
        m_bDirty = true;
    }

    public void OnRelease()
    {
        Clear();
        m_bIsMain = false;
        m_bNeedHide = false;
        m_nMeridianIndex = 0;
        m_nMeridianNumb = 0;
        m_tf = null;
    }

    public void SetOffsetY(float fOffsetY)
    {
        if(Mathf.Abs(m_fOffsetY - fOffsetY) > 0.0001f)
        {
            m_bDirty = true;
        }
        m_fOffsetY = fOffsetY;
    }

    public void Clear()
    {
        ClearSprite();
        m_fLineOffsetY = 0.0f;
        m_fCurLineHeight = 0;
        m_fCurLineWidth = 0;
        m_nStartLineIndex = 0;
        m_nTitleNumb = 0;
        m_bInitHUDMesh = false;
        m_nBloodIndex = 0;
        m_nBloodSpriteID = 0;
        m_nBloodType = HUDBloodType.Blood_None;
        m_nLines = 0;
        m_nMeridianIndex = 0;
        m_nMeridianNumb = 0;
    }
    protected void RebuildForEditor()
    {
        Transform tf = m_tf;
        Vector3 vPos = m_vPos;
        Vector2 vScreenPos = m_vScreenPos;
        HUDBloodType nBloodType = m_nBloodType;

        float fPos = HudSetting.Instance.m_fTestBloodPos;
        HUDTilteLine[] titles = new HUDTilteLine[m_nTitleNumb];
        int nNumb = m_nTitleNumb;
        for (int i = 0; i<m_nTitleNumb; ++i)
        {
            titles[i] = new HUDTilteLine();
            titles[i].m_szText = m_TitleLine[i].m_szText;
            titles[i].m_nType = m_TitleLine[i].m_nType;
            titles[i].m_nColorIndex = m_TitleLine[i].m_nColorIndex;
            titles[i].m_nLine = m_TitleLine[i].m_nLine;
            titles[i].m_nSpriteID = m_TitleLine[i].m_nSpriteID;
        }
        Clear();
        m_tf = tf;
        m_vPos = vPos;
        m_vScreenPos = vScreenPos;
        m_nBloodType = nBloodType;
        int nOldLine = -1;
        bool bStartLine = false;
        for (int i = 0; i<nNumb; ++i)
        {
            HUDTilteLine title = titles[i];
            if(nOldLine != title.m_nLine)
            {
                if(bStartLine)
                {
                    EndTitle();
                }
                bStartLine = true;
                BeginTitle();
            }
            if (title.m_nType == HUDTilteType.Blood)
                PushBlood(m_nBloodType, fPos);
            else if (title.m_nSpriteID != 0)
                PushIcon(title.m_nType, title.m_nSpriteID);
            else
                PushTitle(title.m_szText, title.m_nType, title.m_nColorIndex);
            nOldLine = title.m_nLine;
        }
        if (bStartLine)
        {
            EndTitle();
        }
    }
    // 开始一行Title
    public void BeginTitle()
    {
        m_fCurLineHeight = 0;
        m_fCurLineWidth = 0;
    }
    // 功能：结束一行Title
    public void EndTitle()
    {
        ++m_nLines;
        Align();
    }
    // 必须先有居中的
    public void PushTitle(string szText, HUDTilteType titleType, int nColorIndex)
    {
        if (m_nTitleNumb >= m_TitleLine.Length)
            return;

        UIFont mFont = GetHUDTitleFont();
        CharacterInfo tempCharInfo = new CharacterInfo();

        HudTitleAttribute titleAttrib = HudSetting.Instance.TitleSets[(int)titleType].GetTitle(nColorIndex);
        int nLineGap = titleAttrib.LineGap;
        int nCharGap = titleAttrib.CharGap;

        HUDTextParse hudParse = HUDTextParse.Instance;
        hudParse.ParseText(szText);
        mFont.PrepareQueryText(hudParse.m_szText);

        HudTitleAttribute.Effect nStyle = titleAttrib.Style;
        int nSpriteCount = hudParse.m_SpriteCount;
        HUDCharInfo[] Sprites = hudParse.m_Sprites;
        char ch;
        float fShadowX = titleAttrib.OffsetX;
        float fShadowY = titleAttrib.OffsetY;
        float fx = 0.0f;
        Color32 clrLeftUp = titleAttrib.clrLeftUp;
        Color32 clrLeftDown = titleAttrib.clrLeftDown;
        Color32 clrRightUp = titleAttrib.clrRightUp;
        Color32 clrRightDown = titleAttrib.clrRightDown;
        Color32 clrShadow = titleAttrib.clrShadow;
        Color32 clrCustom;

        int nStart = m_aSprite.size;
        int nHeight = mFont.GetFontHeight();
        int nFontH = titleAttrib.Height;
        int nFontOffsetY = titleAttrib.FontOffsetY;
        int nY = 0;

        for (int i = 0; i < nSpriteCount; ++i)
        {
            if (Sprites[i].bChar)
            {
                ch = Sprites[i].ch;
                mFont.GetCharacterInfo(ch, ref tempCharInfo);
                nY = (tempCharInfo.glyphHeight - nFontH)/2 + nFontOffsetY;

                if (nStyle != HudTitleAttribute.Effect.None)
                {
                    PushShadow(ref tempCharInfo, ch, fx, nY, clrShadow, fShadowX, fShadowY);
                    if (nStyle == HudTitleAttribute.Effect.Outline)
                    {
                        PushShadow(ref tempCharInfo, ch, fx, nY, clrShadow, fShadowX, -fShadowY);
                        PushShadow(ref tempCharInfo, ch, fx, nY, clrShadow, -fShadowX, fShadowY);
                        PushShadow(ref tempCharInfo, ch, fx, nY, clrShadow, -fShadowX, -fShadowY);                        
                    }
                }
                if (Sprites[i].bCustomColor)
                {
                    clrCustom = Sprites[i].CustomColor;
                    HUDVertex node = PushChar(ref tempCharInfo, ch, fx, nY, clrCustom, clrCustom, clrCustom, clrCustom);
                    fx += node.width + nCharGap;
                }
                else
                {
                    HUDVertex node = PushChar(ref tempCharInfo, ch, fx, nY, clrLeftUp, clrLeftDown, clrRightUp, clrRightDown);
                    fx += node.width + nCharGap;
                }                
            }
            else
            {
                // 图片
                if (Sprites[i].CharType == UIFontUnitType.UnitType_Icon)
                {
                    HUDVertex node = PushSprite(Sprites[i].SpriteID, Sprites[i].SpriteWidth, Sprites[i].SpriteHeight, fx, titleAttrib.SpriteOffsetY);
                    fx += node.width + nCharGap;
                    if (nHeight < node.height - titleAttrib.SpriteReduceHeight)
                        nHeight = node.height - titleAttrib.SpriteReduceHeight;
                }
            }
        }

        if (m_TitleLine[m_nTitleNumb] == null)
            m_TitleLine[m_nTitleNumb] = new HUDTilteLine();
        m_TitleLine[m_nTitleNumb].m_nType = titleType;
        m_TitleLine[m_nTitleNumb].m_fWidth = fx - nCharGap;
        m_TitleLine[m_nTitleNumb].m_nHeight = nHeight;
        m_TitleLine[m_nTitleNumb].m_nStart = nStart;
        m_TitleLine[m_nTitleNumb].m_nEnd = m_aSprite.size;
        m_TitleLine[m_nTitleNumb].m_szText = szText;
        m_TitleLine[m_nTitleNumb].m_szValidText = hudParse.m_szText;
        m_TitleLine[m_nTitleNumb].m_nColorIndex = nColorIndex;
        m_TitleLine[m_nTitleNumb].m_nLine = m_nLines;
        m_TitleLine[m_nTitleNumb].m_nSpriteID = 0;

        if (titleAttrib.AlignType == HUDAlignType.align_center)
        {
            m_fCurLineWidth = fx - nCharGap;
        }
        if (nHeight < titleAttrib.LockMaxHeight && titleAttrib.LockMaxHeight > 0)
            nHeight = titleAttrib.LockMaxHeight;
        if (m_fCurLineHeight < nHeight)
            m_fCurLineHeight = nHeight;
        ++m_nTitleNumb;
    }
    // 功能：显示头顶充脉数字
    void PushMeridianNumber(int nNumb)
    {
        if (m_nTitleNumb <= 0)
            return;
        if(m_nMeridianIndex != m_nTitleNumb - 1)
        {
            m_nMeridianIndex = m_nTitleNumb;
            // 添加
            if (m_TitleLine[m_nTitleNumb] == null)
                m_TitleLine[m_nTitleNumb] = new HUDTilteLine();
            m_TitleLine[m_nTitleNumb].m_nType = HUDTilteType.HeadIcon;
            m_TitleLine[m_nTitleNumb].m_fWidth = 0;
            m_TitleLine[m_nTitleNumb].m_nHeight = 0;
            m_TitleLine[m_nTitleNumb].m_nStart = m_aSprite.size;
            m_TitleLine[m_nTitleNumb].m_nEnd = m_aSprite.size;
            m_TitleLine[m_nTitleNumb].m_szText = string.Empty;
            m_TitleLine[m_nTitleNumb].m_nColorIndex = 0;
            m_TitleLine[m_nTitleNumb].m_nLine = m_nLines;
            m_TitleLine[m_nTitleNumb].m_nSpriteID = 0;
            ++m_nTitleNumb;
        }
        UpdateMeridianNumber(nNumb);
        m_nStartLineIndex = m_nMeridianIndex;
    }

    public void ShowMeridianNumber(int nMeridianNumb)
    {
        float fLineOffsetY = m_fLineOffsetY;
        if ( 0 == m_nMeridianIndex || m_nMeridianIndex != m_nTitleNumb - 1)
        {
            BeginTitle();
            PushMeridianNumber(nMeridianNumb);
            EndTitle();
        }
        else
        {
            if (m_nMeridianNumb == nMeridianNumb)
                return;
            UpdateMeridianNumber(nMeridianNumb);
            m_nStartLineIndex = m_nMeridianIndex;
            Align();
        }
        m_fLineOffsetY = fLineOffsetY;
    }

    public void HideMeridianNumber()
    {
        if (m_nMeridianIndex > 0 && m_nMeridianIndex != m_nTitleNumb - 1)
            return;
        HUDTilteLine title = m_TitleLine[m_nMeridianIndex];
        if (title.m_nType != HUDTilteType.HeadIcon)
            return;
        int nStart = title.m_nStart;
        int nEnd = title.m_nEnd;
        for (--nEnd; nEnd >= nStart && nEnd < m_aSprite.size; --nEnd)
        {
            HUDVertex v = m_aSprite[nEnd];
            if (v.hudMesh != null)
                v.hudMesh.EraseHUDVertex(v);
            v.hudMesh = null;
            m_aSprite.RemoveAt(nEnd);
            HUDVertex.ReleaseVertex(v);
        }
        m_nMeridianIndex = 0;
        --m_nTitleNumb;
        --m_nLines;
    }

    void UpdateMeridianNumber(int nMeridianNumb)
    {
        // 必须是在最后
        if (0 == m_nMeridianIndex || m_nMeridianIndex != m_nTitleNumb - 1)
            return;
        // 先释放旧的吧
        HUDTilteLine title = m_TitleLine[m_nMeridianIndex];
        if (title.m_nType != HUDTilteType.HeadIcon)
            return;
        if (nMeridianNumb < 0)
            nMeridianNumb = 0;
        m_nMeridianNumb = nMeridianNumb;
        HudTitleAttribute titleAttrib = HudSetting.Instance.TitleSets[(int)HUDTilteType.HeadIcon].GetTitle(0);
        int nCharGap = titleAttrib.CharGap;

        int nPow = 1;
        while(nPow * 10 <= nMeridianNumb)
        {
            nPow *= 10;
        }

        int nWidth  = 0;
        int nHeight = 0;
        int nMaxWidth = 0;
        int nMaxHeight = 0;
        int[] PicID = HudSetting.Instance.MeridianPic;
        int nSpriteID = 0;
        int nValue = nMeridianNumb;
        int nStart = title.m_nStart;
        int nEnd = title.m_nEnd;
        int nX = 0;
        while(nPow > 0)
        {
            int nIndex = nValue / nPow;
            nValue %= nPow;
            nPow /= 10;
            nSpriteID = PicID[nIndex];
            UISpriteInfo  sp = CAtlasMng.instance.GetSafeSpriteByID(nSpriteID);
            nWidth = (int)sp.outer.width;
            nHeight = (int)sp.outer.height;
            nMaxWidth += nWidth + nCharGap;
            if (nMaxHeight < nHeight)
                nMaxHeight = nHeight;

            HUDVertex v = null;
            if (nStart < m_aSprite.size)
            {
                v = m_aSprite[nStart];
                if(v.AtlasID != sp.m_nAtlasID)
                {
                    if (v.hudMesh != null)
                        v.hudMesh.EraseHUDVertex(v);
                    v.hudMesh = null;
                    v.AtlasID = sp.m_nAtlasID;
                }
                v.SpriteID = nSpriteID;
                v.Offset.Set(nX, titleAttrib.SpriteOffsetY);
                v.InitSprite(nWidth, nHeight);
            }
            else
            {
                v = PushSprite(nSpriteID, nWidth, nHeight, nX, titleAttrib.SpriteOffsetY);
            }
            v.Scale = m_fScale;

            if (v.hudMesh == null)
            {
                if (m_bInitHUDMesh)
                {
                    v.hudMesh = m_pBatcher.m_MeshRender.QueryMesh(sp.m_nAtlasID);
                    v.hudMesh.PushHUDVertex(v);
                }
            }
            else
            {
                v.hudMesh.VertexDirty();
            }
            nX = nMaxWidth;
            ++nStart;
        }

        title.m_nEnd = nStart;
        // 删除多余的
        for(--nEnd; nEnd >= nStart; --nEnd)
        {
            HUDVertex v = m_aSprite[nEnd];
            if (v.hudMesh != null)
                v.hudMesh.EraseHUDVertex(v);
            v.hudMesh = null;
            m_aSprite.RemoveAt(nEnd);
            HUDVertex.ReleaseVertex(v);
        }

        nMaxWidth -= nCharGap;
        title.m_fWidth = nMaxWidth;
        title.m_nHeight = nMaxHeight;
        nMaxHeight += titleAttrib.SpriteReduceHeight;
        m_fCurLineWidth = nMaxWidth;
        m_fCurLineHeight = nMaxHeight;
    }

    public void PushIcon(HUDTilteType titleType, int nSpriteID)
    {
        HudTitleAttribute titleAttrib = HudSetting.Instance.TitleSets[(int)titleType].GetTitle(0);

        UISpriteInfo sp = CAtlasMng.instance.GetSafeSpriteByID(nSpriteID);
        int nWidth = 0;
        int nHeight = 0;
        int nStart = m_aSprite.size;
        if(sp != null)
        {
            nWidth = (int)sp.outer.width;
            nHeight = (int)sp.outer.height;
            PushSprite(nSpriteID, nWidth, nHeight, 0.0f, titleAttrib.SpriteOffsetY);
        }
        if (m_TitleLine[m_nTitleNumb] == null)
            m_TitleLine[m_nTitleNumb] = new HUDTilteLine();
        m_TitleLine[m_nTitleNumb].m_nType = titleType;
        m_TitleLine[m_nTitleNumb].m_fWidth = nWidth;
        m_TitleLine[m_nTitleNumb].m_nHeight = nHeight;
        m_TitleLine[m_nTitleNumb].m_nStart = nStart;
        m_TitleLine[m_nTitleNumb].m_nEnd = m_aSprite.size;
        m_TitleLine[m_nTitleNumb].m_szText = string.Empty;
        m_TitleLine[m_nTitleNumb].m_nColorIndex = 0;
        m_TitleLine[m_nTitleNumb].m_nLine = m_nLines;
        m_TitleLine[m_nTitleNumb].m_nSpriteID = nSpriteID;

        if (titleAttrib.AlignType == HUDAlignType.align_center)
        {
            m_fCurLineWidth = nWidth;
        }
        nHeight += titleAttrib.SpriteReduceHeight;
        if (nHeight < titleAttrib.LockMaxHeight && titleAttrib.LockMaxHeight > 0)
            nHeight = titleAttrib.LockMaxHeight;
        if (m_fCurLineHeight < nHeight)
            m_fCurLineHeight = nHeight;
        ++m_nTitleNumb;
    }

    // 功能：设置血条
    // 参数：nType - 血条的类型
    //       fBloodPos - 血条的进度（百分比)
    public void PushBlood(HUDBloodType nType, float fBloodPos)
    {
        if (m_nTitleNumb >= m_TitleLine.Length)
            return;
        int nStart = m_aSprite.size;
        // 添加背景
        int  nBkWidth = HudSetting.Instance.m_nBloodBkWidth;
        int nBkHeight = HudSetting.Instance.m_nBloodBkHeight;
        int nBloodWidth = HudSetting.Instance.m_nBloodWidth;
        int nHeight = HudSetting.Instance.m_nBloodHeight;
        m_nBloodSpriteID = 0;
        if (nType == HUDBloodType.Blood_Green)
            m_nBloodSpriteID = HudSetting.Instance.m_nBloodGreen;
        else if (nType == HUDBloodType.Blood_Red)
            m_nBloodSpriteID = HudSetting.Instance.m_nBloodRed;
        else if(nType == HUDBloodType.Blood_Blue)
            m_nBloodSpriteID = HudSetting.Instance.m_nBloodBlue;

        PushSprite(HudSetting.Instance.m_nBloodBk, nBkWidth, nBkHeight, (nBloodWidth - nBkWidth) * 0.5f, 0);// (nBkHeight - nHeight) * 0.5f);
        PushSliceTitle(m_nBloodSpriteID, nBloodWidth, nHeight, 0.0f, 0.0f, fBloodPos);
        
        if (m_TitleLine[m_nTitleNumb] == null)
            m_TitleLine[m_nTitleNumb] = new HUDTilteLine();
        m_TitleLine[m_nTitleNumb].m_nType = HUDTilteType.Blood;
        m_TitleLine[m_nTitleNumb].m_fWidth = nBloodWidth;
        m_TitleLine[m_nTitleNumb].m_nHeight = nHeight;
        m_TitleLine[m_nTitleNumb].m_nStart = nStart;
        m_TitleLine[m_nTitleNumb].m_nEnd = m_aSprite.size;
        m_TitleLine[m_nTitleNumb].m_szText = string.Empty;
        m_TitleLine[m_nTitleNumb].m_nSpriteID = 0;

        m_nBloodType = nType;
        m_nBloodIndex = m_nTitleNumb;
        m_fCurLineWidth = nBloodWidth;
        m_fCurLineHeight = nHeight;
        ++m_nTitleNumb;
    }
    public HUDBloodType  GetBloodType()
    {
        return m_nBloodType;
    }
    // 功能：设置血条的进度（百分比)
    public void SetBloodPos(float fBloodPos)
    {
        // 跳过背景
        if(m_nBloodIndex >= 0 && m_nBloodIndex < m_nTitleNumb)
        {
            SlicedFill(m_nBloodSpriteID, HudSetting.Instance.m_nBloodWidth, HudSetting.Instance.m_nBloodHeight, m_TitleLine[m_nBloodIndex].m_nStart + 1, fBloodPos);
        }
    }
    void Align()
    {
        int nLineGap = 0;

        // 先让Y轴居中吧
        float fOffsetY = m_fLineOffsetY + m_fCurLineHeight * 0.5f;
        for (int i = m_nStartLineIndex; i < m_nTitleNumb; ++i)
        {
            HUDTilteLine title = m_TitleLine[i];

            HudTitleAttribute titleAttrib = HudSetting.Instance.TitleSets[(int)title.m_nType].GetTitle(title.m_nColorIndex);
            if (titleAttrib.AlignType == HUDAlignType.align_right)
            {
                OffsetXY(title.m_nStart, title.m_nEnd, m_fCurLineWidth * 0.5f + titleAttrib.CharGap, fOffsetY);
            }
            else if (titleAttrib.AlignType == HUDAlignType.align_left)
            {
                OffsetXY(title.m_nStart, title.m_nEnd, -m_fCurLineWidth * 0.5f - title.m_fWidth - titleAttrib.CharGap, fOffsetY);
            }
            else
            {
                OffsetXY(title.m_nStart, title.m_nEnd, title.m_fWidth * -0.5f, fOffsetY);
            }
            if (nLineGap < titleAttrib.LineGap)
                nLineGap = titleAttrib.LineGap;
        }
        m_fLineOffsetY += m_fCurLineHeight + nLineGap;
        m_nStartLineIndex = m_nTitleNumb;
    }
    void PushSliceTitle(int nSpriteID, int nWidth, int nHeight, float fx, float fy, float fBloodPos)
    {
        UISpriteInfo sp = CAtlasMng.instance.GetSafeSpriteByID(nSpriteID);
        if (sp == null)
            return;
        int nAtlasID = sp.m_nAtlasID;
        int nStart = m_aSprite.size;
        for (int i = 0; i < 9; ++i)
        {
            HUDVertex node = HUDVertex.QueryVertex();
            node.WorldPos = m_vPos;
            node.ScreenPos = m_vScreenPos;
            node.SpriteID = nSpriteID;
            node.AtlasID  = nAtlasID;
            node.Scale = m_fScale;
            node.Offset.Set(fx, fy);
            node.Move.Set(0f, 0f);
            node.width = (short)nWidth;
            node.height = (short)nHeight;
            m_aSprite.Add(node);
        }
        SlicedFill(nSpriteID, nWidth, nHeight, nStart, fBloodPos);
    }   

    // 头顶批处理
    public class HUDTitleBatcher
    {
        public BetterList<HUDTitleInfo> m_ValidTitles = new BetterList<HUDTitleInfo>();
        public HUDRender m_MeshRender = new HUDRender();
        public bool m_bNeedSort = false; // 是不是需要排序
        public bool m_bTitleMove = false;
        public bool m_bStatic = false;
        bool m_bRebuildMesh = false;
        bool m_bHaveNullTitle = false;
        int m_nSortVeresion = 0;
        int m_nMaxSortCount = 0;
        
        void  CompareTitleByDist()
        {
            m_bNeedSort = false;
            bool changed = true;
            HUDTitleInfo[] buffer = m_ValidTitles.buffer;
            int nSize = m_ValidTitles.size;
            HUDTitleInfo temp;
            int nChangeCount = 0;
            while (changed)
            {
                changed = false;
                for (int i = 1; i < nSize; ++i)
                {
                    // 近的排在后面, 数字大的排在前面
                    if(buffer[i - 1].m_fDistToCamera < buffer[i].m_fDistToCamera)
                    {
                        temp = buffer[i];
                        buffer[i] = buffer[i - 1];
                        buffer[i - 1] = temp;
                        changed = true;
                        ++nChangeCount;
                    }
                }
            }
            if (nChangeCount > 0)
            {
                if(m_nMaxSortCount < nChangeCount)
                {
                    m_nMaxSortCount = nChangeCount;
//#if  UNITY_EDITOR
//                    if (m_bStatic)
//                        Debug.Log("Static Title Render Compare count:" + m_nMaxSortCount);
//                    else
//                        Debug.Log("Dynamic Title Render Compare count:" + m_nMaxSortCount);
//#endif
                }

                for (int i = m_ValidTitles.size - 1; i>=0; --i)
                {
                    HUDTitleInfo title = m_ValidTitles[i];
                    if(title.m_nBatcherIndex < i)
                    {
                        m_bRebuildMesh = true;
                    }
                    title.m_nBatcherIndex = i;
                }
            }
        }
        
        void InitTitleHUDMesh(HUDTitleInfo title)
        {
            if (!title.m_bInitHUDMesh && !title.m_bNeedHide)
            {
                title.m_bInitHUDMesh = true;
                for (int i = 0, nSize = title.m_aSprite.size; i < nSize; ++i)
                {
                    HUDVertex v = title.m_aSprite[i];
                    if (v.hudMesh == null)
                    {
                        if (v.AtlasID != 0)
                            v.hudMesh = m_MeshRender.QueryMesh(v.AtlasID);
                        else
                        {
                            v.hudMesh = m_MeshRender.FontMesh();
                        }
                        v.hudMesh.PushHUDVertex(v);
                    }
                }
                //if (title.m_aSprite.size == 0)
                //    return;
                //CharacterInfo tempCharInfo = new CharacterInfo();
                //title.RebuildCharUV(ref tempCharInfo);
            }
        }

        public void OnAllFontChanged(UIFont uiFont)
        {
            CharacterInfo tempCharInfo = new CharacterInfo();
            for(int i = 0, nSize = m_ValidTitles.size; i< nSize; ++i)
            {
                HUDTitleInfo title = m_ValidTitles[i];
                if(title != null)
                    title.RebuildCharUV(ref tempCharInfo);
            }            
            m_MeshRender.OnChangeFont(uiFont);     
        }

        void PrepareRebuild()
        {
            m_MeshRender.FastClearVertex();

            for (int i = 0, nSize = m_ValidTitles.size; i < nSize; ++i)
            {
                HUDTitleInfo title = m_ValidTitles[i];
                title.m_bInitHUDMesh = false;
                title.PrepareRebuildMesh();
            }
        }

        public void UpdateLogic(bool bCameraDirty, Vector3 vCameraPos)
        {
            if (m_bHaveNullTitle)
            {
                m_bHaveNullTitle = false;
                m_ValidTitles.ClearNullItem();
            }

            // 更新位置信息
            for (int i = m_ValidTitles.size - 1; i >= 0; --i)
            {
                HUDTitleInfo title = m_ValidTitles[i];
                title.m_nBatcherIndex = i;
                if(title.m_tf != null)
                {
                    title.ApplyMove(bCameraDirty, vCameraPos);
                }                
            }

            if(m_bTitleMove)
            {
                m_bTitleMove = false;
                m_nSortVeresion++;
            }
            
            if (m_bNeedSort || m_nSortVeresion > 10)
            {
                bool bNeedSort = m_bNeedSort;
                int nSortVersion = m_nSortVeresion;
                m_bTitleMove = false;
                m_bNeedSort = false;
                m_nSortVeresion = 0;
                CompareTitleByDist();
                if(m_bRebuildMesh)
                {
                    //if(m_bStatic)
                    //    Debug.Log("Static Need PrepareRebuild, NeedSort=" + bNeedSort + ", Version=" + nSortVersion);
                    //else
                    //    Debug.Log("Dynamic Need PrepareRebuild, NeedSort=" + bNeedSort + ", Version=" + nSortVersion);
                    m_bRebuildMesh = false;
                    PrepareRebuild();
                }
            }
            for(int i = 0, nSize = m_ValidTitles.size; i<nSize; ++i)
            {
                HUDTitleInfo title = m_ValidTitles[i];
                title.m_nBatcherIndex = i;
                if (!title.m_bNeedHide && !title.m_bInitHUDMesh)
                {
                    InitTitleHUDMesh(title);
                }
            }
            m_MeshRender.FillMesh();
        }

        public void PushTitle(HUDTitleInfo title)
        {
            title.m_nBatcherIndex = m_ValidTitles.size;
            m_ValidTitles.Add(title);
            m_bNeedSort = true;
        }

        public void SwitchPushTitle(HUDTitleInfo title)
        {
            title.m_nBatcherIndex = m_ValidTitles.size;
            m_ValidTitles.Add(title);
            m_bNeedSort = true;
            InitTitleHUDMesh(title);
        }

        public void EraseTitle(HUDTitleInfo title)
        {
            int nIndex = title.m_nBatcherIndex;
            title.EraseSpriteFromMesh();
            if (nIndex >= 0 && nIndex < m_ValidTitles.size)
            {
                if(m_ValidTitles[nIndex] != null && m_ValidTitles[nIndex] == title)
                {
                    //m_bNeedSort = true;
                    m_bHaveNullTitle = true;
                    m_ValidTitles[nIndex] = null;
                    return;
                }
            }

            for (int i = m_ValidTitles.size - 1; i>= 0; --i)
            {
                if(m_ValidTitles[i] != null && m_ValidTitles[i] == title)
                {
                    //m_bNeedSort = true;
                    m_bHaveNullTitle = true;
                    m_ValidTitles[i] = null;
                    break;
                }
            }
        }
    }

    //-------------------------------------------------------------------------------------------
    public class HUDTitleRender
    {
        Dictionary<int, HUDTitleInfo> m_HudTitles = new Dictionary<int, HUDTitleInfo>();
        BetterList<int> m_DelayReleaseTitles = new BetterList<int>();
        int m_nHudID = 0;

        HUDTitleBatcher m_StaticBatcher = new HUDTitleBatcher();  // 不动的(自己的+不动的)
        HUDTitleBatcher m_DynamicBatcher = new HUDTitleBatcher(); // 会动的
                
        bool m_bAddUpdate = false;        
        Vector3 m_vLastCameraPos;
        Vector3 m_vLastEulerAngles;
        Camera m_renderCameara;
        CommandBuffer m_cmdBuffer;

        Transform m_tfMain;
        Camera m_oldCamera;
        bool m_bHideAllTitle = false;
        float m_fLastCheckMoveTime = 0.0f;

        bool m_bInitFontCallback = false;

        bool m_bOpenUI = false; // NPC对话状态
        bool m_bOldOpenUI = false;

        bool m_bStartDark = false;
        bool m_bOldStartDark = false;
        float m_fStartDarkTime = 0.0f;
        float m_fDarkTime = 0.0f;

        int m_nUpdateVer = 0;
        int m_nCameraUpdateVer = 0;
        int m_nBaseUpdateVer = 0;

        bool m_bMeshDirty = false;

        static HUDTitleRender s_HUDTitleRenderIns = null;
        public static HUDTitleRender Instance
        {
            get
            {
                if (s_HUDTitleRenderIns == null)
                {
                    s_HUDTitleRenderIns = new HUDTitleRender();
                }
                return s_HUDTitleRenderIns;
            }
        }

        HUDTitleRender()
        {
            m_StaticBatcher.m_bStatic = true;
            m_DynamicBatcher.m_bStatic = false;
        }

        public void SetMainPlayer(Transform tfMain)
        {
            m_tfMain = tfMain;
        }

        public void OnEnterGame()
        {
            HUDMesh.OnEnterGame();
            m_nUpdateVer = 0;
            m_nBaseUpdateVer = 0;
            m_nCameraUpdateVer = 0;

            foreach (var v in m_HudTitles)
            {
                HUDTitleInfo title = v.Value;
                if(title.m_pBatcher == null)
                {
                    title.m_pBatcher = m_StaticBatcher;
                    title.m_pBatcher.PushTitle(title);
                    title.RebuildFontUI();
                }
            }
        }
        public void OnLeaveGame()
        {
            foreach (var v in m_HudTitles)
            {
                HUDTitleInfo title = v.Value;
                title.m_pBatcher.EraseTitle(title);
                title.m_pBatcher = null;
            }
            HUDMesh.OnLeaveGame();
            HUDNumberRender.Instance.OnLeaveGame();
            m_DynamicBatcher.m_MeshRender.Release();
            m_StaticBatcher.m_MeshRender.Release();
            ReleaseCmmmandBuffer();
        }

        void ReleaseCmmmandBuffer()
        {
            if (m_cmdBuffer != null)
            {
                if(m_renderCameara != null)
                    m_renderCameara.RemoveCommandBuffer(CameraEvent.AfterImageEffects, m_cmdBuffer);
                m_cmdBuffer.Clear();
                m_renderCameara = null;
            }
        }

        // 功能：开始NPC对话
        public void OnOpenUI()
        {
            // 需要隐藏所有的文字
            m_bOpenUI = true;
            ReleaseCmmmandBuffer();
            m_bMeshDirty = true;
        }

        // 功能：结束NPC对话
        public void OnCloseUI()
        {
            m_bOpenUI = false;
            m_bMeshDirty = true;
        }

        // 功能：显示或隐藏所有头顶
        public void ShowAllTitle(bool bShowAllTitle)
        {
            HudSetting.Instance.HideAllTitle = !bShowAllTitle;
            m_bMeshDirty = true;
        }

        public void OnStartScreenDark(float fTime)
        {
            m_bStartDark = true;
            m_fStartDarkTime = Time.time;
            m_fDarkTime = fTime + 1.0f;
            ReleaseCmmmandBuffer();
            m_bMeshDirty = true;
        }
        public void OnEndScreenDark()
        {
            m_bStartDark = false;
            m_bMeshDirty = true;
        }
        public void OnEndMovie()
        {
            m_bOpenUI = false;
            m_bStartDark = false;
            HudSetting.Instance.HideAllTitle = false;
            m_bMeshDirty = true;
        }

        public int RegisterTitle(Transform tf, float fOffsetY, bool bIsMain)
        {
            Camera caMain = HUDMesh.GetHUDMainCamera();

            if (bIsMain)
                m_tfMain = tf;

            Vector3 vPos = tf.position;
            HUDTitleInfo title = new HUDTitleInfo();
            title.m_tf = tf;
            title.m_bIsMain = bIsMain;
            title.m_vPos = vPos;
            title.m_fOffsetY = fOffsetY;
            if (caMain != null)
            {
                vPos.y += fOffsetY + HudSetting.Instance.m_fTitleOffsetY;
                title.m_vScreenPos = caMain.WorldToScreenPoint(vPos);
                title.CaleCameraScale(caMain.transform.position);
            }
            int nID = ++m_nHudID;
            title.m_nTitleID = nID;
            m_HudTitles[nID] = title;

            if (bIsMain)
                title.m_pBatcher = m_StaticBatcher;
            else
                title.m_pBatcher = m_DynamicBatcher;
            title.m_fLastMoveTime = Time.time;
            title.m_pBatcher.PushTitle(title);

            if (!m_bAddUpdate)
            {
                m_bAddUpdate = true;
                UpdateManager.AddLateUpdate(null, 0, UpdateLogic);
            }
            if(!m_bInitFontCallback)
            {
                m_bInitFontCallback = true;

                UIFont uiFont = HUDTitleInfo.GetHUDTitleFont();
                Font.textureRebuilt += OnAllFontChanged;
            }

            return nID;
        }
        public void ReleaseTitle(int nTitleID)
        {
            HUDTitleInfo title;
            if (m_HudTitles.TryGetValue(nTitleID, out title))
            {
                if (title.m_nTitleID != nTitleID)
                {
                    Debug.LogError("非法释放头顶");
                    return;
                }
                if (title.m_bIsMain)
                    m_tfMain = null;
                title.m_pBatcher.EraseTitle(title);
                title.OnRelease();
                m_HudTitles.Remove(nTitleID);
            }
        }
        public void ApplySetting(HudAniSetting  hudSetting)
        {
            foreach(var v in m_HudTitles)
            {
                v.Value.RebuildForEditor();
            }
        }

        // 请不要在外部保存它
        public HUDTitleInfo GetTitle(int nTitleID)
        {
            HUDTitleInfo title;
            if (m_HudTitles.TryGetValue(nTitleID, out title))
                return title;
            return null;
        }
        void OnAllFontChanged(Font font)
        {
            if (font == null)
                return;
            UIFont uiFont = HUDTitleInfo.GetHUDTitleFont();
            if (font.GetInstanceID() != uiFont.dynamicFont.GetInstanceID())
            {
                return;
            }

            m_StaticBatcher.OnAllFontChanged(uiFont);
            m_DynamicBatcher.OnAllFontChanged(uiFont);

            // 头顶气泡
            HUDTalk.HUDTalkRender.Instance.OnAllFontChanged(uiFont);
        }

        void SwitchDynamieStatic()
        {
            float fNow = Time.time;
            m_fLastCheckMoveTime = fNow;
            Dictionary<int, HUDTitleInfo>.Enumerator it = m_HudTitles.GetEnumerator();
            while (it.MoveNext())
            {
                HUDTitleInfo title = it.Current.Value;
                if(title.m_tf == null)
                {
                    m_DelayReleaseTitles.Add(it.Current.Key);
                }
                if (title.m_bIsMain)
                    continue;
                if(title.m_pBatcher == null)
                {
                    title.m_pBatcher = m_StaticBatcher;
                    title.m_pBatcher.PushTitle(title);
                    title.RebuildFontUI();
                }
                if (title.m_pBatcher == m_StaticBatcher)
                {
                    // 动了
                    if (title.m_fLastMoveTime + 1.0f > fNow)
                    {
                        title.m_pBatcher.EraseTitle(title);
                        title.m_pBatcher = m_DynamicBatcher;
                        title.m_pBatcher.SwitchPushTitle(title);
                        m_bMeshDirty = true;
                    }
                }
                else
                {
                    // 一秒钟不动就转静态批
                    if (title.m_fLastMoveTime + 1.0f < fNow && title.m_pBatcher == m_DynamicBatcher)
                    {
                        title.m_pBatcher.EraseTitle(title);
                        title.m_pBatcher = m_StaticBatcher;
                        title.m_pBatcher.SwitchPushTitle(title);
                        m_bMeshDirty = true;
                    }
                }
            }

            // 释放已经无效的
            for(int i = m_DelayReleaseTitles.size - 1; i>=0; --i)
            {
                ReleaseTitle(m_DelayReleaseTitles[i]);
            }
            m_DelayReleaseTitles.Clear();
        }

        // 功能：摄像机变动了
        public void OnUpdateCameara()
        {
            ++m_nCameraUpdateVer;
            if(m_nBaseUpdateVer != m_nCameraUpdateVer)
            {
                BaseUpdateLogic(Time.deltaTime);
                m_nCameraUpdateVer = m_nBaseUpdateVer;
            }
        }
        
        void UpdateLogic(float delta)
        {
            ++m_nUpdateVer;
            if(m_nUpdateVer != m_nBaseUpdateVer)
            {
                BaseUpdateLogic(delta);
                m_nUpdateVer = m_nBaseUpdateVer;
            }
        }

        void CaleNumberScale(Vector3 vCameraPos)
        {
            if(m_tfMain != null)
            {
                Vector3 vPos = m_tfMain.position;
                float m_nearDistance = HudSetting.Instance.CameraNearDist;
                float m_farDistance = HudSetting.Instance.CameraFarDist;
                float m_minScale = HudSetting.Instance.m_fNumberScaleMin;
                float m_maxScale = HudSetting.Instance.m_fNumberScaleMax;
                float dis = Vector3.Distance(vPos, vCameraPos);
                float ratio = Mathf.Clamp01((dis - m_nearDistance) / (m_farDistance - m_nearDistance));
                float fScale = m_minScale * ratio + (1.0f - ratio) * m_maxScale;
                HUDMesh.s_fNumberScale = 1.0f / fScale;
            }
        }

        void BaseUpdateLogic(float delta)
        {
            m_nBaseUpdateVer++;
            Camera caMain = HUDMesh.GetHUDMainCamera();
            if (caMain == null)
                return ;
            HUDTalk.HUDTalkRender talkRender = HUDTalk.HUDTalkRender.Instance;
            Vector3 vCameraPos = caMain.transform.position;
            Vector3 vOffset = vCameraPos - m_vLastCameraPos;
            bool bCameraDirty = vOffset.x * vOffset.x + vOffset.y * vOffset.y + vOffset.z * vOffset.z > 0.000001f;
            if(!bCameraDirty)
            {
                vOffset = caMain.transform.localEulerAngles - m_vLastEulerAngles;
                if (vOffset.x * vOffset.x + vOffset.y * vOffset.y + vOffset.z * vOffset.z > 0.000001f)
                    bCameraDirty = true;
            }
            bool bMeshDirty = m_bMeshDirty;
            if (caMain != m_oldCamera)
            {
                m_oldCamera = caMain;
                bCameraDirty = true;
                bMeshDirty = true;
            }
            if(bCameraDirty)
            {
                m_vLastCameraPos = vCameraPos;
                m_vLastEulerAngles = caMain.transform.localEulerAngles;
                m_fLastCheckMoveTime = Time.time;
                float fScaleX = Screen.width / 1280.0f;
                float fScaleY = Screen.height / 720.0f;
                float fScale = fScaleX > fScaleY ? fScaleX : fScaleY;
                HUDMesh.s_fCameraScaleX = HUDMesh.s_fCameraScale * fScale;
                HUDMesh.s_fCameraScaleY = HUDMesh.s_fCameraScale * fScale;
                CaleNumberScale(vCameraPos);
            }
            else
            {
                // 切换
                float fNow = Time.time;
                if (m_fLastCheckMoveTime + 2.0f < fNow)
                {
                    m_fLastCheckMoveTime = fNow;
                    SwitchDynamieStatic(); // 这个不可以转换是什么鬼，有BUG
                }
            }
            
            // NPC对话
            talkRender.UpdateLogic(bCameraDirty, vCameraPos);
            if (talkRender.IsMeshDirty())
                bMeshDirty = true;

            if (m_bHideAllTitle != HudSetting.Instance.HideAllTitle)
            {
                m_bHideAllTitle = HudSetting.Instance.HideAllTitle;
                bMeshDirty = true;
            }

            // 静态批
            m_StaticBatcher.UpdateLogic(bCameraDirty, vCameraPos);
            if (m_StaticBatcher.m_MeshRender.m_bMeshDirty)
                bMeshDirty = true;

            // 动态批
            m_DynamicBatcher.UpdateLogic(bCameraDirty, vCameraPos);
            if (m_DynamicBatcher.m_MeshRender.m_bMeshDirty)
                bMeshDirty = true;
            
            // 处理二级面板开启
            if (m_bOpenUI != m_bOldOpenUI)
            {
                m_bOldOpenUI = m_bOpenUI;
                bMeshDirty = true;
            }
            else if (m_bOpenUI)
            {
                //if (caMain.cullingMask != 0)
                //{
                //    m_bNpcTalk = false; // 强制隐藏（界面上层的错误)
                //}
            }

            // 屏幕变黑后的处理
            if(m_bStartDark != m_bOldStartDark)
            {
                m_bOldStartDark = m_bStartDark;
                bMeshDirty = true;
            }
            else if(m_bStartDark)
            {
                if (m_fStartDarkTime + m_fDarkTime < Time.time)
                    m_bStartDark = false;
            }
            if (m_bMeshDirty)
            {
                m_bMeshDirty = false;
                bMeshDirty = true;
            }

            // 写和缓冲BUFF
            if (bMeshDirty)
                FillMeshRender();            
        }
        void FillMeshRender()
        {
            Camera caMain = HUDMesh.GetHUDMainCamera();

            if (m_cmdBuffer == null)
            {
                m_cmdBuffer = new CommandBuffer();
            }
            else
            {
                if(m_renderCameara != null)
                    m_renderCameara.RemoveCommandBuffer(CameraEvent.AfterImageEffects, m_cmdBuffer);
            }
            m_cmdBuffer.Clear();
            m_renderCameara = null;
            if (m_bOpenUI || m_bStartDark)
                return;
            
            if (!m_bHideAllTitle)
            {
                m_DynamicBatcher.m_MeshRender.RenderTo(m_cmdBuffer);
                m_StaticBatcher.m_MeshRender.RenderTo(m_cmdBuffer);
            }
            else
            {
                m_DynamicBatcher.m_MeshRender.OnCacelRender();
                m_StaticBatcher.m_MeshRender.OnCacelRender();
            }
            // 添加对话
            HUDTalk.HUDTalkRender.Instance.TryRenderTalk(m_cmdBuffer);
                
            if (m_cmdBuffer.sizeInBytes > 0)
            {
                m_renderCameara = caMain;
                caMain.AddCommandBuffer(CameraEvent.AfterImageEffects, m_cmdBuffer);
            }
        }
    }
};
