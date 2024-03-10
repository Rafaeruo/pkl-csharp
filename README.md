# Pkl C#

> Pkl — pronounced Pickle — is an embeddable configuration language which provides rich support for data templating and validation.

These packages are C# bindings to pkl to provide a native way to interact with pkl files.

- [Pkl Language](https://pkl-lang.org)

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
| Float            | `float`                      |
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