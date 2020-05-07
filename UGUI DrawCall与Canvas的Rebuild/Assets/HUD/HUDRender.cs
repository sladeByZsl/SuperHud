using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

class HUDTitleBase
{
    public Vector3 m_vPos; // 对象的世界坐标
    public Vector2 m_vScreenPos; // 屏幕坐标
    public float m_fScale = 1.0f;  // 缩放
    public float m_fOffsetY;
    protected BetterList<HUDVertex> m_aSprite = new BetterList<HUDVertex>(); // 对应的图片顶点信息, 这个不要在外部设置

    static UIFont s_pFont = null;
    public static UIFont GetHUDTitleFont()
    {
        if (s_pFont == null)
        {
            s_pFont = UIPrefabLoader.LoadFont("Assets/UIPrefab/DFont.prefab") as UIFont;
        }
        return s_pFont;
    }

    protected void ClearSprite()
    {
        for (int i = m_aSprite.size - 1; i >= 0; --i)
        {
            HUDVertex v = m_aSprite[i];
            if (v.hudMesh != null)
                v.hudMesh.EraseHUDVertex(v);
            v.hudMesh = null;
            HUDVertex.ReleaseVertex(v);
            m_aSprite[i] = null;
        }
        m_aSprite.Clear();
    }

    protected void CaleCameraScale(Vector3 vCameraPos)
    {
        Vector3 vPos = m_vPos;
        float m_nearDistance = HudSetting.Instance.CameraNearDist;
        float m_farDistance = HudSetting.Instance.CameraFarDist;
        float m_minScale = HudSetting.Instance.m_fTitleScaleMin;
        float m_maxScale = HudSetting.Instance.m_fTitleScaleMax;
        float dis = Vector3.Distance(vPos, vCameraPos);
        float ratio = Mathf.Clamp01((dis - m_nearDistance) / (m_farDistance - m_nearDistance));
        float fScale = m_minScale * ratio + (1.0f - ratio) * m_maxScale;
        m_fScale = 1.0f / fScale;
    }

    protected void OnChangeScreenPos()
    {
        for (int i = m_aSprite.size - 1; i >= 0; --i)
        {
            HUDVertex v = m_aSprite[i];
            v.ScreenPos = m_vScreenPos;
            v.Scale = m_fScale;
            v.WorldPos = m_vPos;
            if (v.hudMesh != null)
                v.hudMesh.VertexDirty();
        }
    }

    protected void PrepareRebuildMesh()
    {
        for (int i = m_aSprite.size - 1; i >= 0; --i)
        {
            HUDVertex v = m_aSprite[i];
            v.hudMesh = null;
        }
    }

    protected void SetScale(float fScale)
    {
        for (int i = m_aSprite.size - 1; i >= 0; --i)
        {
            HUDVertex v = m_aSprite[i];
            v.Scale = fScale;
        }
    }

    protected void SlicedFill(int nSpriteID, int nWidth, int nHeight, int nStart, float fBloodPos)
    {
        UISpriteInfo sp = CAtlasMng.instance.GetSafeSpriteByID(nSpriteID);
        if (sp == null)
            return;
        if (fBloodPos < 0.0)
            fBloodPos = 0.0f;
        if (fBloodPos > 1.0f)
            fBloodPos = 1.0f;
        int nBloodWidth = (int)(nWidth * fBloodPos + 0.5f);

        int nAtlasID = sp.m_nAtlasID;
        Rect mOuterUV = sp.outer;
        Rect mInnerUV = sp.inner;
        int nOuterW = (int)(mOuterUV.width + 0.5f);
        int nOuterH = (int)(mOuterUV.height + 0.5f);
        //int nInnerW = (int)(mInnerUV.width + 0.5f);
        //int nInnerH = (int)(mInnerUV.height + 0.5f);
        int nW1 = (int)(mInnerUV.xMin - mOuterUV.xMin + 0.5f);
        int nH1 = (int)(mInnerUV.yMin - mOuterUV.yMin + 0.5f);
        int nW2 = (int)(mOuterUV.xMax - mInnerUV.xMax + 0.5f);
        int nH2 = (int)(mOuterUV.yMax - mInnerUV.yMax + 0.5f);
        UITexAtlas texAtlas = CAtlasMng.instance.GetAtlasByID(sp.m_nAtlasID);
        if (texAtlas != null && texAtlas.coordinates == UITexAtlas.Coordinates.Pixels)
        {
            mOuterUV = HUDVertex.ConvertToTexCoords(mOuterUV, texAtlas.texWidth, texAtlas.texHeight);
            mInnerUV = HUDVertex.ConvertToTexCoords(mInnerUV, texAtlas.texWidth, texAtlas.texHeight);
        }
        if (nOuterW > 0 && nW1 + nW2 > nBloodWidth)
        {
            nW1 = nBloodWidth * nW1 / nOuterW;
            nW2 = nBloodWidth - nW1;
        }
        if (nOuterH > 0 && nH1 + nH2 > nHeight)
        {
            nH1 = nHeight * nH1 / nOuterH;
            nH2 = nHeight - nH1;
        }
        //if (nOuterW > 0)
        //{
        //    nW1 = nW1 * nBloodWidth / nOuterW;
        //    nW2 = nW2 * nBloodWidth / nOuterW;
        //}
        //if (nOuterH > 0)
        //{
        //    nH1 = nH1 * nHeight / nOuterH;
        //    nH2 = nH2 * nHeight / nOuterH;
        //}

        int nMW = nBloodWidth - nW1 - nW2;
        int nMH = nHeight - nH1 - nH2;

        HUDVertex v0 = m_aSprite[nStart];
        HUDVertex v1 = m_aSprite[nStart + 1];
        HUDVertex v2 = m_aSprite[nStart + 2];
        HUDVertex v3 = m_aSprite[nStart + 3];
        HUDVertex v4 = m_aSprite[nStart + 4];
        HUDVertex v5 = m_aSprite[nStart + 5];
        HUDVertex v6 = m_aSprite[nStart + 6];
        HUDVertex v7 = m_aSprite[nStart + 7];
        HUDVertex v8 = m_aSprite[nStart + 8];

        //  (mOuterUV.xMin, mOuterUV.yMin)
        // 
        //                              (mInnerUV.xMin, mInnerUV.yMin)
        //                                                                   (mInnerUV.yMin, mInnerUV.yMax)
        // 
        //                                                                                 (mOuterUV.xMax, mOuterUV.yMax)
        // a6  a7   a8         a0   a1   a2
        // a3  a4   a5    ==>  a3   a4   a5
        // a0  a1   a2         a6   a7   a8
        float fIn_xMin = mInnerUV.xMin;
        float fIn_xMax = mInnerUV.xMax;
        float fIn_yMin = mInnerUV.yMin;
        float fIn_yMax = mInnerUV.yMax;
        float fOu_xMin = mOuterUV.xMin;
        float fOu_xMax = mOuterUV.xMax;
        float fOu_yMin = mOuterUV.yMin;
        float fOu_yMax = mOuterUV.yMax;

        float fX2 = nW1 + nMW;
        float fY2 = nH2 + nMH;
        v0.SlicedFill(nW1, nH2, 0f, 0f, fOu_xMin, fOu_yMin, fIn_xMin, fIn_yMin);
        v1.SlicedFill(nMW, nH2, nW1, 0f, fIn_xMin, fOu_yMin, fIn_xMax, fIn_yMin);
        v2.SlicedFill(nW2, nH2, fX2, 0f, fIn_xMax, fOu_yMin, fOu_xMax, fIn_yMin);

        v3.SlicedFill(nW1, nMH, 0f, nH2, fOu_xMin, fIn_yMin, fIn_xMin, fIn_yMax);
        v4.SlicedFill(nMW, nMH, nW1, nH2, fIn_xMin, fIn_yMin, fIn_xMax, fIn_yMax);
        v5.SlicedFill(nW2, nMH, fX2, nH2, fIn_xMax, fIn_yMin, fOu_xMax, fIn_yMax);

        v6.SlicedFill(nW1, nH1, 0f, fY2, fOu_xMin, fIn_yMax, fIn_xMin, fOu_yMax);
        v7.SlicedFill(nMW, nH1, nW1, fY2, fIn_xMin, fIn_yMax, fIn_xMax, fOu_yMax);
        v8.SlicedFill(nW2, nH1, fX2, fY2, fIn_xMax, fIn_yMax, fOu_xMax, fOu_yMax);

        v0.Scale = m_fScale;
        v1.Scale = m_fScale;
        v2.Scale = m_fScale;
        v3.Scale = m_fScale;
        v4.Scale = m_fScale;
        v5.Scale = m_fScale;
        v6.Scale = m_fScale;
        v7.Scale = m_fScale;
        v8.Scale = m_fScale;
    }

    protected HUDVertex PushSprite(int nSpriteID, int nWidth, int nHeight, float fx, float fy)
    {
        HUDVertex node = HUDVertex.QueryVertex();
        node.WorldPos = m_vPos;
        node.ScreenPos = m_vScreenPos;
        node.SpriteID = nSpriteID;
        node.Offset.Set(fx, fy);
        node.Move.Set(0f, 0f);
        node.InitSprite(nWidth, nHeight);
        node.Scale = m_fScale;

        m_aSprite.Add(node);
        return node;
    }
    protected HUDVertex PushChar(ref CharacterInfo tempCharInfo, char ch, float fx, float fy, Color32 clrLeftUp, Color32 clrLeftDown, Color32 clrRightUp, Color32 clrRightDown)
    {
        HUDVertex node = HUDVertex.QueryVertex();
        node.WorldPos = m_vPos;
        node.ScreenPos = m_vScreenPos;
        node.ch = ch;
        node.Offset.Set(fx, fy);
        node.Move.Set(0f, 0f);
        node.clrLU = clrLeftUp;
        node.clrLD = clrLeftDown;
        node.clrRD = clrRightDown;
        node.clrRU = clrRightUp;
        node.InitChar(tempCharInfo);
        node.Scale = m_fScale;
        m_aSprite.Add(node);

        return node;
    }
    protected HUDVertex PushShadow(ref CharacterInfo tempCharInfo, char ch, float fx, float fy, Color32 clrShadow, float fMoveX, float fMoveY)
    {
        HUDVertex node = HUDVertex.QueryVertex();
        node.WorldPos = m_vPos;
        node.ScreenPos = m_vScreenPos;
        node.ch = ch;
        node.Offset.Set(fx, fy);
        node.Move.Set(fMoveX, fMoveY);
        node.clrLU = clrShadow;
        node.clrLD = clrShadow;
        node.clrRD = clrShadow;
        node.clrRU = clrShadow;
        node.InitChar(tempCharInfo);
        node.Scale = m_fScale;
        m_aSprite.Add(node);
        return node;
    }

    protected void OffsetXY(int nStart, int nEnd, float fOffsetX, float fOffsetY)
    {
        for (int i = nStart; i < nEnd; ++i)
        {
            HUDVertex v = m_aSprite[i];
            v.Offset.x += fOffsetX;
            v.Offset.y += fOffsetY - v.height * 0.5f;
        }
    }
    protected void Offset(int nStart, int nEnd, float fOffsetX, float fOffsetY)
    {
        for (int i = nStart; i < nEnd; ++i)
        {
            HUDVertex v = m_aSprite[i];
            v.Offset.x += fOffsetX;
            v.Offset.y += fOffsetY;
        }
    }
    // 功能：下对齐
    // 参数：nStart, nEnd - 开始与结束的位置
    //       fHeight - 高度
    protected void AlignDown(int nStart, int nEnd, float fOffsetX, float fHeight)
    {
        for (int i = nStart; i < nEnd; ++i)
        {
            HUDVertex v = m_aSprite[i];
            v.Offset.x += fOffsetX;
            v.Offset.y += fHeight - v.height;
        }
    }
};

class HUDRender
{
    BetterList<HUDMesh> m_MeshList = new BetterList<HUDMesh>(); // 所有的
    BetterList<HUDMesh> m_ValidList = new BetterList<HUDMesh>(); // 当前有效的
    HUDMesh m_MeshFont;
    HUDMesh m_curFontMesh;
    public bool m_bMeshDirty;

    public HUDMesh QueryMesh(int nAtlasID)
    {
        // 先从当前有效的Mesh的找
        for (int i = m_ValidList.size - 1; i >= 0; --i)
        {
            if (m_ValidList[i].AtlasID == nAtlasID)
                return m_ValidList[i];
        }
        // 从所有的里面找
        for(int i = m_MeshList.size - 1; i>= 0; --i)
        {
            if(m_MeshList[i].AtlasID == nAtlasID)
            {
                m_ValidList.Add(m_MeshList[i]);
                m_MeshList[i].SetAtlasID(nAtlasID);
                m_bMeshDirty = true;
                return m_MeshList[i];
            }
        }
        HUDMesh pHudMesh = new HUDMesh();
        pHudMesh.SetAtlasID(nAtlasID);
        m_MeshList.Add(pHudMesh);
        m_ValidList.Add(pHudMesh);
        m_bMeshDirty = true;
        return pHudMesh;
    }
    public HUDMesh FontMesh()
    {
        if (m_curFontMesh != null)
            return m_curFontMesh;
        if (m_MeshFont == null)
        {
            m_MeshFont = new HUDMesh();
            m_MeshList.Add(m_MeshFont);
        }
        m_curFontMesh = m_MeshFont;
        m_ValidList.Add(m_MeshFont);
        m_bMeshDirty = true;

        UIFont uiFont = HUDTitleInfo.GetHUDTitleFont();
        m_MeshFont.SetFont(uiFont);
        return m_MeshFont;
    }

    public void OnChangeFont(UIFont uiFont)
    {
        if(m_MeshFont != null)
        {
            m_MeshFont.SetFont(uiFont);
        }
    }

    public void Release()
    {
        for (int i = 0; i < m_MeshList.size; ++i)
        {
            m_MeshList[i].Release();
            m_MeshList[i] = null;
        }
        m_MeshFont = null;
        m_curFontMesh = null;
        m_MeshList.Clear();
        m_ValidList.Clear();
    }

    // 功能：快速清队模型的顶点
    public void FastClearVertex()
    {
        m_curFontMesh = null;
        for (int i = m_ValidList.size - 1; i >= 0; --i)
        {
            HUDMesh mesh = m_ValidList[i];
            mesh.FastClearVertex();
        }
        m_ValidList.Clear();
    }

    // 功能：更新模型顶点(每帧更新)
    public void FillMesh()
    {
        for (int i = m_ValidList.size - 1; i >= 0; --i)
        {
            HUDMesh mesh = m_ValidList[i];
            if (mesh.IsDirty())
            {
                int nOldSpriteNumb = mesh.OldSpriteNumb;
                mesh.UpdateLogic();
                int nCurSpriteNumb = mesh.SpriteNumb;
                if (nOldSpriteNumb != 0 && nCurSpriteNumb == 0)
                    m_bMeshDirty = true;
                else if (nOldSpriteNumb == 0 && nCurSpriteNumb != 0)
                    m_bMeshDirty = true;
                if(nCurSpriteNumb == 0)
                {
                    m_ValidList.RemoveAt(i);
                    if(m_MeshFont == mesh)
                    {
                        m_curFontMesh = null;
                    }
                    else
                    {
                        mesh.CleanAllVertex();
                    }
                }
            }
        }
    }
    public void OnCacelRender()
    {
        m_bMeshDirty = false;
    }
    public void RenderTo(CommandBuffer cmdBuffer)
    {
        m_bMeshDirty = false;
        if (m_ValidList.size == 0)
            return;
        Matrix4x4 matWorld = Matrix4x4.identity;
        for (int i = 0, nSize = m_ValidList.size; i<nSize; ++i)
        {
            HUDMesh mesh = m_ValidList[i];
            if(mesh.SpriteNumb > 0 && mesh.AtlasID != 0 )
            {
                cmdBuffer.DrawMesh(mesh.m_Mesh, matWorld, mesh.m_mat);
            }
        }
        for (int i = 0, nSize = m_ValidList.size; i < nSize; ++i)
        {
            HUDMesh mesh = m_ValidList[i];
            if (mesh.SpriteNumb > 0 && mesh.AtlasID == 0)
            {
                cmdBuffer.DrawMesh(mesh.m_Mesh, matWorld, mesh.m_mat);
            }
        }
    }
}
