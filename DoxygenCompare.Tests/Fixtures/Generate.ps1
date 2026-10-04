# Regenerates the Doxygen XML of the test fixtures, requires doxygen in the PATH
foreach ($Version in "old", "new")
{
    Push-Location (Join-Path $PSScriptRoot $Version)
    doxygen (Join-Path $PSScriptRoot "Doxyfile")
    Pop-Location
}
