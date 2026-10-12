using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev;

public sealed class F_OrderManagement : Window
{
    private readonly DataGrid grid = new();
    private readonly TextBox search = new();
    private readonly ComboBox customerFilter = new();
    private readonly DatePicker fromDate = new();
    private readonly DatePicker toDate = new();
    private readonly ComboBox officeFilter = new();
    private readonly ComboBox state = new();
    private readonly ComboBox managementFlag = new();
    private readonly Popup filterPopup = new();
    private readonly TextBlock resultCount = new();
    private readonly TextBlock listCount = new();
    private readonly Border filtersCard = new();
    private readonly Button collapseButton = new();

    public F_OrderManagement()
    {
        Title = "受注管理（一覧・検索・確定・非表示）";
        Width = 1100;
        Height = 680;
        FontFamily = new FontFamily("Meiryo");
        BuildUi();
        LoadFilterMasters();
        LoadRows();
    }

    private void BuildUi()
    {
        Background = new SolidColorBrush(Color.FromRgb(246, 248, 251));
        var root = new Grid { Margin = new Thickness(14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());

        var titleBar = new Border { Background = Brushes.White, Padding = new Thickness(14, 10, 14, 10), BorderBrush = new SolidColorBrush(Color.FromRgb(225, 231, 238)), BorderThickness = new Thickness(0, 0, 0, 1) };
        var titleRow = new DockPanel();
        var title = new TextBlock { Text = "受注検索機能（検索・絞り込み条件）", FontSize = 17, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(27, 55, 86)) };
        DockPanel.SetDock(title, Dock.Left);
        titleRow.Children.Add(title);
        resultCount.Margin = new Thickness(12, 0, 0, 0);
        resultCount.Padding = new Thickness(9, 3, 9, 3);
        resultCount.Background = new SolidColorBrush(Color.FromRgb(231, 241, 255));
        resultCount.FontSize = 11;
        resultCount.FontWeight = FontWeights.SemiBold;
        resultCount.VerticalAlignment = VerticalAlignment.Center;
        resultCount.Foreground = new SolidColorBrush(Color.FromRgb(38, 105, 170));
        titleRow.Children.Add(resultCount);
        collapseButton.Content = "折りたたむ ︿";
        collapseButton.Width = 76;
        collapseButton.Height = 24;
        collapseButton.MinWidth = 0;
        collapseButton.Padding = new Thickness(4, 1, 4, 1);
        collapseButton.Margin = new Thickness(4, 0, 0, 0);
        collapseButton.FontSize = 10;
        collapseButton.Background = Brushes.Transparent;
        collapseButton.BorderBrush = Brushes.Transparent;
        collapseButton.Foreground = new SolidColorBrush(Color.FromRgb(85, 105, 125));
        collapseButton.Click += ToggleFilters;
        DockPanel.SetDock(collapseButton, Dock.Right);
        titleRow.Children.Add(collapseButton);
        if (AppSession.IsAdmin || AppSession.IsSales)
        {
            var registration = Button("⊕ 受注登録", OpenRegistration, new Thickness(4, 0, 8, 0));
            registration.Background = new SolidColorBrush(Color.FromRgb(31, 103, 232));
            registration.Foreground = Brushes.White;
            registration.BorderBrush = new SolidColorBrush(Color.FromRgb(31, 103, 232));
            registration.FontWeight = FontWeights.Bold;
            registration.Width = 104;
            registration.Height = 28;
            registration.MinWidth = 0;
            registration.Padding = new Thickness(6, 2, 6, 2);
            registration.FontSize = 10;
            DockPanel.SetDock(registration, Dock.Right);
            titleRow.Children.Add(registration);
        }
        titleBar.Child = titleRow;
        Grid.SetRow(titleBar, 0);
        root.Children.Add(titleBar);

        filtersCard.Background = Brushes.White;
        filtersCard.Padding = new Thickness(12);
        filtersCard.Margin = new Thickness(0, 8, 0, 8);
        filtersCard.BorderBrush = new SolidColorBrush(Color.FromRgb(225, 231, 238));
        filtersCard.BorderThickness = new Thickness(1);
        var filterGrid = new Grid();
        for (var i = 0; i < 4; i++) filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        filterGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        filterGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        filterGrid.Children.Add(Field("顧客名・顧客コード", customerFilter, 0, 0));
        var dateField = Field("受注期間（受注日）", DateRangePanel(), 1, 0);
        Grid.SetColumnSpan(dateField, 2);
        filterGrid.Children.Add(dateField);
        var officeField = Field("受付営業所", officeFilter, 3, 0);
        filterGrid.Children.Add(officeField);
        filterGrid.Children.Add(Field("受注状態フラグ", state, 0, 1));
        filterGrid.Children.Add(Field("受注管理フラグ（表示/非表示）", managementFlag, 1, 1));
        search.Width = double.NaN;
        search.MinWidth = 180;
        search.ToolTip = "受注番号、品名、品番、備考、担当者名など";
        search.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) LoadRows(); };
        filterGrid.Children.Add(Field("フリーキーワード検索", search, 2, 1));
        var filterActions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
        filterActions.Children.Add(Button("検索", (_, _) => LoadRows(), new Thickness(4)));
        filterActions.Children.Add(Button("条件クリア", ClearFilters, new Thickness(4)));
        Grid.SetColumn(filterActions, 3);
        Grid.SetRow(filterActions, 1);
        filterGrid.Children.Add(filterActions);
        filtersCard.Child = filterGrid;
        Grid.SetRow(filtersCard, 1);
        root.Children.Add(filtersCard);

        var listHeader = new DockPanel { Margin = new Thickness(2, 0, 2, 6) };
        var listTitle = new TextBlock { Text = "▣ 受注一覧表示", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(32, 55, 78)) };
        listCount.Margin = new Thickness(8, 0, 0, 0);
        listCount.VerticalAlignment = VerticalAlignment.Center;
        listCount.Foreground = new SolidColorBrush(Color.FromRgb(82, 103, 124));
        listCount.FontSize = 12;
        var listTitlePanel = new StackPanel { Orientation = Orientation.Horizontal };
        listTitlePanel.Children.Add(listTitle);
        listTitlePanel.Children.Add(listCount);
        DockPanel.SetDock(listTitlePanel, Dock.Left);
        listHeader.Children.Add(listTitlePanel);
        var rightHeader = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        rightHeader.Children.Add(Button("選択を確定", ConfirmSelected, new Thickness(2, 0, 2, 0)));
        rightHeader.Children.Add(Button("選択を非表示", HideSelected, new Thickness(2, 0, 8, 0)));
        var legend = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        legend.Children.Add(Legend("●", "未確定（受付中）", Colors.Gold));
        legend.Children.Add(Legend("●", "確定済み受注", Colors.MediumSeaGreen));
        legend.Children.Add(Legend("●", "非表示", Colors.MediumPurple));
        rightHeader.Children.Add(legend);
        DockPanel.SetDock(rightHeader, Dock.Right);
        listHeader.Children.Add(rightHeader);
        Grid.SetRow(listHeader, 2);
        root.Children.Add(listHeader);

        grid.AutoGenerateColumns = false;
        grid.IsReadOnly = false;
        grid.CanUserAddRows = false;
        grid.SelectionMode = DataGridSelectionMode.Extended;
        grid.RowHeight = 38;
        grid.ColumnHeaderHeight = 38;
        grid.HeadersVisibility = DataGridHeadersVisibility.Column;
        grid.Background = Brushes.White;
        grid.BorderBrush = new SolidColorBrush(Color.FromRgb(225, 231, 238));
        grid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
        grid.HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(232, 236, 241));
        grid.RowBackground = Brushes.White;
        grid.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(250, 252, 254));
        AddGridColumns();
        grid.MouseDoubleClick += (_, _) => ShowSelectedDetails();
        Grid.SetRow(grid, 3);
        root.Children.Add(grid);
        Content = root;
    }

    private void AddGridColumns()
    {
        grid.Columns.Add(new DataGridCheckBoxColumn
        {
            Header = "選択",
            Binding = new Binding(nameof(OrderListRow.選択)),
            Width = 48,
            IsReadOnly = false
        });
        AddTextColumn("受注番号", nameof(OrderListRow.受注番号), 105);
        AddTextColumn("受注日／納期", nameof(OrderListRow.受注日), 105, "yyyy-MM-dd");
        AddTextColumn("顧客名／営業所", nameof(OrderListRow.顧客), 165);
        AddTextColumn("営業所", nameof(OrderListRow.営業所), 120);
        AddTextColumn("受注明細概要", nameof(OrderListRow.明細数), 120, "明細 {0} 件");
        AddTextColumn("受注合計（税込）", nameof(OrderListRow.合計金額), 125, "¥#,##0");
        AddTextColumn("受注状態", nameof(OrderListRow.状態), 115);
        AddTextColumn("管理フラグ", nameof(OrderListRow.表示), 110);
        AddTextColumn("担当者", nameof(OrderListRow.担当者), 100);
        grid.Columns.Add(CreateActionColumn());
    }

    private void AddTextColumn(string header, string propertyName, double width, string? format = null)
    {
        var binding = new Binding(propertyName);
        if (!string.IsNullOrWhiteSpace(format)) binding.StringFormat = format;
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = binding,
            Width = width,
            IsReadOnly = true
        });
    }

    private DataGridTemplateColumn CreateActionColumn()
    {
        var panel = new FrameworkElementFactory(typeof(StackPanel));
        panel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

        var confirm = new FrameworkElementFactory(typeof(System.Windows.Controls.Button));
        confirm.SetValue(System.Windows.Controls.Button.ContentProperty, "確定");
        confirm.SetValue(System.Windows.Controls.Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 166, 112)));
        confirm.SetValue(System.Windows.Controls.Button.ForegroundProperty, Brushes.White);
        confirm.SetValue(System.Windows.Controls.Button.BorderBrushProperty, Brushes.Transparent);
        confirm.SetValue(System.Windows.Controls.Button.PaddingProperty, new Thickness(7, 3, 7, 3));
        confirm.SetValue(System.Windows.Controls.Button.MarginProperty, new Thickness(2));
        confirm.AddHandler(System.Windows.Controls.Button.ClickEvent, new RoutedEventHandler(RowConfirmClick));
        panel.AppendChild(confirm);

        var hide = new FrameworkElementFactory(typeof(System.Windows.Controls.Button));
        hide.SetValue(System.Windows.Controls.Button.ContentProperty, "非表示");
        hide.SetValue(System.Windows.Controls.Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(247, 239, 255)));
        hide.SetValue(System.Windows.Controls.Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(123, 67, 184)));
        hide.SetValue(System.Windows.Controls.Button.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(210, 180, 239)));
        hide.SetValue(System.Windows.Controls.Button.PaddingProperty, new Thickness(7, 3, 7, 3));
        hide.SetValue(System.Windows.Controls.Button.MarginProperty, new Thickness(2));
        hide.AddHandler(System.Windows.Controls.Button.ClickEvent, new RoutedEventHandler(RowHideClick));
        panel.AppendChild(hide);

        var details = new FrameworkElementFactory(typeof(System.Windows.Controls.Button));
        details.SetValue(System.Windows.Controls.Button.ContentProperty, "↗");
        details.SetValue(System.Windows.Controls.Button.ToolTipProperty, "受注明細を表示");
        details.SetValue(System.Windows.Controls.Button.PaddingProperty, new Thickness(5, 3, 5, 3));
        details.SetValue(System.Windows.Controls.Button.MarginProperty, new Thickness(2));
        details.AddHandler(System.Windows.Controls.Button.ClickEvent, new RoutedEventHandler(RowDetailsClick));
        panel.AppendChild(details);

        return new DataGridTemplateColumn
        {
            Header = "操作（確定／非表示）",
            CellTemplate = new DataTemplate { VisualTree = panel },
            Width = new DataGridLength(235)
        };
    }

    private static FrameworkElement Field(string label, UIElement control, int column, int row)
    {
        if (control is FrameworkElement element) element.Margin = new Thickness(4, 2, 8, 2);
        if (control is ComboBox combo) combo.HorizontalContentAlignment = HorizontalAlignment.Left;
        var panel = new StackPanel { Margin = new Thickness(2, 2, 2, 6) };
        panel.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(74, 88, 103)), Margin = new Thickness(4, 0, 4, 2) });
        panel.Children.Add(control);
        Grid.SetColumn(panel, column);
        Grid.SetRow(panel, row);
        return panel;
    }

    private static TextBlock Legend(string marker, string text, Color color)
    {
        return new TextBlock { Text = $"{marker} {text}", Foreground = new SolidColorBrush(color), FontSize = 11, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
    }

    private static Border FilterLine(string label, UIElement control)
    {
        if (control is FrameworkElement element) element.Margin = new Thickness(4);
        if (control is ComboBox combo) combo.Width = 360;
        return new Border { Child = new StackPanel { Children = { new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, 6, 4, 0) }, control } } };
    }

    private StackPanel DateRangePanel()
    {
        fromDate.Width = 135;
        toDate.Width = 135;
        fromDate.ToolTip = "受注日の開始日";
        toDate.ToolTip = "受注日の終了日";
        var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(fromDate);
        panel.Children.Add(new TextBlock { Text = " ～ ", VerticalAlignment = VerticalAlignment.Center });
        panel.Children.Add(toDate);
        panel.Children.Add(CompactButton("本日", (_, _) => SetDateRange(DateTime.Today, DateTime.Today)));
        panel.Children.Add(CompactButton("直近7日", (_, _) => SetDateRange(DateTime.Today.AddDays(-6), DateTime.Today)));
        panel.Children.Add(CompactButton("今月", (_, _) => SetDateRange(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today)));
        panel.Children.Add(CompactButton("全期間", (_, _) => SetDateRange(null, null)));
        return panel;
    }

    private void SetDateRange(DateTime? from, DateTime? to)
    {
        fromDate.SelectedDate = from;
        toDate.SelectedDate = to;
        LoadRows();
    }

    private void ToggleFilters(object? sender, RoutedEventArgs e)
    {
        var isVisible = filtersCard.Visibility == Visibility.Visible;
        filtersCard.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
        collapseButton.Content = isVisible ? "展開する ﹀" : "折りたたむ ︿";
    }

    private void OpenRegistration(object? sender, RoutedEventArgs e)
    {
        if (!(AppSession.IsAdmin || AppSession.IsSales))
        {
            MessageBox.Show("受注登録は管理者または営業担当者のみ実行できます。", "権限エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var registration = new F_OrderRegistration
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        registration.ShowDialog();
        LoadFilterMasters();
        LoadRows();
    }

    private static Button Button(string text, RoutedEventHandler click, Thickness margin)
    {
        var button = new Button { Content = text, Padding = new Thickness(12, 6, 12, 6), Margin = margin, MinWidth = 90 };
        button.Click += click;
        return button;
    }

    private static Button CompactButton(string text, RoutedEventHandler click)
    {
        var button = Button(text, click, new Thickness(2, 0, 0, 0));
        button.MinWidth = 0;
        button.Padding = new Thickness(6, 4, 6, 4);
        button.FontSize = 11;
        return button;
    }

    private void LoadRows()
    {
        try
        {
            using var db = new SalesManagementContext();
            var query = db.TOrders
                .Include(x => x.Cl)
                .Include(x => x.So)
                .Include(x => x.Em)
                .Include(x => x.TOrderDetails)
                .ThenInclude(x => x.Pr)
                .AsQueryable();

            if (!AppSession.IsAdmin) query = query.Where(x => x.SoId == AppSession.Employee!.SoId);
            var totalCount = query.Count();
            if (fromDate.SelectedDate.HasValue) query = query.Where(x => x.OrDate >= fromDate.SelectedDate.Value);
            if (toDate.SelectedDate.HasValue) query = query.Where(x => x.OrDate < toDate.SelectedDate.Value.AddDays(1));
            if (customerFilter.SelectedItem is FilterItem customer && customer.Id >= 0) query = query.Where(x => x.ClId == customer.Id);
            if (officeFilter.SelectedItem is FilterItem office && office.Id >= 0) query = query.Where(x => x.SoId == office.Id);
            if (state.SelectedItem is FilterItem selected && selected.Id >= 0) query = query.Where(x => (x.OrStateFlag ?? 0) == selected.Id);
            if (managementFlag.SelectedItem is FilterItem management && management.Id >= 0) query = query.Where(x => x.OrFlag == management.Id);

            var keyword = search.Text.Trim();
            if (keyword.Length > 0)
            {
                var orderId = int.TryParse(keyword, out var parsedOrderId) ? parsedOrderId : -1;
                query = query.Where(x => x.OrId == orderId || x.Cl.ClName.Contains(keyword) || x.Em.EmName.Contains(keyword) || (x.OrHidden ?? "").Contains(keyword) || x.TOrderDetails.Any(d => d.Pr.PrName.Contains(keyword) || (d.Pr.PrJcode ?? "").Contains(keyword)));
            }

            var rows = query
                .OrderByDescending(x => x.OrDate)
                .ThenByDescending(x => x.OrId)
                .Select(x => new OrderListRow
                {
                    選択 = false,
                    受注番号 = x.OrId,
                    受注日 = x.OrDate,
                    顧客 = x.Cl.ClName,
                    営業所 = x.So.SoName,
                    担当者 = x.Em.EmName,
                    明細数 = x.TOrderDetails.Count,
                    合計金額 = x.TOrderDetails.Sum(d => d.OrTotalPrice),
                    状態 = (x.OrStateFlag ?? 0) == 1 ? "確定" : "未確定",
                    表示 = x.OrFlag == 2 ? "非表示" : "表示"
                })
                .ToList();
            grid.ItemsSource = rows;
            resultCount.Text = $"{rows.Count} 件表示 / 全 {totalCount} 件";
            listCount.Text = $"（{rows.Count} 件）";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"受注一覧の取得に失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadFilterMasters()
    {
        using var db = new SalesManagementContext();
        customerFilter.ItemsSource = new[] { new FilterItem(-1, "すべて") }.Concat(db.MClients.Where(x => x.ClFlag == 0).OrderBy(x => x.ClName).Select(x => new FilterItem(x.ClId, x.ClName))).ToList();
        officeFilter.ItemsSource = new[] { new FilterItem(-1, "すべて") }.Concat(db.MSalesOffices.Where(x => x.SoFlag == 0).OrderBy(x => x.SoName).Select(x => new FilterItem(x.SoId, x.SoName))).ToList();
        state.ItemsSource = new[] { new FilterItem(-1, "すべて"), new FilterItem(0, "未確定"), new FilterItem(1, "確定") };
        managementFlag.ItemsSource = new[] { new FilterItem(-1, "すべて"), new FilterItem(0, "表示"), new FilterItem(2, "非表示") };
        customerFilter.SelectedIndex = 0;
        officeFilter.SelectedIndex = 0;
        state.SelectedIndex = 0;
        managementFlag.SelectedIndex = 0;
    }

    private void ClearFilters(object? sender, RoutedEventArgs e)
    {
        search.Clear();
        fromDate.SelectedDate = null;
        toDate.SelectedDate = null;
        customerFilter.SelectedIndex = 0;
        officeFilter.SelectedIndex = 0;
        state.SelectedIndex = 0;
        managementFlag.SelectedIndex = 0;
        filterPopup.IsOpen = false;
        LoadRows();
    }

    private List<OrderListRow> SelectedRows()
    {
        var checkedRows = grid.Items.OfType<OrderListRow>().Where(x => x.選択).ToList();
        if (checkedRows.Count > 0) return checkedRows;
        return grid.SelectedItem is OrderListRow selected ? new List<OrderListRow> { selected } : new List<OrderListRow>();
    }

    private OrderListRow? SelectedRow() => SelectedRows().FirstOrDefault();

    private void ConfirmSelected(object? sender, RoutedEventArgs e)
    {
        ConfirmRows(SelectedRows());
    }

    private void ConfirmRows(IEnumerable<OrderListRow> selectedRows)
    {
        var rows = selectedRows.ToList();
        if (rows.Count == 0) { MessageBox.Show("受注をチェックまたは選択してください。"); return; }
        var messages = rows.Select(row => BusinessService.ConfirmOrder(row.受注番号, AppSession.Employee!.EmId, out var message) ? $"受注ID {row.受注番号}: {message}" : $"受注ID {row.受注番号}: エラー {message}").ToList();
        MessageBox.Show(string.Join(Environment.NewLine, messages), "受注確定結果", MessageBoxButton.OK, MessageBoxImage.Information);
        LoadRows();
    }

    private void RowConfirmClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is OrderListRow row) ConfirmRows(new[] { row });
    }

    private void HideSelected(object? sender, RoutedEventArgs e)
    {
        HideRows(SelectedRows());
    }

    private void HideRows(IEnumerable<OrderListRow> selectedRows)
    {
        var rows = selectedRows.ToList();
        if (rows.Count == 0) { MessageBox.Show("受注をチェックまたは選択してください。"); return; }
        if (!AppSession.IsAdmin) { MessageBox.Show("受注の非表示は管理者のみ実行できます。"); return; }
        var reason = Microsoft.VisualBasic.Interaction.InputBox("非表示理由を入力してください。", "受注を非表示", "");
        if (string.IsNullOrWhiteSpace(reason)) return;
        using var db = new SalesManagementContext();
        foreach (var row in rows)
        {
            var order = db.TOrders.Find(row.受注番号);
            if (order == null) continue;
            order.OrFlag = 2;
            order.OrHidden = reason.Trim();
        }
        db.SaveChanges();
        LoadRows();
    }

    private void RowHideClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is OrderListRow row) HideRows(new[] { row });
    }

    private void ShowSelectedDetails()
    {
        var row = SelectedRow();
        if (row == null) return;
        ShowDetails(row);
    }

    private void RowDetailsClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is OrderListRow row) ShowDetails(row);
    }

    private void ShowDetails(OrderListRow row)
    {
        using var db = new SalesManagementContext();
        var order = db.TOrders
            .AsNoTracking()
            .Include(x => x.Cl)
            .Include(x => x.So)
            .Include(x => x.Em)
            .Include(x => x.TOrderDetails)
            .ThenInclude(x => x.Pr)
            .SingleOrDefault(x => x.OrId == row.受注番号);
        if (order == null) return;

        var snapshot = new OrderDetailSnapshot
        {
            OrderId = order.OrId,
            OrderDate = order.OrDate,
            OfficeName = order.So.SoName,
            CustomerId = order.ClId,
            CustomerName = order.Cl.ClName,
            CustomerAddress = order.Cl.ClAddress,
            CustomerPhone = order.Cl.ClPhone,
            CustomerContact = order.ClCharge,
            EmployeeName = order.Em.EmName,
            StateFlag = order.OrStateFlag ?? 0,
            ManagementFlag = order.OrFlag,
            HiddenReason = order.OrHidden,
            Lines = order.TOrderDetails
                .OrderBy(x => x.OrDetailId)
                .Select(x => new OrderDetailLine
                {
                    ProductCode = x.Pr.PrJcode ?? x.Pr.PrModelNumber,
                    ProductName = x.Pr.PrName,
                    UnitPrice = x.OrQuantity == 0 ? x.Pr.Price : decimal.Round(x.OrTotalPrice / x.OrQuantity, 0),
                    Quantity = x.OrQuantity,
                    Subtotal = x.OrTotalPrice
                })
                .ToList()
        };

        new F_OrderDetail(snapshot, this).ShowDialog();
        LoadRows();
    }

    private sealed record FilterItem(int Id, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed class OrderListRow
    {
        public bool 選択 { get; set; }
        public int 受注番号 { get; init; }
        public DateTime 受注日 { get; init; }
        public string 顧客 { get; init; } = "";
        public string 営業所 { get; init; } = "";
        public string 担当者 { get; init; } = "";
        public int 明細数 { get; init; }
        public decimal 合計金額 { get; init; }
        public string 状態 { get; init; } = "";
        public string 表示 { get; init; } = "";
    }
}

public sealed class OrderDetailSnapshot
{
    public int OrderId { get; init; }
    public DateTime OrderDate { get; init; }
    public string OfficeName { get; init; } = "";
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public string CustomerAddress { get; init; } = "";
    public string CustomerPhone { get; init; } = "";
    public string? CustomerContact { get; init; }
    public string EmployeeName { get; init; } = "";
    public int StateFlag { get; init; }
    public int ManagementFlag { get; init; }
    public string? HiddenReason { get; init; }
    public List<OrderDetailLine> Lines { get; init; } = new();
}

public sealed class OrderDetailLine
{
    public string ProductCode { get; init; } = "";
    public string ProductName { get; init; } = "";
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal Subtotal { get; init; }
}

public sealed class F_OrderDetail : Window
{
    private static readonly SolidColorBrush Navy = Brush("#101C36");
    private static readonly SolidColorBrush Blue = Brush("#2167E8");
    private static readonly SolidColorBrush Text = Brush("#25364C");
    private static readonly SolidColorBrush Muted = Brush("#71829A");
    private static readonly SolidColorBrush Border = Brush("#E0E7F0");
    private readonly OrderDetailSnapshot data;

    public F_OrderDetail(OrderDetailSnapshot data, Window owner)
    {
        this.data = data;
        Owner = owner;
        Title = $"受注詳細 - ORD-{data.OrderId:000000}";
        Width = 820;
        Height = 720;
        MinWidth = 720;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;
        Background = Brush("#F3F6FA");
        FontFamily = new FontFamily("Meiryo");
        BuildUi();
    }

    private void BuildUi()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(BuildHeader());

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(20, 18, 20, 18)
        };
        scroll.Content = BuildBody();
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        var footer = new Border { Background = Brush("#EEF3F8"), Padding = new Thickness(20, 10, 20, 10) };
        var footerRow = new DockPanel();
        var footerId = new TextBlock
        {
            Text = $"ID: SO-{data.OrderId:0000000000}  |  作成: {data.OrderDate:yyyy-MM-dd HH:mm:ss}",
            Foreground = Muted,
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center
        };
        DockPanel.SetDock(footerId, Dock.Left);
        footerRow.Children.Add(footerId);
        var close = DetailButton("閉じる", (_, _) => Close(), Navy, Brushes.White, 82);
        DockPanel.SetDock(close, Dock.Right);
        footerRow.Children.Add(close);
        footer.Child = footerRow;
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        Content = root;
    }

    private UIElement BuildHeader()
    {
        var header = new Border { Background = Navy, Padding = new Thickness(20, 12, 16, 12) };
        var row = new DockPanel();
        var number = new Border { Background = Blue, CornerRadius = new CornerRadius(4), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 10, 0) };
        number.Child = new TextBlock { Text = $"ORD-{data.OrderId:000000}", Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.Bold };
        DockPanel.SetDock(number, Dock.Left);
        row.Children.Add(number);

        var title = new StackPanel();
        title.Children.Add(new TextBlock { Text = "受注詳細・ステータス管理", Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeights.Bold });
        title.Children.Add(new TextBlock { Text = $"受付営業所: {data.OfficeName} | 登録日: {data.OrderDate:yyyy-MM-dd}", Foreground = Brush("#AAB8D0"), FontSize = 10, Margin = new Thickness(0, 2, 0, 0) });
        DockPanel.SetDock(title, Dock.Left);
        row.Children.Add(title);

        var close = new Button { Content = "×", Width = 28, Height = 28, Padding = new Thickness(0), Margin = new Thickness(8, 0, 0, 0), Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Foreground = Brush("#C4D0E4"), FontSize = 20 };
        close.Click += (_, _) => Close();
        DockPanel.SetDock(close, Dock.Right);
        row.Children.Add(close);
        var print = DetailButton("♧ 伝票印刷", PrintSlip, Brush("#24334F"), Brushes.White, 96);
        DockPanel.SetDock(print, Dock.Right);
        row.Children.Add(print);
        header.Child = row;
        return header;
    }

    private UIElement BuildBody()
    {
        var body = new StackPanel();
        body.Children.Add(BuildStatusCard());

        var infoGrid = new Grid { Margin = new Thickness(0, 16, 0, 16) };
        infoGrid.ColumnDefinitions.Add(new ColumnDefinition());
        infoGrid.ColumnDefinitions.Add(new ColumnDefinition());
        var customer = Card("▣  顧客情報");
        AddCardBody(customer, BuildCustomerInfo());
        Grid.SetColumn(customer, 0);
        infoGrid.Children.Add(customer);
        var order = Card("▣  受注・納期情報");
        AddCardBody(order, BuildOrderInfo());
        Grid.SetColumn(order, 1);
        infoGrid.Children.Add(order);
        body.Children.Add(infoGrid);

        body.Children.Add(BuildDetailsCard());
        body.Children.Add(BuildHistoryCard());
        return body;
    }

    private UIElement BuildStatusCard()
    {
        var card = new Border { Background = Brushes.White, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(12) };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(StatusBlock("受注状態フラグ", data.StateFlag == 1 ? "確定済み受注" : "未確定（受付中）", data.StateFlag == 1 ? "#E6F7F0" : "#FFF4D8", data.StateFlag == 1 ? "#008F65" : "#B77900", 0));
        grid.Children.Add(StatusBlock("受注管理フラグ", data.ManagementFlag == 2 ? "非表示" : "通常表示", data.ManagementFlag == 2 ? "#F5E9FF" : "#EAF0F8", data.ManagementFlag == 2 ? "#8D38D2" : "#5E718C", 1));
        var confirm = DetailButton("✓ 受注確定", ConfirmOrder, Brush("#009E6D"), Brushes.White, 142);
        Grid.SetColumn(confirm, 2);
        confirm.Margin = new Thickness(8, 2, 5, 2);
        grid.Children.Add(confirm);
        var hide = DetailButton("♢ 非表示にする", HideOrder, Brush("#F7EEFF"), Brush("#8A35D5"), 164);
        hide.BorderBrush = Brush("#D9B8F3");
        Grid.SetColumn(hide, 3);
        hide.Margin = new Thickness(5, 2, 0, 2);
        grid.Children.Add(hide);
        card.Child = grid;
        return card;
    }

    private UIElement BuildCustomerInfo()
    {
        var panel = new StackPanel();
        panel.Children.Add(InfoRow("顧客コード", $"C-{data.CustomerId:0000}"));
        panel.Children.Add(InfoRow("企業名", data.CustomerName, true));
        panel.Children.Add(InfoRow("受付営業所", data.OfficeName));
        panel.Children.Add(InfoRow("担当営業員", data.EmployeeName, true));
        if (!string.IsNullOrWhiteSpace(data.CustomerAddress)) panel.Children.Add(InfoRow("住所", data.CustomerAddress));
        if (!string.IsNullOrWhiteSpace(data.CustomerPhone)) panel.Children.Add(InfoRow("電話番号", data.CustomerPhone));
        return panel;
    }

    private UIElement BuildOrderInfo()
    {
        var panel = new StackPanel();
        panel.Children.Add(InfoRow("受注日", data.OrderDate.ToString("yyyy-MM-dd")));
        panel.Children.Add(InfoRow("納品希望期日", "未設定", true, Blue));
        panel.Children.Add(InfoRow("確定処理日時", data.StateFlag == 1 ? "確定済み" : "未確定"));
        panel.Children.Add(InfoRow("特記事項・備考", string.IsNullOrWhiteSpace(data.CustomerContact) ? "（特記事項なし）" : data.CustomerContact!, false, TextBoxBackground: true));
        if (data.ManagementFlag == 2 && !string.IsNullOrWhiteSpace(data.HiddenReason)) panel.Children.Add(InfoRow("非表示理由", data.HiddenReason!));
        return panel;
    }

    private UIElement BuildDetailsCard()
    {
        var totalQuantity = data.Lines.Sum(x => x.Quantity);
        var subtotal = data.Lines.Sum(x => x.Subtotal);
        var tax = Math.Round(subtotal * 0.1m, 0, MidpointRounding.AwayFromZero);
        var total = subtotal + tax;

        var card = Card($"◇  受注明細品目 ({data.Lines.Count} 品)");
        var panel = new StackPanel();
        var totalLabel = new TextBlock { Text = $"総数量 {totalQuantity:N0} 個", Foreground = Muted, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, -25, 0, 8) };
        panel.Children.Add(totalLabel);
        panel.Children.Add(BuildDetailHeader());
        if (data.Lines.Count == 0) panel.Children.Add(new TextBlock { Text = "明細はありません。", Foreground = Muted, Margin = new Thickness(10) });
        foreach (var line in data.Lines) panel.Children.Add(BuildDetailRow(line));
        var totalRow = new Grid { Background = Brush("#F4F7FB"), Margin = new Thickness(0, 1, 0, 0) };
        totalRow.ColumnDefinitions.Add(new ColumnDefinition());
        totalRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        totalRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        totalRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        totalRow.Children.Add(Cell("合計金額 (税込):", HorizontalAlignment.Right, true, 0));
        totalRow.Children.Add(Cell($"¥{subtotal:N0}", HorizontalAlignment.Right, false, 1));
        totalRow.Children.Add(Cell($"¥{tax:N0}", HorizontalAlignment.Right, false, 2));
        totalRow.Children.Add(Cell($"¥{total:N0}", HorizontalAlignment.Right, true, 3, Blue));
        panel.Children.Add(totalRow);
        AddCardBody(card, panel);
        return card;
    }

    private UIElement BuildDetailHeader()
    {
        var grid = DetailGrid(Brush("#F4F7FB"), 8);
        grid.Children.Add(Cell("#", HorizontalAlignment.Left, false, 0));
        grid.Children.Add(Cell("商品コード / 品名", HorizontalAlignment.Left, true, 1));
        grid.Children.Add(Cell("単価", HorizontalAlignment.Right, true, 2));
        grid.Children.Add(Cell("数量", HorizontalAlignment.Right, true, 3));
        grid.Children.Add(Cell("税抜小計", HorizontalAlignment.Right, true, 4));
        grid.Children.Add(Cell("消費税(10%)", HorizontalAlignment.Right, true, 5));
        grid.Children.Add(Cell("行合計(税込)", HorizontalAlignment.Right, true, 6));
        return grid;
    }

    private UIElement BuildDetailRow(OrderDetailLine line)
    {
        var tax = Math.Round(line.Subtotal * 0.1m, 0, MidpointRounding.AwayFromZero);
        var grid = DetailGrid(Brush("#FFFFFF"), 8);
        grid.Children.Add(Cell((data.Lines.IndexOf(line) + 1).ToString(), HorizontalAlignment.Left, false, 0));
        var product = new StackPanel { Margin = new Thickness(8, 6, 8, 6) };
        product.Children.Add(new TextBlock { Text = line.ProductCode, Foreground = Blue, FontSize = 9, FontWeight = FontWeights.Bold });
        product.Children.Add(new TextBlock { Text = line.ProductName, Foreground = Text, FontSize = 11, TextWrapping = TextWrapping.Wrap });
        Grid.SetColumn(product, 1);
        grid.Children.Add(product);
        grid.Children.Add(Cell($"¥{line.UnitPrice:N0}", HorizontalAlignment.Right, false, 2));
        grid.Children.Add(Cell($"{line.Quantity:N0} 個", HorizontalAlignment.Right, true, 3));
        grid.Children.Add(Cell($"¥{line.Subtotal:N0}", HorizontalAlignment.Right, false, 4));
        grid.Children.Add(Cell($"¥{tax:N0}", HorizontalAlignment.Right, false, 5, Muted));
        grid.Children.Add(Cell($"¥{line.Subtotal + tax:N0}", HorizontalAlignment.Right, true, 6));
        return grid;
    }

    private UIElement BuildHistoryCard()
    {
        var card = Card("◴  ステータス変更履歴・監査ログ");
        var panel = new StackPanel();
        var history = new Border { Background = Brushes.White, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(10), Margin = new Thickness(0, 4, 0, 0) };
        var historyPanel = new StackPanel();
        historyPanel.Children.Add(new TextBlock { Text = "●  新規受注登録", Foreground = Text, FontWeight = FontWeights.Bold, FontSize = 11 });
        historyPanel.Children.Add(new TextBlock { Text = $"受注登録 | 明細数: {data.Lines.Count}件, 総数量: {data.Lines.Sum(x => x.Quantity):N0}, 合計金額: ¥{data.Lines.Sum(x => x.Subtotal) * 1.1m:N0}", Foreground = Muted, FontSize = 10, Margin = new Thickness(14, 4, 0, 0) });
        historyPanel.Children.Add(new TextBlock { Text = $"実行担当者: {data.EmployeeName}", Foreground = Muted, FontSize = 10, Margin = new Thickness(14, 3, 0, 0) });
        history.Child = historyPanel;
        panel.Children.Add(history);
        if (data.StateFlag == 1) panel.Children.Add(HistoryNote("●  受注確定", "受注状態が確定済みに変更されています。", "#009E6D"));
        if (data.ManagementFlag == 2) panel.Children.Add(HistoryNote("●  受注非表示", $"理由: {data.HiddenReason ?? "理由未入力"}", "#8A35D5"));
        AddCardBody(card, panel);
        return card;
    }

    private static Border HistoryNote(string title, string detail, string color)
    {
        var border = new Border { Background = Brush("#FAFBFD"), BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0) };
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = title, Foreground = Brush(color), FontWeight = FontWeights.Bold, FontSize = 11 });
        panel.Children.Add(new TextBlock { Text = detail, Foreground = Muted, FontSize = 10, Margin = new Thickness(14, 3, 0, 0) });
        border.Child = panel;
        return border;
    }

    private Border Card(string title)
    {
        var border = new Border { Background = Brushes.White, BorderBrush = Border, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(12), Margin = new Thickness(0) };
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = title, Foreground = Text, FontSize = 12, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 9) });
        border.Child = panel;
        return border;
    }

    private static void AddCardBody(Border card, UIElement body)
    {
        if (card.Child is StackPanel panel) panel.Children.Add(body);
        else card.Child = body;
    }

    private static Border StatusBlock(string label, string value, string background, string foreground, int column)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = label, Foreground = Muted, FontSize = 10 });
        panel.Children.Add(new Border { Background = Brush(background), CornerRadius = new CornerRadius(14), Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 4, 8, 0), Child = new TextBlock { Text = $"◷  {value}", Foreground = Brush(foreground), FontSize = 10, FontWeight = FontWeights.Bold } });
        var border = new Border { Child = panel };
        Grid.SetColumn(border, column);
        return border;
    }

    private static Grid DetailGrid(SolidColorBrush background, int columns)
    {
        var grid = new Grid { Background = background };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(112) });
        return grid;
    }

    private static TextBlock Cell(string value, HorizontalAlignment alignment, bool bold, int column, SolidColorBrush? foreground = null)
    {
        var cell = new TextBlock { Text = value, HorizontalAlignment = alignment, VerticalAlignment = VerticalAlignment.Center, Foreground = foreground ?? Text, FontSize = 10, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, Margin = new Thickness(6, 8, 6, 8), TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(cell, column);
        return cell;
    }

    private static Grid InfoRow(string label, string value, bool bold = false, SolidColorBrush? foreground = null, bool TextBoxBackground = false)
    {
        var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(105) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.Children.Add(new TextBlock { Text = label, Foreground = Muted, FontSize = 10, VerticalAlignment = VerticalAlignment.Center });
        var valueBlock = new TextBlock { Text = value, Foreground = foreground ?? Text, FontSize = 10, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Right };
        if (TextBoxBackground)
        {
            valueBlock.Background = Brush("#F8FAFC");
            valueBlock.Padding = new Thickness(7, 5, 7, 5);
            valueBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        Grid.SetColumn(valueBlock, 1);
        grid.Children.Add(valueBlock);
        return grid;
    }

    private static Button DetailButton(string text, RoutedEventHandler click, SolidColorBrush background, SolidColorBrush foreground, double width)
    {
        var button = new Button { Content = text, Width = width, Padding = new Thickness(7, 5, 7, 5), Margin = new Thickness(3), Background = background, Foreground = foreground, BorderBrush = background, FontSize = 10, FontWeight = FontWeights.Bold };
        button.Click += click;
        return button;
    }

    private void ConfirmOrder(object? sender, RoutedEventArgs e)
    {
        if (data.StateFlag == 1)
        {
            MessageBox.Show("この受注はすでに確定済みです。", "受注確定", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (AppSession.Employee == null) return;
        if (!BusinessService.ConfirmOrder(data.OrderId, AppSession.Employee.EmId, out var message))
        {
            MessageBox.Show(message, "受注確定エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        MessageBox.Show(message, "受注確定", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private void HideOrder(object? sender, RoutedEventArgs e)
    {
        if (!AppSession.IsAdmin)
        {
            MessageBox.Show("受注の非表示は管理者のみ実行できます。", "権限エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var reason = Microsoft.VisualBasic.Interaction.InputBox("非表示理由を入力してください。", "受注を非表示", data.HiddenReason ?? "");
        if (string.IsNullOrWhiteSpace(reason)) return;
        using var db = new SalesManagementContext();
        var order = db.TOrders.Find(data.OrderId);
        if (order == null) return;
        order.OrFlag = 2;
        order.OrHidden = reason.Trim();
        db.SaveChanges();
        Close();
    }

    private void PrintSlip(object? sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Controls.PrintDialog();
        if (dialog.ShowDialog() != true) return;
        var document = new System.Windows.Documents.FlowDocument { PagePadding = new Thickness(40) };
        document.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"受注伝票 ORD-{data.OrderId:000000}")));
        document.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"顧客: {data.CustomerName}　受注日: {data.OrderDate:yyyy-MM-dd}")));
        document.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"合計: ¥{data.Lines.Sum(x => x.Subtotal) * 1.1m:N0}")));
        dialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)document).DocumentPaginator, $"受注伝票 ORD-{data.OrderId:000000}");
    }

    private static SolidColorBrush Brush(string color)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }
}

public sealed class F_OrderRegistration : Window
{
    private readonly SalesManagementContext db = new();
    private readonly ComboBox office = new();
    private readonly ComboBox client = new();
    private readonly TextBox productSearch = new();
    private readonly TextBox quantity = new() { Text = "1" };
    private readonly DataGrid products = new();
    private readonly DataGrid cart = new();
    private readonly List<CartLine> lines = new();

    public F_OrderRegistration()
    {
        Title = "受注登録";
        Width = 1150;
        Height = 720;
        FontFamily = new FontFamily("Meiryo");
        BuildUi();
        LoadMasters();
        LoadProducts();
    }

    private void BuildUi()
    {
        var root = new DockPanel { Margin = new Thickness(12) };
        var header = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        header.Children.Add(new TextBlock { Text = "営業所", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4) });
        office.Width = 220;
        office.Margin = new Thickness(4);
        office.DisplayMemberPath = "Name";
        office.SelectionChanged += (_, _) => LoadClients();
        header.Children.Add(office);
        header.Children.Add(new TextBlock { Text = "顧客", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 4, 4, 4) });
        client.Width = 260;
        client.Margin = new Thickness(4);
        client.DisplayMemberPath = "Name";
        header.Children.Add(client);
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition());
        body.ColumnDefinitions.Add(new ColumnDefinition());
        body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        body.RowDefinitions.Add(new RowDefinition());
        body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var searchBar = new WrapPanel { Margin = new Thickness(0, 0, 8, 8) };
        searchBar.Children.Add(new TextBlock { Text = "商品検索", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4) });
        productSearch.Width = 230;
        productSearch.Margin = new Thickness(4);
        productSearch.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) LoadProducts(); };
        searchBar.Children.Add(productSearch);
        searchBar.Children.Add(Button("検索", (_, _) => LoadProducts()));
        Grid.SetColumn(searchBar, 0);
        Grid.SetRow(searchBar, 0);
        body.Children.Add(searchBar);

        var cartTitle = new TextBlock { Text = "登録明細", FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(8, 0, 0, 8) };
        Grid.SetColumn(cartTitle, 1);
        Grid.SetRow(cartTitle, 0);
        body.Children.Add(cartTitle);

        products.AutoGenerateColumns = true;
        products.IsReadOnly = true;
        products.SelectionMode = DataGridSelectionMode.Single;
        products.CanUserAddRows = false;
        products.Margin = new Thickness(0, 0, 8, 0);
        Grid.SetColumn(products, 0);
        Grid.SetRow(products, 1);
        body.Children.Add(products);

        cart.AutoGenerateColumns = true;
        cart.IsReadOnly = true;
        cart.CanUserAddRows = false;
        cart.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(cart, 1);
        Grid.SetRow(cart, 1);
        body.Children.Add(cart);

        var footer = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
        footer.Children.Add(new TextBlock { Text = "数量", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4) });
        quantity.Width = 70;
        quantity.Margin = new Thickness(4);
        footer.Children.Add(quantity);
        footer.Children.Add(Button("明細追加", AddLine));
        footer.Children.Add(Button("選択明細削除", RemoveLine));
        footer.Children.Add(Button("受注登録", Register));
        footer.Children.Add(Button("閉じる", (_, _) => Close()));
        Grid.SetColumnSpan(footer, 2);
        Grid.SetRow(footer, 2);
        body.Children.Add(footer);

        root.Children.Add(body);
        Content = root;
    }

    private static Button Button(string text, RoutedEventHandler click)
    {
        var button = new Button { Content = text, Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(4), MinWidth = 100 };
        button.Click += click;
        return button;
    }

    private void LoadMasters()
    {
        var offices = db.MSalesOffices.Where(x => x.SoFlag == 0).OrderBy(x => x.SoId).ToList();
        office.ItemsSource = AppSession.IsAdmin ? offices : offices.Where(x => x.SoId == AppSession.Employee!.SoId).ToList();
        if (office.Items.Count > 0) office.SelectedIndex = 0;
        LoadClients();
    }

    private void LoadClients()
    {
        if (office.SelectedItem is not MSalesOffice selectedOffice) return;
        var query = db.MClients.Where(x => x.ClFlag == 0 && x.SoId == selectedOffice.SoId).OrderBy(x => x.ClName);
        client.ItemsSource = query.ToList();
        if (client.Items.Count > 0) client.SelectedIndex = 0;
    }

    private void LoadProducts()
    {
        var keyword = productSearch.Text.Trim();
        var query = db.MProducts.Include(x => x.Ma).Include(x => x.Sc).Where(x => x.PrFlag == 0);
        if (keyword.Length > 0) query = query.Where(x => x.PrName.Contains(keyword) || x.PrModelNumber.Contains(keyword) || (x.PrJcode ?? "").Contains(keyword) || x.Ma.MaName.Contains(keyword));
        products.ItemsSource = query.OrderBy(x => x.PrName).Select(x => new ProductRow
        {
            PrId = x.PrId,
            商品コード = x.PrJcode ?? x.PrModelNumber,
            商品名 = x.PrName,
            メーカー = x.Ma.MaName,
            単価 = x.Price,
            在庫 = x.TStocks.Where(s => s.StFlag == 0).Select(s => s.StQuantity).FirstOrDefault()
        }).ToList();
    }

    private void AddLine(object? sender, RoutedEventArgs e)
    {
        if (products.SelectedItem is not ProductRow selected) { MessageBox.Show("商品を選択してください。"); return; }
        if (!int.TryParse(quantity.Text, out var count) || count <= 0) { MessageBox.Show("数量は1以上の整数で入力してください。"); return; }
        var existing = lines.FirstOrDefault(x => x.PrId == selected.PrId);
        if (existing == null) lines.Add(new CartLine { PrId = selected.PrId, 商品名 = selected.商品名, 単価 = selected.単価, 数量 = count, 在庫 = selected.在庫 });
        else existing.数量 += count;
        RefreshCart();
    }

    private void RemoveLine(object? sender, RoutedEventArgs e)
    {
        if (cart.SelectedItem is CartLine selected) lines.Remove(selected);
        RefreshCart();
    }

    private void RefreshCart()
    {
        cart.ItemsSource = null;
        cart.ItemsSource = lines.ToList();
    }

    private void Register(object? sender, RoutedEventArgs e)
    {
        if (office.SelectedItem is not MSalesOffice selectedOffice || client.SelectedItem is not MClient selectedClient)
        {
            MessageBox.Show("営業所と顧客を選択してください。");
            return;
        }
        if (lines.Count == 0) { MessageBox.Show("登録する商品を追加してください。"); return; }

        try
        {
            using var transaction = db.Database.BeginTransaction();
            var order = new TOrder { SoId = selectedOffice.SoId, EmId = AppSession.Employee!.EmId, ClId = selectedClient.ClId, OrDate = DateTime.Today, OrStateFlag = 0, OrFlag = 0 };
            db.TOrders.Add(order);
            db.SaveChanges();
            foreach (var line in lines) db.TOrderDetails.Add(new TOrderDetail { OrId = order.OrId, PrId = line.PrId, OrQuantity = line.数量, OrTotalPrice = line.単価 * line.数量 });
            db.SaveChanges();
            transaction.Commit();
            MessageBox.Show($"受注を登録しました。\n受注ID: {order.OrId}", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            lines.Clear();
            RefreshCart();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"受注登録に失敗しました。\n{ex.Message}", "登録エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private sealed class ProductRow
    {
        public int PrId { get; init; }
        public string 商品コード { get; init; } = "";
        public string 商品名 { get; init; } = "";
        public string メーカー { get; init; } = "";
        public decimal 単価 { get; init; }
        public int 在庫 { get; init; }
    }

    private sealed class CartLine
    {
        public int PrId { get; init; }
        public string 商品名 { get; init; } = "";
        public decimal 単価 { get; init; }
        public int 数量 { get; set; }
        public int 在庫 { get; init; }
        public decimal 金額 => 単価 * 数量;
    }
}
