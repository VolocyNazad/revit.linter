using Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalog;

namespace Revit.Linter.TypeCatalogDiagnostics.Tests;

public sealed class FamilyTypeCatalogFilePathsTests
{
    [Theory]
    [InlineData("Door.rfa", true)]
    [InlineData("Door.RFA", true)]
    [InlineData("Door.rvt", false)]
    [InlineData("Door.txt", false)]
    [InlineData("Door", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsSavedFamilyFile_reads_the_file_extension(string? path, bool expected)
    {
        Assert.Equal(expected, FamilyTypeCatalogReader.IsSavedFamilyFile(path));
    }

    [Fact]
    public void CatalogPathFor_replaces_the_family_extension()
    {
        string familyPath = Path.Combine("models", "families", "Door.rfa");

        string catalogPath = FamilyTypeCatalogReader.CatalogPathFor(familyPath);

        Assert.Equal(Path.Combine("models", "families", "Door.txt"), catalogPath);
    }

    [Fact]
    public void IsCatalogOf_accepts_the_catalog_next_to_its_family()
    {
        string familyPath = Path.Combine("models", "families", "Door.rfa");
        string catalogPath = Path.Combine("models", "families", "Door.txt");

        Assert.True(FamilyTypeCatalogReader.IsCatalogOf(familyPath, catalogPath));
    }

    [Fact]
    public void IsCatalogOf_ignores_the_letter_case_of_both_paths()
    {
        string familyPath = Path.Combine("Models", "Families", "DOOR.RFA");
        string catalogPath = Path.Combine("models", "families", "door.txt");

        Assert.True(FamilyTypeCatalogReader.IsCatalogOf(familyPath, catalogPath));
    }

    [Fact]
    public void IsCatalogOf_rejects_a_catalog_of_another_family()
    {
        string familyPath = Path.Combine("models", "families", "Door.rfa");
        string catalogPath = Path.Combine("models", "families", "Window.txt");

        Assert.False(FamilyTypeCatalogReader.IsCatalogOf(familyPath, catalogPath));
    }

    [Fact]
    public void IsCatalogOf_rejects_a_catalog_from_another_folder()
    {
        string familyPath = Path.Combine("models", "families", "Door.rfa");
        string catalogPath = Path.Combine("models", "archive", "Door.txt");

        Assert.False(FamilyTypeCatalogReader.IsCatalogOf(familyPath, catalogPath));
    }

    [Fact]
    public void IsCatalogOf_rejects_a_project_that_shares_the_catalog_name()
    {
        string familyPath = Path.Combine("models", "families", "Door.rvt");
        string catalogPath = Path.Combine("models", "families", "Door.txt");

        Assert.False(FamilyTypeCatalogReader.IsCatalogOf(familyPath, catalogPath));
    }

    [Fact]
    public void IsCatalogOf_rejects_an_unsaved_family()
    {
        string catalogPath = Path.Combine("models", "families", "Door.txt");

        Assert.False(FamilyTypeCatalogReader.IsCatalogOf(string.Empty, catalogPath));
        Assert.False(FamilyTypeCatalogReader.IsCatalogOf(null, catalogPath));
    }
}
