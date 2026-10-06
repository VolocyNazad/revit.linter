namespace Revit.Linter.WelcomePresenter.DesignData;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "CodeQuality",
    "S2325",
    Justification = "The XAML designer requires instance properties for design-time bindings.")]
internal sealed class WelcomeDesignData
{
    public string WindowTitle => "Начало работы";
    public string Progress => "Шаг 2 из 5";
    public string CloseButtonText => "Закрыть";
    public string BackButtonText => "Назад";
    public string NextButtonText => "Далее";
    public bool HasNextStep => true;
    public bool IsLastStep => false;
    public WelcomeRulesStepDesignData CurrentStep { get; } = new();
    public IReadOnlyList<WelcomeStepDesignData> Steps { get; } =
    [
        new(1, "Знакомство", false),
        new(2, "Проверки", true),
        new(3, "Примеры", false),
        new(4, "Обучение", false),
        new(5, "Готово", false),
    ];
}

internal sealed record WelcomeStepDesignData(int Number, string Title, bool IsCurrent);

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "CodeQuality",
    "S2325",
    Justification = "The XAML designer requires instance properties for design-time bindings.")]
internal sealed class WelcomeRulesStepDesignData
{
    public string Caption => "Готовые проверки или собственные правила.";
    public string BuiltInTitleText => "Встроенные проверки";
    public string BuiltInDescriptionText => "Работают без настройки и обновляются вместе с Revit Linter.";
    public string UserTitleText => "Пользовательские правила";
    public string UserDescriptionText => "YAML-файлы, адаптируемые под проект или BIM-стандарт компании.";
    public string VisualizerTitleText => "Примеры файлов конфигурации YAML";
    public string ElementPreviewTitleText => "config.yaml — проверка элементов";
    public string RulePreviewText => "- code: \"CSTM201\"\n  description: \"Двери без марки\"\n  message: \"У двери '{elementName}' ({elementId}) не заполнена марка.\"\n  severity: \"Warning\"\n  takeDocument: \"!property('IsFamilyDocument')\"\n  take: \"instance and builtincategory('OST_Doors')\"\n  check: \"!isnullorempty(parameter(me, 'ALL_MODEL_MARK'))\"";
    public string CollisionPreviewTitleText => "collision.config.yaml — проверка коллизий";
    public string CollisionRulePreviewText => "- code: \"CLSN101\"\n  description: \"Воздуховоды против труб\"\n  message: \"'{elementName}' пересекает элементов: {intersection.count}.\"\n  severity: \"Error\"\n  takeDocument: \"!property('IsFamilyDocument')\"\n  take: \"instance and builtincategory('OST_DuctCurves')\"\n  andTake: \"instance and builtincategory('OST_PipeCurves')\"\n  groupBy: \"'all'\"";
    public string ProjectParameterPreviewTitleText => "parameter-element.config.yaml — проверка параметров проекта";
    public string ProjectParameterRulePreviewText => "- code: \"PRMTR201\"\n  description: \"Параметр номера квартиры\"\n  message: \"Параметры проекта настроены неверно: {details}\"\n  severity: \"Message\"\n  take: \"!property('IsFamilyDocument')\"\n  parameters:\n    - guid: \"10fb72de-237e-4b9c-915b-8849b8907695\"\n      name: \"ADSK_Номер квартиры\"\n      group: \"autodesk.parameter.group:data-1.0.0\"\n      isInstance: true\n      categories: [\"OST_Rooms\"]\n      allowVaryBetweenGroups: false";
    public string CodeExplanationText => "code — постоянный уникальный код";
    public string DescriptionExplanationText => "description — понятное название";
    public string SeverityExplanationText => "severity — сообщение, предупреждение или ошибка";
    public string TakeExplanationText => "take — какие элементы Revit проверять";
    public string CheckExplanationText => "check — условие корректного элемента";
    public string AndTakeExplanationText => "andTake — с какими элементами искать пересечения";
    public string GroupByExplanationText => "groupBy — как кандидаты объединяются для поиска";
    public string IntersectionExplanationText => "intersection — данные о пересекающихся элементах";
    public string CollisionTakeExplanationText => "take — какие элементы проверять на коллизии";
    public string ParametersExplanationText => "parameters — обязательные параметры проекта";
    public string GuidExplanationText => "guid — идентификатор общего параметра";
    public string GroupExplanationText => "group — группа параметра в Revit";
    public string CategoriesExplanationText => "categories — категории привязки параметра";
    public string IsInstanceExplanationText => "isInstance — параметр экземпляра или типа";
    public string AllowVaryExplanationText => "allowVaryBetweenGroups — могут ли значения различаться в группах";
}

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "CodeQuality",
    "S2325",
    Justification = "The XAML designer requires instance properties for design-time bindings.")]
internal sealed class PracticalTourDesignData
{
    public string TitleText => "Практическое обучение";
    public IReadOnlyList<PracticalTourStepDesignData> Steps { get; } =
    [
        new("Открыть модель", string.Empty, false, true, false),
        new("Найти конфигурации", "На вкладке «Диагностика» нажмите «Открыть папку». Здесь находятся YAML-файлы собственных правил.", true, false, false),
        new("Поиск и фильтры", string.Empty, false, false, false),
        new("Выбрать проверки", string.Empty, false, false, false),
        new("Запустить проверки", string.Empty, false, false, false),
        new("Изучить замечание", string.Empty, false, false, false),
        new("Визуализировать", string.Empty, false, false, false),
        new("Визуализировать иначе", string.Empty, false, false, false),
        new("Листать замечания", string.Empty, false, false, false),
        new("Выполнить исправление", string.Empty, false, false, false),
        new("Панель исправлений", string.Empty, false, false, false),
        new("Экспортировать отчёт", string.Empty, false, false, false),
        new("Экспорт в другом формате", string.Empty, false, false, false),
    ];
    public string OptOutText => "Больше не показывать";
    public string StopText => "Продолжить позже";
}

internal sealed record PracticalTourStepDesignData(
    string Title,
    string Instruction,
    bool IsCurrent,
    bool IsCompleted,
    bool IsUnavailable);
