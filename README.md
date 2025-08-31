# auto_dial (Automatic Dependency Injection Abstraction Layer)

`auto_dial` is a library that makes setting up Dependency Injection (DI) in .NET applications super easy. Instead of writing a lot of repetitive code to register your services, `auto_dial` does it for you automatically. It scans your code, finds the services, and registers them with the DI container. This saves time and reduces mistakes.

## Table of Contents

- [Why Use auto_dial?](#why-use-auto_dial)
- [What is Dependency Injection (DI)?](#what-is-dependency-injection-di)
- [Getting Started](#getting-started)
  - [Step 1: Install the Library](#step-1-install-the-library)
  - [Step 2: Set Up Your Services](#step-2-set-up-your-services)
  - [Step 3: Use auto_dial to Register Services](#step-3-use-auto_dial-to-register-services)
- [How It Works](#how-it-works)
- [Configuration Options](#configuration-options)
  - [1. Register Services from a Specific Assembly](#1-register-services-from-a-specific-assembly)
  - [2. Filter Services by Namespace](#2-filter-services-by-namespace)
  - [3. Exclude Specific Interfaces or Implementations](#3-exclude-specific-interfaces-or-implementations)
  - [4. Registering Multiple Implementations of an Interface](#4-registering-multiple-implementations-of-an-interface)
  - [5. Registering Concrete Types (Without an Interface)](#5-registering-concrete-types-without-an-interface)
- [Supported Service Lifetimes](#supported-service-lifetimes)
- [Advanced Scenarios & Behavior](#advanced-scenarios--behavior)
  - [Interface vs. Concrete Type Registration](#interface-vs-concrete-type-registration)
  - [Handling Multi-Interface Services](#handling-multi-interface-services)
  - [Constructor Selection for Dependency Resolution](#constructor-selection-for-dependency-resolution)
- [Observing `auto_dial`'s Behavior](#observing-auto_dials-behavior)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

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

Here’s how you can use `auto_dial` to automatically register your decorated services. This example also includes setting up the standard .NET console logger to see the output.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using auto_dial;

class Program
{
    static void Main()
    {
        var services = new ServiceCollection();

        // 1. Configure Logging (Optional, but recommended for visibility)
        services.AddLogging(configure => 
        {
            configure.AddConsole();
            configure.SetMinimumLevel(LogLevel.Information);
        });

        // 2. Automatically register services in the same assembly
        services.AddAutoDial(options =>
        {
            options.FromAssemblyOf<MyService>(); // Scan the assembly containing MyService
            options.IfExceptionOccurs((exception) =>
            {
                // You can get a logger here to handle the exception
                var logger = services.BuildServiceProvider().GetService<ILogger<Program>>();
                logger?.LogCritical(exception, "An error occurred during service registration.");
            });
        });

        var serviceProvider = services.BuildServiceProvider();

        // 3. Resolve and use your services
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

## Advanced Scenarios & Behavior

### Interface vs. Concrete Type Registration

`auto_dial` follows a clear logic for registration:

1.  It begins by finding a **class** decorated with `[ServiceLifetime]`.
2.  It then checks if that class implements any eligible interfaces.
3.  **If a suitable interface is found**, the service is registered against the interface (e.g., `services.AddScoped<IMyService, MyService>()`). This is the default and recommended behavior.
4.  **If no suitable interface is found**, the service is registered against its own concrete type (e.g., `services.AddScoped<MyService, MyService>()`).

### Handling Multi-Interface Services

It's important to note the **"One Registration Per Class"** rule. Even if a class implements multiple valid interfaces, `auto_dial` will only ever create **one** registration for it. It picks the *first* eligible interface it discovers and ignores the rest.

If you need to control which interface is chosen, you can use the `ExcludeInterface<T>()` configuration option. This is an advanced feature for resolving ambiguity.

**Example:**

```csharp
public interface ICanShip { }
public interface ICanBill { }

[ServiceLifetime(ServiceLifetime.Scoped)]
public class OrderService : ICanShip, ICanBill { /* ... */ }

// By default, OrderService might register as ICanShip.
// To force it to register as ICanBill, you would do this:

services.AddAutoDial(options =>
{
    options.FromAssemblyOf<OrderService>();
    options.ExcludeInterface<ICanShip>(); // Ignore this one
});
```

### Constructor Selection for Dependency Resolution

When a service has multiple constructors, the `DependencyResolver` needs to know which one to analyze to find its dependencies. `auto_dial` uses a simple and common convention: **it selects the constructor with the most parameters.**

### Hybrid Registration: Mixing Manual and Automatic

It is common to mix manual service registration with `auto_dial`. The dependency validation in `auto_dial` is designed to support this. It is aware of services that have already been registered in the `IServiceCollection` before it runs.

**IMPORTANT**: For this to work, you must perform your manual registrations **before** you call the `AddAutoDial()` method. The validation check can only see what has *already* been added to the service collection.

```csharp
var services = new ServiceCollection();

// 1. Manually register a third-party service or a complex factory
services.AddSingleton<IThirdPartyService>(new ThirdPartyService());

// 2. Use auto_dial for your application services
// auto_dial will correctly see that IThirdPartyService is available for any
// of your auto-registered services that depend on it.
services.AddAutoDial(options => 
{
    options.FromAssemblyOf<MyApplicationService>();
});
```

---

## Observing `auto_dial`'s Behavior

The core `auto_dial` library does not write to the console or any other output sink. It is completely silent.

To see what the library is doing, you should use a standard logging framework like `Microsoft.Extensions.Logging`. The example console application in this repository (`auto_dial.console.tests`) is configured to use the console logger. When you run it, you will see output from the services as they are created by the DI container. This is not output from `auto_dial` itself, but rather from the example services that have had an `ILogger` injected into them.

By configuring the log level in `Program.cs`, you can control how much detail you see.

---

## Troubleshooting

`auto_dial` is designed to fail fast and provide clear error messages when it detects a problem with your dependency setup. Here are some common errors and how to resolve them:

-   **Unregistered Dependency**: This is the most common error. It occurs when a service depends on another service that `auto_dial` cannot find.
    -   **Error Message**: `auto_dial Error: Cannot resolve dependency 'IServiceB' for the constructor of class 'ServiceA'. Please ensure that the implementation for this service is decorated with the [ServiceLifetime] attribute and is included in the assembly/namespace scan, or that it has been registered manually before calling AddAutoDial().`
    -   **Solution**: This means `ServiceA` depends on `IServiceB`, but `auto_dial` couldn't find a registration for `IServiceB`. To fix this, you must either:
        1.  Find the class that implements `IServiceB` and add the `[ServiceLifetime]` attribute to it, OR
        2.  Ensure that you are manually registering `IServiceB` in the `IServiceCollection` **before** you call `AddAutoDial()`.

-   **Service Not Registered**: This can happen for a few reasons.
    1.  The service implementation class is missing the `[ServiceLifetime]` attribute.
    2.  The service is in a namespace or assembly that is not being scanned. Check your `FromAssemblyOf<T>()` and `InNamespaceStartingWith()` configurations.
    3.  The service class is decorated with the `[ExcludeFromDI]` attribute.

-   **Circular Dependency Detected**: This error occurs when two or more services depend on each other in a loop.
    -   **Error Message**: `auto_dial Error: A circular dependency was detected. The registration order cannot be determined. Dependency chain: ServiceA -> ServiceB -> ServiceA.`
    -   **Solution**: You must refactor your services to break the dependency cycle. For example, instead of `ServiceA` depending on `ServiceB` directly, you could have `ServiceA` depend on a factory or `Lazy<ServiceB>` to break the cycle at instantiation time.

---

## Contributing

We welcome contributions! Feel free to fork the repository, open issues, or submit pull requests.

---

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.