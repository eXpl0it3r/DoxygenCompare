# Doxygen Compare

Compare two versions of the same API with Doxygen's XML output.

When you want to find out what has changed between two version of the same C++
library, you can have Doxygen generate the index as XML file, which can then
be imported by Doxygen Compare and determine what has been added or removed.

This was quickly put together as a proof of concept and may or may not be
expanded in the future.

## Usage

### Step 1 - Generate Doxygen Documentation

Use the `-x` flag or set the `GENERATE_XML` variable in the doxygen file. This
will generate not just the HTML, or whatever other output you've selected, but
also create XML files with all the necessary information.

For now Doxygen Compare only supports the `index.xml` file.

Generate both XML files, for the old and the new API version.

### Step 2 - Run Doxygen Compare

Doxygen Compare offers the following parameters:

```
  -a, --fileA    Required. First doxygen index.xml file for comparison

  -b, --fileB    Required. Second doxygen index.xml file for comparison

  --help         Display this help screen.

  --version      Display version information.
```

You can run it for example like this:

```
DoxygenCompare.exe -a ../build2/doc/xml/index.xml -b ../build3/doc/xml/index.xml
```

## How It Works

-   Doxygen provides with the XML output also XSD files, with which one can
    generate C# classes
    -   Run: `xsd ./index.xsd /out:class /language:CS /classes`
    -   On Windows the `xsd.exe` can be for example located at: `C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8.1 Tools`
-   Doxygen Compare reads the two XML versions and matches via the classes,
    namespaces, and function names to determine, what as been added or removed.

## Enhancements

-   [x] Detect attribute changes
-   [x] Detect enum changes
-   [ ] Detect enum value changes (filter by startsWith(refid)) - only for unchanged enums
-   [ ] Detect function signature changes
-   [ ] Additional automations, e.g. through GitHub Actions
-   [ ] Different comparison result/output
-   [ ] Support as library
-   [ ] Publishing as NuGet package

## Tooling Used / Required

-   [Doxygen 1.11.0](https://www.doxygen.nl/)
-   [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
-   [XSD](https://learn.microsoft.com/en-us/dotnet/standard/serialization/xml-schema-def-tool-gen) to generate the doxygen C# class from the XSD

## License

The code itself is available under 2 licenses: Public Domain or MIT -- choose whichever you prefer, see also the license file.
