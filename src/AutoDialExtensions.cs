
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
        public static IServiceCollection AddAutoDial(
            this IServiceCollection services,
            Action<AutoDialRegistrationBuilder>? configure = null,
            Action? onCompleted = null)
        {
            var builder = new AutoDialRegistrationBuilder(services);
            configure?.Invoke(builder);
            var result = builder.CompleteAutoRegistration();
            onCompleted?.Invoke();
            return result;
        }
    }
}
