using System;
using System.Threading.Tasks;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;

namespace OrchardCore.ContentManagement.Display.ContentDisplay
{
    /// <summary>
    /// Any concrete implementation of this class can provide shapes for any content item which has a specific Part.
    /// </summary>
    /// <typeparam name="TPart"></typeparam>
    public abstract class ContentPartDisplayDriver<TPart> : DisplayDriverBase, IContentPartDisplayDriver where TPart : ContentPart, new()
    {
        private ContentTypePartDefinition _typePartDefinition;

        public override ShapeResult Factory(string shapeType, Func<IBuildShapeContext, Task<IShape>> shapeBuilder, Func<IShape, Task> initializeAsync)
        {
            // e.g., HtmlBodyPart.Summary, HtmlBodyPart-BlogPost, BagPart-LandingPage-Services
            // context.Shape is the ContentItem shape, we need to alter the part shape

            var result = base.Factory(shapeType, shapeBuilder, initializeAsync).Prefix(Prefix);

            if (_typePartDefinition != null)
            {
                // The stereotype is used when not displaying for a specific content type. We don't use [Stereotype] and [ContentType] at
                // the same time in an alternate because a content type is always of one stereotype.

                var stereotype = "";

                var settings = _typePartDefinition.ContentTypeDefinition?.Settings;

                if (settings != null)
                {
                    stereotype = Convert.ToString(settings[nameof(ContentTypeSettings.Stereotype)]);
                }

                if (!String.IsNullOrEmpty(stereotype) && !String.Equals("Content", stereotype, StringComparison.OrdinalIgnoreCase))
                {
                    stereotype = stereotype + "__";
                }
                
                var partName = _typePartDefinition.Name;
                var partType = _typePartDefinition.PartDefinition.Name;
                var contentType = _typePartDefinition.ContentTypeDefinition.Name;
                var editorPartType = GetEditorShapeType(_typePartDefinition);

                if (partType == shapeType || editorPartType == shapeType)
                {
                    // HtmlBodyPart, Services
                    result.Differentiator(partName);
                }
                else
                {
                    // ListPart-ListPartFeed
                    result.Differentiator($"{partName}-{shapeType}");
                }

                result.Displaying(ctx =>
                {
                    string[] displayTypes;

                    if (editorPartType == shapeType)
                    {
                        displayTypes = new[] { "_" + ctx.ShapeMetadata.DisplayType };
                    }
                    else
                    {
                        displayTypes = new[] { "", "_" + ctx.ShapeMetadata.DisplayType };

                        // [ShapeType]_[DisplayType], e.g. HtmlBodyPart.Summary, BagPart.Summary, ListPartFeed.Summary
                        ctx.ShapeMetadata.Alternates.Add($"{shapeType}_{ctx.ShapeMetadata.DisplayType}");
                    }

                    if (shapeType == partType || shapeType == editorPartType)
                    {
                        foreach (var displayType in displayTypes)
                        {
                            // [ContentType]_[DisplayType]__[PartType], e.g. Blog-HtmlBodyPart, LandingPage-BagPart
                            ctx.ShapeMetadata.Alternates.Add($"{contentType}{displayType}__{partType}");

                            if (!String.IsNullOrEmpty(stereotype))
                            {
                                // [Stereotype]__[DisplayType]__[PartType], e.g. Widget-ContentsMetadata
                                ctx.ShapeMetadata.Alternates.Add($"{stereotype}{displayType}__{partType}");
                            }
                        }

                        if (partType != partName)
                        {
                            foreach (var displayType in displayTypes)
                            {
                                // [ContentType]_[DisplayType]__[PartName], e.g. LandingPage-Services
                                ctx.ShapeMetadata.Alternates.Add($"{contentType}{displayType}__{partName}");

                                if (!String.IsNullOrEmpty(stereotype))
                                {
                                    // [Stereotype]_[DisplayType]__[PartName], e.g. LandingPage-Services
                                    ctx.ShapeMetadata.Alternates.Add($"{stereotype}{displayType}__{partName}");
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var displayType in displayTypes)
                        {
                            // [ContentType]_[DisplayType]__[PartType]__[ShapeType], e.g. Blog-ListPart-ListPartFeed
                            ctx.ShapeMetadata.Alternates.Add($"{contentType}{displayType}__{partType}__{shapeType}");

                            if (!String.IsNullOrEmpty(stereotype))
                            {
                                // [Stereotype]_[DisplayType]__[PartType]__[ShapeType], e.g. Blog-ListPart-ListPartFeed
                                ctx.ShapeMetadata.Alternates.Add($"{stereotype}{displayType}__{partType}__{shapeType}");
                            }
                        }

                        if (partType != partName)
                        {
                            foreach (var displayType in displayTypes)
                            {
                                // [ContentType]_[DisplayType]__[PartName]__[ShapeType], e.g. LandingPage-Services-BagPartSummary
                                ctx.ShapeMetadata.Alternates.Add($"{contentType}{displayType}__{partName}__{shapeType}");

                                if (!String.IsNullOrEmpty(stereotype))
                                {
                                    // [Stereotype]_[DisplayType]__[PartName]__[ShapeType], e.g. LandingPage-Services-BagPartSummary
                                    ctx.ShapeMetadata.Alternates.Add($"{stereotype}{displayType}__{partName}__{shapeType}");
                                }
                            }
                        }
                    }
                });
            }

            return result;
        }

        async Task<IDisplayResult> IContentPartDisplayDriver.BuildDisplayAsync(ContentPart contentPart, ContentTypePartDefinition typePartDefinition, BuildDisplayContext context)
        {
            var part = contentPart as TPart;

            if (part == null)
            {
                return null;
            }

            using (BuildPrefix(typePartDefinition, context.HtmlFieldPrefix))
            {
                _typePartDefinition = typePartDefinition;

                var buildDisplayContext = new BuildPartDisplayContext(typePartDefinition, context);

                var result = await DisplayAsync(part, buildDisplayContext);

                _typePartDefinition = null;

                return result;
            }
        }

        async Task<IDisplayResult> IContentPartDisplayDriver.BuildEditorAsync(ContentPart contentPart, ContentTypePartDefinition typePartDefinition, BuildEditorContext context)
        {
            var part = contentPart as TPart;

            if (part == null)
            {
                return null;
            }

            using (BuildPrefix(typePartDefinition, context.HtmlFieldPrefix))
            {

                _typePartDefinition = typePartDefinition;

                var buildEditorContext = new BuildPartEditorContext(typePartDefinition, context);

                var result = await EditAsync(part, buildEditorContext);

                _typePartDefinition = null;

                return result;
            }
        }

        async Task<IDisplayResult> IContentPartDisplayDriver.UpdateEditorAsync(ContentPart contentPart, ContentTypePartDefinition typePartDefinition, UpdateEditorContext context)
        {
            var part = contentPart as TPart;

            if (part == null)
            {
                return null;
            }

            using (BuildPrefix(typePartDefinition, context.HtmlFieldPrefix))
            {

                var updateEditorContext = new UpdatePartEditorContext(typePartDefinition, context);

                var result = await UpdateAsync(part, context.Updater, updateEditorContext);

                part.ContentItem.Apply(typePartDefinition.Name, part);

                return result;
            }
        }

        public virtual Task<IDisplayResult> DisplayAsync(TPart part, BuildPartDisplayContext context)
        {
            return Task.FromResult(Display(part, context));
        }

        public virtual IDisplayResult Display(TPart part, BuildPartDisplayContext context)
        {
            return Display(part);
        }

        public virtual IDisplayResult Display(TPart part)
        {
            return null;
        }

        public virtual Task<IDisplayResult> EditAsync(TPart part, BuildPartEditorContext context)
        {
            return Task.FromResult(Edit(part, context));
        }

        public virtual IDisplayResult Edit(TPart part, BuildPartEditorContext context)
        {
            return Edit(part);
        }

        public virtual IDisplayResult Edit(TPart part)
        {
            return null;
        }

        public virtual Task<IDisplayResult> UpdateAsync(TPart part, IUpdateModel updater, UpdatePartEditorContext context)
        {
            return UpdateAsync(part, context);
        }

        public virtual Task<IDisplayResult> UpdateAsync(TPart part, UpdatePartEditorContext context)
        {
            return UpdateAsync(part, context.Updater);
        }

        public virtual Task<IDisplayResult> UpdateAsync(TPart part, IUpdateModel updater)
        {
            return Task.FromResult<IDisplayResult>(null);
        }

        protected string GetEditorShapeType(string shapeType, ContentTypePartDefinition typePartDefinition)
        {
            var editor = typePartDefinition.Editor();
            return !String.IsNullOrEmpty(editor)
                ? shapeType + "__" + editor
                : shapeType;
        }

        protected string GetEditorShapeType(string shapeType, BuildPartEditorContext context)
        {
            return GetEditorShapeType(shapeType, context.TypePartDefinition);
        }

        protected string GetEditorShapeType(ContentTypePartDefinition typePartDefinition)
        {
            return GetEditorShapeType(typeof(TPart).Name + "_Edit", typePartDefinition);
        }

        protected string GetEditorShapeType(BuildPartEditorContext context)
        {
            return GetEditorShapeType(context.TypePartDefinition);
        }

        private TempPrefix BuildPrefix(ContentTypePartDefinition typePartDefinition, string htmlFieldPrefix)
        {
            var tempPrefix = new TempPrefix(this, Prefix);

            Prefix = typePartDefinition.Name;

            if (!String.IsNullOrEmpty(htmlFieldPrefix))
            {
                Prefix = htmlFieldPrefix + "." + Prefix;
            }

            return tempPrefix;
        }

        /// <summary>
        /// Restores the previous prefix automatically
        /// </summary>
        private class TempPrefix : IDisposable
        {
            private readonly ContentPartDisplayDriver<TPart> _driver;
            private readonly string _originalPrefix;

            public TempPrefix(ContentPartDisplayDriver<TPart> driver, string originalPrefix)
            {
                _driver = driver;
                _originalPrefix = originalPrefix;
            }

            public void Dispose()
            {
                _driver.Prefix = _originalPrefix;
            }
        }
    }
}
