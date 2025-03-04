using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tests
{
    public class AutoDialRegistrationBuilderTests
    {
        [Fact]
        public void CompleteAutoRegistration_RegistersServicesCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services.PrimeServicesForAutoRegistration()
                .FromAssemblyOf<TestService>()
                .InNamespaceStartingWith("tests");

            // Act
            builder.CompleteAutoRegistration();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var testService = serviceProvider.GetService<ITestService>();
            Assert.NotNull(testService);
            Assert.IsType<TestService>(testService);
        }

        [Fact]
        public void CompleteAutoRegistration_RegistersSingletonServicesCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services.PrimeServicesForAutoRegistration()
                .FromAssemblyOf<SingletonTestService>()
                .InNamespaceStartingWith("tests");

            // Act
            builder.CompleteAutoRegistration();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var testService1 = serviceProvider.GetService<ISingletonTestService>();
            var testService2 = serviceProvider.GetService<ISingletonTestService>();
            Assert.NotNull(testService1);
            Assert.NotNull(testService2);
            Assert.Same(testService1, testService2);
        }

        [Fact]
        public void CompleteAutoRegistration_RegistersTransientServicesCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services.PrimeServicesForAutoRegistration()
                .FromAssemblyOf<TransientTestService>()
                .InNamespaceStartingWith("tests");

            // Act
            builder.CompleteAutoRegistration();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var testService1 = serviceProvider.GetService<ITransientTestService>();
            var testService2 = serviceProvider.GetService<ITransientTestService>();
            Assert.NotNull(testService1);
            Assert.NotNull(testService2);
            Assert.NotSame(testService1, testService2);
        }

        [Fact]
        public void CompleteAutoRegistration_ExcludesServicesCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder       
        
    }
}