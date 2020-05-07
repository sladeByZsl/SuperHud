//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Update manager allows for simple programmatic ordering of update events.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/Internal/Update Manager")]
public class UpdateManager : MonoBehaviour
{
	public delegate void OnUpdate (float delta);

	public class UpdateEntry
	{
		public int index = 0;
		public OnUpdate func;
		public MonoBehaviour mb;
		public bool isMonoBehaviour = false;
	}

	public class DestroyEntry
	{
		public UnityEngine.Object obj;
		public float time;
	}

	static int Compare (UpdateEntry a, UpdateEntry b)
	{
		if (a.index < b.index) return 1;
		if (a.index > b.index) return -1;
		return 0;
	}

	static UpdateManager mInst;
	List<UpdateEntry> mOnUpdate = new List<UpdateEntry>();
	List<UpdateEntry> mOnLate = new List<UpdateEntry>();
	List<UpdateEntry> mOnCoro = new List<UpdateEntry>();
    BetterList<DestroyEntry> mDest = new BetterList<DestroyEntry>();
    int mNullWidgetCount = 0;
    int mNullPanelCount = 0;
    int nDirtyVer = 0;
    float mTime = 0f;
    bool mStartCoro = false;

	/// <summary>
	/// Ensure that there is an instance of this class present.
	/// </summary>

	static void CreateInstance ()
	{
		if (mInst == null)
		{
			mInst = GameObject.FindObjectOfType(typeof(UpdateManager)) as UpdateManager;

			//if (mInst == null && Application.isPlaying)
            if(mInst == null)
			{
                GameObject go = new GameObject("_UpdateManager");
                if(Application.isPlaying)
                    DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.DontSave;
				//go.hideFlags = HideFlags.HideAndDontSave;
				mInst = go.AddComponent<UpdateManager>();
			}
		}
	}

	/// <summary>
	/// Update the specified list.
	/// </summary>

	void UpdateList (List<UpdateEntry> list, float delta)
	{
		for (int i = list.Count; i > 0; )
		{
			UpdateEntry ent = list[--i];
            
			// If it's a monobehaviour we need to do additional checks
			if (ent.isMonoBehaviour)
			{
				// If the monobehaviour is null, remove this entry
				if (ent.mb == null)
				{
					list.RemoveAt(i);
					continue;
				}                
			}

			// Call the function
            try
            {
                ent.func(delta);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
		}
	}

	/// <summary>
	/// Start the coroutine.
	/// </summary>

	void Start ()
	{
		if (Application.isPlaying)
		{
			mTime = Time.realtimeSinceStartup;
            if(mOnCoro != null && mOnCoro.Count > 0 )
                StartCoroutine(CoroutineFunction());
		}
	}

    void  AutoStartCoro()
    {
        if (mStartCoro)
            return;
        if (mOnCoro.Count > 0 || mDest.size > 0 )
            StartCoroutine(CoroutineFunction());
    }

	/// <summary>
	/// Don't keep this class around after stopping the Play mode.
	/// </summary>

	void OnApplicationQuit () { DestroyImmediate(gameObject); }

	/// <summary>
	/// Call all update functions.
	/// </summary>

	void Update ()
	{
        if (mInst != this) Tool_Destroy(gameObject);
		else UpdateList(mOnUpdate, Time.deltaTime);

        // 处理
	}
        
	/// <summary>
	/// Call all late update functions and destroy this class if no callbacks have been registered.
	/// </summary>

	void LateUpdate ()
	{
        UpdateList(mOnLate, Time.deltaTime);
		if (!Application.isPlaying) CoroutineUpdate();
	}

	/// <summary>
	/// Call all coroutine update functions and destroy all delayed destroy objects.
	/// </summary>

	bool CoroutineUpdate ()
	{
		float time = Time.realtimeSinceStartup;
		float delta = time - mTime;
		if (delta < 0.001f) return true;

		mTime = time;

		UpdateList(mOnCoro, delta);

		bool appIsPlaying = Application.isPlaying;

		for (int i = mDest.size; i > 0; )
		{
			DestroyEntry de = mDest.buffer[--i];

			if (!appIsPlaying || de.time < mTime)
			{
				if (de.obj != null)
				{
                    Tool_Destroy(de.obj);
					de.obj = null;
				}
				mDest.RemoveAt(i);
			}
		}
        
		// Nothing left to update? Destroy this game object.
		if (mOnUpdate.Count == 0 && mOnLate.Count == 0 && mOnCoro.Count == 0 && mDest.size == 0)
		{
            Tool_Destroy(gameObject);
            mInst = null;
            return false;
		}
        if (mOnCoro.Count == 0 && mDest.size == 0)
            return false;

		return true;
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

    /// <summary>
    /// Coroutine update function.
    /// </summary>

    IEnumerator CoroutineFunction ()
	{
        mStartCoro = true;
        while (Application.isPlaying)
		{
			if (CoroutineUpdate()) yield return null;
			else break;
		}
        mStartCoro = false;
    }

	/// <summary>
	/// Generic add function.
	/// Technically 'mb' is not necessary as it can be retrieved by calling 'func.Target as MonoBehaviour'.
	/// Unfortunately Flash export fails to compile with that, claiming the following:
	/// "Error: Access of possibly undefined property Target through a reference with static type Function."
	/// </summary>

	void Add (MonoBehaviour mb, int updateOrder, OnUpdate func, List<UpdateEntry> list)
	{
#if !UNITY_FLASH
		// Flash export fails at life.
		for (int i = 0, imax = list.Count; i < imax; ++i)
		{
			UpdateEntry ent = list[i];
            if (ent.func == func) {
                ent.index = updateOrder;
                ent.func = func;
                ent.mb = mb;
                ent.isMonoBehaviour = (mb != null);
                return;
            }
		}
#endif
		UpdateEntry item = new UpdateEntry();
		item.index = updateOrder;
		item.func = func;
		item.mb = mb;
		item.isMonoBehaviour = (mb != null);

		list.Add(item);
		if (updateOrder != 0) list.Sort(Compare);
	}
    
    void Del(OnUpdate func, List<UpdateEntry> list)
    {
        for( int i = list.Count - 1; i>=0; --i)
        {
            UpdateEntry ent = list[i];
            if(ent.func == func)
            {
                ent.isMonoBehaviour = true;
                ent.mb = null;
                break;
            }
        }
    }

	/// <summary>
	/// Add a new update function with the specified update order.
	/// </summary>

	static public void AddUpdate (MonoBehaviour mb, int updateOrder, OnUpdate func) { CreateInstance(); mInst.Add(mb, updateOrder, func, mInst.mOnUpdate); }

    static public void DelUpdate(OnUpdate func) { if (mInst != null) mInst.Del(func, mInst.mOnUpdate); }

	/// <summary>
	/// Add a new late update function with the specified update order.
	/// </summary>

	static public void AddLateUpdate (MonoBehaviour mb, int updateOrder, OnUpdate func) { CreateInstance(); mInst.Add(mb, updateOrder, func, mInst.mOnLate); }

    static public void DelLateUpdate(OnUpdate func) { if (mInst != null) mInst.Del(func, mInst.mOnLate); }
    /// <summary>
    /// Add a new coroutine update function with the specified update order.
    /// </summary>

    static public void AddCoroutine (MonoBehaviour mb, int updateOrder, OnUpdate func) { CreateInstance(); mInst.Add(mb, updateOrder, func, mInst.mOnCoro); mInst.AutoStartCoro(); }

    static public void DelCoroutine(OnUpdate func) { if (mInst != null) mInst.Del(func, mInst.mOnCoro); }
    
    /// <summary>
    /// Destroy the object after the specified delay in seconds.
    /// </summary>

    static public void AddDestroy ( UnityEngine.Object obj, float delay)
	{
		if (obj == null) return;

		if (Application.isPlaying)
		{
			if (delay > 0f)
			{
				CreateInstance();

				DestroyEntry item = new DestroyEntry();
				item.obj = obj;
				item.time = Time.realtimeSinceStartup + delay;
				mInst.mDest.Add(item);
                mInst.AutoStartCoro();
            }
			else Destroy(obj);
		}
		else DestroyImmediate(obj);
	}
}