using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base
{
    public class SubscriptionInfo(Type handlerType)
    {
        public Type HandlerType { get;} = handlerType ?? throw new ArgumentNullException(nameof(handlerType));

        public static SubscriptionInfo Typed(Type handlerType)
        {
            return new SubscriptionInfo(handlerType);
        }
    }
}
