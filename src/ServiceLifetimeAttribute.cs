using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace auto_dial
{
     /// <summary>
    /// Custom attribute to specify the service lifetime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ServiceLifetimeAttribute : Attribute
    {
        public ServiceLifetime Lifetime { get; }

        public ServiceLifetimeAttribute(ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
        }
    }
}