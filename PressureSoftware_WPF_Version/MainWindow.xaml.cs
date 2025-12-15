using Oracle.ManagedDataAccess.Client;
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
        private bool _workstationValid = false;
        private WorkstationSequence _workstationSequence;
        private bool _enablePressureTimeValidation = false;
        // 焦点管理状态
        private bool _isBarcodeProcessed = false;

        private readonly Dictionary<string, TimerControl> _timerControls = new Dictionary<string, TimerControl>();
        private readonly Dictionary<char, LetterRow> _letterRows = new Dictionary<char, LetterRow>();

        // 输入模式枚举
        private enum InputMode
        {
            General,
            DoubleInput,
            TripleInput
        }

        private List<string> InputModeSequence = new List<string>{ "General", "DoubleInput", "TripleInput" };

        private InputMode _currentInputMode = InputMode.General;

        public MainWindow()
        {
            InitializeComponent();

            _timerManager = new CountdownTimerManager();
            LoadConfiguration();

            // 先初始化数据库和验证工站
            InitializeDatabase();

            // 只有工站验证通过后才继续初始化其他组件
            if (_workstationValid)
            {
                InitializePositionMapper();
                InitializeTimerGrid();

                // 设置默认输入模式
                cmbInputMode.SelectedIndex = InputModeSequence.IndexOf(_config.DefaultInputMode);
                UpdateInputModeVisibility();

                // 监听窗口大小变化
                this.SizeChanged += MainWindow_SizeChanged;
            }
        }

        // 窗口加载完成后设置初始焦点
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeInputFocus();
        }

        // 初始化输入焦点
        private void InitializeInputFocus()
        {
            ResetInputFields();
            SetInitialFocus();
        }

        // 重置所有输入框
        private void ResetInputFields()
        {
            txtTimerCodeGeneral.Clear();
            txtBarcodeDouble.Clear();
            txtTimerCodeDouble.Clear();
            txtBarcodeTriple.Clear();
            txtTimerCodeTriple.Clear();
            txtTimerCodeTriple2.Clear();

            _isBarcodeProcessed = false;
        }

        // 设置初始焦点到条码输入框
        private void SetInitialFocus()
        {
            switch (_currentInputMode)
            {
                case InputMode.General:
                    txtTimerCodeGeneral.Focus();
                    break;
                case InputMode.DoubleInput:
                    txtBarcodeDouble.Focus();
                    break;
                case InputMode.TripleInput:
                    txtBarcodeTriple.Focus();
                    break;
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                if (_config?.DatabaseConfig != null && !string.IsNullOrEmpty(_config.DatabaseConfig.ConnectionString))
                {
                    _oracleDataAccess = new OracleDataAccess(
                        _config.DatabaseConfig.ConnectionString,
                        _config.DatabaseConfig.TableName,
                        _config.DatabaseConfig.StationValidationTable);

                    // 测试数据库连接
                    if (_oracleDataAccess.TestConnection())
                    {
                        // 获取数据库信息
                        string dbInfo = _oracleDataAccess.GetDatabaseInfo();
                        UpdateStatus($"数据库连接成功 - {dbInfo}");

                        // 确保表存在
                        if (_oracleDataAccess.EnsureTableExists())
                        {
                            _databaseEnabled = true;
                            UpdateStatus($"数据库表 {_config.DatabaseConfig.TableName} 准备就绪");

                            // 验证工站配置
                            ValidateWorkstation();

                            // 获取工站顺序配置
                            InitializeWorkstationSequence();
                        }
                        else
                        {
                            ShowErrorAndExit("数据库表创建失败，程序无法正常运行");
                        }
                    }
                    else
                    {
                        ShowErrorAndExit("数据库连接测试失败，请检查连接字符串和网络连接");
                    }
                }
                else
                {
                    ShowErrorAndExit("未配置数据库连接，程序无法正常运行");
                }
            }
            catch (Exception ex)
            {
                ShowErrorAndExit($"数据库初始化失败: {ex.Message}");
            }
        }

        // 初始化工站顺序配置 - 在 InitializeWorkstationSequence 方法中添加界面更新
        private void InitializeWorkstationSequence()
        {
            try
            {
                _workstationSequence = _oracleDataAccess.GetWorkstationSequence(_config.Workstation);

                if (_workstationSequence.HasPreviousStation)
                {
                    _enablePressureTimeValidation = true;
                    txtWorkstationSequence.Text = $"{_workstationSequence.PreviousWorkstation} → {_workstationSequence.CurrentWorkstation}";
                    UpdateStatus($"工站顺序配置: {_workstationSequence} - 启用时间验证");
                }
                else
                {
                    _enablePressureTimeValidation = false;
                    txtWorkstationSequence.Text = $"{_workstationSequence.CurrentWorkstation} (初始站点)";
                    UpdateStatus($"工站顺序配置: {_workstationSequence} - 无需时间验证");
                }
            }
            catch (Exception ex)
            {
                _enablePressureTimeValidation = false;
                txtWorkstationSequence.Text = "配置加载失败";
                UpdateStatus($"工站顺序配置初始化失败: {ex.Message} - 将禁用时间验证");
            }
        }


        private void ValidateWorkstation()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.Workstation))
                {
                    ShowErrorAndExit("配置文件中未设置工站站点");
                }

                // 验证工站是否在数据库中存在
                bool isValid = _oracleDataAccess.ValidateWorkstation(_config.Workstation);

                if (isValid)
                {
                    _workstationValid = true;
                    UpdateStatus($"工站验证成功: {_config.Workstation}");

                    // 显示有效的工站列表
                    var validWorkstations = _oracleDataAccess.GetValidWorkstations();
                    if (validWorkstations.Any())
                    {
                        UpdateStatus($"有效工站列表: {string.Join(", ", validWorkstations)}");
                    }
                }
                else
                {
                    // 获取有效的工站列表用于错误信息
                    var validWorkstations = _oracleDataAccess.GetValidWorkstations();
                    string validStationsText = validWorkstations.Any()
                        ? string.Join("\n", validWorkstations)
                        : "无有效工站配置";

                    ShowErrorAndExit($"工站配置错误: {_config.Workstation} 不是有效工站\n\n有效工站列表:\n{validStationsText}");
                }
            }
            catch (Exception ex)
            {
                ShowErrorAndExit($"工站验证失败: {ex.Message}");
            }
        }

        private void ShowErrorAndExit(string errorMessage)
        {
            MessageBox.Show(errorMessage, "配置错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
            Environment.Exit(1);
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

            // 显示当前工站配置
            UpdateStatus($"当前工站: {_config.Workstation}");
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
                InitializeInputFocus();
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
                string barcode = txtBarcodeDouble.Text.Trim().ToUpper();
                if (!string.IsNullOrEmpty(barcode))
                {
                    _isBarcodeProcessed = true;
                    txtTimerCodeDouble.Focus(); // 条码输入后焦点切换到计时器编码
                    UpdateStatus($"条码已输入: {barcode}，请输入计时器编码");
                }
                else
                {
                    UpdateStatus("请输入条码");
                }
            }
        }

        // 双输入框模式 - 计时器编码输入处理
        private void TxtTimerCodeDouble_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!_isBarcodeProcessed)
                {
                    UpdateStatus("请先输入条码");
                    txtBarcodeDouble.Focus();
                    return;
                }

                StartTimerInDoubleInputMode();
            }
        }

        // 三输入框模式 - 条码输入处理
        private void TxtBarcodeTriple_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string barcode = txtBarcodeTriple.Text.Trim().ToUpper();
                if (!string.IsNullOrEmpty(barcode))
                {
                    _isBarcodeProcessed = true;
                    txtTimerCodeTriple.Focus(); // 条码输入后焦点切换到第一个计时器编码
                    UpdateStatus($"条码已输入: {barcode}，请输入第一个计时器编码");
                }
                else
                {
                    UpdateStatus("请输入条码");
                }
            }
        }

        // 三输入框模式 - 计时器编码输入处理
        private void TxtTimerCodeTriple_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!_isBarcodeProcessed)
                {
                    UpdateStatus("请先输入条码");
                    txtBarcodeTriple.Focus();
                    return;
                }

                string timerCode1 = txtTimerCodeTriple.Text.Trim().ToUpper();
                if (string.IsNullOrEmpty(timerCode1))
                {
                    UpdateStatus("请输入第一个计时器编码");
                    return;
                }

                txtTimerCodeTriple2.Focus(); // 第一个计时器编码输入后焦点切换到第二个计时器编码
                UpdateStatus($"第一个计时器编码已输入: {timerCode1}，请输入第二个计时器编码");
            }
        }

        // 三输入框模式 - 计时器编码2输入处理
        private void TxtTimerCodeTriple2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!_isBarcodeProcessed)
                {
                    UpdateStatus("请先输入条码");
                    txtBarcodeTriple.Focus();
                    return;
                }

                string timerCode1 = txtTimerCodeTriple.Text.Trim().ToUpper();
                if (string.IsNullOrEmpty(timerCode1))
                {
                    UpdateStatus("请输入第一个计时器编码");
                    txtTimerCodeTriple.Focus();
                    return;
                }

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

            // 通用模式：启动后重置输入框并保持焦点
            txtTimerCodeGeneral.Clear();
            txtTimerCodeGeneral.Focus();
        }

        // 双输入框模式启动计时器
        private void StartTimerInDoubleInputMode()
        {
            string timerCode = txtTimerCodeDouble.Text.Trim().ToUpper();
            string barcode = txtBarcodeDouble.Text.Trim().ToUpper();

            if (string.IsNullOrEmpty(timerCode) || string.IsNullOrEmpty(barcode))
            {
                UpdateStatus("请输入条码和计时器编码");
                return;
            }

            // 验证时间管控（如果启用）
            if (!ValidatePressureTime(barcode))
            {
                // 验证失败，重置输入框
                ResetInputFields();
                SetInitialFocus();
                return; // 验证失败，不启动计时器
            }

            StartTimerByCode(timerCode, barcode, "");

            // 双输入框模式：启动后重置所有输入框，焦点回到条码输入框
            ResetInputFields();
            SetInitialFocus();
        }

        // 三输入框模式启动计时器
        private void StartTimerInTripleInputMode()
        {
            string timerCode1 = txtTimerCodeTriple.Text.Trim().ToUpper();
            string timerCode2 = txtTimerCodeTriple2.Text.Trim().ToUpper();
            string barcode = txtBarcodeTriple.Text.Trim().ToUpper();

            if (string.IsNullOrEmpty(timerCode1) || string.IsNullOrEmpty(timerCode2) || string.IsNullOrEmpty(barcode))
            {
                UpdateStatus("请输入条码和两个计时器编码");
                return;
            }

            // 验证时间管控（如果启用）
            if (!ValidatePressureTime(barcode))
            {
                // 验证失败，重置输入框
                ResetInputFields();
                SetInitialFocus();
                return; // 验证失败，不启动计时器
            }

            // 验证两个计时器编码
            if (!_positionMapper.IsValidCode(timerCode1))
            {
                UpdateStatus($"无效的计时器编码: {timerCode1}");
                txtTimerCodeTriple.Focus();
                return;
            }

            if (!_positionMapper.IsValidCode(timerCode2))
            {
                UpdateStatus($"无效的计时器编码: {timerCode2}");
                txtTimerCodeTriple2.Focus();
                return;
            }

            // 获取目标时长（秒）
            int durationSeconds = _config.TimerDurationSeconds;

            // 检查是否有自定义时长（使用第一个计时器的配置）
            if (_config.CustomDurations != null && _config.CustomDurations.ContainsKey(timerCode1))
            {
                durationSeconds = _config.CustomDurations[timerCode1];
            }

            // 启动第一个计时器
            StartTimerAtPosition(timerCode1, durationSeconds, barcode);

            // 启动第二个计时器
            StartTimerAtPosition(timerCode2, durationSeconds, barcode);

            // 保存一条包含两个计时器信息的数据库记录
            SaveToDatabase(timerCode1, barcode, timerCode2, durationSeconds);

            // 三输入框模式：启动后重置所有输入框，焦点回到条码输入框
            ResetInputFields();
            SetInitialFocus();
        }

        // 验证保压时间
        private bool ValidatePressureTime(string barcode)
        {
            if (!_enablePressureTimeValidation || !_workstationSequence.HasPreviousStation)
            {
                return true; // 不需要验证
            }

            try
            {
                UpdateStatus($"正在验证条码 {barcode} 在工站 {_workstationSequence} 的时间管控...");

                var (isValid, message) = _oracleDataAccess.ValidatePressureTime(barcode, _workstationSequence, _config.TimerDurationSeconds);

                if (isValid)
                {
                    UpdateStatus($"✓ {message}");
                    return true;
                }
                else
                {
                    UpdateStatus($"✗ {message}");
                    MessageBox.Show($"时间管控验证失败:\n\n{message}", "验证失败",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);

                    // 管控错误后重置输入框
                    ResetInputFields();
                    SetInitialFocus();
                    return false;
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"时间验证过程中发生错误: {ex.Message}");
                MessageBox.Show($"时间验证过程中发生错误:\n\n{ex.Message}", "验证错误",
                              MessageBoxButton.OK, MessageBoxImage.Error);

                // 错误后重置输入框
                ResetInputFields();
                SetInitialFocus();
                return false;
            }
        }

        private void StartTimerByCode(string timerCode, string barcode, string timerCode2)
        {
            try
            {
                // 验证编码格式
                if (!_positionMapper.IsValidCode(timerCode))
                {
                    UpdateStatus($"无效的计时器编码: {timerCode}");
                    // 编码错误后重置输入框
                    ResetInputFields();
                    SetInitialFocus();
                    return;
                }

                // 解析位置信息
                var position = _positionMapper.GetPosition(timerCode);
                if (position == null || !position.IsValid)
                {
                    UpdateStatus($"无效的计时器位置: {timerCode}");
                    // 位置错误后重置输入框
                    ResetInputFields();
                    SetInitialFocus();
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
                StartTimerAtPosition(timerCode, durationSeconds, barcode);

                // 只有在双输入框模式下才保存数据库记录（三输入框模式在外部处理）
                if (_currentInputMode == InputMode.DoubleInput)
                {
                    SaveToDatabase(timerCode, barcode, timerCode2, durationSeconds);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"启动计时器失败: {ex.Message}");
                // 启动失败后重置输入框
                ResetInputFields();
                SetInitialFocus();
            }
        }

        private void StartTimerAtPosition(string timerCode, int durationSeconds, string barcode)
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

                    // 监听完成事件
                    timerControl.Timer.TimerCompleted += (sender) =>
                    {
                        UpdateStatus($"计时器完成: {timerCode} | 条码: {barcode}");

                        // 可以添加更明显的提示
                        //if (MessageBox.Show($"计时器 {timerCode} 已完成！", "计时器完成",
                        //    MessageBoxButton.OK, MessageBoxImage.Information) == MessageBoxResult.OK)
                        //{
                        //    // 用户确认后可以执行其他操作
                        //}
                    };
                }
                else
                {
                    UpdateStatus($"未找到计时器位置: {timerCode}");
                    ResetInputFields();
                    SetInitialFocus();
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"启动计时器失败: {ex.Message}");
                ResetInputFields();
                SetInitialFocus();
            }
        }

        private void SaveToDatabase(string timerCode1, string barcode, string timerCode2, int durationSeconds)
        {
            if (!_databaseEnabled || _oracleDataAccess == null)
            {
                UpdateStatus("数据库未启用，跳过记录保存");
                return;
            }

            if (!_workstationValid)
            {
                UpdateStatus("工站配置无效，无法保存记录到数据库");
                return;
            }

            try
            {
                var record = new TimerRecord
                {
                    Barcode = barcode,
                    TimerCode1 = timerCode1,
                    TimerCode2 = timerCode2, // 三输入框模式下不为空，双输入框模式下为空
                    DurationSeconds = durationSeconds,
                    InputMode = _currentInputMode.ToString()
                };

                bool success = _oracleDataAccess.InsertTimerRecord(record, _config.Workstation);
                if (success)
                {
                    string logMessage = $"记录已保存到数据库: {barcode} - {timerCode1}";
                    if (!string.IsNullOrEmpty(timerCode2))
                    {
                        logMessage += $" 和 {timerCode2}";
                    }
                    logMessage += $" | 工站: {_config.Workstation} | 时长: {durationSeconds}秒 | 模式: {_currentInputMode}";

                    UpdateStatus(logMessage);
                }
                else
                {
                    UpdateStatus("数据库记录保存失败，请检查数据库连接");
                    // 数据库保存失败后重置输入框
                    ResetInputFields();
                    SetInitialFocus();
                }
            }
            catch (OracleException oraEx)
            {
                UpdateStatus($"Oracle数据库错误: {oraEx.Message} (错误代码: {oraEx.Number})");
                // 数据库错误后重置输入框
                ResetInputFields();
                SetInitialFocus();
            }
            catch (Exception ex)
            {
                UpdateStatus($"保存到数据库时出错: {ex.Message}");
                // 保存错误后重置输入框
                ResetInputFields();
                SetInitialFocus();
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