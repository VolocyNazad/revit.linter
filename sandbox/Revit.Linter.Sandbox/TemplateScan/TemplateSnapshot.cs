namespace Revit.Linter.Sandbox.TemplateScan;

/// <summary>Result of reading one template; serialized to JSON by the scan application.</summary>
internal sealed class TemplateSnapshot
{
    public string Discipline { get; set; } = string.Empty;

    public string TemplatePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string RevitVersion { get; set; } = string.Empty;

    public string RevitBuild { get; set; } = string.Empty;

    public bool IsWorkshared { get; set; }

    public string? Error { get; set; }

    public string? CloseError { get; set; }

    public string? VerificationFile { get; set; }

    public int VerificationChecked { get; set; }

    public List<string> Verification { get; set; } = [];

    public List<BindingEntry> Bindings { get; } = [];

    public List<ParameterElementEntry> ParameterElements { get; } = [];

    public List<(string Name, string Value)> ProjectInformation { get; } = [];

    public List<ProjectInformationParameterEntry> ProjectInformationParameters { get; } = [];

    public string ToJson()
    {
        JsonBuilder json = new();
        json.BeginObject()
            .Name("discipline").String(Discipline)
            .Name("templatePath").String(TemplatePath)
            .Name("fileName").String(FileName)
            .Name("title").String(Title)
            .Name("revitVersion").String(RevitVersion)
            .Name("revitBuild").String(RevitBuild)
            .Name("isWorkshared").Bool(IsWorkshared)
            .Name("error").String(Error)
            .Name("closeError").String(CloseError)
            .Name("verificationFile").String(VerificationFile)
            .Name("verificationChecked").Number(VerificationChecked)
            .Name("verificationProblems").BeginArray();
        foreach (string problem in Verification)
            json.String(problem);
        json.EndArray();

        json.Name("projectInformation").BeginObject();
        foreach ((string name, string value) in ProjectInformation)
            json.Name(name).String(value);
        json.EndObject();

        json.Name("projectInformationParameters").BeginArray();
        foreach (ProjectInformationParameterEntry entry in ProjectInformationParameters)
            entry.Write(json);
        json.EndArray();

        json.Name("bindings").BeginArray();
        foreach (BindingEntry entry in Bindings)
            entry.Write(json);
        json.EndArray();

        json.Name("parameterElements").BeginArray();
        foreach (ParameterElementEntry entry in ParameterElements)
            entry.Write(json);
        json.EndArray();

        json.EndObject();
        return json.ToString();
    }
}

internal sealed class CategoryEntry
{
    public int Id { get; set; }

    public string BuiltInCategory { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public void Write(JsonBuilder json) => json.BeginObject()
        .Name("id").Number(Id)
        .Name("builtInCategory").String(BuiltInCategory)
        .Name("name").String(Name)
        .EndObject();
}

internal sealed class BindingEntry
{
    public string Name { get; set; } = string.Empty;

    public int ElementId { get; set; }

    public string? SharedGuid { get; set; }

    public string Group { get; set; } = string.Empty;

    public int GroupNumeric { get; set; }

    public bool VariesAcrossGroups { get; set; }

    public string ParameterType { get; set; } = string.Empty;

    public bool Visible { get; set; }

    public bool IsBuiltInParameter { get; set; }

    public string? BuiltInParameter { get; set; }

    public string BindingKind { get; set; } = "unknown";

    public List<CategoryEntry> Categories { get; } = [];

    public void Write(JsonBuilder json)
    {
        json.BeginObject()
            .Name("name").String(Name)
            .Name("elementId").Number(ElementId)
            .Name("sharedGuid").String(SharedGuid)
            .Name("bindingKind").String(BindingKind)
            .Name("group").String(Group)
            .Name("groupNumeric").Number(GroupNumeric)
            .Name("variesAcrossGroups").Bool(VariesAcrossGroups)
            .Name("parameterType").String(ParameterType)
            .Name("visible").Bool(Visible)
            .Name("isBuiltInParameter").Bool(IsBuiltInParameter)
            .Name("builtInParameter").String(BuiltInParameter)
            .Name("categories").BeginArray();
        foreach (CategoryEntry category in Categories)
            category.Write(json);
        json.EndArray().EndObject();
    }
}

internal sealed class ParameterElementEntry
{
    public string Name { get; set; } = string.Empty;

    public int ElementId { get; set; }

    public string? SharedGuid { get; set; }

    public string Group { get; set; } = string.Empty;

    public string ParameterType { get; set; } = string.Empty;

    public bool Visible { get; set; }

    public bool IsBuiltInParameter { get; set; }

    public string? BuiltInParameter { get; set; }

    public bool VariesAcrossGroups { get; set; }

    public void Write(JsonBuilder json) => json.BeginObject()
        .Name("name").String(Name)
        .Name("elementId").Number(ElementId)
        .Name("sharedGuid").String(SharedGuid)
        .Name("group").String(Group)
        .Name("parameterType").String(ParameterType)
        .Name("visible").Bool(Visible)
        .Name("isBuiltInParameter").Bool(IsBuiltInParameter)
        .Name("builtInParameter").String(BuiltInParameter)
        .Name("variesAcrossGroups").Bool(VariesAcrossGroups)
        .EndObject();
}

internal sealed class ProjectInformationParameterEntry
{
    public string Name { get; set; } = string.Empty;

    public string? BuiltInParameter { get; set; }

    public string StorageType { get; set; } = string.Empty;

    public string? Value { get; set; }

    public void Write(JsonBuilder json) => json.BeginObject()
        .Name("name").String(Name)
        .Name("builtInParameter").String(BuiltInParameter)
        .Name("storageType").String(StorageType)
        .Name("value").String(Value)
        .EndObject();
}
