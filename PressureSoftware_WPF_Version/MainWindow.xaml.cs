using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PressureTimerApp
{
    public partial class MainWindow : Window
    {
        private readonly CountdownTimerManager _timerManager;
        private TimerGridConfig _config;
        private TimerPositionMapper _positionMapper;
        private OracleDataAccess _oracleDataAccess;
        private bool _databaseEnabled = false;

        private readonly Dictionary<string, TimerControl> _timerControls = new Dictionary<string, TimerControl>();
        private readonly Dictionary<char, LetterRow> _letterRows = new Dictionary<char, LetterRow>();

        // 输入模式枚举
        private enum InputMode
        {
            General,
            DoubleInput,
            TripleInput
        }

        private InputMode _currentInputMode = InputMode.General;

        public MainWindow()
        {
            InitializeComponent();

            _timerManager = new CountdownTimerManager();
            LoadConfiguration();
            InitializeDatabase();
            InitializePositionMapper();
            InitializeTimerGrid();

            // 设置默认输入模式
            cmbInputMode.SelectedIndex = 0;
            UpdateInputModeVisibility();

            // 监听窗口大小变化
            this.SizeChanged += MainWindow_SizeChanged;
        }

        private void InitializeDatabase()
        {
            try
            {
                if (_config?.DatabaseConfig != null && !string.IsNullOrEmpty(_config.DatabaseConfig.ConnectionString))
                {
                    _oracleDataAccess = new OracleDataAccess(
                        _config.DatabaseConfig.ConnectionString,
                        _config.DatabaseConfig.TableName);

                    // 测试数据库连接
                    if (_oracleDataAccess.TestConnection())
                    {
                        // 确保表存在
                        if (_oracleDataAccess.EnsureTableExists())
                        {
                            _databaseEnabled = true;
                            UpdateStatus("数据库连接成功");
                        }
                        else
                        {
                            UpdateStatus("数据库表创建失败");
                        }
                    }
                    else
                    {
                        UpdateStatus("数据库连接失败");
                    }
                }
                else
                {
                    UpdateStatus("未配置数据库连接");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"数据库初始化失败: {ex.Message}");
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 可以在这里调整字体大小等
            UpdateTimerDisplaySizes();
        }

        private void UpdateTimerDisplaySizes()
        {
            // 根据窗口大小动态调整显示
            double scaleFactor = Math.Min(this.ActualWidth / 1200, 1.0);
            // 可以在这里调整字体大小等
        }

        private void LoadConfiguration()
        {
            _config = ConfigManager.LoadConfig();
            UpdateDurationInfo();
        }

        private void UpdateDurationInfo()
        {
            var timeSpan = TimeSpan.FromSeconds(_config.TimerDurationSeconds);
            txtDurationInfo.Text = $"{_config.TimerDurationSeconds}秒 ({timeSpan:hh\\:mm\\:ss})";
        }

        private void InitializePositionMapper()
        {
            _positionMapper = new TimerPositionMapper(_config.Columns);
        }

        private void InitializeTimerGrid()
        {
            ClearTimerGrid();

            // 按字母分组创建行
            var positionsByLetter = _positionMapper.GetAllPositions()
                .GroupBy(kvp => kvp.Value.RowChar)
                .OrderBy(g => g.Key);

            foreach (var letterGroup in positionsByLetter)
            {
                var letterRow = new LetterRow(letterGroup.Key, _config.Columns * 2); // 每行有奇数和偶数计时器
                mainContainer.Items.Add(letterRow);
                _letterRows[letterGroup.Key] = letterRow;

                // 为该字母的所有位置创建计时器控件
                foreach (var position in letterGroup.OrderBy(p => p.Value.Number))
                {
                    CreateTimerControl(position.Key, position.Value, letterRow);
                }
            }

            UpdateStatus($"网格初始化完成，共 {_timerControls.Count} 个计时器位置");
        }

        private void CreateTimerControl(string timerCode, TimerPosition position, LetterRow letterRow)
        {
            if (!position.IsValid) return;

            var timerControl = new TimerControl();
            timerControl.TimerCode = timerCode;

            // 添加到对应的字母行
            letterRow.AddTimerControl(timerControl, position.Number - 1); // 列索引从0开始

            _timerControls[timerCode] = timerControl;
        }

        private void ClearTimerGrid()
        {
            mainContainer.Items.Clear();
            _timerControls.Clear();
            _letterRows.Clear();

            // 清除所有计时器
            var timers = _timerManager.GetAllTimers().ToList();
            foreach (var timer in timers)
            {
                _timerManager.RemoveTimer(timer.Id);
            }
        }

        // 输入模式切换
        private void InputMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbInputMode.SelectedItem is ComboBoxItem item)
            {
                string modeTag = item.Tag.ToString();

                // 使用传统的 switch 语句替代 switch 表达式
                switch (modeTag)
                {
                    case "General":
                        _currentInputMode = InputMode.General;
                        break;
                    case "DoubleInput":
                        _currentInputMode = InputMode.DoubleInput;
                        break;
                    case "TripleInput":
                        _currentInputMode = InputMode.TripleInput;
                        break;
                    default:
                        _currentInputMode = InputMode.General;
                        break;
                }

                UpdateInputModeVisibility();
                ClearInputFields();
                UpdateStatus($"已切换到 {item.Content} 模式");
            }
        }

        private void UpdateInputModeVisibility()
        {
            // 通用模式
            pnlGeneral.Visibility = _currentInputMode == InputMode.General ? Visibility.Visible : Visibility.Collapsed;

            // 双输入框模式
            pnlDoubleInput.Visibility = _currentInputMode == InputMode.DoubleInput ? Visibility.Visible : Visibility.Collapsed;
            pnlDoubleInput2.Visibility = _currentInputMode == InputMode.DoubleInput ? Visibility.Visible : Visibility.Collapsed;

            // 三输入框模式
            pnlTripleInput.Visibility = _currentInputMode == InputMode.TripleInput ? Visibility.Visible : Visibility.Collapsed;
            pnlTripleInput2.Visibility = _currentInputMode == InputMode.TripleInput ? Visibility.Visible : Visibility.Collapsed;
            pnlTripleInput3.Visibility = _currentInputMode == InputMode.TripleInput ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClearInputFields()
        {
            txtTimerCodeGeneral.Clear();
            txtBarcodeDouble.Clear();
            txtTimerCodeDouble.Clear();
            txtBarcodeTriple.Clear();
            txtTimerCodeTriple.Clear();
            txtTimerCodeTriple2.Clear();
        }

        // 通用模式 - 计时器编码输入处理
        private void TxtTimerCodeGeneral_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                StartTimerInGeneralMode();
            }
        }

        // 双输入框模式 - 条码输入处理
        private void TxtBarcodeDouble_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                StartTimerInDoubleInputMode();
            }
        }

        // 双输入框模式 - 计时器编码输入处理
        private void TxtTimerCodeDouble_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                StartTimerInDoubleInputMode();
            }
        }

        // 三输入框模式 - 条码输入处理
        private void TxtBarcodeTriple_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                StartTimerInTripleInputMode();
            }
        }

        // 三输入框模式 - 计时器编码输入处理
        private void TxtTimerCodeTriple_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                StartTimerInTripleInputMode();
            }
        }

        // 三输入框模式 - 计时器编码2输入处理
        private void TxtTimerCodeTriple2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                StartTimerInTripleInputMode();
            }
        }

        // 通用模式启动计时器
        private void StartTimerInGeneralMode()
        {
            string timerCode = txtTimerCodeGeneral.Text.Trim().ToUpper();

            if (string.IsNullOrEmpty(timerCode))
            {
                UpdateStatus("请输入计时器编码");
                return;
            }

            StartTimerByCode(timerCode, "", "");
            txtTimerCodeGeneral.Clear();
            txtTimerCodeGeneral.Focus();
        }

        // 双输入框模式启动计时器
        private void StartTimerInDoubleInputMode()
        {
            string timerCode = txtTimerCodeDouble.Text.Trim().ToUpper();
            string barcode = txtBarcodeDouble.Text.Trim();

            if (string.IsNullOrEmpty(timerCode) || string.IsNullOrEmpty(barcode))
            {
                UpdateStatus("请输入条码和计时器编码");
                return;
            }

            StartTimerByCode(timerCode, barcode, "");
            txtTimerCodeDouble.Clear();
            txtBarcodeDouble.Clear();
            txtBarcodeDouble.Focus();
        }

        // 三输入框模式启动计时器
        private void StartTimerInTripleInputMode()
        {
            string timerCode1 = txtTimerCodeTriple.Text.Trim().ToUpper();
            string timerCode2 = txtTimerCodeTriple2.Text.Trim().ToUpper();
            string barcode = txtBarcodeTriple.Text.Trim();

            if (string.IsNullOrEmpty(timerCode1) || string.IsNullOrEmpty(timerCode2) || string.IsNullOrEmpty(barcode))
            {
                UpdateStatus("请输入条码和两个计时器编码");
                return;
            }

            // 启动第一个计时器
            StartTimerByCode(timerCode1, barcode, timerCode2);

            // 启动第二个计时器
            StartTimerByCode(timerCode2, barcode, timerCode1);

            txtTimerCodeTriple.Clear();
            txtTimerCodeTriple2.Clear();
            txtBarcodeTriple.Clear();
            txtBarcodeTriple.Focus();
        }

        private void StartTimerByCode(string timerCode, string barcode, string timerCode2)
        {
            // 验证编码格式
            if (!_positionMapper.IsValidCode(timerCode))
            {
                UpdateStatus($"无效的计时器编码: {timerCode}");
                return;
            }

            // 解析位置信息
            var position = _positionMapper.GetPosition(timerCode);
            if (position == null || !position.IsValid)
            {
                UpdateStatus($"无效的计时器位置: {timerCode}");
                return;
            }

            // 获取目标时长（秒）
            int durationSeconds = _config.TimerDurationSeconds;

            // 检查是否有自定义时长
            if (_config.CustomDurations != null && _config.CustomDurations.ContainsKey(timerCode))
            {
                durationSeconds = _config.CustomDurations[timerCode];
            }

            // 启动计时器
            StartTimerAtPosition(timerCode, durationSeconds, barcode, timerCode2);
        }

        private void StartTimerAtPosition(string timerCode, int durationSeconds, string barcode, string timerCode2)
        {
            try
            {
                if (_timerControls.TryGetValue(timerCode, out var timerControl))
                {
                    // 如果计时器正在运行，先停止
                    timerControl.Stop();
                    timerControl.Dispose();

                    // 创建新的计时器
                    timerControl.Initialize(timerCode, durationSeconds, barcode);
                    timerControl.Start();

                    var timeSpan = TimeSpan.FromSeconds(durationSeconds);
                    UpdateStatus($"已启动计时器: {timerCode} | 条码: {barcode} | 时长: {durationSeconds}秒 ({timeSpan:hh\\:mm\\:ss})");

                    // 在双输入框或三输入框模式下，将记录写入数据库
                    if (_currentInputMode == InputMode.DoubleInput || _currentInputMode == InputMode.TripleInput)
                    {
                        SaveToDatabase(timerCode, barcode, timerCode2, durationSeconds);
                    }
                }
                else
                {
                    UpdateStatus($"未找到计时器位置: {timerCode}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"启动计时器失败: {ex.Message}");
            }
        }

        private void SaveToDatabase(string timerCode1, string barcode, string timerCode2, int durationSeconds)
        {
            if (!_databaseEnabled || _oracleDataAccess == null)
            {
                UpdateStatus("数据库未启用，跳过记录保存");
                return;
            }

            try
            {
                var record = new TimerRecord
                {
                    Barcode = barcode,
                    TimerCode1 = timerCode1,
                    TimerCode2 = timerCode2,
                    DurationSeconds = durationSeconds,
                    InputMode = _currentInputMode.ToString()
                };

                bool success = _oracleDataAccess.InsertTimerRecord(record);
                if (success)
                {
                    UpdateStatus($"记录已保存到数据库: {barcode} - {timerCode1}" +
                                (string.IsNullOrEmpty(timerCode2) ? "" : $" - {timerCode2}"));
                }
                else
                {
                    UpdateStatus("数据库记录保存失败");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"保存到数据库时出错: {ex.Message}");
            }
        }

        private void UpdateStatus(string message)
        {
            txtStatus.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }

        protected override void OnClosed(EventArgs e)
        {
            _timerManager?.Dispose();

            // 释放所有计时器控件
            foreach (var control in _timerControls.Values)
            {
                control.Dispose();
            }

            base.OnClosed(e);
        }
    }
}