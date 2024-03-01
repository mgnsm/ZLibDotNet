## Contributions
Contributions such as bug fixes, extended functionality within the scope of the zlib compression library and other kind of improvements are welcome.

Follow these steps when contributing code to this repository:

1. Open an issue where you describe the reason behind the code being contributed.

2. Clone the repository:

        git clone https://github.com/mgnsm/ZLibDotNet.git

3. Create a new feature branch:

        git branch -b feature_branch

4. Make and commit your changes to this feature branch:

        git commit -m "Some informational message"

    The [.editorconfig](.editorconfig) file defines the code style. Do not edit this one or the [project file](https://github.com/mgnsm/ZLibDotNet/blob/main/src/ZLibDotNet/ZLibDotNet.csproj) unless these files are directly related to your changes.

5. Build and test the code in both debug and release mode using the [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0):

        dotnet test tests/ZLibDotNet.UnitTests/ZLibDotNet.UnitTests.csproj -c release

    Make sure that the code builds without any warnings and that all existing and any new unit tests pass.

6. Push the feature branch to GitHub:

        git push -u origin feature_branch

7. Submit a [pull request](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/creating-a-pull-request), including a clear and concise description, against the `main` branch.

8. Wait for the pull request to become validated and approved.