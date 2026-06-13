using Revit.Linter.Core.Abstractions.Models;
using System.Reflection;

namespace Revit.Linter.ElementDiagnostics;

internal static class ElementDiagnosticIdCollector
{
    public static ElementDiagnosticId AnyConnectorsNotConnected = new(
        "SYST001",
        "Проверка экземпляров труб, воздуховодов, коробов, лотков, пользовательских семейств на отсутствие у него неприсоединенных коннекторов.",
        "Элемент с именем '{elementName}' и идентификатором '{elementId}' имеет неприсоединенные коннекторы. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Message,
        true,
        false,
        string.Empty);
    public static ElementDiagnosticId FamilyUnused = new(
        "SHRD001",
        "Проверка семейств на их использование в документе.",
        "Семейство с именем '{elementName}' и идентификатором '{elementId}' не используется. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Message,
        true,
        false,
        string.Empty);
    public static ElementDiagnosticId FamilySymbolUnused = new(
        "SHRD002",
        "Проверка типоразмеров семейств на их использование в документе.",
        "Типоразмер семейства с именем '{elementName}' и идентификатором '{elementId}' не используется. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Message,
        true,
        false,
        string.Empty);
    public static ElementDiagnosticId FamilyInstanceMirrored = new(
        "SHRD003",
        "Проверка экземпляров семейств на отсутствие отзеркаливания.",
        "Экземпляр семейства с именем '{elementName}' и идентификатором '{elementId}' отзеркален. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Warning,
        true,
        false,
        string.Empty);
    public static ElementDiagnosticId ViewUnplaced = new(
        "SHRD004",
        "Проверка видов, размещены ли они на листах.",
        "Вид с именем '{elementName}' и идентификатором '{elementId}' не резмещен на листе. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Warning,
        true,
        false,
        string.Empty);
    public static ElementDiagnosticId WallTopOffsetUnconnected = new(
        "SHRD005",
        "Проверка стены, задана ли привязка верха.",
        "Стена с именем '{elementName}' и идентификатором '{elementId}' не имеет привяки сверху. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Warning,
        true,
        false,
        string.Empty);
    public static ElementDiagnosticId LocationLineElementWithTolerantCoordinates = new(
        "SHRD006",
        "Проверка элементов на основе линии на толерантность координат размещения.",
        "Элемент на основе линии с именем '{elementName}' и идентификатором '{elementId}' имеет не валидные значения координат относительно базовой точки. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Warning,
        true,
        false,
        string.Empty);
    public static ElementDiagnosticId LocationLineElementWithTolerantLength = new(
        "SHRD007",
        "Проверка элементов на основе линии на толерантность значения длины.",
        "Элемент на основе линии с именем '{elementName}' и идентификатором '{elementId}' имеет не валидную длину. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Warning,
        true,
        false,
        string.Empty);
    public static ElementDiagnosticId LevelHeightIsTolerant = new(
        "SHRD008",
        "Проверка уровней на толерантность значения высоты.",
        "Уровень с именем '{elementName}' и идентификатором '{elementId}' имеет не валидную высоту. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Warning,
        true,
        false,
        string.Empty);
#if AFTER2023
    public static ElementDiagnosticId FloorWithTolerantScetchCoordinates = new(
        "SHRD009",
        "Проверка перекрытий на толерантность координат размещения.",
        "Перекрытие с именем '{elementName}' и идентификатором '{elementId}' имеет не валидные значения координат относительно базовой точки. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Warning,
        true,
        false,
        string.Empty);
#endif
    public static ElementDiagnosticId FamilyInstanceElevationIsTolerant = new(
        "SHRD0010",
        "Проверка экземпляров семейств на толерантность высоты размещения.",
        "Экземпляр семейств с именем '{elementName}' и идентификатором '{elementId}' имеет не валидную высоту размещения. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Warning,
        true,
        false,
        string.Empty);
    public static ElementDiagnosticId ParameterElementUnused = new(
        "SHRD0011",
        "Проверка исппользуемости элементов-параметров.",
        "Элемент с именем '{elementName}' и идентификатором '{elementId}' не используется. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Warning,
        true,
        false,
        string.Empty);
    public static ElementDiagnosticId FamilyInstanceLevelIsNearest = new(
        "SHRD0012",
        "Проверка привязки экземпляра семейства к ближайшему уровню.",
        "Экземпляр семейства с именем '{elementName}' и идентификатором '{elementId}' привязан не к ближайшему уровню. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Message,
        true,
        false,
        string.Empty);
    public static ElementDiagnosticId RoomUnplaced = new(
        "ARCH001",
        "Проверка помещения, размещено ли оно.",
        "Помещение с именем '{elementName}' и идентификатором '{elementId}' не резмещено. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Error,
        true,
        false,
        string.Empty);
    public static ElementDiagnosticId RoomNotEnclosed = new(
        "ARCH002",
        "Проверка помещения, окружено ли оно.",
        "Помещение с именем '{elementName}' и идентификатором '{elementId}' не окружено. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Error,
        true,
        false,
        string.Empty);
    public static ElementDiagnosticId RoomIsRedundant = new(
        "ARCH003",
        "Проверка помещения, избыточно ли оно.",
        "Помещение с именем '{elementName}' и идентификатором '{elementId}' избыточно. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Error,
        true,
        false,
        string.Empty);
    public static ElementDiagnosticId WallHeightIsTolerant = new(
        "ARCH004",
        "Проверка стен на толерантность значения высоты.",
        "Стена с именем '{elementName}' и идентификатором '{elementId}' имеет не валидную высоту. Время выполнения '{duration}' мс.",
        DiagnosticSeverity.Warning,
        true,
        false,
        string.Empty);

    private static readonly Lazy<IReadOnlyList<ElementDiagnosticId>> _allDiagnosticIds =
        new(typeof(ElementDiagnosticIdCollector)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(ElementDiagnosticId))
        .Select(field => (ElementDiagnosticId)field.GetValue(null)!).Where(i => i != null)
        .ToList);
    internal static IReadOnlyList<ElementDiagnosticId> GetAllDiagnosticIds() => _allDiagnosticIds.Value;
}
