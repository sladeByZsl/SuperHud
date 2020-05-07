//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

// Dynamic font support contributed by the NGUI community members:
// Unisip, zh4ox, Mudwiz, Nicki, DarkMagicCK.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// UIFont contains everything needed to be able to print text.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Font")]
public class UIFont : MonoBehaviour
{
	public enum Alignment
	{
		Left,
		Center,
		Right,
	}

	public enum SymbolStyle
	{
		None,
		Uncolored,
		Colored,
	}

	[HideInInspector][SerializeField] Material mMat;
	[HideInInspector][SerializeField] Rect mUVRect = new Rect(0f, 0f, 1f, 1f);
	[HideInInspector][SerializeField] int mSpacingX = 0;
	[HideInInspector][SerializeField] int mSpacingY = 0;
	[HideInInspector][SerializeField] UIFont mReplacement;
	[HideInInspector][SerializeField] float mPixelSize = 1f;
    

	// Used for dynamic fonts
	[HideInInspector][SerializeField] Font mDynamicFont;
	[HideInInspector][SerializeField] int mDynamicFontSize = 16;
	[HideInInspector][SerializeField] FontStyle mDynamicFontStyle = FontStyle.Normal;
	[HideInInspector][SerializeField] float mDynamicFontOffset = 0f;

	// Cached value
	int mPMA = -1;
	bool mSpriteSet = false;
                
	/// <summary>
	/// Get or set the material used by this font.
	/// </summary>

	public Material material
	{
		get
		{
			if (mReplacement != null) return mReplacement.material;
            
			if (mMat != null)
			{
				if (mDynamicFont != null && mMat != mDynamicFont.material)
				{
					mMat.mainTexture = mDynamicFont.material.mainTexture;
				}
				return mMat;
			}

			if (mDynamicFont != null)
			{
				return mDynamicFont.material;
			}
			return null;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.material = value;
			}
			else if (mMat != value)
			{
				mPMA = -1;
				mMat = value;
				MarkAsDirty();
			}
		}
	}

	/// <summary>
	/// Pixel size is a multiplier applied to label dimensions when performing MakePixelPerfect() pixel correction.
	/// Most obvious use would be on retina screen displays. The resolution doubles, but with UIRoot staying the same
	/// for layout purposes, you can still get extra sharpness by switching to an HD font that has pixel size set to 0.5.
	/// </summary>

	public float pixelSize
	{
		get
		{
			if (mReplacement != null) return mReplacement.pixelSize;
			return mPixelSize;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.pixelSize = value;
			}
			else
			{
				float val = Mathf.Clamp(value, 0.25f, 4f);

				if (mPixelSize != val)
				{
					mPixelSize = val;
					MarkAsDirty();
				}
			}
		}
	}
    
	/// <summary>
	/// Convenience function that returns the texture used by the font.
	/// </summary>

	public Texture2D texture
	{
		get
		{
			if (mReplacement != null) return mReplacement.texture;
			Material mat = material;
			return (mat != null) ? mat.mainTexture as Texture2D : null;
		}
	}

	/// <summary>
	/// Offset and scale applied to all UV coordinates.
	/// </summary>

	public Rect uvRect
	{
		get
		{
			if (mReplacement != null) return mReplacement.uvRect;
            
			return mUVRect;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.uvRect = value;
			}
		}
	}

    /// <summary>
    /// Sprite used by the font, if any.
    /// </summary>

    public string spriteName
    {
        get
        {
            return (mReplacement != null) ? mReplacement.spriteName : string.Empty;
        }
        set
        {
            if (mReplacement != null)
            {
                mReplacement.spriteName = value;
            }
        }
    }

    /// <summary>
    /// Horizontal spacing applies to characters. If positive, it will add extra spacing between characters. If negative, it will make them be closer together.
    /// </summary>

    public int horizontalSpacing
	{
		get
		{
			return (mReplacement != null) ? mReplacement.horizontalSpacing : mSpacingX;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.horizontalSpacing = value;
			}
			else if (mSpacingX != value)
			{
				mSpacingX = value;
				MarkAsDirty();
			}
		}
	}

	/// <summary>
	/// Vertical spacing applies to lines. If positive, it will add extra spacing between lines. If negative, it will make them be closer together.
	/// </summary>

	public int verticalSpacing
	{
		get
		{
			return (mReplacement != null) ? mReplacement.verticalSpacing : mSpacingY;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.verticalSpacing = value;
			}
			else if (mSpacingY != value)
			{
				mSpacingY = value;
				MarkAsDirty();
			}
		}
	}

	/// <summary>
	/// Whether this is a valid font.
	/// </summary>    
	public bool isValid { get { return mDynamicFont != null; } }

	/// <summary>
	/// Pixel-perfect size of this font.
	/// </summary>

	public int size { get { return (mReplacement != null) ? mReplacement.size : (isDynamic ? mDynamicFontSize : 0); } }
    
	/// <summary>
	/// Setting a replacement atlas value will cause everything using this font to use the replacement font instead.
	/// Suggested use: set up all your widgets to use a dummy font that points to the real font. Switching that font to
	/// another one (for example an eastern language one) is then a simple matter of setting this field on your dummy font.
	/// </summary>

	public UIFont replacement
	{
		get
		{
			return mReplacement;
		}
		set
		{
			UIFont rep = value;
			if (rep == this) rep = null;

			if (mReplacement != rep)
			{
				if (rep != null && rep.replacement == this) rep.replacement = null;
				if (mReplacement != null) MarkAsDirty();
				mReplacement = rep;
				MarkAsDirty();
			}
		}
	}

	/// <summary>
	/// Whether the font is dynamic.
	/// </summary>

	public bool isDynamic { get { return (mDynamicFont != null); } }

	/// <summary>
	/// Get or set the dynamic font source.
	/// </summary>

	public Font dynamicFont
	{
		get
		{
			return (mReplacement != null) ? mReplacement.dynamicFont : mDynamicFont;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.dynamicFont = value;
			}
			else if (mDynamicFont != value)
			{
				if (mDynamicFont != null) material = null;
				mDynamicFont = value;
				MarkAsDirty();
			}
		}
	}

	/// <summary>
	/// Get or set the default size of the dynamic font.
	/// </summary>

	public int dynamicFontSize
	{
		get
		{
			return (mReplacement != null) ? mReplacement.dynamicFontSize : mDynamicFontSize;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.dynamicFontSize = value;
			}
			else
			{
				value = Mathf.Clamp(value, 4, 128);

				if (mDynamicFontSize != value)
				{
					mDynamicFontSize = value;
					MarkAsDirty();
				}
			}
		}
	}

	/// <summary>
	/// Get or set the dynamic font's style.
	/// </summary>

	public FontStyle dynamicFontStyle
	{
		get
		{
			return (mReplacement != null) ? mReplacement.dynamicFontStyle : mDynamicFontStyle;
		}
		set
		{
			if (mReplacement != null)
			{
				mReplacement.dynamicFontStyle = value;
			}
			else if (mDynamicFontStyle != value)
			{
				mDynamicFontStyle = value;
				MarkAsDirty();
			}
		}
	}

    public float dynamicFontOffset
    {
        get { return mDynamicFontOffset; }
    }
    
	/// <summary>
	/// Helper function that determines whether the font uses the specified one, taking replacements into account.
	/// </summary>

	bool References (UIFont font)
	{
		if (font == null) return false;
		if (font == this) return true;
		return (mReplacement != null) ? mReplacement.References(font) : false;
	}
    
	Texture dynamicTexture
	{
		get
		{
			if (mReplacement) return mReplacement.dynamicTexture;
			if (isDynamic) return mDynamicFont.material.mainTexture;
			return null;
		}
	}

	/// <summary>
	/// Refresh all labels that use this font.
	/// </summary>
    /// 

    public static int s_nFontVersion = 0;

	public void MarkAsDirty ()
	{
#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
		if (mReplacement != null) mReplacement.MarkAsDirty();
		RecalculateDynamicOffset();

        ++s_nFontVersion;        
	}

	/// <summary>
	/// BUG: Unity's CharacterInfo.vert.y makes no sense at all. It changes depending on the imported font's size,
	/// even though it shouldn't, since I overwrite the requested character size here. In order to calculate the
	/// actual proper offset that needs to be applied to this weird value, I get the coordinates of the 'j' glyph
	/// and then determine the difference between the glyph's position and the font's size.
	/// </summary>

	public bool RecalculateDynamicOffset()
	{
		if (mDynamicFont != null)
		{
			CharacterInfo j;
			mDynamicFont.RequestCharactersInTexture("j", mDynamicFontSize, mDynamicFontStyle);
			mDynamicFont.GetCharacterInfo('j', out j, mDynamicFontSize, mDynamicFontStyle);
			float offset = (mDynamicFontSize + j.vert.yMax);
			
			if (!float.Equals(mDynamicFontOffset, offset))
			{
				mDynamicFontOffset = offset;
				return true;
			}
		}
		return false;
	}

    public void PrepareQueryText(string szText)
    {
        if(mDynamicFont != null)
            mDynamicFont.RequestCharactersInTexture(szText, mDynamicFontSize, mDynamicFontStyle);
    }

    // 功能：得到字体的信息
    public bool GetCharacterInfo(char ch, ref CharacterInfo chInfo)
    {
        if (mDynamicFont != null)
        {
            return mDynamicFont.GetCharacterInfo(ch, out chInfo, mDynamicFontSize, mDynamicFontStyle);
        }
        return false;
    }

    // 功能：得到字符的宽度
    public int  GetCharWidth(char ch)
    {
        if (mDynamicFont != null)
        {
            if (!mDynamicFont.HasCharacter(ch))
            {
                string szText = new string(ch, 1);
                mDynamicFont.RequestCharactersInTexture(szText, mDynamicFontSize, mDynamicFontStyle);
            }
            if (mDynamicFont.GetCharacterInfo(ch, out mChar, mDynamicFontSize, mDynamicFontStyle))
                return mChar.advance;
        }
        return 0;
    }

    // 功能：得到字符的高度
    public int GetFontHeight()
    {
        int fs = size;
        int lineHeight = (fs + mSpacingY);
        return lineHeight;
    }
    
	static CharacterInfo mChar;           
}

