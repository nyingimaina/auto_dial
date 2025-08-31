using Microsoft.Extensions.DependencyInjection;
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
            Console.WriteLine("Starting auto_dial console verification...");

            var services = new ServiceCollection();

            // Configure auto_dial to scan the current assembly
            services.AddAutoDial(options =>
            {
                options.FromAssemblyOf<Program>();
                // Include both namespaces for testing
                options.InNamespaceStartingWith("auto_dial.console.tests.Services", "auto_dial.console.tests.AnotherNamespace");
            });

            var serviceProvider = services.BuildServiceProvider();

            Console.WriteLine("\n--- Lifetime Verification ---");

            // Singleton Verification
            var singleton1 = serviceProvider.GetRequiredService<ISingletonService>();
            var singleton2 = serviceProvider.GetRequiredService<ISingletonService>();
            Console.WriteLine($"SingletonService 1 Id: {singleton1.Id}");
            Console.WriteLine($"SingletonService 2 Id: {singleton2.Id}");
            Console.WriteLine($"Singleton instances are {(singleton1.Id == singleton2.Id ? "the same" : "DIFFERENT")}. Expected: Same");

            // Scoped Verification
            using (var scope1 = serviceProvider.CreateScope())
            {
                var scoped1_1 = scope1.ServiceProvider.GetRequiredService<IScopedService>();
                var scoped1_2 = scope1.ServiceProvider.GetRequiredService<IScopedService>();
                Console.WriteLine($"\nScope 1 - ScopedService 1 Id: {scoped1_1.Id}");
                Console.WriteLine($"Scope 1 - ScopedService 2 Id: {scoped1_2.Id}");
                Console.WriteLine($"Scoped instances in Scope 1 are {(scoped1_1.Id == scoped1_2.Id ? "the same" : "DIFFERENT")}. Expected: Same");
            }

            using (var scope2 = serviceProvider.CreateScope())
            {
                var scoped2_1 = scope2.ServiceProvider.GetRequiredService<IScopedService>();
                Console.WriteLine($"Scope 2 - ScopedService 1 Id: {scoped2_1.Id}");
                Console.WriteLine($"Scoped instances across scopes are {(singleton1.Id != scoped2_1.Id ? "DIFFERENT" : "the same")}. Expected: Different");
            }

            // Transient Verification
            var transient1 = serviceProvider.GetRequiredService<ITransientService>();
            var transient2 = serviceProvider.GetRequiredService<ITransientService>();
            Console.WriteLine($"\nTransientService 1 Id: {transient1.Id}");
            Console.WriteLine($"TransientService 2 Id: {transient2.Id}");
            Console.WriteLine($"Transient instances are {(transient1.Id != transient2.Id ? "DIFFERENT" : "the same")}. Expected: Different");

            Console.WriteLine("\n--- Dependent Service Verification ---");
            var dependentService = serviceProvider.GetRequiredService<IDependentService>();
            dependentService.DoSomething();

            Console.WriteLine("\n--- Multiple Implementations Verification ---");
            var notificationServices = serviceProvider.GetServices<INotificationService>();
            Console.WriteLine($"Found {notificationServices.Count()} notification services. Expected: 2");
            foreach (var service in notificationServices)
            {
                Console.WriteLine($"  - {service.GetType().Name}: {service.Send()}");
            }
            Console.WriteLine($"All notification services resolved: {(notificationServices.Count() == 2 && notificationServices.Any(s => s is EmailNotificationService) && notificationServices.Any(s => s is SmsNotificationService) ? "YES" : "NO")}. Expected: YES");

            Console.WriteLine("\n--- Concrete Type Verification ---");
            var utilityService = serviceProvider.GetService<UtilityService>();
            Console.WriteLine($"UtilityService resolved: {(utilityService != null ? "YES" : "NO")}. Expected: YES");
            if (utilityService != null)
            {
                Console.WriteLine($"UtilityService Id: {utilityService.Id}, Current Time: {utilityService.GetCurrentTime()}");
            }

            Console.WriteLine("\n--- Excluded Service Verification ---");
            var excludedService = serviceProvider.GetService<IExcludedService>();
            Console.WriteLine($"ExcludedService resolved: {(excludedService == null ? "NO" : "YES")}. Expected: NO");

            Console.WriteLine("\n--- Namespace Filtering Verification ---");
            var otherNamespaceService = serviceProvider.GetService<IOtherNamespaceService>();
            Console.WriteLine($"OtherNamespaceService resolved (from included namespace): {(otherNamespaceService != null ? "YES" : "NO")}. Expected: YES");

            Console.WriteLine("\nauto_dial console verification complete.");
            Console.ReadKey(); // Keep console open
        }
    }
}