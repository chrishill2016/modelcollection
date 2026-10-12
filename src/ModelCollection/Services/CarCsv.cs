using System.Globalization;
using System.Text;
using ModelCollection.Models;

namespace ModelCollection.Services;

/// <summary>Writes cars as CSV for opening in Excel or Google Sheets.</summary>
public static class CarCsv
{
    private static readonly string[] Headers =
    [
        "Make", "Model", "Year", "Manufacturer", "Scale", "Colour", "Condition", "Boxed",
        "Purchase date", "Purchase price", "Estimated value", "Location", "Tags", "Notes",
    ];

    public static string Write(IEnumerable<ModelCar> cars)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', Headers));
        foreach (var car in cars)
        {
            string?[] fields =
            [
                car.Make, car.Model, car.Year?.ToString(CultureInfo.InvariantCulture), car.Manufacturer, car.Scale,
                car.Colour, car.Condition.ToLabel(), car.HasBox ? "Yes" : "No",
                car.PurchaseDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                car.PurchasePrice?.ToString(CultureInfo.InvariantCulture),
                car.EstimatedValue?.ToString(CultureInfo.InvariantCulture),
                car.Location, string.Join("; ", car.Tags), car.Notes,
            ];
            sb.AppendLine(string.Join(',', fields.Select(Escape)));
        }

        return sb.ToString();
    }

    internal static string Escape(string? value)
    {
        value ??= "";
        // Guard against spreadsheet formula injection.
        if (value.Length > 0 && "=+-@".Contains(value[0]) && !decimal.TryParse(value, CultureInfo.InvariantCulture, out _))
        {
            value = "'" + value;
        }

        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
