//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Atlas maker lets you create atlases from a bunch of small textures. It's an alternative to using the external Texture Packer.
/// </summary>

// 新材质编辑器
[ExecuteInEditMode]
public class MyAtlasMaker : EditorWindow
{
    // 当前选中的对象
    class SelectSpriteInfo
    {
        public string m_szAssetsName;   // 资源目录
        public string m_szSpriteName;   // 精灵名字
        public string m_szAtlasName;    // 材质名字
        public UISpriteInfo m_sprite;   // 精灵对象
        public bool m_bSelect;          // 是不是选中状态
        public bool m_bDelete;          // 是不是设置删除标记
        public bool m_bCurSelect;       // 当前选中状态
        public Texture m_selectTex;     // 选择对象
        public int m_nIndex;
        public int m_nNextAssetsType;   // 下一个材质类型
    };
    
    string m_szSelectSpriteName = "";  // 当前选中的精灵名字
    SelectSpriteInfo m_pSelectSprite;
    int m_nSelectCount;

    GameObject m_curObj;
    MyUISpritePreView m_curSelectSprite;

    List<SelectSpriteInfo> m_SelectSprite = new List<SelectSpriteInfo>();
    bool m_bInit;
    Vector2 m_vScrollPos = Vector2.zero;
    float m_fScrollPos;
    bool m_bSortUpID = true;
    bool m_bSortUpSpriteName;
    bool m_bSortUpAtlasName;
    bool m_bSortUpSelect;

    string m_szLockSearchName = ""; // 锁定名字
    string[] m_AtlasKeywords;
    string[] m_SpriteKeywords;
    
    // 点击 select按纽后的事件
	void OnSelectAtlas (MonoBehaviour obj)
	{
		//NGUISettings.atlas = obj as UIAtlas;
		Repaint();
	}
    
    void OnEnable()
    {
        AtlasMng_Editor.instance.IsCanSave();
        OnSelectionChange();
    }

    void OnDisable()
    {
        if (m_curObj != null)
        {
            Object.DestroyImmediate(m_curObj);
            m_curObj = null;
        }
        m_curSelectSprite = null;
    }
    
	public bool ExportTGA(string szPathName, Color32 []iconPixel, int nWidth, int nHeight)
	{		
		CSerialize ar = new CSerialize(SerializeType.write, szPathName);
		ar.Write( (byte)0 );
		ar.Write( (byte)0 );
		ar.Write( (byte)2 );  // m_ImageType
		ar.Write( (short)0 );
		ar.Write( (short)0 );
		ar.Write( (byte)0 );
		
		ar.Write( (short)0 );
		ar.Write( (short)0 );
		ar.Write( (short)nWidth );
		ar.Write( (short)nHeight );
		ar.Write( (byte)32 );
		ar.Write( (byte)8 );
		
		// BGRA
		int  nLen = iconPixel.Length;
		for( int i = 0; i<nLen; ++i )
		{
			ar.Write( iconPixel[i].b );
			ar.Write( iconPixel[i].g );
			ar.Write( iconPixel[i].r );
			ar.Write( iconPixel[i].a );
		}
		ar.Close();
        return true;
    }
    
    // 功能：选择一个精灵对象
    public void OnSelectSprite(string szNewSelectSpriteName)
    {
        if (m_curSelectSprite == null)
        {
            if (m_curObj == null)
            {
                GameObject obj = new GameObject();
                m_curObj = obj;
                m_curObj.SetActive(false);
            }
            m_curSelectSprite = m_curObj.AddComponent<MyUISpritePreView>();
        }
        if (m_curSelectSprite.spritename != szNewSelectSpriteName)
        {
            m_szSelectSpriteName = szNewSelectSpriteName;
            m_pSelectSprite = null;
            m_curSelectSprite.spritename = szNewSelectSpriteName;
        }
        Selection.activeObject = m_curSelectSprite;
    }

    void Awake()
    {
        if (!m_bInit)
        {
            m_bInit = true;
            RefreshSelectObject();
        }
    }
    void Start()
    {
        if (!m_bInit)
        {
            m_bInit = true;
            RefreshSelectObject();
            NGUISettings.atlasTrimming = false;
        }
    }
    
	/// <summary>
	/// Refresh the window on selection.
	/// </summary>

    // 当有选中对象改变时发生的操作
	void OnSelectionChange ()
    {
        RefreshSelectObject();
		Repaint();
		Object []objs = Selection.objects;
		if( objs != null && objs.Length == 1 )
		{
			GameObject  gObj = objs[0] as GameObject;
			if( gObj != null )
			{
				MyUISpritePreView  spView = gObj.GetComponent<MyUISpritePreView>();
				if( spView != null )
				{
					OnSelectSprite(spView.name);
					m_bSortUpSelect = false;
					SortBySelect();
				}
			}
		}
    }
	
    void RefresyByLockName()
    {
        // 分离名字吧
        int nPos = m_szLockSearchName.IndexOf('&');
        string szAtlaName = string.Empty;
        string szSpriteName = string.Empty;
        if (nPos != -1)
        {
            szAtlaName = m_szLockSearchName.Substring(0, nPos);
            szSpriteName = m_szLockSearchName.Substring(nPos + 1);
        }
        else
        {
            szSpriteName = m_szLockSearchName;
        }
        szAtlaName = szAtlaName.ToLower();
        szSpriteName = szSpriteName.ToLower();

        if (string.IsNullOrEmpty(szAtlaName))
        {
            // 只按名字搜吧
            m_AtlasKeywords  = null;
            m_SpriteKeywords = szSpriteName.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        }
        else if (string.IsNullOrEmpty(szSpriteName))
        {
            // 只按材质搜吧
            m_AtlasKeywords  = szAtlaName.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            m_SpriteKeywords = null;
        }
        else
        {
            // 按材质-精灵名搜
            m_AtlasKeywords  = szAtlaName.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            m_SpriteKeywords = szSpriteName.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        }
        RefreshSelectObject();
    }

    bool IsAlikeName(string szName, string[] keywords)
    {
        string tl = szName.ToLower();
        int matches = 0;
        for (int b = 0; b < keywords.Length; ++b)
        {
            if (tl.Contains(keywords[b])) ++matches;
        }
        return matches == keywords.Length;
    }

    bool IsNeedShow(string szName)
    {
        int nSpriteID = AtlasMng_Editor.instance.SpriteNameToID(szName);
        if (0 == nSpriteID)
        {
            return true;
        }
        if (m_AtlasKeywords != null && m_AtlasKeywords.Length > 0)
        {
            string  szAtlasName = AtlasMng_Editor.instance.GetAtlasNameBySpriteName(szName);
            if (!IsAlikeName(szAtlasName, m_AtlasKeywords))
                return false;
        }
        if (m_SpriteKeywords != null && m_SpriteKeywords.Length > 0)
        {
            if (!IsAlikeName(szName, m_SpriteKeywords))
                return false;
        }
        return true;
    }

	void RefrshUIObject(List<string> nameList)
	{
		Dictionary<string, string>  atlasList = new Dictionary<string, string>();
		foreach(string szName in nameList)
		{
			atlasList[szName] = szName;
		}
	}

    void RefreshSelectObject()
    {
        m_bInit = true;

        List<UISpriteInfo> aSprite = CAtlasMng.instance.GetAllSprite();

        m_SelectSprite.Clear();
        List<SelectSpriteInfo> aSelect = GetSelectedInfo();
        Dictionary<string, int> SelectFlags = new Dictionary<string,int>();
        m_nSelectCount = aSelect.Count;
        for (int i = 0; i < m_nSelectCount; ++i)
        {
            SelectFlags[aSelect[i].m_szSpriteName] = 1;
            m_SelectSprite.Add(aSelect[i]);
            aSelect[i].m_bSelect = true;
            aSelect[i].m_bDelete = false;
            if (m_pSelectSprite != null && m_pSelectSprite.m_szSpriteName == aSelect[i].m_szSpriteName)
            {
                m_pSelectSprite = null;
            }
        }
        int nSpriteCount = aSprite.Count;
        for (int i = 0; i < nSpriteCount; ++i)
        {
            if (SelectFlags.ContainsKey(aSprite[i].name))
                continue;
            if (!IsNeedShow(aSprite[i].name))
                continue;
            SelectSpriteInfo node = new SelectSpriteInfo();
            node.m_sprite       = aSprite[i];
            node.m_szAtlasName  = aSprite[i].m_szAtlasName;
            node.m_szSpriteName = aSprite[i].name;
            m_SelectSprite.Add(node);
        }
        nSpriteCount = m_SelectSprite.Count;
        for (int i = 0; i < nSpriteCount; ++i)
        {
            m_SelectSprite[i].m_nIndex = i;
            if (m_szSelectSpriteName == m_SelectSprite[i].m_szSpriteName)
            {
                m_SelectSprite[i].m_bCurSelect = true;
                if (m_pSelectSprite != null && m_pSelectSprite.m_szSpriteName == m_szSelectSpriteName)
                {
                    m_SelectSprite[i].m_szAtlasName = m_pSelectSprite.m_szAtlasName;
                    m_SelectSprite[i].m_nNextAssetsType = m_pSelectSprite.m_nNextAssetsType;
                    m_SelectSprite[i].m_szAssetsName = m_pSelectSprite.m_szAssetsName;
                }
            }
        }
    }
    
    List<SelectSpriteInfo> GetSelectedInfo()
    {
        List<SelectSpriteInfo> aSelect = new List<SelectSpriteInfo>();

        List<Texture> aTexList = AtlasMng_Editor.GetSelectedTextures();

        foreach (Texture o in aTexList)
        {
            string szName = AtlasMng_Editor.GetAssetPathByTexture(o);
            if (!string.IsNullOrEmpty(szName))
            {
                if(szName.IndexOf("Assets/Atlas/") == 0)
                {
                    continue;
                }
                SelectSpriteInfo info = new SelectSpriteInfo();
                info.m_szAssetsName = szName;
                info.m_szSpriteName = AtlasMng_Editor.GetSpriteNameByAssetsName(szName);
				info.m_szAtlasName = CAtlasMng.instance.GetAtlasNameBySpriteName(info.m_szSpriteName);
				if( string.IsNullOrEmpty(info.m_szAtlasName) )
				{
					info.m_szAtlasName = NGUISettings.atlasName.Trim();
					if( string.IsNullOrEmpty(info.m_szAtlasName) )
					{
						info.m_szAtlasName = "default";
					}
				}
                UITexAtlas atlas = CAtlasMng.instance.GetAltas(info.m_szSpriteName);
                if (atlas != null)
				{
					continue;
				}                

                info.m_sprite = CAtlasMng.instance.GetSprite(info.m_szSpriteName);
                info.m_selectTex = o;
                aSelect.Add(info);
            }
        }
        return aSelect;
    }

    static int CompareSpritesByID(SelectSpriteInfo a, SelectSpriteInfo b) { if (a.m_nIndex != b.m_nIndex) return a.m_nIndex < b.m_nIndex ? -1 : 1; return 0; }
    static int CompareSpritesByID_Down(SelectSpriteInfo a, SelectSpriteInfo b) { if (a.m_nIndex != b.m_nIndex) return a.m_nIndex > b.m_nIndex ? -1 : 1; return 0; }
    static int CompareSpritesBySpriteName(SelectSpriteInfo a, SelectSpriteInfo b) { return a.m_szSpriteName.CompareTo(b.m_szSpriteName); }
    static int CompareSpritesBySpriteName_Down(SelectSpriteInfo a, SelectSpriteInfo b) { return b.m_szSpriteName.CompareTo(a.m_szSpriteName); }
    static int CompareSpritesByAtlsName(SelectSpriteInfo a, SelectSpriteInfo b) { return a.m_szAtlasName.CompareTo(b.m_szAtlasName); }
    static int CompareSpritesByAtlsName_Down(SelectSpriteInfo a, SelectSpriteInfo b) { return b.m_szAtlasName.CompareTo(a.m_szAtlasName); }
    static int CompareSpritesBySelect(SelectSpriteInfo a, SelectSpriteInfo b)
    {
        if (a.m_bSelect != b.m_bSelect)
            return a.m_bSelect ? -1 : 1;
        if (a.m_bDelete != b.m_bDelete)
            return a.m_bDelete ? -1 : 1;
        if (a.m_bCurSelect != b.m_bCurSelect)
            return a.m_bCurSelect ? -1 : 1;
        return 0;
    }
    static int CompareSpritesBySelect_Down(SelectSpriteInfo a, SelectSpriteInfo b)
    {
        return CompareSpritesBySelect(b, a);
    }
    
    void SortByID()
    {
        m_bSortUpID = !m_bSortUpID;
        if( m_bSortUpID )
            m_SelectSprite.Sort(CompareSpritesByID);
        else
            m_SelectSprite.Sort(CompareSpritesByID_Down);
    }
    void SortBySpriteName()
    {
        m_bSortUpSpriteName = !m_bSortUpSpriteName;
        if( m_bSortUpSpriteName )
            m_SelectSprite.Sort(CompareSpritesBySpriteName);
        else
            m_SelectSprite.Sort(CompareSpritesBySpriteName_Down);
    }
    void SortByAtlasName()
    {
        m_bSortUpAtlasName = !m_bSortUpAtlasName;
        if( m_bSortUpAtlasName )
            m_SelectSprite.Sort(CompareSpritesByAtlsName);
        else
            m_SelectSprite.Sort(CompareSpritesByAtlsName_Down);
    }
    void SortBySelect()
    {
        m_bSortUpSelect = !m_bSortUpSelect;
        if( m_bSortUpSelect )
            m_SelectSprite.Sort(CompareSpritesBySelect);
        else
            m_SelectSprite.Sort(CompareSpritesBySelect_Down);
    }

    Texture2D ImportPackTextureFunc(string szPathName)
    {
        return HUDEditorTools.ImportTexture(szPathName, true, false);
    }

    int RenderEditorNumb(float fx, float fy, float fw, float fh, int nNumb)
    {
        string szNumb = nNumb.ToString();
        szNumb = GUI.TextField(new Rect(fx, fy, fw, fh), szNumb, 25);
        int.TryParse(szNumb, out nNumb);
        return nNumb;
    }

    bool RenderCheckBox(float fx, float fy, float fw, float fh, bool bSelect, string szTips)
    {
        return GUI.Toggle(new Rect(fx, fy, fw, fh), bSelect, szTips);
    }

    void RenderAtlasInfo(ref int nX, ref int nY)
    {
        nY += 5;
        float fLineGap = 3.0f;

        float fBtH = 20.0f;
        float fBtW = 80.0f;
        float fTexH = 15.0f;

        float  fx = (float)nX;
        float  fy = (float)nY;
        float fButtonX = fx;
        fButtonX += fBtW + 5.0f;
        NGUISettings.atlasName = GUI.TextField(new Rect(fButtonX, fy, 100.0f, fBtH), NGUISettings.atlasName, 25);
        fButtonX += 100.0f + 5.0f;
        bool bModifyAtlas = false;
        if (GUI.Button(new Rect(fButtonX, fy, fBtW, fBtH), "修改"))
            bModifyAtlas = true;
        fButtonX += fBtW;
        bool bRepareAtlas = false;
        if (GUI.Button(new Rect(fButtonX, fy, fBtW, fBtH), "整理"))
            bRepareAtlas = true;
        fButtonX += fBtW;
		bool bExportIcon = false;
		if (GUI.Button(new Rect(fButtonX, fy, fBtW, fBtH), "导出"))
			bExportIcon = true;
        //fy += fBtH + fLineGap;

        fButtonX += fBtW;
        bool bExportID = false;
        if (GUI.Button(new Rect(fButtonX, fy, fBtW, fBtH), "导出ID表"))
            bExportID = true;
        fButtonX += fBtW;
        fy += fBtH + fLineGap;


        // 渲染一个数字吧
        float x = fx + fBtW;
        GUI.Label(new Rect(fx, fy, fBtW, fBtH), "Padding");
        NGUISettings.atlasPadding = RenderEditorNumb(x, fy, 20, fTexH + 2, NGUISettings.atlasPadding);
        GUI.Label(new Rect(x + 25, fy, 150, fBtH), "Pixel in-between of sprites");
        fy += fTexH + fLineGap;

        // 
        GUI.Label(new Rect(fx, fy, fBtW, fBtH), "Trim Alpha");
        NGUISettings.atlasTrimming = RenderCheckBox(x, fy, 200, fBtH, NGUISettings.atlasTrimming, "Remove empty space");
        bool bAdjustAtlas = false;
        float fLeft = x + 25 + 150 + fBtW;
        if (GUI.Button(new Rect(x + 25 + 150 + fBtW, fy, fBtW, fBtH), "纠正数据"))
            bAdjustAtlas = true;

        bool bBinToTxt = false;
        if (GUI.Button(new Rect(fLeft + fBtW + 5, fy, fBtW * 2, fBtH), "二进制转换文本"))
            bBinToTxt = true;
        fLeft += fBtW * 2 + 5;

        bool bTxtToBin = false;
        if (GUI.Button(new Rect(fLeft + fBtW + 5, fy, fBtW * 2, fBtH), "文本转二进制"))
            bTxtToBin = true;
        fy += fTexH + fLineGap;

        GUI.Label(new Rect(fx, fy, fBtW, fBtH), "PMA Shader");
        NGUISettings.atlasPMA = RenderCheckBox(x, fy, 200, fBtH, NGUISettings.atlasPMA, "Pre-multiply color by alpha");
        if(NGUISettings.atlasPMA)
        {
            NGUISettings.atlasPMA = false;
            EditorUtility.DisplayDialog("警告", "这个已经不支持了，只能选择false", "确定");
        }
        fy += fTexH + fLineGap;

        GUI.Label(new Rect(fx, fy, fBtW, fBtH), "4096x4096");
        NGUISettings.allow4096 = RenderCheckBox(x, fy, 200, fBtH, NGUISettings.allow4096, "if off, limit atlases to 2048x2048");
        fy += fTexH + fLineGap;

        nY = (int)fy;
        // 渲染材质属性
        
        if (bModifyAtlas)
        {
            AtlasMng_Editor.instance.m_lpImportPackTextureFunc = ImportPackTextureFunc;
            UITexAtlas atlas = AtlasMng_Editor.instance.GetAltas(NGUISettings.atlasName);
            if (atlas != null)
            {
                atlas.pixelSize = NGUISettings.atlasPadding;
                AtlasMng_Editor.instance.SaveAltasCfg();
            }
        }
        if (bRepareAtlas)
        {
			bool  bTrimAlpha = NGUISettings.atlasTrimming;
            AtlasMng_Editor.instance.m_lpImportPackTextureFunc = ImportPackTextureFunc;
			if( AtlasMng_Editor.instance.RepareAtlas(NGUISettings.atlasName, bTrimAlpha) )
            {
                EditorUtility.DisplayDialog("提示", "整理完毕", "OK");
			}
            else
                EditorUtility.DisplayDialog("提示", "整理失败", "OK");
        }
		if( bExportIcon )
		{         
            // 导出选选择的图片
            string szDefPath = Application.dataPath + "/";
            string szPath = EditorUtility.OpenFolderPanel("打开要导出的目录", szDefPath, "");
			if( !string.IsNullOrEmpty(szPath) )
			{
				szPath = szPath + '/';
				AtlasMng_Editor.instance.m_lpImportPackTextureFunc = ImportPackTextureFunc;
				if( AtlasMng_Editor.instance.ExportSpriteIcon(NGUISettings.atlasName, szPath, ExportTGA) )
				{
					EditorUtility.DisplayDialog("提示", "导出完毕", "OK");
				}
				else
				{
					EditorUtility.DisplayDialog("提示", "导出失败", "OK");
				}
			}
        }
        if(bExportID)
        {
            string szDefPath = Application.dataPath + "/";
            string[] filters = { ".txt", ".*" };
            string szPath = EditorUtility.OpenFolderPanel("打开要导出的目录", szDefPath, "");
            //string szPathName = EditorUtility.SaveFilePanel("导出ID文件", szDefPath, "gif_id.txt", "txt");
            if (!string.IsNullOrEmpty(szPath))
            {
                szPath = szPath + '/';
                string szPathName = szPath + "gif_id.txt";
                ExportSelectID(szPathName);
            }
        }
        if (bAdjustAtlas)
        {
            if (AtlasMng_Editor.instance.AdjustAtlasData())
            {
                EditorUtility.DisplayDialog("提示", "纠正完毕，已保存", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "没有需要纠正的数据", "OK");
            }
        }
        if (bBinToTxt)
        {
            AtlasMng_Editor atlasMng = new AtlasMng_Editor();
            atlasMng.LoadAndSaveAtlas(false);
        }
        if (bTxtToBin)
        {
            AtlasMng_Editor atlasMng = new AtlasMng_Editor();
            atlasMng.LoadAndSaveAtlas(true);
        }
    }

    void MakeLODTextureByPath(List<string> texList)
    {
        string szAssetPath = "Assets/MyAssets/Raw/texture/";
        List<string> validList = new List<string>();
        foreach (string  szAssetPathName in texList)
        {
            string file_name = System.IO.Path.GetFileNameWithoutExtension(szAssetPathName);
            int nLen = file_name.Length;
            if(nLen > 2 && file_name[nLen - 2] == '_')
            {
                if (file_name[nLen - 1] == 'L'
                    || file_name[nLen - 1] == 'l')
                    continue;
            }
            validList.Add(file_name);
            AtlasMng_Editor.instance.MakeLODTexture(szAssetPath, file_name, false, 512);
        }
        AssetDatabase.Refresh();
        foreach (string file_name in validList)
        {
            string szLodAtlasPathName = szAssetPath + file_name + "_L.png";
            AtlasMng_Editor.instance.ChangeTextureFormat(szLodAtlasPathName);
        }
    }
    
    void ExportSelectID(string szPathName)
    {
        if (m_SelectSprite == null)
            return;
        CSerialize ar = new CSerialize(SerializeType.write, szPathName);
        ar.PushTextString("ID\tName");
        for(int i = 0; i< m_SelectSprite.Count; ++i)
        {
            SelectSpriteInfo spInfo = m_SelectSprite[i];
            if (spInfo == null)
                continue;
            UISpriteInfo sp = spInfo.m_sprite;
            ar.PushTextString(string.Format("\r\n{0}\t{1}", sp.m_nNameID, sp.name));
        }
        ar.Close();

        szPathName = szPathName.Replace('/', '\\');
        System.Diagnostics.Process.Start("explorer.exe", szPathName);
    }

    void RenderAllSpriteInfo(ref int nX, ref int nY)
    {
        float fx = (float)nX;
        float fy = (float)nY;
        float fLineGap = 3.0f;

        float fBtH = 20.0f;
        float fBtW = 100.0f;
        float fTexH = 15.0f;

        bool bUpdate = false;
        // 得到当前选中的对象
        Color backColor = GUI.backgroundColor;
        float fButtonX = fx;
        if (m_nSelectCount > 0)
		{
			GUI.backgroundColor = Color.green;
            if (GUI.Button(new Rect(fButtonX, fy, 100, fBtH), "添加或更新"))
                bUpdate = true;
			GUI.backgroundColor = Color.white;
		}
		else
		{
            GUI.Button(new Rect(fButtonX, fy, 100, fBtH), "请先选中图片");
		}
        fButtonX += 100;
        if (GUI.Button(new Rect(fButtonX, fy, 80, fBtH), "ID排序"))
            SortByID();
        fButtonX += 80;
        if (GUI.Button(new Rect(fButtonX, fy, 80, fBtH), "Name排序"))
            SortBySpriteName();
        fButtonX += 80;
        if (GUI.Button(new Rect(fButtonX, fy, 80, fBtH), "材质名排序"))
            SortByAtlasName();
        fButtonX += 80;
        if (GUI.Button(new Rect(fButtonX, fy, 80, fBtH), "选择排序"))
            SortBySelect();
        fButtonX += 80;

        GUI.Label(new Rect(fButtonX, fy - fBtH - fLineGap, 200, fBtH), "我要搜索(材质名&精灵名)");
        string szNewLockSearchName = GUI.TextField(new Rect(fButtonX, fy, 200, fBtH), m_szLockSearchName, 25);
        if (szNewLockSearchName != m_szLockSearchName)
        {
            m_szLockSearchName = szNewLockSearchName;
            RefresyByLockName();
        }

        fButtonX += 210;
        UITexAtlas atlas = CAtlasMng.instance.GetAltas(NGUISettings.atlasName);
        bool bOldLOD = NGUISettings.canLOD;
        if (atlas != null)
            bOldLOD = atlas.IsCanLOD();
        bool bIsCanLOD = GUI.Toggle(new Rect(fButtonX, fy, 100, fBtH), bOldLOD, "图集允许LOD");
        if(bIsCanLOD != bOldLOD)
        {
            NGUISettings.canLOD = bIsCanLOD;
            if(atlas != null)
            {
                atlas.SetLODFlag(bIsCanLOD);
                CAtlasMng.instance.SaveAltasCfg();
            }
        }

        fy += fBtH + fLineGap;

        // m_szLockSearchName

        // 开始渲染当前材质的对象了
        Rect rc = position;
        int nScreenW = Screen.width;
        float fListH = rc.height - fy;
        float fWindowW = rc.width;

        int  nSelect = -1;
        bool bDelete = false;

        SelectSpriteInfo select = null;
        int nSpriteCount = m_SelectSprite.Count;
        if (nSpriteCount > 0)
        {
            float fSize = fListH;
            float topValue = 0.0f;
            float bottomValue = nSpriteCount *(fTexH + fLineGap) + fTexH;

            Rect rcView = new Rect(fx, fy, fWindowW, fListH);
            Rect rcRange = new Rect(fx, fy, fWindowW + fx, bottomValue - m_fScrollPos + fTexH);
            m_vScrollPos = GUI.BeginScrollView(rcView, m_vScrollPos, rcRange, false, false);

            //m_fScrollPos = GUI.VerticalScrollbar(new Rect(fWindowW - 30, fy, 30, fListH), m_fScrollPos, fSize, topValue, bottomValue);
            m_fScrollPos = m_vScrollPos.y;

            int nStartShow = (int)(m_fScrollPos) / (int)(fTexH + fLineGap);
            float fSpriteWidth = fWindowW * 0.75f;
            if (nStartShow < 0)
                nStartShow = 0;
            float x = fx;
            float y = fy;
            float fTempY = fy + m_fScrollPos;
            for (int i = nStartShow; i < nSpriteCount; ++i)
            {
                SelectSpriteInfo sp = m_SelectSprite[i];
                y = fTempY - m_fScrollPos;
                float fRight = GetButtonLeft(i, fWindowW - 70);
                GUI.backgroundColor = GetSpriteButtonColor(i);
                if (GUI.Button(new Rect(x, y, fRight, fTexH), GUIContent.none))
                {
                    m_szSelectSpriteName = sp.m_szSpriteName;
                    sp.m_bCurSelect = true;// !sp.m_bCurSelect;
                    nSelect = i;
                    select = sp;
                    m_pSelectSprite = sp;
                    if (!string.IsNullOrEmpty(sp.m_szAssetsName))
                    {
                        sp.m_nNextAssetsType++;
                        if (sp.m_nNextAssetsType > 1)
                            sp.m_nNextAssetsType = 0;
                        // 输入名字 -- 文件名字，两个做转换
                        if (sp.m_nNextAssetsType == 0)
                        {
                            sp.m_szAtlasName = NGUISettings.atlasName.Clone() as string;
                            if (string.IsNullOrEmpty(sp.m_szAtlasName))
                            {
                                sp.m_szAtlasName = sp.m_szSpriteName;
                                NGUISettings.atlasName = sp.m_szAtlasName.Clone() as string;
                            }
                        }
                        else
                        {
                            sp.m_szAtlasName = sp.m_szSpriteName;
                        }
                    }
                }
                GUI.backgroundColor = Color.white;

                //GUI.Label(new Rect(x, y, 30, fTexH), sp.m_nIndex.ToString());
                int nSpriteID = sp.m_sprite != null ? sp.m_sprite.m_nNameID : 0;
                GUI.Label(new Rect(x, y, 30, fTexH), nSpriteID.ToString());
                GUI.Label(new Rect(x + 30, y, fSpriteWidth, fTexH), sp.m_szSpriteName);
                GUI.Label(new Rect(x + fWindowW * 0.5f - 20, y, fWindowW * 0.25f + 40, fTexH), sp.m_szAtlasName);

                RenderSpriteButton(i, fWindowW - 70, y, ref bDelete);

                fTempY += fTexH + fLineGap;
            }
            GUI.EndScrollView();
        }
        else
        {
        }

        // 处理消息逻辑
        if (bDelete || bUpdate || select != null )
        {
            if (select != null)
            {
                UITexAtlas selectAtlas = CAtlasMng.instance.GetAltasBySpriteName(m_szSelectSpriteName);
                if (selectAtlas != null)
                {
                    NGUISettings.atlasPadding = selectAtlas.pixelSize;
                    NGUISettings.atlasTrimming = false;
                    NGUISettings.atlasPMA = selectAtlas.premultipliedAlpha;
                    NGUISettings.atlasName = selectAtlas.m_szAtlasName.Clone() as string;
                }
            }
            OnSpriteOperator(bDelete, bUpdate, select);
        }
    }
    void OnSpriteOperator(bool delete, bool update, SelectSpriteInfo select)
    {
        int nSpriteCount = m_SelectSprite.Count;

        if (delete)
        {
            // 有要删除对象
            List<string> nameList = new List<string>();
            for (int i = 0; i < nSpriteCount; ++i)
            {
                if (m_SelectSprite[i].m_bDelete)
                    nameList.Add(m_SelectSprite[i].m_szSpriteName);
            }
            AtlasMng_Editor.instance.m_lpImportPackTextureFunc = ImportPackTextureFunc;
            CAtlasMng.instance.DeleteSprite(nameList);
			RefrshUIObject(nameList);

            RefreshSelectObject();
        }
        else if (update)
        {
            // 有要更新的对象
            List<SUpdateTexInfo> spriteNameList = new List<SUpdateTexInfo>();
			List<string> nameList = new List<string>();
            for (int i = 0; i < nSpriteCount; ++i)
            {
                if (m_SelectSprite[i].m_bSelect)
                {
                    SelectSpriteInfo sp = m_SelectSprite[i];

                    SUpdateTexInfo node = new SUpdateTexInfo();
                    node.m_szAssetsName = sp.m_szAssetsName;
                    node.m_szAtlasName = sp.m_szAtlasName;
                    node.m_szSpriteName = sp.m_szSpriteName;
					spriteNameList.Add(node);
					nameList.Add(sp.m_szSpriteName);
                }
            }
			bool  bTrimAlpha = NGUISettings.atlasTrimming;
            AtlasMng_Editor.instance.m_lpImportPackTextureFunc = ImportPackTextureFunc;
            AtlasMng_Editor.instance.AddOrUpdateSelectSprite(spriteNameList, bTrimAlpha, NGUISettings.atlasPMA, NGUISettings.canLOD);
			RefrshUIObject(nameList);
            RefreshSelectObject();
        }

        if (select != null)
        {
            for (int i = 0; i < nSpriteCount; ++i)
            {
                if (select.m_nIndex != m_SelectSprite[i].m_nIndex)
                {
                    m_SelectSprite[i].m_bCurSelect = false;
                }
            }

            if (select.m_sprite != null)
            {
                // 创建一个对象
                OnSelectSprite(select.m_sprite.name);
            }
            else
            {
                Selection.activeObject = select.m_selectTex;
            }
        }
    }

    float GetButtonLeft(int nIndex, float fx)
    {
        float fTexH = 15.0f;
        SelectSpriteInfo sp = m_SelectSprite[nIndex];
        int nValueType = 0;
        if (sp.m_bSelect)
        {
            nValueType = 2;
            if (sp.m_sprite != null)
            {
                nValueType = 1;
            }
        }
        if (nValueType == 2)
        {
            return fx;
        }
        else if (nValueType == 1)
        {
            return fx - 15.0f;
        }
        else
        {
            if (sp.m_bDelete)
            {
                return fx - 60.0f;
            }
            else
            {
                return fx;
            }
        }
        return fx;
    }

    Color GetSpriteButtonColor(int nIndex)
    {
        SelectSpriteInfo sp = m_SelectSprite[nIndex];
        int nValueType = 0;
        if (sp.m_bSelect)
        {
            nValueType = 2;
            if (sp.m_sprite != null)
            {
                nValueType = 1;
            }
        }
        if (nValueType == 2)
        {
            return Color.green;
        }
        else if (nValueType == 1)
        {
            return Color.cyan;
        }
        else
        {
            if (sp.m_bDelete)
            {
                return Color.red;
            }
            else
            {
                if (sp.m_bCurSelect)
                    return Color.yellow;
                return Color.white;
            }
        }
        return Color.white;
    }

    // 渲染按纽操作
    void RenderSpriteButton(int nIndex, float fx, float fy, ref bool delete)
    {
        float fTexH = 15.0f;
        SelectSpriteInfo sp = m_SelectSprite[nIndex];
        int nValueType = 0;
        if (sp.m_bSelect)
        {
            nValueType = 2;
            if (sp.m_sprite != null)
            {
                nValueType = 1;
            }
        }
        if (nValueType == 2)
		{
            GUI.color = Color.green;
            GUI.Label(new Rect(fx, fy, 30.0f, fTexH), "Add");
			GUI.color = Color.white;
		}
        else if (nValueType == 1)
        {
            GUI.color = Color.cyan;
            GUI.Label(new Rect(fx - 15.0f, fy, 45.0f, fTexH), "Update");
            GUI.color = Color.white;
        }
        else
        {
            if (sp.m_bDelete)
            {
                GUI.backgroundColor = Color.red;

                if (GUI.Button(new Rect(fx - 60.0f, fy, 60.0f, fTexH), "Delete"))
                {
                    sp.m_bDelete = true;
                    delete = true;
                }
                GUI.backgroundColor = Color.green;
                if(GUI.Button(new Rect(fx, fy, 30, fTexH), "X"))
                {
                    sp.m_bDelete = false;
                    delete = false;
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                // If we have not yet selected a sprite for deletion, show a small "X" button
                if( GUI.Button(new Rect(fx, fy, 30, fTexH), "X") )
                    sp.m_bDelete = !sp.m_bDelete;
            }
        }
    }
           
	/// <summary>
	/// Draw the UI for this tool.
	/// </summary>
    /// 
	void OnGUI ()
    {
        if (!m_bInit)
        {
            m_bInit = true;
            RefreshSelectObject();
        }
        int nX = 0, nY = 0;
        RenderAtlasInfo(ref nX, ref nY);
        RenderAllSpriteInfo(ref nX, ref nY);
	}
}
