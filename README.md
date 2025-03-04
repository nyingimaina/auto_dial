# auto_dial

`auto_dial` is a library designed to simplify the Dependency Injection (DI) setup in .NET applications. It reduces the boilerplate code required to register services by automatically scanning assemblies and namespaces to register them based on their interfaces. This library is useful for developers who want to streamline their DI configuration process, ensuring that all services are registered correctly with minimal effort.

## Why It's Valuable

- **Reduced Boilerplate**: Automatically registers services based on namespaces and interfaces, reducing the manual setup and potential for human error.
- **Flexible Configuration**: Supports assembly and namespace-based filtering, allowing you to register only relevant services.
- **Customizable Exclusion**: Easily exclude specific services from DI registration using attributes, ensuring more control over the process.
- **Multiple Service Lifetimes**: Supports different lifetimes (Singleton, Scoped, Transient), making it easy to configure how services are instantiated.

## Installation

To use `auto_dial` in your project, add the NuGet package:

```bash
dotnet add package auto_dial
```

## Usage

1. **Prime the DI container** using the `PrimeServicesForAutoRegistration` extension.
2. **Use Fluent API** to configure the registration.
3. **Complete the auto-registration** process with `CompleteAutoRegistration()`.

### Example

Here’s an example of how to use `auto_dial` for automatic DI registration:

```csharp
using Microsoft.Extensions.DependencyInjection;
using auto_dial;
using System;

public interface IMyService
{
    void DoWork();
}

public class MyService : IMyService
{
    public void DoWork()
    {
        Console.WriteLine("Service is working!");
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

        // Automatically register services in the same assembly
        services.PrimeServicesForAutoRegistration()
            .FromAssemblyOf<MyService>()
            .CompleteAutoRegistration();

        var serviceProvider = services.BuildServiceProvider();

        // Resolve and use ConsumerClass
        var consumer = serviceProvider.GetRequiredService<ConsumerClass>();
        consumer.Execute();
    }
}
```

### How It Works

- **`PrimeServicesForAutoRegistration()`**: Prepares the DI container for auto-registration.
- **`FromAssemblyOf<T>()`**: Filters services from the specified assembly.
- **`CompleteAutoRegistration()`**: Registers all services automatically based on your configuration.

## Configuration Options

- **`FromAssemblyOf<T>()`**: Register services from a specific assembly.
- **`InNamespaceStartingWith(string namespacePrefix)`**: Filter services by a namespace prefix.
- **`ExcludeInterface<T>()`**: Exclude specific interfaces from being registered.
- **`ExcludeInterfaces(params Type[] types)`**: Exclude multiple interfaces at once.

## Supported Service Lifetimes

- **Singleton**: One instance of the service throughout the application's lifetime.
- **Scoped**: One instance per scope, typically per web request.
- **Transient**: A new instance every time the service is requested.

## Contributing

Feel free to fork, open issues, or submit pull requests! If you find bugs or have feature requests, don't hesitate to let us know.

## License

MIT License
