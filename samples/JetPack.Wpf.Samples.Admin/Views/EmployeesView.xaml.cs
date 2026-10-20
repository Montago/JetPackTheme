using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using JetPack.Wpf.Controls;

namespace JetPack.Wpf.Samples.Admin.Views;

public partial class EmployeesView : UserControl
{
    private readonly ObservableCollection<Employee> employees = new(SampleData.Employees);
    private readonly ICollectionView view;

    public EmployeesView()
    {
        InitializeComponent();
        view = CollectionViewSource.GetDefaultView(employees);
        view.SortDescriptions.Add(new SortDescription(nameof(Employee.Name), ListSortDirection.Ascending));
        view.Filter = Matches;
        List.ItemsSource = view;
    }

    private bool Matches(object item)
    {
        var employee = (Employee)item;
        var department = Department.SelectedIndex > 0 ? (string)Department.SelectedItem : null;
        return employee.Name.Contains(Search.Text, StringComparison.CurrentCultureIgnoreCase)
            && (department == null || employee.Department == department)
            && (ActiveOnly.IsChecked != true || employee.IsActive);
    }

    private void OnFilterChanged(object sender, RoutedEventArgs e) => view?.Refresh();

    private void OnSave(object sender, RoutedEventArgs e) => Saved.IsOpen = true;

    private async void OnDelete(object sender, RoutedEventArgs e) => await ConfirmDeleteAsync();

    /// <summary>Asks before removing the selected employee.</summary>
    public async Task ConfirmDeleteAsync()
    {
        if (List.SelectedItem is not Employee employee || Window.GetWindow(this) is not JetPackWindow window)
        {
            return;
        }
        var dialog = new JetPackDialog
        {
            Title = "Delete employee?",
            Content = $"{employee.Name} will be removed from the staff list. Approved timesheets stay in the archive.",
            AffirmativeText = "Delete",
            NegativeText = "Cancel",
            AffirmativeButtonStyle = (Style)FindResource("JetPack.Styles.Button.Orange"),
        };
        if (await window.ShowDialogAsync(dialog) == MessageDialogResult.Affirmative)
        {
            employees.Remove(employee);
        }
    }
}
