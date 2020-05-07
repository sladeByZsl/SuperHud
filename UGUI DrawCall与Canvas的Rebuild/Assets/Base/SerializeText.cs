using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Threading;

class CSerialzieTextStream
{
    byte[] m_pData;
    int m_nSize;
    int m_nSizeMax;
    int m_nPos;
    SerializeType m_arType;
    bool m_bLittleByte;
    bool m_bEndLine;  // 是不是到行尾

    byte[] m_pTempString;
    int m_nStringLen;
    int m_nStringMaxLen;
    public CSerialzieTextStream(SerializeType arType)
    {
        m_nSize = 0;
        m_nSizeMax = 0;
        m_nPos = 0;
        m_arType = arType;
        m_bLittleByte = System.BitConverter.IsLittleEndian;
        m_bEndLine = false;

        m_pTempString = null;
        m_nStringLen = 0;
        m_nStringMaxLen = 0;
    }
    public CSerialzieTextStream(SerializeType arType, byte[] buffer, int nBufSize)
    {
        m_pData = new byte[nBufSize];
        if (buffer != null)
            Array.Copy(buffer, m_pData, nBufSize);
        m_nSize = m_nSizeMax = nBufSize;
        m_nPos = 0;
        m_arType = arType;
        m_bLittleByte = System.BitConverter.IsLittleEndian;
        m_bEndLine = false;

        m_pTempString = null;
        m_nStringLen = 0;
        m_nStringMaxLen = 0;
    }
    public byte[] GetBuffer()
    {
        return m_pData;
    }
    public int GetBufferSize()
    {
        return m_nSize;
    }
    public bool CanRead { get { return m_arType == SerializeType.read; } }
    public bool CanSeek { get { return true; } }
    public bool CanWrite { get { return m_arType == SerializeType.read || m_arType == SerializeType.append; } }
    public void Flush()
    {
        m_nPos = 0;
        m_nSize = 0;
    }
    void reserve(int nSizeMax)
    {
        if (m_nSizeMax < nSizeMax)
        {
            byte[] pData = new byte[nSizeMax];
            m_nSizeMax = nSizeMax;
            if (m_nSize > 0)
                Array.Copy(m_pData, pData, m_nSize);
            m_pData = pData;
        }
    }
    void auto_grow(int nGrowSize)
    {
        if (nGrowSize > 0 && m_nPos + nGrowSize > m_nSizeMax)
        {
            int nNewSize = m_nSizeMax * 2 + (nGrowSize + 4095) / 4096 * 4096;
            if (nNewSize < 4096)
                nNewSize = 4096;
            reserve(nNewSize);
        }
    }
    void clear_temp()
    {
        m_nStringLen = 0;
    }
    string get_temp()
    {
        if (m_nStringLen <= 0)
            return string.Empty;
        while (m_nStringLen > 0 && m_pTempString[m_nStringLen - 1] == 32 )
        {
            --m_nStringLen;
        }
        return System.Text.Encoding.UTF8.GetString(m_pTempString, 0, m_nStringLen);
    }
    void push_temp(byte ch)
    {
        if (m_nStringLen + 1 > m_nStringMaxLen)
        {
            m_nStringMaxLen = m_nStringMaxLen > 0 ? (m_nStringMaxLen * 3 / 2) : 32;
            byte[] pNewString = new byte[m_nStringMaxLen];
            if( m_pTempString != null && m_nStringLen > 0 )
                Array.Copy(m_pTempString, 0, pNewString, 0, m_nStringLen);
            m_pTempString = pNewString;
        }
        m_pTempString[m_nStringLen] = ch;
        m_nStringLen += 1;
    }
    // 功能：读取头
    public string readHeadstring()
    {
        m_bEndLine = false;
        clear_temp();
        int nValidCharNumb = 0;
        for (; m_nPos < m_nSize; ++m_nPos)
        {
            if (m_pData[m_nPos] == ':')
            {
                ++m_nPos;
                break;
            }
            else if (m_pData[m_nPos] == ' '
                || m_pData[m_nPos] == '\t')
            {
                if (0 == nValidCharNumb)
                {
                    continue;
                }
            }
            ++nValidCharNumb;
            push_temp(m_pData[m_nPos]);
        }
        return get_temp();
    }
    public void trySkipEnter(ref int nPos)
    {
        if (nPos + 2 <= m_nSize
            && m_pData[nPos] == '/'
            && m_pData[nPos + 1] == '/')
        {
            bool bNeedBreak = false;
            for (; nPos < m_nSize; ++nPos)
            {
                if (m_pData[nPos] == '\r')
                {
                    ++nPos;
                    bNeedBreak = true;
                }
                if (m_pData[nPos] == '\n')
                {
                    ++nPos;
                    bNeedBreak = true;
                }
                if (bNeedBreak)
                    break;
            }
        }
    }
    // 统计变量个数
    public int countValue()
    {
        int nCount = 0;
        int nPos = m_nPos;
        for (; nPos < m_nSize; ++nPos)
        {
            if (m_pData[nPos] == '\\')
            {
                ++nPos;
                continue;
            }
            trySkipEnter(ref nPos);
            if (m_pData[nPos] == ','
                || m_pData[nPos] == ';')
            {
                ++nCount;
            }
            if (m_pData[nPos] == '\r'
                || m_pData[nPos] == '\n')
            {
                break;
            }
        }
        return nCount;
    }
    // 如果是普通变量，以逗号分隔，以分号做换行结束
    // [关键字]:xxx, xxx;
    public string readString()
    {
        m_bEndLine = false;
        clear_temp();
        int nValidCharNumb = 0;
        for (; m_nPos < m_nSize; ++m_nPos)
        {
            // 这是转义字符
            if (m_pData[m_nPos] == '\\')
            {
                ++m_nPos;
                if (m_nPos < m_nSize)
                {
                    ++nValidCharNumb;
                    push_temp(m_pData[m_nPos]);
                }
            }
            else
            {
                if (m_pData[m_nPos] == '\t')
                    continue;
                if (m_pData[m_nPos] == ' ')
                {
                    if (m_nStringLen > 0)
                    {
                        ++nValidCharNumb;
                        push_temp(m_pData[m_nPos]);
                    }
                    continue;
                }
                trySkipEnter(ref m_nPos);

                if (m_pData[m_nPos] == ','
                    || m_pData[m_nPos] == ';')
                {
                    if (nValidCharNumb > 0)
                    {
                        ++m_nPos;
                        break;
                    }
                    continue;
                }
                if (m_pData[m_nPos] == '\r')
                {
                    ++m_nPos;
                    m_bEndLine = true;
                }
                if (m_pData[m_nPos] == '\n')
                {
                    ++m_nPos;
                    m_bEndLine = true;
                }
                if (m_bEndLine)
                    break;
                ++nValidCharNumb;
                push_temp(m_pData[m_nPos]);
            }
        }
        return get_temp();
    }
    public void WriteHeader(string strHeader)
    {
        int nLen = strHeader.Length;
        auto_grow(nLen + 2);
        char[] pcsStr = strHeader.ToCharArray();
        for (int i = 0; i < nLen; ++i)
        {
            m_pData[m_nPos++] = (byte)pcsStr[i];
        }
        m_pData[m_nPos++] = (byte)':';
        if (m_nSize < m_nPos)
            m_nSize = m_nPos;
    }
    void push_string(string strValue, bool bCheck)
    {
        if (string.IsNullOrEmpty(strValue))
            return;
        if (bCheck)
        {
            strValue = strValue.Replace(';', '\\'); // 分号
            strValue = strValue.Replace(',', '\\'); // 逗号
        }
        char[] pcsStr = strValue.ToCharArray();
        byte  []byBuf = System.Text.Encoding.UTF8.GetBytes(strValue);
        auto_grow(byBuf.Length);
        Array.Copy(byBuf, 0, m_pData, m_nPos, byBuf.Length);
        m_nPos += byBuf.Length;
        if (m_nSize < m_nPos)
            m_nSize = m_nPos;
    }
    // 写入一个变量
    void WriteString(string strValue, string strTab)
    {
        push_string(strValue, true);
        push_string(strTab, false);
    }
    public void WriteValue(string szName, bool tValue)
    {
        WriteHeader(szName);
        WriteString(tValue.ToString(), "\r\n");
    }
    public void WriteValue(string szName, byte tValue)
    {
        WriteHeader(szName);
        WriteString(tValue.ToString(), "\r\n");
    }
    void WriteValue(string szName, short tValue)
    {
        WriteHeader(szName);
        WriteString(tValue.ToString(), "\r\n");
    }
    public void WriteValue(string szName, int tValue)
    {
        WriteHeader(szName);
        WriteString(tValue.ToString(), "\r\n");
    }
    public void WriteValue(string szName, float tValue)
    {
        WriteHeader(szName);
        WriteString(tValue.ToString(), "\r\n");
    }
    public void WriteValue(string szName, double tValue)
    {
        WriteHeader(szName);
        WriteString(tValue.ToString(), "\r\n");
    }
    public void WriteValue(string szName, Vector3 tValue)
    {
        WriteHeader(szName);
        WriteString(tValue.x.ToString(), ",");
        WriteString(tValue.y.ToString(), ",");
        WriteString(tValue.z.ToString(), "\r\n");
    }
    public void WriteValue(string szName, Quaternion tValue)
    {
        WriteHeader(szName);
        WriteString(tValue.x.ToString(), ",");
        WriteString(tValue.y.ToString(), ",");
        WriteString(tValue.z.ToString(), ",");
        WriteString(tValue.w.ToString(), ";\r\n");
    }
    public void WriteValue(string szName, Rect tValue)
    {
        WriteHeader(szName);
        WriteString(tValue.xMin.ToString(), ",");
        WriteString(tValue.yMin.ToString(), ",");
        WriteString(tValue.xMax.ToString(), ",");
        WriteString(tValue.yMax.ToString(), "\r\n");
    }

    public void WriteValue(string szName, Bounds tValue)
    {
        WriteHeader(szName);
        Vector3 vMin = tValue.min;
        Vector3 vMax = tValue.max;
        WriteString(vMin.x.ToString(), ",");
        WriteString(vMin.y.ToString(), ",");
        WriteString(vMin.z.ToString(), ";");
        WriteString(vMax.x.ToString(), ",");
        WriteString(vMax.y.ToString(), ",");
        WriteString(vMax.z.ToString(), "\r\n");
    }
    public void WriteValue(string szName, string tValue)
    {
        WriteHeader(szName);
        WriteString(tValue, "\r\n");
    }
    public void WriteArray(string szName, int[] aValue)
    {
        WriteHeader(szName);
        int nLen = aValue != null ? aValue.Length : 0;
        for (int i = 0; i < nLen; ++i)
        {
            if (i + 1 < nLen)
            {
                WriteString(aValue[i].ToString(), ",");
            }
            else
            {
                WriteString(aValue[i].ToString(), "");
            }
        }
        push_string("\r\n", false);
    }
    public void WriteArray(string szName, string[] aValue)
    {
        WriteHeader(szName);
        int nLen = aValue != null ? aValue.Length : 0;
        for (int i = 0; i < nLen; ++i)
        {
            if (i + 1 < nLen)
            {
                WriteString(aValue[i], ",");
            }
            else
            {
                WriteString(aValue[i], "");
            }
        }
        push_string("\r\n", false);
    }
    int StringToInt(string szName, int nDef)
    {
        if (szName == null || szName.Length == 0)
            return nDef;
        int nValue = nDef;
        int.TryParse(szName, out nValue);
        return nValue;
    }
    float float_parse(string szName)
    {
        if (szName == null || szName.Length == 0)
            return 0.0f;
        float fValue = 0.0f;
        float.TryParse(szName, out fValue);
        return fValue;
    }
    double double_parse(string szName)
    {
        if (szName == null || szName.Length == 0)
            return 0.0f;
        double fValue = 0;
        double.TryParse(szName, out fValue);
        return fValue;
    }
    public void ReadValue(string szName, ref bool tValue)
    {
        string szHeader = readHeadstring();
        string szValue = readString();
        if (szValue == "True" || szValue == "true")
            tValue = true;
        else if (szValue == "False" || szValue == "false")
            tValue = false;
        else
        {
            szValue = szValue.ToLower();
            if (szValue == "true")
                tValue = true;
            else if (szValue == "false")
                tValue = false;
            else
                tValue = StringToInt(szValue, 0) != 0;
        }
    }
    public void ReadValue(string szName, ref byte tValue)
    {
        string szHeader = readHeadstring();
        string szValue = readString();
        tValue = (byte)StringToInt(szValue, 0);
    }
    public void ReadValue(string szName, ref short tValue)
    {
        string szHeader = readHeadstring();
        string szValue = readString();
        tValue = (short)StringToInt(szValue, 0);
    }
    public void ReadValue(string szName, ref int tValue)
    {
        string szHeader = readHeadstring();
        string szValue = readString();
        tValue = (int)StringToInt(szValue, 0);
    }
    public void ReadValue(string szName, ref float tValue)
    {
        string szHeader = readHeadstring();
        string szValue = readString();
        tValue = float_parse(szValue);
    }
    public void ReadValue(string szName, ref double tValue)
    {
        string szHeader = readHeadstring();
        string szValue = readString();
        tValue = double_parse(szValue);
    }
    public void ReadValue(string szName, ref string tValue)
    {
        string szHeader = readHeadstring();
        tValue = readString();
    }
    public void ReadValue(string szName, ref Vector3 tValue)
    {
        string szHeader = readHeadstring();
        string szValue = readString();
        tValue.x = float_parse(szValue);
        szValue = readString();
        tValue.y = float_parse(szValue);
        szValue = readString();
        tValue.z = float_parse(szValue);
    }
    public void ReadValue(string szName, ref Quaternion tValue)
    {
        string szHeader = readHeadstring();
        string szValue = readString();
        tValue.x = float_parse(szValue);
        szValue = readString();
        tValue.y = float_parse(szValue);
        szValue = readString();
        tValue.z = float_parse(szValue);
        szValue = readString();
        tValue.w = float_parse(szValue);
    }
    public void ReadValue(string szName, ref Rect tValue)
    {
        string szHeader = readHeadstring();
        string szValue = readString();
        tValue.xMin = float_parse(szValue);
        szValue = readString();
        tValue.yMin = float_parse(szValue);
        szValue = readString();
        tValue.xMax = float_parse(szValue);
        szValue = readString();
        tValue.yMax = float_parse(szValue);
    }
    public void ReadValue(string szName, ref Bounds tValue)
    {
        string szHeader = readHeadstring();
        Vector3 vMin, vMax;
        string szValue = readString();
        vMin.x = float_parse(szValue);
        szValue = readString();
        vMin.y = float_parse(szValue);
        szValue = readString();
        vMin.z = float_parse(szValue);
        szValue = readString();
        vMax.x = float_parse(szValue);
        szValue = readString();
        vMax.y = float_parse(szValue);
        szValue = readString();
        vMax.z = float_parse(szValue);
        tValue.min = vMin;
        tValue.max = vMax;
    }
    public void ReadArray(string szName, ref int[] aValue)
    {
        string szHeader = readHeadstring();
        int nCount = countValue();
        aValue = new int[nCount];
        int nIndex = 0;
        while (!m_bEndLine && m_nPos < m_nSize)
        {
            string szValue = readString();
            aValue[nIndex] = StringToInt(szValue, 0);
        }
    }
    public void ReadArray(string szName, ref string[] aValue)
    {
        string szHeader = readHeadstring();
        int nCount = countValue();
        aValue = new string[nCount];
        int nIndex = 0;
        while (!m_bEndLine && m_nPos < m_nSize)
        {
            string szValue = readString();
            aValue[nIndex] = szValue;
        }
    }
};

public class SerializeText
{
    CSerialzieTextStream m_arFile;
    SerializeType m_arType;
    string m_szFileName;
    int m_nVersion;

    bool m_bCreate = false;

    public SerializeText(SerializeType arType)
    {
        m_arFile = new CSerialzieTextStream(arType);
        m_arType = arType;
        m_nVersion = 0;
    }
    public SerializeText(SerializeType arType, byte[] pData, int nDataSize)
    {
        m_arFile = new CSerialzieTextStream(arType, pData, nDataSize);
        m_arType = arType;
        m_nVersion = 0;
    }
    public SerializeText(SerializeType arType, string szFileName)
    {
        m_arType = arType;
        m_szFileName = szFileName;
        m_nVersion = 0;
        if (m_arType == SerializeType.read)
        {
            if (System.IO.File.Exists(m_szFileName))
            {
                byte[] fileData = System.IO.File.ReadAllBytes(m_szFileName);
                m_arFile = new CSerialzieTextStream(arType, fileData, fileData.Length);
            }
            else
                m_arFile = new CSerialzieTextStream(arType, null, 0);
        }
        else
        {
            m_arFile = new CSerialzieTextStream(arType);
        }
    }
    ~SerializeText()
    {
        if (m_arType != SerializeType.read && !string.IsNullOrEmpty(m_szFileName))
        {
            Close();
        }
    }

    public void SetVersion(int nVersion)
    {
        m_nVersion = nVersion;
    }
    public int GetVersion()
    {
        return m_nVersion;
    }

    public void Close()
    {
        Flush();
        m_bCreate = false;
        m_arType = SerializeType.read;
        m_szFileName = string.Empty;
    }

    public void Flush()
    {
        if (string.IsNullOrEmpty(m_szFileName))
            return;

        // 写入文件
        if (m_arType != SerializeType.read)
        {
            try
            {
                FileStream pFile = null;
                if (!m_bCreate && m_arType == SerializeType.write)
                {
                    m_bCreate = true;
                    if (File.Exists(m_szFileName))
                        File.Delete(m_szFileName);
                    pFile = File.Open(m_szFileName, FileMode.CreateNew, FileAccess.Write);
                }
                else
                {
                    if (GetBufferSize() == 0)
                        return;
                    pFile = File.Open(m_szFileName, FileMode.Append, FileAccess.Write);
                    if (pFile != null)
                    {
                        pFile.Seek(0, SeekOrigin.End);
                    }
                }

                if (pFile != null)
                {
                    if (GetBufferSize() > 0)
                        pFile.Write(GetBuffer(), 0, GetBufferSize());
                    pFile.Flush();
                    pFile.Close();
                    m_arFile.Flush();
                }
            }
            catch (Exception e)
            {
            }
        }
    }
    public byte[] GetBuffer()
    {
        return m_arFile.GetBuffer();
    }
    public int GetBufferSize()
    {
        return m_arFile.GetBufferSize();
    }
    public bool IsLoading()
    {
        return m_arType == SerializeType.read;
    }
    public void Read(string szName, ref bool tValue)
    {
        m_arFile.ReadValue(szName, ref tValue);
    }
    public void Read(string szName, ref byte tValue)
    {
        m_arFile.ReadValue(szName, ref tValue);
    }
    public void Read(string szName, ref short tValue)
    {
        m_arFile.ReadValue(szName, ref tValue);
    }
    public void Read(string szName, ref int tValue)
    {
        m_arFile.ReadValue(szName, ref tValue);
    }
    public void Read(string szName, ref float tValue)
    {
        m_arFile.ReadValue(szName, ref tValue);
    }
    public void Read(string szName, ref double tValue)
    {
        m_arFile.ReadValue(szName, ref tValue);
    }
    public void Read(string szName, ref Vector3 tValue)
    {
        m_arFile.ReadValue(szName, ref tValue);
    }
    public void Read(string szName, ref Quaternion tValue)
    {
        m_arFile.ReadValue(szName, ref tValue);
    }
    public void Read(string szName, ref Bounds tValue)
    {
        m_arFile.ReadValue(szName, ref tValue);
    }
    public void Read(string szName, ref string tValue)
    {
        m_arFile.ReadValue(szName, ref tValue);
    }
    public void Read(string szName, ref int[] aValue)
    {
        m_arFile.ReadArray(szName, ref aValue);
    }
    //-------------------
    public void Write(string szName, bool tValue)
    {
        m_arFile.WriteValue(szName, tValue);
    }
    public void Write(string szName, byte tValue)
    {
        m_arFile.WriteValue(szName, tValue);
    }
    public void Write(string szName, short tValue)
    {
        m_arFile.WriteValue(szName, tValue);
    }
    public void Write(string szName, int tValue)
    {
        m_arFile.WriteValue(szName, tValue);
    }
    public void Write(string szName, float tValue)
    {
        m_arFile.WriteValue(szName, tValue);
    }
    public void Write(string szName, double tValue)
    {
        m_arFile.WriteValue(szName, tValue);
    }
    public void Write(string szName, Vector3 tValue)
    {
        m_arFile.WriteValue(szName, tValue);
    }
    public void Write(string szName, Quaternion tValue)
    {
        m_arFile.WriteValue(szName, tValue);
    }
    public void Write(string szName, Bounds tValue)
    {
        m_arFile.WriteValue(szName, tValue);
    }
    public void Write(string szName, string tValue)
    {
        m_arFile.WriteValue(szName, tValue);
    }
    public void Write(string szName, int[] aValue)
    {
        m_arFile.WriteArray(szName, aValue);
    }
    // -----------------------------------
    public void ReadWriteValue(string szName, ref bool tValue)
    {
        if (IsLoading())
            m_arFile.ReadValue(szName, ref tValue);
        else
            m_arFile.WriteValue(szName, tValue);
    }
    public void ReadWriteValue(string szName, ref byte tValue)
    {
        if (IsLoading())
            m_arFile.ReadValue(szName, ref tValue);
        else
            m_arFile.WriteValue(szName, tValue);
    }
    public void ReadWriteValue(string szName, ref short tValue)
    {
        if (IsLoading())
            m_arFile.ReadValue(szName, ref tValue);
        else
            m_arFile.WriteValue(szName, tValue);
    }
    public void ReadWriteValue(string szName, ref int tValue)
    {
        if (IsLoading())
            m_arFile.ReadValue(szName, ref tValue);
        else
            m_arFile.WriteValue(szName, tValue);
    }
    public void ReadWriteValue(string szName, ref float tValue)
    {
        if (IsLoading())
            m_arFile.ReadValue(szName, ref tValue);
        else
            m_arFile.WriteValue(szName, tValue);
    }
    public void ReadWriteValue(string szName, ref double tValue)
    {
        if (IsLoading())
            m_arFile.ReadValue(szName, ref tValue);
        else
            m_arFile.WriteValue(szName, tValue);
    }
    public void ReadWriteValue(string szName, ref Vector3 tValue)
    {
        if (IsLoading())
            m_arFile.ReadValue(szName, ref tValue);
        else
            m_arFile.WriteValue(szName, tValue);
    }
    public void ReadWriteValue(string szName, ref Quaternion tValue)
    {
        if (IsLoading())
            m_arFile.ReadValue(szName, ref tValue);
        else
            m_arFile.WriteValue(szName, tValue);
    }
    public void ReadWriteValue(string szName, ref Rect tValue)
    {
        if (IsLoading())
            m_arFile.ReadValue(szName, ref tValue);
        else
            m_arFile.WriteValue(szName, tValue);
    }
    public void ReadWriteValue(string szName, ref Bounds tValue)
    {
        if (IsLoading())
            m_arFile.ReadValue(szName, ref tValue);
        else
            m_arFile.WriteValue(szName, tValue);
    }
    public void ReadWriteValue(string szName, ref string tValue)
    {
        if (IsLoading())
            m_arFile.ReadValue(szName, ref tValue);
        else
            m_arFile.WriteValue(szName, tValue);
    }
    public void ReadWriteValue(string szName, ref int[] aValue)
    {
        if (IsLoading())
            m_arFile.ReadArray(szName, ref aValue);
        else
            m_arFile.WriteArray(szName, aValue);
    }
    public void ReadWriteValue(string szName, ref string[] aValue)
    {
        if (IsLoading())
            m_arFile.ReadArray(szName, ref aValue);
        else
            m_arFile.WriteArray(szName, aValue);
    }
    public void SerializeArray<_Ty>(string szName, ref _Ty[] aValue)
    {
        int nLen = 0;
        if (IsLoading())
        {
            Read(szName, ref nLen);
            if (nLen < 0 || nLen > 1024 * 1024)
                nLen = 0;
            if (nLen <= 0)
                aValue = null;
            else
                aValue = new _Ty[nLen];
        }
        else
        {
            nLen = aValue != null ? aValue.Length : 0;
            Write(szName, nLen);
        }
    }
    public delegate void SerializeArrayNode<_Ty>(SerializeText ar, ref _Ty value);
    public void SerializeArray<_Ty>(string szName, ref _Ty[] aValue, SerializeArrayNode<_Ty> serializeFunc)
    {
        int nLen = 0;
        if (IsLoading())
        {
            Read(szName, ref nLen);
            if (nLen < 0 || nLen > 1024 * 1024)
                nLen = 0;
            if (nLen <= 0)
                aValue = new _Ty[0];
            else
            {
                aValue = new _Ty[nLen];
            }
        }
        else
        {
            nLen = aValue != null ? aValue.Length : 0;
            Write(szName, nLen);
        }
        for (int i = 0; i < nLen; ++i)
        {
            serializeFunc(this, ref aValue[i]);
        }
    }

    public void SerializeArray(string szName, ref List<string> aValue, string szKey)
    {
        if (aValue == null)
            aValue = new List<string>();

        int nLen = 0;
        if (IsLoading())
        {
            Read(szName, ref nLen);
            if (nLen < 0 || nLen > 1024 * 1024)
                nLen = 0;
            aValue.Clear();
            for (int i = 0; i < nLen; ++i)
            {
                string value = string.Empty;
                ReadWriteValue(szKey, ref value);
                aValue.Add(value);
            }
        }
        else
        {
            nLen = aValue != null ? aValue.Count : 0;
            Write(szName, nLen);
            for (int i = 0; i < nLen; ++i)
            {
                string value = aValue[i];
                ReadWriteValue(szKey, ref value);
            }
        }
    }

    public void SerializeArray<_Ty>(string szName, ref List<_Ty> aValue, SerializeArrayNode<_Ty> serializeFunc) where _Ty : new()
    {
        if (aValue == null)
            aValue = new List<_Ty>();

        int nLen = 0;
        if (IsLoading())
        {
            Read(szName, ref nLen);
            if (nLen < 0 || nLen > 1024 * 1024)
                nLen = 0;
            aValue.Clear();
            for (int i = 0; i < nLen; ++i)
            {
                _Ty value = new _Ty();
                serializeFunc(this, ref value);
                if (value != null)
                {
                    aValue.Add(value);
                }
            }
        }
        else
        {
            nLen = aValue != null ? aValue.Count : 0;
            Write(szName, nLen);
            for (int i = 0; i < nLen; ++i)
            {
                _Ty value = aValue[i];
                serializeFunc(this, ref value);
            }
        }
    }
    
    public delegate void SerializeIterator<_TyKey, _TyValue>(SerializeText ar, ref _TyKey key, ref _TyValue value);
    public void SerializeDictionary<_TyKey, _TyValue>(string szName, ref Dictionary<_TyKey, _TyValue> aValue, SerializeIterator<_TyKey, _TyValue> serializeFunc)
    {
        if (aValue == null)
            aValue = new Dictionary<_TyKey, _TyValue>();

        int nLen = 0;
        if (IsLoading())
        {
            Read(szName, ref nLen);
            if (nLen < 0 || nLen > 1024 * 1024)
                nLen = 0;
            if (aValue == null)
                aValue = new Dictionary<_TyKey, _TyValue>();
            aValue.Clear();
            for (int i = 0; i < nLen; ++i)
            {
                _TyKey key = default(_TyKey);
                _TyValue value = default(_TyValue);
                serializeFunc(this, ref key, ref value);
                if (key != null && value != null )
                    aValue[key] = value;
            }
        }
        else
        {
            nLen = aValue != null ? aValue.Count : 0;
            Write(szName, nLen);
            if (nLen > 0)
            {
                Dictionary<_TyKey, _TyValue>.Enumerator it = aValue.GetEnumerator();
                while (it.MoveNext())
                {
                    _TyKey key = it.Current.Key;
                    _TyValue value = it.Current.Value;
                    serializeFunc(this, ref key, ref value);
                }
            }
        }
    }
}
