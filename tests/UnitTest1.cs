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
            Assert.Contains("Cannot resolve registration order.", exception.Message);
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

