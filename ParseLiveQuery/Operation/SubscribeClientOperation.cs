using System.Collections.Generic;
using Parse;
using Parse.Abstractions.Internal;
using Parse.Infrastructure.Utilities;

namespace Parse.LiveQuery {
    public class SubscribeClientOperation<T> : SessionClientOperation where T : ParseObject {

        private static readonly Dictionary<string, object> EmptyJsonObject = new Dictionary<string, object>(0);

        private readonly int _requestId;
        private readonly ParseQuery<T> _query;

        internal SubscribeClientOperation(Subscription<T> subscription, string sessionToken) : base(sessionToken) {
            _requestId = subscription.RequestId;
            _query = subscription.Query;
        }

        // TODO: add support for fields
        // https://github.com/ParsePlatform/parse-server/issues/3671
        protected override IDictionary<string, object> ToJsonObject()
        {
            var poo = _query.BuildParameters();
            Dictionary<string, object> d = new Dictionary<string, object>
            {
                ["op"] = "subscribe",
                ["requestId"] = _requestId,
                ["query"] = new Dictionary<string, object>
                {
                    ["className"] = _query.GetClassName(),
                    ["where"] = _query.BuildParameters().GetOrDefault("where", EmptyJsonObject)
                }
            };
            return d;
        }

    }
}
