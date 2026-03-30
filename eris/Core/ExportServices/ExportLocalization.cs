using System.Globalization;
using System.Resources;

namespace eris.Core.ExportServices;

public sealed class ExportLocalization
{
    private static readonly ResourceManager ResourceManager =
        new("eris.Core.Resources.Strings.ExportStrings", typeof(ExportLocalization).Assembly);

    public CultureInfo TextCulture { get; }
    public CultureInfo FormatCulture { get; }
    public string CsvDelimiter { get; }

    private ExportLocalization(CultureInfo textCulture, CultureInfo formatCulture)
    {
        TextCulture = textCulture;
        FormatCulture = formatCulture;
        CsvDelimiter = string.IsNullOrWhiteSpace(formatCulture.TextInfo.ListSeparator)
            ? ";"
            : formatCulture.TextInfo.ListSeparator;
    }

    public static ExportLocalization Current()
        => new(CultureInfo.CurrentUICulture, CultureInfo.CurrentCulture);

    public string Get(string key)
        => ResourceManager.GetString(key, TextCulture) ?? key;
}
