using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace JetPack.Web;

/// <summary>
/// What a JetPackGrid in server mode (<c>items-source-url</c>) asks for: one page of the rows that pass its
/// search and header filters, in its sort order; or, with <see cref="ValuesFor"/>, the values a header filter lists.
/// </summary>
public sealed class JetPackGridRequest
{
    /// <summary>The page, from 0.</summary>
    public int PageIndex { get; set; }

    /// <summary>Rows per page; 0 for every row.</summary>
    public int PageSize { get; set; }

    /// <summary>Words that must all be found in a row (in any searchable column); quotes keep a phrase together.</summary>
    public string? SearchText { get; set; }

    /// <summary>The binding of the column the rows are grouped by; empty for no groups.</summary>
    public string? GroupMemberPath { get; set; }

    /// <summary>The culture of the page (the cells' formats, typed numbers and dates); the request's culture when empty.</summary>
    public string? Culture { get; set; }

    /// <summary>The grid's columns, in order; a response row has one cell per column.</summary>
    public List<JetPackGridColumnRequest> Columns { get; set; } = [];

    /// <summary>The sort, first column first.</summary>
    public List<JetPackGridSortRequest> Sorts { get; set; } = [];

    /// <summary>The binding of the column whose filter box opened; the response then lists its values instead of rows.</summary>
    public string? ValuesFor { get; set; }

    /// <summary>A filter box lists the values when the column has at most this many (FilterListLimit).</summary>
    public int ValuesLimit { get; set; } = 20;
}

/// <summary>A column of a <see cref="JetPackGridRequest"/>: how its cells are shown, and its header filter.</summary>
public sealed class JetPackGridColumnRequest
{
    public string? Binding { get; set; }

    /// <summary>StringFormat: N0, d, or "{0} hp".</summary>
    public string? StringFormat { get; set; }

    public bool IsCheckBox { get; set; }

    public string TrueText { get; set; } = "Yes";

    public string FalseText { get; set; } = "No";

    public bool IsSearchable { get; set; } = true;

    /// <summary>The header filter's values: text, * for any text, and ranges such as "&gt;50000" or "2020-01..2020-06".</summary>
    public string? FilterText { get; set; }

    /// <summary>The days ticked in the header's date tree ("yyyy-MM-dd"; null for rows without a date).</summary>
    public List<string?>? FilterDates { get; set; }
}

public sealed class JetPackGridSortRequest
{
    public string? Binding { get; set; }

    /// <summary>"ascending" or "descending".</summary>
    public string? Direction { get; set; }
}

/// <summary>A group of rows: its rows are <see cref="Count"/> rows from <see cref="Start"/> among all the rows that pass.</summary>
public sealed class JetPackGridGroup
{
    public string Key { get; set; } = "";

    public string Name { get; set; } = "";

    public int Start { get; set; }

    public int Count { get; set; }
}

/// <summary>A value a header filter lists, with the number of rows that have it.</summary>
public sealed class JetPackGridValue
{
    public string Text { get; set; } = "";

    public int Count { get; set; }
}

/// <summary>A day of a column of dates ("yyyy-MM-dd"; null for the rows without a date), with its number of rows.</summary>
public sealed class JetPackGridDay
{
    public string? Day { get; set; }

    public int Count { get; set; }
}

/// <summary>The answer to a <see cref="JetPackGridRequest"/>.</summary>
public sealed class JetPackGridResponse
{
    /// <summary>The rows of the page: one cell per requested column, in the form the &lt;jp-jetpack-grid&gt; tag helper writes.</summary>
    public List<object?[]> Rows { get; set; } = [];

    /// <summary>Rows in the source.</summary>
    public int TotalCount { get; set; }

    /// <summary>Rows that pass the search and the filters.</summary>
    public int FilteredCount { get; set; }

    /// <summary>The page sent (the requested one, kept within the pages there are).</summary>
    public int PageIndex { get; set; }

    /// <summary>The groups that have rows on the page, while grouped.</summary>
    public List<JetPackGridGroup>? Groups { get; set; }

    /// <summary>For <see cref="JetPackGridRequest.ValuesFor"/>: the column's values; null when it has more than the limit, or holds dates.</summary>
    public List<JetPackGridValue>? Values { get; set; }

    /// <summary>For <see cref="JetPackGridRequest.ValuesFor"/> on a column of dates: its days, for the date tree.</summary>
    public List<JetPackGridDay>? Dates { get; set; }
}

/// <summary>
/// Answers the requests of a JetPackGrid in server mode from rows in memory, with the rules of the WPF
/// JetPackGrid (search, header filters, three-state sorting, groups in the order of their first row, pages):
/// <code>
/// app.MapPost("/api/cars", (JetPackGridRequest request) => JetPackGridServer.Query(cars, request));
/// </code>
/// The grid names the columns it shows, so only bindings you allow are read: the ones you list, or else the
/// public properties of the item type (not nested paths). Pass view models rather than entities
/// with properties the page must not see. For a database, implement the endpoint yourself from the request
/// and send back the same response.
/// </summary>
public static class JetPackGridServer
{
    private const int MaxColumns = 200;
    private const int MaxPageSize = 10_000;

    public static JetPackGridResponse Query<T>(IEnumerable<T> source, JetPackGridRequest request, params string[] bindings)
    {
        var culture = CultureOf(request.Culture);
        var allowed = bindings.Length > 0
            ? new HashSet<string>(bindings, StringComparer.Ordinal)
            : typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0).Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        var items = source.Cast<object?>().ToList();
        var columns = request.Columns.Take(MaxColumns)
            .Select((c, i) => new Column(c, i, c.Binding is { } b && allowed.Contains(b) ? b : null, culture))
            .ToArray();
        foreach (var column in columns)
        {
            column.Type = column.Path == null ? "text" : GridCells.TypeOf(column.Request.IsCheckBox, items.Select(column.Raw));
        }
        var response = new JetPackGridResponse { TotalCount = items.Count };

        if (!string.IsNullOrEmpty(request.ValuesFor))
        {
            if (columns.FirstOrDefault(c => c.Path == request.ValuesFor) is { } column)
            {
                ListValues(column, items, Math.Clamp(request.ValuesLimit, 0, 1000), response);
            }
            return response;
        }

        // The search and the header filters
        var words = GridFilter.Tokenize(request.SearchText, joinRanges: false);
        var searched = words.Count == 0 ? [] : columns.Where(c => c.Path != null && c.Request.IsSearchable).ToArray();
        var filters = columns.Where(c => c.Path != null)
            .Select(c => (Filter: GridFilter.Create(c.Request, culture), Column: c))
            .Where(x => x.Filter != null)
            .ToArray();
        var rows = new List<Row>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var row = new Row(items[i], i, columns.Length);
            if (filters.All(f => f.Filter!.Matches(f.Column.Raw(row.Item), () => row.Text(f.Column)))
                && words.All(w => searched.Any(c => row.Text(c).Contains(w, StringComparison.OrdinalIgnoreCase))))
            {
                rows.Add(row);
            }
        }

        // Ascending, descending; the source order breaks ties
        var sorts = request.Sorts
            .Select(s => (Column: columns.FirstOrDefault(c => c.Path != null && c.Path == s.Binding), Descending: string.Equals(s.Direction, "descending", StringComparison.OrdinalIgnoreCase)))
            .Where(s => s.Column != null)
            .ToArray();
        if (sorts.Length > 0)
        {
            var compare = StringComparer.Create(culture, ignoreCase: false);
            rows.Sort((x, y) =>
            {
                foreach (var (column, descending) in sorts)
                {
                    var c = GridCells.Compare(column!.Raw(x.Item), column.Raw(y.Item), compare);
                    if (c != 0)
                    {
                        return descending ? -c : c;
                    }
                }
                return x.Index.CompareTo(y.Index);
            });
        }

        // Groups in the order of their first row, as CollectionView groups are
        List<JetPackGridGroup>? groups = null;
        if (!string.IsNullOrEmpty(request.GroupMemberPath)
            && columns.FirstOrDefault(c => c.Path != null && string.Equals(c.Path, request.GroupMemberPath, StringComparison.OrdinalIgnoreCase)) is { } by)
        {
            var runs = new Dictionary<string, (JetPackGridGroup Group, List<Row> Rows)>();
            var order = new List<string>();
            foreach (var row in rows)
            {
                var raw = by.Raw(row.Item);
                var key = raw == null ? "" : raw.GetType().Name + ":" + Convert.ToString(raw, CultureInfo.InvariantCulture);
                if (!runs.TryGetValue(key, out var run))
                {
                    runs[key] = run = (new JetPackGridGroup { Key = key, Name = raw == null ? "(blank)" : row.Text(by) }, []);
                    order.Add(key);
                }
                run.Rows.Add(row);
            }
            rows = [];
            groups = [];
            foreach (var key in order)
            {
                var (group, members) = runs[key];
                group.Start = rows.Count;
                group.Count = members.Count;
                groups.Add(group);
                rows.AddRange(members);
            }
        }

        // The page
        var size = Math.Clamp(request.PageSize, 0, MaxPageSize);
        var pages = size > 0 && rows.Count > size ? (rows.Count + size - 1) / size : 1;
        var index = Math.Clamp(request.PageIndex, 0, pages - 1);
        var start = pages > 1 ? index * size : 0;
        var end = pages > 1 ? Math.Min(rows.Count, start + size) : rows.Count;
        response.FilteredCount = rows.Count;
        response.PageIndex = index;
        response.Groups = groups?.Where(g => g.Start < end && g.Start + g.Count > start).ToList();
        for (var i = start; i < end; i++)
        {
            response.Rows.Add(columns.Select(c => c.Path == null ? null : GridCells.Cell(c.Type, c.Raw(rows[i].Item), c.Format)).ToArray());
        }
        return response;
    }

    private static void ListValues(Column column, List<object?> items, int limit, JetPackGridResponse response)
    {
        if (limit <= 0)
        {
            return;
        }
        if (column.Type == "date")
        {
            var days = new Dictionary<string, int>();
            var blanks = 0;
            foreach (var item in items)
            {
                if (GridCells.DateOf(column.Raw(item)) is { } date)
                {
                    var day = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    days[day] = days.GetValueOrDefault(day) + 1;
                }
                else
                {
                    blanks++;
                }
            }
            if (days.Count > 0)
            {
                response.Dates = days.OrderBy(d => d.Key, StringComparer.Ordinal).Select(d => new JetPackGridDay { Day = d.Key, Count = d.Value }).ToList();
                if (blanks > 0)
                {
                    response.Dates.Add(new JetPackGridDay { Day = null, Count = blanks });
                }
                return;
            }
        }
        var counts = new Dictionary<string, JetPackGridValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var text = column.Format(column.Raw(item)).Trim();
            if (text.Length == 0)
            {
                continue;
            }
            if (counts.TryGetValue(text, out var value))
            {
                value.Count++;
            }
            else if (counts.Count == limit)
            {
                return;
            }
            else
            {
                counts[text] = new JetPackGridValue { Text = text, Count = 1 };
            }
        }
        response.Values = counts.Values.ToList();
    }

    private static CultureInfo CultureOf(string? name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            try
            {
                return CultureInfo.GetCultureInfo(name);
            }
            catch (CultureNotFoundException)
            {
            }
        }
        return CultureInfo.CurrentCulture;
    }

    private sealed class Column
    {
        private readonly string? format;
        private readonly CultureInfo culture;

        public Column(JetPackGridColumnRequest request, int index, string? path, CultureInfo culture)
        {
            Request = request;
            Index = index;
            Path = path;
            this.culture = culture;
            format = GridCells.SafeFormat(request.StringFormat);
        }

        public JetPackGridColumnRequest Request { get; }

        public int Index { get; }

        /// <summary>The binding, when it is allowed; null leaves the column's cells empty.</summary>
        public string? Path { get; }

        public string Type { get; set; } = "text";

        public object? Raw(object? item) => Path == null ? null : GridCells.Read(item, Path);

        /// <summary>The text of a value as the cell shows it (TrueText and FalseText for check boxes).</summary>
        public string Format(object? value) => GridCells.Text(value, format, Request.IsCheckBox ? (Request.TrueText, Request.FalseText) : null, culture);
    }

    private sealed class Row(object? item, int index, int columns)
    {
        private readonly string?[] texts = new string?[columns];

        public object? Item { get; } = item;

        public int Index { get; } = index;

        /// <summary>The cell's text, read once.</summary>
        public string Text(Column column) => column.Path == null ? "" : texts[column.Index] ??= column.Format(column.Raw(Item));
    }
}

/// <summary>Reading, formatting and encoding cells, shared by the tag helper and <see cref="JetPackGridServer"/>.</summary>
internal static class GridCells
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> Properties = new();

    /// <summary>The value at a dotted member path of an item (properties, or the keys of a dictionary).</summary>
    public static object? Read(object? item, string? path)
    {
        if (item is null || string.IsNullOrEmpty(path))
        {
            return item;
        }
        object? value = item;
        foreach (var name in path.Split('.'))
        {
            value = value switch
            {
                null => null,
                IDictionary<string, object?> map => map.TryGetValue(name, out var v) ? v : null,
                _ => Properties.GetOrAdd((value.GetType(), name), static key => key.Item1.GetProperty(key.Item2, BindingFlags.Public | BindingFlags.Instance)) is { } p
                     && p.GetIndexParameters().Length == 0 ? p.GetValue(value) : null,
            };
        }
        return value;
    }

    /// <summary>number, date or bool when the column's values are; text for everything else.</summary>
    public static string TypeOf(bool checkBox, IEnumerable<object?> values)
    {
        if (checkBox)
        {
            return "bool";
        }
        foreach (var value in values)
        {
            switch (value)
            {
                case null:
                    continue;
                case DateTime or DateTimeOffset or DateOnly:
                    return "date";
                case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                    return "number";
                default:
                    return "text";
            }
        }
        return "text";
    }

    public static bool IsNumber(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong or decimal
        || (value is double d && double.IsFinite(d)) || (value is float f && float.IsFinite(f));

    public static DateTime? DateOf(object? value) => value switch
    {
        DateTime d => d,
        DateTimeOffset o => o.DateTime,
        DateOnly o => o.ToDateTime(TimeOnly.MinValue),
        _ => null,
    };

    /// <summary>A StringFormat ready for string.Format ("{0:N0}"); null for none.</summary>
    public static string? FormatOf(string? stringFormat) =>
        string.IsNullOrEmpty(stringFormat) ? null : stringFormat.Contains('{') ? stringFormat : "{0:" + stringFormat + "}";

    /// <summary>
    /// <see cref="FormatOf"/> for a format a browser sent: short, one argument, no alignment and at most two
    /// digits of precision, so a request cannot make the server build huge strings. Null for anything else.
    /// </summary>
    public static string? SafeFormat(string? stringFormat)
    {
        if (string.IsNullOrEmpty(stringFormat) || stringFormat.Length > 64)
        {
            return null;
        }
        var format = FormatOf(stringFormat)!;
        return Regex.IsMatch(format, @"^(?:[^{}]|\{\{|\}\}|\{0(?::[^{}]*)?\})*$")
            && !Regex.IsMatch(format, @"\{0:[^{}]*\d{3,}[^{}]*\}") ? format : null;
    }

    public static string Text(object? value, string? format, (string True, string False)? checkTexts, CultureInfo culture)
    {
        if (checkTexts is var (yes, no))
        {
            return value switch { true => yes, false => no, _ => "" };
        }
        if (value is null)
        {
            return "";
        }
        if (format != null)
        {
            try
            {
                return string.Format(culture, format, value);
            }
            catch (FormatException)
            {
            }
        }
        return value is IFormattable f ? f.ToString(null, culture) : value.ToString() ?? "";
    }

    /// <summary>
    /// A cell in the form jetpack.js reads: the text for text columns; for numbers the number, or [text, number]
    /// when the cell shows it formatted; for dates [text, "yyyy-MM-dd(THH:mm:ss)"]; true or false for check boxes.
    /// </summary>
    public static object? Cell(string type, object? value, Func<object?, string> text)
    {
        var inv = CultureInfo.InvariantCulture;
        if (value is null)
        {
            return null;
        }
        switch (type)
        {
            case "bool":
                return value is true;
            case "number" when IsNumber(value):
                var shown = text(value);
                var number = Convert.ToDecimal(value, inv);
                return shown == number.ToString(inv) ? number : new object[] { shown, number };
            case "date" when DateOf(value) is { } date:
                return new object[] { text(value), date.TimeOfDay == TimeSpan.Zero ? date.ToString("yyyy-MM-dd", inv) : date.ToString("yyyy-MM-ddTHH:mm:ss", inv) };
            default:
                return text(value);
        }
    }

    /// <summary>Sort order of two cell values: nulls first, numbers by value, text in the culture's order.</summary>
    public static int Compare(object? a, object? b, StringComparer text)
    {
        if (a is null || b is null)
        {
            return a is null ? (b is null ? 0 : -1) : 1;
        }
        if (IsNumber(a) && IsNumber(b))
        {
            return a is double or float || b is double or float
                ? Convert.ToDouble(a, CultureInfo.InvariantCulture).CompareTo(Convert.ToDouble(b, CultureInfo.InvariantCulture))
                : Convert.ToDecimal(a, CultureInfo.InvariantCulture).CompareTo(Convert.ToDecimal(b, CultureInfo.InvariantCulture));
        }
        if (a is string || b is string)
        {
            return text.Compare(Convert.ToString(a, CultureInfo.InvariantCulture), Convert.ToString(b, CultureInfo.InvariantCulture));
        }
        if (a.GetType() == b.GetType() && a is IComparable comparable)
        {
            return comparable.CompareTo(b);
        }
        return text.Compare(a.ToString(), b.ToString());
    }
}

/// <summary>A header filter of a column, with the rules of the WPF JetPackGrid (JetPackGrid.cs).</summary>
internal sealed class GridFilter
{
    private readonly FilterValue[] values;
    private readonly HashSet<DateTime>? days;
    private readonly bool blankDays;
    private readonly bool hasRanges;

    private GridFilter(FilterValue[] values, HashSet<DateTime>? days, bool blankDays)
    {
        this.values = values;
        this.days = days;
        this.blankDays = blankDays;
        hasRanges = values.Any(v => v.Range != null);
    }

    /// <summary>The column's filter; null when it has none.</summary>
    public static GridFilter? Create(JetPackGridColumnRequest column, CultureInfo culture)
    {
        var tokens = Tokenize(column.FilterText, joinRanges: true);
        var dates = column.FilterDates;
        if (tokens.Count == 0 && dates is not { Count: > 0 })
        {
            return null;
        }
        HashSet<DateTime>? days = null;
        if (dates is { Count: > 0 })
        {
            days = [];
            foreach (var d in dates)
            {
                if (d != null && DateTime.TryParseExact(d.Length > 10 ? d[..10] : d, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
                {
                    days.Add(day);
                }
            }
        }
        return new GridFilter(tokens.Select(t => FilterValue.Parse(t, culture)).ToArray(), days, dates?.Contains(null) == true);
    }

    public bool Matches(object? raw, Func<string> text)
    {
        if (days != null && !(GridCells.DateOf(raw) is { } date ? days.Contains(date.Date) : raw == null && blankDays))
        {
            return false;
        }
        if (values.Length == 0)
        {
            return true;
        }
        string? shown = null;
        foreach (var value in values)
        {
            if (hasRanges && value.Range is { } range && range.TryMatch(raw, out var inRange))
            {
                if (inRange)
                {
                    return true;
                }
                continue;
            }
            shown ??= text().Trim();
            if (value.Matches(shown))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Splits on spaces, commas and semicolons; quotes keep a value together; with joinRanges "&gt; 5" and "a .. b" stay one value.</summary>
    public static List<string> Tokenize(string? text, bool joinRanges)
    {
        var tokens = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return tokens;
        }
        var current = new StringBuilder();
        var quoted = false;
        string? pending = null;
        void Flush()
        {
            var token = current.ToString().Trim();
            current.Clear();
            if (token.Length == 0)
            {
                return;
            }
            if (joinRanges)
            {
                if (token == ".." && pending == null && tokens.Count > 0)
                {
                    pending = tokens[^1] + token;
                    tokens.RemoveAt(tokens.Count - 1);
                    return;
                }
                if (token is ">" or "<" or ">=" or "<=")
                {
                    pending += token;
                    return;
                }
                if (pending != null)
                {
                    token = pending + token;
                    pending = null;
                }
            }
            if (!tokens.Contains(token, StringComparer.OrdinalIgnoreCase))
            {
                tokens.Add(token);
            }
        }
        foreach (var ch in text)
        {
            if (ch == '"')
            {
                Flush();
                quoted = !quoted;
            }
            else if (!quoted && (char.IsWhiteSpace(ch) || ch == ',' || ch == ';'))
            {
                Flush();
            }
            else
            {
                current.Append(ch);
            }
        }
        Flush();
        if (pending != null && !tokens.Contains(pending, StringComparer.OrdinalIgnoreCase))
        {
            tokens.Add(pending);
        }
        return tokens;
    }

    private readonly record struct FilterValue(string Text, bool AnyStart, bool AnyEnd, FilterRange? Range)
    {
        public static FilterValue Parse(string token, CultureInfo culture) =>
            new(token.Trim('*'), token.StartsWith('*'), token.EndsWith('*') && token.Length > 1, FilterRange.Parse(token, culture));

        public bool Matches(string text) => (AnyStart, AnyEnd) switch
        {
            _ when Text.Length == 0 => text.Length > 0,
            (true, true) => text.Contains(Text, StringComparison.OrdinalIgnoreCase),
            (true, false) => text.EndsWith(Text, StringComparison.OrdinalIgnoreCase),
            (false, true) => text.StartsWith(Text, StringComparison.OrdinalIgnoreCase),
            _ => text.Equals(Text, StringComparison.OrdinalIgnoreCase),
        };
    }

    /// <summary>A range of numbers or dates; a year or a month stands for all of it, a date without a time for the whole day.</summary>
    private sealed class FilterRange(FilterRange.Bound? from, bool above, FilterRange.Bound? to, bool below)
    {
        public static FilterRange? Parse(string token, CultureInfo culture)
        {
            string? low = null, high = null;
            bool above = false, below = false;
            if (token.StartsWith(">=", StringComparison.Ordinal)) low = token[2..];
            else if (token.StartsWith("<=", StringComparison.Ordinal)) high = token[2..];
            else if (token.StartsWith('>')) { low = token[1..]; above = true; }
            else if (token.StartsWith('<')) { high = token[1..]; below = true; }
            else if (token.IndexOf("..", StringComparison.Ordinal) is var dots and >= 0) { low = token[..dots]; high = token[(dots + 2)..]; }
            else return null;

            low = low?.Trim();
            high = high?.Trim();
            if (string.IsNullOrEmpty(low) && string.IsNullOrEmpty(high))
            {
                return null;
            }
            Bound? from = null, to = null;
            if ((!string.IsNullOrEmpty(low) && (from = Bound.Parse(low, culture)) == null)
                || (!string.IsNullOrEmpty(high) && (to = Bound.Parse(high, culture)) == null))
            {
                return null;
            }
            return new FilterRange(from, above, to, below);
        }

        /// <summary>Whether a value is in the range; false when the range does not apply to it (it is then matched as text).</summary>
        public bool TryMatch(object? value, out bool result)
        {
            switch (value)
            {
                case null:
                    result = false;
                    return true;
                case DateTime or DateTimeOffset or DateOnly:
                    return TryMatch(GridCells.DateOf(value)!.Value, b => b.Start is { } start ? (start, b.End ?? start, b.End == null) : null, out result);
                case double or float:
                    var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    return TryMatch(number, b => b.Number is { } n ? ((double)n, (double)n, true) : null, out result) && !double.IsNaN(number);
                case decimal or int or long or short or byte or sbyte or uint or ulong or ushort:
                    return TryMatch(Convert.ToDecimal(value, CultureInfo.InvariantCulture), b => b.Number is { } n ? (n, n, true) : null, out result);
                default:
                    result = false;
                    return false;
            }
        }

        private bool TryMatch<T>(T value, Func<Bound, (T Low, T High, bool HighInclusive)?> read, out bool result) where T : IComparable<T>
        {
            result = false;
            (T Low, T High, bool HighInclusive)? low = null, high = null;
            if ((from != null && (low = read(from)) == null) || (to != null && (high = read(to)) == null))
            {
                return false;
            }
            if (low is var (fromLow, fromHigh, fromInclusive))
            {
                var c = value.CompareTo(above ? fromHigh : fromLow);
                if (c < 0 || (above && c == 0 && fromInclusive))
                {
                    return true;
                }
            }
            if (high is var (toLow, toHigh, toInclusive))
            {
                var c = value.CompareTo(below ? toLow : toHigh);
                if (below ? c >= 0 : c > 0 || (c == 0 && !toInclusive))
                {
                    return true;
                }
            }
            result = true;
            return true;
        }

        public sealed record Bound(decimal? Number, DateTime? Start, DateTime? End)
        {
            private static readonly string[] MonthFormats = ["yyyy-M", "yyyy-MM"];

            public static Bound? Parse(string text, CultureInfo culture)
            {
                decimal? number = decimal.TryParse(text, NumberStyles.Number, culture, out var n) ? n : null;
                DateTime? start = null, end = null;
                if (text.Length == 4 && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var year) && year >= 1)
                {
                    start = new DateTime(year, 1, 1);
                    end = year < 9999 ? start.Value.AddYears(1) : DateTime.MaxValue;
                }
                else if (DateTime.TryParseExact(text, MonthFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var month))
                {
                    start = month;
                    end = month.AddMonths(1);
                }
                else if (DateTime.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out var date))
                {
                    start = date;
                    end = date.TimeOfDay == TimeSpan.Zero && text.IndexOf(':') < 0 && date < DateTime.MaxValue.Date ? date.AddDays(1) : null;
                }
                return number == null && start == null ? null : new Bound(number, start, end);
            }
        }
    }
}
