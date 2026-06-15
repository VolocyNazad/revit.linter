using AddinManifestGenerator;
using System.Xml.Linq;

var solutionPath = TryGetSolutionDirectoryPath();
var config = "Debug 2021.1.9";
var framework = "net48";
var platform = "x64";

string TryGetSolutionDirectoryPath(string? currentPath = null)
{
	DirectoryInfo? directory = new(currentPath ?? Directory.GetCurrentDirectory());
	while (directory != null && directory.GetFiles("*.slnx").Length == 0)
		directory = directory.Parent;
	if (directory is null) throw new InvalidOperationException("Solution directory not found");
	return directory.FullName;
}

List<RevitApplicationData> apps = [
		new() {
			Name = "Revit_Linter",
			Assembly = "Revit.Linter.dll",
			FullClassName = "Revit.Linter.InitExternalApplication",
		},
];
List<RevitCommandData> commands = [
	];

Console.WriteLine("Введите путь для сохранения .addin файла (или нажмите Enter для текущей папки):");
var savePath = Console.ReadLine();

if (string.IsNullOrWhiteSpace(savePath)) savePath = Directory.GetCurrentDirectory();

if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

XDocument doc = GenerateAddInFile(apps, commands);

var fileName = Path.Combine(savePath, "Revit.Linter.addin");
doc.Save(fileName);

Console.WriteLine("\nНажмите любую клавишу для выхода...");
Console.ReadKey();

XDocument GenerateAddInFile(List<RevitApplicationData> apps, List<RevitCommandData> commands)
{
	XElement root = new("RevitAddIns");

	foreach (RevitApplicationData app in apps) {
		var appElement = new XElement("AddIn");
		appElement.SetAttributeValue("Type", "Application");

		if (!string.IsNullOrEmpty(app.Name))
			appElement.Add(new XElement("Name", app.Name));

		var assemblyPath = Path.Combine(solutionPath, "src", Path.GetFileNameWithoutExtension(app.Assembly), "bin", platform, config, framework, app.Assembly);
		appElement.Add(new XElement("Assembly", assemblyPath));

		appElement.Add(new XElement("AddInId", app.AddInId));

		if (!string.IsNullOrEmpty(app.FullClassName))
			appElement.Add(new XElement("FullClassName", app.FullClassName));

		appElement.Add(new XElement("VendorId", "VolocyNazad"));
		appElement.Add(new XElement("VendorDescription", "VolocyNazad"));

		root.Add(appElement);
	}

	foreach (RevitCommandData command in commands) {
		var commandElement = new XElement("AddIn");
		commandElement.SetAttributeValue("Type", "Command");

		if (!string.IsNullOrEmpty(command.Name))
			commandElement.Add(new XElement("Name", command.Name));

		var assemblyPath = Path.Combine(solutionPath, Path.GetFileNameWithoutExtension(command.Assembly), "bin", config, framework, command.Assembly);
		commandElement.Add(new XElement("Assembly", assemblyPath));

		commandElement.Add(new XElement("AddInId", command.AddInId));

		if (!string.IsNullOrEmpty(command.FullClassName))
			commandElement.Add(new XElement("FullClassName", command.FullClassName));

        commandElement.Add(new XElement("VendorId", "VolocyNazad"));
        commandElement.Add(new XElement("VendorDescription", "VolocyNazad"));

        if (!string.IsNullOrEmpty(command.Text))
			commandElement.Add(new XElement("Text", command.Text));

		if (!string.IsNullOrEmpty(command.VisibilityMode))
			commandElement.Add(new XElement("VisibilityMode", command.VisibilityMode));

		root.Add(commandElement);
	}

	var declaration = new XDeclaration("1.0", "utf-8", null);
	var doc = new XDocument(declaration, root);
	return doc;
}