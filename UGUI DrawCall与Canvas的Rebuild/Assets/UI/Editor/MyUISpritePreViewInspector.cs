
//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Inspector class used to edit UISprites.
/// </summary>

[CustomEditor(typeof(MyUISpritePreView))]
public class MyUISpritePreViewInspector : Editor
{
    protected MyUISpritePreView mSprite;

    UISpriteInfo m_sprite;
    bool m_bDirty = false;
	
	void OnDisable()
	{
		if( m_bDirty )
		{
			UISpriteInfo sprite = AtlasMng_Editor.instance.GetSprite(m_sprite.name);
			if (EditorUtility.DisplayDialog("提示", "你确定要保存当前的修改么？", "OK", "cancel"))
			{ 
				// 修改吧
				//sprite.Copy(m_sprite);
				AtlasMng_Editor.instance.SaveAltasCfg();
			}
			else
			{
				if( sprite != null )
				{
					sprite.Copy(m_sprite);  // 撤消修改
				}
			}
			MarkSpriteAsDirty();
			m_bDirty = false;
		}
	}

	static bool  IsSameRect( Rect a, Rect b )
	{
		if( a == null || b == null )
			return a == b;
		int  nLeftA = (int)a.xMin;
		int  nTopA = (int)a.yMin;
		int  nRightA = (int)(a.xMax + 0.5f);
		int  nBottomA = (int)(b.yMax + 0.5);
		int  nLeftB = (int)b.xMin;
		int  nTopB = (int)b.yMin;
		int  nRightB = (int)(b.xMax + 0.5f);
		int  nBottomB = (int)(b.yMax + 0.5f);
		return nLeftA == nLeftB	&& nTopA == nTopB && nRightA == nRightB	&& nBottomA == nBottomB;
	}

	static bool  IsChangeSprite(UISpriteInfo a, UISpriteInfo b)
	{
		if( a == null || b == null )
			return a == b;
		if( !IsSameRect(a.outer, b.outer) )
			return true;
		if( !IsSameRect(a.inner, b.inner) )
			return true;
		int  nLeftA   = Mathf.RoundToInt(a.paddingLeft * a.outer.width);
		int  nTopA    = Mathf.RoundToInt(a.paddingTop * a.outer.height);
		int  nRightA  = Mathf.RoundToInt(a.paddingRight * a.outer.width);
		int  nBottomA = Mathf.RoundToInt(a.paddingBottom * a.outer.height);
		
		int  nLeftB   = Mathf.RoundToInt(b.paddingLeft * b.outer.width);
		int  nTopB    = Mathf.RoundToInt(b.paddingTop * b.outer.height);
		int  nRightB  = Mathf.RoundToInt(b.paddingRight * b.outer.width);
		int  nBottomB = Mathf.RoundToInt(b.paddingBottom * b.outer.height);
		if( nLeftA != nLeftB || nTopA != nTopB || nRightA != nRightB || nBottomA != nBottomB )
			return true;
		return false;
	}
	
	void MarkSpriteAsDirty ()
	{
		if( m_sprite == null )
			return ;
		if( string.IsNullOrEmpty(m_sprite.name) )
			return ;				
	}
    
    /// <summary>
    /// Draw the atlas and sprite selection fields.
    /// </summary>

    protected virtual bool DrawProperties ()
	{
        mSprite = target as MyUISpritePreView;
		// 属性绘制
		string  spriteName = mSprite != null ? mSprite.name : "";
        if (m_sprite == null || m_sprite.name != spriteName)
        {
            UISpriteInfo sprite = AtlasMng_Editor.instance.GetSprite(spriteName);
            if (m_bDirty)
            {
				if( m_sprite != null )
				{
					UISpriteInfo oldSprite = AtlasMng_Editor.instance.GetSprite(m_sprite.name);
					if( oldSprite != null )
					{
						oldSprite.Copy(m_sprite);  // 自动取消修改
						MarkSpriteAsDirty();
					}
				}
                m_bDirty = false;
			}
            if( sprite != null )
                m_sprite = sprite.Clone();
        }
		// Sprite selection drop-down list
		GUILayout.BeginHorizontal();
		{
			if( string.IsNullOrEmpty(spriteName) )
				spriteName = "";
			GUILayout.Label(spriteName, GUILayout.Width(180f));
		}
		GUILayout.EndHorizontal();

		return true;
	}

    public override void OnInspectorGUI()
	{
		EditorGUIUtility.LookLikeControls(80f);
		EditorGUILayout.Space();
		
		// Check to see if we can draw the widget's default properties to begin with
		if (DrawProperties())
		{
			// Draw all common properties next
			DrawExtraProperties();
		}
	}
    void  DrawEditData()
    {
        Color blue = new Color(0f, 0.7f, 1f, 1f);
        Color green = new Color(0.4f, 1f, 0f, 1f);
        // 绘制预览面板
        if (mSprite == null || !mSprite.isValid) return;
        if (m_sprite == null)
            return;
        UISpriteInfo sprite = AtlasMng_Editor.instance.GetSprite(mSprite.name);
        if (sprite == null)
            return;
        UITexAtlas atlas = AtlasMng_Editor.instance.GetAltasBySpriteName(mSprite.name);
        if (atlas == null)
            return;

        Rect inner = sprite.inner;
        Rect outer = sprite.outer;

        if (atlas.coordinates == UITexAtlas.Coordinates.Pixels)
        {
            GUI.backgroundColor = green;

            // 渲染编辑对象
            outer = HUDEditorTools.IntRect("Dimensions", sprite.outer);

            Vector4 border = new Vector4(
                sprite.inner.xMin - sprite.outer.xMin,
                sprite.inner.yMin - sprite.outer.yMin,
                sprite.outer.xMax - sprite.inner.xMax,
                sprite.outer.yMax - sprite.inner.yMax);

            // 渲染编辑对象
            GUI.backgroundColor = blue;
            border = HUDEditorTools.IntPadding("Border", border);
            GUI.backgroundColor = Color.white;

            inner.xMin = sprite.outer.xMin + border.x;
            inner.yMin = sprite.outer.yMin + border.y;
            inner.xMax = sprite.outer.xMax - border.z;
            inner.yMax = sprite.outer.yMax - border.w;
        }
        else
        {
            // Draw the inner and outer rectangle dimensions
            GUI.backgroundColor = green;
            outer = EditorGUILayout.RectField("Outer Rect", sprite.outer);
            GUI.backgroundColor = blue;
            inner = EditorGUILayout.RectField("Inner Rect", sprite.inner);
            GUI.backgroundColor = Color.white;
        }

		if (outer.xMax < outer.xMin) outer.xMax = outer.xMin;
		if (outer.yMax < outer.yMin) outer.yMax = outer.yMin;

		if (outer != sprite.outer)
		{
			float x = outer.xMin - sprite.outer.xMin;
			float y = outer.yMin - sprite.outer.yMin;

			inner.x += x;
			inner.y += y;
		}

		// Sanity checks to ensure that the inner rect is always inside the outer
		inner.xMin = Mathf.Clamp(inner.xMin, outer.xMin, outer.xMax);
		inner.xMax = Mathf.Clamp(inner.xMax, outer.xMin, outer.xMax);
		inner.yMin = Mathf.Clamp(inner.yMin, outer.yMin, outer.yMax);
		inner.yMax = Mathf.Clamp(inner.yMax, outer.yMin, outer.yMax);
						
		bool changed = false;
						
		if (sprite.inner != inner || sprite.outer != outer)
		{
			sprite.inner = inner;
			sprite.outer = outer;
			MarkSpriteAsDirty();  // 应用修改
            changed = true;
            m_bDirty = true;
		}

		EditorGUILayout.Separator();

		if (atlas.coordinates == UITexAtlas.Coordinates.Pixels)
		{
			int left	= Mathf.RoundToInt(sprite.paddingLeft	* sprite.outer.width);
			int right	= Mathf.RoundToInt(sprite.paddingRight	* sprite.outer.width);
			int top		= Mathf.RoundToInt(sprite.paddingTop	* sprite.outer.height);
			int bottom	= Mathf.RoundToInt(sprite.paddingBottom	* sprite.outer.height);

            HUDEditorTools.IntVector a = HUDEditorTools.IntPair("Padding", "Left", "Top", left, top);
            HUDEditorTools.IntVector b = HUDEditorTools.IntPair(null, "Right", "Bottom", right, bottom);

			if (changed || a.x != left || a.y != top || b.x != right || b.y != bottom)
			{
				sprite.paddingLeft		= a.x / sprite.outer.width;
				sprite.paddingTop		= a.y / sprite.outer.height;
				sprite.paddingRight		= b.x / sprite.outer.width;
				sprite.paddingBottom	= b.y / sprite.outer.height;
				MarkSpriteAsDirty();  // 应用修改
                m_bDirty = true;
				changed = true;
			}
		}
		else
		{
			// Create a button that can make the coordinates pixel-perfect on click
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Correction", GUILayout.Width(75f));

				Rect corrected0 = outer;
				Rect corrected1 = inner;

				if (atlas.coordinates == UITexAtlas.Coordinates.Pixels)
				{
					corrected0 = HUDMath.MakePixelPerfect(corrected0);
					corrected1 = HUDMath.MakePixelPerfect(corrected1);
				}
				else
				{
                    corrected0 = HUDMath.MakePixelPerfect(corrected0, atlas.texWidth, atlas.texHeight);
                    corrected1 = HUDMath.MakePixelPerfect(corrected1, atlas.texWidth, atlas.texHeight);
				}

				if (corrected0 == sprite.outer && corrected1 == sprite.inner)
				{
					GUI.color = Color.grey;
					GUILayout.Button("Make Pixel-Perfect");
					GUI.color = Color.white;
				}
				else if (GUILayout.Button("Make Pixel-Perfect"))
				{
					outer = corrected0;
					inner = corrected1;
					GUI.changed = true;
                    m_bDirty = true;
					changed = true;
				}
			}
			GUILayout.EndHorizontal();
		}

		if( changed )
		{
			m_bDirty = IsChangeSprite(m_sprite, sprite);
		}

        HUDEditorTools.DrawSeparator();
		GUILayout.BeginHorizontal();
        GUI.backgroundColor = m_bDirty ? Color.green : Color.white;
        if (GUILayout.Button("修改"))
        {
            if (m_bDirty && m_sprite != null)
            {
				m_sprite.Copy(sprite);
				MarkSpriteAsDirty();  // 应用修改
                AtlasMng_Editor.instance.SaveAltasCfg();
                m_bDirty = false;
            }
        }
		if (GUILayout.Button("撤消"))
		{
			sprite.Copy(m_sprite);
			MarkSpriteAsDirty();  // 应用修改
			m_bDirty = false;
		}
        GUI.backgroundColor = Color.white;
		GUILayout.EndHorizontal();


	    if (HUDEditorTools.previousSelection != null)
	    {
		    //NGUIEditorTools.DrawSeparator();

		    //GUI.backgroundColor = Color.green;

		    //if (GUILayout.Button("<< Return to " + NGUIEditorTools.previousSelection.name))
		    //{
			//    NGUIEditorTools.SelectPrevious();
		    //}
		    //GUI.backgroundColor = Color.white;
	    }
    }

	/// <summary>
	/// Sprites's custom properties based on the type.
	/// </summary>
	
	protected virtual void DrawExtraProperties ()
	{
		if( mSprite == null )
			return ;

        HUDEditorTools.DrawSeparator();

        DrawEditData();

		GUILayout.Space(4f);
	}
	
	/// <summary>
	/// All widgets have a preview.
	/// </summary>
	
	public override bool HasPreviewGUI () { return true; }
	
	/// <summary>
	/// Draw the sprite preview.
	/// </summary>
	
	public override void OnPreviewGUI (Rect rect, GUIStyle background)
	{
		// 绘制预览面板
		if (mSprite == null || !mSprite.isValid) return;

        if (m_sprite == null)
            return;

        UISpriteInfo sp = AtlasMng_Editor.instance.GetSprite(mSprite.name);
        if (sp == null)
            return;

		UITexAtlas   atlas = AtlasMng_Editor.instance.GetAltasBySpriteName(mSprite.name);
		if( atlas == null || atlas.m_material == null )
			return ;
		Texture2D tex = atlas.m_material.mainTexture as Texture2D;
		if (tex == null) return;

        Rect outer = new Rect(sp.outer);
        Rect inner = new Rect(sp.inner);
		Rect uv = outer;
		
		if (atlas.coordinates == UITexAtlas.Coordinates.Pixels)
		{
			uv = HUDMath.ConvertToTexCoords(outer, tex.width, tex.height);
		}
		else
		{
			outer = HUDMath.ConvertToPixels(outer, tex.width, tex.height, true);
			inner = HUDMath.ConvertToPixels(inner, tex.width, tex.height, true);
		}
        HUDEditorTools.DrawSprite(tex, rect, outer, inner, uv, new Color(1.0f, 1.0f, 1.0f, 1.0f));
	}
}
