using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Data;
using System.Threading.Tasks;
using System;

public class ContextCache : MonoBehaviour
{
    /// <summary>
    /// Has the system been initialized
    /// </summary>
    public static bool IsInitialized;

    /// <summary>
    /// Initilaize the system
    /// </summary>
    public static void Initialize()
    {
        GameObject myGameObject = new GameObject("ContextCache", typeof(ContextCache));
        IsInitialized = true;
        DontDestroyOnLoad(myGameObject);
    }

    private static SynchronizationContext mainThreadContext;

    public static SynchronizationContext MainThreadContext
    {
        get
        {
            if (mainThreadContext == null)
            {
                throw new NoNullAllowedException("ContextCache must be placed in the scene to initialise"); ;
            }
            return mainThreadContext;
        }
    }

    public static void DoOnMainThread(System.Action action, Action callback = null)
    {
        MainThreadContext.Post(delegate (object s) { action?.Invoke(); callback?.Invoke(); }, null);
    }

    private void Awake()
    {
        if (mainThreadContext == null)
        {
            mainThreadContext = SynchronizationContext.Current;
        }
    }

    private void Update()
    {
        if (mainThreadContext == null)
        {
            mainThreadContext = SynchronizationContext.Current;
        }
    }
}
