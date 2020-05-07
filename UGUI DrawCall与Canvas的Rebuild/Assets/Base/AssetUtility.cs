using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

class AssetUtility
{

    public static bool LoadBinText(ref byte[] fileData, AssetBundle bunlde)
    {
        if (bunlde == null)
            return false;
        TextAsset text = bunlde.mainAsset as TextAsset;
        if (text == null)
        {
            string[] Names = bunlde.GetAllAssetNames();
            if (Names != null && Names.Length > 0)
            {
                text = bunlde.LoadAsset(Names[0], typeof(TextAsset)) as TextAsset;
            }
        }
        if (text != null)
        {
            fileData = text.bytes;
            bunlde.Unload(true);
            return true;
        }
        bunlde.Unload(true);
        return false;
    }
    public static bool LoadText(ref string fileText, AssetBundle bunlde)
    {
        if (bunlde == null)
            return false;
        TextAsset text = bunlde.mainAsset as TextAsset;
        if (text == null)
        {
            string[] Names = bunlde.GetAllAssetNames();
            if (Names != null && Names.Length > 0)
            {
                text = bunlde.LoadAsset(Names[0], typeof(TextAsset)) as TextAsset;
            }
        }
        if (text != null)
        {
            fileText = text.text;
            bunlde.Unload(true);
            return true;
        }
        bunlde.Unload(true);
        return false;
    }
    public static Texture LoadTexture(AssetBundle bunlde)
    {
        if (bunlde == null)
            return null;
        Texture tex = bunlde.mainAsset as Texture;
        if (tex == null)
        {
            string[] Names = bunlde.GetAllAssetNames();
            if (Names != null && Names.Length > 0)
            {
                tex = bunlde.LoadAsset(Names[0], typeof(Texture)) as Texture;
            }
        }
        return tex;
    }
}