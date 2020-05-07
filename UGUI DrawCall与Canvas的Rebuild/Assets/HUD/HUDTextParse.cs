using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

///////////////////////////////////////////////////////////
//
//  Written by              ：laishikai
//  Copyright(C)            ：成都博瑞梦工厂
//  ------------------------------------------------------
//  功能描述                ：文本控制符解释模块
//
///////////////////////////////////////////////////////////

public enum UIFontUnitType
{
    UnitType_Char,  // 字符
    UnitType_Icon,  // 图标
    UnitType_Gif,   // 动画
    UnitType_Link,  // 连接
    UnitType_Space, // 占位符
    UnitType_Enter, // 换行符
}

public enum UIHyperlinkType
{
    None,    // 没有下划线
    Underline, // 有下划线
    Underline_Flash, // 下划线闪烁
}


public class UIFontUnit
{
    public int m_nX;     // 显示的相对坐标X
    public int m_nY;     // 显示的相对坐标Y
    public int m_nWidth;
    public int m_nHeight;
    public int m_nLineHeight;
    public UIFontUnitType m_type; // 类型
    public int m_nIconID;   // 图标ID或动画ID
    public int m_nObjIndex; // 对象索引
    public int m_nRow;      // 当前所在的行
    public int m_nCharPos;  // 字符串的位置
    public char m_ch;
    public Color32 m_color1;
    public Color32 m_color2;
    public Color32 m_color3;
    public Color32 m_color4;
    public bool m_bCustomColor;
    public bool m_bZoom;

    public int right { get { return m_nX + m_nWidth; } }
    public int bottom { get { return m_nY + m_nHeight; } }
    public int midX { get { return m_nX + m_nWidth / 2; } }
}

public class UIFontCustomObject
{
    public UIFontUnitType m_type;  // 
    public int m_nIconID;          // 图标ID或动画ID
    public string m_szLink;
    public string m_szCustomDesc;  // 上层自定义的信息
    public UIHyperlinkType m_HyperlinkType = UIHyperlinkType.None; // 超连接的类型
}


struct HUDCharInfo
{
    public UIFontUnitType CharType;
    public bool bChar;
    public bool bCustomColor;
    public char ch;      // 字符
    public int SpriteID; // 图片ID
    public short SpriteWidth;
    public short SpriteHeight;
    public Color32 CustomColor;
    public int nX;
    public int nY;
    public int nLine;
    public int LineH; // 当前行高
};

class HUDTextParse
{
    public HUDCharInfo[] m_Sprites;
    public short[] LineHeight;
    public int m_SpriteCount;
    public string m_szText;
    char[] m_ValidChars;
    int m_nCharCount;
    bool m_bCharDirty = false;
    BetterList<Color32> m_colors;
    List<UIFontCustomObject> m_CustomObj;

    static HUDTextParse s_pTextParse;
    public static HUDTextParse  Instance
    {
        get
        {
            if(s_pTextParse == null)
            {
                s_pTextParse = new HUDTextParse();
                s_pTextParse.Init();
            }
            return s_pTextParse;
        }
    }
    void  Init()
    {
        m_Sprites = new HUDCharInfo[100]; // 最多显示128个字符
        m_ValidChars = new char[100];
        LineHeight = new short[100];
        m_colors = new BetterList<Color32>(); // 不会有这么多的
        m_CustomObj = new List<UIFontCustomObject>();
    }
    void  AutoGrow()
    {
        int nSize = m_Sprites.Length * 2;
        HUDCharInfo[] Sprties = new HUDCharInfo[nSize];
        char[] Chars = new char[nSize];
        for(int i = 0; i<m_SpriteCount; ++i)
        {
            Sprties[i] = m_Sprites[i];
        }
        for(int i = 0; i<m_nCharCount; ++i)
        {
            Chars[i] = m_ValidChars[i];
        }
        m_Sprites = Sprties;
        m_ValidChars = Chars;
    }
    
    void push_char(char ch, int nCharPos)
    {
        if(m_SpriteCount >= m_Sprites.Length
            || m_SpriteCount >= m_ValidChars.Length)
        {
            AutoGrow();
        }
        int nColorSize = m_colors.size;

        m_Sprites[m_SpriteCount].CharType = UIFontUnitType.UnitType_Char;
        m_Sprites[m_SpriteCount].bChar = true;
        m_Sprites[m_SpriteCount].ch = ch;
        m_Sprites[m_SpriteCount].bCustomColor = nColorSize > 0;
        m_Sprites[m_SpriteCount].SpriteID = 0;
        if (m_colors.size > 0)
        {
            m_Sprites[m_SpriteCount].CustomColor = m_colors[nColorSize - 1];
        }
        ++m_SpriteCount;

        if (ch != '\n')
        {
            m_ValidChars[m_nCharCount++] = ch;
        }
    }
    void push_enter(char ch, int nCharPos)
    {
        m_Sprites[m_SpriteCount].bChar = false;
        m_Sprites[m_SpriteCount].CharType = UIFontUnitType.UnitType_Enter;
        m_Sprites[m_SpriteCount].ch = ch;
        m_Sprites[m_SpriteCount].bCustomColor = false;
        m_Sprites[m_SpriteCount].SpriteID = 0;
        ++m_SpriteCount;        
    }

    int GetX16(int iStart)
    {
        int nValue = 0;
        char ch = m_szText[iStart];
        if (ch >= '0' && ch <= '9')
            nValue = ch - '0';
        else if (ch >= 'a' && ch <= 'f')
            nValue = ch - 'a' + 10;
        else if (ch >= 'A' && ch <= 'F')
            nValue = ch - 'A' + 10;

        nValue *= 16;
        ch = m_szText[iStart + 1];
        if (ch >= '0' && ch <= '9')
            nValue += ch - '0';
        else if (ch >= 'a' && ch <= 'f')
            nValue += ch - 'a' + 10;
        else if (ch >= 'A' && ch <= 'F')
            nValue += ch - 'A' + 10;

        return nValue;
    }

    bool IsColorARGB(string szText, int iStart)
    {
        int length = szText.Length;
        if (iStart + 16 > length)
        {
            return false;
        }
        if (szText[iStart + 2] != '#'
            || szText[iStart + 15] != ']')
        {
            return false;
        }

        // 检查不是合法数字
        for (int i = 0; i < 12; ++i)
        {
            char ch = szText[iStart + 3 + i];
            if (ch < '0' || ch > '9')
            {
                return false;
            }
        }
        return true;
    }

    int GetColorValue(string szText, int iStart)
    {
        int nValue = szText[iStart] - '0';
        nValue *= 10;
        nValue += szText[iStart + 1] - '0';
        nValue *= 10;
        nValue += szText[iStart + 2] - '0';
        if (nValue < 0)
            nValue = 0;
        if (nValue > 255)
            nValue = 255;
        return nValue;
    }

    void AnylseColorARGB(ref int iStart)
    {
        int nA = GetColorValue(m_szText, iStart + 3);
        int nR = GetColorValue(m_szText, iStart + 6);
        int nG = GetColorValue(m_szText, iStart + 9);
        int nB = GetColorValue(m_szText, iStart + 12);
        Color32 c = new Color32((byte)nR, (byte)nG, (byte)nB, (byte)nA);
        m_colors.Add(c);
        iStart += 15;
    }

    bool TryParseOldColor(ref int iStart)
    {
        //[rrggbb][-]
        int length = m_szText.Length;
        if (iStart + 3 > length)
        {
            return false;
        }
        if (m_szText[iStart + 1] == '-'
            && m_szText[iStart + 2] == ']')
        {
            if (m_colors.size > 0)
            {
                m_colors.Pop();
            }
            iStart += 2;
            return true;
        }

        if (iStart + 8 > length)
            return false;
        if (m_szText[iStart + 7] != ']')
            return false;

        for (int i = 0; i < 6; ++i)
        {
            char ch = m_szText[iStart + i + 1];
            if (ch >= '0' && ch <= '9')
                continue;
            if (ch >= 'a' && ch <= 'f')
                continue;
            if (ch >= 'A' && ch <= 'F')
                continue;
            return false;
        }

        int nR = GetX16(iStart + 1);
        int nG = GetX16(iStart + 3);
        int nB = GetX16(iStart + 5);

        Color32 c = new Color32((byte)nR, (byte)nG, (byte)nB, (byte)255);
        m_colors.Add(c);
        iStart += 7;
        return true;
    }

    // 功能：分析结束码
    void ParseEndCode(ref int iStart)
    {
        int length = m_szText.Length;
        // [0#]
        if (iStart + 4 > length)
        {
            ++iStart;
            push_char('[', iStart - 1);
            return;
        }
        // 合法的
        if (m_szText[iStart + 2] == '#'
            && m_szText[iStart + 3] == ']')
        {
            if (m_colors.size > 0)
            {
                m_colors.Pop();
            }
            iStart += 3;
            return;
        }
        ++iStart;
        push_char('[', iStart - 1); // 这是不合法的字符
    }

    // 功能：分析颜色码
    bool TryParseColorARGB(ref int iStart)
    {
        // [1#AAARRRGGGBBB]
        if (IsColorARGB(m_szText, iStart))
        {
            AnylseColorARGB(ref iStart);
            return true;
        }
        else
        {
            ++iStart;
            push_char('[', iStart - 1);
            return false;
        }
    }

    // 功能：分析颜色码
    bool ParseColorRGB(ref int iStart)
    {
        // [1#AAARRRGGGBBB]
        if (IsColorRGB(m_szText, iStart))
        {
            int nR = GetColorValue(m_szText, iStart + 3);
            int nG = GetColorValue(m_szText, iStart + 6);
            int nB = GetColorValue(m_szText, iStart + 9);
            Color32 c = new Color32((byte)nR, (byte)nG, (byte)nB, (byte)255);
            m_colors.Add(c);
            iStart += 12;
            return true;
        }
        else
        {
            ++iStart;
            push_char('[', iStart - 1);
            return false;
        }
    }

    int ParseMulNumb(int[] aNumb, ref int iStart, char chEnd, char chTab)
    {
        for (int i = 0; i < aNumb.Length; ++i)
        {
            aNumb[i] = 0;
        }
        int nCount = 0;
        int length = m_szText.Length;
        bool bValidNumb = false;
        for (; iStart < length; ++iStart)
        {
            char ch = m_szText[iStart];
            if (ch == chEnd)
            {
                if (nCount == 0 && bValidNumb)
                    ++nCount;
                return nCount;
            }
            if (ch >= '0' && ch <= '9')
            {
                if (nCount < aNumb.Length)
                {
                    bValidNumb = true;
                    aNumb[nCount] *= 10;
                    aNumb[nCount] += ch - '0';
                }
            }
            else if (ch == chTab)
            {
                ++nCount;
            }
            else
            {
                // 不合法的
                break;
            }
        }
        return 0;  // 没有结束符的都不合法
    }

    void push_icon(int nIconID, int nWidth, int nHeight, int nCharPos)
    {
        m_Sprites[m_SpriteCount].bChar = false;
        m_Sprites[m_SpriteCount].CharType = UIFontUnitType.UnitType_Icon;
        m_Sprites[m_SpriteCount].bCustomColor = false;
        m_Sprites[m_SpriteCount].SpriteID = nIconID;
        m_Sprites[m_SpriteCount].SpriteWidth = (short)nWidth;
        m_Sprites[m_SpriteCount].SpriteHeight = (short)nHeight;

        ++m_SpriteCount;
    }

    void push_gif(int nGifID, int nWidth, int nHeight, int nCharPos)
    {
        m_Sprites[m_SpriteCount].bChar = false;
        m_Sprites[m_SpriteCount].CharType = UIFontUnitType.UnitType_Gif;
        m_Sprites[m_SpriteCount].bCustomColor = false;
        m_Sprites[m_SpriteCount].SpriteID = nGifID;
        m_Sprites[m_SpriteCount].SpriteWidth = (short)nWidth;
        m_Sprites[m_SpriteCount].SpriteHeight = (short)nHeight;

        ++m_SpriteCount;
    }

    void SetLastColor(ref HUDCharInfo pNode)
    {
        if (m_colors != null && m_colors.size > 0)
        {
            Color32 c = m_colors[m_colors.size - 1];
            pNode.bCustomColor = true;
            pNode.CustomColor = c;
        }
        else
        {
            pNode.bCustomColor = false;
        }
    }

    Color32 GetLastColor()
    {
        if (m_colors != null && m_colors.size > 0)
            return m_colors[m_colors.size - 1];
        else
            return Color.white;
    }

    // 功能：分析图标ID
    void ParseIocnID(ref int iStart)
    {
        // [4#xxxx]
        int length = m_szText.Length;
        int nBakStart = iStart;
        iStart += 3;
        int[] aValueNumb = { 0, 0, 0 };
        int nCount = ParseMulNumb(aValueNumb, ref iStart, ']', ';');
        if (nCount > 0)
        {
            push_icon(aValueNumb[0], aValueNumb[1], aValueNumb[2], nBakStart);
            return;
        }
        iStart = nBakStart + 1;
        push_char('[', iStart - 1);
    }

    // 功能：分析图标
    void ParseIconName(ref int iStart)
    {
        // [5#iconname]
        int nEnd = m_szText.IndexOf(']', iStart + 3);
        if (nEnd == -1)
        {
            ++iStart;
            push_char('[', iStart - 1);
            return;
        }
        string szIconName = m_szText.Substring(iStart + 3, nEnd - iStart - 3);
        int nIconID = CAtlasMng.instance.SpriteNameToID(szIconName);
        push_icon(nIconID, 0, 0, iStart);
        iStart = nEnd;
    }

    // 功能：分析一个动画
    void ParseGifName(ref int iStart)
    {
        // [6#ani_name]
        int nEnd = m_szText.IndexOf(']', iStart + 3);
        if (nEnd == -1)
        {
            ++iStart;
            push_char('[', iStart - 1);
            return;
        }
        int nPos = iStart + 3;

        int[] aValueNumb = { 0, 0, 0 };
        int nCount = ParseMulNumb(aValueNumb, ref nPos, ']', ';');
        push_gif(aValueNumb[0], aValueNumb[1], aValueNumb[2], iStart);
        //string szGifName = m_szText.Substring(iStart + 3, nEnd - iStart - 3);
        iStart = nEnd;
    }
    
    void push_link_char(char ch, int nCharPos, int nObjIndex)
    {
        m_Sprites[m_SpriteCount].bChar = true;
        m_Sprites[m_SpriteCount].CharType = UIFontUnitType.UnitType_Link;
        m_Sprites[m_SpriteCount].ch = ch;
        if (m_colors.size > 0)
        {
            m_Sprites[m_SpriteCount].bCustomColor = true;
            m_Sprites[m_SpriteCount].CustomColor = m_colors[m_colors.size - 1];
        }
        else
            m_Sprites[m_SpriteCount].bCustomColor = false;
        m_Sprites[m_SpriteCount].SpriteID = 0;

        m_ValidChars[m_nCharCount++] = ch;
    }

    void push_link(string szLink, string szCustomDesc, int nCharPos, UIHyperlinkType linkType)
    {
        UIFontCustomObject obj = new UIFontCustomObject();
        obj.m_type = UIFontUnitType.UnitType_Link;
        obj.m_szLink = szLink;
        obj.m_szCustomDesc = szCustomDesc;
        obj.m_HyperlinkType = linkType;
        m_CustomObj.Add(obj);
        int nObjIndex = m_CustomObj.Count - 1;

        for (int i = 0; i < szLink.Length; ++i)
        {
            push_link_char(szLink[i], nCharPos, nObjIndex);
        }
    }

    bool TryParseOldColor(string szText, ref int iStart)
    {
        string szOld = m_szText;
        m_szText = szText;
        bool bSuc = TryParseOldColor(ref iStart);
        m_szText = szOld;
        return bSuc;
    }

    void ParseLinkColorARGB(string szText, ref int iStart)
    {
        string szOld = m_szText;
        m_szText = szText;
        AnylseColorARGB(ref iStart);
        m_szText = szOld;
    }

    bool IsColorRGB(string szText, int iStart)
    {
        int length = szText.Length;
        if (iStart + 13 > length)
        {
            return false;
        }
        if (szText[iStart + 2] != '#'
            || szText[iStart + 12] != ']')
        {
            return false;
        }

        // 检查不是合法数字
        for (int i = 0; i < 9; ++i)
        {
            char ch = szText[iStart + 3 + i];
            if (ch < '0' || ch > '9')
            {
                return false;
            }
        }
        return true;
    }

    bool ParseColorRGB(string szText, ref int iStart)
    {
        // [1#AAARRRGGGBBB]
        if (IsColorRGB(m_szText, iStart))
        {
            int nR = GetColorValue(m_szText, iStart + 3);
            int nG = GetColorValue(m_szText, iStart + 6);
            int nB = GetColorValue(m_szText, iStart + 9);
            Color32 c = new Color32((byte)nR, (byte)nG, (byte)nB, (byte)255);
            m_colors.Add(c);
            iStart += 12;
            return true;
        }
        else
        {
            ++iStart;
            push_char('[', iStart - 1);
            return false;
        }
    }

    // 功能：扫描超连接字符，并去除颜色码
    void ScaleLinkColor(string szLink, string szCustomDesc, int nCharPos)
    {
        UIHyperlinkType linkType = UIHyperlinkType.None;
        if (!string.IsNullOrEmpty(szCustomDesc) && szCustomDesc.Length > 2)
        {
            char chFirst = '0';
            if (szCustomDesc[1] == ':')
            {
                chFirst = szCustomDesc[0];
                szCustomDesc = szCustomDesc.Substring(2);
                if (chFirst == 'u' || chFirst == 'U')
                    linkType = UIHyperlinkType.Underline;
                else if (chFirst == 'f' || chFirst == 'F')
                    linkType = UIHyperlinkType.Underline_Flash;
            }
        }

        int nIndex = szLink.IndexOf('[');
        if (nIndex == -1)
        {
            push_link(szLink, szCustomDesc, nCharPos, linkType);
            return;
        }

        // 只支持一个颜色码，不支持多组
        int nLen = szLink.Length;
        int[] NewCharPos = new int[nLen];
        char[] szNewLink = new char[nLen];
        UIFontUnit[] fontUnit = new UIFontUnit[nLen];
        int nNewLen = 0;

        for (int i = 0; i < nLen; ++i)
        {
            char chType = szLink[i];
            if (chType == '[')
            {
                if (i + 3 > nLen
                    || szLink[i + 2] != '#')
                {
                    // 兼容旧的颜色码  [ff0000] [-]
                    if (TryParseOldColor(szLink, ref i))
                        continue;
                }
                if (i + 2 <= nLen)
                {
                    bool bValidFalgs = false;
                    switch (szLink[i + 1])
                    {
                        case '0': // 结束符 [0#]
                            {
                                if (szLink[i + 2] == '#')
                                {
                                    bValidFalgs = true;
                                    m_colors.Pop();
                                    i += 3;
                                }
                            }
                            break;
                        case '1': // [1#ARGB
                            {
                                bValidFalgs = IsColorARGB(szLink, i);
                                if (bValidFalgs)
                                    ParseLinkColorARGB(szLink, ref i);
                            }
                            break;
                        case '2': // [2#RGB
                            {
                                bValidFalgs = ParseColorRGB(szLink, ref i);
                            }
                            break;
                        case '-':  // [-] 旧的结束码
                            {
                                if (szLink[i + 2] == ']')
                                {
                                    bValidFalgs = true;
                                    m_colors.Pop();
                                    i += 2;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                    if (bValidFalgs)
                    {
                        continue;
                    }
                }
            }
            push_link_char(chType, nCharPos, m_CustomObj.Count);
        }

        UIFontCustomObject obj = new UIFontCustomObject();
        obj.m_type = UIFontUnitType.UnitType_Link;
        obj.m_szLink = new string(szNewLink, 0, nNewLen);
        obj.m_szCustomDesc = szCustomDesc;
        obj.m_HyperlinkType = linkType;
        m_CustomObj.Add(obj);        
    }

    // 功能：分析一个超连接
    void ParseLink(ref int iStart)
    {
        // [7#链接字符;自定义字符串]
        string szLink = string.Empty;
        string szCustomDesc = string.Empty;
        int nLinkStart = iStart + 3;
        int nCustomStart = m_szText.IndexOf(';', nLinkStart);
        int nEnd = iStart;
        if (nCustomStart != -1)
        {
            ++nCustomStart;
            nEnd = m_szText.IndexOf(']', nCustomStart);
            if (nEnd == -1)
            {
                ++iStart;
                push_char('[', iStart - 1);
                return;
            }
            szLink = m_szText.Substring(nLinkStart, nCustomStart - nLinkStart - 1);
            szCustomDesc = m_szText.Substring(nCustomStart, nEnd - nCustomStart);
        }
        else
        {
            nEnd = m_szText.IndexOf(']', nLinkStart);
            if (nEnd == -1)
            {
                ++iStart;
                push_char('[', iStart - 1);
                return;
            }
            szLink = m_szText.Substring(nLinkStart, nEnd - nLinkStart);
        }
        ScaleLinkColor(szLink, szCustomDesc, iStart);
        iStart = nEnd;
    }

    // 功能：分析数字
    bool ParseNumb(ref int nIconID, ref int iStart, char chEnd1, char chEnd2 = '\0')
    {
        nIconID = 0;
        int length = m_szText.Length;
        for (; iStart < length; ++iStart)
        {
            char ch = m_szText[iStart];
            if (ch == chEnd1 || ch == chEnd2)
            {
                return true;
            }
            if (ch >= '0' && ch <= '9')
            {
                nIconID *= 10;
                nIconID += ch - '0';
            }
            else
            {
                // 不合法的
                break;
            }
        }
        return false;
    }
    void push_space(int nW, int nH, int nCharPos)
    {
        m_Sprites[m_SpriteCount].bChar = false;
        m_Sprites[m_SpriteCount].CharType = UIFontUnitType.UnitType_Space;
        m_Sprites[m_SpriteCount].ch = '\0';
        m_Sprites[m_SpriteCount].SpriteID = 0;
        m_Sprites[m_SpriteCount].SpriteWidth = (short)nW;
        m_Sprites[m_SpriteCount].SpriteHeight = (short)nH;
        ++m_SpriteCount;
    }

    void ParseSpace(ref int iStart)
    {
        // [8#www-hhh]
        int nBakStart = iStart;
        int nW = 0, nH = 0;
        if (!ParseNumb(ref nW, ref iStart, '-'))
        {
            iStart = nBakStart + 1;
            push_char('[', iStart - 1);
            return;
        }
        if (!ParseNumb(ref nH, ref iStart, ']'))
        {
            iStart = nBakStart + 1;
            push_char('[', iStart - 1);
            return;
        }
        push_space(nW, nH, nBakStart);
    }

    void ParseObject(ref int iStart)
    {
        m_bCharDirty = true;

        int length = m_szText.Length;
        if (iStart + 3 > length
            || m_szText[iStart + 2] != '#')
        {
            // 兼容旧的颜色码
            // [ff0000] [-]
            if (!TryParseOldColor(ref iStart))
                push_char('[', iStart);
            return;
        }
        char chType = m_szText[iStart + 1];
        switch (chType)
        {
            case '[':
                ++iStart;
                push_char('[', iStart - 1);
                return;
            case '0':  // [0#]结束码
                ParseEndCode(ref iStart);
                break;
            case '1': // [1#AAARRRGGGBBB] 颜色码 ARGB
                TryParseColorARGB(ref iStart);
                break;
            case '2': // [2#RRRGGGBBB] 颜色码 RGB
                ParseColorRGB(ref iStart);
                break;
            case '3': // 暂时不支持的字符
                ++iStart;
                push_char('[', iStart - 1);
                break;
            case '4': // [4#xxxx]  图片ID
                ParseIocnID(ref iStart);
                break;
            case '5': // [5#iconname] 图片名字
                ParseIconName(ref iStart);
                break;
            case '6': // [6#ani_name] 动画的名字
                ParseGifName(ref iStart);
                break;
            case '7': // [7#链接字符;自定义字符串]
                ParseLink(ref iStart);
                break;
            case '8': // [8#www-hhh] 任意大小的空格
                ParseSpace(ref iStart);
                break;
            case '9':
                break;
            default:
                ++iStart;
                push_char('[', iStart - 1);
                break;
        }
    }

    public void  ParseText(string szText)
    {
        m_SpriteCount = 0;
        m_nCharCount = 0;
        m_szText = szText;
        m_colors.Clear();
        m_CustomObj.Clear();
        if (string.IsNullOrEmpty(szText))
            return;
        m_bCharDirty = false;
        int length = szText.Length;
        for(int i = 0; i< length; ++i)
        {
            char ch = m_szText[i];
            switch (ch)
            {
                case '[':
                    ParseObject(ref i);
                    break;
                case '\n':
                    push_enter(ch, i);
                    break;
                case '\\':
                    {
                        if (i + 1 < length)
                        {
                            if (m_szText[i + 1] == 'n')
                            {
                                push_enter('\n', i);
                                ++i;
                            }
                            else if (m_szText[i + 1] == '\\')
                            {
                                push_char('\\', i);
                                ++i;
                            }
                            else
                            {
                                push_char(ch, i);
                            }
                        }
                        else
                        {
                            push_char(ch, i);
                        }
                    }
                    break;
                case '\0': // 不可见字符(结束符，不能显示)
                    break;
                default:
                    {
                        push_char(ch, i);
                    }
                    break;
            }
        }
        if(m_bCharDirty)
        {
            if (m_nCharCount == 0)
                m_szText = string.Empty;
            else
                m_szText = new string(m_ValidChars, 0, m_nCharCount);
        }
    }
};
