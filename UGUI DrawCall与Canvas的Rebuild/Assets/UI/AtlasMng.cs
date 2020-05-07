using UnityEngine;
using System.Collections;
using System.Collections.Generic;

///////////////////////////////////////////////////////////
//
//  Written by              ：laishikai
//  Copyright(C)            ：成都博瑞梦工厂
//  ------------------------------------------------------
//  功能描述                ：通用的图集管理器
//
///////////////////////////////////////////////////////////

// 单个纹理材质
public class UITexAtlas
{
    public enum Coordinates
    {
        Pixels,
        TexCoords,
    }

    public string m_szAtlasName = "Input name"; // 材质名字
    public string m_szTexName = "";   // 纹理名字
    public string m_szShaderName = ""; // shader名字
    public Material m_material;    // 材质
    public Texture m_MainAlpha;    // 主贴图的通道图
    public int m_nAtlasID = 0;        // 材质ID

    Coordinates m_Coordinates = Coordinates.Pixels;

    // Size in pixels for the sake of MakePixelPerfect functions.
    int m_PixelSize = 1;

    // Whether the atlas is using a pre-multiplied alpha material. -1 = not checked. 0 = no. 1 = yes.
    int m_PMA = -1;

    int m_nTexWidth = 1;   // 纹理的宽度
    int m_nTexHeight = 1;  // 纹理的高度

    bool m_bCanLOD = false; // 是不是可以LOD缩放
    
    // -----------------------------------------------------------
    // 以下时临时变量
    public int m_nRef;  // 引用计数
    public int m_nSpriteNumb;  // 精寻对象数量
    public int m_nVersion;     // 当前修改的版本号(有修改就改变)
    public bool m_bDirty;      // 脏了，需要修改
    public float m_fReleaseTime; // 释放时间
    public bool m_bLoading = false;      // 是不是正在加载中
    public bool m_bAddAssetBundleRef = false; // 是不是添加了AssetBundle的引用计数
    public string m_szMainResName;
    public string m_szAlphaResName;
    public float m_fLoadingTime = 0.0f; // 上一次加载的时间
    public AssetBundle m_mainBundle;
    public AssetBundle m_mainAlphaBundle;

    public delegate void OnLoadAtlas();
    public OnLoadAtlas m_lpOnLoadAtlas; // 加载纹理成功后的事件，因为是异步的操作
    // -----------------------------------------------------------

    public void CopyFromSetting(UITexAtlas from)
    {
        m_Coordinates = from.m_Coordinates;
        m_PixelSize   = from.m_PixelSize;
        m_PMA         = from.m_PMA;
    }

    public Texture MainAlphaTexture
    {
        get { return m_MainAlpha != null ? m_MainAlpha : mainTexture; }
    }

    public Texture mainTexture
    {
        get { if (m_material != null) return m_material.mainTexture; else return null; }
    }
    public void SetTextureSizeByMaterial(Material mat)
    {
        Texture  tex = mat != null ? mat.mainTexture : null;
        SetTextureSizeByTexture(tex);
        if (mat != null && mat.shader != null)
            m_szShaderName = mat.shader.name;
    }
    public void SetTextureSizeByTexture(Texture tex)
    {
        if (tex != null)
        {
            m_nTexWidth = tex.width;
            m_nTexHeight = tex.height;
        }
        else
        {
            m_nTexWidth = m_nTexHeight = 1;
        }
    }
    public int texWidth
    {
        get{ return m_nTexWidth; }
    }
    public int texHeight
    {
        get { return m_nTexHeight; }
    }
    
    public Coordinates coordinates
    {
        get
        {
            return m_Coordinates;
        }
        set
        {
            m_Coordinates = value;
        }
    }
    public int pixelSize
    {
        get
        {
            return m_PixelSize;
        }
        set 
        {
            m_PixelSize = value; 
        }
    }

    public bool premultipliedAlpha
    {
        get
        {
            if (m_PMA == -1)
            {
                Material mat = m_material;
                m_PMA = (mat != null && mat.shader != null && mat.shader.name.Contains("Premultiplied")) ? 1 : 0;
            }
            return (m_PMA == 1);
        }
    }
    public bool  IsCanLOD()
    {
        return m_bCanLOD;
    }
    public void  SetLODFlag(bool bCanLOD)
    {
        m_bCanLOD = bCanLOD;
    }

    public void AdjustAtlas(UITexAtlas other)
    {
        m_szAtlasName = other.m_szAtlasName;
        m_szTexName = other.m_szTexName;
        m_nAtlasID = other.m_nAtlasID;

        m_szShaderName = other.m_szShaderName;
        m_PixelSize = other.m_PixelSize;
        m_Coordinates = other.m_Coordinates;
        m_nTexWidth = other.m_nTexWidth;
        m_nTexHeight = other.m_nTexHeight;
        m_bCanLOD    = other.m_bCanLOD;
    }

    public void Serailize(ref CSerialize ar)
    {
        int nCoordinatesType = (int)m_Coordinates;
        ar.ReadWriteValue(ref m_szAtlasName);
        ar.ReadWriteValue(ref m_szTexName);
        ar.ReadWriteValue(ref nCoordinatesType);
        ar.ReadWriteValue(ref m_PixelSize);
        m_Coordinates = nCoordinatesType == (int)Coordinates.Pixels ? Coordinates.Pixels : Coordinates.TexCoords;
        ar.ReadWriteValue(ref m_nTexWidth);
        ar.ReadWriteValue(ref m_nTexHeight);
        if (ar.GetVersion() >= 1)
        {
            ar.ReadWriteValue(ref m_nAtlasID);
        }
        if (ar.GetVersion() >= 2)
        {
            ar.ReadWriteValue(ref m_szShaderName);
        }
        if(ar.GetVersion() >= 4)
        {
            ar.ReadWriteValue(ref m_bCanLOD);
        }
    }
    public void SerializeToTxt(ref SerializeText ar)
    {
        int nCoordinatesType = (int)m_Coordinates;
        ar.ReadWriteValue("AtlasName", ref m_szAtlasName);
        ar.ReadWriteValue("TexName", ref m_szTexName);
        ar.ReadWriteValue("Coordinates", ref nCoordinatesType);
        ar.ReadWriteValue("PixelSize", ref m_PixelSize);
        m_Coordinates = nCoordinatesType == (int)Coordinates.Pixels ? Coordinates.Pixels : Coordinates.TexCoords;
        ar.ReadWriteValue("texWidth", ref m_nTexWidth);
        ar.ReadWriteValue("texHeight", ref m_nTexHeight);
        if (ar.GetVersion() >= 1)
        {
            ar.ReadWriteValue("AtlasID", ref m_nAtlasID);
        }
        if (ar.GetVersion() >= 2)
        {
            ar.ReadWriteValue("ShaderName", ref m_szShaderName);
        }
        if(ar.GetVersion() >= 4)
        {
            ar.ReadWriteValue("CanScale", ref m_bCanLOD);
        }
    }
};

public class UISpriteInfo  // 兼容NGUI的Sprite对象，将Sprite成员放到这里来
{
    public string name = "Unity Bug";   // 对象的名字
    public Rect outer = new Rect(0f, 0f, 1f, 1f);     // 外框，精灵的实际大小（在纹理的像素坐标)
	public Rect inner = new Rect(0f, 0f, 1f, 1f);     // 内框，用来做填充模式时的像素坐标，这个必须是在外框之内的
    public bool rotated = false;

    // Padding is needed for trimmed sprites and is relative to sprite width and height
    public float paddingLeft = 0f;   // 用来做精灵图层选择时扩展选择框范围的东东，没有实际意义
    public float paddingRight = 0f;
    public float paddingTop = 0f;
    public float paddingBottom = 0f;

    // 下面是扩展属性
    public int m_nNameID;   // 精灵ID
    public int m_nAtlasID;  // 材质ID
    public string m_szAtlasName;  // 对应的材质名字

    public bool hasPadding { get { return paddingLeft != 0f || paddingRight != 0f || paddingTop != 0f || paddingBottom != 0f; } }

    public UISpriteInfo Clone()
    {
        UISpriteInfo p = new UISpriteInfo();
        p.Copy(this);
        return p;
    }

    // 功能：拷贝对象 
    public void Copy(UISpriteInfo src)
    {
        name = src.name.Clone() as string;
        outer = new Rect(src.outer.xMin, src.outer.yMin, src.outer.width, src.outer.height);
        inner = new Rect(src.inner.xMin, src.inner.yMin, src.inner.width, src.inner.height);
        rotated = src.rotated;
        paddingLeft = src.paddingLeft;
        paddingRight = src.paddingRight;
        paddingTop = src.paddingTop;
        paddingBottom = src.paddingBottom;
        m_nNameID = src.m_nNameID;
        m_nAtlasID = src.m_nAtlasID;
        m_szAtlasName = src.m_szAtlasName.Clone() as string;
    }
    public void Serailize(ref CSerialize ar)
    {
        ar.ReadWriteValue(ref name);
        ar.ReadWriteValue(ref outer);
        ar.ReadWriteValue(ref inner);
        ar.ReadWriteValue(ref rotated);
        ar.ReadWriteValue(ref paddingLeft);
        ar.ReadWriteValue(ref paddingRight);
        ar.ReadWriteValue(ref paddingTop);
        ar.ReadWriteValue(ref paddingBottom);
        ar.ReadWriteValue(ref m_szAtlasName);
        if (ar.GetVersion() >= 1)
        {
            ar.ReadWriteValue(ref m_nNameID);
            ar.ReadWriteValue(ref m_nAtlasID);
        }
    }
    public void SerializeToTxt(ref SerializeText ar)
    {
        ar.ReadWriteValue("name", ref name);
        ar.ReadWriteValue("outer", ref outer);
        ar.ReadWriteValue("inner", ref inner);
        ar.ReadWriteValue("rotated", ref rotated);
        ar.ReadWriteValue("paddingLeft", ref paddingLeft);
        ar.ReadWriteValue("paddingRight", ref paddingRight);
        ar.ReadWriteValue("paddingTop", ref paddingTop);
        ar.ReadWriteValue("paddingBottom", ref paddingBottom);
        ar.ReadWriteValue("AtlasName", ref m_szAtlasName);
        if (ar.GetVersion() >= 1)
        {
            ar.ReadWriteValue("NameID", ref m_nNameID);
            ar.ReadWriteValue("AtlasID", ref m_nAtlasID);
        }
    }
}

// 材质管理器
public class CAtlasMng
{
    public class CUITextureCache
    {
        public Texture m_tex;
        public Texture m_defTex;
        public float m_fTime = 0.0f;
        public int m_nRef = 0;
        public bool m_bLoading = false;
        public bool m_bValidTexture = false;
        public bool m_bFromAssets = false;
        public bool m_bQueryAssetBundleRef = false;
        public string m_szResName = string.Empty;
        public AssetBundle m_mainBundle;
        public OnLoadNetTexture m_pOnLoadFunc = null;
    };

    protected Dictionary<string, UISpriteInfo> m_AllSprite = new Dictionary<string, UISpriteInfo>();   // 所有的精灵对象
    protected Dictionary<string, UITexAtlas> m_TexAtlas = new Dictionary<string, UITexAtlas>();    // 材质对象
    protected Dictionary<int, UITexAtlas> m_QueryAtlas = new Dictionary<int, UITexAtlas>();
    protected CMyArray<UISpriteInfo> m_SpritePtr = new CMyArray<UISpriteInfo>();
    protected CMyArray<UITexAtlas> m_AtlasPtr = new CMyArray<UITexAtlas>();
    protected CMyArray<int> m_NeedReleaseAtlas = new CMyArray<int>();
    
    protected Dictionary<string, CUITextureCache> m_TexCache = new Dictionary<string, CUITextureCache>();    // 外部缓冲纹理对象
    protected CMyArray<string> m_tempDelete = new CMyArray<string>();
    protected string m_szLoadingUrl; // 登陆的Loading图，每次总是预存一张，永不释放，解决Loading图加载延时的问题

    protected UITexAtlas m_defTexAltas = new UITexAtlas();
    protected UISpriteInfo m_defSprite = new UISpriteInfo();
    protected bool m_bInitCfg = false;
    protected int n_nObjNumb;
    protected float m_fNextUpdateTime = 0.0f;
    protected float m_fNextCheckPanelTime = 0.0f;
    protected int m_nFileVersion = 0;  // 文件的版本号，每次保存加1 

    protected bool m_bLoadingCfg = false;
    protected int m_nLoadNumb = 0;

    protected Material m_hyperlinkMat;  // 超连接的材质
    protected int m_hyperlinkMatRef = 0;

    public delegate string GetResURL(string szFileName);
    public GetResURL m_lpGetUIResURL = null;
    public GetResURL m_lpGetUIResPath = null;

    public delegate IEnumerator LPFuncDownUITexture(string szResName1, string szResName2);
    public delegate Texture LPFuncGetLoadTexture(string szResName);
    public delegate void LPFuncAddTextureRef(string szResName, int nAddRef, bool bImmRelease);

    public LPFuncDownUITexture m_lpFuncDownUITexture = null;   // 下载UI的事件, 这里使用委托，并不直接调用AssetManager
    public LPFuncGetLoadTexture m_lpFuncGetLoadTexture = null;  // 加载纹理的事件
    public LPFuncAddTextureRef m_lpFuncAddTextureRef = null;    // 增加纹理资源引用计数

    public delegate bool IsHaveAssetFunc(string szFileName);
    public IsHaveAssetFunc m_pHaveAssetFunc = null;

    static CAtlasMng s_pAltasMng;
    static int s_nLowQuality = 0; // 是不是使用低画质

    // 功能：是不是低画质
    static public bool IsLowQuality()
    {
        if (0 == s_nLowQuality)
            s_nLowQuality = SystemInfo.systemMemorySize <= 1024 ? 1 : 2; // 如果内存小于1G，就认定是低画质
        return s_nLowQuality == 1;
    }
    static public void SetQuality(int nQuality)
    {
        s_nLowQuality = nQuality + 1;
    }

    public class CAtlasLoader : MonoBehaviour
    {
        static CAtlasLoader s_pIns;
        float m_fStart;
        float m_fLastTime;
        int m_nTaskNumb = 0;

        public static CAtlasLoader instance
        {
            get
            {
                if (s_pIns == null)
                {
                    GameObject go = GameObject.Find("_AltasMng_Loader");
                    if (go != null)
                    {
                        GameObject.DestroyImmediate(go);
                        go = null;
                    }
                    if (go == null)
                    {
                        go = new GameObject("_AltasMng_Loader");
                        if (Application.isPlaying)
                            DontDestroyOnLoad(go);    // 通知不释放
                    }
                    go.hideFlags = HideFlags.HideAndDontSave;
                    s_pIns = go.GetComponent<CAtlasLoader>();
                    if (s_pIns == null)
                        s_pIns = go.AddComponent<CAtlasLoader>();
                    go.SetActive(true);

                    s_nLowQuality = SystemInfo.systemMemorySize <= 1024 ? 1 : 2; // 如果内存小于1G，就认定是低画质
                }
                return s_pIns;
            }
        }
        void OnApplicationQuit()
        {
            if (s_pIns != null)
            {
                GameObject.DestroyImmediate(s_pIns.gameObject);
                s_pIns = null;
            }
        }
        public void StartInit(CAtlasMng pMng, bool bReload)
        {
            m_fStart = Time.time;
            ++m_nTaskNumb;
            StartCoroutine(LoadAtlasCfgData(pMng, bReload));
            ++m_nTaskNumb;
        }
        void ReleaseObj()
        {
            s_pIns = null;
            if (gameObject != null)
                Object.DestroyImmediate(gameObject);
            else
                Object.DestroyImmediate(this);
        }
        void Update()
        {
        }
        void FixedUpdate()
        {
            if (m_nTaskNumb == 0)
            {
                if (m_fLastTime + 1.0f < Time.time)
                {
                    ReleaseObj();
                }
            }
        }
        bool TryLoadUICfgData(WWW www, CAtlasMng pMng, string szAssetsName, bool bReload)
        {
            string url = pMng.GetUIResURL(szAssetsName);
            bool bSucInit = false;
            if (www.error != null)
            {
                Debug.LogError(url + " " + www.error);
            }
            else
            {
                AssetBundle bundle = www.assetBundle;
                // 这里按文本方式加载
                if (bundle != null)
                {
                    byte[] fileData = null;
                    bSucInit = AssetUtility.LoadBinText(ref fileData, bundle);
                    if (bSucInit)
                    {
                        if (bReload)
                            pMng.ReloadCfg(fileData);
                        else
                            pMng.InitCfg(fileData);
                    }
                }
                www.Dispose();
            }
            www = null;
            return bSucInit;
        }
        IEnumerator LoadAtlasCfgData(CAtlasMng pMng, bool bReLoad)
        {
            string szAssetsName = "bytes_atlas_assets_all.unity3d";
            string url = pMng.GetUIResURL(szAssetsName);

            WWW www = new WWW(url);
            yield return www;
            bool bSucInit = true;
            if (!TryLoadUICfgData(www, pMng, szAssetsName, bReLoad))
            {
                Debug.LogError("UI cfg load failed, url = " + url);
            }
            if (!bSucInit)
            {
                pMng.InitCfg(null);
            }
            --m_nTaskNumb;
            m_fLastTime = Time.time;
        }
        public void StartLoatAtlas(CAtlasMng pMng, int nAtlasID, bool bReload)
        {
            m_fStart = Time.time;
            ++m_nTaskNumb;
            if(pMng.IsInitAssetManager())
                StartCoroutine(LoadAtlasMaterialByBundle(pMng, nAtlasID, bReload));
            else
                StartCoroutine(LoadAtlasMaterial(pMng, nAtlasID, bReload));
        }
        IEnumerator LoadAtlasMaterialByBundle(CAtlasMng pMng, int nAtlasID, bool bReload)
        {
            UITexAtlas atlas = pMng.GetAtlasByID(nAtlasID);
            
            bool bHaveMainAlpha = atlas.m_szShaderName == "Unlit/Transparent Colored MainAlpha";
            // PC平台强制使用一张纹图
            if (pMng.IsForceOneTexture())
                bHaveMainAlpha = false;
            string szResName = "res_texture_" + atlas.m_szAtlasName;
            bool bIsLowQuality = IsLowQuality() && atlas.IsCanLOD();
            if (atlas.texWidth <= 256 && atlas.texHeight <= 256)
                bIsLowQuality = false;

            szResName = szResName.ToLower();
            string szResNameAlpha = string.Empty;
            if (bHaveMainAlpha)
            {
                if (bIsLowQuality)
                    szResNameAlpha = szResName + "_alpha_l";
                else
                    szResNameAlpha = szResName + "_alpha";
                szResNameAlpha = szResNameAlpha.ToLower();
            }
            pMng.RealReleaseMaterial(atlas); // 加载前必须先释放旧的AssetBundle，不然加载会失败
            yield return StartCoroutine(pMng.m_lpFuncDownUITexture(szResName, szResNameAlpha));

            if(!atlas.m_bAddAssetBundleRef)
            {
                atlas.m_bAddAssetBundleRef = true;
                atlas.m_szMainResName = szResName;
                atlas.m_szAlphaResName = szResNameAlpha;
                pMng.m_lpFuncAddTextureRef(szResName, 1, true);
                pMng.m_lpFuncAddTextureRef(szResNameAlpha, 1, true);
            }

            Texture texMain = pMng.m_lpFuncGetLoadTexture(szResName);
            Texture texAlpha = pMng.m_lpFuncGetLoadTexture(szResNameAlpha);
            pMng.OnQueryAtlasTexByAssetBundle(texMain, texAlpha, atlas, bReload);

            --m_nTaskNumb;
            m_fLastTime = Time.time;
        }

        IEnumerator LoadAtlasMaterial(CAtlasMng pMng, int nAtlasID, bool bReload)
        {
            UITexAtlas atlas = pMng.GetAtlasByID(nAtlasID);
            bool bHaveMainAlpha = atlas.m_szShaderName == "Unlit/Transparent Colored MainAlpha";
            // PC平台强制使用一张纹图
            if (pMng.IsForceOneTexture())
                bHaveMainAlpha = false;
            string szResName = "res_texture_" + atlas.m_szAtlasName;
            bool bIsLowQuality = IsLowQuality() && atlas.IsCanLOD();
            if (atlas.texWidth <= 256 && atlas.texHeight <= 256)
                bIsLowQuality = false;

            string url = bIsLowQuality ? pMng.GetUIResURL(szResName + "_l.unity3d") : pMng.GetUIResURL(szResName + ".unity3d");

            WWW www = new WWW(url);
            WWW alpha_www = null;
            if (bHaveMainAlpha)
            {
                string alpha_url = bIsLowQuality ? pMng.GetUIResURL(szResName + "_alpha_l.unity3d") : pMng.GetUIResURL(szResName + "_alpha.unity3d");
                alpha_www = new WWW(alpha_url);
            }
            yield return www;

            if (bHaveMainAlpha)
                yield return alpha_www;

            AssetBundle alpha_bundle = null;
            if (bHaveMainAlpha && alpha_www.error == null)
                alpha_bundle = alpha_www.assetBundle;

            if (www.error != null)
            {
                Debug.LogError(www.error);
                pMng.OnQueryAtlasTex(null, null, atlas, bReload);
                if (alpha_bundle != null)
                    alpha_bundle.Unload(true);
            }
            else
            {
                AssetBundle bundle = www.assetBundle;
                pMng.OnQueryAtlasTex(bundle, alpha_bundle, atlas, bReload);
            }
            www.Dispose();
            www = null;
            if (alpha_www != null)
            {
                alpha_www.Dispose();
                alpha_www = null;
            }
            --m_nTaskNumb;
            m_fLastTime = Time.time;
        }
        public void StartLoadOtherTexture(CAtlasMng pMng, string url)
        {
            m_fStart = Time.time;
            ++m_nTaskNumb;
            if(url.IndexOf("http://") != -1 || !pMng.IsInitAssetManager())
                StartCoroutine(LoadOtherTexture(pMng, url));
            else
                StartCoroutine(LoadOtherTextureByAssetBundle(pMng, url));
        }
        IEnumerator LoadOtherTextureByAssetBundle(CAtlasMng pMng, string url)
        {
            string szPathName = url;
            if (url.IndexOf("file:///") == 0)
            {
                szPathName = url.Substring(8);
            }
            else if (url.IndexOf("file://") == 0)
            {
                szPathName = url.Substring(7);
            }
            string szResName = System.IO.Path.GetFileName(szPathName);
            int nPos = szResName.IndexOf('.');
            if (nPos != -1)
                szResName = szResName.Substring(0, nPos);
            szResName = szResName.ToLower();

            yield return StartCoroutine(pMng.m_lpFuncDownUITexture(szResName, string.Empty));
            Texture tex = pMng.m_lpFuncGetLoadTexture(szResName);
            pMng.OnLoadOtherTextureByAssetBundle(url, szResName, tex);
            --m_nTaskNumb;
            m_fLastTime = Time.time;
        }

        IEnumerator LoadOtherTexture(CAtlasMng pMng, string url)
        {
            // 取文件名
            string szRealUrl = url;
            if (CAtlasMng.IsLowQuality())
            {
                int nLastIndex = url.LastIndexOf('/');
                if (nLastIndex != -1)
                {
                    string szFileName = url.Substring(nLastIndex + 1);
                    szFileName = szFileName.Replace(".unity3d", "");
                    szFileName = szFileName + "_l";
                    if (pMng.IsHaveAsset(szFileName))
                    {
                        szRealUrl = url.Replace(".unity3d", "_l.unity3d");
                    }
                }
            }
            WWW www = new WWW(szRealUrl);
            yield return www;
            if (www.error == null)
            {
                AssetBundle bundle = www.assetBundle;
                Texture tex = null;
                bool bFromAssets = false;
                if (bundle != null)
                {
                    bFromAssets = true;
                    tex = AssetUtility.LoadTexture(bundle);
                    if (tex != null)
                    {
                        Object.DontDestroyOnLoad(tex);
                    }
                    //bundle.Unload(false);
                }
                else
                {
                    tex = www.texture;
                    if (tex != null)
                        Object.DontDestroyOnLoad(tex);
                }
                pMng.OnLoadOtherTexture(url, bundle, tex, bFromAssets);
            }
            else
            {
                pMng.OnLoadOtherTexture(url, null, null, false);
            }
            www.Dispose();
            www = null;
            --m_nTaskNumb;
            m_fLastTime = Time.time;
        }
    };

    public class CAtlasMngStart : MonoBehaviour
    {
        CAtlasMng m_pMng;
        public CAtlasMngStart()
        {
#if  UNITY_EDITOR
            if (CAtlasMng.s_pAltasMng != null)
            {
                AtlasMng_Editor pEditor = CAtlasMng.s_pAltasMng as AtlasMng_Editor;
                if (pEditor == null)
                {
                    m_pMng = CAtlasMng.s_pAltasMng = new AtlasMng_Editor();
                }
            }
            if (CAtlasMng.s_pAltasMng == null)
            {
                m_pMng = CAtlasMng.s_pAltasMng = new AtlasMng_Editor();
            }
#else
            if (CAtlasMng.s_pAltasMng == null)
            {
                m_pMng = CAtlasMng.s_pAltasMng = new CAtlasMng();
            }
#endif
        }
        void Awake()
        {

        }
        void Start()
        {
            // 释放自己吧
            if (m_pMng == null)
            {
                if (gameObject != null)
                    Object.DestroyImmediate(gameObject);
                else
                    Object.DestroyImmediate(this);
            }
            else
            {
                m_pMng.AutoInit();
            }
        }
        void FixedUpdate()
        {
            if (m_pMng != null)
            {
                m_pMng.ReleaseLogic();
            }
        }
    };

    static public CAtlasMng instance
    {
        get
        {
            if (s_pAltasMng == null)
            {
                CAtlasMngStart pStart = Object.FindObjectOfType(typeof(CAtlasMngStart)) as CAtlasMngStart;
                if (pStart == null)
                {
                    GameObject go = GameObject.Find("_AltasMng_Start");
                    if (go != null)
                    {
                        GameObject.DestroyImmediate(go);
                        go = null;
                    }
                    if (go == null)
                    {
                        go = new GameObject("_AltasMng_Start");
                        if (Application.isPlaying)
                            MonoBehaviour.DontDestroyOnLoad(go);
                    }
                    go.hideFlags = HideFlags.HideAndDontSave;
                    pStart = go.GetComponent<CAtlasMngStart>();
                    if (pStart == null)
                        pStart = go.AddComponent<CAtlasMngStart>();
                }
            }
            if (s_pAltasMng != null)
                s_pAltasMng.AutoInit();
            return s_pAltasMng;
        }
    }

    void OnApplicationQuit()
    {
        GameObject go = GameObject.Find("_AltasMng_Start");
        if (go != null)
        {
            GameObject.DestroyImmediate(go);
        }
        s_pAltasMng = null;
    }
    static public CAtlasMng get_instance_ptr()
    {
        return s_pAltasMng;
    }

    // 功能：设置获取URL地址的接口
    public void SetUIResURLFunc(GetResURL lpGetUIResURL)
    {
        m_lpGetUIResURL = lpGetUIResURL;
    }
    public void SetUIResPathFunc(GetResURL lpGetUIResPath)
    {
        m_lpGetUIResPath = lpGetUIResPath;
    }
    public void SetDownUITextureFunc(LPFuncDownUITexture pDownFunc)
    {
        m_lpFuncDownUITexture = pDownFunc;
    }
    public void SetGetLoadTextureFunc(LPFuncGetLoadTexture pGetLoadTextureFunc)
    {
        m_lpFuncGetLoadTexture = pGetLoadTextureFunc;
    }
    public void SetFuncAddTextureRef(LPFuncAddTextureRef pAddTextureRefFunc)
    {
        m_lpFuncAddTextureRef = pAddTextureRefFunc;
    }

    public bool IsInitAssetManager()
    {
        return false; // 总是使用旧的方式加载吧 WWW
        //return m_lpGetUIResPath != null;
    }
    public string GetUIResURL(string szAssetsName)
    {
        // AssetManager.Instance 第一次加载，只加载安装包内的，第二次重载才加载动态下载的
        if (m_lpGetUIResURL != null && m_nLoadNumb > 0)
            return m_lpGetUIResURL(szAssetsName);
        szAssetsName = szAssetsName.ToLower();  // 新的打包后的文件一律小写
        return GetStreamAssetsURL(szAssetsName);
    }
    static public string GetTargetPlatName()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.IPhonePlayer:
                return "Ios";
            case RuntimePlatform.Android:
                return "Android";
            default:
                break;
        }
        return "Windows";
    }
    public static string GetStreamAssetsURL(string szFileName)
    {
#if   UNITY_STANDALONE || UNITY_EDITOR
        string url = "file:///" + Application.streamingAssetsPath + "/" + GetTargetPlatName() + '/' + szFileName;
        return url;
#elif   UNITY_ANDROID
        string url = Application.streamingAssetsPath + "/" + GetTargetPlatName() + '/' + szFileName;
        return url;
#elif   UNITY_IPHONE
        string url = "file://" + Application.streamingAssetsPath + "/" + GetTargetPlatName() + '/' + szFileName;
        return url;
#else
        string url = "file:///" + Application.streamingAssetsPath + "/" + GetTargetPlatName() + '/' + szFileName;
        return url;
#endif
    }
    // 功能：设置查询资源是不是存在的接口
    public void  SetFindFunc(IsHaveAssetFunc pHaveAssetFunc)
    {
        m_pHaveAssetFunc = pHaveAssetFunc;
    }
    public bool  IsHaveAsset(string szResName)
    {
        if (m_pHaveAssetFunc != null)
            return m_pHaveAssetFunc(szResName);
        return false;
    }

    // 功能：下载完成后的事件
    public virtual void OnAfterDown()
    {
        CAtlasLoader.instance.StartInit(this, true);
    }

    public void FirstLod()
    {
        if (m_bLoadingCfg)
            return;
        m_bLoadingCfg = true;

		// 先尝试本地加载吧
        CAtlasLoader.instance.StartInit(this, false);
    }

    void Awake()
    {
        InitAltasCfg();
    }

    // 功能：真正释放纹理吧
    protected virtual void RealReleaseMaterial(UITexAtlas atlas)
    {
        bool bNeedReleaseTex = true;
        if(atlas.m_bAddAssetBundleRef)
        {
            bNeedReleaseTex = false;
            atlas.m_bAddAssetBundleRef = false;
            m_lpFuncAddTextureRef(atlas.m_szMainResName, -1, true);
            m_lpFuncAddTextureRef(atlas.m_szAlphaResName, -1, true);
            atlas.m_szMainResName = string.Empty;
            atlas.m_szAlphaResName = string.Empty;
        }

        if (atlas.m_material != null)
        {
            // 只释放纹理吧
            if (atlas.m_material.mainTexture != null)
            {
                if(bNeedReleaseTex)
                    DestroyTexture(atlas.m_material.mainTexture, true);
                atlas.m_material.mainTexture = null;
            }
            if(atlas.m_MainAlpha != null)
            {
                atlas.m_material.SetTexture("_MainAlpha", null);
                if(bNeedReleaseTex)
                    DestroyTexture(atlas.m_MainAlpha, true);
                atlas.m_MainAlpha = null;
            }
            if(atlas.m_mainBundle != null)
            {
                atlas.m_mainBundle.Unload(true);
                atlas.m_mainBundle = null;
            }
            if(atlas.m_mainAlphaBundle != null)
            {
                atlas.m_mainAlphaBundle.Unload(true);
                atlas.m_mainAlphaBundle = null;
            }
        }
        atlas.m_nVersion++;
    }

    public void ReleaseLogic()
    {
        float  fNow = Time.time;
        if (fNow > m_fNextUpdateTime)
        {
            m_fNextUpdateTime = fNow + 5.0f; // 每10秒走一下释放逻辑
            bool bRemove = false;
            bool bLowQuality = IsLowQuality();
            for (int i = m_NeedReleaseAtlas.size() - 1; i >= 0; --i )
            {
                UITexAtlas atlas = GetAtlasByID(m_NeedReleaseAtlas[i]);
                bRemove = false;
                if (atlas.m_nRef > 0)
                {
                    bRemove = true;
                }
                else
                {
                    if (atlas.m_nRef == 0 && atlas.m_material != null && atlas.m_material.mainTexture != null && !atlas.m_bLoading)
                    {
                        if (atlas.m_fReleaseTime + 15.0f < fNow
                            || (bLowQuality && m_NeedReleaseAtlas.size() > 5))
                        {
                            // 释放吧
                            atlas.m_fReleaseTime = fNow;
                            RealReleaseMaterial(atlas);
                            bRemove = true;
                        }
                    }
                }
                if (bRemove)
                    m_NeedReleaseAtlas.pop_back();
                else
                    break;
            }
            if (m_TexCache.Count > 0)
            {           
                ReleaseTextureCache(false);
            }
            if(bLowQuality && (m_NeedReleaseAtlas.size() + m_TexCache.Count) > 5)
            {
                m_fNextUpdateTime = fNow + 0.1f;
            }
        }
    }
    void DestroyTexture(Texture tex, bool bFromAssets)
    {
        if (tex != null)
        {
            if (bFromAssets)
                Object.DestroyImmediate(tex, true);
            else
                Object.Destroy(tex);
        }
    }
    void ReleaseTextureCache(bool bSwitchScene)
    {
        float fNow = Time.time;
        bool bLowQuality = IsLowQuality();
        Dictionary<string, CUITextureCache>.Enumerator itCache = m_TexCache.GetEnumerator();
        while (itCache.MoveNext())
        {
            CUITextureCache pTexCache = itCache.Current.Value;
            if (pTexCache.m_nRef == 0 && !pTexCache.m_bLoading)
            {
                if ( bSwitchScene 
                    || pTexCache.m_fTime + 15.0f < fNow 
                    || (bLowQuality && m_TexCache.Count > 2))
                {
                    ImmReleaseCacheTexture(pTexCache);
                    m_tempDelete.push_back(itCache.Current.Key);
                }
            }
        }
        if (m_tempDelete.size() > 0)
        {
            for (int i = 0, iCount = m_tempDelete.size(); i < iCount; ++i)
            {
                m_TexCache.Remove(m_tempDelete[i]);
            }
            m_tempDelete.Clear();
        }
    }
    void  ImmReleaseCacheTexture(CUITextureCache pTexCache)
    {
        if(pTexCache.m_bQueryAssetBundleRef)
        {
            pTexCache.m_bQueryAssetBundleRef = false;
            m_lpFuncAddTextureRef(pTexCache.m_szResName, -1, true);
            pTexCache.m_tex = null;
        }

        if (pTexCache.m_tex != pTexCache.m_defTex)
            DestroyTexture(pTexCache.m_tex, pTexCache.m_bFromAssets);
        DestroyTexture(pTexCache.m_defTex, false);
        if (pTexCache.m_mainBundle != null)
        {
            pTexCache.m_mainBundle.Unload(true);
            pTexCache.m_mainBundle = null;
        }
        pTexCache.m_defTex = null;
        pTexCache.m_tex = null;
    }

    void SerializeIterator(CSerialize ar, ref string key, ref UISpriteInfo value)
    {
        if (key == null) key = string.Empty;
        if (value == null) value = new UISpriteInfo();
        ar.ReadWriteValue(ref key);
        value.Serailize(ref ar);
    }
    void SerializeIterator(CSerialize ar, ref string key, ref UITexAtlas value)
    {
        if (key == null) key = string.Empty;
        if (value == null) value = new UITexAtlas();
        ar.ReadWriteValue(ref key);
        value.Serailize(ref ar);
    }
    void SerializeIterator(SerializeText ar, ref string key, ref UISpriteInfo value)
    {
        if (key == null) key = string.Empty;
        if (value == null) value = new UISpriteInfo();
        ar.ReadWriteValue("Sprite", ref key);
        value.SerializeToTxt(ref ar);
    }
    void SerializeIterator(SerializeText ar, ref string key, ref UITexAtlas value)
    {
        if (key == null) key = string.Empty;
        if (value == null) value = new UITexAtlas();
        ar.ReadWriteValue("Atlas", ref key);
        value.SerializeToTxt(ref ar);
    }
    protected void Serialize(CSerialize ar)
    {
        byte yVersion = 4;
        ar.ReadWriteValue(ref yVersion);
        ar.SetVersion(yVersion);
        if (yVersion > 2)
            ar.ReadWriteValue(ref m_nFileVersion);
        ar.SerializeDictionary<string, UISpriteInfo>(ref m_AllSprite, SerializeIterator);
        ar.SerializeDictionary<string, UITexAtlas>(ref m_TexAtlas, SerializeIterator);
    }
    protected void SerializeToTxt(SerializeText ar)
    {
        byte yVersion = 4;
        ar.ReadWriteValue("Version", ref yVersion);
        if (yVersion > 2)
            ar.ReadWriteValue("SaveCount",ref m_nFileVersion);
        ar.SetVersion(yVersion);
        ar.SerializeDictionary<string, UISpriteInfo>("AllSprite", ref m_AllSprite, SerializeIterator);
        ar.SerializeDictionary<string, UITexAtlas>("AllAtlas", ref m_TexAtlas, SerializeIterator);
    }   

    public void AutoInit()
    {
        if (!m_bInitCfg || n_nObjNumb != (m_AllSprite.Count + m_TexAtlas.Count + 10) )
        {
            m_bInitCfg = true;
            InitAltasCfg();
        }
    }

    protected bool MakeSpriteAtlasID()
    {
        m_SpritePtr.Clear();
        m_AtlasPtr.Clear();
        m_SpritePtr.reserve(m_AllSprite.Count);
        m_AtlasPtr.reserve(m_TexAtlas.Count);

        bool bDirty = false;
        CMyArray<UITexAtlas> newAtlas = new CMyArray<UITexAtlas>();

        Dictionary<string, UITexAtlas>.Enumerator itAtlas = m_TexAtlas.GetEnumerator();
        int nMaxAtlasID = m_TexAtlas.Count + 1;
        while (itAtlas.MoveNext())
        {
            UITexAtlas atlas = itAtlas.Current.Value;
            if (atlas.m_nAtlasID > 0 && atlas.m_nAtlasID <= nMaxAtlasID)
            {
                if (m_AtlasPtr.IsValid(atlas.m_nAtlasID - 1) && m_AtlasPtr[atlas.m_nAtlasID - 1] != null)
                {
                    newAtlas.push_back(atlas);
                }
                else
                {
                    m_AtlasPtr.GrowSet(atlas.m_nAtlasID - 1, atlas);
                }
            }
            else
            {
                newAtlas.push_back(atlas);
            }
        }
        if (newAtlas.size() > 0)
            bDirty = true;
        int nStartPos = m_AtlasPtr.FindNextNull(0);
        for (int i = 0; i < newAtlas.size(); ++i)
        {
            UITexAtlas atlas = newAtlas[i];
            atlas.m_nAtlasID = m_AtlasPtr.FindNextNull(nStartPos) + 1;
            nStartPos = atlas.m_nAtlasID;
            m_AtlasPtr.GrowSet(atlas.m_nAtlasID - 1, atlas);
        }

        CMyArray<UISpriteInfo> newSprite = new CMyArray<UISpriteInfo>();

        Dictionary<string, UISpriteInfo>.Enumerator itSprite = m_AllSprite.GetEnumerator();
        int nMaxID = m_AllSprite.Count + 1;
        while (itSprite.MoveNext())
        {
            UISpriteInfo sp = itSprite.Current.Value;
            sp.m_nAtlasID = AtlasNameToID(sp.m_szAtlasName);
            if (sp.m_nNameID > 0 && sp.m_nNameID <= nMaxID)
            {
                if (m_SpritePtr.IsValid(sp.m_nNameID - 1) && m_SpritePtr[sp.m_nNameID - 1] != null)
                {
                    // 重复的ID
                    newSprite.push_back(sp);
                }
                else
                {
                    m_SpritePtr.GrowSet(sp.m_nNameID - 1, sp);
                }
            }
            else
            {
                newSprite.push_back(sp);
            }
        }
        nStartPos = m_SpritePtr.FindNextNull(0);
        int nNewSpriteCount = newSprite.size();
        for (int i = 0; i < nNewSpriteCount; ++i)
        {
            UISpriteInfo sp = newSprite[i];
            sp.m_nNameID = m_SpritePtr.FindNextNull(nStartPos) + 1;
            nStartPos = sp.m_nNameID;
            m_SpritePtr.GrowSet(sp.m_nNameID - 1, sp);
        }
        if (newSprite.size() > 0)
            bDirty = true;
        return bDirty;
    }

    // 这个是第二次加载噢
    public void ReloadCfg(byte[] fileData)
    {
        if (fileData != null)
        {
            ++m_nLoadNumb;
            Dictionary<string, UISpriteInfo> AllSprite = new Dictionary<string,UISpriteInfo>();   // 所有的精灵对象    
            Dictionary<string, UITexAtlas>   TexAtlas = new Dictionary<string, UITexAtlas>();    // 材质对象

            CSerialize arRead = new CSerialize(SerializeType.read, fileData, fileData.Length);

            int nFileVersion = 0;

            byte yVersion = 3;
            arRead.ReadWriteValue(ref yVersion);
            if (yVersion > 2)
                arRead.ReadWriteValue(ref nFileVersion);

            // 文件没有修改
            if (nFileVersion <= m_nFileVersion)
            {
                return;
            }
            arRead.SetVersion(yVersion);
            arRead.SerializeDictionary<string, UISpriteInfo>(ref AllSprite, SerializeIterator);
            arRead.SerializeDictionary<string, UITexAtlas>(ref TexAtlas, SerializeIterator);
            arRead.Close();
            
            // 插入吧
            Dictionary<string, UITexAtlas>.Enumerator itAtlas = TexAtlas.GetEnumerator();
            while (itAtlas.MoveNext())
            {
                string szAtlas = itAtlas.Current.Key;
                UITexAtlas atlas = null;
                if (m_TexAtlas.TryGetValue(szAtlas, out atlas))
                {
                    UITexAtlas newAtlas = itAtlas.Current.Value;
                    atlas.AdjustAtlas(newAtlas);
                    if ( atlas.m_material != null )
                    {                       
                        // 需要重新加载纹理噢, 其实这里一般不会发生的
                        CAtlasLoader.instance.StartLoatAtlas(this, atlas.m_nAtlasID, true);
                    }
                }
                else
                    m_TexAtlas[szAtlas] = itAtlas.Current.Value;
            }
            // 原有的精灵也释放吧
            m_AllSprite = AllSprite;
            MakeSpriteAtlasID();
        }
    }

    public void InitCfg(byte[] fileData)
    {
        if (fileData != null && m_TexAtlas.Count == 0)
        {
            ++m_nLoadNumb;
            m_bInitCfg = true;
            CSerialize arRead = new CSerialize(SerializeType.read, fileData, fileData.Length);
            Serialize(arRead);

            n_nObjNumb = m_AllSprite.Count + m_TexAtlas.Count + 10;
            MakeSpriteAtlasID();
            // 必要的话，通知所有界面
        }
        m_bLoadingCfg = false;
    }
    public int GetAllSpriteNumb()
    {
        return m_AllSprite.Count;
    }
    public int GetAllAtlasNumb()
    {
        return m_TexAtlas.Count;
    }
    public virtual bool IsEditorMode()
    {
        return false;
    }
    public bool IsForceOneTexture()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        // PC平台强制使用一张纹图
        if (Application.platform == RuntimePlatform.WindowsEditor)
            return IsEditorMode();
        else if(Application.platform == RuntimePlatform.WindowsPlayer)
            return true;// IsEditorMode();
#endif
        return false;
    }

    // 功能：初始化配置吧
    public virtual void InitAltasCfg()
    {
        m_bInitCfg = true;
        n_nObjNumb = m_AllSprite.Count + m_TexAtlas.Count + 10;

        // 异步加载吧
        if (!m_bLoadingCfg)
        {
            FirstLod();
        }
    }

    // 功能：保存配置在编辑器模式下
    public virtual void SaveAltasCfg() { }

    public void OnQueryAtlasTexByAssetBundle(Texture tex, Texture texAlpha, UITexAtlas atlas, bool bReload)
    {
        if (atlas.m_mainBundle != null)
        {
            atlas.m_mainBundle.Unload(true);
            atlas.m_mainBundle = null;
        }
        if (atlas.m_mainAlphaBundle != null)
        {
            atlas.m_mainAlphaBundle.Unload(true);
            atlas.m_mainAlphaBundle = null;
        }

        Texture oldAlpha = null;
        atlas.m_MainAlpha = texAlpha;
        if (texAlpha != null && atlas.m_material != null)
        {
            oldAlpha = atlas.m_material.GetTexture("_MainAlpha");
            atlas.m_material.SetTexture("_MainAlpha", texAlpha);
        }
        Texture oldTex = null;
        if (tex != null)
        {
            if (atlas.m_material != null)
            {
                oldTex = atlas.m_material.mainTexture;
                atlas.m_material.mainTexture = tex;
            }
            if (oldAlpha == oldTex)
                oldAlpha = null;
            if (oldTex != null)
            {
                DestroyTexture(oldTex, false);
            }
        }
        atlas.m_nVersion++;

        atlas.m_bLoading = false;
        if (atlas.m_lpOnLoadAtlas != null)
        {
            UITexAtlas.OnLoadAtlas lpFunc = atlas.m_lpOnLoadAtlas;
            atlas.m_lpOnLoadAtlas = null;
            lpFunc();
            OnAtlasLoad(atlas.m_nAtlasID);
        }
        else if (bReload)
        {
            OnAtlasLoad(atlas.m_nAtlasID);
        }
    }

    public void OnQueryAtlasTex(AssetBundle bundle, AssetBundle alpha_boundle, UITexAtlas atlas, bool bReload)
    {
        if(atlas.m_mainBundle != null)
        {
            atlas.m_mainBundle.Unload(true);
            atlas.m_mainBundle = null;
        }
        if(atlas.m_mainAlphaBundle != null)
        {
            atlas.m_mainAlphaBundle.Unload(true);
            atlas.m_mainAlphaBundle = null;
        }
        atlas.m_mainBundle = bundle;
        atlas.m_mainAlphaBundle = alpha_boundle;

        if (bundle != null)
        {
            Texture mainAlpha = null;
            Texture oldAlpha = null;
            if (alpha_boundle != null)
            {
                mainAlpha = AssetUtility.LoadTexture(alpha_boundle);
                //mainAlpha = alpha_boundle.mainAsset as Texture;
                //alpha_boundle.Unload(false);
                atlas.m_MainAlpha = mainAlpha;
                
                if (atlas.m_material != null)
                {
                    oldAlpha = atlas.m_material.GetTexture("_MainAlpha");
                    atlas.m_material.SetTexture("_MainAlpha", mainAlpha);
                }
            }

            if (bundle != null)
            {
                //Texture tex = bundle.mainAsset as Texture;
                Texture tex = AssetUtility.LoadTexture(bundle);
                Texture oldTex = null;
                if (tex != null)
                {
                    if (atlas.m_material != null)
                    {
                        oldTex = atlas.m_material.mainTexture;
                        atlas.m_material.mainTexture = tex;
                    }
                    if (oldAlpha == oldTex)
                        oldAlpha = null;
                    if (oldTex != null)
                    {
                        DestroyTexture(oldTex, false);
                    }
                }
                atlas.m_nVersion++;
            }
            //if(oldAlpha != null)
            //    DestroyTexture(oldAlpha, false);

            //if (bundle != null)
            //{
            //    bundle.Unload(false);
            //}
        }
        atlas.m_bLoading = false;
        if (atlas.m_lpOnLoadAtlas != null)
        {
            UITexAtlas.OnLoadAtlas lpFunc = atlas.m_lpOnLoadAtlas;
            atlas.m_lpOnLoadAtlas = null;
            lpFunc();
            OnAtlasLoad(atlas.m_nAtlasID);
        }
        else if(bReload)
        {
            OnAtlasLoad(atlas.m_nAtlasID);
        }
    }
    public void OnLoadOtherTextureByAssetBundle(string url, string szResName, Texture tex)
    {
        m_lpFuncAddTextureRef(szResName, 1, false);

        CUITextureCache pTexCache = null;
        if (!m_TexCache.TryGetValue(url, out pTexCache))
        {
            pTexCache = new CUITextureCache();
            m_TexCache[url] = pTexCache;
            pTexCache.m_tex = tex;
            pTexCache.m_bFromAssets = false;
            pTexCache.m_bQueryAssetBundleRef = true;
            pTexCache.m_szResName = szResName;
        }
        else
        {
            if(pTexCache.m_bQueryAssetBundleRef)
            {
                pTexCache.m_bQueryAssetBundleRef = false;
                m_lpFuncAddTextureRef(pTexCache.m_szResName, -1, true);
            }

            pTexCache.m_bLoading = false;
            pTexCache.m_bFromAssets = false;
            pTexCache.m_bQueryAssetBundleRef = true;
            pTexCache.m_szResName = szResName;
            if (tex != null)
            {
                pTexCache.m_bValidTexture = true;
                pTexCache.m_tex = tex;
            }
            else
            {
                pTexCache.m_bValidTexture = false;
            }
            if (pTexCache.m_mainBundle != null)
            {
                pTexCache.m_mainBundle.Unload(true);
                pTexCache.m_mainBundle = null;
            }

            if (pTexCache.m_pOnLoadFunc != null)
            {
                pTexCache.m_pOnLoadFunc(url, pTexCache.m_tex, pTexCache.m_bValidTexture);
                pTexCache.m_pOnLoadFunc = null;
            }
        }
    }
    public void OnLoadOtherTexture(string url, AssetBundle bundle, Texture tex, bool bFromAssets)
    {
        CUITextureCache   pTexCache = null;
        if (!m_TexCache.TryGetValue(url, out pTexCache))
        {
            pTexCache = new CUITextureCache();
            pTexCache.m_mainBundle = bundle;
            m_TexCache[url] = pTexCache;
            pTexCache.m_tex = tex;
            pTexCache.m_bFromAssets = bFromAssets;
        }
        else
        {
            pTexCache.m_bLoading = false;
            pTexCache.m_bFromAssets = bFromAssets;
            if (tex != null)
            {
                pTexCache.m_bValidTexture = true;
                pTexCache.m_tex = tex;
            }
            else
            {
                pTexCache.m_bValidTexture = false;
            }
            if(pTexCache.m_mainBundle != null)
            {
                pTexCache.m_mainBundle.Unload(true);
                pTexCache.m_mainBundle = null;
            }
            pTexCache.m_mainBundle = bundle;

            if (pTexCache.m_pOnLoadFunc != null)
            {
                pTexCache.m_pOnLoadFunc(url, pTexCache.m_tex, pTexCache.m_bValidTexture);
                pTexCache.m_pOnLoadFunc = null;
            }
        }
    }

    void OnAtlasLoad(int nAtlasID)
    {
    }    

    // 功能：创建一个1*1大小的纹理
    Texture2D CreateDefaultTexture()
    {
        Texture2D tex = new Texture2D(1, 1);
        Color32[] newPixels = new Color32[1];
        newPixels[0] = new Color32(0, 0, 0, 0);
        tex.SetPixels32(newPixels);
        tex.Apply();
        return tex;
    }

    // 功能：返回当前Loading的url
    public string GetCurrentLoadingUrl()
    {
        return m_szLoadingUrl;
    }
    // 功能：预加载Loading图
    public void  PrepareLoadingTexture(string url)
    {
        if (string.IsNullOrEmpty(m_szLoadingUrl))
            m_szLoadingUrl = string.Empty;
        if (string.IsNullOrEmpty(url))
            url = string.Empty;
        if (m_szLoadingUrl == url)
        {
            return;
        }
        QueryNetTexture(url, OnLoadLoadingTexture);
    }
    void  OnLoadLoadingTexture(string url, Texture tex, bool bDelayLoad)
    {
        // 加载失败的图
        if(tex == null)
        {
            ReleaseNetTexture(url);
            return;
        }
        if (!string.IsNullOrEmpty(m_szLoadingUrl))
        {
            ReleaseNetTexture(m_szLoadingUrl);
        }
        m_szLoadingUrl = url;
    }
    
    public delegate void OnLoadNetTexture(string url, Texture tex, bool bDelayLoad);
    public Texture QueryNetTexture(string url, OnLoadNetTexture pOnLoadFunc,int callIndex = 0)
    {
        CUITextureCache   pTexCache = null;
        if (!m_TexCache.TryGetValue(url, out pTexCache))
        {       
            pTexCache = new CUITextureCache();
            pTexCache.m_nRef = 1;
            pTexCache.m_bLoading = true;
            pTexCache.m_fTime = Time.time;
            pTexCache.m_pOnLoadFunc = pOnLoadFunc;
            pTexCache.m_bValidTexture = false;
            m_TexCache[url] = pTexCache;
            // 创建一个空图
            pTexCache.m_tex = CreateDefaultTexture();
            pTexCache.m_defTex = pTexCache.m_tex;
            CAtlasLoader.instance.StartLoadOtherTexture(this, url);
        }
        else
        {
            pTexCache.m_nRef++;
            if( pTexCache.m_bLoading )
            {
                pTexCache.m_pOnLoadFunc += pOnLoadFunc;            
            }
            else if (!pTexCache.m_bValidTexture)
            {           
                if (pTexCache.m_defTex == null)
                {
                    pTexCache.m_tex = CreateDefaultTexture();
                    pTexCache.m_defTex = pTexCache.m_tex;
                }
                pTexCache.m_bLoading = true;
                pTexCache.m_pOnLoadFunc += pOnLoadFunc;
                CAtlasLoader.instance.StartLoadOtherTexture(this, url);
            }
            else
            {
                pOnLoadFunc(url, pTexCache.m_tex, false);
            }
        }
        return pTexCache.m_tex;
    }
    // 功能：释放一个动态URL的纹理
    // 参数：bImmRelease - true表示立即释放
    public void ReleaseNetTexture(string url, bool bImmRelease = false)
    {
        CUITextureCache   pTexCache = null;
        if (m_TexCache.TryGetValue(url, out pTexCache))
        {
            pTexCache.m_nRef--; // 不立即释放
            pTexCache.m_fTime = Time.time;
            if(bImmRelease && pTexCache.m_nRef == 0)
            {
                ImmReleaseCacheTexture(pTexCache);
                m_TexCache.Remove(url);
            }
        }
    }

    // 加载吧
    protected virtual bool QueryAltasTex(UITexAtlas atlas, UITexAtlas.OnLoadAtlas  lpOnLoadFunc)
    {
        if (!atlas.m_bLoading)
        {
            // 先尝试本地同步加载
            string url = GetUIResURL("res_texture_" + atlas.m_szAtlasName + ".unity3d");
            //if (TryLoadAtlasTex(atlas, url))
            //{
            //    atlas.m_bLoading = false;
            //    atlas.m_fLoadingTime = Time.time;
            //    return true;
            //}
            atlas.m_bLoading = true;
            atlas.m_fLoadingTime = Time.time;
            if (lpOnLoadFunc != null)
                atlas.m_lpOnLoadAtlas += lpOnLoadFunc;
            string szShaderName = atlas.m_szShaderName;
            if (atlas.m_material == null)
            {
                if (IsForceOneTexture())
                {
                    // PC平台强制使用一张纹图
                    szShaderName = szShaderName.Replace(" MainAlpha", "");
                }
                Shader shader = Shader.Find(szShaderName);
                atlas.m_material = new Material(shader);
                atlas.m_material.name = atlas.m_szAtlasName;
            }
            if (atlas.m_material.mainTexture == null)
            {
                atlas.m_material.mainTexture = CreateDefaultTexture();
                if(szShaderName == "Unlit/Transparent Colored MainAlpha")
                {
                    atlas.m_material.SetTexture("_MainAlpha", atlas.m_material.mainTexture);
                }
            }
            CAtlasLoader.instance.StartLoatAtlas(this, atlas.m_nAtlasID, false);
        }
        else
        {
            if( lpOnLoadFunc != null )
                atlas.m_lpOnLoadAtlas += lpOnLoadFunc;
        }
        return true;
    }
    
    // 功能：切换场景时调用
    public void OnSwitchScene()
    {
        m_fNextUpdateTime = Time.time + 60.0f;  // 延迟60秒释放

        if (m_TexCache.Count > 0)
        {
            ReleaseTextureCache(true);
        }

        //Dictionary<string, UITexAtlas>.Enumerator it = m_TexAtlas.GetEnumerator();
        //while (it.MoveNext())
        //{
        //    // 释放材质吧
        //    UITexAtlas atlas = it.Current.Value;
        //    if( atlas.m_material != null && atlas.m_nRef == 0 )
        //    {
        //        RealReleaseMaterial(atlas);
        //    }
        //}
        //m_NeedReleaseAtlas.Clear();
        //if (m_TexCache.Count > 0)
        //{
        //    ReleaseTextureCache(true);
        //}
    }
        
    // 功能：删除精灵（不需要修改纹理，只需要
    public virtual bool DeleteSprite(List<string> nameListe) { return true; }

    // 功能：查找精灵名字
    public bool FindSpriteByName(string szSpriteName)
    {
        return m_AllSprite.ContainsKey(szSpriteName);
    }
    public UITexAtlas GetAltas(string szAtlasName)
    {
        if (string.IsNullOrEmpty(szAtlasName))
            return null;
        UITexAtlas atlas = null;
        if (m_TexAtlas.TryGetValue(szAtlasName, out atlas))
        {
        }
        return atlas;
    }
    // 功能：得到精灵的材质名称
    public string GetAtlasNameBySpriteName(string szSpriteName)
    {
        if (string.IsNullOrEmpty(szSpriteName))
            return null;
        UISpriteInfo sprite = null;
        if (m_AllSprite.TryGetValue(szSpriteName, out sprite))
        {
            return sprite.m_szAtlasName;
        }
        return null;
    }

    // 功能：判断材质不是是加载了
    public bool IsLoadAtlasBySpriteID(int nSpriteID)
    {
        UISpriteInfo sprite = GetSpriteByID(nSpriteID);
        if (sprite != null)
        {
            UITexAtlas altas = GetAtlasByID(sprite.m_nAtlasID);
            if( altas != null )
            {
                return altas.m_material != null;
            }
        }
        return false;
    }
    public UITexAtlas GetAtlasByID(int nAtlasID)
    {
        if (m_AtlasPtr.IsValid(nAtlasID - 1))
            return m_AtlasPtr[nAtlasID - 1];
        return null;
    }
    public UITexAtlas FastGetAtlasBySpriteID(int nSpriteID)
    {
        if (m_SpritePtr.IsValid(nSpriteID - 1))
        {
            UISpriteInfo sprite = m_SpritePtr[nSpriteID - 1];
            if (sprite != null)
            {
                if (m_AtlasPtr.IsValid(sprite.m_nAtlasID - 1))
                    return m_AtlasPtr[sprite.m_nAtlasID - 1];
            }
        }
        return null;
    }
    public int AtlasNameToID(string szAtlasName)
    {
        if (string.IsNullOrEmpty(szAtlasName))
            return 0;
        UITexAtlas atlas = null;
        if (m_TexAtlas.TryGetValue(szAtlasName, out atlas))
        {
            return atlas.m_nAtlasID;
        }
        return 0;
    }
    public int GetAtlasIDBySpriteID(int nSpriteID)
    {
        UISpriteInfo sprite = GetSpriteByID(nSpriteID);
        if (sprite != null)
            return sprite.m_nAtlasID;
        return 0;
    }
    public int GetSpriteAtlasID(string szSpriteName)
    {
        if (string.IsNullOrEmpty(szSpriteName))
            return 0;
        UISpriteInfo sprite = null;
        if (m_AllSprite.TryGetValue(szSpriteName, out sprite))
        {
            return sprite.m_nAtlasID;
        }
        return 0;
    }
    public int SpriteNameToID(string szSpriteName)
    {
        if (string.IsNullOrEmpty(szSpriteName))
            return 0;
        UISpriteInfo sprite = null;
        if (m_AllSprite.TryGetValue(szSpriteName, out sprite))
        {
            return sprite.m_nNameID;
        }
        return 0;
    }
    public int GetAtlasRefBySpriteID(int nSpriteID)
    {
        UITexAtlas atlas = FastGetAtlasBySpriteID(nSpriteID);
        if (atlas != null)
        {
            return atlas.m_nRef;
        }
        return 0;
    }
    public bool IsQueryAtlasBySpriteID(int nSpriteID)
    {
        UITexAtlas atlas = FastGetAtlasBySpriteID(nSpriteID);
        if (atlas != null)
        {
            return atlas.m_material != null;
        }
        return false;
    }

    public Material GetHypelinkMaterial()
    {
        if (m_hyperlinkMat == null)
        {
            m_hyperlinkMat = new Material(Shader.Find("Unlit/FillSolid"));
        }
        return m_hyperlinkMat;
    }
    public Material  QueryHypelinkMaterial()
    {
        if(m_hyperlinkMat == null)
        {
            m_hyperlinkMat = new Material(Shader.Find("Unlit/FillSolid"));
        }
        ++m_hyperlinkMatRef;
        return m_hyperlinkMat;
    }
    public void ReleaseHypelinkMaterial()
    {
        --m_hyperlinkMatRef;
        if(m_hyperlinkMatRef ==0)
        {
            if (m_hyperlinkMat != null)
                GameObject.DestroyImmediate(m_hyperlinkMat);
            m_hyperlinkMat = null;
        }
    }

    // 功能：申请图集的资源
    public void QueryAtlasByID(int nAtlasID, UITexAtlas.OnLoadAtlas lpOnLoadFunc)
    {
        UITexAtlas atlas = GetAtlasByID(nAtlasID);
        QueryByAtlas(atlas, lpOnLoadFunc);
    }
    // 功能：申请材质资源
    public void QueryAtlasBySpriteID(int nSpriteID, UITexAtlas.OnLoadAtlas lpOnLoadFunc)
    {
        UITexAtlas atlas = FastGetAtlasBySpriteID(nSpriteID);
        QueryByAtlas(atlas, lpOnLoadFunc);
    }
    private void QueryByAtlas(UITexAtlas atlas, UITexAtlas.OnLoadAtlas lpOnLoadFunc)
    {
        if (atlas != null)
        {
            if (atlas.m_material == null || atlas.m_material.mainTexture == null || atlas.m_bLoading)
                QueryAltasTex(atlas, lpOnLoadFunc);
            atlas.m_nRef++;
            m_QueryAtlas[atlas.m_nAtlasID] = atlas;
        }
    }

    // 功能：申请材质资源
    public void QueryAtlasBySpriteName(string szSpriteName)
    {
        if (string.IsNullOrEmpty(szSpriteName))
            return ;
        UISpriteInfo  sprite = null;
        if (m_AllSprite.TryGetValue(szSpriteName, out sprite))
        {
            UITexAtlas atlas = null;
            if (m_TexAtlas.TryGetValue(sprite.m_szAtlasName, out atlas))
            {
                if (atlas.m_material == null)
                    QueryAltasTex(atlas, null);
                atlas.m_nRef++;
                m_QueryAtlas[atlas.m_nAtlasID] = atlas;
            }
        }
    }
    void PushReleaseAtlasID(int nAtlasID)
    {
        for (int i = 0, iLen = m_NeedReleaseAtlas.size(); i < iLen; ++i)
        {
            if (m_NeedReleaseAtlas[i] == nAtlasID)
                return;
        }
        m_NeedReleaseAtlas.push_front(nAtlasID);
    }
    // 功能：释放图集
    public void ReleaseAtlasByID(int nAtlasID)
    {
        UITexAtlas atlas = GetAtlasByID(nAtlasID);
        RelaaseByAtlas(atlas);
    }
    public void ReleaseAtlasBySprteID(int nSpriteID)
    {
        UITexAtlas atlas = FastGetAtlasBySpriteID(nSpriteID);
        RelaaseByAtlas(atlas);
    }
    private  void RelaaseByAtlas(UITexAtlas atlas)
    {
        if (atlas != null)
        {
            atlas.m_nRef--;
            if (atlas.m_nRef == 0)
            {
                // 从前面添加
                PushReleaseAtlasID(atlas.m_nAtlasID);

                // 这里只做标记，延迟释放吧
                atlas.m_fReleaseTime = Time.time;

                atlas.m_nVersion++;

                m_QueryAtlas.Remove(atlas.m_nAtlasID);

                if(IsLowQuality() && m_NeedReleaseAtlas.size() > 5)
                {
                    m_fNextUpdateTime = 0.0f;
                }
            }
        }
    }

    // 功能：通过精灵的ID得到材质
    public UITexAtlas GetAltasBySpriteID(int nSpriteID)
    {
        UISpriteInfo sprite = GetSpriteByID(nSpriteID);
        if (sprite != null)
        {
            UITexAtlas atlas = GetAtlasByID(sprite.m_nAtlasID);
            if( atlas.m_material == null )
                QueryAltasTex(atlas, null);
            return atlas;
        }
        return null;
    }
    // 功能：通过精灵的名字得到材质
    public UITexAtlas GetAltasBySpriteName(string szSpriteName)
    {
        if (string.IsNullOrEmpty(szSpriteName))
            return null;
        UISpriteInfo  sprite = null;
        if (m_AllSprite.TryGetValue(szSpriteName, out sprite) )
        {
            UITexAtlas atlas = null;
            if (m_TexAtlas.TryGetValue(sprite.m_szAtlasName, out atlas))
            {
                // 加载纹理吧
                if( atlas.m_material == null )
                    QueryAltasTex(atlas, null);
                return atlas;
            }
        }
        return null;
    }
    public UITexAtlas GetSafeAltasBySpriteID(int nSpriteID)
    {
        UITexAtlas altas = FastGetAtlasBySpriteID(nSpriteID);
        if (altas != null)
            return altas;
        return m_defTexAltas; // 返回一个默认的
    }
    public UISpriteInfo GetSpriteByID(int nSpriteID)
    {
        if (m_SpritePtr.IsValid(nSpriteID - 1))
            return m_SpritePtr[nSpriteID - 1];
        return null;
    }
    public UISpriteInfo GetSprite(string szSpriteName)
    {
        if (string.IsNullOrEmpty(szSpriteName))
            return null;
        UISpriteInfo  sprite = null;
        if (m_AllSprite.TryGetValue(szSpriteName, out sprite))
        {
            return sprite;
        }
        return null;
    }
    public int GetSpriteWidth(int nSpriteID)
    {
        UISpriteInfo sp = GetSpriteByID(nSpriteID);
        return sp != null ? (int)(sp.outer.width + 0.5f) : 0;
    }
    public int GetSpriteHeight(int nSpriteID)
    {
        UISpriteInfo sp = GetSpriteByID(nSpriteID);
        return sp != null ? (int)(sp.outer.height + 0.5f) : 0;
    }
    public UISpriteInfo GetSafeSpriteByID(int nSpriteID)
    {
        UISpriteInfo sprite = GetSpriteByID(nSpriteID);
        if (sprite != null)
            return sprite;
        return m_defSprite;
    }
    public List<UISpriteInfo> GetAllSprite()
    {
        List<UISpriteInfo> aSprite = new List<UISpriteInfo>();

        Dictionary<string, UISpriteInfo>.Enumerator it = m_AllSprite.GetEnumerator();
        while (it.MoveNext())
        {
            aSprite.Add(it.Current.Value);
        }
        return aSprite;
    }
    public int GetSpriteAtlasVersion(int nSpriteID)
    {
        UITexAtlas altas = FastGetAtlasBySpriteID(nSpriteID);
        if( altas != null )
        {
            return altas.m_nVersion;
        }
        return 0;
    }
    // 功能：得到指定材质，指定UV的像素颜色
    // 说明：仅限编辑器模式生效
    public virtual Color GetAtlasPixelBilinear(int nAtlasID, float fu, float fv) { return Color.white; }    
}
