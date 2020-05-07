using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

///////////////////////////////////////////////////////////
//
//  Written by              ：laishikai
//  Copyright(C)            ：成都博瑞梦工厂
//  ------------------------------------------------------
//  功能描述                ：封装UI预制资源的加载
//
///////////////////////////////////////////////////////////

public class UIRootLoader
{
    public class  RootChildPanel
    {
        public string  m_szParentName;
        public string  m_szChildName;
    };

    public List<RootChildPanel> m_AllChild = new List<RootChildPanel>();

    public void PushChiid(string szParentName, string szChildName)
    {
        RootChildPanel node = new RootChildPanel();
        node.m_szParentName = szParentName;
        node.m_szChildName = szChildName;
        m_AllChild.Add(node);
    }
    
    void SerializeChildPanel(SerializeText ar, ref RootChildPanel value)
    {
        if (value == null)
            value = new RootChildPanel();
        ar.ReadWriteValue("ParentName", ref value.m_szParentName);
        ar.ReadWriteValue("ChildName", ref value.m_szChildName);
    }
    public void Serialize(SerializeText ar)
    {
        byte yVersion = 0;
        ar.ReadWriteValue("Version", ref yVersion);
        ar.SerializeArray("ChildCount", ref m_AllChild, SerializeChildPanel);
    }
    public void ReadFrom(byte[] fileData)
    {
        if (fileData == null)
            return;
        SerializeText ar = new SerializeText(SerializeType.read, fileData, fileData.Length);
        Serialize(ar);
    }
}

public class UIPrefabLoader
{
    public List<string>    m_UIPrefabName = new List<string>(); // UI实例
    public List<string>    m_DependPrefabName = new List<string>(); // 依赖项
    public List<string> m_FontPrefabName = new List<string>();   // 依赖的字体(Prefab/UI/DFont.prefab ==>Prefab_UI_DFont)
    public Dictionary<string, Object> m_AllObj = new Dictionary<string, Object>();
    public Dictionary<string, string> m_DelayLoad = new Dictionary<string, string>(); // 延迟加载的列表
    public AssetBundle m_packResBundle;
    public GameObject m_uiResRoot;  // UI资源的根节点
    public UIFont m_dfFont;
    private bool m_bAsyncPreLoad = false;

    static Dictionary<string, Object> s_FontList = new Dictionary<string, Object>();

    static public UIPrefabLoader s_UIPrefabLoader;

    static public UIPrefabLoader instance
    {
        get
        {
            return s_UIPrefabLoader;
        }
        set
        {
            s_UIPrefabLoader = value;
        }
    }

    public void ReleaseUIResource()
    {
        if(m_packResBundle != null)
        {
            //m_packResBundle.Unload(true);
            m_packResBundle = null;
        }
        s_UIPrefabLoader = null;
        if (m_uiResRoot != null)
            DestroyImmediate(m_uiResRoot);
        m_uiResRoot = null;
    }

    static public void DestroyImmediate(UnityEngine.Object obj)
    {
        if (obj != null)
        {
            if (Application.isEditor) UnityEngine.Object.DestroyImmediate(obj);
            else UnityEngine.Object.Destroy(obj);
        }
    }

    void SerializePrefabName(SerializeText ar, ref string value)
    {
        if (value == null)
            value = string.Empty;
        ar.ReadWriteValue("name", ref value);
    }
    public void Serialize(SerializeText ar)
    {
        byte yVersion = 0;
        ar.ReadWriteValue("Version", ref yVersion);
        ar.SerializeArray("ChildCount", ref m_UIPrefabName, "name");
        ar.SerializeArray("DependCount", ref m_DependPrefabName, "name");
        ar.SerializeArray("FontCount", ref m_FontPrefabName, "name");
    }
    public void ReadFrom(byte[] fileData)
    {
        if (fileData == null)
            return;
        SerializeText ar = new SerializeText(SerializeType.read, fileData, fileData.Length);
        Serialize(ar);
    }

    // 功能：设置资源Bundle
    public void   SetPackResBundle(AssetBundle  packResBundle)
    {
        m_packResBundle = packResBundle;
        string  []allAssertName = m_packResBundle.GetAllAssetNames();
        for(int i = 0; i<allAssertName.Length; ++i)
        {
            string szLowerName = allAssertName[i].ToLower();
            if (szLowerName.Equals("empty_ui"))
            {
                continue;
            }
            if (!IsNeedLoad(szLowerName))
                continue;
            szLowerName = szLowerName.Substring(16);
            if (szLowerName.Contains("/") && !szLowerName.Equals("/dfont.prefab"))  //预制的路劲多了些层级
            {
                int index = szLowerName.LastIndexOf("/");
                int startIndex = index + 1;
                szLowerName = szLowerName.Substring(startIndex);
            }
            int nIndex = szLowerName.IndexOf(".prefab");
            if(nIndex != -1)
            {
                szLowerName = szLowerName.Remove(nIndex);
            }
            if (szLowerName == "/dfont")
            {
                szLowerName = "dfont";
                //continue;
            }
            m_DelayLoad[szLowerName] = allAssertName[i];
        }
    }

    bool  IsNeedLoad(string szLowerName)
    {
        if (szLowerName.IndexOf("assets/uiprefab/") == 0)
            return true;    
        return szLowerName.IndexOf("assets/prefab/ui/dfont.prefab") == 0; // 这个字体是要加载的
    }

    // 功能：立即加载所有的UI
    public void LoadAllUI(AssetBundle bundle_pack)
    {
        GameObject objResRoot = new GameObject("_ui_res_root");
        GameObject.DontDestroyOnLoad(objResRoot);
        objResRoot.hideFlags = HideFlags.DontSave;
        objResRoot.SetActive(false);
        m_uiResRoot = objResRoot;
        Transform tfResRoot = objResRoot.transform;
        GameObject[] allUIObj = bundle_pack.LoadAllAssets<GameObject>();
        int nUINumb = 0;
        GameObject obj = null;
        if (allUIObj != null)
        {
            nUINumb = allUIObj.Length;
            for (int i = 0; i < nUINumb; ++i)
            {
                obj = allUIObj[i];
                if (obj.name.Equals("empty_ui"))
                {
                    continue;
                }
                allUIObj[i].transform.SetParent(tfResRoot, false);
                m_AllObj[obj.name.ToLower()] = obj;
            }
        }
    }

    // 功能：执行下一个加载逻辑
    public bool  DoNextDelayLoad()
    {
        if(m_DelayLoad.Count > 0 )
        {
            Dictionary<string, string>.Enumerator it = m_DelayLoad.GetEnumerator();
            while(it.MoveNext())
            {
                LoadUIResource(it.Current.Key);
                break;
            }
            return true;
        }
        return false;
    }

    public void OnBeginAsyncLoadUI()
    {
        m_bAsyncPreLoad = true;

        if (m_uiResRoot == null)
        {
            m_uiResRoot = new GameObject("_ui_res_root");
            GameObject.DontDestroyOnLoad(m_uiResRoot);
            m_uiResRoot.hideFlags = HideFlags.DontSave;
            m_uiResRoot.SetActive(false);
        }
    }

    public bool IsInDelayList(string szLowerName)
    {
        return m_DelayLoad.ContainsKey(szLowerName);
    }

    public void OnAsyncLoadUI(string name, GameObject uiAsset)
    {
        string szLowerName = name.ToLower();
        Object obj = null;
        if (m_AllObj.TryGetValue(szLowerName, out obj))
        {
            return;
        }
        GameObject uiObj = uiAsset;
        //if (Application.platform == RuntimePlatform.WindowsEditor)
        //{
        //    UIEffect.OnBeginPrepareInstance();
        //    uiObj = GameObject.Instantiate(uiAsset);
        //    UIEffect.OnEndPrepareInstance();
        //    uiObj.name = uiAsset.name;
        //}
        if (uiObj != null)
        {
            uiObj.transform.SetParent(m_uiResRoot.transform, false);
            m_AllObj[szLowerName] = uiObj;
        }
        m_DelayLoad.Remove(szLowerName);        
    }

    public void OnEndAsyncLoadUI()
    {
        m_bAsyncPreLoad = false;
        if (m_DelayLoad.Count == 0)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                //m_packResBundle.Unload(false);
                //m_packResBundle = null;
            }
            //m_packResBundle.Unload(false);
            //m_packResBundle = null;
        }
    }

    // 代替 Resources.load
    public Object LoadUIResource(string name)
    {
        // 这里忽略大小写吧
        string szLowerName = name.ToLower();
        Object obj = null;
        if( m_AllObj.TryGetValue(szLowerName, out obj) )
        {
            return obj;
        }
        
        // 没有就立即加一个吧
        if (m_packResBundle != null)
        {
            if(m_uiResRoot == null)
            {
                m_uiResRoot = new GameObject("_ui_res_root");
                GameObject.DontDestroyOnLoad(m_uiResRoot);
                m_uiResRoot.hideFlags = HideFlags.DontSave;
                m_uiResRoot.SetActive(false);
            }

            string szAssertName = string.Empty;
            if(m_DelayLoad.TryGetValue(szLowerName, out szAssertName))
            {
                //long nStartTime = System.DateTime.Now.Ticks;
                GameObject uiAsset = m_packResBundle.LoadAsset<GameObject>(szAssertName);
                GameObject uiObj = uiAsset;
                if(Application.platform == RuntimePlatform.WindowsEditor)
                {
                    uiObj = GameObject.Instantiate(uiAsset);
                    uiObj.name = uiAsset.name;
                }
                else if(szLowerName == "dfont")
                {
                    uiObj = GameObject.Instantiate(uiAsset);
                    uiObj.name = uiAsset.name;
                }
                if (uiObj != null)
                {
                    uiObj.transform.SetParent(m_uiResRoot.transform, false);
                    m_AllObj[szLowerName] = uiObj;
                }
                m_DelayLoad.Remove(szLowerName);

                // 全部加载完成，就释放这玩意吧
                if(m_DelayLoad.Count == 0 && !m_bAsyncPreLoad)
                {
                    //m_packResBundle.Unload(false);
                    //m_packResBundle = null;
                }
                //long nCostTime = (System.DateTime.Now.Ticks - nStartTime) / 1000;
                //if(nCostTime > 0)
                //{
                //    Debug.LogError(szAssertName + "加载费时" + nCostTime);
                //}
                return uiObj;
            }
        }
        else
        {
            string szUIName = string.Empty;
            if (m_DelayLoad.TryGetValue(szLowerName, out szUIName))
            {
                Debug.LogError("出问题了，无法加载UI资源" + szUIName);
                m_DelayLoad.Remove(szLowerName);
            }
        }

        return obj; // 如果都没有，就去Resources目录查找
    }
    static public string GetFileName(string szAssetsName)
    {
        int nBegin = szAssetsName.LastIndexOf('/') + 1;
        int nEnd = szAssetsName.LastIndexOf('.');
        string name = szAssetsName.Substring(nBegin, nEnd - nBegin);
        return name;
    }
    static public void RegisterFont(UIFont font)
    {
        if (font == null || s_UIPrefabLoader == null)
            return;
        if (s_UIPrefabLoader.m_dfFont != null)
            return;
        if(font.name == "DFont")
        {
            s_UIPrefabLoader.m_dfFont = font;
        }
    }
    // 功能：加载字体
    static public Object LoadFont(string szAssetsName)
    {
        Object pFont = null;
        if (s_FontList.TryGetValue(szAssetsName, out pFont))
        {
            if (pFont != null)
                return pFont;
            else
                s_FontList.Remove(szAssetsName);
        }

#if UNITY_EDITOR
        //if (!Application.isPlaying)
        if(CAtlasMng.instance.IsEditorMode())
        {
            // 直接加载吧
            GameObject fontObj = AssetDatabase.LoadAssetAtPath(szAssetsName, typeof(Object)) as GameObject;
            if (fontObj != null)
            {
                UIFont font = fontObj.GetComponent<UIFont>();
                s_FontList[szAssetsName] = font;
                return font;
            }
        }
#endif
        string name = GetFileName(szAssetsName);

        // 转换成依赖项
        Object obj = s_UIPrefabLoader != null ? s_UIPrefabLoader.LoadUIResource(name) : null;
        if (obj != null)
        {
            GameObject fontObj = obj as GameObject;
            if (fontObj != null)
            {
                UIFont font = fontObj.GetComponent<UIFont>();
                s_FontList[szAssetsName] = font;
                return font;
            }
        }
        if (obj == null)
            obj = Resources.Load(szAssetsName);
        if(obj == null && s_UIPrefabLoader != null)
        {
            if(s_UIPrefabLoader.m_dfFont != null)
            {
                obj = s_UIPrefabLoader.m_dfFont;
            }
        }
        s_FontList[szAssetsName] = obj;
        return obj;
    }
    static public Object LoadUIRoot()
    {
#if UNITY_EDITOR
        // 如果是调试模式
        string szAssetsName = "Assets/Prefab/UI/empty_ui.prefab";
        Object uiObj = AssetDatabase.LoadAssetAtPath(szAssetsName, typeof(Object));
        if (uiObj != null)
        {
            GameObject objIns = Object.Instantiate(uiObj) as GameObject;
            objIns.name = uiObj.name;
            Object.DontDestroyOnLoad(objIns);
            return objIns;
        } 
#endif
        return null;
    }

#if UNITY_EDITOR
    static Dictionary<string, string> s_NameToAssetsName;  // 文件名==>Assets路径
    static void InitUIPrefabAssertPath()
    {
        if (s_NameToAssetsName != null)
            return;
        s_NameToAssetsName = new Dictionary<string, string>();

        string szDataPath = Application.dataPath;
        string szUIPath = Application.dataPath + "/UIPrefab/";
        string[] fileList = Directory.GetFiles(szUIPath, "*.prefab", SearchOption.AllDirectories);
        int nRootLen = szDataPath.Length - 6;
        if (fileList == null)
            return ;
        string szAssertName = string.Empty;
        string szName = string.Empty;
        for (int i = 0; i<fileList.Length; ++i)
        {
            szAssertName = fileList[i].Substring(nRootLen);
            szAssertName = szAssertName.Replace('\\', '/');
            szName = Path.GetFileNameWithoutExtension(fileList[i]).ToLower();
            if(s_NameToAssetsName.ContainsKey(szName))
            {
                Debug.LogError("UI预制名字重复，这是不允许的:" + szAssertName);
            }
            s_NameToAssetsName[szName] = szAssertName;
        }
    }
    static string  GetUIPrefabAssetsName(string name)
    {
        name = name.ToLower();
        InitUIPrefabAssertPath();
        string szAssetsName = string.Empty;
        if (s_NameToAssetsName.TryGetValue(name, out szAssetsName))
            return szAssetsName;
        szAssetsName = "Assets/UIPrefab/" + name + ".prefab";
        return szAssetsName;
    }
#endif

    static public bool  IsUIPrefab(string name)
    {
#if UNITY_EDITOR
        if (CAtlasMng.instance.IsEditorMode())
        {
            InitUIPrefabAssertPath();
            name = name.ToLower();
            if (s_NameToAssetsName.ContainsKey(name))
                return true;
            return false;
        }
#endif
        if (s_UIPrefabLoader != null)
        {
            string szLowerName = name.ToLower();
            if (s_UIPrefabLoader.m_AllObj.ContainsKey(szLowerName))
            {
                return true;
            }
            if (s_UIPrefabLoader.m_DelayLoad.ContainsKey(szLowerName))
            {
                return true;
            }
        }
        return false;
    }

    static public Object Load(string name)
    {
       
#if UNITY_EDITOR
        // 如果是调试模式
        if (CAtlasMng.instance.IsEditorMode())
        {
            string szAssetsName = GetUIPrefabAssetsName(name);
            Object uiObj = AssetDatabase.LoadAssetAtPath(szAssetsName, typeof(Object));
            if (uiObj != null)
            {
                return uiObj;
            }
        }
#endif

        // 加载界面吧，必须打包了
        Object obj = s_UIPrefabLoader != null ? s_UIPrefabLoader.LoadUIResource(name) : null;
        //if (obj == null)
        //    Helper.LogError(string.Format("Load prefab {0}, can't find!", name));
        return  obj;
    }
};
