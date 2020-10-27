using System;
using System.Collections.Generic;
using System.Diagnostics;
using Parse;
using Parse.Abstractions.Internal;

namespace Parse.LiveQuery {
    public class Subscription<T> : Subscription where T : ParseObject {

        private readonly List<EventsCallback<T>> _eventsCallbacks = new List<EventsCallback<T>>();
        private readonly List<ErrorCallback<T>> _errorCallbacks = new List<ErrorCallback<T>>();
        private readonly List<SubscribeCallback<T>> _subscribeCallbacks = new List<SubscribeCallback<T>>();
        private readonly List<UnsubscribeCallback<T>> _unsubscribeCallbacks = new List<UnsubscribeCallback<T>>();

        /// <summary>
        /// Called when the object is has entered the query
        /// </summary>
        public event EventCallback<T> OnEnter;
        /// <summary>
        /// Called when a object matching the query is created
        /// </summary>
        public event EventCallback<T> OnCreate;

        /// <summary>
        /// Called when a object matching the query is updated
        /// </summary>
        public event EventCallback<T> OnUpdate;

        /// <summary>
        /// Called when a object has stopped matching the query 
        /// </summary>
        public event EventCallback<T> OnLeave;

        /// <summary>
        /// Called when a object matching the query is Deleted
        /// </summary>
        public event EventCallback<T> OnDelete;

        /// <summary>
        /// Called when there is an error
        /// </summary>
        public event ErrorCallback<T> OnError;

        internal Subscription(int requestId, ParseQuery<T> query) {
            RequestId = requestId;
            Query = query;
        }

        public int RequestId { get; }

        internal ParseQuery<T> Query { get; }

        internal override object QueryObj => Query;

        /// <summary>
        /// Register a callback for when any event occurs.
        /// </summary>
        /// <param name="callback">The events callback to register.</param>
        /// <returns>The same Subscription, for easy chaining.</returns>
        public Subscription<T> HandleEvents(EventsCallback<T> callback) {
            _eventsCallbacks.Add(callback);
            return this;
        }

        /// <summary>
        /// Register a callback for when a specific event occurs.
        /// </summary>
        /// <param name="objEvent">The event type to handle.</param>
        /// <param name="callback">The event callback to register.</param>
        /// <returns>The same Subscription, for easy chaining.</returns>
        public Subscription<T> HandleEvent(Event objEvent, EventCallback<T> callback) {
            return HandleEvents((query, callbackObjEvent, obj) => {
                if (callbackObjEvent == objEvent) callback(query, obj);
            });
        }

        /// <summary>
        /// Register a callback for when an error occurs.
        /// </summary>
        /// <param name="callback">The error callback to register.</param>
        /// <returns>The same Subscription, for easy chaining.</returns>
        public Subscription<T> HandleError(ErrorCallback<T> callback) {
            _errorCallbacks.Add(callback);
            return this;
        }

        /// <summary>
        /// Register a callback for when a client succesfully subscribes to a query.
        /// </summary>
        /// <param name="callback">The subscribe callback to register.</param>
        /// <returns>The same Subscription, for easy chaining.</returns>
        public Subscription<T> HandleSubscribe(SubscribeCallback<T> callback) {
            _subscribeCallbacks.Add(callback);
            return this;
        }

        /// <summary>
        /// Register a callback for when a query has been unsubscribed.
        /// </summary>
        /// <param name="callback">The unsubscribe callback to register.</param>
        /// <returns>The same Subscription, for easy chaining.</returns>
        public Subscription<T> HandleUnsubscribe(UnsubscribeCallback<T> callback) {
            _unsubscribeCallbacks.Add(callback);
            return this;
        }


        internal override void DidReceive(object queryObj, Event objEvent, ParseObject objState) {
            ParseQuery<T> query = (ParseQuery<T>) queryObj;
            //T obj = ParseClient.Instance.ServicesFromState<T>(objState, query.GetClassName());

            // Copyed from internal code
            //T obj = (T)ObjectServiceExtensions.CreateObjectWithoutData(ParseClient.Instance.Services, query.GetClassName(), objState.ObjectId);
            T obj = (T)objState;

            // Fire those events!
            switch (objEvent)
            {
                case Event.Create:
                    OnCreate?.Invoke(query, obj);
                    break;
                case Event.Enter:
                    OnEnter?.Invoke(query, obj);
                    break;
                case Event.Update:
                    OnUpdate?.Invoke(query, obj);
                    break;
                case Event.Leave:
                    OnLeave?.Invoke(query, obj);
                    break;
                case Event.Delete:
                    OnDelete?.Invoke(query, obj);
                    break;
                default:
                    throw new NotImplementedException("The following event type has not been handeled " + objEvent);
            }

            foreach (EventsCallback<T> eventsCallback in _eventsCallbacks) {
                eventsCallback(query, objEvent, obj);
            }
        }

        internal override void DidEncounter(object queryObj, LiveQueryException error) {
            foreach (ErrorCallback<T> errorCallback in _errorCallbacks) {
                errorCallback((ParseQuery<T>) queryObj, error);
            }
            OnError?.Invoke((ParseQuery<T>)queryObj, error);
        }

        internal override void DidSubscribe(object queryObj) {
            foreach (SubscribeCallback<T> subscribeCallback in _subscribeCallbacks) {
                subscribeCallback((ParseQuery<T>) queryObj);
            }
        }

        internal override void DidUnsubscribe(object queryObj) {
            foreach (UnsubscribeCallback<T> unsubscribeCallback in _unsubscribeCallbacks) {
                unsubscribeCallback((ParseQuery<T>) queryObj);
            }
        }

        internal override IClientOperation CreateSubscribeClientOperation(string sessionToken) {
            return new SubscribeClientOperation<T>(this, sessionToken);
        }

    }

    public abstract class Subscription {

        public string QuiryClass;

        internal abstract object QueryObj { get; }

        internal abstract void DidReceive(object queryObj, Event objEvent, ParseObject obj);

        internal abstract void DidEncounter(object queryObj, LiveQueryException error);

        internal abstract void DidSubscribe(object queryObj);

        internal abstract void DidUnsubscribe(object queryObj);

        internal abstract IClientOperation CreateSubscribeClientOperation(string sessionToken);

        public delegate void EventCallback<T>(ParseQuery<T> query, T obj) where T : ParseObject;

        public delegate void EventsCallback<T>(ParseQuery<T> query, Event objEvent, T obj) where T : ParseObject;

        public delegate void ErrorCallback<T>(ParseQuery<T> query, LiveQueryException exception) where T : ParseObject;

        public delegate void SubscribeCallback<T>(ParseQuery<T> query) where T : ParseObject;

        public delegate void UnsubscribeCallback<T>(ParseQuery<T> query) where T : ParseObject;

        public enum Event {
            Create,
            Enter,
            Update,
            Leave,
            Delete
        }

    }

}
