using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace JetPack.Wpf.Controls;

/// <summary>
/// Navigation pane on the left (a list of <see cref="HamburgerMenuItem"/>) and
/// a content area on the right. The selected item gets the accent gradient.
/// </summary>
/// <remarks>
/// Two levels: an item with <see cref="HamburgerMenuItem.Items"/> is a group that opens and closes
/// (it is never selected itself). The menu keeps a group's sub-items right after it in its own
/// <see cref="ItemsControl.Items"/>, so <see cref="System.Windows.Controls.Primitives.Selector.SelectedItem"/> is the selected page on
/// either level.
/// </remarks>
public class HamburgerMenu : ListBox
{
    private bool _syncing;
    private bool _syncQueued;

    static HamburgerMenu()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HamburgerMenu), new FrameworkPropertyMetadata(typeof(HamburgerMenu)));
        // ListBox selects the item that keyboard navigation moves the focus to; a group only takes
        // the focus. Class handlers of a derived class run before those of its base classes.
        EventManager.RegisterClassHandler(typeof(HamburgerMenu), Keyboard.GotKeyboardFocusEvent,
            new KeyboardFocusChangedEventHandler((_, e) =>
            {
                if (e.NewFocus is HamburgerMenuItem { IsGroup: true })
                {
                    e.Handled = true;
                }
            }));
    }

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(object), typeof(HamburgerMenu), new PropertyMetadata(null));

    /// <summary>What the content area shows; usually bound to the selected item.</summary>
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.Register(
        nameof(ContentTemplate), typeof(DataTemplate), typeof(HamburgerMenu), new PropertyMetadata(null));

    public DataTemplate? ContentTemplate
    {
        get => (DataTemplate?)GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public static readonly DependencyProperty IsPaneOpenProperty = DependencyProperty.Register(
        nameof(IsPaneOpen), typeof(bool), typeof(HamburgerMenu),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsPaneOpenChanged));

    /// <summary>False shows only the icons; sub-items are hidden then, and their group shows the selection.</summary>
    public bool IsPaneOpen
    {
        get => (bool)GetValue(IsPaneOpenProperty);
        set => SetValue(IsPaneOpenProperty, value);
    }

    public static readonly DependencyProperty PaneWidthProperty = DependencyProperty.Register(
        nameof(PaneWidth), typeof(double), typeof(HamburgerMenu), new PropertyMetadata(170d));

    public double PaneWidth
    {
        get => (double)GetValue(PaneWidthProperty);
        set => SetValue(PaneWidthProperty, value);
    }

    public static readonly DependencyProperty CompactPaneWidthProperty = DependencyProperty.Register(
        nameof(CompactPaneWidth), typeof(double), typeof(HamburgerMenu), new PropertyMetadata(40d));

    public double CompactPaneWidth
    {
        get => (double)GetValue(CompactPaneWidthProperty);
        set => SetValue(CompactPaneWidthProperty, value);
    }

    protected override bool IsItemItsOwnContainerOverride(object item) => item is HamburgerMenuItem;

    protected override DependencyObject GetContainerForItemOverride() => new HamburgerMenuItem();

    private static void OnIsPaneOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var menu = (HamburgerMenu)d;
        foreach (var item in menu.MenuItems)
        {
            item.CoerceValue(VisibilityProperty);
            item.UpdateShowsChildSelection();
        }
    }

    private IEnumerable<HamburgerMenuItem> MenuItems => Items.OfType<HamburgerMenuItem>();

    private bool HasGroups => MenuItems.Any(i => i.HasItems && i.Group is null);

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        SyncSubItems();
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        // Items cannot change while they announce a change; groups added or removed later get their
        // sub-items put in or taken out right after.
        if (IsInitialized && !_syncing && !_syncQueued)
        {
            _syncQueued = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
            {
                _syncQueued = false;
                SyncSubItems();
            });
        }
    }

    /// <summary>Puts each group's sub-items right after it in <see cref="ItemsControl.Items"/>.</summary>
    internal void SyncSubItems()
    {
        if (ItemsSource is not null || !IsInitialized)
        {
            return;
        }
        _syncing = true;
        try
        {
            // Sub-items whose group has gone, or that left their group
            for (var i = Items.Count - 1; i >= 0; i--)
            {
                if (Items[i] is HamburgerMenuItem { Group: { } g } sub && (!Items.Contains(g) || !g.Items.Contains(sub)))
                {
                    sub.Group = null;
                    Items.RemoveAt(i);
                }
            }

            var index = 0;
            while (index < Items.Count)
            {
                var at = index + 1;
                if (Items[index] is HamburgerMenuItem { Group: null } group)
                {
                    foreach (var sub in group.Items)
                    {
                        var current = Items.IndexOf(sub);
                        if (current != at)
                        {
                            if (current >= 0)
                            {
                                Items.RemoveAt(current);
                                if (current < at)
                                {
                                    at--;
                                }
                            }
                            Items.Insert(at, sub);
                        }
                        sub.Group = group;
                        at++;
                    }
                }
                index = at;
            }
        }
        finally
        {
            _syncing = false;
        }

        // A sub-item marked IsSelected in XAML was not part of the menu when the selection was made
        if (MenuItems.FirstOrDefault(i => i.Group is not null && i.IsSelected) is { } marked && !ReferenceEquals(SelectedItem, marked))
        {
            SelectedItem = marked;
        }
        else if (SelectedItem is HamburgerMenuItem { HasItems: true, Group: null } group)
        {
            SelectedItem = group.Items[0];
        }
        UpdateGroups();
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);
        // A group is not a page: SelectedIndex or IsSelected on one selects its first sub-item
        if (SelectedItem is HamburgerMenuItem { HasItems: true, Group: null } group && group.Items[0].Group == group)
        {
            SelectedItem = group.Items[0];
            return;
        }
        if (SelectedItem is HamburgerMenuItem { Group: { } parent })
        {
            parent.IsExpanded = true;
        }
        UpdateGroups();
    }

    private void UpdateGroups()
    {
        foreach (var item in MenuItems)
        {
            if (item.Group is null)
            {
                item.SetValue(HamburgerMenuItem.IsChildSelectedPropertyKey, item.HasItems && item.Items.Contains(SelectedItem));
                item.UpdateShowsChildSelection();
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // With groups the arrow keys move over the visible items and select pages, but not groups
        if (HasGroups && Keyboard.Modifiers == ModifierKeys.None && e.Key is Key.Up or Key.Down or Key.Home or Key.End)
        {
            var visible = MenuItems.Where(i => i.IsVisible && i.IsEnabled).ToList();
            if (visible.Count > 0)
            {
                var current = Keyboard.FocusedElement is HamburgerMenuItem focused ? visible.IndexOf(focused) : -1;
                var next = e.Key switch
                {
                    Key.Home => 0,
                    Key.End => visible.Count - 1,
                    Key.Up => Math.Max(0, current - 1),
                    _ => current < 0 ? 0 : Math.Min(visible.Count - 1, current + 1),
                };
                var target = visible[next];
                target.Focus();
                target.BringIntoView();
                if (!target.IsGroup)
                {
                    SelectedItem = target;
                }
            }
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }
}

public class HamburgerMenuItem : ListBoxItem
{
    static HamburgerMenuItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HamburgerMenuItem), new FrameworkPropertyMetadata(typeof(HamburgerMenuItem)));
        VisibilityProperty.OverrideMetadata(typeof(HamburgerMenuItem), new FrameworkPropertyMetadata(Visibility.Visible, null, CoerceVisibility));
    }

    public HamburgerMenuItem()
    {
        Items.CollectionChanged += OnSubItemsChanged;
    }

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(object), typeof(HamburgerMenuItem), new PropertyMetadata(null));

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Sub-items: they make this item a group that opens and closes. Sub-items show no icon, and
    /// their own sub-items are ignored (two levels).
    /// </summary>
    public ObservableCollection<HamburgerMenuItem> Items { get; } = [];

    private static readonly DependencyPropertyKey HasItemsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasItems), typeof(bool), typeof(HamburgerMenuItem), new PropertyMetadata(false));

    public static readonly DependencyProperty HasItemsProperty = HasItemsPropertyKey.DependencyProperty;

    /// <summary>True when the item has sub-items (a group).</summary>
    public bool HasItems => (bool)GetValue(HasItemsProperty);

    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
        nameof(IsExpanded), typeof(bool), typeof(HamburgerMenuItem),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsExpandedChanged));

    /// <summary>Whether a group shows its sub-items. Selecting a sub-item opens its group.</summary>
    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    private static readonly DependencyPropertyKey IsSubItemPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsSubItem), typeof(bool), typeof(HamburgerMenuItem), new PropertyMetadata(false));

    public static readonly DependencyProperty IsSubItemProperty = IsSubItemPropertyKey.DependencyProperty;

    /// <summary>True for the sub-items of a group (the second level).</summary>
    public bool IsSubItem => (bool)GetValue(IsSubItemProperty);

    internal static readonly DependencyPropertyKey IsChildSelectedPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsChildSelected), typeof(bool), typeof(HamburgerMenuItem), new PropertyMetadata(false));

    public static readonly DependencyProperty IsChildSelectedProperty = IsChildSelectedPropertyKey.DependencyProperty;

    /// <summary>True for a group when one of its sub-items is the selected page.</summary>
    public bool IsChildSelected => (bool)GetValue(IsChildSelectedProperty);

    private static readonly DependencyPropertyKey ShowsChildSelectionPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ShowsChildSelection), typeof(bool), typeof(HamburgerMenuItem), new PropertyMetadata(false));

    public static readonly DependencyProperty ShowsChildSelectionProperty = ShowsChildSelectionPropertyKey.DependencyProperty;

    /// <summary>
    /// True for a group that stands in for its selected sub-item: the group is closed, or the pane
    /// shows only icons. The template then gives the group the selected look.
    /// </summary>
    public bool ShowsChildSelection => (bool)GetValue(ShowsChildSelectionProperty);

    private HamburgerMenuItem? _group;

    /// <summary>The group this item is a sub-item of, while both are in a menu.</summary>
    internal HamburgerMenuItem? Group
    {
        get => _group;
        set
        {
            _group = value;
            SetValue(IsSubItemPropertyKey, value is not null);
            CoerceValue(VisibilityProperty);
        }
    }

    internal bool IsGroup => HasItems && _group is null;

    private HamburgerMenu? Menu => ItemsControl.ItemsControlFromItemContainer(this) as HamburgerMenu;

    private void OnSubItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SetValue(HasItemsPropertyKey, Items.Count > 0);
        Menu?.SyncSubItems();
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var item = (HamburgerMenuItem)d;
        foreach (var sub in item.Items)
        {
            sub.CoerceValue(VisibilityProperty);
        }
        item.UpdateShowsChildSelection();
    }

    private static object CoerceVisibility(DependencyObject d, object value)
    {
        var item = (HamburgerMenuItem)d;
        return item._group is { } group && (!group.IsExpanded || group.Menu is { IsPaneOpen: false }) ? Visibility.Collapsed : value;
    }

    internal void UpdateShowsChildSelection() =>
        SetValue(ShowsChildSelectionPropertyKey, IsChildSelected && (!IsExpanded || Menu is { IsPaneOpen: false }));

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (IsGroup)
        {
            // A group opens and closes; it is not selected. With the pane showing only icons it
            // opens the pane, since the sub-items have no icons to show.
            e.Handled = true;
            Focus();
            if (Menu is { IsPaneOpen: false } menu)
            {
                menu.IsPaneOpen = true;
                IsExpanded = true;
            }
            else
            {
                IsExpanded = !IsExpanded;
            }
            return;
        }
        base.OnMouseLeftButtonDown(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (IsGroup && Keyboard.Modifiers == ModifierKeys.None)
        {
            switch (e.Key)
            {
                case Key.Enter or Key.Space:
                    IsExpanded = !IsExpanded;
                    e.Handled = true;
                    return;
                case Key.Right when !IsExpanded:
                    IsExpanded = true;
                    e.Handled = true;
                    return;
                case Key.Left when IsExpanded:
                    IsExpanded = false;
                    e.Handled = true;
                    return;
            }
        }
        else if (_group is { } group && e.Key == Key.Left && Keyboard.Modifiers == ModifierKeys.None)
        {
            group.Focus();
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }
}
