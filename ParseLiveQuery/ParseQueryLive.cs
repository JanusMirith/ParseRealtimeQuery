using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Parse;
using Parse.Abstractions.Infrastructure;

namespace Parse.LiveQuery
{
    /// <summary>
    /// A derived class of ParseQuery that does not hide some of its data so it can be used with livequirys, there is for sure a better way to do this but for the moment it will work
    /// </summary>
    public class ParseQueryLive<T> : ParseQuery<T> where T : ParseObject 
    {
        /// <summary>
        /// The serviceHub for this query
        /// </summary>
        public IServiceHub ServiceHub { get; }

        /// <summary>
        /// The Classname for this query
        /// </summary>
        public string ClassName { get; }

        /// <summary>
        /// Constructs a query based upon the ParseObject subclass used as the generic parameter
        //     for the ParseQuery.
        /// </summary>
        /// <param name="serviceHub">the service hub to use for this request</param>
        public ParseQueryLive(IServiceHub serviceHub) : base(serviceHub)
        {
            ServiceHub = serviceHub;
        }

        /// <summary>
        /// Constructs a query. A default query with no further parameters will retrieve
        ///     all Parse.ParseObjects of the provided class.
        /// </summary>
        /// <param name="serviceHub">the service hub to use for this request</param>
        /// <param name="className">The name of the class to retrieve ParseObjects for.</param>
        public ParseQueryLive(IServiceHub serviceHub, string className) : base(serviceHub, className)
        {
            ServiceHub = serviceHub;
            ClassName = className;
        }
    }
}
