fake.sh status: not present in this checkout; ./fake.sh build -t GeneratedProductCheck could not be invoked.
Substitute: direct Testing generated-product validation helper filter.

$ dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-build --filter "generated product validation"
Test run for /home/developer/projects/FS.GG.Rendering/tests/Testing.Tests/bin/Debug/net10.0/Testing.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 14 ms - Testing.Tests.dll (net10.0)
