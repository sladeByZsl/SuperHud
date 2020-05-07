using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

// 说明，这个代码来自NUIEditorTools，这里摘出来，是为了分离NGUI的代码

public class HUDEditorTools
{
    static Texture2D mWhiteTex;
    static Texture2D mBackdropTex;
    static Texture2D mContrastTex;
    static Texture2D mGradientTex;
    static GameObject mPrevious;

    static public Texture2D blankTexture
    {
        get
        {
            return EditorGUIUtility.whiteTexture;
        }
    }

    static public Texture2D backdropTexture
    {
        get
        {
            if (mBackdropTex == null)
                mBackdropTex = CreateCheckerTex(
new Color(0.1f, 0.1f, 0.1f, 0.5f),
new Color(0.2f, 0.2f, 0.2f, 0.5f));
            return mBackdropTex;
        }
    }
    static public Texture2D contrastTexture
    {
        get
        {
            if (mContrastTex == null)
                mContrastTex = CreateCheckerTex(
new Color(0f, 0.0f, 0f, 0.5f),
new Color(1f, 1f, 1f, 0.5f));
            return mContrastTex;
        }
    }

    static Texture2D CreateCheckerTex(Color c0, Color c1)
    {
        Texture2D tex = new Texture2D(16, 16);
        tex.name = "[Generated] Checker Texture";
        tex.hideFlags = HideFlags.DontSave;

        for (int y = 0; y < 8; ++y) for (int x = 0; x < 8; ++x) tex.SetPixel(x, y, c1);
        for (int y = 8; y < 16; ++y) for (int x = 0; x < 8; ++x) tex.SetPixel(x, y, c0);
        for (int y = 0; y < 8; ++y) for (int x = 8; x < 16; ++x) tex.SetPixel(x, y, c0);
        for (int y = 8; y < 16; ++y) for (int x = 8; x < 16; ++x) tex.SetPixel(x, y, c1);

        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return tex;
    }

    static public GameObject previousSelection { get { return mPrevious; } }

    static public string GetSelectionFolder()
    {
        if (Selection.activeObject != null)
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());

            if (!string.IsNullOrEmpty(path))
            {
                int dot = path.LastIndexOf('.');
                int slash = Mathf.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
                if (slash > 0) return (dot > slash) ? path.Substring(0, slash + 1) : path + "/";
            }
        }
        return "Assets/";
    }

    static public void DrawSeparator()
    {
        GUILayout.Space(12f);

        if (Event.current.type == EventType.Repaint)
        {
            Texture2D tex = blankTexture;
            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.color = new Color(0f, 0f, 0f, 0.25f);
            GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 4f), tex);
            GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 1f), tex);
            GUI.DrawTexture(new Rect(0f, rect.yMin + 9f, Screen.width, 1f), tex);
            GUI.color = Color.white;
        }
    }

    static bool MakeTextureReadable(string path, bool force)
    {
        if (string.IsNullOrEmpty(path)) return false;
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return false;

        TextureImporterSettings settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);

        if (force ||
            settings.mipmapEnabled ||
            !settings.readable ||
            settings.maxTextureSize < 4096 ||
            settings.filterMode != FilterMode.Point ||
            settings.wrapMode != TextureWrapMode.Clamp ||
            settings.npotScale != TextureImporterNPOTScale.None)
        {
            settings.mipmapEnabled = false;
            settings.readable = true;
            settings.maxTextureSize = 4096;
            settings.textureFormat = TextureImporterFormat.ARGB32;
            settings.filterMode = FilterMode.Point;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.npotScale = TextureImporterNPOTScale.None;

            ti.SetTextureSettings(settings);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
        return true;
    }

    static bool MakeTextureAnAtlas(string path, bool force)
    {
        if (string.IsNullOrEmpty(path)) return false;
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return false;

        TextureImporterSettings settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);

        if (force ||
            settings.readable ||
            settings.maxTextureSize < 4096 ||
            settings.wrapMode != TextureWrapMode.Clamp ||
            settings.npotScale != TextureImporterNPOTScale.ToNearest)
        {
            //settings.mipmapEnabled = true;
            settings.readable = false;
            settings.maxTextureSize = 4096;
            settings.textureFormat = TextureImporterFormat.RGBA32;
            settings.filterMode = FilterMode.Trilinear;
            settings.aniso = 4;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.npotScale = TextureImporterNPOTScale.ToNearest;

            ti.SetTextureSettings(settings);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
        return true;
    }

    static public Texture2D ImportTexture(string path, bool forInput, bool force)
    {
        if (!string.IsNullOrEmpty(path))
        {
            if (forInput) { if (!MakeTextureReadable(path, force)) return null; }
            else if (!MakeTextureAnAtlas(path, force)) return null;

            Texture2D tex = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return tex;
        }
        return null;
    }

    static public Rect IntRect(string prefix, Rect rect)
    {
        int left = Mathf.RoundToInt(rect.xMin);
        int top = Mathf.RoundToInt(rect.yMin);
        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);

        IntVector a = IntPair(prefix, "Left", "Top", left, top);
        IntVector b = IntPair(null, "Width", "Height", width, height);

        return new Rect(a.x, a.y, b.x, b.y);
    }

    static public Vector4 IntPadding(string prefix, Vector4 v)
    {
        int left = Mathf.RoundToInt(v.x);
        int top = Mathf.RoundToInt(v.y);
        int right = Mathf.RoundToInt(v.z);
        int bottom = Mathf.RoundToInt(v.w);

        IntVector a = IntPair(prefix, "Left", "Top", left, top);
        IntVector b = IntPair(null, "Right", "Bottom", right, bottom);

        return new Vector4(a.x, a.y, b.x, b.y);
    }

    public struct IntVector
    {
        public int x;
        public int y;
    }

    static public IntVector IntPair(string prefix, string leftCaption, string rightCaption, int x, int y)
    {
        GUILayout.BeginHorizontal();

        if (string.IsNullOrEmpty(prefix))
        {
            GUILayout.Space(82f);
        }
        else
        {
            GUILayout.Label(prefix, GUILayout.Width(74f));
        }

        EditorGUIUtility.LookLikeControls(48f);

        IntVector retVal;
        retVal.x = EditorGUILayout.IntField(leftCaption, x, GUILayout.MinWidth(30f));
        retVal.y = EditorGUILayout.IntField(rightCaption, y, GUILayout.MinWidth(30f));

        EditorGUIUtility.LookLikeControls(80f);

        GUILayout.EndHorizontal();
        return retVal;
    }

    static public bool DrawHeader(string text) { return DrawHeader(text, text); }

    static public bool DrawHeader(string text, string key)
    {
        bool state = EditorPrefs.GetBool(key, false);

        GUILayout.Space(3f);
        if (!state) GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
        GUILayout.BeginHorizontal();
        GUILayout.Space(3f);

        GUI.changed = false;
        if (!GUILayout.Toggle(true, "<b><size=11>" + text + "</size></b>", "dragtab")) state = !state;
        if (GUI.changed) EditorPrefs.SetBool(key, state);

        GUILayout.Space(2f);
        GUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;
        if (!state) GUILayout.Space(3f);
        return state;
    }

    static public void DrawTiledTexture(Rect rect, Texture tex)
    {
        GUI.BeginGroup(rect);
        {
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);

            for (int y = 0; y < height; y += tex.height)
            {
                for (int x = 0; x < width; x += tex.width)
                {
                    GUI.DrawTexture(new Rect(x, y, tex.width, tex.height), tex);
                }
            }
        }
        GUI.EndGroup();
    }

    public static void DrawSprite(Texture2D tex, Rect rect, Rect outer, Rect inner, Rect uv, Color color, Material mat)
    {
        // Create the texture rectangle that is centered inside rect.
        Rect outerRect = rect;
        outerRect.width = outer.width;
        outerRect.height = outer.height;

        if (outerRect.width > 0f)
        {
            float f = rect.width / outerRect.width;
            outerRect.width *= f;
            outerRect.height *= f;
        }

        if (rect.height > outerRect.height)
        {
            outerRect.y += (rect.height - outerRect.height) * 0.5f;
        }
        else if (outerRect.height > rect.height)
        {
            float f = rect.height / outerRect.height;
            outerRect.width *= f;
            outerRect.height *= f;
        }

        if (rect.width > outerRect.width) outerRect.x += (rect.width - outerRect.width) * 0.5f;

        // Draw the background
        DrawTiledTexture(outerRect, backdropTexture);

        // Draw the sprite
        GUI.color = color;

        if (mat == null)
        {
            GUI.DrawTextureWithTexCoords(outerRect, tex, uv, true);
        }
        else
        {
            // NOTE: There is an issue in Unity that prevents it from clipping the drawn preview
            // using BeginGroup/EndGroup, and there is no way to specify a UV rect... le'suq.
            UnityEditor.EditorGUI.DrawPreviewTexture(outerRect, tex, mat);
        }

        // Draw the border indicator lines
        GUI.BeginGroup(outerRect);
        {
            tex = contrastTexture;
            GUI.color = Color.white;

            if (inner.xMin != outer.xMin)
            {
                float x0 = (inner.xMin - outer.xMin) / outer.width * outerRect.width - 1;
                DrawTiledTexture(new Rect(x0, 0f, 1f, outerRect.height), tex);
            }

            if (inner.xMax != outer.xMax)
            {
                float x1 = (inner.xMax - outer.xMin) / outer.width * outerRect.width - 1;
                DrawTiledTexture(new Rect(x1, 0f, 1f, outerRect.height), tex);
            }

            if (inner.yMin != outer.yMin)
            {
                float y0 = (inner.yMin - outer.yMin) / outer.height * outerRect.height - 1;
                DrawTiledTexture(new Rect(0f, y0, outerRect.width, 1f), tex);
            }

            if (inner.yMax != outer.yMax)
            {
                float y1 = (inner.yMax - outer.yMin) / outer.height * outerRect.height - 1;
                DrawTiledTexture(new Rect(0f, y1, outerRect.width, 1f), tex);
            }
        }
        GUI.EndGroup();

        // Draw the lines around the sprite
        Handles.color = Color.black;
        Handles.DrawLine(new Vector3(outerRect.xMin, outerRect.yMin), new Vector3(outerRect.xMin, outerRect.yMax));
        Handles.DrawLine(new Vector3(outerRect.xMax, outerRect.yMin), new Vector3(outerRect.xMax, outerRect.yMax));
        Handles.DrawLine(new Vector3(outerRect.xMin, outerRect.yMin), new Vector3(outerRect.xMax, outerRect.yMin));
        Handles.DrawLine(new Vector3(outerRect.xMin, outerRect.yMax), new Vector3(outerRect.xMax, outerRect.yMax));

        // Sprite size label
        string text = string.Format("Sprite Size: {0}x{1}",
            Mathf.RoundToInt(Mathf.Abs(outer.width)),
            Mathf.RoundToInt(Mathf.Abs(outer.height)));
        EditorGUI.DropShadowLabel(GUILayoutUtility.GetRect(Screen.width, 18f), text);
    }

    public static void DrawSprite(Texture2D tex, Rect rect, Rect outer, Rect inner, Rect uv, Color color)
    {
        DrawSprite(tex, rect, outer, inner, uv, color, null);
    }


    static public void RegisterUndo(string name, params Object[] objects)
    {
        if (objects != null && objects.Length > 0)
        {
            foreach (Object obj in objects)
            {
                if (obj == null) continue;
                Undo.RegisterUndo(obj, name);
                EditorUtility.SetDirty(obj);
            }
        }
        else
        {
            Undo.RegisterSceneUndo(name);
        }
    }
}
