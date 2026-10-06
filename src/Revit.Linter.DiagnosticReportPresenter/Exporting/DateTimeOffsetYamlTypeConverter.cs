using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Revit.Linter.DiagnosticReportPresenter.Exporting;

/// <summary>
/// Writes <see cref="DateTimeOffset"/> values as a mapping with the same keys YamlDotNet emits by default.
/// </summary>
/// <remarks>
/// The only deliberate difference from the default rendering is <c>localDateTime</c>: it is derived from
/// the value's own offset instead of <see cref="TimeZoneInfo.Local"/>, so exported reports (and their
/// snapshots) are identical on machines in any time zone. Sub-second parts are computed from ticks so the
/// output stays the same on runtimes without microsecond and nanosecond date members.
/// </remarks>
internal sealed class DateTimeOffsetYamlTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(DateTimeOffset);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer) =>
        throw new NotSupportedException("Diagnostic reports are exported only; reading them back is not supported.");

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var stamp = (DateTimeOffset)value!;
        DateTime wallClock = stamp.DateTime;
        emitter.Emit(new MappingStart(null, null, true, MappingStyle.Block));
        Emit(emitter, "dateTime", wallClock.ToString("o", CultureInfo.InvariantCulture));
        Emit(emitter, "utcDateTime", stamp.UtcDateTime.ToString("o", CultureInfo.InvariantCulture));
        Emit(emitter, "localDateTime", new DateTimeOffset(wallClock, stamp.Offset).ToString("o", CultureInfo.InvariantCulture));
        Emit(emitter, "date", stamp.Date.ToString("o", CultureInfo.InvariantCulture));
        Emit(emitter, "day", stamp.Day.ToString(CultureInfo.InvariantCulture));
        Emit(emitter, "dayOfWeek", stamp.DayOfWeek.ToString());
        Emit(emitter, "dayOfYear", stamp.DayOfYear.ToString(CultureInfo.InvariantCulture));
        Emit(emitter, "hour", stamp.Hour.ToString(CultureInfo.InvariantCulture));
        Emit(emitter, "millisecond", stamp.Millisecond.ToString(CultureInfo.InvariantCulture));
        Emit(emitter, "microsecond", ((stamp.Ticks / 10) % 1000).ToString(CultureInfo.InvariantCulture));
        Emit(emitter, "nanosecond", ((stamp.Ticks % 10) * 100).ToString(CultureInfo.InvariantCulture));
        Emit(emitter, "minute", stamp.Minute.ToString(CultureInfo.InvariantCulture));
        Emit(emitter, "month", stamp.Month.ToString(CultureInfo.InvariantCulture));
        Emit(emitter, "offset", stamp.Offset.ToString());
        Emit(emitter, "totalOffsetMinutes", stamp.Offset.TotalMinutes.ToString(CultureInfo.InvariantCulture));
        Emit(emitter, "second", stamp.Second.ToString(CultureInfo.InvariantCulture));
        Emit(emitter, "ticks", stamp.Ticks.ToString(CultureInfo.InvariantCulture));
        Emit(emitter, "utcTicks", stamp.UtcTicks.ToString(CultureInfo.InvariantCulture));
        Emit(emitter, "timeOfDay", stamp.TimeOfDay.ToString());
        Emit(emitter, "year", stamp.Year.ToString(CultureInfo.InvariantCulture));
        emitter.Emit(new MappingEnd(default, default));
    }

    private static void Emit(IEmitter emitter, string key, string text)
    {
        emitter.Emit(new Scalar(null, null, key, ScalarStyle.Plain, true, false, default, default));
        emitter.Emit(new Scalar(null, null, text, ScalarStyle.Plain, true, false, default, default));
    }
}
