using Microsoft.Extensions.DependencyInjection;
using System.Windows.Markup;

namespace Revit.Linter.Infrastructure.MarkupExtensions;

public class TypeFromDIExtension(Type type) : MarkupExtension
{
    public Type Type { get; set; } = type;

    public override object ProvideValue(IServiceProvider serviceProvider) => Program.Provider.GetRequiredService(Type);
}
