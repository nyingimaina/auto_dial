using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System;
using System.Linq;
using auto_dial;

namespace auto_dial.tests.DependencyOrderTests
{
    // Define test interfaces and implementations
    public interface IServiceA { }
    public class ServiceA : IServiceA { }

    public interface IServiceB { }
    public class ServiceB : IServiceB
    {
        public ServiceB(IServiceA serviceA) { }
    }

    public interface IServiceC { }
    public class ServiceC : IServiceC
    {
        public ServiceC(IServiceB serviceB) { }
    }

    public interface IServiceD { }
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
            services.PrimeServicesForAutoRegistration()
                .FromAssemblyOf<ServiceA>() // Scan the assembly containing our test services
                .InNamespaceStartingWith("auto_dial.tests.DependencyOrderTests") // Filter to our test namespace
                .CompleteAutoRegistration();

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
    public class CircularServiceA : ICircularServiceA
    {
        public CircularServiceA(ICircularServiceB serviceB) { }
    }

    public interface ICircularServiceB { }
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
                services.PrimeServicesForAutoRegistration()
                    .FromAssemblyOf<CircularServiceA>()
                    .InNamespaceStartingWith("auto_dial.tests.CircularDependencyTests")
                    .CompleteAutoRegistration();
            });

            Assert.NotNull(exception);
            Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("Circular dependency detected", exception.Message);
        }
    }
}