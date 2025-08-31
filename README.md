# auto_dial (Automatic Dependency Injection Abstraction Layer)

`auto_dial` is a library that makes setting up Dependency Injection (DI) in .NET applications super easy. Instead of writing a lot of repetitive code to register your services, `auto_dial` does it for you automatically. It scans your code, finds the services, and registers them with the DI container. This saves time and reduces mistakes.

## Why Use auto_dial?

- **Safe & Explicit**: `auto_dial` uses an opt-in model. Only services you explicitly mark will be registered, preventing accidental registration of non-service classes.
- **Less Repetitive Code**: You don't have to manually register every service in your application.
- **Flexible**: You can control which services are registered by filtering based on namespaces or assemblies.
- **Customizable**: You can exclude specific services from being registered if needed.
- **Supports Different Lifetimes**: Easily configure services as Singleton, Scoped, or Transient.

## What is Dependency Injection (DI)?

Dependency Injection is a way to manage the dependencies (like services or classes) that your application needs. Instead of creating these dependencies manually, DI allows you to "inject" them into your classes. This makes your code cleaner, easier to test, and more maintainable.

For example:

```csharp
public class MyClass
{
    private readonly IMyService _myService;

    public MyClass(IMyService myService)
    {
        _myService = myService; // The service is injected here
    }

    public void DoSomething()
    {
        _myService.DoWork();
    }
}
```

With DI, you don't have to worry about creating `IMyService`. The DI container does it for you.

---

## Getting Started

### Step 1: Install the Library

First, add the `auto_dial` library to your project using NuGet. Open your terminal and run:

```bash
dotnet add package auto_dial
```

This will download and add the library to your project.

---

### Step 2: Set Up Your Services

To make a class eligible for auto-registration, decorate it with the `[ServiceLifetime]` attribute. This tells `auto_dial` that the class is a service and specifies its lifetime.

Let’s say you have a service like this:

```csharp
using Microsoft.Extensions.DependencyInjection;
using auto_dial; // Add this using directive

public interface IMyService
{
    void DoWork();
}

[ServiceLifetime(ServiceLifetime.Scoped)] // Opt-in for registration
public class MyService : IMyService
{
    public void DoWork()
    {
        Console.WriteLine("MyService is working!");
    }
}
```

And you want to use this service in another class (which also needs to be registered):

```csharp
[ServiceLifetime(ServiceLifetime.Transient)] // This class also needs to be registered to be resolved
public class ConsumerClass
{
    private readonly IMyService _myService;

    public ConsumerClass(IMyService myService)
    {
        _myService = myService;
    }

    public void Execute()
    {
        _myService.DoWork();
    }
}
```

---

### Step 3: Use auto_dial to Register Services

Here’s how you can use `auto_dial` to automatically register your decorated services:

```csharp
using Microsoft.Extensions.DependencyInjection;
using auto_dial;

class Program
{
    static void Main()
    {
        var services = new ServiceCollection();

        // Automatically register services in the same assembly
        services.AddAutoDial(options =>
        {
            options.IfExceptionOccurs((exception) =>
            {
                // Handle the exception (log it, rethrow, etc.)
                Console.WriteLine($"An error occurred during service registration: {exception.Message}");
            });
            options.FromAssemblyOf<MyService>(); // Scan the assembly containing MyService
        });

        var serviceProvider = services.BuildServiceProvider();

        // Resolve and use ConsumerClass
        var consumer = serviceProvider.GetRequiredService<ConsumerClass>();
        consumer.Execute();
    }
}
```

---

## How It Works

`auto_dial` simplifies DI setup by automating service registration. Here's a deeper look into its mechanisms:

1.  **`AddAutoDial()`**: This is the primary extension method on `IServiceCollection` to initiate auto-registration.
2.  **`configure` action (optional)**: An `Action<AutoDialRegistrationBuilder>` that allows you to customize the registration process using the fluent API (e.g., `FromAssemblyOf`, `InNamespaceStartingWith`, `ExcludeInterface`).
3.  **Service Discovery (Opt-In Model)**: `auto_dial` uses reflection to scan the specified assembly for classes decorated with the `[ServiceLifetime]` attribute. This attribute serves as the explicit opt-in signal for registration.
4.  **Default Behavior**: If no `configure` action is provided, `auto_dial` will scan the assembly where it is called. **Only classes with the `[ServiceLifetime]` attribute will be registered.** There is no default registration for undecorated classes.
5.  **Interface Matching**: For each registered class, `auto_dial` attempts to find a corresponding interface to register it against. The interface must be in an eligible namespace and not be explicitly excluded.
6.  **Concrete Type Registration**: If a class is decorated with `[ServiceLifetime]` but does not have a suitable interface, it will be registered as a concrete type (e.g., `services.AddScoped<MyConcreteClass>()`).
7.  **Dependency Resolution (Topological Sort)**: Before registering services, `auto_dial` builds a dependency graph of all discovered services. It then performs a [topological sort (Kahn's algorithm)](https://en.wikipedia.org/wiki/Topological_sorting#Kahn's_algorithm) to determine the correct order of registration, ensuring that dependencies are registered before the services that consume them.
8.  **Circular Dependency Detection**: If the topological sort detects a circular dependency, `auto_dial` will throw an `InvalidOperationException`.
9.  **`CompleteAutoRegistration()`**: This method is called internally by `AddAutoDial()`, so you no longer need to call it explicitly.

---

## Configuration Options

`auto_dial` gives you several options to customize how services are registered:

### 1. Register Services from a Specific Assembly

Use `AddAutoDial()` with `FromAssemblyOf<T>()` to scan a specific assembly for services.

```csharp
services.AddAutoDial(options =>
{
    options.FromAssemblyOf<MyService>(); // Scans the assembly containing MyService
});
```

### 2. Filter Services by Namespace

If you only want to register services from specific namespaces, use `InNamespaceStartingWith()`:

```csharp
services.AddAutoDial(options =>
{
    options.FromAssemblyOf<MyService>(); // Scans the assembly containing MyService
    options.InNamespaceStartingWith("MyApp.Services", "MyApp.Common"); // Filters to these namespaces
});
```

### 3. Exclude Specific Interfaces or Implementations

You can exclude certain interfaces or implementations from being registered:

```csharp
services.AddAutoDial(options =>
{
    options.FromAssemblyOf<MyService>();
    options.ExcludeInterface<IMyService>(); // Exclude IMyService from registration
});
```

Or exclude multiple interfaces:

```csharp
services.AddAutoDial(options =>
{
    options.FromAssemblyOf<MyService>();
    options.ExcludeInterfaces(typeof(IMyService), typeof(IOtherService));
});
```

You can also exclude an implementation using the `[ExcludeFromDI]` attribute on the class:

```csharp
using auto_dial;

[ExcludeFromDI]
public class ExcludedService : IMyService
{
    public void DoWork() { /* ... */ }
}
```

### 4. Registering Multiple Implementations of an Interface

`auto_dial` supports registering multiple concrete implementations for the same interface. The underlying `Microsoft.Extensions.DependencyInjection` container will then allow you to resolve all of them as an `IEnumerable<TService>`. Just decorate each implementation with its own `[ServiceLifetime]` attribute.

```csharp
using Microsoft.Extensions.DependencyInjection;
using auto_dial;
using System.Collections.Generic;
using System.Linq;

// Define an interface and multiple implementations
public interface INotificationService
{
    string SendNotification();
}

[ServiceLifetime(ServiceLifetime.Transient)]
public class EmailNotificationService : INotificationService
{
    public string SendNotification() => "Email sent!";
}

[ServiceLifetime(ServiceLifetime.Transient)]
public class SmsNotificationService : INotificationService
{
    public string SendNotification() => "SMS sent!";
}

class Program
{
    static void Main()
    {
        var services = new ServiceCollection();

        services.AddAutoDial(options =>
        {
            options.FromAssemblyOf<EmailNotificationService>();
            options.InNamespaceStartingWith("YourApp.Notifications"); // Assuming these services are in this namespace
        });

        var serviceProvider = services.BuildServiceProvider();

        // Resolve all implementations of INotificationService
        IEnumerable<INotificationService> notificationServices = serviceProvider.GetServices<INotificationService>();

        Console.WriteLine($"Found {notificationServices.Count()} notification services:");
        foreach (var service in notificationServices)
        {
            Console.WriteLine($"- {service.GetType().Name}: {service.SendNotification()}");
        }

        // Output:
        // Found 2 notification services:
        // - EmailNotificationService: Email sent!
        // - SmsNotificationService: SMS sent!
    }
}
```

### 5. Registering Concrete Types (Without an Interface)

Sometimes you might have a class that doesn't implement an interface but still needs to be registered in the DI container. `auto_dial` can register these directly as long as they are decorated with `[ServiceLifetime]`.

```csharp
using Microsoft.Extensions.DependencyInjection;
using auto_dial;
using System;

[ServiceLifetime(ServiceLifetime.Singleton)]
public class UtilityService
{
    public string GetCurrentTime() => DateTime.Now.ToShortTimeString();
}

class Program
{
    static void Main()
    {
        var services = new ServiceCollection();

        services.AddAutoDial(options =>
        {
            options.FromAssemblyOf<UtilityService>();
            options.InNamespaceStartingWith("YourApp.Utilities"); // Assuming UtilityService is in this namespace
        });

        var serviceProvider = services.BuildServiceProvider();

        // Resolve the concrete UtilityService directly
        UtilityService utilityService = serviceProvider.GetRequiredService<UtilityService>();

        Console.WriteLine($"Current time: {utilityService.GetCurrentTime()}");
    }
}
```

---

## Supported Service Lifetimes

`auto_dial` has an **opt-in** registration model. To register a service, you must decorate the implementation class with the `[ServiceLifetime]` attribute. This attribute tells `auto_dial` to register the service and specifies its lifetime.

-   **Singleton**: One instance for the entire application.
-   **Scoped**: One instance per request (e.g., per HTTP request in a web app).
-   **Transient**: A new instance every time the service is requested.

If a class is not decorated with `[ServiceLifetime]`, it will be ignored.

```csharp
using Microsoft.Extensions.DependencyInjection;
using auto_dial; // Ensure this using directive is present

[ServiceLifetime(ServiceLifetime.Singleton)] // This service will be registered as a Singleton
public class MySingletonService : IMyService
{
    public void DoWork()
    {
        Console.WriteLine("Singleton service is working!");
    }
}

public class NotAService // This class will be ignored by auto_dial
{
    // ...
}
```

---

## Troubleshooting

-   **Service Not Registered**: 
    1.  Ensure the service implementation class is decorated with the `[ServiceLifetime]` attribute. This is the most common reason for a service not being registered.
    2.  Make sure the service is in the correct namespace or assembly being scanned.
    3.  Check that it does not have the `[ExcludeFromDI]` attribute.
-   **Circular Dependency Detected**: If you encounter an `InvalidOperationException` with a "Circular dependency detected" message, it means your services have a dependency loop. You'll need to refactor your service dependencies to break the cycle.

---

## Contributing

We welcome contributions! Feel free to fork the repository, open issues, or submit pull requests.

---

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.