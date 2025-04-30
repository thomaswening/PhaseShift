using System.Collections.ObjectModel;
using System.Windows;

namespace PhaseShift.UI.PomodoroFeature;

public partial class PomodoroWorkUnitsCtrl : System.Windows.Controls.UserControl
{
    public ObservableCollection<WorkUnit> WorkUnits
    {
        get => (ObservableCollection<WorkUnit>)GetValue(WorkUnitsProperty);
        set => SetValue(WorkUnitsProperty, value);
    }

    public static readonly DependencyProperty WorkUnitsProperty = DependencyProperty.Register(
        nameof(WorkUnits),
        typeof(ObservableCollection<WorkUnit>),
        typeof(PomodoroWorkUnitsCtrl),
        new PropertyMetadata(new ObservableCollection<WorkUnit>()));

    public int Count
    {
        get => (int)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public static readonly DependencyProperty CountProperty = DependencyProperty.Register(
        nameof(Count),
        typeof(int),
        typeof(PomodoroWorkUnitsCtrl),
        new PropertyMetadata(5, OnPropertyChanged));

    public int UnitsBeforeLongBreak
    {
        get => (int)GetValue(UnitsBeforeLongBreakProperty);
        set => SetValue(UnitsBeforeLongBreakProperty, value);
    }

    public static readonly DependencyProperty UnitsBeforeLongBreakProperty = DependencyProperty.Register(
        nameof(UnitsBeforeLongBreak),
        typeof(int),
        typeof(PomodoroWorkUnitsCtrl),
        new PropertyMetadata(4, OnPropertyChanged));

    public int CompletedUnits
    {
        get => (int)GetValue(CompletedUnitsProperty);
        set => SetValue(CompletedUnitsProperty, value);
    }

    public static readonly DependencyProperty CompletedUnitsProperty = DependencyProperty.Register(
        nameof(CompletedUnits),
        typeof(int),
        typeof(PomodoroWorkUnitsCtrl),
        new PropertyMetadata(0, OnPropertyChanged));

    public System.Windows.Media.Brush CompletedUnitColor
    {
        get => (System.Windows.Media.Brush)GetValue(CompletedUnitColorProperty);
        set => SetValue(CompletedUnitColorProperty, value);
    }

    public static readonly DependencyProperty CompletedUnitColorProperty = DependencyProperty.Register(
        nameof(CompletedUnitColor),
        typeof(System.Windows.Media.Brush),
        typeof(PomodoroWorkUnitsCtrl),
        new PropertyMetadata(System.Windows.Media.Brushes.Green, OnPropertyChanged));

    public bool ShortBreakEqualsLongBreak
    {
        get => (bool)GetValue(ShortBreakEqualsLongBreakProperty);
        set => SetValue(ShortBreakEqualsLongBreakProperty, value);
    }

    public static readonly DependencyProperty ShortBreakEqualsLongBreakProperty = DependencyProperty.Register(
        nameof(ShortBreakEqualsLongBreak),
        typeof(bool),
        typeof(PomodoroWorkUnitsCtrl),
        new PropertyMetadata(false, OnPropertyChanged));

    public int MaxCirclesPerRow
    {
        get => (int)GetValue(MaxCirclesPerRowProperty);
        set => SetValue(MaxCirclesPerRowProperty, value);
    }

    public static readonly DependencyProperty MaxCirclesPerRowProperty = DependencyProperty.Register(
        nameof(MaxCirclesPerRow),
        typeof(int),
        typeof(PomodoroWorkUnitsCtrl),
        new PropertyMetadata(14, OnPropertyChanged));

    public int Rows
    {
        get => (int)GetValue(RowsProperty);
        set => SetValue(RowsProperty, value);
    }

    public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(
        nameof(Rows),
        typeof(int),
        typeof(PomodoroWorkUnitsCtrl),
        new PropertyMetadata(1));

    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
        nameof(Columns),
        typeof(int),
        typeof(PomodoroWorkUnitsCtrl),
        new PropertyMetadata(1));

    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
        nameof(ItemTemplate),
        typeof(DataTemplate),
        typeof(PomodoroWorkUnitsCtrl),
        new PropertyMetadata(null, OnItemTemplateChanged));

    private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PomodoroWorkUnitsCtrl control)
        {
            control.ApplyTemplate();
        }
    }

    public PomodoroWorkUnitsCtrl()
    {
        InitializeComponent();
        UpdateWorkUnits();
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PomodoroWorkUnitsCtrl control)
        {
            control.UpdateWorkUnits();
        }
    }

    private void UpdateWorkUnits()
    {
        WorkUnits.Clear();

        var positionInRow = 0;
        var currentColumn = 0;
        var currentRow = 0;

        for (int i = 0; i < Count; i++)
        {
            AddWorkUnit(i, currentRow, currentColumn);

            positionInRow++;
            currentColumn++;

            if (ShouldAddBreakIndicator(i, positionInRow))
            {
                AddBreakIndicator(currentRow, currentColumn);

                positionInRow++;
                currentColumn++;
            }

            if (positionInRow >= MaxCirclesPerRow)
            {
                positionInRow = 0;
                currentColumn = 0;
                currentRow++;
            }
        }

        int totalCells = CalculateTotalCells();
        int rowsRequired = (totalCells + MaxCirclesPerRow - 1) / MaxCirclesPerRow; // Ceiling division
        UpdateGridDimensions(rowsRequired, totalCells);
    }

    private int CalculateTotalCells()
    {
        if (ShortBreakEqualsLongBreak)
        {
            return Count;
        }

        // Add an extra unit for each long break, the last unit is not followed by a break
        return Count + (Count - 1) / UnitsBeforeLongBreak;
    }

    private void AddWorkUnit(int index, int row, int column)
    {
        WorkUnits.Add(new WorkUnit
        {
            Color = index < CompletedUnits ? CompletedUnitColor : Foreground,
            Row = row,
            Column = column
        });
    }

    private bool ShouldAddBreakIndicator(int index, int positionInRow)
    {
        return !ShortBreakEqualsLongBreak
               && index != Count - 1 // Do not add a break indicator for the last unit
               && positionInRow < MaxCirclesPerRow // Do not add a break indicator if the row is full
               && index % UnitsBeforeLongBreak == UnitsBeforeLongBreak - 1;
    }

    private void AddBreakIndicator(int row, int column)
    {
        WorkUnits.Add(new WorkUnit
        {
            Color = System.Windows.Media.Brushes.Transparent,
            Row = row,
            Column = column
        });
    }

    private void UpdateGridDimensions(int rowsRequired, int totalCircles)
    {
        Rows = rowsRequired;
        Columns = MaxCirclesPerRow;

        if (totalCircles < MaxCirclesPerRow)
        {
            Columns = totalCircles;
        }
    }
}

public class WorkUnit
{
    public System.Windows.Media.Brush Color { get; set; } = System.Windows.Media.Brushes.Gray;
    public int Row { get; set; }
    public int Column { get; set; }
}
