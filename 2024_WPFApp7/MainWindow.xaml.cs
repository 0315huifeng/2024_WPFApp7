using System.Net.Http; // 用於進行 HTTP 請求的命名空間
using System.Text.Json; // 用於 JSON 序列化和反序列化的命名空間
using System.Windows; // WPF 應用程序的基本命名空間
using System.Windows.Controls; // 用於 WPF 控件的命名空間
using LiveCharts.Wpf; // 用於 WPF 圖表控件的 LiveCharts 命名空間
using LiveCharts; // 用於圖表相關功能的 LiveCharts 命名空間

namespace _2024_WPFApp7
{
    /// <summary>
    /// MainWindow 類的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        // 預設的 API URL
        string defaultURL = "https://data.moenv.gov.tw/api/v2/aqx_p_432?api_key=e8dd42e6-9b8b-43f8-991e-b3dee723a52d&limit=1000&sort=ImportDate%20desc&format=JSON";
        // 用於存儲 AQI 數據的實例
        AQIData aqiData = new AQIData();
        // 用於存儲字段和記錄的列表
        List<Field> fields = new List<Field>();
        List<Record> records = new List<Record>();
        List<Record> selectedRecords = new List<Record>();

        // 用於圖表顯示的系列集合
        SeriesCollection seriesCollection = new SeriesCollection();

        // MainWindow 構造函數
        public MainWindow()
        {
            InitializeComponent();
            // 將預設 URL 顯示在文本框中
            UrlTextBox.Text = defaultURL;
        }

        // 按鈕點擊事件處理器，用於獲取 AQI 數據
        private async void GetAQIButton_Click(object sender, RoutedEventArgs e)
        {
            // 在文本框中顯示抓取資料中的提示
            ContentTextBox.Text = "抓取資料中...";

            // 異步獲取 API 數據
            string data = await FetchContentAsync(defaultURL);
            ContentTextBox.Text = data;

            // 反序列化獲取到的 JSON 數據
            aqiData = JsonSerializer.Deserialize<AQIData>(data);
            fields = aqiData.fields.ToList();
            records = aqiData.records.ToList();
            selectedRecords = records;

            // 更新狀態文本塊顯示的數據條數
            statusTextBlock.Text = $"共有 {records.Count} 筆資料";

            // 顯示 AQI 數據
            DisplayAQIData();
        }

        // 顯示 AQI 數據的方法
        private void DisplayAQIData()
        {
            // 將數據綁定到 DataGrid
            RecordDataGrid.ItemsSource = records;

            // 獲取第一條記錄
            Record record = records[0];

            // 遍歷所有字段，並為可用字段創建複選框
            foreach (Field field in fields)
            {
                var propertyInfo = record.GetType().GetProperty(field.id);
                if (propertyInfo != null)
                {
                    var value = propertyInfo.GetValue(record) as string;
                    if (double.TryParse(value, out double v))
                    {
                        CheckBox cb = new CheckBox
                        {
                            Content = field.info.label,
                            Tag = field.id,
                            Margin = new Thickness(3),
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Width = 150
                        };
                        cb.Checked += UpdateChart;
                        cb.Unchecked += UpdateChart;
                        DataWrapPanel.Children.Add(cb);
                    }
                }
            }
        }

        // 更新圖表的方法
        private void UpdateChart(object sender, RoutedEventArgs e)
        {
            seriesCollection.Clear();

            // 遍歷所有選中的複選框，並更新圖表數據
            foreach (CheckBox cb in DataWrapPanel.Children)
            {
                if (cb.IsChecked == true)
                {
                    List<string> labels = new List<string>();
                    string tag = cb.Tag as string;
                    ColumnSeries columnSeries = new ColumnSeries();
                    ChartValues<double> values = new ChartValues<double>();

                    // 遍歷所有選中的記錄，並提取對應字段的數據
                    foreach (Record r in selectedRecords)
                    {
                        var propertyInfo = r.GetType().GetProperty(tag);
                        if (propertyInfo != null)
                        {
                            var value = propertyInfo.GetValue(r) as string;
                            if (double.TryParse(value, out double v))
                            {
                                labels.Add(r.sitename);
                                values.Add(v);
                            }
                        }
                    }
                    columnSeries.Values = values;
                    columnSeries.Title = tag;
                    columnSeries.LabelPoint = point => $"{labels[(int)point.X]}:{point.Y.ToString()}";
                    seriesCollection.Add(columnSeries);
                }
            }
            AQIChart.Series = seriesCollection;
        }

        // 異步獲取 API 數據的方法
        private async Task<string> FetchContentAsync(string url)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(100);
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
                catch (HttpRequestException e)
                {
                    MessageBox.Show($"Request exception: {e.Message}");
                    return null;
                }
            }
        }

        // DataGrid 加載行事件處理器，為每行添加行號
        private void RecordDataGrid_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        // DataGrid 選擇改變事件處理器，更新選中的記錄
        private void RecordDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedRecords = RecordDataGrid.SelectedItems.Cast<Record>().ToList();
            statusTextBlock.Text = $"共選擇 {selectedRecords.Count} 筆資料";
            UpdateChart(null, null);
        }
    }
}
