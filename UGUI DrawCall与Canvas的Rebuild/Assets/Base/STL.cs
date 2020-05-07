using System;
using System.Collections.Generic;
using System.Text;

public class CMyArray<_Ty>
{
    _Ty[] m_pData;
    int m_nLeng;
    public CMyArray()
    {
        m_pData = new _Ty[0];
        m_nLeng = 0;
    }
    public bool reserve(int nSize)
    {
        if (nSize <= m_pData.Length)
            return true;
        _Ty[] pData = new _Ty[nSize];
        if (m_nLeng > 0)
            Array.Copy(m_pData, 0, pData, 0, m_nLeng);
        m_pData = pData;
        return true;
    }
    public void Clear()
    {
        m_pData = new _Ty[0];
        m_nLeng = 0;
    }
	public void FastClear()
	{
		m_nLeng = 0;
    }
    // fast clear and set null
    public void ClearAnSetNull()
    {
        if (m_nLeng <= 0)
            return;
        _Ty defVal = default(_Ty);
        for (int i = 0; i < m_nLeng; ++i)
        {
            m_pData[i] = defVal;
        }
        m_nLeng = 0;
    }
    public int size()
    {
        return m_nLeng;
    }
    public bool IsValid(int nIndex)
    {
        return nIndex >= 0 && nIndex < m_nLeng;
    }
    public void push_front(_Ty value)
    {
        if (m_nLeng >= m_pData.Length)
        {
            int nNewLen = m_pData.Length == 0 ? 8 : m_pData.Length;
            reserve(nNewLen * 2);
        }
        for (int i = m_nLeng; i > 0; --i)
        {
            m_pData[i] = m_pData[i - 1];
        }
        m_pData[0] = value;
        ++m_nLeng;
    }
    public void push_back(_Ty value)
    {
        if (m_nLeng >= m_pData.Length)
        {
            int nNewLen = m_pData.Length == 0 ? 8 : m_pData.Length;
            reserve(nNewLen * 2);
        }
        m_pData[m_nLeng++] = value;
    }
    // 功能：重载下标运算符
    public _Ty this[int nIndex]
    {
        get { return m_pData[nIndex]; }
        set { m_pData[nIndex] = value; }
    }
    public _Ty front
    {
        get
        {
            return m_pData[0];
        }
    }
    public _Ty back
    {
        get
        {
            return m_pData[m_nLeng - 1];
        }
    }
    public _Ty Get(int nIndex)
    {
        return m_pData[nIndex];
    }
    public void Set(int nIndex, _Ty value)
    {
        m_pData[nIndex] = value;
    }
    public void Remove(int nIndex)
    {
        if (IsValid(nIndex))
        {
            for (; nIndex < m_nLeng - 1; ++nIndex)
            {
                m_pData[nIndex] = m_pData[nIndex + 1];
            }
            m_pData[nIndex] = default(_Ty);
            --m_nLeng;
        }
    }
    public void pop_front()
    {
        Remove(0);
    }
    public void pop_back()
    {
        Remove(m_nLeng - 1);
    }

    public void GrowSet(int nIndex, _Ty value)
    {
        if (nIndex < 0)
            return;
        if (nIndex >= m_nLeng)
        {
            if (!reserve(nIndex + 1))
                return;
            m_nLeng = nIndex + 1;
        }
        m_pData[nIndex] = value;
    }
    public int FindNextNull(int nIndex)
    {
        for (; nIndex < m_nLeng; ++nIndex)
        {
            if (m_pData[nIndex] == null)
                return nIndex;
        }
        return m_nLeng;
    }
    public _Ty[] ToArray()
    {
        if (0 == m_nLeng)
            return null;
        _Ty[] aTemp = new _Ty[m_nLeng];
        Array.Copy(m_pData, 0, aTemp, 0, m_nLeng);
        return aTemp;
    }
    public bool Sort(System.Comparison<_Ty> comparer)
    {
        if (m_nLeng <= 1)
            return false;

        bool bSort = false;
        _Ty temp;
        int i = m_nLeng - 1;
        int j = 0;
        int nSwapIndex = 0;
        while (i > 0)
        {
            nSwapIndex = 0;
            for (j = 0; j < i; j++)
            {
                if (comparer(m_pData[j + 1], m_pData[j]) < 0)
                {//
                    temp = m_pData[j + 1];
                    m_pData[j + 1] = m_pData[j];
                    m_pData[j] = temp;
                    nSwapIndex = j;//记录交换下标
                    bSort = true;
                }
            }
            i = nSwapIndex;
        }
        return bSort;
    }
    public delegate int custom_compare<_TyParam>(_Ty p1, _Ty p2, _TyParam param);
    public bool Sort<_TyParam>(custom_compare<_TyParam> comparer, _TyParam param)
    {
        if (m_nLeng <= 1)
            return false;
        bool changed = true;
        bool bSort = false;

        while (changed)
        {
            changed = false;

            for (int i = 1; i < m_nLeng; ++i)
            {
                if (comparer(m_pData[i - 1], m_pData[i], param) > 0)
                {
                    _Ty temp = m_pData[i];
                    m_pData[i] = m_pData[i - 1];
                    m_pData[i - 1] = temp;
                    changed = true;
                    bSort = true;
                }
            }
        }
        return bSort;
    }
};

public class CFastList<_Ty>
{
    public class CFastListNode
    {
        public CFastListNode m_pLast;
        public CFastListNode m_pNext;
        public _Ty m_value;
    };

    CFastListNode m_pHeader;
    CFastListNode m_pEnd;
    iterator  m_pEndItera;
    int m_nCount;
        
    public class iterator
    {
        CFastListNode m_pNode;
        public iterator() 
        {
        }
        public iterator(CFastListNode pNode)
        {
            m_pNode = pNode;
        }
        public static iterator operator ++(iterator it)
        {
            it.m_pNode = it.m_pNode.m_pNext;
            return it;
        }
        public _Ty value
        {
            get { return m_pNode.m_value; }
        }
        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(iterator))
            {
                return Equals((iterator)obj);
            }
            return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        bool Equals(iterator it)
        {
            return m_pNode == it.m_pNode;
        }
        public static bool operator ==(iterator itA, iterator itB)
        {
            return itA.m_pNode == itB.m_pNode;
        }
        public static bool operator !=(iterator itA, iterator itB)
        {
            return itA.m_pNode != itB.m_pNode;
        }
        public CFastListNode get_ptr()
        {
            return m_pNode;
        }
    };

    public CFastList()
    {
        m_pHeader = m_pEnd = new CFastListNode();
        m_pEnd.m_pLast = m_pEnd;
        m_nCount = 0;
        m_pEndItera = new iterator(m_pEnd);
    }
    ~CFastList()
    {
        clear();
        m_pHeader = null;
        m_pEnd.m_pLast = null;
        m_pEnd.m_pNext = null;
        m_pEnd = null;
    }
    public int size()
    {
        return m_nCount;
    }
    public _Ty front()
    {
        return m_pHeader.m_value;
    }
    public _Ty back()
    {
        return m_pEnd.m_pLast.m_value;
    }
    public CFastListNode front_ptr()
    {
        return m_pHeader;
    }
    public CFastListNode back_ptr()
    {
        return m_pEnd;
    }
    public void pop_front()
    {
        if (m_pHeader != m_pEnd && m_nCount > 0)
        {
            m_pHeader = m_pHeader.m_pNext;
            --m_nCount;
        }
    }
    public void pop_back()
    {
        if( m_nCount > 0 )
        {
            CFastListNode pNode = m_pEnd;
            if (pNode.m_pLast != null)
                pNode.m_pLast.m_pNext = m_pEnd;
            m_pEnd.m_pLast = pNode.m_pLast;
            --m_nCount;

            pNode.m_pLast = null;
            pNode.m_pNext = null;
        }
    }
    public void push_back(_Ty value)
    {
        CFastListNode pNode = new CFastListNode();
        pNode.m_value = value;
        if (0 == m_nCount)
        {
            m_pHeader = pNode;
            pNode.m_pNext = m_pEnd;
            m_pEnd.m_pLast = pNode;
        }
        else
        {
            pNode.m_pLast = m_pEnd.m_pLast;
            pNode.m_pLast.m_pNext = pNode;
            pNode.m_pNext  = m_pEnd;
            m_pEnd.m_pLast = pNode;
        }
        ++m_nCount;
    }
    public void push_back(CFastListNode pNode)
    {
        if (0 == m_nCount)
        {
            m_pHeader = pNode;
            pNode.m_pNext = m_pEnd;
            m_pEnd.m_pLast = pNode;
        }
        else
        {
            pNode.m_pLast = m_pEnd.m_pLast;
            pNode.m_pLast.m_pNext = pNode;
            pNode.m_pNext = m_pEnd;
            m_pEnd.m_pLast = pNode;
        }
        ++m_nCount;
    }
    public void clear()
    {
        CFastListNode pNode = null;
        while (m_pHeader != m_pEnd)
        {
            pNode = m_pHeader;
            m_pHeader = m_pHeader.m_pNext;
            pNode.m_pLast = null;
            pNode.m_pNext = null;
        }
        m_pHeader      = m_pEnd;
        m_pEnd.m_pLast = m_pEnd;
        m_nCount       = 0;
    }
    public void swap( CFastList<_Ty> other)
    {
        CFastListNode temp = null;
        temp = m_pHeader; m_pHeader = other.m_pHeader; other.m_pHeader = temp;
        temp = m_pEnd; m_pEnd = other.m_pEnd; other.m_pEnd = temp;
        iterator pIt = null;
        pIt = m_pEndItera; m_pEndItera = other.m_pEndItera; other.m_pEndItera = pIt;
        int nTemp = 0;
        nTemp = m_nCount; m_nCount = other.m_nCount; other.m_nCount = nTemp;
    }
    public iterator begin()
    {
        return new iterator(m_pHeader);
    }
    public iterator end()
    {
        return m_pEndItera;
    }
    public void erase(iterator itWhere)
    {
        if (itWhere != m_pEndItera)
        {
            CFastListNode pNode = itWhere.get_ptr();
            if (pNode == m_pHeader)
            {
                m_pHeader = m_pHeader.m_pNext;
                m_pHeader.m_pLast = null;
                if (m_pHeader == m_pEnd)
                    m_pEnd.m_pLast = m_pEnd;
            }
            else
            {
                if (pNode.m_pLast != null)
                    pNode.m_pLast.m_pNext = pNode.m_pNext;
                if (pNode.m_pNext != null)
                    pNode.m_pNext.m_pLast = pNode.m_pLast;
            }
            pNode.m_pLast = null;
            pNode.m_pNext = null;
            --m_nCount;
        }
    }
};

// 线程安全的对象
public class CThreadFastList<_Ty>
{
    CFastList<_Ty> m_List = new CFastList<_Ty>();
    public void push_back(_Ty value, int nMaxCount = 0)
    {
        CFastList<_Ty>.CFastListNode node = new CFastList<_Ty>.CFastListNode();
        node.m_value = value;
        lock (this)
        {
            if (nMaxCount <= 0 || m_List.size() < nMaxCount)
                m_List.push_back(node);
        }
    }
    public void pop_list( ref CFastList<_Ty> sList )
    {
        sList.clear();
        lock (this)
        {
            sList.swap(m_List);
        }
    }
};