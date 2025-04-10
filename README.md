# auto_dial

`auto_dial` is a library that makes setting up Dependency Injection (DI) in .NET applications super easy. Instead of writing a lot of repetitive code to register your services, `auto_dial` does it for you automatically. It scans your code, finds the services, and registers them with the DI container. This saves time and reduces mistakes.

## Why Use auto_dial?

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

Let’s say you have a service like this:

```csharp
public interface IMyService
{
    void DoWork();
}

public class MyService : IMyService
{
    public void DoWork()
    {
        Console.WriteLine("MyService is working!");
    }
}
```

And you want to use this service in another class:

```csharp
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

Normally, you would have to manually register `IMyService` and `MyService` in the DI container. But with `auto_dial`, this is done automatically!

---

### Step 3: Use auto_dial to Register Services

Here’s how you can use `auto_dial` to automatically register your services:

```csharp
using Microsoft.Extensions.DependencyInjection;
using auto_dial;

class Program
{
    static void Main()
    {
        var services = new ServiceCollection();

        // Automatically register services in the same assembly
        services.PrimeServicesForAutoRegistration()
            .FromAssemblyOf<MyService>() // Scan the assembly containing MyService
            .CompleteAutoRegistration(); // Automatically register services

        var serviceProvider = services.BuildServiceProvider();

        // Resolve and use ConsumerClass
        var consumer = serviceProvider.GetRequiredService<ConsumerClass>();
        consumer.Execute();
    }
}
```

---

## How It Works

1. **`PrimeServicesForAutoRegistration()`**: Prepares the DI container for auto-registration.
2. **`FromAssemblyOf<T>()`**: Tells `auto_dial` to scan the assembly containing the specified type (`MyService` in this case).
3. **`CompleteAutoRegistration()`**: Automatically registers all services found in the assembly.

---

## Configuration Options

`auto_dial` gives you several options to customize how services are registered:

### 1. Register Services from a Specific Assembly

Use `FromAssemblyOf<T>()` to scan a specific assembly for services.

```csharp
services.PrimeServicesForAutoRegistration()
    .FromAssemblyOf<MyService>()
    .CompleteAutoRegistration();
```

### 2. Filter Services by Namespace

If you only want to register services from a specific namespace, use `InNamespaceStartingWith()`:

```csharp
services.PrimeServicesForAutoRegistration()
    .FromAssemblyOf<MyService>()
    .InNamespaceStartingWith("MyApp.Services")
    .CompleteAutoRegistration();
```

### 3. Exclude Specific Interfaces

You can exclude certain interfaces from being registered:

```csharp
services.PrimeServicesForAutoRegistration()
    .FromAssemblyOf<MyService>()
    .ExcludeInterface<IMyService>() // Exclude IMyService
    .CompleteAutoRegistration();
```

Or exclude multiple interfaces:

```csharp
services.PrimeServicesForAutoRegistration()
    .FromAssemblyOf<MyService>()
    .ExcludeInterfaces(typeof(IMyService), typeof(IOtherService))
    .CompleteAutoRegistration();
```

---

## Supported Service Lifetimes

When registering services, you can specify how they should be instantiated:

- **Singleton**: One instance for the entire application.
- **Scoped**: One instance per request (useful for web apps).
- **Transient**: A new instance every time the service is requested.

To specify a lifetime, use the `[ServiceLifetime]` attribute on your class:

```csharp
using Microsoft.Extensions.DependencyInjection;

[ServiceLifetime(ServiceLifetime.Singleton)]
public class MySingletonService : IMyService
{
    public void DoWork()
    {
        Console.WriteLine("Singleton service is working!");
    }
}
```

---

## Example: Full Setup

Here’s a complete example:

```csharp
using Microsoft.Extensions.DependencyInjection;
using auto_dial;

public interface IMyService
{
    void DoWork();
}

[ServiceLifetime(ServiceLifetime.Singleton)]
public class MyService : IMyService
{
    public void DoWork()
    {
        Console.WriteLine("MyService is working!");
    }
}

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

class Program
{
    static void Main()
    {
        var services = new ServiceCollection();

        // Automatically register services
        services.PrimeServicesForAutoRegistration()
            .FromAssemblyOf<MyService>()
            .CompleteAutoRegistration();

        var serviceProvider = services.BuildServiceProvider();

        // Use the service
        var consumer = serviceProvider.GetRequiredService<ConsumerClass>();
        consumer.Execute();
    }
}
```

---

## Troubleshooting

- **Service Not Registered**: Make sure the service is in the correct namespace or assembly being scanned.
- **Exclude Attribute**: If a service is not being registered, check if it has the `[ExcludeFromDI]` attribute.

---

## Contributing

We welcome contributions! Feel free to fork the repository, open issues, or submit pull requests.

---

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.
