using System.Windows;
using System.Windows.Controls;

namespace PressureTimerApp
{
    public partial class LetterRow : UserControl
    {
        public char Letter { get; }
        private int _totalColumns;

        public LetterRow(char letter, int totalColumns)
        {
            Letter = letter;
            _totalColumns = totalColumns;
            InitializeComponent();
            DataContext = this;
            InitializeGridColumns();
        }

        private void InitializeGridColumns()
        {
            // 动态创建等宽的列定义
            for (int i = 0; i < _totalColumns; i++)
            {
                timersGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });
            }
        }

        public void AddTimerControl(TimerControl timerControl, int column)
        {
            if (column >= 0 && column < _totalColumns)
            {
                // 设置计时器控件占满整个单元格
                timerControl.HorizontalAlignment = HorizontalAlignment.Stretch;
                timerControl.VerticalAlignment = VerticalAlignment.Stretch;

                Grid.SetColumn(timerControl, column);
                timersGrid.Children.Add(timerControl);
            }
        }
    }
}