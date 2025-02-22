using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Threading.Tasks;
using OrchardCore.ContentManagement;

namespace OrchardCore.Localization
{
    /// <summary>
    /// Provides an extension methods for <see cref="IOrchardHelper"/>.
    /// </summary>
    public static class OrchardHelperExtensions
    {
        /// <summary>
        /// Gets the culture for a given <see cref="ContentItem"/>.
        /// </summary>
        /// <param name="orchardHelper">The <see cref="IOrchardHelper"/>.</param>
        /// <param name="contentItem">The <see cref="ContentItem"/> in which to get its culture.</param>
        /// <returns></returns>
        public async static Task<CultureInfo> GetContentCultureAsync(this IOrchardHelper orchardHelper, ContentItem contentItem)
        {
            var contentManager = orchardHelper.HttpContext.RequestServices.GetService<IContentManager>();
            var cultureAspect = await contentManager.PopulateAspectAsync<CultureAspect>(contentItem);

            return cultureAspect.Culture;
        }
    }
}