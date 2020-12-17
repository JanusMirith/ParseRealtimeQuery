using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Parse;
using UnityEditor;
using System;
using System.Linq;

namespace Parse.LiveQuery
{
    /// <summary>
    /// This is a realtime query, upon creating this your quiry will be sent and all objects that match will be sent via the OnEnter event
    /// If you subscribe to the onEnter event of a running realtime Query then every object that currently matches the quiry will be sent as OnEnter events
    /// 
    /// Please note that not all quiry types are supported by live querys
    /// </summary>
    /// <typeparam name="T">The type of object you are requesting</typeparam>
    public class RealtimeQuery<T> where T : ParseObject
    {
        /// <summary>
        /// The query that is sent to parse
        /// </summary>
        private readonly ParseQuery<T> query;

        /// <summary>
        /// should the system try catch all the calls
        /// </summary>
        private readonly bool slowAndSafe;

        /// <summary>
        /// should the events be called on the main thread
        /// </summary>
        private readonly bool runOnMainThread;
        private readonly bool enterFiresUpdate;

        /// <summary>
        /// The core subscriptiton for this realtime query
        /// </summary>
        private Subscription<T> subscription;

        private readonly ParseClient parseClient;
        private readonly ParseLiveQueryClient parseLiveQueryClient;

        /// <summary>
        /// A list of all the listeners to the on enter event
        /// </summary>
        private List<Action<T>> OnEnterListeners;

        /// <summary>
        /// A list of all the listeners to the on enter event
        /// </summary>
        private List<Action<T>> OnCreateListeners;

        /// <summary>
        /// A list of all the listeners to the on enter event
        /// </summary>
        private List<Action<T>> OnUpdateListeners;

        /// <summary>
        /// A list of all the listeners to the on enter event
        /// </summary>
        private List<Action<T>> OnLeaveListeners;

        /// <summary>
        /// A list of all the listeners to the on enter event
        /// </summary>
        private List<Action<T>> OnDeleteListeners;

        /// <summary>
        /// A list of all the listeners to the on destroy event
        /// </summary>
        private List<Action> OnDestroyListenters;

        /// <summary>
        /// Called when the object is has entered the query
        /// </summary>
        public event Action<T> OnEnter
        {
            add
            {
                if (OnEnterListeners != null)
                {
                    // Go through and simulate the entery of all current objects for this new listener
                    foreach (var obj in watchedObjects.Values)
                    {
                        CallEventSingle(obj, value);
                    }
                    OnEnterListeners.Add(value);
                }
            }
            remove
            {
                if (OnEnterListeners != null)
                {
                    OnEnterListeners.Remove(value);
                }
                else
                {
                    
                }
            }
        }

        /// <summary>
        /// Called when a object matching the query is created
        /// </summary>
        public event Action<T> OnCreate
        {
            add
            {
                if (OnCreateListeners != null)
                {
                    OnCreateListeners.Add(value);
                }
            }
            remove
            {
                if (OnCreateListeners != null)
                {
                    OnCreateListeners.Remove(value);
                }
            }
        }

        /// <summary>
        /// Called when a object matching the query is updated
        /// </summary>
        public event Action<T> OnUpdate
        {
            add
            {
                if (OnUpdateListeners != null)
                {
                    foreach (var obj in watchedObjects.Values)
                    {
                        CallEventSingle(obj, value);
                    }
                    OnUpdateListeners.Add(value);
                }
            }
            remove
            {
                if (OnUpdateListeners != null)
                {
                    OnUpdateListeners.Remove(value);
                }
            }
        }

        /// <summary>
        /// Called when a object has stopped matching the query 
        /// </summary>
        public event Action<T> OnLeave
        {
            add
            {
                if (OnLeaveListeners != null)
                {
                    OnLeaveListeners.Add(value);
                }
            }
            remove
            {
                if (OnLeaveListeners != null)
                {
                    OnLeaveListeners.Remove(value);
                }
            }
        }

        /// <summary>
        /// Called when a object matching the query is Deleted
        /// </summary>
        public event Action<T> OnDelete
        {
            add
            {
                if (OnDeleteListeners != null)
                {
                    OnDeleteListeners.Add(value);
                }
            }
            remove
            {
                if (OnDeleteListeners != null)
                {
                    OnDeleteListeners.Remove(value);
                }
            }
        }

        /// <summary>
        /// Called when this realtime query is destroyed
        /// </summary>
        public event Action OnDestroy
        {
            add
            {
                if (OnDestroyListenters != null)
                {
                    OnDestroyListenters.Add(value);
                }
            }
            remove
            {
                if (OnDestroyListenters != null)
                {
                    OnDestroyListenters.Remove(value);
                }
            }
        }

        /// <summary>
        /// this is a list of the objects currently known by this quiry
        /// </summary>
        private Dictionary<string, T> watchedObjects;

        /// <summary>
        /// this is a list of the objects currently known by this quiry
        /// </summary>
        public Dictionary<string, T> WatchedObjects
        {
            get
            {
                if (watchedObjects == null)
                {
                    watchedObjects = new Dictionary<string, T>();
                }
                return watchedObjects;
            }
        }

        /// <summary>
        /// Creates a new realtime query
        /// </summary>
        /// <param name="query">The core query to base this request on</param>
        /// <param name="slowAndSafe">Should the system try catch each event called?</param>
        /// <param name="runOnMainThread">Shoud the system make sure the events will only be called on the main thread</param>
        /// <param name="parseClient">an override to the parse client</param>
        /// <param name="parseLiveQueryClient">an overide to the live query client</param>
        /// <param name="enterFiresUpdate">when an object enters the query a update event will be called after enter</param>
        public RealtimeQuery(ParseQuery<T> query, bool slowAndSafe = false, bool runOnMainThread = true, ParseClient parseClient = null, ParseLiveQueryClient parseLiveQueryClient = null, bool enterFiresUpdate = true)
        {
            if (!ContextCache.IsInitialized)
            {
                ContextCache.Initialize();
            }

            // Get the collections started
            watchedObjects = new Dictionary<string, T>();
            OnEnterListeners = new List<Action<T>>();
            OnCreateListeners = new List<Action<T>>();
            OnUpdateListeners = new List<Action<T>>();
            OnLeaveListeners = new List<Action<T>>();
            OnDeleteListeners = new List<Action<T>>();
            OnDestroyListenters = new List<Action>();

            this.query = query;
            this.slowAndSafe = slowAndSafe;
            this.runOnMainThread = runOnMainThread;
            this.enterFiresUpdate = enterFiresUpdate;

            // look to see if there is a ParseClientOverride
            if (parseClient == null)
            {
                this.parseClient = ParseClient.Instance;
            }
            else
            {
                this.parseClient = parseClient;
            }

            if (parseLiveQueryClient == null)
            {
                this.parseLiveQueryClient = ParseLiveQueryClient.Instance;
            }
            else
            {
                this.parseLiveQueryClient = parseLiveQueryClient;
            }

            MonobehaviourListener.onApplicationQuit += Destroy;

            MonobehaviourListener.onApplicationFocus += OnFocusChanged;

            // Setup the query
            SetupQuery();
        }

        private void OnFocusChanged(bool focus)
        {
            //Refocused the app
            if (focus)
            {
                query.FindAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Debug.Log(t.IsFaulted);
                        Debug.Log(t.Exception);
                        Debug.Log(t.Exception.Message);
                    }
                    else
                    {
                        if (t.Result.GetEnumerator() != null)
                        {
                            foreach (T obj in t.Result)
                            {
                                Enter(obj);
                            }
                        }
                        List<T> objectsToKill = new List<T>();
                        foreach(T obj in watchedObjects.Values)
                        {
                            if (t.Result.Contains<T>(obj))
                            {
                                // This is fine 
                            }
                            else
                            {
                                objectsToKill.Add(obj);
                            }
                        }
                        for (int i = 0; i < objectsToKill.Count; i++)
                        {
                            Leave(objectsToKill[i]);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Now that we are init, time to send the Query
        /// </summary>
        private void SetupQuery()
        {
            query.FindAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.Log(t.IsFaulted);
                    Debug.Log(t.Exception);
                    Debug.Log(t.Exception.Message);
                }
                else
                {
                    if (t.Result.GetEnumerator() != null)
                    {
                        foreach (T obj in t.Result)
                        {

                            Enter(obj);
                        }


                    }
                }
            });

            subscription = parseLiveQueryClient.Subscribe(query);
            subscription.OnCreate += Sub_OnCreate;
            subscription.OnDelete += Sub_OnDelete;
            subscription.OnEnter += Sub_OnEnter;
            subscription.OnLeave += Sub_OnLeave;
            subscription.OnUpdate += Sub_OnUpdate;
            subscription.OnError += Subscription_OnError;
        }

        private void Subscription_OnError(ParseQuery<T> query, LiveQueryException exception)
        {
            Debug.LogException(exception);
        }

        /// <summary>
        /// Destroys this realtime quiry
        /// </summary>
        public void Destroy()
        {
            parseLiveQueryClient.Unsubscribe<T>(query);
            watchedObjects = null;
            OnEnterListeners = new List<Action<T>>();
            OnCreateListeners = new List<Action<T>>();
            OnUpdateListeners = new List<Action<T>>();
            OnLeaveListeners = new List<Action<T>>();
            OnDeleteListeners = new List<Action<T>>();
            CallEventList(OnDestroyListenters);

            MonobehaviourListener.onApplicationFocus -= OnFocusChanged;
            MonobehaviourListener.onApplicationQuit -= Destroy;
        }

        /// <summary>
        /// A callback direct from the liveQuiry
        /// </summary>
        /// <param name="query">a reference to the query</param>
        /// <param name="obj">the object returned</param>
        private void Sub_OnUpdate(ParseQuery<T> query, T obj)
        {
            Update(obj);
        }

        /// <summary>
        /// A callback direct from the liveQuiry
        /// </summary>
        /// <param name="query">a reference to the query</param>
        /// <param name="obj">the object returned</param>
        private void Sub_OnLeave(ParseQuery<T> query, T obj)
        {
            Leave(obj);
        }

        /// <summary>
        /// A callback direct from the liveQuiry
        /// </summary>
        /// <param name="query">a reference to the query</param>
        /// <param name="obj">the object returned</param>
        private void Sub_OnEnter(ParseQuery<T> query, T obj)
        {
            Enter(obj);
        }

        /// <summary>
        /// A callback direct from the liveQuiry
        /// </summary>
        /// <param name="query">a reference to the query</param>
        /// <param name="obj">the object returned</param>
        private void Sub_OnDelete(ParseQuery<T> query, T obj)
        {
            Delete(obj);
        }

        /// <summary>
        /// A callback direct from the liveQuiry
        /// </summary>
        /// <param name="query">a reference to the query</param>
        /// <param name="obj">the object returned</param>
        private void Sub_OnCreate(ParseQuery<T> query, T obj)
        {
            Create(obj);
        }


        /// <summary>
        /// Called on the create event from the subscription
        /// </summary>
        /// <param name="obj">the returned object</param>
        private void Create(T obj)
        {
            // Look to see if we know about this 
            if (watchedObjects.Keys.Contains(obj.ObjectId))
            {
                // Make this a update instead
                Update(obj);
            }
            else
            {
                watchedObjects[obj.ObjectId] = obj;
                CallEventList(obj, OnCreateListeners);
            }
        }

        /// <summary>
        /// Called on the enter event from the subscription
        /// </summary>
        /// <param name="obj">the returned object</param>
        private void Enter(T obj)
        {
            // Look to see if we know about this 
            if (watchedObjects.Keys.Contains(obj.ObjectId))
            {
                // Make this a update instead
                Update(obj);
            }
            else
            {
                watchedObjects[obj.ObjectId] = obj;
                CallEventList(obj, OnEnterListeners);

                // if desired we should also send update events
                if (enterFiresUpdate)
                {
                    Update(obj);
                }
            }
        }

        /// <summary>
        /// Called on the update event from the subscription
        /// </summary>
        /// <param name="obj">the returned object</param>
        private void Update(T obj)
        {
            // Look to see if we know about this 
            if (!watchedObjects.Keys.Contains(obj.ObjectId))
            {
                // Make this a enter instead
                Enter(obj);
            }
            else
            {
                watchedObjects[obj.ObjectId] = obj;
                CallEventList(obj, OnUpdateListeners);
            }
        }

        /// <summary>
        /// Called on the leave event from the subscription
        /// </summary>
        /// <param name="obj">the returned object</param>
        private void Leave(T obj)
        {
            // Look to see if we know about this 
            if (watchedObjects.Keys.Contains(obj.ObjectId))
            {
                watchedObjects.Remove(obj.ObjectId);
                CallEventList(obj, OnLeaveListeners);
            }
            else
            {
                // Do Nothing
            }
        }

        /// <summary>
        /// Called on the delete event from the subscription
        /// </summary>
        /// <param name="obj">the returned object</param>
        private void Delete(T obj)
        {
            // Look to see if we know about this 
            if (watchedObjects.Keys.Contains(obj.ObjectId))
            {
                watchedObjects.Remove(obj.ObjectId);
                CallEventList(obj, OnDeleteListeners);
            }
            else
            {
                // Do Nothing
            }
        }

        /// <summary>
        /// Calls the event in a predictable way
        /// </summary>
        /// <param name="obj">The object to send for the event</param>
        /// <param name="listenerList">The list of the listerners for this event</param>
        private void CallEventList(T obj, List<Action<T>> listenerList)
        {
            for (int i = 0; i < listenerList.Count; i++)
            {
                // Call each event singley, technicaly this is a tad slower then doing the ifs around slow and safe first but more robust in the terms of code reuse
                CallEventSingle(obj, listenerList[i]);
            }
        }

        /// <summary>
        /// Calls the event in a predictable way
        /// </summary>
        /// <param name="obj">The object to send for the event</param>
        /// <param name="listener">The listener for the event</param>
        private void CallEventSingle(T obj, Action<T> listener)
        {
            // If we are doing this slow and safe then we should try catch the call
            if (slowAndSafe)
            {

                // if need be run on the main thread
                if (runOnMainThread)
                {
                    // Join the main thread
                    ContextCache.DoOnMainThread(() =>
                    {
                        try
                        {
                            // try to invoke it
                            listener?.Invoke(obj);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                        }
                    });
                }
                else
                {
                    try
                    {
                        // Try to invoke it
                        listener?.Invoke(obj);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

            }
            else
            {
                if (runOnMainThread)
                {
                    // Invoke on main thread
                    ContextCache.DoOnMainThread(() =>
                    {
                        listener?.Invoke(obj);
                    });
                }
                else
                {
                    // Just invoke it
                    listener?.Invoke(obj);
                }
            }
        }

        /// <summary>
        /// Calls the event in a predictable way
        /// </summary>
        /// <param name="obj">The object to send for the event</param>
        /// <param name="listenerList">The list of the listerners for this event</param>
        private void CallEventList(List<Action> listenerList)
        {
            for (int i = 0; i < listenerList.Count; i++)
            {
                // Call each event singley, technicaly this is a tad slower then doing the ifs around slow and safe first but more robust in the terms of code reuse
                CallEventSingle(listenerList[i]);
            }
        }

        /// <summary>
        /// Calls the event in a predictable way
        /// </summary>
        /// <param name="obj">The object to send for the event</param>
        /// <param name="listener">The listener for the event</param>
        private void CallEventSingle(Action listener)
        {
            // If we are doing this slow and safe then we should try catch the call
            if (slowAndSafe)
            {

                // if need be run on the main thread
                if (runOnMainThread)
                {
                    // Join the main thread
                    ContextCache.DoOnMainThread(() =>
                    {
                        try
                        {
                            // try to invoke it
                            listener?.Invoke();
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                        }
                    });
                }
                else
                {
                    try
                    {
                        // Try to invoke it
                        listener?.Invoke();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

            }
            else
            {
                if (runOnMainThread)
                {
                    // Invoke on main thread
                    ContextCache.DoOnMainThread(() =>
                    {
                        listener?.Invoke();
                    });
                }
                else
                {
                    // Just invoke it
                    listener?.Invoke();
                }
            }
        }
    }
}
