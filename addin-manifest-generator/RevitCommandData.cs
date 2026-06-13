namespace AddinManifestGenerator
{
	internal class RevitManifestData
	{
		public Guid AddInId { get; } = Guid.NewGuid();
		public required string Name { get; init; }
		public required string Assembly { get; init; }
		public required string FullClassName { get; init; }
	}


	internal sealed class RevitCommandData : RevitManifestData
	{
		public string? Text { get; init; }
		public string? VisibilityMode { get; init; }
	}

	internal sealed class RevitApplicationData : RevitManifestData
	{
	}
}