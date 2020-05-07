using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Object = UnityEngine.Object;
using System.Collections;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;

///////////////////////////////////////////////////////////
//
//  Written by              ：laishikai
//  Copyright(C)            ：成都博瑞梦工厂
//  ------------------------------------------------------
//  功能描述                ：编辑器模式下图集管理代码
//
///////////////////////////////////////////////////////////

// 这个只是在编辑器模式下
public struct SelecctTexInfo
{
    public Texture2D m_tex;        // 纹理对象
    public string m_szSpriteName;        // 节点的名字(文件名，没有扩展名)
    public string m_szAssetsName;  // Assets相对文件目录名
    public string m_szAtlasName;   // 所在的材质
    public UISpriteInfo m_sprite;  // 精灵信息
};

public struct SUpdateTexInfo
{
    public string m_szAssetsName;   // 资源目录
    public string m_szSpriteName;   // 精灵名字
    public string m_szAtlasName;    // 材质名字
};

public class UpdateTexNameList
{
    public List<SelecctTexInfo> nameList = new List<SelecctTexInfo>();
    public void push_back(SelecctTexInfo node)
    {
        nameList.Add(node);
    }
};

#if UNITY_EDITOR


public class CPngHelp
{
    [DllImport("MakeMD5", CallingConvention = CallingConvention.Cdecl)]
    static extern int MakeMD5(string szFileName, StringBuilder szOutMD5);
    //[DllImport("MakeMD5.dll", EntryPoint = "GetFileEditorKey")]
    [DllImport("MakeMD5", CallingConvention = CallingConvention.Cdecl)]
    static extern int GetFileEditorKey(string szFileName, StringBuilder szOutMD5);
    //[DllImport("MakeMD5.dll", EntryPoint = "ReadPng")]
    [DllImport("MakeMD5", CallingConvention = CallingConvention.Cdecl)]
    static extern int ReadPng(string szFileName, byte []pOutData, int nOutBuffSize);

    public static bool GetMD5Key(string szFileName, ref string szMD5Key)
    {
        StringBuilder szOutMD5 = new StringBuilder(40);
        if (MakeMD5(szFileName, szOutMD5) == 1)
        {
            szMD5Key = szOutMD5.ToString();
            return true;
        }
        return false;
    }
    public static bool GetEditKey(string szFileName, ref string szKey)
    {
        StringBuilder szOutMD5 = new StringBuilder(40);
        if (GetFileEditorKey(szFileName, szOutMD5) == 1)
        {
            szKey = szOutMD5.ToString();
            return true;
        }
        return false;
    }
    public static bool ReadPng(string szFileName, out int nWidth, out int nHeight, out Color32 []pixels )
    {
        nWidth = 0;
        nHeight = 0;
        pixels = null;
        int nOutSize = 4096 * 4096 * 4 + 100;
        byte  []data = new byte[nOutSize];
        int nSize = 0;
        try
        {
            nSize = ReadPng(szFileName, data, nOutSize);
        }
        catch(Exception e)
        {
            Debug.LogException(e);
        }
        if(nSize > 0)
        {
            int nOffset = 0;
            nWidth = System.BitConverter.ToInt32(data, nOffset); nOffset += 4;
            nHeight = System.BitConverter.ToInt32(data, nOffset); nOffset += 4;
            if (nWidth < 0 || nWidth > 4096 || nHeight < 0 || nHeight > 4096)
                return false;
            int nLen = nWidth * nHeight;
            if (nLen * 4 > nOutSize)
                return false;
            pixels = new Color32[nLen];
            for (int i = 0; i< nLen; ++i)
            {
                pixels[i].b = data[nOffset++];
                pixels[i].g = data[nOffset++];
                pixels[i].r = data[nOffset++];
                pixels[i].a = data[nOffset++];
            }
            return true;
        }
        else
        {
            return false;
        }
    }
    public static Texture2D  ReadPng(string szPathname)
    {
        int nWidth = 0;
        int nHeight = 0;
        Color32[] pixels = null;
        if(ReadPng(szPathname, out nWidth, out nHeight, out pixels))
        {
            Texture2D tex = new Texture2D(nWidth, nHeight, TextureFormat.ARGB32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
        string szDataPath = Application.dataPath;
        string szAssetsPathName = szPathname.Substring(szDataPath.Length - 6);
        return ReadPngByUnity(szAssetsPathName);
    }
    public static Texture2D ReadAssetPng(string szAssetPathName)
    {
        string szDataPath = Application.dataPath;
        szDataPath = szDataPath.Substring(0, szDataPath.Length - 6);
        return ReadPng(szDataPath + szAssetPathName);
    }
    static string m_szCurReadTexturePath = string.Empty;

    public static string curReadTextureAssetsPathName
    {
        get { return m_szCurReadTexturePath; }
    }

    static string GetMetaPathname(string szAssetsPathName)
    {
        string szDataPath = Application.dataPath;
        szDataPath = szDataPath.Substring(0, szDataPath.Length - 6);
        return szDataPath + szAssetsPathName + ".meta";
    }
    public static Texture2D  ReadPngByUnity(string szAssetsPathName)
    {
        if (string.IsNullOrEmpty(szAssetsPathName)) return null;
        TextureImporter ti = AssetImporter.GetAtPath(szAssetsPathName) as TextureImporter;
        if (ti == null) return null;

        m_szCurReadTexturePath = szAssetsPathName;

        string szPlatName = "Standalone";
#if UNITY_ANDROID
        szPlatName = "Android";
#elif UNITY_IPHONE
        szPlatName = "iPhone";
#endif

        TextureImporterPlatformSettings oldSettings = ti.GetPlatformTextureSettings(szPlatName);
        TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
        oldSettings.CopyTo(settings);

        //TextureImporterPlatformSettings  androidSet = ti.GetPlatformTextureSettings("Android");
        //TextureImporterPlatformSettings iosSet = ti.GetPlatformTextureSettings("iPhone");
        //TextureImporterPlatformSettings pcSet = ti.GetPlatformTextureSettings("Standalone");
        TextureImporterPlatformSettings defaultSet = ti.GetPlatformTextureSettings("Default");

        bool mipmapEnabled = ti.mipmapEnabled;
        bool isReadable = ti.isReadable;
        FilterMode filterMode = ti.filterMode;
        TextureWrapMode wrapMode = ti.wrapMode;
        TextureImporterNPOTScale npotScale = ti.npotScale;
        TextureImporterType textureType = ti.textureType;

        string szMetaPathName = GetMetaPathname(szAssetsPathName);
        //byte[] metaFileData = File.ReadAllBytes(szMetaPathName);

        bool bDirty = false;
        if (ti.mipmapEnabled ||
            !ti.isReadable ||
            ti.npotScale != TextureImporterNPOTScale.None ||
            ti.textureType != TextureImporterType.GUI ||
            settings.format != TextureImporterFormat.ARGB32 ||
            settings.textureCompression != TextureImporterCompression.Uncompressed)
        {
            ti.mipmapEnabled = false;
            ti.isReadable = true;
            ti.textureType = TextureImporterType.GUI;
            ti.npotScale = TextureImporterNPOTScale.None;

            settings.maxTextureSize = 4096;
            settings.format = TextureImporterFormat.ARGB32;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            settings.overridden = true;

            ti.SetPlatformTextureSettings(settings);

            //ti.SetPlatformTextureSettings(szPlatName, 2048, TextureImporterFormat.ARGB32);

            //ti.SetPlatformTextureSettings("Android", 2048, TextureImporterFormat.ARGB32);
            //ti.SetPlatformTextureSettings("iPhone", 2048, TextureImporterFormat.ARGB32);
            //ti.SetPlatformTextureSettings("Standalone", 2048, TextureImporterFormat.ARGB32);

            //AssetDatabase.ImportAsset(szAssetsPathName, ImportAssetOptions.ForceUpdate);
            ti.SaveAndReimport();
            AssetDatabase.Refresh();
            bDirty = true;
        }
        Texture2D tex = AssetDatabase.LoadAssetAtPath(szAssetsPathName, typeof(Texture2D)) as Texture2D;

        if (tex != null)
        {
            Texture2D texNew = new Texture2D(tex.width, tex.height, tex.format, tex.mipmapCount > 1);
            Color[] pixel = tex.GetPixels();
            texNew.SetPixels(pixel);
            texNew.Apply();
            tex = texNew;
        }

        m_szCurReadTexturePath = string.Empty;

        if (bDirty)
        {
            ti.mipmapEnabled = mipmapEnabled;
            ti.isReadable = isReadable;
            ti.filterMode = filterMode;
            ti.wrapMode = wrapMode;
            ti.npotScale = npotScale;
            ti.textureType = textureType;

            ti.SetPlatformTextureSettings(oldSettings);
            ti.SaveAndReimport();
            AssetDatabase.ImportAsset(szAssetsPathName, ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
            //File.WriteAllBytes(szMetaPathName, metaFileData);
            AssetDatabase.Refresh();
        }
        return tex;
    }
};


public class AtlasMng_Editor : CAtlasMng
{
    public delegate Texture2D LPImportPackTextureFunc(string szPathName);

    public LPImportPackTextureFunc m_lpImportPackTextureFunc;
    protected bool m_bInitEditMode = false;
    protected bool m_bInEditMode = true;
    int   m_nPlayMode = 0; // 0,没有初始化; 1,运行中; 2,编辑器状态
    DateTime m_tLastWriteTime; // assets_all.txt 的最后修改时间

    Texture2D m_pLastPixelTex;
    int m_nLastAtlasID = -1;

    static AtlasMng_Editor s_pAtlasMng_Editor;
    static public new AtlasMng_Editor instance
    {
        get
        {
            if (s_pAtlasMng_Editor == null)
            {
                CAtlasMng pMng = CAtlasMng.instance;
                s_pAtlasMng_Editor = pMng as AtlasMng_Editor;
            }
            if (s_pAtlasMng_Editor != null )
                s_pAtlasMng_Editor.AutoInit();
            return s_pAtlasMng_Editor;
        }
    }
    
    struct RectInt
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
        static RectInt s_zero;
        //public RectInt()
        //{
        //    left = top = right = bottom = 0;
        //}
        public RectInt(int nLeft, int nTop, int nRight, int nBottom)
        {
            left = nLeft;
            top = nTop;
            right = nRight;
            bottom = nBottom;
        }
        public RectInt(Rect rc)
        {
            left   = (int)rc.xMin;
            top    = (int)rc.yMin;
            right  = (int)(rc.xMax + 0.5f);
            bottom = (int)(rc.yMax + 0.5f);
        }
        static RectInt zero
        {
            get 
            {
                if (s_zero == null)
                    s_zero = new RectInt(0, 0, 0, 0);
                return s_zero;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(RectInt))
            {
                return Equals((RectInt)obj);
            }
            return false;
        }
        public bool Equals(RectInt other)
        {
            return left == other.left && top == other.top && right == other.right && bottom == other.bottom;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator !=(RectInt lhs, RectInt rhs)
        {
            return lhs.left != rhs.left || lhs.top != rhs.top || lhs.right != rhs.right || lhs.bottom != rhs.bottom;
        }
        public static bool operator ==(RectInt lhs, RectInt rhs)
        {
            return lhs.left == rhs.left && lhs.top == rhs.top && lhs.right == rhs.right && lhs.bottom == rhs.bottom;
        }
        // 功能：左右交换，上下交换
        public void swap()
        {
            int nTemp = left; left = right; right = nTemp;
            nTemp = top; top = bottom; bottom = nTemp;
        }
        public void SetRect(int nLeft, int nTop, int nRight, int nBottom)
        {
            left = nLeft;
            top = nTop;
            right = nRight;
            bottom = nBottom;
        }
        public void SetRect(Rect rc)
        {
            left   = (int)rc.xMin;
            top    = (int)rc.yMin;
            right  = (int)(rc.xMax + 0.5f);
            bottom = (int)(rc.yMax + 0.5f);
        }

        // 功能：合并一个坐标点，扩大四边形
        public void unitPoint(int x, int y)
        {
            if (left > x)
                left = x;
            if (right < x + 1)
                right = x + 1;
            if (top > y)
                top = y;
            if (bottom < y + 1)
                bottom = y + 1;
        }
        // 功能：求交
        static public RectInt clipRect(RectInt a, RectInt b)
        {
            RectInt r = RectInt.zero;
            r.left   = a.left < b.left ? b.left : a.left;
            r.top    = a.top < b.top ? b.top : a.top;
            r.right  = a.right < b.right ? a.right : b.right;
            r.bottom = a.bottom < b.bottom ? a.bottom : b.bottom;
            if (r.right < r.left || r.bottom < r.top)
            {
                r.left = r.right = r.top = r.bottom = 0;
            }
            return r;
        }
        public int width
        {
            get{ return right - left; }
            set { right = left + value; }
        }
        public int height
        {
            get { return bottom - top; }
            set { bottom = top + value; }
        }
    };

    public override bool IsEditorMode()
    {
        if (!m_bInitEditMode)
        {
            m_bInitEditMode = true;
            string szCfgPathName = Application.dataPath;
            szCfgPathName = szCfgPathName.Substring(0, szCfgPathName.Length - 6) + "android_mode.txt";
            if (File.Exists(szCfgPathName))
            {
                m_bInEditMode = false;
            }
            else
            {
                szCfgPathName = Application.dataPath + "/Atlas/android_mode.txt";
                m_bInEditMode = !File.Exists(szCfgPathName);
            }
        }
        return m_bInEditMode;
    }

    UITexAtlas  CopyTexAtlas(UITexAtlas  atlas)
    {
        UITexAtlas  newAtlas = new UITexAtlas();
        newAtlas.m_szAtlasName = new string(atlas.m_szAtlasName.ToCharArray());
        newAtlas.m_szTexName = new string(atlas.m_szTexName.ToCharArray());
        newAtlas.m_szShaderName = new string(atlas.m_szShaderName.ToCharArray());
        newAtlas.SetTextureSizeByMaterial(atlas.m_material);
        newAtlas.m_nAtlasID = atlas.m_nAtlasID;
        
        newAtlas.m_nRef = atlas.m_nRef;
        newAtlas.m_nSpriteNumb = atlas.m_nSpriteNumb;
        newAtlas.m_nVersion = atlas.m_nVersion;
        newAtlas.m_bDirty = atlas.m_bDirty;
        newAtlas.m_fReleaseTime = atlas.m_fReleaseTime;
        newAtlas.m_bLoading = atlas.m_bLoading;
        newAtlas.m_fLoadingTime = atlas.m_fLoadingTime;
        
        return newAtlas;
    }

    // 功能：重新加载assets_all.txt
    // 这个功能似乎有BUG，先暂时不要调用了
    void Reload()
    {
        string szCfgPathName = Application.dataPath + "/Atlas/assets_all.txt";

        if (File.Exists(szCfgPathName))
        {
            CMyArray<UITexAtlas>     oldAtlasPtr = new CMyArray<UITexAtlas>();
            oldAtlasPtr.reserve(m_AtlasPtr.size());
            for( int i = 0; i<m_AtlasPtr.size(); ++i )
            {
                UITexAtlas  oldAtlas = m_AtlasPtr[i];
                if( oldAtlas != null )
                {
                    UITexAtlas  atlas = CopyTexAtlas(oldAtlas);
                    oldAtlasPtr.push_back(atlas);
                }
            }
            SerializeText ar = new SerializeText(SerializeType.read, szCfgPathName);
            SerializeToTxt(ar);
            MakeSpriteAtlasID();

            // 纠正吧
            for (int i = 0; i < oldAtlasPtr.size(); ++i)
            {
                UITexAtlas  oldAtlas = oldAtlasPtr[i];
                UITexAtlas  newAtlas = GetAltas(oldAtlas.m_szAtlasName);
                if (newAtlas != null)
                {
                    newAtlas.m_nRef = oldAtlas.m_nRef;
                    newAtlas.SetTextureSizeByMaterial(oldAtlas.m_material);
                }
            }
        }
    }

    public bool IsCanSave()
    {
        if (!IsEditorMode())
            return false;

        string szCfgPathName = Application.dataPath + "/Atlas/assets_all.txt";
        if (File.Exists(szCfgPathName))
        {
            SerializeText ar = new SerializeText(SerializeType.read, szCfgPathName);

            int nFileVersion = m_nFileVersion;
            byte yVersion = 3;
            ar.ReadWriteValue("Version", ref yVersion);
            if (yVersion > 2)
                ar.ReadWriteValue("SaveCount", ref nFileVersion);
            bool bDirty = nFileVersion != m_nFileVersion;

            if (bDirty)
            {
                //if (EditorUtility.DisplayDialog("警告", "你的材质有更新，选择YES重新加载。\n请确认Atlas目录资源全部是最新的。\n为保证图集的正确性，请重启Unity。\n", "YES", "Cancel"))
                //{
                    //Reload();
                    //return true;
                //}
                EditorUtility.DisplayDialog("警告", "你的材质有更新。\n请确认Atlas目录资源全部是最新的。\n为保证图集的正确性，请重启Unity。\n记得撤消你本地的修改，再拉取最新的资源。", "记得重启噢");
            }
            return !bDirty;
        }
        return true;
    }
    
    // 功能：下载完成后的事件
    public override void OnAfterDown()
    {
        if (!IsEditorMode())
        {
            base.OnAfterDown();
        }
    }

    // 功能：加载并重新保存材质
    public void LoadAndSaveAtlas(bool bLoadFromTxt)
    {
        if (bLoadFromTxt)
        {
            string szCfgPathName = Application.dataPath + "/Atlas/assets_all.txt";

            if (File.Exists(szCfgPathName))
            {
                SerializeText ar = new SerializeText(SerializeType.read, szCfgPathName);
                SerializeToTxt(ar);
                MakeSpriteAtlasID();
                SaveAltasCfg();
            }
        }
        else
        {
            string szCfgPathName = Application.dataPath + "/Atlas/assets_all.bytes";
            if (File.Exists(szCfgPathName))
            {
                CSerialize ar = new CSerialize(SerializeType.read, szCfgPathName);
                Serialize(ar);
                MakeSpriteAtlasID();
                SaveAltasCfg();
            }
        }
    }

    // 功能：生成所有的低分辩率贴图
    public void MakeAllLODTexture()
    {
        Dictionary<string, UITexAtlas>.Enumerator itTexture = m_TexAtlas.GetEnumerator();
        string szAssetPath = "Assets/Atlas/";
        while (itTexture.MoveNext())
        {
            string szAtlasName = itTexture.Current.Key;
            if(itTexture.Current.Value.IsCanLOD())
            {
                MakeLODTexture(szAssetPath, szAtlasName, false);
                MakeLODTexture(szAssetPath, szAtlasName + "_alpha", false);
            }
        }
        AssetDatabase.Refresh();
        itTexture = m_TexAtlas.GetEnumerator();
        while (itTexture.MoveNext())
        {
            string szAtlasName = itTexture.Current.Key;
            if (itTexture.Current.Value.IsCanLOD())
            {
                string szLodAtlasPathName = szAssetPath + szAtlasName + "_L.png";
                ChangeTextureFormat(szLodAtlasPathName);

                szAtlasName = itTexture.Current.Key + "_alpha";
                szLodAtlasPathName = szAssetPath + szAtlasName + "_L.png";
                ChangeTextureFormat(szLodAtlasPathName);
            }
        }
    }

    int  GetTextureExSize(int MaxSize)
    {
        int nS = 1;
        for(; nS * 2 < MaxSize; nS *= 2){ }
        return nS;
    }

    public void MakeLODTexture(string szAssetPath, string szAtlasName, bool bChangeFormat = true, int nMinSize = 256)
    {
        string szAtlasPathName = szAssetPath + szAtlasName + ".png";
        Texture2D atlasTex = LoadCanWriteTextureByAssetsName(szAtlasPathName);
        int nWidth = atlasTex.width;
        int nHeight = atlasTex.height;
        Color[] PixelOld = atlasTex.GetPixels();
        if (nWidth <= nMinSize && nHeight <= nMinSize)
            return;

        //int nLodW = nWidth > nMinSize ? GetTextureExSize(nWidth): nWidth;
        //int nLodH = nHeight > nMinSize ? GetTextureExSize(nHeight) : nHeight;        
        int nLodW = nWidth > nMinSize ? nWidth/2: nWidth;
        int nLodH = nHeight > nMinSize ? nHeight/2 : nHeight;
        int nScaleW = nWidth / nLodW;
        int nScaleH = nHeight / nLodH;
        Texture2D texLOD = new Texture2D(nLodW, nLodH);
        Color[] PixelNew = new Color[nLodW * nLodH];
        int nSrcI = 0;
        int nDesI = 0;
        for (int nRow = 0; nRow < nLodH; ++nRow)
        {
            for (int nCol = 0; nCol < nLodW; ++nCol, ++nDesI)
            {
                //nSrcI = nRow * nWidth * nHeight / nLodH + nCol * nWidth / nLodW;
                nSrcI = nRow * nWidth * nScaleH + nCol * nScaleW;
                PixelNew[nDesI] = PixelOld[nSrcI];
            }
        }
        texLOD.SetPixels(PixelNew);
        texLOD.Apply();

        string szLodAtlasPathName = szAssetPath + szAtlasName + "_L.png";

        bool bFindFile = false;
        if (System.IO.File.Exists(szLodAtlasPathName))
        {
            bFindFile = true;
            System.IO.FileAttributes newPathAttrs = System.IO.File.GetAttributes(szLodAtlasPathName);
            newPathAttrs &= ~System.IO.FileAttributes.ReadOnly;
            System.IO.File.SetAttributes(szLodAtlasPathName, newPathAttrs);
        }
        byte[] bytes = texLOD.EncodeToPNG();
        if (bytes == null)
        {
            return;
        }
        System.IO.File.WriteAllBytes(szLodAtlasPathName, bytes);
        bytes = null;
        if (!bFindFile && bChangeFormat)
        {
            AssetDatabase.Refresh();
            ChangeTextureFormat(szLodAtlasPathName);
        }
    }    

    // 功能：初始化配置吧
    public override void InitAltasCfg()
    {
        if (!IsEditorMode())
        {
            base.InitAltasCfg();
            return;
        }
        string szCfgPathName = Application.dataPath + "/Atlas/assets_all.txt";
        if (File.Exists(szCfgPathName))
        {
            SerializeText ar = new SerializeText(SerializeType.read, szCfgPathName);
            SerializeToTxt(ar);
            ar.Close();
            m_tLastWriteTime = File.GetLastWriteTime(szCfgPathName);
        }
        n_nObjNumb = m_AllSprite.Count + m_TexAtlas.Count + 10;
        if (MakeSpriteAtlasID())
        {
            SaveAltasCfg();
        }
        int nAtlasNumb = m_AtlasPtr.size();
        int nDiryty = 0;
        for (int i = 0; i < nAtlasNumb; ++i)
        {
            UITexAtlas atlas = m_AtlasPtr[i];
            if (atlas != null && string.IsNullOrEmpty(atlas.m_szShaderName))
            {
                if (AdjustAtlasShaderName(atlas))
                {
                    ++nDiryty;
                }
            }
        }
        if (nDiryty > 0)
        {
            SaveAltasCfg();
        }        
    }

    bool AdjustAtlasShaderName(UITexAtlas atlas)
    {
        string szAssetsPath = "Assets/Atlas/" + atlas.m_szAtlasName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath(szAssetsPath, typeof(Material)) as Material;
        string szShaderName = atlas.m_szShaderName;
        if( mat != null && mat.shader != null )
            szShaderName = mat.shader.name;

        szAssetsPath = "Assets/Atlas/" + atlas.m_szAtlasName + ".png";
        Texture mainTex = AssetDatabase.LoadAssetAtPath(szAssetsPath, typeof(Texture)) as Texture;

        string szOldShaderName = atlas.m_szShaderName;

        int nOldW = atlas.texWidth;
        int nOldH = atlas.texHeight;
        atlas.SetTextureSizeByTexture(mainTex);
        bool bDirty = false;
        if (nOldH != atlas.texHeight
            || nOldW != atlas.texWidth)
            bDirty = true;

        if (string.IsNullOrEmpty(szOldShaderName)
            || szShaderName != szOldShaderName)
        {
            atlas.m_szShaderName = szShaderName;
            bDirty = true;
        }
        return bDirty;
    }

    public bool AdjustAtlasData()
    {
        if (!IsCanSave())
            return false;
        int nAtlasNumb = m_AtlasPtr.size();
        int nDiryty = 0;
        for (int i = 0; i < nAtlasNumb; ++i)
        {
            UITexAtlas atlas = m_AtlasPtr[i];
            if (atlas == null)
                continue;
            if( AdjustAtlasShaderName(atlas) )
            {
                ++nDiryty;
            }
        }
        SaveAltasCfg();
        if (nDiryty > 0)
        {
            return true;
        }
        return false;
    }

    // 功能：保存配置在编辑器模式下
    public override void SaveAltasCfg()
    {
        if (IsCanSave())
        {
            ++m_nFileVersion;
            MakeSpriteAtlasID();
            string szCfgPathName = Application.dataPath + "/Atlas/assets_all.txt";
            SerializeText ar = new SerializeText(SerializeType.write, szCfgPathName);
            SerializeToTxt(ar);
            ar.Close();
            string szBinPathName = Application.dataPath + "/Atlas/assets_all.bytes";
            CSerialize arBin = new CSerialize(SerializeType.write, szBinPathName);
            Serialize(arBin);
            arBin.Close();
            n_nObjNumb = m_AllSprite.Count + m_TexAtlas.Count + 10;
            m_tLastWriteTime = File.GetLastWriteTime(szCfgPathName);
        }
    }
    protected override void RealReleaseMaterial(UITexAtlas atlas)
    {
        if (IsEditorMode())
        {
            if (atlas.m_material != null)
            {
                atlas.m_material.mainTexture = null;
            }
            atlas.m_nVersion++;
        }
        else
        {
            base.RealReleaseMaterial(atlas);
        }
    }

    // 功能：加载一个纹理吧
    Texture2D LoadTextureByAssetsName(string szAssetsName)
    {
		return LoadCanWriteTextureByAssetsName(szAssetsName);
    }

    // 功能：去除多余的像素
    Color32[] TrimAlpha(ref Color32[] pixels, ref RectInt rc)
    {
        Color32[] oldPixels = pixels;
        RectInt oldRc = rc;
        RectInt newRc = rc;
        newRc.swap();
        int nW = rc.width, nH = rc.height;
        int nPixelNumb = 0, newIndex = 0;
        for (int y = 0; y < nH; ++y)
        {
            for (int x = 0; x < nW; ++x, ++newIndex)
            {
                if (pixels[newIndex].a != 0)
                {
                    ++nPixelNumb;
                    newRc.unitPoint(x, y);
                }
            }
        }
        if (newRc == oldRc)
            return pixels;
        // 空的图
        if (0 == nPixelNumb)
        {
            rc.SetRect(0, 0, 1, 1);
            pixels = new Color32[1];
            pixels[0] = new Color32(0, 0, 0, 0);
            return pixels;
        }
        pixels = new Color32[newRc.width * newRc.height];
        newIndex = 0;
        int oldIndex = 0;
        for (int y = newRc.top; y < newRc.bottom; ++y)
        {
            for (int x = newRc.left; x < newRc.right; ++x, ++newIndex)
            {
                oldIndex = y * nW + x;
                pixels[newIndex] = oldPixels[oldIndex];
            }
        }
		rc = newRc;
        //rc.SetRect(0, 0, newRc.width, newRc.height);
        return pixels;
    }

	// 功能：从主材质拷贝精灵像素数据
	Color32[] GetSpritePixelData(UISpriteInfo sprite, Color32[] atlasPixels, int nAtlasTexWidth, int nAtlasTexHeight)
	{
		RectInt rcAtlas  = new RectInt(0, 0, nAtlasTexWidth, nAtlasTexHeight);
		RectInt rcSprite = new RectInt(sprite.outer);
		rcSprite = RectInt.clipRect(rcAtlas, rcSprite);
		
		int newWidth  = rcSprite.width;
		int newHeight = rcSprite.height;
		int oldWidth  = rcAtlas.width;
		int oldHeight = rcAtlas.height;
		
		if (newWidth <= 0 || newHeight <= 0)
			return null;
		
		Color32[] newPixels = new Color32[newWidth * newHeight];
		
		int newIndex = 0, oldIndex = 0;
		int nTop = oldHeight - rcSprite.bottom;
		int nBottom = nTop + rcSprite.height;     // 这图像是倒过来的
		for (int y = nTop; y < nBottom; ++y)
		{
			for (int x = rcSprite.left; x < rcSprite.right; ++x, ++newIndex)
			{
				oldIndex = y * oldWidth + x;
				newPixels[newIndex] = atlasPixels[oldIndex];
            }
        }
		return newPixels;
    }
    
    // 功能：从主材质中拷贝精灵的纹理
    // 参数：pAtlasTex - 主材质
    //       sprite - 精灵对象
    //       bPMA - 是不是将颜色预乘以alpha
    Texture2D GetSpriteTex(Texture2D pAtlasTex, UISpriteInfo sprite, bool bPMA, bool bTrimAlpha)
    {
        RectInt rcAtlas  = new RectInt(0, 0, pAtlasTex.width, pAtlasTex.height);
        RectInt rcSprite = new RectInt(sprite.outer);
        rcSprite = RectInt.clipRect(rcAtlas, rcSprite);
        
        int newWidth  = rcSprite.width;
        int newHeight = rcSprite.height;
        int oldWidth  = rcAtlas.width;
        int oldHeight = rcAtlas.height;

        if (newWidth <= 0 || newHeight <= 0)
            return null;
        Color32[] pixels = pAtlasTex.GetPixels32();

        Color32[] newPixels = new Color32[newWidth * newHeight];

        int newIndex = 0, oldIndex = 0;
		int nTop = oldHeight - rcSprite.bottom;
		int nBottom = nTop + rcSprite.height;     // 这图像是倒过来的
        for (int y = nTop; y < nBottom; ++y)
        {
            for (int x = rcSprite.left; x < rcSprite.right; ++x, ++newIndex)
            {
                oldIndex = y * oldWidth + x;
                // 如果是PMA属性  NGUISettings.atlasPMA
                if (bPMA && pixels[oldIndex].a != 1) // NGUISettings.atlasPMA)
                    newPixels[newIndex] = ApplyPMA(pixels[oldIndex]);
                else 
                    newPixels[newIndex] = pixels[oldIndex];
            }
        }

        // 去掉多余的像素(一般来说，从主图中获取时不需要做alpha测试)，但这里还是加上吧
		rcSprite = new RectInt(0, 0, rcSprite.width, rcSprite.height);
		RectInt  rcOld = rcSprite;
		if( bTrimAlpha )
		{
			newPixels = TrimAlpha(ref newPixels, ref rcSprite);
			if( rcSprite != rcOld )
			{
				int  nPadingLeft = Mathf.RoundToInt(sprite.paddingLeft * sprite.outer.width);
				int  nPadingRight = Mathf.RoundToInt(sprite.paddingRight * sprite.outer.width);
				int  nPadingTop = Mathf.RoundToInt(sprite.paddingTop * sprite.outer.height);
				int  nPadingBottom = Mathf.RoundToInt(sprite.paddingBottom * sprite.outer.height);

				nPadingLeft  += rcSprite.left;
				nPadingRight += rcOld.right - rcSprite.right;
				nPadingTop   += rcSprite.top;
				nPadingBottom += rcOld.bottom - rcSprite.bottom;

				float  fNewW = (float)rcSprite.width;
				float  fNewH = (float)rcSprite.height;
				sprite.paddingLeft   = (float)nPadingLeft / fNewW;
				sprite.paddingRight  = (float)nPadingRight / fNewW;
				sprite.paddingTop    = (float)nPadingTop / fNewH;
				sprite.paddingBottom = (float)nPadingBottom / fNewH;
			}
		}

		Texture2D tex = null;
		try
		{
			tex = new Texture2D(rcSprite.width, rcSprite.height, TextureFormat.ARGB32, false);
        	tex.SetPixels32(newPixels);
        	tex.Apply();
		}
		catch (Exception e)
		{
			Debug.LogError(e.ToString());
		}

        return tex;
    }

    static public Color ApplyPMA(Color c)
    {
        if (c.a != 1f)
        {
            c.r *= c.a;
            c.g *= c.a;
            c.b *= c.a;
        }
        return c;
    }

    // 功能：得到当前选中的纹理对象
    static public List<Texture> GetSelectedTextures()
    {
        List<Texture> textures = new List<Texture>();

        if (Selection.objects != null && Selection.objects.Length > 0)
        {
            Object[] objects = EditorUtility.CollectDependencies(Selection.objects);

            foreach (Object o in objects)
            {
                Texture tex = o as Texture;
                if (tex != null ) textures.Add(tex);
            }
        }
        return textures;
    }
    // 功能：得到精灵的名字
    static public string GetSpriteNameByAssetsName(string szAssetsName)
    {
        if (string.IsNullOrEmpty(szAssetsName))
            return "";
        szAssetsName = szAssetsName.Replace( '\\', '/' );
        int  nPos = szAssetsName.LastIndexOf( '/');
        if (nPos != -1)
        {
            szAssetsName = szAssetsName.Substring(nPos + 1);
        }
        nPos = szAssetsName.IndexOf('.');
        if (nPos != -1)
            szAssetsName = szAssetsName.Remove(nPos);
        return szAssetsName;
    }
    List<SelecctTexInfo> GetSelectedTexInfo()
    {
        List<SelecctTexInfo> selectList = new List<SelecctTexInfo>();
        List<Texture> aTexList = GetSelectedTextures();
        foreach (Texture o in aTexList)
        {
            string szName = GetAssetPathByTexture(o);
            if (!string.IsNullOrEmpty(szName))
            {
                SelecctTexInfo info = new SelecctTexInfo();
                info.m_szAssetsName = szName;
                info.m_tex          = LoadTextureByAssetsName(szName);
                info.m_szSpriteName = GetSpriteNameByAssetsName(szName);
                UITexAtlas atlas = GetAltasBySpriteName(info.m_szSpriteName);
                if (atlas != null)
                    info.m_szAtlasName = atlas.m_szAtlasName;
                info.m_sprite = GetSprite(info.m_szSpriteName);
                selectList.Add(info);
            }
        }
        return selectList;
    }
    Dictionary<string, UpdateTexNameList> GetSelectedTexInfo(List<SUpdateTexInfo> nameList)
    {
        Dictionary<string, UpdateTexNameList> selectList = new Dictionary<string, UpdateTexNameList>();
        int nCount = nameList.Count;
        foreach (SUpdateTexInfo o in nameList)
        {
            SelecctTexInfo info = new SelecctTexInfo();
            info.m_szAssetsName = o.m_szAssetsName;
            info.m_szSpriteName = o.m_szSpriteName;
            info.m_szAtlasName  = o.m_szAtlasName;
            info.m_sprite = GetSprite(o.m_szSpriteName);
            info.m_tex = LoadTextureByAssetsName(o.m_szAssetsName);

            if (selectList.ContainsKey(o.m_szAtlasName))
                selectList[o.m_szAtlasName].push_back(info);
            else
            {
                UpdateTexNameList sList = new UpdateTexNameList();
                sList.push_back(info);
                selectList[o.m_szAtlasName] = sList;
            }
        }
        return selectList;
    }

    // 释放纹理对象吧
    static void ReleaseSprites(List<SelecctTexInfo> sprites)
    {
        int nCount = sprites.Count;
        for( int i = 0; i<nCount; ++i )
        {
            SelecctTexInfo se = sprites[i];
            if (se.m_tex)
            {
                Tool_Destroy(se.m_tex);
                se.m_tex = null;
            }
        }
        Resources.UnloadUnusedAssets();
    }

    static public void Tool_Destroy(UnityEngine.Object obj)
    {
        if (obj != null)
        {
            if (Application.isPlaying)
            {
                if (obj is GameObject)
                {
                    GameObject go = obj as GameObject;
                    go.transform.parent = null;
                }

                UnityEngine.Object.Destroy(obj);
            }
            else UnityEngine.Object.DestroyImmediate(obj);
        }
    }

    // 功能：得到材质的所有精灵对象
    List<UISpriteInfo> GetAllSpriteByAtlas(string szAtlasName)
    {
        List<UISpriteInfo> aSprite = new List<UISpriteInfo>();
        Dictionary<string, UISpriteInfo>.Enumerator it = m_AllSprite.GetEnumerator();
        while (it.MoveNext())
        {
            if (it.Current.Value.m_szAtlasName == szAtlasName)
            {
                aSprite.Add(it.Current.Value);
            }
        }
        return aSprite;
    }

    // 功能：得到纹理对象AssetsPath
    static public string GetAssetPathByTexture(Texture tex)
    {
        if (tex != null)
        {
            string path = AssetDatabase.GetAssetPath(tex.GetInstanceID());
            return path;
        }
        return "";
    }
    
    // 加载吧
    protected override bool QueryAltasTex(UITexAtlas atlas, UITexAtlas.OnLoadAtlas lpOnLoadFunc)
    {
        if (IsEditorMode())
        {
            string szTxtAssetPath = "Assets/Atlas/" + atlas.m_szAtlasName + ".png";
            Texture tex = AssetDatabase.LoadAssetAtPath(szTxtAssetPath, typeof(Texture)) as Texture;
            if (atlas.m_material == null)
            {
                string szShaderName = atlas.m_szShaderName;
                bool bHaveMainAlpha = atlas.m_szShaderName == "Unlit/Transparent Colored MainAlpha";

                if (IsForceOneTexture())
                {
                    // PC平台强制使用一张纹图
                    bHaveMainAlpha = false;
                    szShaderName = szShaderName.Replace(" MainAlpha", "");
                }
                Shader shader = Shader.Find(szShaderName);
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Transparent Colored");
                }
                atlas.m_material = new Material(shader);
                atlas.m_material.name = atlas.m_szAtlasName;
                if(bHaveMainAlpha)
                {
                    szTxtAssetPath = "Assets/Atlas/" + atlas.m_szAtlasName + "_alpha.png";
                    Texture mainAlpha = AssetDatabase.LoadAssetAtPath(szTxtAssetPath, typeof(Texture)) as Texture;
                    atlas.m_MainAlpha = mainAlpha;
                    atlas.m_material.SetTexture("_MainAlpha", mainAlpha);
                }
            }
            atlas.m_material.mainTexture = tex;
            if (tex != null)
                atlas.m_nVersion++;

            return true;
        }
        else
        {
            return base.QueryAltasTex(atlas, lpOnLoadFunc);
        }
    }

    // 功能：新建材质
    bool NewAtlas(string szAtlasName, string szShaderName, bool bCanLOD)
    {
        Shader shader = Shader.Find(szShaderName);
        UITexAtlas atlas = new UITexAtlas();
        atlas.m_szAtlasName = szAtlasName;
        atlas.m_szTexName = szAtlasName + ".png";
        atlas.m_material = new Material(shader);
        atlas.m_szShaderName = szShaderName;// bAtlasPMA ? "Unlit/Premultiplied Colored" : "Unlit/Transparent Colored";
        atlas.SetLODFlag(bCanLOD);
        atlas.SetTextureSizeByMaterial(atlas.m_material);
        m_TexAtlas[szAtlasName] = atlas;

        return true;
    }

    // 功能：添加或更新当前选择精灵对象
    // 参数：nameList - 要更新的精灵对象
	public bool AddOrUpdateSelectSprite(List<SUpdateTexInfo> nameList, bool bTrimAlpha, bool bAtlasPMA, bool bCanLOD )
    {
        if (!IsCanSave())
            return false;

        Dictionary<string, UpdateTexNameList> aSelectTex = GetSelectedTexInfo(nameList);        

        Dictionary<string, int> aDirtyAtlas = new Dictionary<string,int>();

        Dictionary<string, UpdateTexNameList>.Enumerator it = aSelectTex.GetEnumerator();

        // 先修改精灵材质
        while (it.MoveNext())
        {
            string szAtlasName = it.Current.Key;
            List<SelecctTexInfo> selectList = it.Current.Value.nameList;

            aDirtyAtlas[szAtlasName] = 1;

            foreach (SelecctTexInfo o in selectList)
            {
                if (o.m_sprite != null && szAtlasName != o.m_sprite.m_szAtlasName)
                {
                    o.m_sprite.m_szAtlasName = szAtlasName;
                }
            }
        }

        string szShaderName = bAtlasPMA ? "Unlit/Premultiplied Colored" : "Unlit/Transparent Colored MainAlpha";

        bool bCfgDirty = false;
        it = aSelectTex.GetEnumerator();
        while (it.MoveNext())
        {
            string szAtlasName = it.Current.Key;
            List<SelecctTexInfo> selectList = it.Current.Value.nameList;

            // 如果当前材质不存在，就新建一个材质吧
            if (GetAltas(szAtlasName) == null)
            {
                NewAtlas(szAtlasName, szShaderName, bCanLOD);
            }

            // 再更新当前的吧
            bool bReplace = false;
			if (UpdateAltasBySelect(ref selectList, szAtlasName, bTrimAlpha, ref bReplace))
            {
                if (!bReplace)
                    bCfgDirty = true;
                // 添加对象吧
                int nCount = selectList.Count;
                for (int i = 0; i < nCount; ++i)
                {
                    m_AllSprite[selectList[i].m_szSpriteName] = selectList[i].m_sprite;
                }
                if( aDirtyAtlas.ContainsKey(szAtlasName) )
                    aDirtyAtlas.Remove(szAtlasName);
            }
        }

        Dictionary<string, int>.Enumerator itDirty = aDirtyAtlas.GetEnumerator();
        while (itDirty.MoveNext())
        {
            // 更新材质吧
            if( itDirty.Current.Value == 1 )
				UpdateAtlasTexture(itDirty.Current.Key, false, bTrimAlpha);
        }
        if(bCfgDirty)
            SaveAltasCfg();
        return true;
    }
    
    // 功能：加载一个可以读写的纹理
    Texture2D LoadCanWriteTextureByAssetsName(string szAssetsPathName)
    {
        string szDataPath = Application.dataPath;
        // Assets
        szDataPath = szDataPath.Substring(0, szDataPath.Length - 6);
        
        string szPathName = szDataPath + szAssetsPathName;
        Texture2D tex = CPngHelp.ReadPng(szPathName);
        return tex;
    }
    // 功能：更新选中的对象
	bool UpdateAltasBySelect(ref List<SelecctTexInfo> aSelectTex, string szAtlasName, bool bTrimAlpha, ref bool bReplace)
    {
        // 尝试原位置替换
        bReplace = true;
        if (TryRepaceLocalPosition(ref aSelectTex, szAtlasName, bTrimAlpha))
            return true;

        bReplace = false;
        UITexAtlas atlas = GetAltas(szAtlasName);
        string szAtlasPathName = "Assets/Atlas/" + szAtlasName + ".png";

        Texture2D atlasTex = LoadCanWriteTextureByAssetsName(szAtlasPathName);

        Dictionary<string, SelecctTexInfo> aAllSpriteInfo = new Dictionary<string, SelecctTexInfo>();

        List<Texture2D> aTex = new List<Texture2D>();
        int nCount = aSelectTex.Count;
        for (int i = 0; i < nCount; ++i)
        {
            aAllSpriteInfo[aSelectTex[i].m_szSpriteName] = aSelectTex[i];
        }

        bool bPMA = atlas != null ? atlas.premultipliedAlpha : false;

        // 先得到旧的吧
        List<SelecctTexInfo> aNewSelect = new List<SelecctTexInfo>();
        List<UISpriteInfo> aSprite = GetAllSpriteByAtlas(szAtlasName);
        nCount = aSprite.Count;
        for (int i = 0; i < nCount; ++i)
        {
            if (!aAllSpriteInfo.ContainsKey(aSprite[i].name))
            {
				Texture2D tex = GetSpriteTex(atlasTex, aSprite[i], bPMA, bTrimAlpha);
                SelecctTexInfo info = new SelecctTexInfo();
                info.m_sprite = aSprite[i];
                info.m_szAssetsName = GetAssetPathByTexture(tex);
                info.m_szAtlasName = szAtlasName;
                info.m_szSpriteName = aSprite[i].name;
                info.m_tex = tex;
                aNewSelect.Add(info);
            }
        }
        
        nCount = aSelectTex.Count;
        for (int i = 0; i < nCount; ++i)
        {
            aNewSelect.Add(aSelectTex[i]);
        }
        aSelectTex = aNewSelect;
		nCount = aNewSelect.Count;
		for( int i = 0; i<nCount; ++i )
		{
			aTex.Add(aNewSelect[i].m_tex);
		}        
        return UpdateAltasBySelect(aNewSelect, aTex, atlas, atlasTex, szAtlasPathName);
    }

    // 功能：尝试只是原位置替换图片
    bool TryRepaceLocalPosition(ref List<SelecctTexInfo> aSelectTex, string szAtlasName, bool bTrimAlpha)
    {
        UITexAtlas atlas = GetAltas(szAtlasName);
        if (atlas == null)
            return false;
        // 检查一下所有选择对象，必须是原来存在的，并且大小相同
        foreach (SelecctTexInfo selTex in aSelectTex)
        {
            UISpriteInfo  sp = GetSprite(selTex.m_szSpriteName);
            if (null == sp)
                return false;
            if (selTex.m_tex == null)
                return false;
            RectInt rcOut = new RectInt(sp.outer);
            if (rcOut.width != selTex.m_tex.width
                || rcOut.height != selTex.m_tex.height)
                return false;
        }
        string szAtlasPathName = "Assets/Atlas/" + szAtlasName + ".png";
        Texture2D atlasTex = LoadCanWriteTextureByAssetsName(szAtlasPathName);
        if (atlasTex == null)
            return false;

        Color32[] atlasPixels = atlasTex.GetPixels32();
        atlasTex = new Texture2D(atlasTex.width, atlasTex.height, TextureFormat.ARGB32, false);
        foreach (SelecctTexInfo selTex in aSelectTex)
        {
            UISpriteInfo  sp = GetSprite(selTex.m_szSpriteName);
            RectInt rcOut = new RectInt(sp.outer);
            Color32[] iconPixels = selTex.m_tex.GetPixels32();
            WriteIconPixels(ref atlasPixels, atlasTex.width, atlasTex.height, rcOut.left, rcOut.top, rcOut.width, rcOut.height, iconPixels);
        }
        atlasTex.SetPixels32(atlasPixels);
        atlasTex.Apply();
        // 先去只读吧
        string szDataPath = Application.dataPath;
        // Assets
        szDataPath = szDataPath.Substring(0, szDataPath.Length - 6);

        string newPath = szDataPath + szAtlasPathName;
        if (System.IO.File.Exists(newPath))
        {
            System.IO.FileAttributes newPathAttrs = System.IO.File.GetAttributes(newPath);
            newPathAttrs &= ~System.IO.FileAttributes.ReadOnly;
            System.IO.File.SetAttributes(newPath, newPathAttrs);
            //System.IO.File.Delete(newPath);
        }
        byte[] bytes = atlasTex.EncodeToPNG();
        if (bytes == null)
        {
            return false;
        }
        System.IO.File.WriteAllBytes(newPath, bytes);
        bytes = null;

        SaveMainAlpha(atlasTex, newPath);

        Object.DestroyImmediate(atlasTex);
        
        return true;
    }

    void SaveMainAlpha(Texture2D atlasTex, string szPathName)
    {
        if (atlasTex == null)
            return;
        Texture2D mainAlpha = new Texture2D(atlasTex.width, atlasTex.height, TextureFormat.ARGB32, false);
        Color32[] pixel = atlasTex.GetPixels32();
        if(pixel != null)
        {
            for (int i = 0; i < pixel.Length; ++i)
            {
                pixel[i].r = pixel[i].g = pixel[i].b = pixel[i].a;
            }
        }
        mainAlpha.SetPixels32(pixel);
        mainAlpha.Apply();
        string szAlphaPathName = szPathName;
        if(szAlphaPathName.IndexOf("_alpha.") == -1)
            szAlphaPathName = szPathName.Replace(".png", "_alpha.png");
        bool bFindFile = false;
        if (System.IO.File.Exists(szAlphaPathName))
        {
            bFindFile = true;
            System.IO.FileAttributes newPathAttrs = System.IO.File.GetAttributes(szAlphaPathName);
            newPathAttrs &= ~System.IO.FileAttributes.ReadOnly;
            System.IO.File.SetAttributes(szAlphaPathName, newPathAttrs);
        }
        byte[] bytes = mainAlpha.EncodeToPNG();
        if (bytes == null)
        {
            return;
        }
        System.IO.File.WriteAllBytes(szAlphaPathName, bytes);
        bytes = null;
        if(!bFindFile)
        {
            AssetDatabase.Refresh();
            ChangeTextureFormat(szAlphaPathName);
        }
    }

    // 功能：给指定目标的文件生成通道图
    public void MakeMainAlpha(string  szAtlasPathName)
    {
        Texture2D atlasTex = LoadCanWriteTextureByAssetsName(szAtlasPathName);

        string szDataPath = Application.dataPath;
        szDataPath = szDataPath.Substring(0, szDataPath.Length - 6);
        string newPath = szDataPath + szAtlasPathName;
        int nIndex = newPath.LastIndexOf('.');
        if(nIndex != -1)
        {
            string szExt = newPath.Substring(nIndex);
            newPath = newPath.Substring(0, nIndex);
            newPath = newPath + "_alpha" + szExt;
        }
        else
        {
            newPath = newPath + "_alpha.png";
        }
        SaveMainAlpha(atlasTex, newPath);
    }

    public void ChangeAssetTextureFormat(string szAssetsPath, TextureImporterType nImType, bool bRefresh = true)
    {
        TextureImporter texImp = TextureImporter.GetAtPath(szAssetsPath) as TextureImporter;
        if (texImp == null)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            //AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            texImp = TextureImporter.GetAtPath(szAssetsPath) as TextureImporter;
        }
        if (texImp != null)
        {
            texImp.textureType = nImType;
            if (nImType == TextureImporterType.GUI)
            {
                texImp.textureType = TextureImporterType.GUI;
                texImp.mipmapEnabled = false;
                texImp.anisoLevel = 0;
            }
            texImp.isReadable = false;
            texImp.filterMode = FilterMode.Bilinear;
            int maxsize = 2048;

            
            TextureImporterPlatformSettings  androidSet = texImp.GetPlatformTextureSettings("Android");
            TextureImporterPlatformSettings iosSet = texImp.GetPlatformTextureSettings("iPhone");
            TextureImporterPlatformSettings pcSet = texImp.GetPlatformTextureSettings("Standalone");
            //TextureImporterPlatformSettings defaultSet = texImp.GetPlatformTextureSettings("Default");

            androidSet.maxTextureSize = maxsize;
            androidSet.format = TextureImporterFormat.ETC_RGB4;

            iosSet.maxTextureSize = maxsize;
            iosSet.format = TextureImporterFormat.PVRTC_RGB4;

            pcSet.maxTextureSize = maxsize;
            pcSet.format = TextureImporterFormat.RGBA32;

            if(szAssetsPath.IndexOf("_alpha.png") != -1
                || szAssetsPath.IndexOf("_alpha_L.png") != -1)
            {
                pcSet.format = TextureImporterFormat.Alpha8;
            }

            //defaultSet.maxTextureSize = maxsize;
            //defaultSet.format = TextureImporterFormat.RGBA32;

            texImp.SetPlatformTextureSettings(androidSet);
            texImp.SetPlatformTextureSettings(iosSet);
            texImp.SetPlatformTextureSettings(pcSet);
            //texImp.SetPlatformTextureSettings(defaultSet);

            //texImp.SetPlatformTextureSettings("Android", maxsize, TextureImporterFormat.ETC_RGB4);
            //texImp.SetPlatformTextureSettings("iPhone", maxsize, TextureImporterFormat.PVRTC_RGB4);
            //texImp.SetPlatformTextureSettings("Standalone", maxsize, TextureImporterFormat.RGBA32);
            texImp.SaveAndReimport();
            if(bRefresh)
            {
                AssetDatabase.ImportAsset(szAssetsPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();
            }
        }
    }

    public void ChangeTextureFormat(string szPathName)
    {
        szPathName = szPathName.Replace('\\', '/');
        int nPos = szPathName.IndexOf("/Assets/Atlas/");
        string szAssetsPath = szPathName.Substring(nPos + 1);
        ChangeAssetTextureFormat(szAssetsPath, TextureImporterType.GUI);
    }

    void WriteIconPixels(ref Color32[] atlasPixels, int nAtlasW, int nAtlasH, int x, int y, int w, int h, Color32[] iconPixels)
    {
        // 左下角是(0, 0), 坐标是反的噢
        for (int nY = 0; nY < h; ++nY)
        {
            for (int nX = 0; nX < w; ++nX)
            {
                int nIndex = (nAtlasH - nY - y - 1) * nAtlasW + (nX + x);
                int  nSrcIndex = (h - nY - 1) * w + nX;
                atlasPixels[nIndex] = iconPixels[nSrcIndex];
            }
        }
    }

    // 功能：更新材质
    bool UpdateAltasBySelect(List<SelecctTexInfo> aSelectTex, List<Texture2D> aTex, UITexAtlas atlas, Texture2D atlasTex, string szAtlasPathName)
    {
        int nPadding = 1;
        int nMaxSize = 4096;
        if (atlas != null)
        {
            nPadding = atlas.pixelSize;
        }
        if (aTex.Count == 1)
            nPadding = 0;
        bool bNeedSaveAssets = false;
        if (atlasTex == null)
        {
            bNeedSaveAssets = true;
            atlasTex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        }

        Rect[] aSpriteRect = atlasTex.PackTextures(aTex.ToArray(), nPadding, nMaxSize);
        if (aSpriteRect.Length != aSelectTex.Count)
        {
            // 失败了
            return false;
        }

        // 检查是不是变小了
        for (int i = 0; i < aSelectTex.Count; ++i)
        {
            UISpriteInfo sp = aSelectTex[i].m_sprite;
            if (sp != null)
            {
                Rect rect = ConvertToPixels(aSpriteRect[i], atlasTex.width, atlasTex.height, true);
                RectInt rcOut = new RectInt(sp.outer);
                RectInt rcNew = new RectInt(rect);
                if (aTex[i].width != rcNew.width
                    || aTex[i].height != rcNew.height)
                {
                    return false;
                }
            }
        }

        // 先去只读吧
        string szDataPath = Application.dataPath;
        // Assets
        szDataPath = szDataPath.Substring(0, szDataPath.Length - 6);

        string newPath = szDataPath + szAtlasPathName;
        bool bFindAltasFile = false;
        if (System.IO.File.Exists(newPath))
        {
            bFindAltasFile = true;
            System.IO.FileAttributes newPathAttrs = System.IO.File.GetAttributes(newPath);
            newPathAttrs &= ~System.IO.FileAttributes.ReadOnly;
            System.IO.File.SetAttributes(newPath, newPathAttrs);
        }
        byte[] bytes = atlasTex.EncodeToPNG();
        if (bytes == null)
        {
            return false;
        }
        System.IO.File.WriteAllBytes(newPath, bytes);
        bytes = null;
        
        int nCount = aSelectTex.Count;
        bool bNew = false;
        for (int i = 0; i < nCount; ++i)
        {
            UISpriteInfo sp = aSelectTex[i].m_sprite;
            bNew = (sp == null);

            Rect rect = ConvertToPixels(aSpriteRect[i], atlasTex.width, atlasTex.height, true);
            if (sp == null)
            {
                SelecctTexInfo sObj = aSelectTex[i];
                sObj.m_sprite = sp = new UISpriteInfo();
                sp.m_szAtlasName = sObj.m_szAtlasName;
                sp.name = sObj.m_szSpriteName;
                aSelectTex[i] = sObj;

                // 这是新的
                sp.outer = rect;
                sp.inner = rect;

                sp.paddingLeft = 0.0f;
                sp.paddingTop = 0.0f;
                sp.paddingRight = 0.0f;
                sp.paddingBottom = 0.0f;
            }
            else
            {
                RectInt rcOut = new RectInt(sp.outer);
                RectInt rcIn = new RectInt(sp.inner);
                sp.outer = rect;

                rcIn.left -= rcOut.left;
                rcIn.right -= rcOut.right;
                rcIn.top -= rcOut.top;
                rcIn.bottom -= rcOut.bottom;

				rcOut.SetRect(rect);
				rcOut.left += rcIn.left;
				rcOut.top += rcIn.top;
				rcOut.right += rcIn.right;
				rcOut.bottom += rcIn.bottom;
				sp.inner = new Rect(rcOut.left, rcOut.top, rcOut.width, rcOut.height);
            }
            float width = Mathf.Max(1f, sp.outer.width);
            float height = Mathf.Max(1f, sp.outer.height);

            // Sprite's padding values are relative to width and height
        }        
        atlas.SetTextureSizeByTexture(atlasTex);
        atlas.m_nVersion++;
        atlas.m_bDirty = true;
        if (atlas.m_nRef > 0)
        {
            if (atlas.m_material != null)
                atlas.m_material.mainTexture = atlasTex;
        }

        if(!bFindAltasFile)
        {
            ChangeTextureFormat(newPath);
        }
        SaveMainAlpha(atlasTex, newPath);

        if(atlas.IsCanLOD())
        {
            string szAssetPath = "Assets/Atlas/";
            string szAtlasName = atlas.m_szAtlasName;
            MakeLODTexture(szAssetPath, szAtlasName);
            MakeLODTexture(szAssetPath, szAtlasName + "_alpha");
        }

        return true;
    }

    /// <summary>
    /// Convert from bottom-left based UV coordinates to top-left based pixel coordinates.
    /// </summary>

    static public Rect ConvertToPixels(Rect rect, int width, int height, bool round)
    {
        Rect final = rect;

        if (round)
        {
            final.xMin = Mathf.RoundToInt(rect.xMin * width);
            final.xMax = Mathf.RoundToInt(rect.xMax * width);
            final.yMin = Mathf.RoundToInt((1f - rect.yMax) * height);
            final.yMax = Mathf.RoundToInt((1f - rect.yMin) * height);
        }
        else
        {
            final.xMin = rect.xMin * width;
            final.xMax = rect.xMax * width;
            final.yMin = (1f - rect.yMax) * height;
            final.yMax = (1f - rect.yMin) * height;
        }
        return final;
    }

    // 功能：仅仅是更新材质贴图
    bool UpdateAtlasTexture(string szAtlasName, bool bForceNew = false, bool bTrimAlpha = false)
    {
        UITexAtlas atlas = GetAltas(szAtlasName);
        List<UISpriteInfo> aSprite = GetAllSpriteByAtlas(szAtlasName);
        string  szAtlasPathName = "Assets/Atlas/" + szAtlasName + ".png";
		Texture2D atlasTex = LoadCanWriteTextureByAssetsName(szAtlasPathName);

        // 创建一个空的吧
        if (aSprite == null || aSprite.Count == 0)
        {
            Color32[] newPixels = new Color32[1];
            newPixels[0] = new Color32(0, 0, 0, 0);
            atlasTex = new Texture2D(1, 1);
            atlasTex.SetPixels32(newPixels);
            atlasTex.Apply();
            return true;
        }
        if (atlasTex == null)
        {
            //atlasTex = new Texture2D;
            // 搜索所有的文件, 这是一个错误, 就不搜了
            return false;
        }
        else
        {
            List<SelecctTexInfo> aSelectTex = new List<SelecctTexInfo>();
            List<Texture2D> aTex = new List<Texture2D>();
            foreach (UISpriteInfo sp in aSprite)
            {
				Texture2D tex = GetSpriteTex(atlasTex, sp, true, bTrimAlpha);
                SelecctTexInfo info = new SelecctTexInfo();
                info.m_sprite = sp;
                info.m_szAssetsName = szAtlasName;
                info.m_szSpriteName = sp.name;
                info.m_tex = tex;
                aSelectTex.Add(info);
                aTex.Add(tex);
            }
            if( bForceNew )
                return UpdateAltasBySelect(aSelectTex, aTex, atlas, null, szAtlasPathName);
            else
                return UpdateAltasBySelect(aSelectTex, aTex, atlas, atlasTex, szAtlasPathName);
        }
        return true;
    }
    
    // 功能：删除精灵（不需要修改纹理，只需要
    public override bool DeleteSprite(List<string> nameList)
    {
        if (!IsCanSave())
            return false;

        int nCount = nameList.Count;
        if (nCount < 0)
            return false;
        Dictionary<string, int>  DirtyList = new Dictionary<string,int>();
        for (int i = 0; i < nCount; ++i)
        {
            string szSpriteName = nameList[i];
            if (m_AllSprite.ContainsKey(szSpriteName))
            {
                UITexAtlas atlas = GetAltasBySpriteName(szSpriteName);
                m_AllSprite.Remove(szSpriteName);
                if (atlas != null)
                {
                    DirtyList[atlas.m_szAtlasName] = 1;
                }
            }
        }
        Dictionary<string, int>.Enumerator it = DirtyList.GetEnumerator();
        while (it.MoveNext())
        {
			UpdateAtlasTexture(it.Current.Key, false, false);
        }

        // 删除没有引用的材质吧
        Dictionary<string, UITexAtlas> TexAtlas = new Dictionary<string, UITexAtlas>(); 
        Dictionary<string, UISpriteInfo>.Enumerator itSp = m_AllSprite.GetEnumerator();
        while (itSp.MoveNext())
        {
            string  szAtlasName = itSp.Current.Value.m_szAtlasName;
            if ( !string.IsNullOrEmpty(szAtlasName) && !TexAtlas.ContainsKey(szAtlasName))
            {
                TexAtlas[szAtlasName] = GetAltas(szAtlasName);
            }
        }
        m_TexAtlas = TexAtlas;
        SaveAltasCfg();
        return true;
    }
    public void BeginChange()
    {
        if (!IsCanSave())
            return ;
        m_AllSprite.Clear();
        m_TexAtlas.Clear();
    }
    public void PushChangeAtlas(UITexAtlas atlas, List<UISpriteInfo> spriteList, List<string> repeatNameList)
    {
        if (!IsCanSave())
            return ;
        if (atlas == null || spriteList == null)
            return;
        if (spriteList.Count <= 0)
            return;
        m_TexAtlas[atlas.m_szAtlasName] = atlas;
        foreach (UISpriteInfo sp in spriteList)
        {
            if (m_AllSprite.ContainsKey(sp.name))
            {
                UISpriteInfo oldSp = null;
                m_AllSprite.TryGetValue(sp.name, out oldSp);

                string szName = sp.m_szAtlasName + ":" + atlas.m_szAtlasName + ":" + sp.name + "-----" + oldSp.m_szAtlasName + "/" + oldSp.name;
                repeatNameList.Add(szName);
            }
            m_AllSprite[sp.name] = sp;
        }
    }
    public int EndChange()
    {
        if (!IsCanSave())
            return 0;
        SaveAltasCfg();
        return m_AllSprite.Count;
    }
    public int GetSpriteNumb(string szAtlasName)
    {
        int nSpriteNumb = 0;
        int nAllNumb = m_AllSprite.Count;

        Dictionary<string, UISpriteInfo>.Enumerator it = m_AllSprite.GetEnumerator();
        while( it.MoveNext() )
        {
            if (it.Current.Value.m_szAtlasName == szAtlasName)
            {
                ++nSpriteNumb;
            }
        }
        return nSpriteNumb;
    }
	public bool SplitAtlas(string szAtlasName, bool bTrimAlpha, bool bCanLOD)
    {
        if (!IsCanSave())
            return false;
        if (!m_TexAtlas.ContainsKey(szAtlasName))
        {
            return false;
        }
        UITexAtlas oldAtlas = GetAltas(szAtlasName);

        // 先找一个合适的名字吧
        string szNewAtlasName = szAtlasName;
        bool bFind = false;
        for (int i = 1; i < 100; ++i)
        {
            szNewAtlasName = szAtlasName + "_" + i.ToString();
            if (!m_TexAtlas.ContainsKey(szNewAtlasName) || GetSpriteNumb(szNewAtlasName) == 0)
            {
                bFind = true;
                break;
            }
        }
        if (!bFind)
        {
            return false;
        }
        List<UISpriteInfo>    spriteList = GetAllSpriteByAtlas(szAtlasName);

        int nSpriteCount = spriteList.Count;
        if (nSpriteCount < 1)
            return false;

        if (!m_TexAtlas.ContainsKey(szNewAtlasName))
        {
            if (!NewAtlas(szNewAtlasName, oldAtlas.m_szShaderName, bCanLOD))
                return false;
        }
        UITexAtlas newAtlas = GetAltas(szNewAtlasName);
        newAtlas.CopyFromSetting(oldAtlas);

        string szAtlasPathName = "Assets/Atlas/" + szAtlasName + ".png";
        string szNewAtlasPathName = "Assets/Atlas/" + szNewAtlasName + ".png";

        Texture2D oldAtlasTex = LoadCanWriteTextureByAssetsName(szAtlasPathName);
        Texture2D newAtlasTex = LoadCanWriteTextureByAssetsName(szNewAtlasPathName);

        // 分裂一半吧
        List<SelecctTexInfo> newSelect = new List<SelecctTexInfo>();
        List<Texture2D> newTexList = new List<Texture2D>();
        List<SelecctTexInfo> oldSelect = new List<SelecctTexInfo>();
        List<Texture2D> oldTexList = new List<Texture2D>();
        bool bPMA = oldAtlas.premultipliedAlpha;
        int nHalfSize = nSpriteCount / 2;
        for (int i = 0; i < nSpriteCount; ++i)
        {
            UISpriteInfo sp = spriteList[i];
			Texture2D tex = GetSpriteTex(oldAtlasTex, sp, bPMA, bTrimAlpha);
            SelecctTexInfo info = new SelecctTexInfo();
            info.m_sprite = sp;
            info.m_szAssetsName = GetAssetPathByTexture(tex);
            info.m_szAtlasName = szNewAtlasName;
            info.m_szSpriteName = sp.name;
            info.m_tex = tex;
            if (i < nHalfSize)
            {
                oldSelect.Add(info);
                oldTexList.Add(tex);
            }
            else
            {
                newSelect.Add(info);
                newTexList.Add(tex);
                sp.m_szAtlasName = szNewAtlasName;
            }
        }
        UpdateAltasBySelect(newSelect, newTexList, newAtlas, null, szNewAtlasPathName);
        UpdateAltasBySelect(oldSelect, oldTexList, oldAtlas, null, szAtlasPathName);
        SaveAltasCfg();
        return true;
    }
	public bool RepareAtlas(string szAtlasName, bool bTrimAlpha)
    {
        if (!IsCanSave())
            return false;
        UITexAtlas oldAtlas = GetAltas(szAtlasName);
        if (oldAtlas == null)
            return false;
        
		bool bSuc = UpdateAtlasTexture(szAtlasName, true, bTrimAlpha);
        SaveAltasCfg();
        return bSuc;
    }
	public delegate bool LPExportIconFunc(string szPathName, Color32 []iconPixel, int nWidth, int nHeight);
	public bool ExportSpriteIcon(string szAtlasName, string szPath, LPExportIconFunc lpExportFunc)
    {
        if (!IsCanSave())
            return false;
		UITexAtlas oldAtlas = GetAltas(szAtlasName);
		if( oldAtlas == null )
			return false;
		string szAtlasPathName = "Assets/Atlas/" + szAtlasName + ".png";
		Texture2D oldAtlasTex = LoadCanWriteTextureByAssetsName(szAtlasPathName);
		if( oldAtlasTex == null )
			return false;

		Color32[] pixels = oldAtlasTex.GetPixels32();
		if( pixels == null || pixels.Length <= 0 )
			return false;

		int  nAtlasWidth = oldAtlasTex.width;
		int  nAtlasHeight = oldAtlasTex.height;
		List<UISpriteInfo> aSprite = GetAllSpriteByAtlas(szAtlasName);
		foreach(UISpriteInfo sp in aSprite )
		{
			string  szPathName = szPath + sp.name + ".tga";

			Color32  []iconPixel = GetSpritePixelData(sp, pixels, nAtlasWidth, nAtlasHeight);
			lpExportFunc(szPathName, iconPixel, (int)(sp.outer.width + 0.5f), (int)(sp.outer.height+0.5f));
		}

		return true;
	}

    // 功能：得到指定材质，指定UV的像素颜色
    // 说明：仅限编辑器模式生效
    public override Color GetAtlasPixelBilinear(int nAtlasID, float fu, float fv) 
    {
        UITexAtlas  atlas = GetAtlasByID(nAtlasID);
        if( atlas == null )
            return Color.black;

        if (m_nLastAtlasID != nAtlasID || m_pLastPixelTex == null)
        {
            if (m_pLastPixelTex != null)
            {
                Object.DestroyImmediate(m_pLastPixelTex);
                m_pLastPixelTex = null;
            }
            m_nLastAtlasID = nAtlasID;
            string szAtlasPathName = "Assets/Atlas/" + atlas.m_szAtlasName + ".png";
            m_pLastPixelTex = LoadCanWriteTextureByAssetsName(szAtlasPathName);
        }
        if (m_pLastPixelTex != null)
        {
            return m_pLastPixelTex.GetPixelBilinear(fu, fv);
        }
        return Color.black; 
    }    
}

#endif
