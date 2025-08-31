using Microsoft.Extensions.DependencyInjection;
using auto_dial;
using System;

namespace auto_dial.console.tests.Services
{
    // --- Lifetime Services ---

    public interface ISingletonService
    {
        Guid Id { get; }
    }

    [ServiceLifetime(ServiceLifetime.Singleton)]
    public class SingletonService : ISingletonService
    {
        public Guid Id { get; } = Guid.NewGuid();
        public SingletonService() => Console.WriteLine($"SingletonService created with Id: {Id}");
    }

    public interface IScopedService
    {
        Guid Id { get; }
    }

    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ScopedService : IScopedService
    {
        public Guid Id { get; } = Guid.NewGuid();
        public ScopedService() => Console.WriteLine($"ScopedService created with Id: {Id}");
    }

    public interface ITransientService
    {
        Guid Id { get; }
    }

    [ServiceLifetime(ServiceLifetime.Transient)]
    public class TransientService : ITransientService
    {
        public Guid Id { get; } = Guid.NewGuid();
        public TransientService() => Console.WriteLine($"TransientService created with Id: {Id}");
    }

    // --- Service with Dependency ---

    public interface IDependentService
    {
        void DoSomething();
    }

    public class DependentService : IDependentService
    {
        private readonly ISingletonService _singletonService;
        private readonly IScopedService _scopedService;
        private readonly ITransientService _transientService;

        public DependentService(ISingletonService singletonService, IScopedService scopedService, ITransientService transientService)
        {
            _singletonService = singletonService;
            _scopedService = scopedService;
            _transientService = transientService;
            Console.WriteLine($"DependentService created. Singleton: {_singletonService.Id}, Scoped: {_scopedService.Id}, Transient: {_transientService.Id}");
        }

        public void DoSomething()
        {
            Console.WriteLine($"DependentService doing something. Singleton: {_singletonService.Id}, Scoped: {_scopedService.Id}, Transient: {_transientService.Id}");
        }
    }

    // --- Multiple Implementations ---

    public interface INotificationService
    {
        string Send();
    }

    public class EmailNotificationService : INotificationService
    {
        public string Send() => "Email notification sent.";
    }

    public class SmsNotificationService : INotificationService
    {
        public string Send() => "SMS notification sent.";
    }

    // --- Concrete Type (no interface) ---

    public class UtilityService
    {
        public Guid Id { get; } = Guid.NewGuid();
        public UtilityService() => Console.WriteLine($"UtilityService created with Id: {Id}");
        public string GetCurrentTime() => DateTime.Now.ToLongTimeString();
    }

    // --- Excluded Service ---

    public interface IExcludedService { }

    [ExcludeFromDI]
    public class ExcludedService : IExcludedService
    {
        public ExcludedService() => Console.WriteLine("ExcludedService created (should not happen via DI)");
    }

    // --- Service in a different namespace for filtering test ---
}

namespace auto_dial.console.tests.AnotherNamespace
{
    public interface IOtherNamespaceService { }
    public class OtherNamespaceService : IOtherNamespaceService
    {
        public OtherNamespaceService() => Console.WriteLine("OtherNamespaceService created.");
    }
}
