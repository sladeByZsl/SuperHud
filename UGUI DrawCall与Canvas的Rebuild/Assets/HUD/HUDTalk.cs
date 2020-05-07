using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

///////////////////////////////////////////////////////////
//
//  Written by              ：laishikai
//  Copyright(C)            ：成都博瑞梦工厂
//  ------------------------------------------------------
//  功能描述                ：角色头顶气泡对话管理模块
//
///////////////////////////////////////////////////////////

// 为了解决气泡对话堆叠的问题，每一个气泡单独使用一批DrawCall, 因为同时并不会存在太多的气泡
class HUDTalk : HUDTitleBase
{
    Transform m_tf;
    HUDRender m_MeshRender = new HUDRender();
    BetterList<HUDVertex> m_HyperlinkNode = new BetterList<HUDVertex>();
    float m_fStartTime;
    float m_fEndTime; // 结束的时间
    string m_szTalk;
    bool m_bHaveHyperlink = false;

    void  ReleaseTalk()
    {
        ClearSprite();
        m_MeshRender.Release();
        m_HyperlinkNode.ClearAnSetNull();
    }
    void  Render(CommandBuffer cmdBuffer)
    {
        m_MeshRender.RenderTo(cmdBuffer);
    }

    void  CheckPos(bool bCameraDirty, Vector3 vCameraPos)
    {
        if(m_tf == null)
        {
            m_fEndTime = 0.0f;
            return;
        }
        Vector3 vPos = m_tf.position;
        vPos.y += m_fOffsetY;
        if (!bCameraDirty)
        {
            Vector3 v = vPos - m_vPos;
            if (v.x * v.x + v.y * v.y + v.z * v.z < 0.000001f)
                return;
        }
        m_vPos = vPos;
        CaleCameraScale(vCameraPos);
        Camera caMain = HUDMesh.GetHUDMainCamera();
        if (caMain != null)
        {
            m_vScreenPos = caMain.WorldToScreenPoint(vPos);
            OnChangeScreenPos();
        }
    }
    
    void FillMesh()
    {
        m_MeshRender.FillMesh();
    }
    
    void  ShowTalk(string szTalk, int nColorIndex = 0)
    {
        if (nColorIndex < 0)
            nColorIndex = 0;
        else if (nColorIndex >= HudSetting.Instance.TalkTitle.Length)
            nColorIndex = HudSetting.Instance.TalkTitle.Length - 1;

        HudTitleAttribute titleAttrib = HudSetting.Instance.TalkTitle[nColorIndex];

        int nTalkWidth = HudSetting.Instance.m_nTalkWidth;
        int nSpriteID = HudSetting.Instance.m_nTalkBk;
        UISpriteInfo sp = CAtlasMng.instance.GetSafeSpriteByID(nSpriteID);
        int nAtlasID = sp != null ? sp.m_nAtlasID : 0;

        // 先分析文本
        UIFont mFont = GetHUDTitleFont();
        HUDTextParse hudParse = HUDTextParse.Instance;
        hudParse.ParseText(szTalk);
        mFont.PrepareQueryText(hudParse.m_szText);
        int nFontH = mFont.GetFontHeight();
        m_szTalk = hudParse.m_szText;

        m_HyperlinkNode.Clear();

        // 先搞背景
        for (int i = 0; i<9; ++i)
        {
            HUDVertex node = HUDVertex.QueryVertex();
            node.WorldPos = m_vPos;
            node.ScreenPos = m_vScreenPos;
            node.SpriteID = nSpriteID;
            node.AtlasID = nAtlasID;
            node.Scale = 1f;
            node.Offset.Set(0f, 0f);
            node.Move.Set(0f, 0f);
            node.width = (short)nTalkWidth;
            node.height = (short)100;
            m_aSprite.Add(node);
        }

        int nCharGap = titleAttrib.CharGap;
        int nLineGap = titleAttrib.LineGap;
        HudTitleAttribute.Effect nStyle = titleAttrib.Style;
        float fShadowX = titleAttrib.OffsetX;
        float fShadowY = titleAttrib.OffsetY;
        Color32 clrLeftUp = titleAttrib.clrLeftUp;
        Color32 clrLeftDown = titleAttrib.clrLeftDown;
        Color32 clrRightUp = titleAttrib.clrRightUp;
        Color32 clrRightDown = titleAttrib.clrRightDown;
        Color32 clrShadow = titleAttrib.clrShadow;
        Color32 clrCustom;

        // 计算文本的行数
        CharacterInfo tempCharInfo = new CharacterInfo();
        HUDCharInfo[] Sprites = hudParse.m_Sprites;
        int nSpriteCount = hudParse.m_SpriteCount;
        
        int nX = 0;
        int nY = 0;
        char  ch;
        int nCurLineHeight = nFontH;
        int nLines = 0;
        int nTalkHeight = 0;
        short[] LineHeight = hudParse.LineHeight;
        int nWidthMax = 0;
        int nSpaceWidth = 0;
        for (int i = 0; i < nSpriteCount; ++i)
        {
            if (Sprites[i].bChar)
            {
                ch = Sprites[i].ch;
                if(ch == ' ')
                {
                    if (nSpaceWidth == 0)
                    {
                        mFont.GetCharacterInfo('o', ref tempCharInfo);
                        nSpaceWidth = HUDVertex.GetCharWidth(tempCharInfo);
                    }
                    Sprites[i].SpriteWidth = (short)nSpaceWidth;
                    Sprites[i].SpriteHeight = (short)nFontH;
                }
                else
                {
                    mFont.GetCharacterInfo(ch, ref tempCharInfo);
                    Sprites[i].SpriteWidth = (short)HUDVertex.GetCharWidth(tempCharInfo);
                    Sprites[i].SpriteHeight = (short)tempCharInfo.glyphHeight;
                }
            }
            else
            {
                if (Sprites[i].CharType == UIFontUnitType.UnitType_Icon)
                {
                    sp = CAtlasMng.instance.GetSafeSpriteByID(Sprites[i].SpriteID);
                    if (sp != null)
                    {
                        Sprites[i].SpriteWidth = (short)(sp.outer.width + 0.5f);
                        Sprites[i].SpriteHeight = (short)(sp.outer.height + 0.5f);
                    }
                }
                else if(Sprites[i].CharType == UIFontUnitType.UnitType_Gif)
                {
                    int nGifID = Sprites[i].SpriteID;
                    Sprites[i].SpriteWidth = (short)UISpriteGifManager.Instance.GetSpriteGifWidth(nGifID);
                    Sprites[i].SpriteHeight = (short)UISpriteGifManager.Instance.GetSpriteGifHeight(nGifID);
                }
            }
            if (nX + Sprites[i].SpriteWidth > nTalkWidth || Sprites[i].CharType == UIFontUnitType.UnitType_Enter)
            {
                LineHeight[nLines] = (short)nCurLineHeight;
                nTalkHeight += nCurLineHeight + nLineGap;
                nX = 0;
                nY = nTalkHeight;
                // 按下对齐
                nCurLineHeight = nFontH;
                ++nLines;
            }
            Sprites[i].nX = nX;
            Sprites[i].nY = nY;
            Sprites[i].nLine = nLines;
            nX += Sprites[i].SpriteWidth + nCharGap;
            if (nCurLineHeight < Sprites[i].SpriteHeight)
                nCurLineHeight = Sprites[i].SpriteHeight;
            if (nX > nWidthMax)
                nWidthMax = nX;
        }
        LineHeight[nLines] = (short)nCurLineHeight;
        nTalkHeight += nCurLineHeight;
        nTalkWidth = nWidthMax;
        nY = nTalkHeight;
        // 需要倒过来
        for (int i = 0; i < nSpriteCount; ++i)
        {
            Sprites[i].nY = nY - Sprites[i].nY - LineHeight[Sprites[i].nLine];
        }

        // -------- 更新
        for (int i = 0; i < nSpriteCount; ++i)
        {
            nX = Sprites[i].nX;
            nY = Sprites[i].nY;

            if (Sprites[i].bChar)
            {
                ch = Sprites[i].ch;
                mFont.GetCharacterInfo(ch, ref tempCharInfo);                
                if (nStyle != HudTitleAttribute.Effect.None)
                {
                    PushShadow(ref tempCharInfo, ch, nX, nY, clrShadow, fShadowX, fShadowY);
                    if (nStyle == HudTitleAttribute.Effect.Outline)
                    {
                        PushShadow(ref tempCharInfo, ch, nX, nY, clrShadow, fShadowX, -fShadowY);
                        PushShadow(ref tempCharInfo, ch, nX, nY, clrShadow, -fShadowX, fShadowY);
                        PushShadow(ref tempCharInfo, ch, nX, nY, clrShadow, -fShadowX, -fShadowY);
                    }
                }
                if (Sprites[i].bCustomColor)
                {
                    clrCustom = Sprites[i].CustomColor;
                    HUDVertex node = PushChar(ref tempCharInfo, ch, nX, nY, clrCustom, clrCustom, clrCustom, clrCustom);
                }
                else
                {
                    HUDVertex node = PushChar(ref tempCharInfo, ch, nX, nY, clrLeftUp, clrLeftDown, clrRightUp, clrRightDown);
                }
            }
            else
            {
                // 图片
                if (Sprites[i].CharType == UIFontUnitType.UnitType_Icon)
                {
                    HUDVertex node = PushSprite(Sprites[i].SpriteID, Sprites[i].SpriteWidth, Sprites[i].SpriteHeight, nX, nY);
                }
                else if(Sprites[i].CharType == UIFontUnitType.UnitType_Gif)
                {
                    m_bHaveHyperlink = true;
                    HUDGif gif = new HUDGif();
                    gif.InitGif(Sprites[i].SpriteID);
                    HUDVertex node = PushSprite(gif.GetSpriteID(), Sprites[i].SpriteWidth, Sprites[i].SpriteHeight, nX, nY);
                    node.hudGif = gif;
                    m_HyperlinkNode.Add(node);
                }
            }
        }
        int nTalkBkBorderWidth = HudSetting.Instance.TalkBorderWidth;
        int nTalkBkBorderHeight = HudSetting.Instance.TalkBorderHeight;
        // 填充背景气泡
        int nTalkBkWidth  = nTalkWidth + nTalkBkBorderWidth * 2;
        int nTalkBkHeight = nTalkHeight + nTalkBkBorderHeight * 2;
        for (int i = 0; i<9; ++i)
        {
            m_aSprite[i].width  = (short)nTalkBkWidth;
            m_aSprite[i].height = (short)nTalkBkHeight;
        }
        SlicedFill(nSpriteID, nTalkBkWidth, nTalkBkHeight, 0, 1.0f);
        Offset(0, 9, -nTalkBkBorderWidth - nTalkWidth / 2, -nTalkBkBorderHeight + HudSetting.Instance.m_nTalkBkOffsetY); // Y越小，越在屏幕下面
        Offset(9, m_aSprite.size, -nTalkWidth / 2, 0.0f);

        for (int i = 0; i<m_aSprite.size; ++i)
        {
            HUDVertex v = m_aSprite[i];
            if (v.hudMesh == null)
            {
                if (v.AtlasID != 0)
                    v.hudMesh = m_MeshRender.QueryMesh(v.AtlasID);
                else
                    v.hudMesh = m_MeshRender.FontMesh();
                v.hudMesh.PushHUDVertex(v);
            }
        }
    }
    void UpdateGif(ref bool bMeshDirth, float deltaTime)
    {
        for (int i = 0; i< m_HyperlinkNode.size; ++i)
        {
            HUDVertex v = m_HyperlinkNode[i];
            int nOldSpriteID = v.SpriteID;
            if (v.hudGif == null)
                continue ;
            v.hudGif.Update(deltaTime);
            int nNewSpriteID = v.hudGif.GetSpriteID();
            if(nOldSpriteID != nNewSpriteID)
            {
                bMeshDirth = true;
                if (v.hudMesh != null)
                    v.hudMesh.EraseHUDVertex(v);
                int nAtlasID = CAtlasMng.instance.GetAtlasIDBySpriteID(nNewSpriteID);
                v.hudMesh = m_MeshRender.QueryMesh(nAtlasID);
                v.hudMesh.PushHUDVertex(v);
                v.AtlasID = nAtlasID;
                v.SpriteID = nNewSpriteID;
                v.InitSprite(v.width, v.height);
                v.Scale = m_fScale;
            }
        }
    }

    void RebuildCharUV(UIFont uiFont)
    {
        CharacterInfo tempCharInfo = new CharacterInfo();
        
        uiFont.PrepareQueryText(m_szTalk);
        for (int i = m_aSprite.size - 1; i >= 0; --i)
        {
            HUDVertex v = m_aSprite[i];
            if (0 == v.AtlasID)
            {
                uiFont.GetCharacterInfo(v.ch, ref tempCharInfo);
                v.RebuildCharUV(tempCharInfo);
                if (v.hudMesh != null)
                    v.hudMesh.VertexDirty();
            }
        }
        m_MeshRender.OnChangeFont(uiFont);
    }
    public class HUDTalkRender
    {
        static HUDTalkRender s_HUDTalkRenderIns = null;
        public static HUDTalkRender Instance
        {
            get
            {
                if (s_HUDTalkRenderIns == null)
                {
                    s_HUDTalkRenderIns = new HUDTalkRender();
                }
                return s_HUDTalkRenderIns;
            }
        }
        List<HUDTalk> m_TalkList = new List<HUDTalk>();
        bool m_bMeshDirty = false;
        bool m_bHideAllTalk = false; // 是不是隐藏所有气泡
        // 一个对象的头顶只能一个气泡对话
        public void ShowTalk(Transform tf, string szTalk, float fOffsetY, float fShowTime, int nColorIndex = 0)
        {
            EraseTalk(tf);

            Camera caMain = HUDMesh.GetHUDMainCamera();

            if (fShowTime < 1f)
                fShowTime = HudSetting.Instance.m_fTalkShowTime;
            fOffsetY += HudSetting.Instance.m_fTalkOffsetY;

            Vector3 vPos = tf.position;
            HUDTalk talk = new HUDTalk();
            talk.m_tf = tf;
            talk.m_vPos = vPos;
            talk.m_fOffsetY = fOffsetY;
            talk.m_fStartTime = Time.time;
            talk.m_fEndTime = talk.m_fStartTime + fShowTime;
            vPos.y += fOffsetY;
            talk.m_vScreenPos = caMain.WorldToScreenPoint(vPos);
            talk.CaleCameraScale(caMain.transform.position);
            talk.ShowTalk(szTalk, nColorIndex);
            m_TalkList.Add(talk);
            m_bMeshDirty = true;            
        }
        void EraseTalk(Transform tf)
        {
            for(int i = m_TalkList.Count - 1; i>= 0; --i)
            {
                if(m_TalkList[i].m_tf == tf)
                {
                    m_bMeshDirty = true;
                    m_TalkList[i].ClearSprite();
                    m_TalkList[i].m_fEndTime = 0.0f; // 结束了
                }
            }
        }
        public bool  IsMeshDirty()
        {
            return m_bMeshDirty;
        }
        public void OnAllFontChanged(UIFont uiFont)
        {
            for(int i = 0; i<m_TalkList.Count; ++i)
            {
                m_TalkList[i].RebuildCharUV(uiFont);
                m_bMeshDirty = true;
            }
        }
        public bool IsHideAllTalk()
        {
            return m_bHideAllTalk;
        }
        // 功能：隐藏所有气泡
        public void HideAllTalk()
        {
            m_bHideAllTalk = true;
        }
        // 功能：显示所有气泡
        public void ShowAllTalk()
        {
            m_bHideAllTalk = false;
        }
        public void UpdateLogic(bool bCameraDirty, Vector3 vCameraPos)
        {
            for(int i = m_TalkList.Count - 1; i>=0; --i)
            {
                HUDTalk talk = m_TalkList[i];
                talk.CheckPos(bCameraDirty, vCameraPos);
                talk.FillMesh();
                if(talk.m_bHaveHyperlink)
                {
                    talk.UpdateGif(ref m_bMeshDirty, Time.deltaTime);
                }
                if (talk.m_fEndTime < Time.time)
                {
                    if(talk.m_aSprite.size > 0)
                        m_bMeshDirty = true;
                    talk.ClearSprite();
                }
            }
        }
        // 功能：尝试深度渲染
        public bool  TryRenderTalk(CommandBuffer cmdBuffer)
        {
            m_bMeshDirty = false;

            if (m_bHideAllTalk)
                return false;
            
            // 先删除旧的
            for (int i = m_TalkList.Count - 1; i >= 0; --i)
            {
                HUDTalk talk = m_TalkList[i];
                if (talk.m_fEndTime < Time.time)
                {
                    talk.ReleaseTalk();
                    m_TalkList.RemoveAt(i);
                }
            }
            int nOldSize = cmdBuffer.sizeInBytes;
            for (int i = 0; i<m_TalkList.Count; ++i)
            {
                m_TalkList[i].Render(cmdBuffer);
            }
            return cmdBuffer.sizeInBytes > nOldSize;
        }
    }

}
