# Migration to .Net 9

In order to migrate the existing service of User management from .Net 8 to .Net 9 follow the next steps:

## 1. Update SDK

Ensure that you have the .NET 9 SDK installed.

1. Install .NET 9 SDK: 
[Download .NET 9 SDK](https://dotnet.microsoft.com/es-es/download/dotnet/9.0).

2. Verify the correct installation by running this command in a cmd 
```cmd
dotnet --list-sdks
```
You should see an entry for .NET 9.x.x in the output.

In case that Visual Studio doesn't recognize the SDK you should update it to the latest version or check which version of VS supports the new SDK.

## 2. Read the most relevant changes in the new version

See what breaking changes may  affect the app ( none in this case) 

[Breaking Changes](https://learn.microsoft.com/en-us/dotnet/core/compatibility/9.0?toc=%2Fdotnet%2Ffundamentals%2Ftoc.json&bc=%2Fdotnet%2Fbreadcrumb%2Ftoc.json#core-net-libraries)

## 3. Update Your Project File
Update the target framework in your project files (.csproj) to use .NET 9.

1. Open your .csproj file.

2. Change the <TargetFramework> element from "net8.0" to "net9.0".
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>
```
or you can right click a project, select properties, application and select the target framework to select the .Net 9.0 SDK

## 4. Update Your Packages
1. Right click on your solution, and select "manage NuGet packages for solution".
2. Select the Update tab.
3. Update the packages that appear on the list.

You can also use this command in the Package Manager Console in order to find vulnerabilities in the packages your're using.
```cmd
dotnet list package --vulnerable
```
## 5. Test Your Application
1. Run Unit Tests: Ensure that all existing unit tests pass.
2. Manual Testing: Conduct thorough testing of critical functionalities to ensure nothing is broken due to the migration.
3. Check for Deprecations: Ensure that none of the deprecated features are being used.
## 6. Review Documentation and Configuration
1. Update Documentation: Reflect any new changes or requirements introduced by .NET 9 in your project documentation.

2. Configuration Files: Review and update any configuration files if necessary.
## 7. Additional Resources
[Official .NET 9 What's new](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview)


# In order to run the app

## Setting up the DB

1. install [SQLite](https://www.sqlite.org/download.html)
2. run the migrations in the solution, select infrastructure as the default project in the Packet Management Console. And use the following command:

```cmd
Update-Database
```

## Run the app
Run the app normally most of the endpoints are protected by Authentication and Authorization, the users are in DataSeeder in the seed method in the Infrastructure project if you don't want to register a user.

## Tests
You can use the test viewer that come with Visual studio in order to explore and run the tests in a more friendly way or you can use this .NET cli command in the test project directory
```cmd
dotnet test
```

