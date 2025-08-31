using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        public SingletonService(ILogger<SingletonService> logger)
        {
            logger.LogInformation($"SingletonService created with Id: {Id}");
        }
    }

    public interface IScopedService
    {
        Guid Id { get; }
    }

    [ServiceLifetime(ServiceLifetime.Scoped)]
    public class ScopedService : IScopedService
    {
        public Guid Id { get; } = Guid.NewGuid();
        public ScopedService(ILogger<ScopedService> logger)
        {
            logger.LogInformation($"ScopedService created with Id: {Id}");
        }
    }

    public interface ITransientService
    {
        Guid Id { get; }
    }

    [ServiceLifetime(ServiceLifetime.Transient)]
    public class TransientService : ITransientService
    {
        public Guid Id { get; } = Guid.NewGuid();
        public TransientService(ILogger<TransientService> logger)
        {
            logger.LogInformation($"TransientService created with Id: {Id}");
        }
    }

    // --- Service with Dependency ---

    public interface IDependentService
    {
        void DoSomething();
    }

    [ServiceLifetime(ServiceLifetime.Transient)]
    public class DependentService : IDependentService
    {
        private readonly ISingletonService _singletonService;
        private readonly IScopedService _scopedService;
        private readonly ITransientService _transientService;
        private readonly ILogger<DependentService> _logger;

        public DependentService(ISingletonService singletonService, IScopedService scopedService, ITransientService transientService, ILogger<DependentService> logger)
        {
            _singletonService = singletonService;
            _scopedService = scopedService;
            _transientService = transientService;
            _logger = logger;
            _logger.LogInformation($"DependentService created. Singleton: {_singletonService.Id}, Scoped: {_scopedService.Id}, Transient: {_transientService.Id}");
        }

        public void DoSomething()
        {
            _logger.LogInformation($"DependentService doing something. Singleton: {_singletonService.Id}, Scoped: {_scopedService.Id}, Transient: {_transientService.Id}");
        }
    }

    // --- Multiple Implementations ---

    public interface INotificationService
    {
        string Send();
    }

    [ServiceLifetime(ServiceLifetime.Transient)]
    public class EmailNotificationService : INotificationService
    {
        public string Send() => "Email notification sent.";
    }

    [ServiceLifetime(ServiceLifetime.Transient)]
    public class SmsNotificationService : INotificationService
    {
        public string Send() => "SMS notification sent.";
    }

    // --- Concrete Type (no interface) ---

    [ServiceLifetime(ServiceLifetime.Singleton)]
    public class UtilityService
    {
        public Guid Id { get; } = Guid.NewGuid();
        public UtilityService(ILogger<UtilityService> logger)
        {
            logger.LogInformation($"UtilityService created with Id: {Id}");
        }
        public string GetCurrentTime() => DateTime.Now.ToLongTimeString();
    }

    // --- Excluded Service ---

    public interface IExcludedService { }

    [ExcludeFromDI]
    [ServiceLifetime(ServiceLifetime.Scoped)] // Add lifetime to make it a candidate for exclusion
    public class ExcludedService : IExcludedService
    {
        public ExcludedService()
        {
            // This should not be logged by DI
        }
    }

    // --- Service in a different namespace for filtering test ---
}

namespace auto_dial.console.tests.AnotherNamespace
{
    public interface IOtherNamespaceService { }

    [ServiceLifetime(ServiceLifetime.Transient)]
    public class OtherNamespaceService : IOtherNamespaceService
    {
        public OtherNamespaceService(ILogger<OtherNamespaceService> logger)
        {
            logger.LogInformation("OtherNamespaceService created.");
        }
    }
}
