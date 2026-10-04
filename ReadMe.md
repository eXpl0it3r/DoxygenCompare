# Doxygen Compare

Compare two versions of the same API with Doxygen's XML output.

When you want to find out what has changed between two version of the same C++
library, you can have Doxygen generate its XML output for both versions, which
can then be read by Doxygen Compare to determine what has been added, removed
or changed.

It covers the whole API surface Doxygen documents:

- Namespaces, classes, structs and unions, including base classes, template parameters, `abstract` and `final`
- Functions and their overloads, including return type, parameter types, default arguments, `const`, `static`, `virtual`, `explicit`, `constexpr`, `noexcept`, `[[nodiscard]]` as well as deleted and defaulted functions
- Variables, typedefs and `using` aliases
- Enums and their values, including initializers
- Macros
- Friends and protected members (the latter can be excluded)
- Deprecation via `\deprecated` or `[[deprecated]]`

## Usage

### Step 1 - Generate Doxygen Documentation

Set `GENERATE_XML = YES` in the Doxyfile. This will generate not just the HTML,
or whatever other output you've selected, but also an `xml` directory with an
`index.xml` and one XML file per class, namespace and header file.

Generate the XML output for both, the old and the new API version.

For SFML you can use `GenerateDocs.ps1`. Note that it runs `git checkout .` in
the given SFML directory, so don't use it on a working copy with uncommitted
changes.

### Step 2 - Run Doxygen Compare

Doxygen Compare offers the following parameters:

```
  -a, --fileA           Required. Doxygen XML output of the old version, either the index.xml file or the directory containing it

  -b, --fileB           Required. Doxygen XML output of the new version, either the index.xml file or the directory containing it

  --nameA               Name of the old version used in the output, defaults to the path

  --nameB               Name of the new version used in the output, defaults to the path

  -f, --format          (Default: Text) Output format: Text, Markdown or Json

  -o, --output          Write the result to this file instead of the console

  -e, --exclude         Comma separated scopes or names to ignore, e.g. sf::priv

  --excludeProtected    Ignore protected members

  --help                Display this help screen.

  --version             Display version information.
```

You can run it for example like this:

```
DoxygenCompare.exe -a ../build-3.0.2/doc/xml -b ../build-3.1.0/doc/xml --nameA 3.0.2 --nameB 3.1.0 -f markdown -o changes.md
```

The result is grouped by scope and lists for each scope:

- Added and removed declarations, members of added or removed classes and namespaces aren't listed again
- Functions with a changed signature, where the only overload with that name got different parameters or qualifiers
- Changed declarations, together with the changed properties, e.g. return type, default arguments or deprecation

## How It Works

- Doxygen Compare reads the `index.xml` to find all namespaces, classes and files, then reads the XML file of each of them
- Every public and protected declaration is turned into an entity with a key, functions use the parameter types and the `const`/`&`/`&&` qualifiers to tell overloads apart
- Everything else about a declaration, e.g. the return type or default arguments, is a property that is compared between the two versions
- Type spelling is normalized, so `const Texture &` and `const Texture&` compare equal

## Limitations

- Doxygen only sees what the preprocessor lets through, so platform specific declarations depend on the `PREDEFINED` macros in the Doxyfile
- Declarations hidden from Doxygen, e.g. through `EXCLUDE_SYMBOLS` or `\cond`, aren't compared
- Renames show up as one removed and one added declaration
- When several overloads of the same function change at once, they're listed as removed and added instead of as signature changes
- Changed parameter names aren't reported, as they don't affect the API

## Enhancements

- [x] Detect attribute changes
- [x] Detect enum changes
- [x] Detect enum value changes
- [x] Detect function signature changes
- [ ] Additional automations, e.g. through GitHub Actions
- [x] Different comparison result/output
- [ ] Support as library
- [ ] Publishing as NuGet package

## Tooling Used / Required

- [Doxygen](https://www.doxygen.nl/) - tested with 1.18.0
- [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

The test fixtures in `DoxygenCompare.Tests/Fixtures` can be regenerated with `Generate.ps1`, which requires `doxygen` in the `PATH`.

## License

The code itself is available under 2 licenses: Public Domain or MIT -- choose whichever you prefer, see also the license file.
