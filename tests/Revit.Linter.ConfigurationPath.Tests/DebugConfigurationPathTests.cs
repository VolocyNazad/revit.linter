namespace Revit.Linter.ConfigurationPath.Tests;

public sealed class DebugConfigurationPathTests
{
    [Fact]
    public void DebugFolderName_is_debug()
    {
        Assert.Equal("debug", ConfigurationPathUtils.DebugFolderName);
    }

    [Fact]
    public void Debug_path_uses_the_single_configuration_file_in_the_debug_folder()
    {
        Assert.Equal(
            Path.Combine(ConfigurationPathUtils.Directory, ConfigurationPathUtils.DebugFolderName,
                ConfigurationPathUtils.DiagnosticsConfigFileName),
            ConfigurationPathUtils.DebugConfigPath);
    }

    [Fact]
    public void Example_path_uses_the_single_configuration_file_in_the_examples_folder()
    {
        Assert.Equal(
            Path.Combine(ConfigurationPathUtils.Directory, ConfigurationPathUtils.ExamplesFolderName,
                ConfigurationPathUtils.DiagnosticsConfigFileName),
            ConfigurationPathUtils.ExampleConfigPath);
    }

    [Fact]
    public void Debug_and_example_paths_differ_from_the_user_path_and_each_other()
    {
        Assert.NotEqual(ConfigurationPathUtils.ConfigPath, ConfigurationPathUtils.DebugConfigPath);
        Assert.NotEqual(ConfigurationPathUtils.ConfigPath, ConfigurationPathUtils.ExampleConfigPath);
        Assert.NotEqual(ConfigurationPathUtils.DebugConfigPath, ConfigurationPathUtils.ExampleConfigPath);
    }

    [Fact]
    public void Tour_path_stays_in_the_managed_tour_folder()
    {
        Assert.Equal(
            Path.Combine(ConfigurationPathUtils.Directory, ConfigurationPathUtils.TourFolderName,
                ConfigurationPathUtils.PracticalTourFileName),
            ConfigurationPathUtils.PracticalTourConfigPath);
    }
}
