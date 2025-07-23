
using Microsoft.Extensions.DependencyInjection;
using System;

namespace auto_dial
{
    public static class AutoDialExtensions
    {
        /// <summary>
        /// Prepares the IServiceCollection for auto-registration of services.
        /// </summary>
        /// <param name="services">The IServiceCollection to extend.</param>
        /// <returns>An AutoDialRegistrationBuilder instance to configure auto-registration.</returns>
        public static AutoDialRegistrationBuilder PrimeServicesForAutoRegistration(this IServiceCollection services)
        {
            return new AutoDialRegistrationBuilder(services);
        }
    }
}
