# Shaman.Tld

Low-allocation version of `DomainNameParser`.

```csharp
using Shaman.Runtime;

Tld.GetTldRulesCallback => () => File.ReadAllText("public_suffix_list.dat"); // Only read once

Tld.GetDomainFromHost("host1.example.co.uk"); // "example.co.uk"
```