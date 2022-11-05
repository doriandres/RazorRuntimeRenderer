using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ObjectPool;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace RazorRuntimeRenderer
{
    public static class RazorRenderer
    {
        #region Private methods

        private static HtmlHelperOptions GetHtmlHelperOptions()
        {
            return new HtmlHelperOptions();
        }

        private static TempDataDictionary GetTempDataDictionary(this HttpContext httpContext)
        {
            return new TempDataDictionary(httpContext, httpContext.RequestServices.GetRequiredService<ITempDataProvider>());
        }

        private static ActionContext ToActionContext(this HttpContext httpContext)
        {
            return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        }

        private static ViewDataDictionary GetDefaultViewData()
        {
            return new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        }

        private static ViewDataDictionary GetViewDataForModel<TModel>(TModel model)
        {
            return new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model };
        }

        private static ViewContext CreateViewContext(HttpContext httpContext, ViewDataDictionary viewData, IView view, TextWriter writer)
        {
            return new ViewContext(httpContext.ToActionContext(), view, viewData, httpContext.GetTempDataDictionary(), writer, GetHtmlHelperOptions());
        }

        private static (string, string) GenerateRandomViewPathAndFilePath()
        {
            var viewPath = $"{Guid.NewGuid().ToString().Replace("-", string.Empty)}.cshtml";
            var filepath = Path.Combine(Directory.GetCurrentDirectory(), viewPath);

            return (viewPath, filepath);
        }

        private static async Task<string> RenderViewAsync(HttpContext httpContext, ViewDataDictionary viewData, IView view)
        {
            using var writer = new StringWriter();            
            var viewContext = CreateViewContext(httpContext, viewData, view, writer);
            await view.RenderAsync(viewContext);
            return writer.ToString();
        }

        #endregion

        #region IServiceCollection Extensions
        public static IServiceCollection AddRazorRuntimeRendererSupport(this IServiceCollection services)
        {
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddSingleton<DiagnosticSource>(new DiagnosticListener("Microsoft.AspNetCore"));
            services.AddLogging();
            services.AddMvc().AddRazorRuntimeCompilation();

            return services;
        }
        #endregion

        public async static Task<string?> RenderStringAsync(HttpContext httpContext, string razorCode)
        {
            var razorViewEngine = httpContext.RequestServices.GetRequiredService<IRazorViewEngine>();
            var (viewPath, filepath) = GenerateRandomViewPathAndFilePath();
            File.WriteAllText(filepath, razorCode);
            var viewResult = razorViewEngine.GetView(Directory.GetCurrentDirectory(), viewPath, true);
            if (!viewResult.Success) return null;
            var result = await RenderViewAsync(httpContext, GetDefaultViewData(), viewResult.View);
            File.Delete(filepath);
            return result;
        }

        public async static Task<string?> RenderStringAsync<TModel>(HttpContext httpContext, TModel model, string razorCode)
        {
            var razorViewEngine = httpContext.RequestServices.GetRequiredService<IRazorViewEngine>();
            var (viewPath, filepath) = GenerateRandomViewPathAndFilePath();
            File.WriteAllText(filepath, razorCode);
            var viewResult = razorViewEngine.GetView(Directory.GetCurrentDirectory(), viewPath, true);
            if (!viewResult.Success) return null;
            var result = await RenderViewAsync(httpContext, GetViewDataForModel(model), viewResult.View);
            File.Delete(filepath);
            return result;
        }

        public async static Task<string?> RenderFileAsync(HttpContext httpContext, string basePath, string filePath)
        {
            var razorViewEngine = httpContext.RequestServices.GetRequiredService<IRazorViewEngine>();
            var viewResult = razorViewEngine.GetView(basePath, filePath, true);
            if (!viewResult.Success) return null;
            return await RenderViewAsync(httpContext, GetDefaultViewData(), viewResult.View);
        }

        public async static Task<string?> RenderFileAsync<TModel>(HttpContext httpContext, TModel model, string basepath, string filepath)
        {
            var razorViewEngine = httpContext.RequestServices.GetRequiredService<IRazorViewEngine>();
            var viewResult = razorViewEngine.GetView(basepath, filepath, true);
            if (!viewResult.Success) return null;
            return await RenderViewAsync(httpContext, GetViewDataForModel(model), viewResult.View);
        }
    }
}
