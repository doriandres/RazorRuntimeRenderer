# RazorRuntimeRenderer
Razor runtime renderer for .NET Core


## Dependency
- [Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation/6.0.10)

#### Dependency installation
```
PS C:\> dotnet add package Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation --version 6.0.10
```

## Usage Example on .NET 6
```csharp
using WebApp;
using RazorRuntimeRenderer;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorRuntimeRendererSupport(); // Add service support

var app = builder.Build();
app.Map("/", async (HttpContext httpCtx) =>
{
    var nameQueryParam = httpCtx.Request.Query?["name"];

    var model = new MyViewModel
    {
        PageTitle = "My Page",
        Name = !string.IsNullOrEmpty(nameQueryParam) ? nameQueryParam : "World",
    };

    // Dynamically compile Razor from a string template and get a string result
    var html = await RazorRenderer.RenderStringAsync(httpCtx, model, @"
        @model WebApp.MyViewModel
        <html>
        <head>
            <title>@Model.PageTitle</title>
        </head>
        <body>
            <h1>Hello @Model.Name</h1>
        </body>
        </html>
    ");

    await httpCtx.Response.WriteAsync(html);
});

app.Run(); 
```
