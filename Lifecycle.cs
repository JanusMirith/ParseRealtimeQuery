using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Parse.LiveQuery
{
    /// <summary>
    /// A lightweight singleton that allows for the system to understand some aspects of the unity lifecycle
    /// </summary>
    public class Lifecycle : MonoBehaviour
    {
        /// <summary>
        /// The actual instance of the object
        /// </summary>
        private static Lifecycle _instance;

        /// <summary>
        /// This is the instance for this object
        /// </summary>
        private static Lifecycle Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<Lifecycle>();

                    if (_instance == null)
                    {
                        GameObject go = new GameObject();
                        _instance = go.AddComponent<Lifecycle>();
                        go.name = _instance.GetType().ToString();
                        DontDestroyOnLoad(_instance);
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// The backing event for the <see cref="OnUnityQuit"/> message
        /// </summary>
        private event Action onUnityQuit;

        /// <summary>
        /// The backing event for the <see cref="onUnityFocus"/> message
        /// </summary>
        private event Action<bool> onUnityFocus;


        /// <summary>
        /// Echos unity's OnApplicationQuit
        /// </summary>
        public static event Action OnUnityQuit
        {
            add { Instance.onUnityQuit += value; }
            remove { Instance.onUnityQuit -= value; }
        }

        /// <summary>
        /// Echos unity's OnApplicationFocus
        /// </summary>
        public static event Action<bool> OnUnityFocus
        {
            add { Instance.onUnityFocus += value; }
            remove { Instance.onUnityFocus -= value; }
        }

        /// <summary>
        /// Part of the unity lifecycle, called when the focus changes
        /// </summary>
        /// <param name="focus">true when focus returns</param>
        private void OnApplicationFocus(bool focus)
        {
            onUnityFocus?.Invoke(focus);
        }

        /// <summary>
        /// Part of the unity lifecycle called when the app quits
        /// </summary>
        private void OnApplicationQuit()
        {
            onUnityQuit?.Invoke();
        }
    }
}
