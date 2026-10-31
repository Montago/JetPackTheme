using System.ComponentModel;
using System.Globalization;

namespace JetPack.Wpf.Controls;

/// <summary>What a <see cref="GridDateFilterNode"/> stands for.</summary>
public enum GridDateFilterLevel
{
    /// <summary>Ten years (2010–2019); the top level when the dates span ten years or more.</summary>
    Decade,
    Year,
    Month,
    Day,

    /// <summary>The rows without a date.</summary>
    Blank,
}

/// <summary>
/// A decade, year, month or day in the date tree of a <see cref="JetPackGrid"/> header filter, which a
/// column of dates gets instead of the tick list. Ticking a node ticks every day in it; a node with only
/// some of its days ticked is partly ticked (<see cref="IsChecked"/> null).
/// </summary>
public sealed class GridDateFilterNode : INotifyPropertyChanged
{
    private readonly Action changed;
    private GridDateFilterNode? parent;
    private bool? isChecked = false;
    private bool isExpanded;

    private GridDateFilterNode(GridDateFilterLevel level, DateTime? date, string text, int count, GridDateFilterNode[] children, Action changed)
    {
        Level = level;
        Date = date;
        Text = text;
        Count = count;
        Children = children;
        this.changed = changed;
        foreach (var child in children)
        {
            child.parent = this;
        }
    }

    public GridDateFilterLevel Level { get; }

    /// <summary>The day, or the first day of the decade, year or month; null for the rows without a date.</summary>
    public DateTime? Date { get; }

    /// <summary>The node as the tree shows it: "2019", "March", "14", "(blank)".</summary>
    public string Text { get; }

    /// <summary>Rows in the source with a date in it.</summary>
    public int Count { get; }

    public IReadOnlyList<GridDateFilterNode> Children { get; }

    /// <summary>True when every day in it is ticked, null when some are. Ticking writes the column's FilterDates.</summary>
    public bool? IsChecked
    {
        get => isChecked;
        set
        {
            // The check box goes from partly ticked to null; take that as a click that clears the node.
            var tick = value ?? false;
            if (isChecked == tick)
            {
                return;
            }
            SetAll(tick);
            parent?.UpdateFromChildren();
            changed();
        }
    }

    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            if (isExpanded != value)
            {
                isExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The day of a DateTime, DateTimeOffset or DateOnly, without its time; false for any other value.</summary>
    internal static bool TryGetDay(object? value, out DateTime day)
    {
        (var isDate, day) = value switch
        {
            DateTime date => (true, date.Date),
            DateTimeOffset offset => (true, offset.Date),
            DateOnly only => (true, only.ToDateTime(TimeOnly.MinValue)),
            _ => (false, default),
        };
        return isDate;
    }

    /// <summary>
    /// The tree of the days and their row counts: years with their months and days in date order, under
    /// decades when the years span ten or more, and "(blank)" last for the rows without a date.
    /// </summary>
    internal static List<GridDateFilterNode> Build(IReadOnlyDictionary<DateTime, int> days, int blanks, CultureInfo culture, Action changed)
    {
        var names = culture.DateTimeFormat;
        var years = new List<GridDateFilterNode>();
        foreach (var year in days.GroupBy(d => d.Key.Year).OrderBy(g => g.Key))
        {
            var months = new List<GridDateFilterNode>();
            foreach (var month in year.GroupBy(d => d.Key.Month).OrderBy(g => g.Key))
            {
                var dayNodes = month.OrderBy(d => d.Key)
                    .Select(d => new GridDateFilterNode(GridDateFilterLevel.Day, d.Key, d.Key.Day.ToString(culture), d.Value, [], changed))
                    .ToArray();
                var name = names.GetMonthName(month.Key);
                if (name.Length > 0)
                {
                    name = char.ToUpper(name[0], culture) + name[1..];
                }
                months.Add(new GridDateFilterNode(GridDateFilterLevel.Month, new DateTime(year.Key, month.Key, 1), name, dayNodes.Sum(d => d.Count), dayNodes, changed));
            }
            years.Add(new GridDateFilterNode(GridDateFilterLevel.Year, new DateTime(year.Key, 1, 1), year.Key.ToString(culture),
                months.Sum(m => m.Count), months.ToArray(), changed));
        }

        var top = years;
        if (years.Count > 1 && years[^1].Date!.Value.Year - years[0].Date!.Value.Year >= 10)
        {
            top = years.GroupBy(y => y.Date!.Value.Year / 10 * 10)
                .Select(g => new GridDateFilterNode(GridDateFilterLevel.Decade, new DateTime(g.Key, 1, 1),
                    $"{g.Key.ToString(culture)}–{(g.Key + 9).ToString(culture)}", g.Sum(y => y.Count), g.ToArray(), changed))
                .ToList();
        }
        if (top.Count == 1)
        {
            // A single year (or decade) has nothing to choose between; show what is in it.
            top[0].isExpanded = true;
        }
        if (blanks > 0)
        {
            top.Add(new GridDateFilterNode(GridDateFilterLevel.Blank, null, "(blank)", blanks, [], changed));
        }
        return top;
    }

    /// <summary>The ticked days in date order, with null for "(blank)" when it is ticked.</summary>
    internal static List<DateTime?> GetTicked(IEnumerable<GridDateFilterNode> nodes)
    {
        var ticked = new List<DateTime?>();
        foreach (var node in nodes)
        {
            node.CollectTicked(ticked);
        }
        return ticked;
    }

    /// <summary>Ticks exactly the days in <paramref name="dates"/> (their time does not count; null ticks "(blank)").</summary>
    internal static void Tick(IEnumerable<GridDateFilterNode> nodes, IEnumerable<DateTime?>? dates)
    {
        var days = new HashSet<DateTime>();
        var blanks = false;
        foreach (var date in dates ?? [])
        {
            if (date is { } day)
            {
                days.Add(day.Date);
            }
            else
            {
                blanks = true;
            }
        }
        foreach (var node in nodes)
        {
            node.TickFrom(days, blanks);
        }
    }

    /// <summary>Opens every node with only some of its days ticked, so the tree shows what the filter holds.</summary>
    internal static void ExpandPartlyTicked(IEnumerable<GridDateFilterNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.isChecked == null)
            {
                node.IsExpanded = true;
                ExpandPartlyTicked(node.Children);
            }
        }
    }

    private void CollectTicked(List<DateTime?> ticked)
    {
        if (isChecked == false)
        {
            return;
        }
        if (Children.Count == 0)
        {
            ticked.Add(Date);
            return;
        }
        foreach (var child in Children)
        {
            child.CollectTicked(ticked);
        }
    }

    private bool? TickFrom(HashSet<DateTime> days, bool blanks)
    {
        bool? state;
        switch (Level)
        {
            case GridDateFilterLevel.Day:
                state = days.Contains(Date!.Value);
                break;
            case GridDateFilterLevel.Blank:
                state = blanks;
                break;
            default:
                // Every child is ticked before their states are combined.
                var states = new bool?[Children.Count];
                for (var i = 0; i < states.Length; i++)
                {
                    states[i] = Children[i].TickFrom(days, blanks);
                }
                state = Combine(states);
                break;
        }
        Set(state);
        return state;
    }

    private void SetAll(bool tick)
    {
        Set(tick);
        foreach (var child in Children)
        {
            child.SetAll(tick);
        }
    }

    private void UpdateFromChildren()
    {
        if (Set(Combine(Children.Select(c => c.isChecked))))
        {
            parent?.UpdateFromChildren();
        }
    }

    /// <summary>The common state of the children; null when they differ.</summary>
    private static bool? Combine(IEnumerable<bool?> states)
    {
        bool? common = null;
        var first = true;
        foreach (var state in states)
        {
            if (first)
            {
                common = state;
                first = false;
            }
            else if (common != state)
            {
                return null;
            }
        }
        return common;
    }

    private bool Set(bool? value)
    {
        if (isChecked == value)
        {
            return false;
        }
        isChecked = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
        return true;
    }
}
