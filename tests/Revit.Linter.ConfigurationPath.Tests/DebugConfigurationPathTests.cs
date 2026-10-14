namespace Revit.Linter.ConfigurationPath.Tests;

public sealed class DebugConfigurationPathTests
{
    [Fact]
    public void DebugFolderName_is_debug()
    {
        Assert.Equal("debug", ConfigurationPathUtils.DebugFolderName);
    }

    [Fact]
    public void DebugConfigPath_lives_in_debug_folder()
    {
        Assert.Equal(
            Path.Combine(
                ConfigurationPathUtils.Directory,
                ConfigurationPathUtils.DebugFolderName,
                ConfigurationPathUtils.ConfigFileName),
            ConfigurationPathUtils.DebugConfigPath);
    }

    [Fact]
    public void DebugCollisionConfigPath_lives_in_debug_folder()
    {
        Assert.Equal(
            Path.Combine(
                ConfigurationPathUtils.Directory,
                ConfigurationPathUtils.DebugFolderName,
                ConfigurationPathUtils.CollisionConfigFileName),
            ConfigurationPathUtils.DebugCollisionConfigPath);
    }

    [Fact]
    public void DebugParameterElementConfigPath_lives_in_debug_folder()
    {
        Assert.Equal(
            Path.Combine(
                ConfigurationPathUtils.Directory,
                ConfigurationPathUtils.DebugFolderName,
                ConfigurationPathUtils.ParameterElementConfigFileName),
            ConfigurationPathUtils.DebugParameterElementConfigPath);
    }

    [Fact]
    public void Debug_folder_differs_from_examples_and_tour_folders()
    {
        Assert.NotEqual(ConfigurationPathUtils.DebugFolderName, ConfigurationPathUtils.ExamplesFolderName);
        Assert.NotEqual(ConfigurationPathUtils.DebugFolderName, ConfigurationPathUtils.TourFolderName);
    }

    [Fact]
    public void Debug_paths_differ_from_example_paths()
    {
        Assert.NotEqual(ConfigurationPathUtils.DebugConfigPath, ConfigurationPathUtils.ExampleConfigPath);
        Assert.NotEqual(
            ConfigurationPathUtils.DebugCollisionConfigPath,
            ConfigurationPathUtils.ExampleCollisionConfigPath);
        Assert.NotEqual(
            ConfigurationPathUtils.DebugParameterElementConfigPath,
            ConfigurationPathUtils.ExampleParameterElementConfigPath);
    }
}
