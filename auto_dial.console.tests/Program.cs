using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using auto_dial;
using auto_dial.console.tests.Services;
using auto_dial.console.tests.AnotherNamespace;

namespace auto_dial.console.tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();

            // Configure logging
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Debug);
            });

            // Configure auto_dial to scan the current assembly
            services.AddAutoDial(options =>
            {
                options.FromAssemblyOf<Program>();
                // Include both namespaces for testing
                options.InNamespaceStartingWith("auto_dial.console.tests.Services", "auto_dial.console.tests.AnotherNamespace");
            });

            var serviceProvider = services.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting auto_dial console verification...");

            logger.LogInformation("\n--- Lifetime Verification ---");

            // Singleton Verification
            var singleton1 = serviceProvider.GetRequiredService<ISingletonService>();
            var singleton2 = serviceProvider.GetRequiredService<ISingletonService>();
            logger.LogInformation($"SingletonService 1 Id: {singleton1.Id}");
            logger.LogInformation($"SingletonService 2 Id: {singleton2.Id}");
            logger.LogInformation($"Singleton instances are {(singleton1.Id == singleton2.Id ? "the same" : "DIFFERENT")}. Expected: Same");

            // Scoped Verification
            using (var scope1 = serviceProvider.CreateScope())
            {
                var scoped1_1 = scope1.ServiceProvider.GetRequiredService<IScopedService>();
                var scoped1_2 = scope1.ServiceProvider.GetRequiredService<IScopedService>();
                logger.LogInformation($"\nScope 1 - ScopedService 1 Id: {scoped1_1.Id}");
                logger.LogInformation($"Scope 1 - ScopedService 2 Id: {scoped1_2.Id}");
                logger.LogInformation($"Scoped instances in Scope 1 are {(scoped1_1.Id == scoped1_2.Id ? "the same" : "DIFFERENT")}. Expected: Same");
            }

            using (var scope2 = serviceProvider.CreateScope())
            {
                var scoped2_1 = scope2.ServiceProvider.GetRequiredService<IScopedService>();
                logger.LogInformation($"Scope 2 - ScopedService 1 Id: {scoped2_1.Id}");
                logger.LogInformation($"Scoped instances across scopes are {(singleton1.Id != scoped2_1.Id ? "DIFFERENT" : "the same")}. Expected: Different");
            }

            // Transient Verification
            var transient1 = serviceProvider.GetRequiredService<ITransientService>();
            var transient2 = serviceProvider.GetRequiredService<ITransientService>();
            logger.LogInformation($"\nTransientService 1 Id: {transient1.Id}");
            logger.LogInformation($"TransientService 2 Id: {transient2.Id}");
            logger.LogInformation($"Transient instances are {(transient1.Id != transient2.Id ? "DIFFERENT" : "the same")}. Expected: Different");

            logger.LogInformation("\n--- Dependent Service Verification ---");
            var dependentService = serviceProvider.GetRequiredService<IDependentService>();
            dependentService.DoSomething();

            logger.LogInformation("\n--- Multiple Implementations Verification ---");
            var notificationServices = serviceProvider.GetServices<INotificationService>();
            logger.LogInformation($"Found {notificationServices.Count()} notification services. Expected: 2");
            foreach (var service in notificationServices)
            {
                logger.LogInformation($"  - {service.GetType().Name}: {service.Send()}");
            }
            logger.LogInformation($"All notification services resolved: {(notificationServices.Count() == 2 && notificationServices.Any(s => s is EmailNotificationService) && notificationServices.Any(s => s is SmsNotificationService) ? "YES" : "NO")}. Expected: YES");

            logger.LogInformation("\n--- Concrete Type Verification ---");
            var utilityService = serviceProvider.GetService<UtilityService>();
            logger.LogInformation($"UtilityService resolved: {(utilityService != null ? "YES" : "NO")}. Expected: YES");
            if (utilityService != null)
            {
                logger.LogInformation($"UtilityService Id: {utilityService.Id}, Current Time: {utilityService.GetCurrentTime()}");
            }

            logger.LogInformation("\n--- Excluded Service Verification ---");
            var excludedService = serviceProvider.GetService<IExcludedService>();
            logger.LogInformation($"ExcludedService resolved: {(excludedService == null ? "NO" : "YES")}. Expected: NO");

            logger.LogInformation("\n--- Namespace Filtering Verification ---");
            var otherNamespaceService = serviceProvider.GetService<IOtherNamespaceService>();
            logger.LogInformation($"OtherNamespaceService resolved (from included namespace): {(otherNamespaceService != null ? "YES" : "NO")}. Expected: YES");

            logger.LogInformation("\nauto_dial console verification complete.");
            Console.ReadKey(); // Keep console open
        }
    }
}