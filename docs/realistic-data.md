# Realistic data

`Gen.Realistic` makes realistic English text for names, addresses, and companies. The values come from fixed
lists in the library. A failing value shrinks to the first entry of each list.

| Generator | Values |
| --- | --- |
| `Gen.Realistic.FirstName()` | A first name from `Gen.Realistic.FirstNames`. |
| `Gen.Realistic.LastName()` | A last name from `Gen.Realistic.LastNames`. |
| `Gen.Realistic.FullName()` | A first name, a space, and a last name. |
| `Gen.Realistic.StreetAddress()` | A street address, for example `1 Green Drive`. |
| `Gen.Realistic.City()` | The name of a well-known city. |
| `Gen.Realistic.Country()` | The name of a country. |
| `Gen.Realistic.PhoneNumber()` | A phone number in the format `NNN-555-01NN`. |
| `Gen.Realistic.CompanyName()` | The name of a fictional company. |

The lists hold no real private data. The names are common given names and surnames, and the companies are
fictional. The phone numbers are in the 555-01xx range, which is reserved for fiction. The lists are only in
English.

```csharp
using GalaxyCheck;

var customers =
    from name in Gen.Realistic.FullName()
    from city in Gen.Realistic.City()
    from phone in Gen.Realistic.PhoneNumber()
    select (name, city, phone);

Property.ForAll(customers, c => c.name.Contains(' ') && c.phone.Contains("-555-01")).Assert();

foreach (var customer in customers.Sample(iterations: 3, seed: 0))
{
    Console.WriteLine(customer);
}
```

To pick from a data list with your own rules, use the list with `Gen.Element`:

```csharp
using GalaxyCheck;

var shortFirstNames = Gen.Element(Gen.Realistic.FirstNames.Where(n => n.Length <= 4).ToList());
Property.ForAll(shortFirstNames, n => n.Length <= 4).Assert();
Console.WriteLine(Gen.Realistic.FirstNames.Count > 0);
```
