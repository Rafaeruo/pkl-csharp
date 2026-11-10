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

Pkl property names are matched to C# properties by upper-casing the first letter (`host` → `Host`), and properties the C# class does not have are ignored. When a C# property needs a different name, say which Pkl property it holds with `[PklProperty]`:

```csharp
public class AppConfig
{
    [PklProperty("max-connections")]
    public int MaxConnections { get; set; }
}
``` `EvaluatorManager` owns the `pkl` child process, so create one and reuse it rather than creating one per evaluation, and call `Close()` when finished.

`IEvaluator` also provides `EvaluateOutputText`, `EvaluateOutputValue<T>` and `EvaluateExpression<T>`.

## Generating C# classes from Pkl

Writing a C# class for every Pkl class by hand is tedious and easy to get out of sync. `PklCsharp.Generator` is a Roslyn source generator that reads your `.pkl` **schema** files at build time and generates the matching C# classes. It uses its own Pkl parser, so it does not need the `pkl` executable at build time.

### Setup

Reference the generator next to `PklCsharp` (generated code uses types from `PklCsharp`), and tell it which files to generate from by marking them as `AdditionalFiles`:

```xml
<ItemGroup>
  <PackageReference Include="PklCsharp" Version="0.5.0" />
  <PackageReference Include="PklCsharp.Generator" Version="0.5.0" PrivateAssets="all" />

  <AdditionalFiles Include="Config/Schema/*.pkl"
                   PklGenerator_Generate="true"
                   PklGenerator_Namespace="MyApp.Config" />
</ItemGroup>
```

Keep `PklCsharp` and `PklCsharp.Generator` on the same version: generated code refers to attributes such as `[PklProperty]` that live in `PklCsharp`.

| Metadata | Required | Meaning |
|---|---|---|
| `PklGenerator_Generate` | yes | Must be `true`. Pkl files without it are ignored, so config files that are only *evaluated* are never turned into classes. |
| `PklGenerator_Namespace` | no | Namespace of the generated classes. Defaults to the project's `RootNamespace`, then to `PklGenerated`. |
| `PklGenerator_Enums` | no | Set to `true` to generate C# enums for unions of string literals. See [Enums](#enums-from-string-unions). |

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
- **Properties** are renamed to PascalCase, invalid identifier characters become `_`, and `hidden` and `local` properties are skipped because they never appear in evaluated output. Whenever the C# name is not simply the Pkl name with its first letter upper-cased (`` `my-prop` `` → `My_prop`, or `box` in `Box.pkl` → `Box_` because a member cannot share the name of its class), the generator adds `[global::Pkl.PklProperty("my-prop")]` so the value still reaches the property.
- **Nullability** follows the Pkl type: `String?` becomes `string?`. Non-nullable reference types are initialized with `default!` because the evaluator always assigns them.
- **Defaults**: a literal default in the schema (`port: Port = 5000`) is carried over when it fits the C# type. Anything that is not a plain literal is left to the Pkl evaluator.
- **Types without an annotation** are inferred from a literal (`count = 3` → `long`) and are otherwise `object`.
- **Inheritance**: `class Dog extends Animal` and a module-level `extends "Base.pkl"` become C# base classes, and `abstract` is preserved.
- **Imports**: `import "NestedConfig.pkl"` refers to the class generated from `NestedConfig.pkl`, which is expected to be generated into the same namespace. Generate every file that is imported.
- **Type aliases and constraints** are resolved to their underlying type (`typealias Port = UInt16(this > 0)` → `ushort`). The constraint itself is *not* generated; Pkl enforces it when the module is evaluated.
- **Unions** map to a single C# type when every member does (for example `"a"|"b"` → `string`), and to `object` otherwise. A `typealias` that is a union of string literals can become a C# enum instead, see [Enums](#enums-from-string-unions).

Pkl types map to the C# types in the [type mappings](#type-mappings) table below. `Float` and `Number` both map to `double`, `Int` to `long`, and `Listing`/`List` to arrays.

### Enums from string unions

Pkl has no enums. The idiom is a `typealias` of string literals:

```pkl
typealias Status = "active" | "in-progress" | "1st place"

status: Status = "in-progress"
```

By default that is a `string` in C#. Add `PklGenerator_Enums="true"` to the file's `AdditionalFiles` entry to get a real enum:

```xml
<AdditionalFiles Include="Config/Schema/*.pkl"
                 PklGenerator_Generate="true"
                 PklGenerator_Enums="true" />
```

```csharp
[global::Pkl.PklStringUnion]
public enum Status
{
    [global::Pkl.PklStringUnionValue("active")]
    Active,
    [global::Pkl.PklStringUnionValue("in-progress")]
    InProgress,
    [global::Pkl.PklStringUnionValue("1st place")]
    _1stPlace,
}

public class AppConfig
{
    public Status Status { get; set; } = global::MyApp.Config.Status.InProgress;
}
```

- A `typealias` becomes an enum when its body is only string literals, optionally with `Null` (which makes properties of that type nullable). Aliases that mix in other types (`"a" | Int`, `"a" | String`) and aliases with type parameters stay `string` or `object`.
- Enum names are PascalCased from the string and made valid C# identifiers (`"in-progress"` → `InProgress`, `"1st place"` → `_1stPlace`). Names that collide get a trailing `_`. The exact Pkl string is kept in `[PklStringUnionValue]`, so the name only has to be valid.
- Enum properties work everywhere a value can appear: plain, nullable, in `Listing`, as `Mapping` keys and in `Set`. A literal default (`= "in-progress"`) becomes the matching enum member.
- It is opt-in because it changes the type of generated properties from `string`, and the generated code needs `PklCsharp` 0.4.0 or newer (the attributes live there).
- Decoding is strict: a Pkl string that is not one of the enum's values throws a `FormatException` listing the accepted values. A `string` property never does this.

You can also write such an enum yourself. `[PklStringUnion]` is required on the enum, and a member without `[PklStringUnionValue("...")]` matches its own name exactly:

```csharp
[PklStringUnion]
public enum LogLevel
{
    [PklStringUnionValue("debug")] Debug,
    [PklStringUnionValue("info")] Info,
    Warning, // matches the Pkl string "Warning"
}
```

### Mapping types to your own C# types

Some values are `String`s in Pkl but have a proper type in C#: GUIDs, timestamps, IP addresses, money. Put `@CSharpType` on a `typealias` to say which existing C# type values of it have:

```pkl
import "package://github.com/Rafaeruo/pkl-csharp/releases/download/0.5.0/pkl-csharp@0.5.0#/CSharp.pkl"

@CSharp.CSharpType { name = "System.Guid" }
typealias Id = String

@CSharp.CSharpType { name = "MyApp.Domain.Money" }
typealias Price = String

id: Id
price: Price?
replicas: Listing<Id>
```

```csharp
public global::System.Guid Id { get; set; } = default!;
public global::MyApp.Domain.Money? Price { get; set; }
public global::System.Guid[] Replicas { get; set; } = default!;
```

- **Where the annotation comes from.** Pkl will not evaluate a schema that uses an annotation whose class it can't find, so the schema has to bring `CSharpType` in. The simplest way is the `pkl-csharp` Pkl package imported above (change the version in the URL to the one you use). You can also write the class yourself, in the schema or in a module you share, because the generator only looks at the annotation's name, `CSharpType`:

  ```pkl
  class CSharpType extends Annotation {
    name: String
  }
  ```
- **`name`** is a plain C# type name such as `System.Guid` or `MyApp.Domain.Money` (a leading `global::` is fine). Generic, array and nullable forms are not supported. The generator does not check that the type exists, so a typo shows up as a compiler error in the generated code. If the name isn't usable, the annotation is ignored with a `PKLGEN004` warning.
- **It applies to `typealias`es.** Every property of that type uses the C# type, including when it is nullable or inside a `Listing`, `Mapping` or `Set`. It takes precedence over [enum generation](#enums-from-string-unions) for the same alias.
- **Conversion at runtime.** PklCsharp converts the Pkl value into the type:
  - A `String` becomes any type that implements `IParsable<T>` (`Guid`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `IPAddress`, and your own types, which only need a static `Parse`/`TryParse`), using the invariant culture. `DateTime` keeps its kind (a `Z` stays UTC) and `Version` is supported too.
  - An object becomes a class with matching properties, as usual.
  - A string that does not parse throws a `FormatException` naming the value and the type. Built-in numeric and boolean types are deliberately not parsed from strings.
- This also works without the generator: any property of such a type in a class you write yourself is converted the same way.

### Diagnostics

| ID | Severity | Meaning |
|---|---|---|
| `PKLGEN001` | Error | Unexpected failure while generating from a file. |
| `PKLGEN002` | Error | A marked file could not be read. |
| `PKLGEN003` | Error | Invalid Pkl syntax. Reported at the location in the `.pkl` file. |
| `PKLGEN004` | Warning | A Pkl construct could not be translated, for example a type that cannot be resolved (falls back to `object`), type parameters on a class (ignored), an unusable `@CSharpType` annotation (ignored), or an `amends` file (nothing is generated, point the generator at the schema instead). |

One file that fails does not stop the others from generating.

### Limitations

- The generator parses Pkl itself and does not evaluate it, so it only knows what is written in the file. Standard library types and imports of other generated files work. A type it cannot resolve falls back to `object` with a `PKLGEN004` warning.
- `@CSharpType` works on `typealias`es only, not on single properties or on classes, and the generator takes the C# type name on trust.
- Unions of string literals only become C# enums when [enabled](#enums-from-string-unions), and only for a `typealias` in the same file: an alias declared in another file and used through an import is not recognised yet. Inline unions (`q: "a" | "b"`) have no name, so they stay `string`. Class type parameters, methods and functions are ignored.

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
| Char [3]         | `char`                       |
| Uri [3]          | `System.Uri`                 |
| pkl.net#Url [4]  | `System.Uri`                 |
| Bytes            | `byte[]`                     |
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

> [3] In Pkl these are strings with a constraint (`Char` is `String(length == 1)`, `Uri` is `String(isValidUrl)`), so they are converted when the C# property is a `char` or `System.Uri`. Untyped (`dynamic`) values stay `string`. A `Char` holds one UTF-16 code unit, so a character outside the Basic Multilingual Plane (such as an emoji) cannot be decoded into a `char` and gives a `FormatException`.

> [4] `Url` is a class in the `pkl:net` module, added in Pkl 0.33 (`import "pkl:net"`, then `net.Url`). It is decoded from its `stringValue`, so unlike `Uri` it also accepts relative references. Generated code maps it to `System.Uri`; a hand-written property can be a `System.Uri` or a `string`.
