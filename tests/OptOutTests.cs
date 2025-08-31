using Microsoft.Extensions.DependencyInjection;
using Xunit;
using auto_dial;

namespace auto_dial.tests.OptOutTests
{
    public class OptOutTests
    {
        public interface IUnregisteredService { }
        public class UnregisteredService : IUnregisteredService { }

        [Fact]
        public void ClassWithoutAttributeIsNotRegistered()
        {
            var services = new ServiceCollection();

            services.AddAutoDial(options =>
            {
                options.FromAssemblyOf<OptOutTests>();
                options.InNamespaceStartingWith("auto_dial.tests.OptOutTests");
            });

            var serviceProvider = services.BuildServiceProvider();

            var unregisteredService = serviceProvider.GetService<IUnregisteredService>();

            Assert.Null(unregisteredService);
        }
    }
}
