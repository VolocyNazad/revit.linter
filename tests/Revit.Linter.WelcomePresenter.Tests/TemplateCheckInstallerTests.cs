using Revit.Linter.ConfigurationPath;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DiagnosticConfiguration.Models;
using Revit.Linter.DiagnosticConfiguration.Validation;
using Revit.Linter.Testing;
using Revit.Linter.WelcomePresenter.Abstractions.Models;
using Revit.Linter.WelcomePresenter.Infrastructure.Services;

namespace Revit.Linter.WelcomePresenter.Tests;

public sealed class TemplateCheckInstallerTests : IDisposable
{
    // Each block holds the single project-parameter rule and one custom fill check per text
    // parameter of the ADSK template. The counts are the same in both group editions.
    public static TheoryData<TemplateCheckPack, string, int, int> CheckPacks => new()
    {
        { TemplateCheckPack.Architecture, "PRMTR401", 104, 81 },
        { TemplateCheckPack.WaterSupply, "PRMTR402", 65, 58 },
        { TemplateCheckPack.ReinforcedConcrete, "PRMTR403", 93, 72 },
        { TemplateCheckPack.SteelStructures, "PRMTR404", 93, 69 },
        { TemplateCheckPack.Hvac, "PRMTR405", 88, 68 },
        { TemplateCheckPack.LowCurrentSystems, "PRMTR406", 119, 97 },
        { TemplateCheckPack.Technology, "PRMTR407", 61, 46 },
        { TemplateCheckPack.Electrical, "PRMTR408", 108, 93 },
    };

    private const int ExampleCustomRuleCount = 6;

    private const string ForgeGroupPrefix = "autodesk.parameter.group:";

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        nameof(TemplateCheckInstallerTests),
        Guid.NewGuid().ToString("N"));

    public TemplateCheckInstallerTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory)) Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup of a test directory.
        }
    }

    private static string ReadExampleTemplate(string language, string fileName) => File.ReadAllText(
        Path.Combine(
            RepositoryRoot.Find(),
            "wiki",
            "examples",
            "configuration",
            language == "en" ? string.Empty : language,
            fileName));

    private static string ReadCheckBlock(string language, TemplateCheckPack pack, bool useLegacyParameterGroups) =>
        File.ReadAllText(
            Path.Combine(
                RepositoryRoot.Find(),
                "wiki",
                "examples",
                "template-checks",
                useLegacyParameterGroups ? language : Path.Combine("2024", language),
                TemplateCheckComposer.GetFileName(pack)));

    private static string GetFillCodePrefix(TemplateCheckPack pack) =>
        $"CSTM{TemplateCheckComposer.GetCodeNumber(pack)}";

    private static bool IsFillRule(DiagnosticRule rule, string prefix) =>
        rule.Type == "custom"
        && rule.Code is not null
        && rule.Code.StartsWith(prefix, StringComparison.Ordinal);

    private static DiagnosticConfigurationFile ReadInstalledFile(
        string directory,
        IReadOnlyCollection<TemplateCheckPack> packs,
        string language = "en",
        bool useLegacyParameterGroups = true)
    {
        ExampleInstallationResult result = ExampleConfigurationInstaller.Install(
            directory,
            [],
            packs,
            fileName => ReadExampleTemplate(language, fileName),
            pack => ReadCheckBlock(language, pack, useLegacyParameterGroups),
            useLegacyParameterGroups);

        InstalledExampleFile installed = Assert.Single(result.Files);
        return Assert.IsType<DiagnosticConfigurationFile>(
            ConfigurationPathUtils.GetDiagnosticConfiguration(installed.Path));
    }

    [Theory]
    [MemberData(nameof(CheckPacks))]
    public void Real_check_blocks_append_the_expected_parameter_and_fill_rules(
        TemplateCheckPack pack, string code, int parameterCount, int fillCount)
    {
        foreach (bool useLegacyParameterGroups in new[] { true, false })
        {
            string directory = Path.Combine(
                _tempDirectory, "packs", useLegacyParameterGroups ? "legacy" : "forge", code);
            DiagnosticConfigurationFile file = ReadInstalledFile(
                directory, [pack], useLegacyParameterGroups: useLegacyParameterGroups);

            Assert.Equal(2, file.Version);
            DiagnosticRule parameterRule = Assert.Single(file.Rules, candidate => candidate.Type == "parameter");
            Assert.Equal(code, parameterRule.Code);
            DiagnosticParameter[] declaredParameters = Assert.IsType<DiagnosticParameter[]>(parameterRule.Parameters);
            Assert.Equal(parameterCount, declaredParameters.Length);
            Assert.Empty(DiagnosticRuleValidator.ValidateIssues(parameterRule));
            Assert.All(declaredParameters, parameter =>
            {
                string group = parameter.Group ?? string.Empty;
                Assert.True(
                    useLegacyParameterGroups
                        ? group == "INVALID" || group.StartsWith("PG_", StringComparison.Ordinal)
                        : group.Length == 0 || group.StartsWith(ForgeGroupPrefix, StringComparison.Ordinal),
                    $"Unexpected parameter group '{group}'.");
            });

            string fillCodePrefix = GetFillCodePrefix(pack);
            int packNumber = TemplateCheckComposer.GetCodeNumber(pack);
            DiagnosticRule[] fillRules = file.Rules
                .Where(rule => IsFillRule(rule, fillCodePrefix))
                .ToArray();
            Assert.Equal(fillCount, fillRules.Length);
            Assert.Equal(ExampleCustomRuleCount + fillCount, file.Rules.Count(rule => rule.Type == "custom"));

            // The block keeps its project-parameter rule as the first list item, so every fill check
            // of the pack follows it.
            int parameterIndex = file.Rules.IndexOf(parameterRule);
            Assert.All(fillRules, fillRule => Assert.True(
                file.Rules.IndexOf(fillRule) > parameterIndex,
                $"The fill rule '{fillRule.Code}' must follow the parameter rule '{code}'."));

            foreach (DiagnosticRule fillRule in fillRules)
            {
                string fillCode = fillRule.Code!;
                Assert.Empty(DiagnosticRuleValidator.ValidateIssues(fillRule));
                Assert.Equal(DiagnosticSeverity.Message, fillRule.Severity);
                Assert.True(fillRule.IsActive is true);
                Assert.False(string.IsNullOrWhiteSpace(fillRule.TakeDocument));
                string take = fillRule.Take ?? string.Empty;
                string check = fillRule.Check ?? string.Empty;
                string message = fillRule.Message ?? string.Empty;
                Assert.False(string.IsNullOrWhiteSpace(take));
                Assert.False(string.IsNullOrWhiteSpace(check));
                Assert.True(fillRule.Parameters is null or { Length: 0 });
                Assert.Matches($"^CSTM{packNumber}\\d{{3}}$", fillCode);
                Assert.NotEmpty(fillRule.Tags);
                Assert.StartsWith("ADSK", fillRule.Tags[0]);
                Assert.True(
                    take.StartsWith("instance and ", StringComparison.Ordinal)
                    || take.StartsWith("type and ", StringComparison.Ordinal),
                    $"The fill rule '{fillCode}' must take instances or element types.");
                Assert.Contains("!isnullorempty(parameter(me, '", check);
                Assert.Contains("{elementName}", message);
                Assert.Contains("{elementId}", message);
            }

            Assert.Equal(fillCount, fillRules.Select(rule => rule.Code).Distinct(StringComparer.Ordinal).Count());

            // Every pack binds at least one text parameter to element types, not to instances.
            Assert.Contains(fillRules, fillRule =>
                (fillRule.Take ?? string.Empty).StartsWith("type and ", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Both_editions_describe_the_same_parameters_with_the_matching_groups()
    {
        foreach (TemplateCheckPack pack in Enum.GetValues(typeof(TemplateCheckPack)).Cast<TemplateCheckPack>())
        {
            DiagnosticRule legacy = Assert.Single(
                ReadInstalledFile(Path.Combine(_tempDirectory, "same", "legacy", pack.ToString()), [pack])
                    .Rules,
                rule => rule.Type == "parameter");
            DiagnosticRule forge = Assert.Single(
                ReadInstalledFile(
                    Path.Combine(_tempDirectory, "same", "forge", pack.ToString()),
                    [pack],
                    useLegacyParameterGroups: false).Rules,
                rule => rule.Type == "parameter");

            Assert.Equal(legacy.Code, forge.Code);
            Assert.Equal(legacy.Description, forge.Description);
            DiagnosticParameter[] legacyParameters = Assert.IsType<DiagnosticParameter[]>(legacy.Parameters);
            DiagnosticParameter[] forgeParameters = Assert.IsType<DiagnosticParameter[]>(forge.Parameters);
            Assert.Equal(legacyParameters.Length, forgeParameters.Length);

            for (int index = 0; index < legacyParameters.Length; index++)
            {
                DiagnosticParameter legacyParameter = legacyParameters[index];
                DiagnosticParameter forgeParameter = forgeParameters[index];

                Assert.Equal(legacyParameter.Name, forgeParameter.Name);
                Assert.Equal(legacyParameter.Guid, forgeParameter.Guid);
                Assert.Equal(legacyParameter.IsInstance, forgeParameter.IsInstance);
                Assert.Equal(legacyParameter.AllowVaryBetweenGroups, forgeParameter.AllowVaryBetweenGroups);
                Assert.Equal(legacyParameter.Categories, forgeParameter.Categories);
                if (legacyParameter.Group == "INVALID")
                {
                    Assert.Equal(string.Empty, forgeParameter.Group);
                }
                else
                {
                    Assert.NotNull(forgeParameter.Group);
                    Assert.Equal(
                        ForgeGroupPrefix,
                        forgeParameter.Group.Substring(0, ForgeGroupPrefix.Length));
                }
            }

            int invalidGroups = legacyParameters.Count(parameter => parameter.Group == "INVALID");
            Assert.Equal(
                invalidGroups,
                forgeParameters.Count(parameter => parameter.Group == string.Empty));

            // PG_GEOMETRY is the one group whose modern name differs from the enum suffix.
            for (int index = 0; index < legacyParameters.Length; index++)
            {
                DiagnosticParameter legacyParameter = legacyParameters[index];
                DiagnosticParameter forgeParameter = forgeParameters[index];
                if (legacyParameter.Group == "PG_GEOMETRY")
                    Assert.Equal($"{ForgeGroupPrefix}dimensions-1.0.0", forgeParameter.Group);
                else if (legacyParameter.Group == "PG_DATA")
                    Assert.Equal($"{ForgeGroupPrefix}data-1.0.0", forgeParameter.Group);
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Fill_rule_codes_are_unique_in_the_installed_file_and_across_all_blocks(
        bool useLegacyParameterGroups)
    {
        TemplateCheckPack[] packs = Enum.GetValues(typeof(TemplateCheckPack))
            .Cast<TemplateCheckPack>()
            .ToArray();

        DiagnosticConfigurationFile combined = ReadInstalledFile(
            Path.Combine(_tempDirectory, "unique-all", useLegacyParameterGroups.ToString()), packs,
            useLegacyParameterGroups: useLegacyParameterGroups);
        string[] combinedFillCodes = combined.Rules
            .Where(rule => IsFillRule(rule, "CSTM4"))
            .Select(rule => rule.Code!)
            .ToArray();
        Assert.Equal(584, combinedFillCodes.Length);
        Assert.Equal(combinedFillCodes.Length, combinedFillCodes.Distinct(StringComparer.Ordinal).Count());

        HashSet<string> blockCodes = new(StringComparer.Ordinal);
        foreach (TemplateCheckPack pack in packs)
        {
            string prefix = GetFillCodePrefix(pack);
            DiagnosticConfigurationFile blockFile = ReadInstalledFile(
                Path.Combine(_tempDirectory, "unique", useLegacyParameterGroups.ToString(), prefix),
                [pack],
                useLegacyParameterGroups: useLegacyParameterGroups);
            string[] codes = blockFile.Rules
                .Where(rule => IsFillRule(rule, prefix))
                .Select(rule => rule.Code!)
                .ToArray();

            Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
            Assert.All(codes, fillCode => Assert.True(
                blockCodes.Add(fillCode),
                $"The fill-rule code '{fillCode}' is used by more than one block."));
        }

        Assert.Equal(combinedFillCodes.Length, blockCodes.Count);
        Assert.Equal(
            combinedFillCodes.OrderBy(fillCode => fillCode, StringComparer.Ordinal).ToArray(),
            blockCodes.OrderBy(fillCode => fillCode, StringComparer.Ordinal).ToArray());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Fill_rules_are_localized_with_identical_codes(bool useLegacyParameterGroups)
    {
        TemplateCheckPack pack = TemplateCheckPack.Architecture;
        string prefix = GetFillCodePrefix(pack);

        DiagnosticConfigurationFile english = ReadInstalledFile(
            Path.Combine(_tempDirectory, "localized-en", useLegacyParameterGroups.ToString()), [pack],
            useLegacyParameterGroups: useLegacyParameterGroups);
        DiagnosticConfigurationFile russian = ReadInstalledFile(
            Path.Combine(_tempDirectory, "localized-ru", useLegacyParameterGroups.ToString()), [pack], "ru",
            useLegacyParameterGroups);

        Dictionary<string, DiagnosticRule> englishFills = english.Rules
            .Where(rule => IsFillRule(rule, prefix))
            .ToDictionary(rule => rule.Code!);
        Dictionary<string, DiagnosticRule> russianFills = russian.Rules
            .Where(rule => IsFillRule(rule, prefix))
            .ToDictionary(rule => rule.Code!);

        Assert.NotEmpty(englishFills);
        Assert.Equal(
            englishFills.Keys.OrderBy(fillCode => fillCode, StringComparer.Ordinal).ToArray(),
            russianFills.Keys.OrderBy(fillCode => fillCode, StringComparer.Ordinal).ToArray());

        foreach (string fillCode in englishFills.Keys)
        {
            Assert.NotEqual(englishFills[fillCode].Description, russianFills[fillCode].Description);
            Assert.NotEqual(englishFills[fillCode].Message, russianFills[fillCode].Message);
        }
    }

    [Fact]
    public void Install_appends_every_selected_pack_in_code_order()
    {
        string directory = Path.Combine(_tempDirectory, "order");

        ExampleInstallationResult result = ExampleConfigurationInstaller.Install(
            directory,
            [],
            [TemplateCheckPack.Electrical, TemplateCheckPack.Architecture],
            fileName => ReadExampleTemplate("en", fileName),
            pack => ReadCheckBlock("en", pack, useLegacyParameterGroups: true),
            useLegacyParameterGroups: true);

        DiagnosticConfigurationFile file = Assert.IsType<DiagnosticConfigurationFile>(
            ConfigurationPathUtils.GetDiagnosticConfiguration(result.Files[0].Path));
        string[] codes = file.Rules
            .Where(rule => rule.Type == "parameter")
            .Select(rule => rule.Code!)
            .ToArray();
        Assert.Equal(["PRMTR401", "PRMTR408"], codes);

        string[] fillPrefixes = file.Rules
            .Where(rule => IsFillRule(rule, "CSTM4"))
            .Select(rule => rule.Code!.Substring(0, 7))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["CSTM401", "CSTM408"], fillPrefixes);
    }

    [Fact]
    public void Install_with_only_check_packs_keeps_the_element_checks()
    {
        string directory = Path.Combine(_tempDirectory, "checks-only");

        ExampleInstallationResult result = ExampleConfigurationInstaller.Install(
            directory,
            [],
            [TemplateCheckPack.Architecture],
            fileName => ReadExampleTemplate("en", fileName),
            pack => ReadCheckBlock("en", pack, useLegacyParameterGroups: true),
            useLegacyParameterGroups: true);

        DiagnosticConfigurationFile file = Assert.IsType<DiagnosticConfigurationFile>(
            ConfigurationPathUtils.GetDiagnosticConfiguration(result.Files[0].Path));
        Assert.Equal(ExampleCustomRuleCount + 81, file.Rules.Count(rule => rule.Type == "custom"));
        Assert.Single(file.Rules, rule => rule.Type == "parameter");
        Assert.DoesNotContain(file.Rules, rule => rule.Type is "collision" or "builtIn");
    }

    [Fact]
    public void Install_supports_check_packs_for_revit_2024_and_newer()
    {
        string directory = Path.Combine(_tempDirectory, "forge-edition");

        ExampleInstallationResult result = ExampleConfigurationInstaller.Install(
            directory,
            [ExampleDiscipline.Mep],
            [TemplateCheckPack.Architecture],
            fileName => ReadExampleTemplate("en", fileName),
            pack => ReadCheckBlock("en", pack, useLegacyParameterGroups: false),
            useLegacyParameterGroups: false);

        DiagnosticConfigurationFile file = Assert.IsType<DiagnosticConfigurationFile>(
            ConfigurationPathUtils.GetDiagnosticConfiguration(result.Files[0].Path));
        DiagnosticRule checkRule = Assert.Single(
            file.Rules, rule => rule.Type == "parameter" && rule.Code == "PRMTR401");
        DiagnosticParameter[] declaredParameters = Assert.IsType<DiagnosticParameter[]>(checkRule.Parameters);
        Assert.Equal(104, declaredParameters.Length);
        Assert.All(declaredParameters, parameter =>
        {
            string group = parameter.Group ?? string.Empty;
            Assert.True(
                group.Length == 0 || group.StartsWith(ForgeGroupPrefix, StringComparison.Ordinal),
                $"Unexpected parameter group '{group}'.");
        });

        // The example project-parameter rules keep their ForgeTypeId groups on Revit 2024 and newer.
        Assert.Contains(
            file.Rules,
            rule =>
            {
                string code = rule.Code ?? string.Empty;
                return rule.Type == "parameter"
                       && code.StartsWith("PRMTR1", StringComparison.Ordinal)
                       && (rule.Parameters ?? []).All(parameter =>
                       {
                           string group = parameter.Group ?? string.Empty;
                           return group.Length == 0
                                  || group.StartsWith(ForgeGroupPrefix, StringComparison.Ordinal);
                       });
            });
    }

    [Fact]
    public void Install_without_packs_produces_the_plain_example()
    {
        string withPacksDirectory = Path.Combine(_tempDirectory, "plain-a");
        string plainDirectory = Path.Combine(_tempDirectory, "plain-b");
        ExampleInstallationResult plain = ExampleConfigurationInstaller.Install(
            plainDirectory,
            [ExampleDiscipline.Mep],
            fileName => ReadExampleTemplate("en", fileName),
            useLegacyParameterGroups: false);
        ExampleInstallationResult withEmptyPacks = ExampleConfigurationInstaller.Install(
            withPacksDirectory,
            [ExampleDiscipline.Mep],
            [],
            fileName => ReadExampleTemplate("en", fileName),
            readCheckBlock: null,
            useLegacyParameterGroups: false);

        Assert.Equal(
            File.ReadAllText(plain.Files[0].Path),
            File.ReadAllText(withEmptyPacks.Files[0].Path));
    }

    [Fact]
    public void Install_rejects_an_empty_selection()
    {
        Assert.Throws<ArgumentException>(() => ExampleConfigurationInstaller.Install(
            _tempDirectory, [], [], _ => string.Empty, _ => string.Empty, useLegacyParameterGroups: true));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("ru")]
    public void Every_check_block_is_embedded_per_language(string language)
    {
        string[] resources = typeof(ExampleConfigurationInstaller).Assembly.GetManifestResourceNames();
        foreach (TemplateCheckPack pack in Enum.GetValues(typeof(TemplateCheckPack)).Cast<TemplateCheckPack>())
        {
            string fileName = TemplateCheckComposer.GetFileName(pack);
            Assert.Contains($"Revit.Linter.WelcomePresenter.TemplateChecks.{language}.{fileName}", resources);
            Assert.Contains($"Revit.Linter.WelcomePresenter.TemplateChecks2024.{language}.{fileName}", resources);
        }
    }

    [Fact]
    public void Check_blocks_use_the_group_spelling_of_their_edition()
    {
        foreach (TemplateCheckPack pack in Enum.GetValues(typeof(TemplateCheckPack)).Cast<TemplateCheckPack>())
        {
            Assert.DoesNotContain(
                ForgeGroupPrefix,
                ReadCheckBlock("en", pack, useLegacyParameterGroups: true),
                StringComparison.Ordinal);

            string forgeEdition = ReadCheckBlock("en", pack, useLegacyParameterGroups: false);
            Assert.DoesNotContain("PG_", forgeEdition, StringComparison.Ordinal);
            Assert.Contains(ForgeGroupPrefix, forgeEdition, StringComparison.Ordinal);
        }
    }
}
