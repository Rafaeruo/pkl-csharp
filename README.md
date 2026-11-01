# Pkl C#

> Pkl — pronounced Pickle — is an embeddable configuration language which provides rich support for data templating and validation.

These packages are C# bindings to [Pkl](https://pkl-lang.org), to provide a native way to evaluate `.pkl` files from .NET.

| Package | Purpose |
|---|---|
| [`PklCsharp`](https://www.nuget.org/packages/PklCsharp) | Evaluate Pkl modules into C# objects. |
| [`PklCsharp.Generator`](https://www.nuget.org/packages/PklCsharp.Generator) | Source generator that writes the C# classes for you from your `.pkl` schema files. |
| [`PklCSharp.Microsoft.Extensions.Configuration`](https://www.nuget.org/packages/PklCSharp.Microsoft.Extensions.Configuration) | Use a Pkl module as a `Microsoft.Extensions.Configuration` source (for example in ASP.NET Core). |

Working examples are in the [`Examples`](https://github.com/Rafaeruo/pkl-csharp/tree/master/Examples) folder.

## Requirements

- **.NET 8 or newer** (the packages target `net8.0`, `net9.0` and `net10.0`).
- **The `pkl` executable, version 0.26.0 or newer.** The library starts `pkl server` as a child process, so Pkl must be installed on any machine that evaluates Pkl files, see the [Pkl installation guide](https://pkl-lang.org/main/current/pkl-cli/index.html#installation).
  - By default `pkl` is looked up on the `PATH`. Set the `PKL_EXEC` environment variable (for example `PKL_EXEC=/opt/pkl/pkl`), or pass the command to `new EvaluatorManager(["/opt/pkl/pkl"])`, to use a specific executable.
  - The version is checked the first time an evaluator is created, and an unsupported version throws a `NotSupportedException`. Set `PKL_CSHARP_SKIP_VERSION_CHECK=1` to skip the check.
  - CI tests the oldest supported release, the latest patch of every minor release since, and the newest release, so newer Pkl versions are expected to work. Please open an issue if one does not.

## Evaluating Pkl

```sh
dotnet add package PklCsharp
```

Given a Pkl module and a class that matches its properties:

```pkl
// Config/AppConfig.pkl
host: String = "127.0.0.1"
port: UInt16 = 5000
```

```csharp
public class AppConfig
{
    public string Host { get; set; } = default!;
    public ushort Port { get; set; }
}
```

evaluate it like this:

```csharp
using Pkl;
using Pkl.Evaluation;
using Pkl.EvaluatorManager;

var manager = new EvaluatorManager();
var evaluator = await manager.NewEvaluator(EvaluatorOptions.PreconfiguredOptons());

var config = await evaluator.EvaluateModule<AppConfig>(ModuleSource.FileSource("Config/AppConfig.pkl"));
Console.WriteLine($"{config.Host}:{config.Port}");

manager.Close();
```

Pkl property names are matched to C# properties by upper-casing the first letter (`host` → `Host`), and properties the C# class does not have are ignored. `EvaluatorManager` owns the `pkl` child process, so create one and reuse it rather than creating one per evaluation, and call `Close()` when finished.

`IEvaluator` also provides `EvaluateOutputText`, `EvaluateOutputValue<T>` and `EvaluateExpression<T>`.

## Generating C# classes from Pkl

Writing a C# class for every Pkl class by hand is tedious and easy to get out of sync. `PklCsharp.Generator` is a Roslyn source generator that reads your `.pkl` **schema** files at build time and generates the matching C# classes. It uses its own Pkl parser, so it does not need the `pkl` executable at build time.

### Setup

Reference the generator next to `PklCsharp` (generated code uses types from `PklCsharp`), and tell it which files to generate from by marking them as `AdditionalFiles`:

```xml
<ItemGroup>
  <PackageReference Include="PklCsharp" Version="0.3.1" />
  <PackageReference Include="PklCsharp.Generator" Version="0.3.1" PrivateAssets="all" />

  <AdditionalFiles Include="Config/Schema/*.pkl"
                   PklGenerator_Generate="true"
                   PklGenerator_Namespace="MyApp.Config" />
</ItemGroup>
```

| Metadata | Required | Meaning |
|---|---|---|
| `PklGenerator_Generate` | yes | Must be `true`. Pkl files without it are ignored, so config files that are only *evaluated* are never turned into classes. |
| `PklGenerator_Namespace` | no | Namespace of the generated classes. Defaults to the project's `RootNamespace`, then to `PklGenerated`. |

### Example

```pkl
// Config/Schema/AppConfig.pkl
import "NestedConfig.pkl"

host: String = "127.0.0.1"

typealias Port = UInt16(this > 0)
port: Port = 5000

nested: NestedConfig
tags: Listing<String>
timeout: Duration?
```

```pkl
// Config/Schema/NestedConfig.pkl
title: String
```

generates (in namespace `MyApp.Config`):

```csharp
public class AppConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public ushort Port { get; set; } = 5000;
    public NestedConfig Nested { get; set; } = default!;
    public string[] Tags { get; set; } = default!;
    public global::Pkl.PklTypes.Duration? Timeout { get; set; }
}

public class NestedConfig
{
    public string Title { get; set; } = default!;
}
```

which you then use with the evaluator. The *config* file is a normal Pkl file that amends the schema, and does **not** need to be an `AdditionalFile`:

```pkl
// Config/config.pkl
amends "Schema/AppConfig.pkl"

host = "example.com"
nested { title = "hello" }
tags { "a"; "b" }
```

```csharp
var config = await evaluator.EvaluateModule<AppConfig>(ModuleSource.FileSource("Config/config.pkl"));
```

Remember to copy the `.pkl` files you evaluate to the output directory (`<Content Include="Config/**/*.pkl" CopyToOutputDirectory="Always" />`). See [`Pkl.ConsoleAppExample`](https://github.com/Rafaeruo/pkl-csharp/tree/master/Examples/Pkl.ConsoleAppExample) for a complete project.

### How it works

- **A file becomes a class.** Each marked file produces one C# class named after the file (`AppConfig.pkl` → `AppConfig`) holding the module's properties, plus one class for every `class` declared in it. Generated sources are named `<File>.PklGen_<Type>.g.cs`.
- **Properties** are renamed to PascalCase, invalid identifier characters become `_`, and `hidden` and `local` properties are skipped because they never appear in evaluated output.
- **Nullability** follows the Pkl type: `String?` becomes `string?`. Non-nullable reference types are initialized with `default!` because the evaluator always assigns them.
- **Defaults**: a literal default in the schema (`port: Port = 5000`) is carried over when it fits the C# type. Anything that is not a plain literal is left to the Pkl evaluator.
- **Types without an annotation** are inferred from a literal (`count = 3` → `long`) and are otherwise `object`.
- **Inheritance**: `class Dog extends Animal` and a module-level `extends "Base.pkl"` become C# base classes, and `abstract` is preserved.
- **Imports**: `import "NestedConfig.pkl"` refers to the class generated from `NestedConfig.pkl`, which is expected to be generated into the same namespace. Generate every file that is imported.
- **Type aliases and constraints** are resolved to their underlying type (`typealias Port = UInt16(this > 0)` → `ushort`). The constraint itself is *not* generated; Pkl enforces it when the module is evaluated.
- **Unions** map to a single C# type when every member does (for example `"a"|"b"` → `string`), and to `object` otherwise.

Pkl types map to the C# types in the [type mappings](#type-mappings) table below. `Float` and `Number` both map to `double`, `Int` to `long`, and `Listing`/`List` to arrays.

### Diagnostics

| ID | Severity | Meaning |
|---|---|---|
| `PKLGEN001` | Error | Unexpected failure while generating from a file. |
| `PKLGEN002` | Error | A marked file could not be read. |
| `PKLGEN003` | Error | Invalid Pkl syntax. Reported at the location in the `.pkl` file. |
| `PKLGEN004` | Warning | A Pkl construct could not be translated, for example a type that cannot be resolved (falls back to `object`), type parameters on a class (ignored), or an `amends` file (nothing is generated, point the generator at the schema instead). |

One file that fails does not stop the others from generating.

### Limitations

- The generator parses Pkl itself and does not evaluate it, so it only knows what is written in the file. Standard library types and imports of other generated files work. A type it cannot resolve falls back to `object` with a `PKLGEN004` warning.
- Enum-like unions (`"a"|"b"`) become `string`, not a C# `enum`, and class type parameters, methods and functions are ignored.
- A property named the same as its class (for example `box` in `Box.pkl`) is renamed to `Box_`, because C# forbids it, and the evaluator does not currently fill renamed properties in. Rename the Pkl property to avoid this.

## Use Pkl as a configuration source

```sh
dotnet add package PklCSharp.Microsoft.Extensions.Configuration
```

```csharp
using Microsoft.Extensions.Configuration.Pkl;
using Pkl;

builder.Configuration.AddPklModule(ModuleSource.FileSource("Config/AppConfig.pkl"));
builder.Services.Configure<AppConfig>(builder.Configuration);
```

See [`Pkl.AspNetCoreExample`](https://github.com/Rafaeruo/pkl-csharp/tree/master/Examples/Pkl.AspNetCoreExample).

### Type mappings

The below table describes how Pkl types are mapped into C# types.

| Pkl type         | C# type                      |
|------------------|----------------------------- |
| Null             | `null`                       |
| Boolean          | `bool`                       |
| String           | `string`                     |
| Int              | `long`                       |
| Int8             | `sbyte`                      |
| Int16            | `short`                      |
| Int32            | `int`                        |
| UInt             | `ulong`                      |
| UInt8            | `byte`                       |
| UInt16           | `ushort`                     |
| UInt32           | `uint`                       |
| Float            | `double`                     |
| Number           | `double`                     |
| List<T>          | `T[]`                        |
| Listing<T>       | `T[]`                        |
| Map<K, V>        | `Dictionary<K, V>`           |
| Mapping<K, V>    | `Dictionary<K, V>`           |
| Set<T>           | `HashSet<T>`                 |
| Pair<A, B>       | `(A, B)`                     |
| Dynamic          | `dynamic`                    | 
| DataSize         | `Pkl.PklTypes.DataSize`      |
| Duration         | `Pkl.PklTypes.Duration`      |
| IntSeq           | `Pkl.PklTypes.IntSeq`        |
| Class            | `Pkl.PklTypes.Class` [1]     |
| TypeAlias        | `Pkl.PklTypes.TypeAlias` [1] |
| Any              | `dynamic`                    |
| Unions (A\|B\|C) | `dynamic` [2]                |
| Regex            | `Regex`                      |

> [1] `Pkl.PklTypes.Class` and `Pkl.PklTypes.TypeAlias` only exist for compatibilty purposes because they are possible Pkl runtime values.

> [2] If all types within a union are mapped to the same C# type, it can be decoded to that specific type instead of `dynamic`. For example, a union of `"error" | "warning"` can be decoded to `string`, whereas a union of `String | Int` would need to be decoded to `dynamic`.