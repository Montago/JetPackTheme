using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JetPack.Wpf.Demo.Views;

public partial class ToolkitPage : UserControl
{
    public ToolkitPage()
    {
        InitializeComponent();
        Time.SelectedTime = new TimeSpan(8, 30, 0);
        DrawChart();
    }

    // Grouped columns, 0-200 hours mapped onto 125px.
    private void DrawChart()
    {
        int[][] data = { new[] { 120, 80, 60 }, new[] { 150, 95, 70 }, new[] { 135, 110, 55 }, new[] { 170, 120, 80 } };
        string[] quarters = { "K1", "K2", "K3", "K4" };
        const double bottom = 135, scale = 125.0 / 200, left = 30;
        var grid = new SolidColorBrush(Color.FromArgb(0x1A, 0, 0, 0));
        var label = new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60));

        foreach (var v in new[] { 0, 50, 100, 150, 200 })
        {
            var y = bottom - v * scale;
            Chart.Children.Add(new Line { X1 = left, X2 = 290, Y1 = y, Y2 = y, Stroke = v == 0 ? Brushes.Gray : grid });
            var t = new TextBlock { Text = v.ToString(), FontSize = 9, Foreground = label, Width = 24, TextAlignment = System.Windows.TextAlignment.Right };
            Canvas.SetLeft(t, 0);
            Canvas.SetTop(t, y - 6);
            Chart.Children.Add(t);
        }

        for (var q = 0; q < data.Length; q++)
        {
            var x0 = left + q * 65 + 11;
            for (var s = 0; s < 3; s++)
            {
                var h = data[q][s] * scale;
                var bar = new Rectangle { Width = 13, Height = h, RadiusX = 1, RadiusY = 1, ToolTip = $"{quarters[q]}: {data[q][s]} timer" };
                bar.SetResourceReference(Shape.FillProperty, $"JetPack.Brushes.Chart.{s + 1}");
                Canvas.SetLeft(bar, x0 + s * 15);
                Canvas.SetTop(bar, bottom - h);
                Chart.Children.Add(bar);
            }
            var name = new TextBlock { Text = quarters[q], FontSize = 10, Foreground = label };
            Canvas.SetLeft(name, x0 + 15);
            Canvas.SetTop(name, bottom + 4);
            Chart.Children.Add(name);
        }
    }
}
