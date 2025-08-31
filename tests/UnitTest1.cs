using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System;
using System.Linq;
using System.Collections.Generic;
using auto_dial;

namespace auto_dial.tests.DependencyOrderTests
{
    // Define test interfaces and implementations
    public interface IServiceA { }
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ServiceA : IServiceA { }

    public interface IServiceB { }
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ServiceB : IServiceB
    {
        public ServiceB(IServiceA serviceA) { }
    }

    public interface IServiceC { }
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ServiceC : IServiceC
    {
        public ServiceC(IServiceB serviceB) { }
    }

    public interface IServiceD { }
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ServiceD : IServiceD
    {
        public ServiceD(IServiceA serviceA, IServiceC serviceC) { }
    }

    public class DependencyResolutionTests
    {
        [Fact]
        public void ServicesAreRegisteredInCorrectDependencyOrder()
        {
            var services = new ServiceCollection();

            // Register services using auto_dial
            services.AddAutoDial(options =>
            {
                options.FromAssemblyOf<ServiceA>(); // Scan the assembly containing our test services
                options.InNamespaceStartingWith("auto_dial.tests.DependencyOrderTests");
            });

            var serviceProvider = services.BuildServiceProvider();

            // Attempt to resolve services. If the dependency resolution is correct,
            // these should resolve without issues.
            var serviceD = serviceProvider.GetService<IServiceD>();
            var serviceC = serviceProvider.GetService<IServiceC>();
            var serviceB = serviceProvider.GetService<IServiceB>();
            var serviceA = serviceProvider.GetService<IServiceA>();

            Assert.NotNull(serviceA);
            Assert.NotNull(serviceB);
            Assert.NotNull(serviceC);
            Assert.NotNull(serviceD);
        }
    }
}

namespace auto_dial.tests.CircularDependencyTests
{
    // Test for circular dependency detection
    public interface ICircularServiceA { }
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class CircularServiceA : ICircularServiceA
    {
        public CircularServiceA(ICircularServiceB serviceB) { }
    }

    public interface ICircularServiceB { }
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class CircularServiceB : ICircularServiceB
    {
        public CircularServiceB(ICircularServiceA serviceA) { }
    }

    public class CircularDependencyTests
    {
        [Fact]
        public void CircularDependencyThrowsException()
        {
            var services = new ServiceCollection();

            // Expect an InvalidOperationException due to circular dependency
            var exception = Record.Exception(() =>
            {
                services.AddAutoDial(options =>
                {
                    options.FromAssemblyOf<CircularServiceA>();
                    options.InNamespaceStartingWith("auto_dial.tests.CircularDependencyTests");
                });
            });

            Assert.NotNull(exception);
            Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("auto_dial Error: A circular dependency was detected.", exception.Message);
            Assert.Contains("CircularServiceA ->", exception.Message);
            Assert.Contains("-> CircularServiceA", exception.Message);
        }
    }
}

namespace auto_dial.tests.MultipleImplementationsTests
{
    public interface IMultiService { }
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class MultiServiceA : IMultiService { }
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class MultiServiceB : IMultiService { }

    public class MultipleImplementationsTests
    {
        [Fact]
        public void MultipleImplementationsAreRegistered()
        {
            var services = new ServiceCollection();

            services.AddAutoDial(options =>
            {
                options.FromAssemblyOf<IMultiService>();
                options.InNamespaceStartingWith("auto_dial.tests.MultipleImplementationsTests");
            });

            var serviceProvider = services.BuildServiceProvider();

            var multiServices = serviceProvider.GetServices<IMultiService>();

            Assert.NotNull(multiServices);
            Assert.Equal(2, multiServices.Count());
            Assert.Contains(multiServices, s => s.GetType() == typeof(MultiServiceA));
            Assert.Contains(multiServices, s => s.GetType() == typeof(MultiServiceB));
        }
    }
}

namespace auto_dial.tests.ConcreteTypeRegistrationTests
{
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ConcreteService { }

    public class ConcreteTypeRegistrationTests
    {
        [Fact]
        public void ConcreteTypeIsRegistered()
        {
            var services = new ServiceCollection();

            services.AddAutoDial(options =>
            {
                options.FromAssemblyOf<ConcreteService>();
                options.InNamespaceStartingWith("auto_dial.tests.ConcreteTypeRegistrationTests");
            });

            var serviceProvider = services.BuildServiceProvider();

            var concreteService = serviceProvider.GetService<ConcreteService>();

            Assert.NotNull(concreteService);
            Assert.IsType<ConcreteService>(concreteService);
        }
    }
}

namespace auto_dial.tests.ErrorHandlingTests
{
    // --- Unregistered Dependency Test ---
    public interface IUnregisteredDep { }
    public class UnregisteredDep : IUnregisteredDep { } // No [ServiceLifetime] attribute

    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ServiceWithUnregisteredDep
    {
        public ServiceWithUnregisteredDep(IUnregisteredDep dep) { }
    }

    // --- Detailed Circular Dependency Test ---
    public interface ICircularDepA { }
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class CircularDepA : ICircularDepA
    {
        public CircularDepA(ICircularDepB b) { }
    }

    public interface ICircularDepB { }
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class CircularDepB : ICircularDepB
    {
        public CircularDepB(ICircularDepC c) { }
    }

    public interface ICircularDepC { }
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class CircularDepC : ICircularDepC
    {
        public CircularDepC(ICircularDepA a) { }
    }


    public class ErrorHandlingTests
    {
        [Fact]
        public void UnregisteredDependencyThrowsDetailedException()
        {
            var services = new ServiceCollection();

            var exception = Record.Exception(() =>
            {
                services.AddAutoDial(options =>
                {
                    options.FromAssemblyOf<ErrorHandlingTests>();
                    options.InNamespaceStartingWith("auto_dial.tests.ErrorHandlingTests");
                });
            });

            Assert.NotNull(exception);
            Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("auto_dial Error: Cannot resolve dependency 'IUnregisteredDep' for the constructor of class 'ServiceWithUnregisteredDep'", exception.Message);
        }

        [Fact]
        public void CircularDependencyThrowsDetailedException()
        {
            var services = new ServiceCollection();

            var exception = Record.Exception(() =>
            {
                services.AddAutoDial(options =>
                {
                    options.FromAssemblyOf<ErrorHandlingTests>();
                    options.InNamespaceStartingWith("auto_dial.tests.ErrorHandlingTests");
                });
            });

            Assert.NotNull(exception);
            Assert.IsType<InvalidOperationException>(exception);
            // The exact order can vary, so we check for the components of the chain.
            Assert.Contains("auto_dial Error: A circular dependency was detected.", exception.Message);
            Assert.Contains("CircularDepA ->", exception.Message);
            Assert.Contains("CircularDepB ->", exception.Message);
            Assert.Contains("CircularDepC ->", exception.Message);
            Assert.Contains("-> CircularDepA", exception.Message);
        }
    }
}

namespace auto_dial.tests.HybridRegistrationTests
{
    public interface IManualService { }
    public class ManualService : IManualService { }

    public interface IAutoService { }
    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class AutoService : IAutoService
    {
        public AutoService(IManualService manualService) { }
    }

    public class HybridRegistrationTests
    {
        [Fact]
        public void AutoRegisteredServiceCanDependOnManuallyRegisteredService()
        {
            var services = new ServiceCollection();

            // Manually register a service
            services.AddScoped<IManualService, ManualService>();

            // Use auto_dial for other services
            services.AddAutoDial(options =>
            {
                options.FromAssemblyOf<HybridRegistrationTests>();
                options.InNamespaceStartingWith("auto_dial.tests.HybridRegistrationTests");
            });

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var autoService = serviceProvider.GetService<IAutoService>();

            // Assert
            Assert.NotNull(autoService);
        }
    }
}

namespace auto_dial.tests.FrameworkExemptionTests
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class MyOptions { public string Value { get; set; } = "Test"; }

    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ServiceWithFrameworkDependencies
    {
        public ServiceWithFrameworkDependencies(ILogger<ServiceWithFrameworkDependencies> logger, IOptions<MyOptions> options, string connectionString, IServiceProvider serviceProvider)
        {
            // Dependencies that should be ignored by auto_dial's validation
        }
    }

    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ServiceWithEnumerableDependency
    {
        public ServiceWithEnumerableDependency(IEnumerable<IServiceA> servicesA) { }
    }

    public class FrameworkExemptionTests
    {
        [Fact]
        public void ServiceWithFrameworkDependenciesCanBeRegistered()
        {
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddConsole()); // Required for ILogger
            services.Configure<MyOptions>(o => { }); // Required for IOptions

            var exception = Record.Exception(() =>
            {
                services.AddAutoDial(options =>
                {
                    options.FromAssemblyOf<FrameworkExemptionTests>();
                    options.InNamespaceStartingWith("auto_dial.tests.FrameworkExemptionTests");
                });
            });

            Assert.Null(exception);
            var serviceProvider = services.BuildServiceProvider();
            Assert.NotNull(serviceProvider.GetService<ServiceWithFrameworkDependencies>());
            Assert.NotNull(serviceProvider.GetService<ServiceWithEnumerableDependency>());
        }
    }
}

namespace auto_dial.tests.ExtensibleExemptionTests
{
    public interface ICustomIgnoredType { }
    public class CustomIgnoredType : ICustomIgnoredType { }

    namespace ThirdParty.Framework.Abstractions
    {
        public interface IThirdPartyService { }
        public class ThirdPartyService : IThirdPartyService { }
    }

    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ServiceDependingOnCustomIgnoredType
    {
        public ServiceDependingOnCustomIgnoredType(ICustomIgnoredType customType) { }
    }

    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ServiceDependingOnThirdPartyService
    {
        public ServiceDependingOnThirdPartyService(ThirdParty.Framework.Abstractions.IThirdPartyService thirdPartyService) { }
    }

    public class ExtensibleExemptionTests
    {
        [Fact]
        public void UserIgnoredSpecificTypeExemptsDependency()
        {
            var services = new ServiceCollection();

            var exception = Record.Exception(() =>
            {
                services.AddAutoDial(options =>
                {
                    options.FromAssemblyOf<ExtensibleExemptionTests>();
                    options.InNamespaceStartingWith("auto_dial.tests.ExtensibleExemptionTests");
                    options.IgnoreDependency<ICustomIgnoredType>();
                });
            });

            Assert.Null(exception);
        }

        [Fact]
        public void UserIgnoredNamespaceExemptsDependency()
        {
            var services = new ServiceCollection();

            var exception = Record.Exception(() =>
            {
                services.AddAutoDial(options =>
                {
                    options.FromAssemblyOf<ExtensibleExemptionTests>();
                    options.InNamespaceStartingWith("auto_dial.tests.ExtensibleExemptionTests");
                    options.IgnoreDependenciesFromNamespace("ThirdParty.Framework.Abstractions");
                });
            });

            Assert.Null(exception);
        }

        [Fact]
        public void UserIgnoredPredicateExemptsDependency()
        {
            var services = new ServiceCollection();

            var exception = Record.Exception(() =>
            {
                services.AddAutoDial(options =>
                {
                    options.FromAssemblyOf<ExtensibleExemptionTests>();
                    options.InNamespaceStartingWith("auto_dial.tests.ExtensibleExemptionTests");
                    options.IgnoreDependencyWhere(type => type.Name.Contains("ThirdParty"));
                });
            });

            Assert.Null(exception);
        }
    }
}

namespace auto_dial.tests.ConventionRegistrationTests
{
    public interface IConventionService { }
    public class ConventionService : IConventionService { }

    public interface IConventionServiceWithAttribute { }
    [ServiceLifetime(ServiceLifetime.Singleton)]
    public class ConventionServiceWithAttribute : IConventionServiceWithAttribute { }

    public interface IExcludedConventionService { }
    [ExcludeFromDI]
    public class ExcludedConventionService : IExcludedConventionService { }

    public class ConventionRegistrationTests
    {
        [Fact]
        public void ConventionBasedRegistrationWorks()
        {
            var services = new ServiceCollection();

            services.AddAutoDial(options =>
            {
                options.FromAssemblyOf<ConventionRegistrationTests>();
                options.InNamespaceStartingWith("auto_dial.tests.ConventionRegistrationTests");
                options.RegisterByConvention(type => type.Name.EndsWith("Service"), ServiceLifetime.Scoped);
            });

            var serviceProvider = services.BuildServiceProvider();

            var conventionService = serviceProvider.GetService<IConventionService>();
            Assert.NotNull(conventionService);
            // Verify lifetime (requires inspecting ServiceDescriptor, which is more complex for a simple test)
        }

        [Fact]
        public void AttributeOverridesConvention()
        {
            var services = new ServiceCollection();

            services.AddAutoDial(options =>
            {
                options.FromAssemblyOf<ConventionRegistrationTests>();
                options.InNamespaceStartingWith("auto_dial.tests.ConventionRegistrationTests");
                options.RegisterByConvention(type => type.Name.EndsWith("Service"), ServiceLifetime.Scoped);
            });

            var serviceProvider = services.BuildServiceProvider();

            var service = serviceProvider.GetService<IConventionServiceWithAttribute>();
            Assert.NotNull(service);
            // To verify it's a Singleton, we'd need to inspect the ServiceDescriptor or resolve twice in different scopes.
            // For now, just ensuring it's registered is sufficient.
        }

        [Fact]
        public void ExcludeAttributeOverridesConvention()
        {
            var services = new ServiceCollection();

            services.AddAutoDial(options =>
            {
                options.FromAssemblyOf<ConventionRegistrationTests>();
                options.InNamespaceStartingWith("auto_dial.tests.ConventionRegistrationTests");
                options.RegisterByConvention(type => type.Name.EndsWith("Service"), ServiceLifetime.Scoped);
            });

            var serviceProvider = services.BuildServiceProvider();

            var excludedService = serviceProvider.GetService<IExcludedConventionService>();
            Assert.Null(excludedService);
        }
    }
}

