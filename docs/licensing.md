# Licensing

ComOfficeKit is licensed per product, quoted and delivered by a sales team. A licence comes as a key
(a string) or a licence file, and the engine verifies it on the machine. **No network is used, at
activation or ever.**

## Activate

```csharp
License.Activate(key);                 // the key, as a string
// or
License.ActivateFromFile(path);        // the licence file
```

Activate once, before the first document. A key or file that is not valid on this machine throws
`LicenseException`, whose `Error` says why (below).

### Licence files

`ActivateFromFile` reads a licence in any of the forms one is delivered in:

| Form | Where the licence is |
|---|---|
| a `.lic` file | its `Sign=` line |
| an XML file | its first `key` element |
| a text file | the whole file, which holds only the licence |

The file may be UTF-8, with or without a byte-order mark, or UTF-16 with one, with any line endings. A file
that cannot be read throws `ComOfficeKitException` with `ErrorKind.IO`.

## Where an activation is kept

```csharp
License.Configure(LicenseStorage.Persistent);          // the default
License.Configure(LicenseStorage.Memory);              // this process only
License.Configure(LicenseStorage.Persistent, "MyApp"); // a name of your own
```

| Storage | What happens |
|---|---|
| `Persistent` | The licence is kept for the current Windows user, encrypted so that only that user on this machine can read it (Windows' data protection, DPAPI). The next start of your application finds it. Another user, or a copy of the file on another machine, reads as not activated. |
| `Memory` | Nothing is written. The licence lasts until the process ends. Use it when you hold the key yourself and activate at every start, for example in a service. |

`Configure` comes before `Activate`. The name keeps two applications on one machine from sharing an
activation.

`License.Deactivate()` removes the active licence and the stored one.

## What an application shows

```csharp
switch (License.State)
{
    case LicenseState.Active:
        Console.WriteLine($"Licensed to {License.LicensedTo ?? "-"}, until {License.Expires?.ToString("d") ?? "-"}");
        break;
    case LicenseState.Expired:
        Console.WriteLine("The licence has expired: contact sales to renew.");
        break;
    case LicenseState.NotActivated:
        Console.WriteLine("No licence yet: ask for the key.");
        break;
}
```

`License.CheckUnlocked()` throws the `LicenseException` an operation would, and does nothing when a
licence is active: a way to check at start-up rather than at the first document.

## The application a licence is for

A licence can be bound to one application. The application is the .NET assembly that calls
`License.Activate` or `License.ActivateFromFile`, by its assembly name:

| The call is made in | The application is |
|---|---|
| your program, `MyApp.exe` | `MyApp` |
| a library of yours, `MyApp.Licensing.dll` | `MyApp.Licensing` |

- **Activate from the assembly the licence names.** When one library of yours activates for several
  programs, ask for the licence in that library's name.
- **A kept licence is checked against your program's main assembly.** A licence kept from an earlier run
  (`LicenseStorage.Persistent`) is checked against the main assembly until something activates in this process.
  So when the licence names a library rather than your program, activate from that library at every start.
- **How the program is started does not matter.** `dotnet MyApp.dll` and `MyApp.exe` both activate as `MyApp`.

## Errors

| `LicenseError` | Meaning | What to show |
|---|---|---|
| `NotActivated` | no licence yet | your licence entry |
| `InvalidKey` | not a ComOfficeKit licence, or altered since it was issued | "paste the whole key from your licence email" |
| `WrongProduct` | issued for another product | "check the licence" |
| `Expired` | the term has ended | "contact sales to renew" |
| `PlatformMismatch` | the licence does not cover Windows | "contact sales" |
| `AppIdMismatch` | the licence is bound to another application | "contact sales" |
| `FeatureNotLicensed` | the licence does not include this operation | "contact sales" |
| `VersionNotLicensed` | the licence does not cover this version of ComOfficeKit | "contact sales, or use the version your licence covers" |
| `NoVerificationKey` | this build cannot verify licences | "contact support"; a release never shows it |

Catch `LicenseException` apart from other errors: it is the one failure a healthy file can produce,
and the one to answer with a licence prompt rather than "could not open".

## Third-party notices

The licence check includes third-party software. Its notices are in the package's
`THIRD-PARTY-NOTICES.txt`, under "Licence verification: the third-party software it includes". Ship that file
with your application, beside the other notices it carries.

## Trial

Each release's README publishes a free evaluation key, valid for about 60 days, that names no
application: [Free trial and licence](../README.md#free-trial-and-licence). For a production licence
or a quote, email sales@comofficekit.com.
