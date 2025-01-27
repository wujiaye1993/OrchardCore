using System.Threading.Tasks;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Modules;

namespace OrchardCore.ContentFields.Settings
{
    [RequireFeatures("OrchardCore.ContentLocalization")]
    public class LocalizationSetContentPickerFieldSettingsDriver : ContentPartFieldDefinitionDisplayDriver<LocalizationSetContentPickerField>
    {
        public override IDisplayResult Edit(ContentPartFieldDefinition partFieldDefinition)
        {
            return Initialize<LocalizationSetContentPickerFieldSettings>("LocalizationSetContentPickerFieldSettings_Edit", model => partFieldDefinition.Settings.Populate(model))
                .Location("Content");
        }

        public override async Task<IDisplayResult> UpdateAsync(ContentPartFieldDefinition partFieldDefinition, UpdatePartFieldEditorContext context)
        {
            var model = new LocalizationSetContentPickerFieldSettings();

            await context.Updater.TryUpdateModelAsync(model, Prefix);

            context.Builder.MergeSettings(model);
            context.Builder.WithSetting(nameof(LocalizationSetContentPickerFieldSettings.DisplayedContentTypes), model.DisplayedContentTypes);

            return Edit(partFieldDefinition);
        }
    }
}
