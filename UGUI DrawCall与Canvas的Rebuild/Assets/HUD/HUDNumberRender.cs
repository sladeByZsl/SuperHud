using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

///////////////////////////////////////////////////////////
//
//  Written by              ：laishikai
//  Copyright(C)            ：成都博瑞梦工厂
//  ------------------------------------------------------
//  功能描述                ：角色伤害数字渲染管理模块
//
///////////////////////////////////////////////////////////

// 伤害数字渲染
enum HUDNumberRenderType
{
    HUD_SHOW_EXP_ADD,       // e+ 主角获得经验
    HUD_SHOW_LIFE_EXP,      // l+ 主角获得历练
    HUD_SHOW_MONEY_ADD,     // m+ 主角获得Money
    HUD_SHOW_XINFA,         // X+ 主角获得新法
    HUD_SHOW_HP_HURT,       // - 主角掉血
    HUD_SHOW_COMMON_ATTACK, // - 其他角色掉血
    HUD_SHOW_CT_ATTACKED,   // 主角被爆击
    HUD_SHOW_CT_ATTACK,     // 主角爆击其他角色
    HUD_SHOW_ABSORB,        // 吸收
    HUB_SHOW_DODGE,         // 闪躲
    HUD_SHOW_RECOVER_HP,    // 回血
    HUD_SHOW_PET_ATTACK, // - 武魂造成的伤害数值

    // ... 可以在这后面继续添加新的数字类型


    HUD_SHOW_NUMBER,
   
};

class HUDNumberEntry
{
    public Transform m_tf;
    public HUDNumberEntry m_pNext;
    public HUDNumberRenderType m_nType;
    public Vector3 m_vPos; // 对象的世界坐标
    public Vector2 m_vScreenPos; // 屏幕坐标
    public Vector2 m_vInitOffset; // 初始化位移
    public Vector2 m_vMove;  // 位移
    public float m_fAniScale;  // 动画缩放
    public float m_fScale;
    public float m_fAlpha;  // 半透
    public BetterList<HUDVertex> m_aSprite = new BetterList<HUDVertex>(); // 对应的图片ID
    public int m_nWidth = 0; // 宽度
    public int m_nHeight = 0;
    public int m_nSpriteGap = 0;
    public float m_fStartTime = 0.0f;
    public bool m_bStop = false;
    public void reset()
    {
        m_tf = null;
        m_vPos = Vector3.zero;
        m_vScreenPos = Vector2.zero;
        m_vInitOffset = Vector2.zero;
        m_vMove = Vector2.zero;
        m_fScale = 1.0f;
        m_fAniScale = 1.0f;
        m_fAlpha = 1.0f;
        ReleaseVertex();
        m_nWidth = 0;
        m_nHeight = 0;
        m_nSpriteGap = 0;
        m_bStop = false;
    }
    public void ReleaseVertex()
    {
        for(int i = m_aSprite.size - 1; i >= 0; --i)
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

    // 功能：更新屏幕位置
    public bool UpdateScreenPos( ref HudAnimAttibute attrib)
    {
        if (attrib.ScreenAlign)
            return false;

        if(m_tf != null)
        {
            Vector3 vPos = m_tf.position;
            Vector3 v = vPos - m_vPos;
            if(v.x * v.x + v.y * v.y + v.z * v.z > 0.00001f)
            {
                Camera caMain = HUDMesh.GetHUDMainCamera();
                if(caMain != null)
                {
                    m_vPos = vPos;
                    m_vScreenPos = caMain.WorldToScreenPoint(vPos);
                    Vector3 vCameraPos = caMain.transform.position;
                    CaleCameraScale(vCameraPos);
                    return true;
                }
            }
        }
        return false;
    }

    public void CaleCameraScale(Vector3 vCameraPos)
    {
        Vector3 vPos = m_vPos;
        float m_nearDistance = HudSetting.Instance.CameraNearDist;
        float m_farDistance = HudSetting.Instance.CameraFarDist;
        float m_minScale = HudSetting.Instance.m_fNumberScaleMin;
        float m_maxScale = HudSetting.Instance.m_fNumberScaleMax;
        float dis = Vector3.Distance(vPos, vCameraPos);
        float ratio = Mathf.Clamp01((dis - m_nearDistance) / (m_farDistance - m_nearDistance));
        float fScale = m_minScale * ratio + (1.0f - ratio) * m_maxScale;
        m_fScale = 1.0f / fScale;
    }

    public void PushSprite(float y, int nSpriteID)
    {
        HUDVertex node = HUDVertex.QueryVertex();
        node.WorldPos = m_vPos;
        node.ScreenPos = m_vScreenPos;
        node.Offset.Set(m_nWidth, y);
        node.SpriteID = nSpriteID;
        node.InitSprite();
        m_aSprite.Add(node);
        m_nWidth += node.width + m_nSpriteGap;
        if (m_nHeight < node.height)
            m_nHeight = node.height;
    }
    public void MakeLeft()
    {
        m_nWidth -= m_nSpriteGap;
        MoveAll(0.0f);
    }
    // 居中
    public void MakeCenter()
    {
        m_nWidth -= m_nSpriteGap;
        float fHalfW = m_nWidth * 0.5f;
        MoveAll(fHalfW);
    }
    // 右对齐
    public void MakeRight()
    {
        m_nWidth -= m_nSpriteGap;
        float fMoveX = m_nWidth;
        MoveAll(fMoveX);
    }
    void  MoveAll(float fMoveX)
    {
        float fHalfH = m_nHeight * 0.5f;
        for (int i = 0, nSize = m_aSprite.size; i < nSize; ++i)
        {
            float fH = m_aSprite[i].height;
            m_aSprite[i].Offset.x -= fMoveX;
            m_aSprite[i].Offset.y -= fH * 0.5f - fHalfH;
        }
    }
};

class HUDSprtieSetting
{
    public int m_nHeadID; // 开头的图片ID
    public int m_nAddID;  // + 号
    public int m_nSubID;  // - 号
    public int[] m_NumberID = new int[10]; // 数字ID
    public void InitNumber(string szHeadName, string szPrefix)
    {
        CAtlasMng pMng = CAtlasMng.instance;
        m_nHeadID = pMng.SpriteNameToID(szHeadName);
        m_nAddID = pMng.SpriteNameToID(szPrefix + '+');
        m_nSubID = pMng.SpriteNameToID(szPrefix + '-');
        for(int i = 0; i<10; ++i)
        {
            m_NumberID[i] = pMng.SpriteNameToID(szPrefix + i.ToString());
        }
    }
};

class HUDNumberRender
{
    static HUDNumberRender s_pHUDNumberRenderIns = null;
    public static HUDNumberRender Instance
    {
        get
        {
            if (s_pHUDNumberRenderIns == null)
            {
                s_pHUDNumberRenderIns = new HUDNumberRender();
            }
            return s_pHUDNumberRenderIns;
        }
    }
    bool m_bInit = false;
    bool m_bAddUpdateLogic = false;
    bool m_bKeep1280x720 = true;
    bool m_bAddCommandBuffer = false;
    float m_fLastUpdateLogicTime = 0.0f;

    HudAnimAttibute[] m_aTtribute;
    HUDNumberEntry m_aInvalid;
    HUDSprtieSetting[] m_Settings;
    float m_fDurationTime = 2.0f; // 动画持续时间

    HUDNumberEntry m_ValidList;
    BetterList<int> m_tempNumb = new BetterList<int>();
    HUDRender m_MeshRender = new HUDRender();
    bool m_bMeshDirty = false;
    bool m_bCalcCameraScale = false;
    bool m_bCaleScreenScale = false;
    float m_fScreenScaleX = 1.0f;
    float m_fScreenScaleY = 1.0f;

    bool m_bOpenUI = false;
    bool m_bOldOpenUI = false;

    bool m_bStartDark = false;
    bool m_bStartMovie = false;
    bool m_bOldStartDark = false;
    float m_fStartDarkTime = 0.0f;
    float m_fDarkTime = 0.0f;

    Camera m_oldCamera;

    Camera m_renderCamera;
    CommandBuffer s_cmdBuffer = new CommandBuffer();

    void InitHUDSetting(HudSetting hudSetting)
    {
        m_aTtribute = hudSetting.NumberAttibute;
        m_fDurationTime = hudSetting.m_fDurationTime;
        m_bKeep1280x720 = hudSetting.m_bKeep1280x720;
    }

    public static void ApplySetting(HudAniSetting hudSetting)
    {
        if (s_pHUDNumberRenderIns != null)
            s_pHUDNumberRenderIns.InitHUDSetting(HudSetting.Instance);
    }

    public void OnLeaveGame()
    {
        while(m_ValidList != null)
        {
            HUDNumberEntry pDel = m_ValidList;
            m_ValidList = m_ValidList.m_pNext;
            OnErase(pDel);
        }
        ReleaseCmmmandBuffer();
        m_MeshRender.Release();
    }

    void ReleaseCmmmandBuffer()
    {
        if (m_bAddCommandBuffer)
        {
            m_bAddCommandBuffer = false;
            if (m_renderCamera != null)
                m_renderCamera.RemoveCommandBuffer(CameraEvent.AfterImageEffects, s_cmdBuffer);
            m_renderCamera = null;
            s_cmdBuffer.Clear();
        }
    }

    // 功能：开始NPC对话
    public void OnOpenUI()
    {
        // 需要隐藏所有的文字
        m_bOpenUI = true;

        ReleaseCmmmandBuffer();
    }

    // 功能：结束NPC对话
    public void OnCloseUI()
    {
        m_bOpenUI = false;
    }
    public void OnStartScreenDark(float fTime)
    {
        m_bStartDark = true;
        m_fStartDarkTime = Time.time;
        m_fDarkTime = fTime;
        ReleaseCmmmandBuffer();
    }
    public void OnEndScreenDark()
    {
        m_bStartDark = false;
    }

    public void OnBeginMovie()
    {
        m_bStartMovie = true;
        CleanCurrentNumber();
        ReleaseCmmmandBuffer();
    }

    public void OnEndMovie()
    {
        m_bStartMovie = false;
    }

    void CleanCurrentNumber()
    {
        while (m_ValidList != null)
        {
            HUDNumberEntry pDel = m_ValidList;
            m_ValidList = m_ValidList.m_pNext;
            pDel.m_pNext = null;
            OnErase(pDel);
        }
        m_bMeshDirty = true;
    }

    void InitHUD()
    {
        int nNumber = (int)HUDNumberRenderType.HUD_SHOW_NUMBER;
        m_aTtribute = new HudAnimAttibute[nNumber];
        m_Settings = new HUDSprtieSetting[nNumber];

        m_aTtribute = HudSetting.Instance.NumberAttibute;
        
        for (int i = 0; i < nNumber; ++i)
        {
            m_Settings[i] = new HUDSprtieSetting();
        }
        m_Settings[(int)HUDNumberRenderType.HUD_SHOW_EXP_ADD].InitNumber("经验", "green"); // 主角获得经验
        m_Settings[(int)HUDNumberRenderType.HUD_SHOW_LIFE_EXP].InitNumber("历练", "green"); // 主角获得历练
        m_Settings[(int)HUDNumberRenderType.HUD_SHOW_MONEY_ADD].InitNumber("铜钱", "green"); // 主角获得Money
        m_Settings[(int)HUDNumberRenderType.HUD_SHOW_XINFA].InitNumber("心法", "green"); // 主角获得心法
        m_Settings[(int)HUDNumberRenderType.HUD_SHOW_HP_HURT].InitNumber(string.Empty, "red"); // 主角掉血
        m_Settings[(int)HUDNumberRenderType.HUD_SHOW_COMMON_ATTACK].InitNumber(string.Empty, "red"); // 其他角色掉血
        m_Settings[(int)HUDNumberRenderType.HUD_SHOW_CT_ATTACKED].InitNumber("被爆击", "red"); // 主角被爆击
        m_Settings[(int)HUDNumberRenderType.HUD_SHOW_CT_ATTACK].InitNumber("爆击", "red"); // 主角爆击其他角色
        m_Settings[(int)HUDNumberRenderType.HUD_SHOW_ABSORB].InitNumber("吸收", "green"); // 吸收
        m_Settings[(int)HUDNumberRenderType.HUB_SHOW_DODGE].InitNumber("闪躲", "green"); // 闪躲
        m_Settings[(int)HUDNumberRenderType.HUD_SHOW_RECOVER_HP].InitNumber(string.Empty, "green"); // 回血
        m_Settings[(int)HUDNumberRenderType.HUD_SHOW_PET_ATTACK].InitNumber(string.Empty, "green"); // 武魂伤害数值
    }
    
    HUDNumberEntry  QueryHudNumber(HUDNumberRenderType nType)
    {
        int nIndex = (int)nType;
        HUDNumberEntry pNode = m_aInvalid;
        if (pNode != null)
        {
            m_aInvalid = pNode.m_pNext;
            pNode.m_pNext = null;
        }
        if (pNode == null)
        {
            pNode = new HUDNumberEntry();
            pNode.m_nType = nType;
        }
        return pNode;
    }

    void  ReleaseHudNumber(HUDNumberEntry pNode)
    {
        if (pNode != null)
        {
            int nIndex = (int)pNode.m_nType;
            pNode.m_pNext = m_aInvalid;
            m_aInvalid = pNode;
        }
    }

    bool m_bPuase = false;
    float m_currentDuration = 0.0f;

    // 功能：执行数字的动画效果
    void PlayAnimation(HUDNumberEntry pNode, bool bFirst)
    {
        int nIndex = (int)pNode.m_nType;
        float currentDuration = (Time.time - pNode.m_fStartTime);
        if (m_bPuase)
            currentDuration = m_currentDuration;
        HudAnimAttibute attrib = m_aTtribute[nIndex];
        bool bDirty = false;
        float fAlpha = attrib.AlphaCurve.Evaluate(currentDuration);                 //淡出效果;
        float fScale = attrib.ScaleCurve.Evaluate(currentDuration);                 //变大效果;
        float fPos = attrib.MoveCurve.Evaluate(currentDuration);                    //Y轴移动效果;
        float fOldAlpha = pNode.m_fAlpha;
        float fOldScale = pNode.m_fAniScale;
        float fOldMoveX = pNode.m_vMove.x;
        float fOldMoveY = pNode.m_vMove.y;
        pNode.m_fAlpha = fAlpha;
        pNode.m_fAniScale = fScale;
        if(attrib.ScreenAlign)
        {
            pNode.m_vMove.x = 0.0f;
            pNode.m_vMove.y = fPos * m_fScreenScaleY;
        }
        else
        {
            pNode.m_vMove.x = attrib.OffsetX * m_fScreenScaleX;
            pNode.m_vMove.y = (attrib.OffsetY + fPos) * m_fScreenScaleY;
        }
        pNode.m_bStop = currentDuration > m_fDurationTime;
        if (m_bPuase)
            pNode.m_bStop = false;

        int nAlpha = (int)(fAlpha * 255.0f + 0.5f);
        if (nAlpha < 0)
            nAlpha = 0;
        if (nAlpha > 255)
            nAlpha = 255;
        byte alpha = (byte)nAlpha;
        
        if (!bFirst)
            bDirty = pNode.UpdateScreenPos(ref attrib);
        else
            bDirty = true;

        if(!bDirty)
        {
            if (Mathf.Abs(fOldAlpha - pNode.m_fAlpha) > 0.0001f)
                bDirty = true;
            if (!bDirty && Mathf.Abs(fOldScale - pNode.m_fScale) > 0.0001f)
                bDirty = true;
            if (!bDirty && Mathf.Abs(fOldMoveX - pNode.m_vMove.x) > 0.0001f)
                bDirty = true;
            if (!bDirty && Mathf.Abs(fOldMoveY - pNode.m_vMove.y) > 0.0001f)
                bDirty = true;
        }
        if (!bDirty)
            return;

        fScale *= pNode.m_fScale;

        // 更新顶点数据
        Vector2 vScreenPos = pNode.m_vScreenPos;
        Vector2 vMove = pNode.m_vMove;
        for (int i = pNode.m_aSprite.size - 1; i>=0; --i)
        {
            HUDVertex v = pNode.m_aSprite[i];
            v.Move  = vMove;
            v.WorldPos = pNode.m_vPos;
            v.ScreenPos = vScreenPos;
            v.Scale = fScale;
            v.clrLU.a = alpha;
            v.clrLD.a = alpha;
            v.clrRU.a = alpha;
            v.clrRD.a = alpha;
            v.hudMesh.VertexDirty();
        }
    }
    
    void  OnPush(HUDNumberEntry pNode)
    {
        for(int i = 0; i<pNode.m_aSprite.size; ++i)
        {
            HUDMesh hudMesh = m_MeshRender.QueryMesh(pNode.m_aSprite[i].AtlasID);
            pNode.m_aSprite[i].hudMesh = hudMesh;
            hudMesh.PushHUDVertex(pNode.m_aSprite[i]);
        }
    }
    void  OnErase(HUDNumberEntry pNode)
    {
        // 释放模型的数据吧
        pNode.ReleaseVertex();
        ReleaseHudNumber(pNode);
    }
    
    void  FillMeshRender()
    {
        m_MeshRender.FillMesh();
        if (m_MeshRender.m_bMeshDirty)
            m_bMeshDirty = true;
        if(!m_bMeshDirty)
        {
            Camera caMain = HUDMesh.GetHUDMainCamera();
            if(caMain != m_oldCamera)
            {
                m_oldCamera = caMain;
                m_bMeshDirty = true;
            }
        }

        if(m_bMeshDirty)
        {
            m_bMeshDirty = false;
            if(m_renderCamera != null)
            {
                m_renderCamera.RemoveCommandBuffer(CameraEvent.AfterImageEffects, s_cmdBuffer);
                m_renderCamera = null;
                m_bAddCommandBuffer = false;
            }
            Camera caMain = HUDMesh.GetHUDMainCamera();
            s_cmdBuffer.Clear();
            if (caMain == null || m_bOpenUI || m_bStartDark || m_bStartMovie)
                return;
            m_MeshRender.RenderTo(s_cmdBuffer);
            if(s_cmdBuffer.sizeInBytes > 0 && caMain != null)
            {
                m_renderCamera = caMain;
                caMain.AddCommandBuffer(CameraEvent.AfterImageEffects, s_cmdBuffer);
                m_bAddCommandBuffer = true;
            }
        }
    }
    
    void CaleScreenScale()
    {
        m_bCaleScreenScale = true;
        m_fScreenScaleX = Screen.width / 1280.0f;
        m_fScreenScaleY = Screen.height / 720.0f;
        m_fScreenScaleX *= HUDMesh.s_fNumberScale;
        m_fScreenScaleY *= HUDMesh.s_fNumberScale;
    }

    void  UpdateLogic(float delta)
    {
        // 计算屏幕的缩放
        CaleScreenScale();

        HUDNumberEntry pNode = m_ValidList;
        HUDNumberEntry pLast = m_ValidList;
        while (pNode != null)
        {
            PlayAnimation(pNode, false);
            if(pNode.m_bStop)
            {
                HUDNumberEntry pDel = pNode;
                if (pNode == m_ValidList)
                {
                    m_ValidList = m_ValidList.m_pNext;
                    pLast = m_ValidList;
                }
                else
                {
                    pLast.m_pNext = pNode.m_pNext;
                }
                pNode = pNode.m_pNext;
                OnErase(pDel);                
                continue;
            }
            pLast = pNode;
            pNode = pNode.m_pNext;
        }
        if (m_ValidList == null)
            m_bMeshDirty = true;

        // 处理二级面板开启
        // 屏幕变黑后的处理
        if (m_bStartDark != m_bOldStartDark)
        {
            m_bOldStartDark = m_bStartDark;
            m_bMeshDirty = true;
        }
        else if (m_bStartDark)
        {
            if (m_fStartDarkTime + m_fDarkTime < Time.time)
                m_bStartDark = false;
        }
        bool bOpenUI = m_bOpenUI || m_bStartDark || m_bStartMovie;
        if(m_bOldOpenUI != bOpenUI)
        {
            m_bOldOpenUI = bOpenUI;
            m_bMeshDirty = true;
        }

        FillMeshRender();
        if (m_ValidList == null)
        {
            if(m_fLastUpdateLogicTime + 5.0f < Time.time)
            {
                m_bAddUpdateLogic = false;
                UpdateManager.DelUpdate(UpdateLogic);
            }
            CleanAllMeshRender();
        }
        else
        {
            m_fLastUpdateLogicTime = Time.time;
        }
    }

    // 功能：清除所有的模型渲染
    void CleanAllMeshRender()
    {
        m_MeshRender.FastClearVertex(); // CleanAllVertex
        ReleaseCmmmandBuffer();
    }

    // 功能：添加一个显示的数字
    public void  AddHudNumber(Transform tf, HUDNumberRenderType nType, int nNumber, bool bShowHead, bool bShowAdd, bool bShowSub)
    {
        if (m_bOpenUI || m_bStartDark || m_bStartMovie)
            return ;

        Vector3 vPos = tf.position;
        int nIndex = (int)nType;
        //if (nIndex < 0 || nIndex > (int)HUDNumberRenderType.HUD_SHOW_NUMBER)
        //    return;
        Camera caMain = HUDMesh.GetHUDMainCamera();
        if (caMain == null)
            return;
        if(!m_bInit)
        {
            m_bInit = true;
            InitHUD();
        }
        if (!m_bAddUpdateLogic)
        {
            m_bMeshDirty = true;
            UpdateManager.AddLateUpdate(null, 0, UpdateLogic);
        }
        HudAnimAttibute attrib = m_aTtribute[nIndex];

        HUDNumberEntry pNode = QueryHudNumber(nType);
        pNode.m_nType = nType;
        pNode.m_pNext = m_ValidList;
        m_ValidList = pNode;

        if(nNumber < 0)
        {
            bShowSub = true;
            nNumber = -nNumber;
        }

        pNode.reset();

        // 初始化
        pNode.m_nSpriteGap = attrib.SpriteGap;
        pNode.m_fStartTime = Time.time;
        pNode.m_tf = tf;
        pNode.m_vPos = vPos;
        
        if(caMain != null)
        {
            // 如果按屏幕对齐
            if (attrib.ScreenAlign)
            {
                Vector3 v1 = caMain.WorldToScreenPoint(vPos);
                v1.x = attrib.OffsetX;
                v1.y = attrib.OffsetY;
                float fScaleX = (float)Screen.width / 1280.0f;
                float fScaleY = (float)Screen.height / 1280.0f;

                if (attrib.ScreenAlignType == HUDAlignType.align_left)
                {
                    v1.x = attrib.OffsetX;
                    v1.y = attrib.OffsetY;
                }
                else if(attrib.ScreenAlignType == HUDAlignType.align_right)
                {
                    v1.x = 1280.0f - attrib.OffsetX;
                    v1.y = attrib.OffsetY;
                }
                else
                {
                    v1.x = Screen.width / 2.0f + attrib.OffsetX;
                    v1.y = attrib.OffsetY;
                }
                v1.x *= fScaleX;
                v1.y *= fScaleY;

                pNode.m_vScreenPos = v1;
                vPos = caMain.ScreenToWorldPoint(v1);
                pNode.m_vPos = vPos;

                Vector3 vCameraPos = caMain.transform.position;
                pNode.CaleCameraScale(vCameraPos);
            }
            else
            {
                pNode.m_vScreenPos = caMain.WorldToScreenPoint(vPos);
                Vector3 vCameraPos = caMain.transform.position;
                pNode.CaleCameraScale(vCameraPos);
            }
        }

        float y = 0.0f;
        HUDSprtieSetting pSetting = m_Settings[nIndex];
        if(bShowHead && pSetting.m_nHeadID != 0)
        {
            pNode.PushSprite(y, pSetting.m_nHeadID);
        }
        bool bHaveNumber = true;
        if (nType == HUDNumberRenderType.HUD_SHOW_ABSORB
            || nType == HUDNumberRenderType.HUB_SHOW_DODGE)
            bHaveNumber = false;
        if (bHaveNumber)
        {
            if (bShowAdd && pSetting.m_nAddID != 0)
            {
                pNode.PushSprite(y, pSetting.m_nAddID);
            }
            else if (bShowSub && pSetting.m_nSubID != 0)
            {
                pNode.PushSprite(y, pSetting.m_nSubID);
            }
            m_tempNumb.Clear();
            int nI = 0;
            do
            {
                nI = nNumber % 10;
                nNumber /= 10;
                m_tempNumb.Add(nI);
            } while (nNumber > 0);
            // 反转数组
            m_tempNumb.Reverse();
            for (int i = 0, nSize = m_tempNumb.size; i < nSize; ++i)
            {
                nI = m_tempNumb[i];
                pNode.PushSprite(y, pSetting.m_NumberID[nI]);
            }
        }
        // 居中处理吧
        switch(attrib.AlignType)
        {
            case HUDAlignType.align_right:
                pNode.MakeRight();
                break;
            case HUDAlignType.align_center:
                pNode.MakeCenter();
                break;
            default:
                pNode.MakeLeft();
                break;
        }
        
        // 申请纹理
        OnPush(pNode);

        if(!m_bCaleScreenScale)
        {
            CaleScreenScale();
        }
        PlayAnimation(pNode, true);
    }
}
