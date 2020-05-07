
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

static public class NGUIMenu
{
    [MenuItem("HUD/打开新材质编辑器 #&m")]
    static public void OpenNewAtlasMaker()
    {
        EditorWindow.GetWindow<MyAtlasMaker>(false, "新材质编辑器", true);
    }
    [MenuItem("HUD/重新加载GIF动画")]
    static public void ReloadLoadSpriteGifMaker()
    {
        UISpriteGifManager.Instance.LoadXmlEditorMode();
    }
}
