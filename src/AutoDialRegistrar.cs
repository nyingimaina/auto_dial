using Microsoft.Extensions.DependencyInjection;

namespace auto_dial
{
    public static class AutoDialRegistrar
    {
        /// <summary>
        /// Fluent API for registering services with DI.
        /// </summary>
        public static AutoDialRegistrationBuilder PrimeServicesForAutoRegistration(
            this IServiceCollection services,
            Action<AutoDialRegistrationBuilder>? action = null)
        {
            var builder = new AutoDialRegistrationBuilder(services);
            if (action != null)
            {
                action(builder);
            }
            return builder;
        }
    }
}