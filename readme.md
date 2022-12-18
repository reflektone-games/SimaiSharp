# SimaiSharp

SimaiSharp is an interpreter and serializer for [simai](https://w.atwiki.jp/simai/), 
a custom chart format for the arcade rhythm game [maimai](https://maimai.sega.jp/),
written in [C#](https://learn.microsoft.com/en-us/dotnet/csharp/), 
originally intended for use in [AstroDX](https://github.com/2394425147/maipaddx).

# Getting Started

To use SimaiSharp in your own project, 
you will need to add a reference to the SimaiSharp library in your solution.

Then, use the following method to deserialize a chart:

```csharp
SimaiConvert.Deserialize(string value);
```

# Contribute

Issues and pull requests are welcome!