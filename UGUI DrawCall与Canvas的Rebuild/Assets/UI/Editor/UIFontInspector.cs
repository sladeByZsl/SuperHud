//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

// Dynamic font support contributed by the NGUI community members:
// Unisip, zh4ox, Mudwiz, Nicki, DarkMagicCK.

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Inspector class used to view and edit UIFonts.
/// </summary>

[CustomEditor(typeof(UIFont))]
public class UIFontInspector : Editor
{
	enum View
	{
		Nothing,
		Atlas,
		Font,
	}

	enum FontType
	{
		Normal,
		Reference,
		Dynamic,
	}

	static View mView = View.Font;
	static bool mUseShader = false;

	UIFont mFont;
	FontType mType = FontType.Normal;
	UIFont mReplacement = null;
	string mSymbolSequence = "";
	string mSymbolSprite = "";
	//BMSymbol mSelectedSymbol = null;

	public override bool HasPreviewGUI () { return mView != View.Nothing; }

	void OnSelectFont (MonoBehaviour obj)
	{
		// Undo doesn't work correctly in this case... so I won't bother.
		//NGUIEditorTools.RegisterUndo("Font Change");
		//NGUIEditorTools.RegisterUndo("Font Change", mFont);

		mFont.replacement = obj as UIFont;
		mReplacement = mFont.replacement;
		UnityEditor.EditorUtility.SetDirty(mFont);
		if (mReplacement == null) mType = FontType.Normal;
	}

	void OnSelectAtlas (MonoBehaviour obj)
	{
		if (mFont != null)
		{
            HUDEditorTools.RegisterUndo("Font Atlas", mFont);
			MarkAsChanged();
		}
	}

	void MarkAsChanged ()
	{
	}

	public override void OnInspectorGUI ()
	{
		mFont = target as UIFont;
		EditorGUIUtility.LookLikeControls(80f);

        HUDEditorTools.DrawSeparator();

		if (mFont.replacement != null)
		{
			mType = FontType.Reference;
			mReplacement = mFont.replacement;
		}
		else if (mFont.dynamicFont != null)
		{
			mType = FontType.Dynamic;
		}

		GUILayout.BeginHorizontal();
		FontType fontType = (FontType)EditorGUILayout.EnumPopup("Font Type", mType);
		GUILayout.Space(18f);
		GUILayout.EndHorizontal();

		if (mType != fontType)
		{
			if (fontType == FontType.Normal)
			{
				OnSelectFont(null);
			}
			else
			{
				mType = fontType;
			}

			if (mType != FontType.Dynamic && mFont.dynamicFont != null)
				mFont.dynamicFont = null;
		}

		if (mType == FontType.Reference)
		{
			ComponentSelector.Draw<UIFont>(mFont.replacement, OnSelectFont);

            HUDEditorTools.DrawSeparator();
			EditorGUILayout.HelpBox("You can have one font simply point to " +
				"another one. This is useful if you want to be " +
				"able to quickly replace the contents of one " +
				"font with another one, for example for " +
				"swapping an SD font with an HD one, or " +
				"replacing an English font with a Chinese " +
				"one. All the labels referencing this font " +
				"will update their references to the new one.", MessageType.Info);

			if (mReplacement != mFont && mFont.replacement != mReplacement)
			{
                HUDEditorTools.RegisterUndo("Font Change", mFont);
				mFont.replacement = mReplacement;
				UnityEditor.EditorUtility.SetDirty(mFont);
			}
			return;
		}
		else if (mType == FontType.Dynamic)
		{
#if UNITY_3_5
			EditorGUILayout.HelpBox("Dynamic fonts require Unity 4.0 or higher.", MessageType.Error);
#else
            HUDEditorTools.DrawSeparator();
			Font fnt = EditorGUILayout.ObjectField("TTF Font", mFont.dynamicFont, typeof(Font), false) as Font;
			
			if (fnt != mFont.dynamicFont)
			{
                HUDEditorTools.RegisterUndo("Font change", mFont);
				mFont.dynamicFont = fnt;
			}

			GUILayout.BeginHorizontal();
			int size = EditorGUILayout.IntField("Size", mFont.dynamicFontSize, GUILayout.Width(120f));
			FontStyle style = (FontStyle)EditorGUILayout.EnumPopup(mFont.dynamicFontStyle);
			GUILayout.Space(18f);
			GUILayout.EndHorizontal();

			if (size != mFont.dynamicFontSize)
			{
                HUDEditorTools.RegisterUndo("Font change", mFont);
				mFont.dynamicFontSize = size;
			}

			if (style != mFont.dynamicFontStyle)
			{
                HUDEditorTools.RegisterUndo("Font change", mFont);
				mFont.dynamicFontStyle = style;
			}

			Material mat = EditorGUILayout.ObjectField("Material", mFont.material, typeof(Material), false) as Material;

			if (mFont.material != mat)
			{
                HUDEditorTools.RegisterUndo("Font Material", mFont);
				mFont.material = mat;
			}
#endif
		}
		else
		{
            HUDEditorTools.DrawSeparator();
            
			{
				// No atlas specified -- set the material and texture rectangle directly
				Material mat = EditorGUILayout.ObjectField("Material", mFont.material, typeof(Material), false) as Material;

				if (mFont.material != mat)
				{
                    HUDEditorTools.RegisterUndo("Font Material", mFont);
					mFont.material = mat;
				}
			}

			// For updating the font's data when importing from an external source, such as the texture packer
			bool resetWidthHeight = false;

			if (mFont.material != null)
			{
				TextAsset data = EditorGUILayout.ObjectField("Import Data", null, typeof(TextAsset), false) as TextAsset;

				if (data != null)
				{
                    HUDEditorTools.RegisterUndo("Import Font Data", mFont);
					mFont.MarkAsDirty();
					resetWidthHeight = true;
				}
			}            
		}

		// The font must be valid at this point for the rest of the options to show up
		if (mFont.isDynamic)
		{
			// Font spacing
			GUILayout.BeginHorizontal();
			{
				EditorGUIUtility.LookLikeControls(0f);
				GUILayout.Label("Spacing", GUILayout.Width(60f));
				GUILayout.Label("X", GUILayout.Width(12f));
				int x = EditorGUILayout.IntField(mFont.horizontalSpacing);
				GUILayout.Label("Y", GUILayout.Width(12f));
				int y = EditorGUILayout.IntField(mFont.verticalSpacing);
				GUILayout.Space(18f);
				EditorGUIUtility.LookLikeControls(80f);

				if (mFont.horizontalSpacing != x || mFont.verticalSpacing != y)
				{
                    HUDEditorTools.RegisterUndo("Font Spacing", mFont);
					mFont.horizontalSpacing = x;
					mFont.verticalSpacing = y;
				}
			}
			GUILayout.EndHorizontal();
            
			EditorGUILayout.Space();
		}

		// Preview option
		if (!mFont.isDynamic)
		{
			GUILayout.BeginHorizontal();
			{
				mView = (View)EditorGUILayout.EnumPopup("Preview", mView);
				GUILayout.Label("Shader", GUILayout.Width(45f));
				mUseShader = EditorGUILayout.Toggle(mUseShader, GUILayout.Width(20f));
			}
			GUILayout.EndHorizontal();
		}        
	}

	/// <summary>
	/// "New Sprite" selection.
	/// </summary>

	void SelectSymbolSprite (string spriteName)
	{
		mSymbolSprite = spriteName;
		Repaint();
	}

	/// <summary>
	/// Existing sprite selection.
	/// </summary>

	void ChangeSymbolSprite (string spriteName)
	{
	}

	/// <summary>
	/// Draw the font preview window.
	/// </summary>

	public override void OnPreviewGUI (Rect rect, GUIStyle background)
	{
		mFont = target as UIFont;
		if (mFont == null) return;
		Texture2D tex = mFont.texture;

		if (mView != View.Nothing && tex != null)
		{
			Material m = (mUseShader ? mFont.material : null);

			if (mView == View.Font)
			{
				Rect outer = new Rect(mFont.uvRect);
				Rect uv = outer;

				outer = HUDMath.ConvertToPixels(outer, tex.width, tex.height, true);

                HUDEditorTools.DrawSprite(tex, rect, outer, outer, uv, Color.white, m);
			}
			else
			{
				Rect outer = new Rect(0f, 0f, 1f, 1f);
				Rect inner = new Rect(mFont.uvRect);
				Rect uv = outer;

				outer = HUDMath.ConvertToPixels(outer, tex.width, tex.height, true);
				inner = HUDMath.ConvertToPixels(inner, tex.width, tex.height, true);

                HUDEditorTools.DrawSprite(tex, rect, outer, inner, uv, Color.white, m);
			}
		}
	}

	/// <summary>
	/// Sprite selection callback.
	/// </summary>

	void SelectSprite (string spriteName)
	{
        HUDEditorTools.RegisterUndo("Font Sprite", mFont);
		mFont.spriteName = spriteName;
		Repaint();
	}
}
