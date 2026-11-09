using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace Revit.Linter.Sandbox.TemplateScan;

/// <summary>One template of the scan queue: a discipline label and the absolute .rte path.</summary>
internal sealed class TemplateQueueItem(string discipline, string path)
{
    public string Discipline { get; } = discipline;

    public string Path { get; } = path;
}

/// <summary>Reads every project parameter binding of a template document.</summary>
internal static class TemplateScanner
{
    public static TemplateSnapshot Read(Document document, TemplateQueueItem item, string? packsDirectory = null)
    {
        TemplateSnapshot snapshot = new()
        {
            Discipline = item.Discipline,
            TemplatePath = item.Path,
            FileName = Path.GetFileName(item.Path),
        };

        try
        {
            snapshot.Title = document.Title;
            snapshot.IsWorkshared = document.IsWorkshared;
            snapshot.RevitVersion = document.Application.VersionNumber;
            snapshot.RevitBuild = document.Application.VersionBuild;

            ReadBindings(document, snapshot);
            ReadParameterElements(document, snapshot);
            ReadProjectInformation(document, snapshot);
            if (packsDirectory is not null)
                TemplateVerifier.Verify(document, snapshot, packsDirectory);
        }
        catch (Exception exception)
        {
            snapshot.Error = exception.GetType().Name + ": " + exception.Message;
        }

        return snapshot;
    }

    /// <summary>
    /// Creates a new project from the template and reads it. The document is intentionally left open:
    /// the Revit API may not close the active document, and the scan process treats every produced
    /// document as disposable, because Revit is stopped without saving at the end of the scan.
    /// </summary>
    public static TemplateSnapshot OpenAndRead(
        Application application, TemplateQueueItem item, string? packsDirectory = null)
    {
        TemplateSnapshot snapshot = new()
        {
            Discipline = item.Discipline,
            TemplatePath = item.Path,
            FileName = Path.GetFileName(item.Path),
        };

        try
        {
            Document document = application.NewProjectDocument(item.Path);
            snapshot = Read(document, item, packsDirectory);
        }
        catch (Exception exception)
        {
            snapshot.Error = exception.GetType().Name + ": " + exception.Message;
        }

        return snapshot;
    }

    private static void ReadBindings(Document document, TemplateSnapshot snapshot)
    {
        BindingMap bindingMap = document.ParameterBindings;
        DefinitionBindingMapIterator iterator = bindingMap.ForwardIterator();
        iterator.Reset();
        while (iterator.MoveNext())
        {
            if (iterator.Key is not InternalDefinition definition)
                continue;

            BindingEntry entry = new()
            {
                Name = definition.Name,
                ElementId = definition.Id.IntegerValue,
                Group = definition.ParameterGroup.ToString(),
                GroupNumeric = (int)definition.ParameterGroup,
                VariesAcrossGroups = definition.VariesAcrossGroups,
                ParameterType = definition.ParameterType.ToString(),
                Visible = definition.Visible,
                IsBuiltInParameter = definition.BuiltInParameter != BuiltInParameter.INVALID,
                BuiltInParameter = definition.BuiltInParameter != BuiltInParameter.INVALID
                    ? definition.BuiltInParameter.ToString()
                    : null,
            };

            if (document.GetElement(definition.Id) is SharedParameterElement shared)
                entry.SharedGuid = shared.GuidValue.ToString();

            switch (iterator.Current)
            {
                case InstanceBinding instanceBinding:
                    entry.BindingKind = "instance";
                    ReadCategories(instanceBinding, entry);
                    break;
                case TypeBinding typeBinding:
                    entry.BindingKind = "type";
                    ReadCategories(typeBinding, entry);
                    break;
            }

            snapshot.Bindings.Add(entry);
        }
    }

    private static void ReadCategories(ElementBinding binding, BindingEntry entry)
    {
        foreach (Category category in binding.Categories)
        {
            entry.Categories.Add(new CategoryEntry
            {
                Id = category.Id.IntegerValue,
                BuiltInCategory = ((BuiltInCategory)category.Id.IntegerValue).ToString(),
                Name = category.Name,
            });
        }
    }

    private static void ReadParameterElements(Document document, TemplateSnapshot snapshot)
    {
        foreach (ParameterElement element in new FilteredElementCollector(document)
                     .OfClass(typeof(ParameterElement))
                     .Cast<ParameterElement>())
        {
            ParameterElementEntry entry = new()
            {
                Name = element.Name,
                ElementId = element.Id.IntegerValue,
            };

            if (element is SharedParameterElement shared)
                entry.SharedGuid = shared.GuidValue.ToString();

            if (element.GetDefinition() is InternalDefinition definition)
            {
                entry.Group = definition.ParameterGroup.ToString();
                entry.ParameterType = definition.ParameterType.ToString();
                entry.Visible = definition.Visible;
                entry.IsBuiltInParameter = definition.BuiltInParameter != BuiltInParameter.INVALID;
                entry.BuiltInParameter = definition.BuiltInParameter != BuiltInParameter.INVALID
                    ? definition.BuiltInParameter.ToString()
                    : null;
                entry.VariesAcrossGroups = definition.VariesAcrossGroups;
            }

            snapshot.ParameterElements.Add(entry);
        }
    }

    private static void ReadProjectInformation(Document document, TemplateSnapshot snapshot)
    {
        ProjectInfo? projectInformation = document.ProjectInformation;
        if (projectInformation is null)
            return;

        snapshot.ProjectInformation.Add(("Number", projectInformation.Number ?? string.Empty));
        snapshot.ProjectInformation.Add(("Name", projectInformation.Name ?? string.Empty));
        snapshot.ProjectInformation.Add(("OrganizationName", projectInformation.OrganizationName ?? string.Empty));
        snapshot.ProjectInformation.Add(("Author", projectInformation.Author ?? string.Empty));
        snapshot.ProjectInformation.Add(("IssueDate", projectInformation.IssueDate ?? string.Empty));

        foreach (Parameter parameter in projectInformation.Parameters)
        {
            if (parameter.Definition is not InternalDefinition definition)
                continue;

            string? value = parameter.StorageType switch
            {
                StorageType.String => parameter.AsString(),
                StorageType.Integer => parameter.AsInteger().ToString(),
                _ => parameter.AsValueString(),
            };

            snapshot.ProjectInformationParameters.Add(new ProjectInformationParameterEntry
            {
                Name = definition.Name,
                BuiltInParameter = definition.BuiltInParameter != BuiltInParameter.INVALID
                    ? definition.BuiltInParameter.ToString()
                    : null,
                StorageType = parameter.StorageType.ToString(),
                Value = value,
            });
        }
    }
}
