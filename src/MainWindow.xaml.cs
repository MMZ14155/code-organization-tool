using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace code_organization_tool;

public partial class MainWindow
{
    private CodeProcessor _codeProcessor = new CodeProcessor();
    private CodeLineCounter _codeLineCounter = new CodeLineCounter();

    public MainWindow()
    { 
        InitializeComponent();
    }

    // 添加文件按钮点击事件
    private void AddFilesButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "代码文件|*.c;*.cpp;*.h;*.cs;*.vb;*.java;*.js;*.py;*.xaml|所有文件|*.*"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            foreach (var file in openFileDialog.FileNames)
            {
                var fileInfo = new FileInfo(file);
                if (!FilesListBox.Items.Contains(fileInfo))
                {
                    FilesListBox.Items.Add(fileInfo);
                }
            }
            UpdateLineCount();
        }
    }

    // 上移按钮点击事件
    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = FilesListBox.SelectedItems.Cast<FileInfo>().ToList();
        if (selectedItems.Count == 0) return;

        var items = FilesListBox.Items.Cast<FileInfo>().ToList();
        foreach (var item in selectedItems)
        {
            int index = items.IndexOf(item);
            if (index > 0)
            {
                items.Remove(item);
                items.Insert(index - 1, item);
            }
        }

        FilesListBox.Items.Clear();
        foreach (var item in items)
        {
            FilesListBox.Items.Add(item);
        }

        // 重新选择
        foreach (var item in selectedItems)
        {
            FilesListBox.SelectedItems.Add(item);
        }
        
        UpdateLineCount();
    }

    // 下移按钮点击事件
    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = FilesListBox.SelectedItems.Cast<FileInfo>().ToList();
        if (selectedItems.Count == 0) return;

        var items = FilesListBox.Items.Cast<FileInfo>().ToList();
        foreach (var item in selectedItems)
        {
            int index = items.IndexOf(item);
            if (index < items.Count - 1)
            {
                items.Remove(item);
                items.Insert(index + 1, item);
            }
        }

        FilesListBox.Items.Clear();
        foreach (var item in items)
        {
            FilesListBox.Items.Add(item);
        }

        // 重新选择
        foreach (var item in selectedItems)
        {
            FilesListBox.SelectedItems.Add(item);
        }
        
        UpdateLineCount();
    }

    // 删除按钮点击事件
    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = FilesListBox.SelectedItems.Cast<FileInfo>().ToList();
        foreach (var item in selectedItems)
        {
            FilesListBox.Items.Remove(item);
        }
        
        UpdateLineCount();
    }
    
    // 确定按钮点击事件
    private void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateLineCount();
    }

    // 浏览输出路径按钮点击事件
    private void BrowseOutputButton_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "文本文件|*.txt|所有文件|*.*",
            DefaultExt = ".txt"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            OutputPathTextBox.Text = saveFileDialog.FileName;
        }
    }

    // 合并输出按钮点击事件
    private void MergeButton_Click(object sender, RoutedEventArgs e)
    {
        if (FilesListBox.Items.Count == 0)
        {
            MessageBox.Show("请先添加代码文件！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (string.IsNullOrEmpty(OutputPathTextBox.Text))
        {
            MessageBox.Show("请选择输出文件路径！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            List<string> filepaths = new List<string>();
            foreach (FileInfo file in FilesListBox.Items)
            {
                filepaths.Add(file.FullName);
            }

            using (var writer = new StreamWriter(OutputPathTextBox.Text))
            {
                if (LineLimitCheckBox.IsChecked == true)
                {
                    ValidateLineLimit();
                    ProcessFilesWithLineLimit(writer, filepaths);
                }
                else
                {
                    foreach (FileInfo file in FilesListBox.Items)
                    {
                        string content = _codeProcessor.ProcessFile(
                            file.FullName,
                            AddFileNameCheckBox.IsChecked ?? false,
                            RemoveCommentsCheckBox.IsChecked ?? false,
                            RemoveImportsCheckBox.IsChecked ?? false,
                            RemoveExtraNewlinesCheckBox.IsChecked ?? false
                        );
                        
                        writer.WriteLine(content);
                        writer.WriteLine(); // 添加空行分隔不同文件
                    }
                }
            }

            MessageBox.Show($"代码已成功合并到: {OutputPathTextBox.Text}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"处理过程中出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 取消按钮点击事件
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
    
    // 更新行数显示
    private void UpdateLineCount()
    {
        List<string> filepaths = new List<string>();
        foreach (FileInfo file in FilesListBox.Items)
        {
            filepaths.Add(file.FullName);
        }

        int totalLines = _codeLineCounter.CalculateTotalLines(
            filepaths,
            AddFileNameCheckBox.IsChecked ?? false,
            RemoveCommentsCheckBox.IsChecked ?? false,
            RemoveImportsCheckBox.IsChecked ?? false,
            RemoveExtraNewlinesCheckBox.IsChecked ?? false
        );

        LineCountTextBlock.Text = $"总行数：{totalLines}";
    }
    
    // 处理行数上限逻辑
    private void ProcessFilesWithLineLimit(StreamWriter writer, List<string> filepaths)
    {
        // 合并所有文件内容
        string allContent = string.Empty;
        foreach (var filepath in filepaths)
        {
            string content = _codeProcessor.ProcessFile(
                filepath,
                AddFileNameCheckBox.IsChecked ?? false,
                RemoveCommentsCheckBox.IsChecked ?? false,
                RemoveImportsCheckBox.IsChecked ?? false,
                RemoveExtraNewlinesCheckBox.IsChecked ?? false
            );
            allContent += content + Environment.NewLine; // 添加空行分隔不同文件
        }

        // 计算分割行数
        int totalLines = allContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Length;
        int lineLimit = int.Parse(LineLimitTextBox.Text);

        if (totalLines <= lineLimit)
        {
            // 如果总行数小于或等于上限，则直接写入所有内容
            writer.WriteLine(allContent);
        }
        else
        {
            int firstHalfLines = (lineLimit + 1) / 2; // 前半部分向上取整
            int secondHalfLines = lineLimit / 2; // 后半部分向下取整

            // 按行分割内容
            var lines = allContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            string firstPart = string.Join(Environment.NewLine, lines.Take(firstHalfLines));
            string secondPart = string.Join(Environment.NewLine, lines.Skip(totalLines - secondHalfLines).Take(secondHalfLines));

            // 写入分割后的内容
            writer.WriteLine(firstPart);
            writer.WriteLine(); // 添加空行分隔符
            writer.WriteLine("=================================================="); // 分割线
            writer.WriteLine(); // 添加空行分隔符
            writer.WriteLine(secondPart);
        }
    }
    
    // 验证行数上限合理性
    private void ValidateLineLimit()
    {
        if (string.IsNullOrEmpty(LineLimitTextBox.Text))
        {
            MessageBox.Show("请输入行数上限！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!int.TryParse(LineLimitTextBox.Text, out int lineLimit))
        {
            MessageBox.Show("请输入有效的整数作为行数上限！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            LineLimitTextBox.Clear();
        }
        else if (lineLimit <= 0)
        {
            MessageBox.Show("行数上限必须大于0！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            LineLimitTextBox.Clear();
        }
    }
}