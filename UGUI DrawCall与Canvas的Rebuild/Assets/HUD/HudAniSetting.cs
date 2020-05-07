using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HUDAlignType
{
    align_left,   // 左对齐
    align_center, // 右对齐
    align_right,  // 居中
};

public enum HUDBloodType
{
    Blood_None,
    Blood_Red,
    Blood_Green,
    Blood_Blue,//队友血条颜色
}

[System.Serializable]
public struct HudAnimAttibute
{
    public AnimationCurve AlphaCurve;
    public AnimationCurve ScaleCurve;
    public AnimationCurve MoveCurve;
    public float OffsetX;
    public float OffsetY;
    public float GapTime;
    public int SpriteGap; // 图片间隔
    public HUDAlignType AlignType;
    public bool ScreenAlign; // 是不是按屏幕对齐
    public HUDAlignType ScreenAlignType; // 屏幕对齐类型
}

[System.Serializable]
public struct HudTitleAttribute
{
    public enum Effect
    {
        None,
        Shadow,
        Outline,
    }
    public Effect Style;
    public Color32 clrShadow;
    public int OffsetX;    // X偏移
    public int OffsetY;    // Y偏移

    public Color clrLeftUp;
    public Color clrLeftDown;
    public Color clrRightUp;
    public Color clrRightDown;
    public int CharGap;
    public int LineGap;
    public int Height;

    public HUDAlignType AlignType; // 对齐类型
    public int LockMaxHeight; // 锁定最大高度
    public int SpriteReduceHeight; // 图片缩减的高度
    public int SpriteOffsetY; // 图片上下移动的距离
    public int FontOffsetY; // 文本的上下移动的距力
}

public class HudTitleLabelSet
{
    public HudTitleAttribute[] m_pData;
    public HudTitleLabelSet(HudTitleAttribute p)
    {
        m_pData = new HudTitleAttribute[1];
        m_pData[0] = p;
    }
    public HudTitleLabelSet(HudTitleAttribute []pArray)
    {
        m_pData = pArray;
    }
    public HudTitleAttribute GetTitle(int nIndex)
    {
        if (nIndex < 0)
            nIndex = 0;
        else if (nIndex >= m_pData.Length)
            nIndex = m_pData.Length - 1;
        return m_pData[nIndex];
    }
};


class HudSetting
{
    static HudSetting s_pHudSetting = null;
    public static HudSetting Instance
    {
        get
        {
            if(s_pHudSetting == null)
            {
                s_pHudSetting = new HudSetting();

                s_pHudSetting.Init();
            }
            return s_pHudSetting;
        }
    }
    public static void ApplySetting(HudAniSetting hudSetting)
    {
        if(s_pHudSetting != null)
        {
            s_pHudSetting.InitSetting(hudSetting);
        }
    }

    public float m_fDurationTime = 2.0f;

    public float m_fCalbackTime = 1.0f;
    public bool m_bKeep1280x720 = true;
    public float m_fTitleScaleMin = 0.1f;
    public float m_fTitleScaleMax = 0.8f;
    public float m_fNumberScaleMin = 0.8f;
    public float m_fNumberScaleMax = 0.8f;
    public float CameraNearDist = 6.5f;
    public float CameraFarDist = 40.0f;
    public float m_fTitleOffsetY = 0.5f;
    public int m_nBloodBk;
    public int m_nBloodRed;
    public int m_nBloodGreen;
    public int m_nBloodBlue;
    public int m_nBloodBkWidth;
    public int m_nBloodBkHeight;
    public int m_nBloodWidth;
    public int m_nBloodHeight;
    public float m_fTestBloodPos = 1.0f;
    public int m_nTeamFlagPic;
    public int m_nPKFlagPic;
    public int m_nNpcMissionPic;
    public int[] MeridianPic; // 头顶充穴动画数字

    public HudAnimAttibute []NumberAttibute; // HUD_SHOW_NUMBER
    public HudTitleLabelSet[]TitleSets; // Tilte_Number    

    public int m_nTalkBk;
    public int m_nTalkWidth = 300;
    public int TalkBorderWidth = 15;
    public int TalkBorderHeight = 20;
    public int m_nTalkBkOffsetY = -10;
    public float m_fTalkShowTime = 5.0f; // 显示时间
    public float m_fTalkOffsetY = 0.2f;
    public Vector2 m_vTalkOffset;
    public HudTitleAttribute[]TalkTitle; // 气泡属性

    public bool HideAllTitle = false;

    private void Init()
    {
        NumberAttibute = new HudAnimAttibute[(int)HUDNumberRenderType.HUD_SHOW_NUMBER];
        TitleSets = new HudTitleLabelSet[(int)HUDTilteType.Tilte_Number];

        GameObject obj = UIPrefabLoader.Load("HUDSetting") as GameObject;
        if (obj != null)
        {
            HudAniSetting hudSetting = obj.transform.GetComponent<HudAniSetting>();
            if (hudSetting != null)
            {
                InitSetting(hudSetting);
            }
        }
    }
    public void InitSetting(HudAniSetting hudSetting)
    {
        NumberAttibute[(int)HUDNumberRenderType.HUD_SHOW_EXP_ADD] = hudSetting.ExpAnimAttibute;
        NumberAttibute[(int)HUDNumberRenderType.HUD_SHOW_LIFE_EXP] = hudSetting.LifeExpAnimAttibute;
        NumberAttibute[(int)HUDNumberRenderType.HUD_SHOW_MONEY_ADD] = hudSetting.MoneyAnimAttibute;
        NumberAttibute[(int)HUDNumberRenderType.HUD_SHOW_XINFA] = hudSetting.XinfaAnimAttibute;
        NumberAttibute[(int)HUDNumberRenderType.HUD_SHOW_HP_HURT] = hudSetting.HurtAnimAttibute;
        NumberAttibute[(int)HUDNumberRenderType.HUD_SHOW_COMMON_ATTACK] = hudSetting.CommonAnimAttibute;
        NumberAttibute[(int)HUDNumberRenderType.HUD_SHOW_CT_ATTACKED] = hudSetting.CtedAnimAttibute;
        NumberAttibute[(int)HUDNumberRenderType.HUD_SHOW_CT_ATTACK] = hudSetting.CtAnimAttibute;
        NumberAttibute[(int)HUDNumberRenderType.HUD_SHOW_ABSORB] = hudSetting.AbsorbAnimAttibute;
        NumberAttibute[(int)HUDNumberRenderType.HUB_SHOW_DODGE] = hudSetting.DodgeAnimAttibute;
        NumberAttibute[(int)HUDNumberRenderType.HUD_SHOW_RECOVER_HP] = hudSetting.RecoverAnimAttibute;
        NumberAttibute[(int)HUDNumberRenderType.HUD_SHOW_PET_ATTACK] = hudSetting.PetDemAnimAttibute;
        

        m_fDurationTime = hudSetting.m_fDurationTime;
        m_bKeep1280x720 = hudSetting.m_bKeep1280x720;
        m_fTitleScaleMin = hudSetting.m_fTitleScaleMin;
        m_fTitleScaleMax = hudSetting.m_fTitleScaleMax;
        m_fNumberScaleMin = hudSetting.m_fNumberScaleMin;
        m_fNumberScaleMax = hudSetting.m_fNumberScaleMax;
        CameraNearDist = hudSetting.CameraNearDist;
        CameraFarDist = hudSetting.CameraFarDist;

        TitleSets[(int)HUDTilteType.PlayerName] = new HudTitleLabelSet(hudSetting.PlayerTitle);
        TitleSets[(int)HUDTilteType.PlayerPrestige] = new HudTitleLabelSet(hudSetting.PrestigeTitle);
        TitleSets[(int)HUDTilteType.PlayerCorp] = new HudTitleLabelSet(hudSetting.PlayerCorp);
        TitleSets[(int)HUDTilteType.PlayerDesignation] = new HudTitleLabelSet(hudSetting.DesignationTitle);
        TitleSets[(int)HUDTilteType.MonsterName] = new HudTitleLabelSet(hudSetting.MonsterTitle);
        TitleSets[(int)HUDTilteType.ItemName] = new HudTitleLabelSet(hudSetting.ItemName);
        TitleSets[(int)HUDTilteType.PetName] = new HudTitleLabelSet(hudSetting.PetName);
        TitleSets[(int)HUDTilteType.Blood] = new HudTitleLabelSet(hudSetting.Blood);
        TitleSets[(int)HUDTilteType.PKFlag] = new HudTitleLabelSet(hudSetting.PKFlag);
        TitleSets[(int)HUDTilteType.HeadIcon] = new HudTitleLabelSet(hudSetting.HeadIcon);

        m_fTitleOffsetY = hudSetting.m_fTitleOffsetY;
        m_nBloodBk = CAtlasMng.instance.SpriteNameToID(hudSetting.m_szBloodBk);
        m_nBloodRed = CAtlasMng.instance.SpriteNameToID(hudSetting.m_szBloodRed);
        m_nBloodGreen = CAtlasMng.instance.SpriteNameToID(hudSetting.m_szBloodGreen);
        m_nBloodBlue = CAtlasMng.instance.SpriteNameToID(hudSetting.m_szBloodBlue);
        m_nBloodBkWidth = hudSetting.m_nBloodBkWidth;
        m_nBloodBkHeight = hudSetting.m_nBloodBkHeight;
        m_nBloodWidth = hudSetting.m_nBloodWidth;
        m_nBloodHeight = hudSetting.m_nBloodHeight;
        m_fTestBloodPos = hudSetting.m_fTestBloodPos;
        m_nTeamFlagPic = CAtlasMng.instance.SpriteNameToID(hudSetting.TeamFlagSprite);
        m_nPKFlagPic = CAtlasMng.instance.SpriteNameToID(hudSetting.PKFlagSprite);
        m_nNpcMissionPic = CAtlasMng.instance.SpriteNameToID(hudSetting.NpcMessionSprite);

        // 头顶充脉数字
        MeridianPic = new int[10];
        for(int i = 0; i<10; ++i)
        {
            MeridianPic[i] = CAtlasMng.instance.SpriteNameToID(hudSetting.MeridianNumbHeader + i.ToString());
        }

        m_nTalkBk = CAtlasMng.instance.SpriteNameToID(hudSetting.m_szTalkBk);
        m_nTalkWidth = hudSetting.m_nTalkWidth;
        TalkBorderWidth = hudSetting.TalkBorderWidth;
        TalkBorderHeight = hudSetting.TalkBorderHeight;
        m_nTalkBkOffsetY = hudSetting.m_nTalkBkOffsetY;
        m_fTalkShowTime = hudSetting.m_fTalkShowTime;
        m_fTalkOffsetY = hudSetting.m_fTalkOffsetY;
        m_vTalkOffset = hudSetting.m_vTalkOffset;
        TalkTitle = hudSetting.TalkTitle;

        HideAllTitle = hudSetting.HideAllTitle;
    }
}

class HudAniSetting : MonoBehaviour
{
    /// <summary>
    /// 动画播放持续时间;
    /// </summary>
    public float m_fDurationTime = 2.0f;

    public float m_fCalbackTime = 1.0f;
    public bool  m_bKeep1280x720 = true;
        
    public HudAnimAttibute HurtAnimAttibute;
    public HudAnimAttibute CommonAnimAttibute;
    public HudAnimAttibute CtAnimAttibute;
    public HudAnimAttibute RecoverAnimAttibute;
    public HudAnimAttibute ExpAnimAttibute;
    public HudAnimAttibute LifeExpAnimAttibute;
    public HudAnimAttibute AbsorbAnimAttibute;
    public HudAnimAttibute DodgeAnimAttibute;
    public HudAnimAttibute MoneyAnimAttibute;
    public HudAnimAttibute XinfaAnimAttibute;
    public HudAnimAttibute CtedAnimAttibute;
    public HudAnimAttibute PetDemAnimAttibute;

    public float m_fTitleScaleMin = 0.1f;
    public float m_fTitleScaleMax = 0.8f;
    public float m_fTitleOffsetY = 0.5f;
    public float m_fNumberScaleMin = 0.8f;
    public float m_fNumberScaleMax = 0.8f;
    public float CameraNearDist = 6.5f;
    public float CameraFarDist = 40.0f;
    public string m_szBloodBk;
    public string m_szBloodRed;
    public string m_szBloodGreen;
    public string m_szBloodBlue;
    public int m_nBloodBkWidth;
    public int m_nBloodBkHeight;
    public int m_nBloodWidth;
    public int m_nBloodHeight;
    public float m_fTestBloodPos = 1.0f;
    public string TeamFlagSprite;
    public string PKFlagSprite;
    public string NpcMessionSprite;
    public string MeridianNumbHeader;

    public HudTitleAttribute PlayerCorp;    
    public HudTitleAttribute ItemName;
    public HudTitleAttribute PetName; // 宠物元神的名字
    public HudTitleAttribute Blood; // 血条
    public HudTitleAttribute PKFlag; // PK标识
    public HudTitleAttribute HeadIcon; // NPC头顶标记或队长图标

    public HudTitleAttribute[] PlayerTitle;
    public HudTitleAttribute[] PrestigeTitle;
    public HudTitleAttribute[] DesignationTitle;
    public HudTitleAttribute[] MonsterTitle;

    public string m_szTalkBk;
    public int m_nTalkWidth = 300;
    public int TalkBorderWidth = 15;
    public int TalkBorderHeight = 20;
    public int m_nTalkBkOffsetY = -10;
    public float m_fTalkShowTime = 5.0f; // 显示时间
    public float m_fTalkOffsetY = 0.2f;
    public Vector2 m_vTalkOffset;
    public HudTitleAttribute []TalkTitle; // 气泡属性

    public bool HideAllTitle = false;
    public bool CopyFirst = false;
    public bool bRefresh = false;
    //public bool bCopyOld = false;

#if UNITY_EDITOR
    AnimationCurve CopyAnimationCurve(AnimationCurve right)
    {
        AnimationCurve left = new AnimationCurve(right.keys);
        left.postWrapMode = right.postWrapMode;
        left.preWrapMode = right.preWrapMode;
        return left;
    }

    void CopyFirstSetting(HudTitleAttribute[] titleAttribs)
    {
        if (titleAttribs == null)
            return;
        for(int i = 1; i< titleAttribs.Length; ++i)
        {
            titleAttribs[i].Style = titleAttribs[0].Style;
            titleAttribs[i].OffsetX = titleAttribs[0].OffsetX;
            titleAttribs[i].OffsetY = titleAttribs[0].OffsetY;
            titleAttribs[i].CharGap = titleAttribs[0].CharGap;
            titleAttribs[i].LineGap = titleAttribs[0].LineGap;
            titleAttribs[i].Height = titleAttribs[0].Height;
            titleAttribs[i].AlignType = titleAttribs[0].AlignType;
            titleAttribs[i].LockMaxHeight = titleAttribs[0].LockMaxHeight;
            titleAttribs[i].SpriteReduceHeight = titleAttribs[0].SpriteReduceHeight;
            titleAttribs[i].SpriteOffsetY = titleAttribs[0].SpriteOffsetY;
            titleAttribs[i].FontOffsetY = titleAttribs[0].FontOffsetY;
        }
    }

    void Update()
    {
        if (CopyFirst)
        {
            CopyFirst = false;
            CopyFirstSetting(PlayerTitle);
            CopyFirstSetting(PrestigeTitle);
            CopyFirstSetting(DesignationTitle);
            CopyFirstSetting(MonsterTitle);
        }

        if(bRefresh)
        {
            bRefresh = false;
            HudSetting.ApplySetting(this);
            HUDNumberRender.ApplySetting(this);
            HUDTitleInfo.HUDTitleRender.Instance.ApplySetting(this);
        }
    }
#endif
}
