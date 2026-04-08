using System.Windows.Forms;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WindowsFormsApp2.LexicalAnalyzer;
using System.Resources;
using WindowsFormsApp2.Model;

namespace WindowsFormsApp2
{
    public partial class CompilerForm : Form
    {
        private Stack<string> undoStack = new Stack<string>();
        private Stack<string> redoStack = new Stack<string>();
        private bool isUndoRedoOperation = false;
        private string previousText = "";

        private string currentFilePath = string.Empty;
        private bool isTextModified = false;
        private string lastSavedText = "";

        //private LexicalAnalyzer _analyzer = new LexicalAnalyzer();
        private AnalysisResult _lastAnalysisResult;

        private Lexer _lexer;
        private Parser _parser;
        private List<Lexem> _lastLexems;

        private List<Parser.SyntaxError> _lastSyntaxErrors;
        public CompilerForm()
        {
            InitializeComponent();

            previousText = textBoxEditor.Text;
            undoStack.Push(previousText);

            InitializeEditMenu();
            InitializeFileMenu();
            InitializeResultsDataGridView();
            InitializeRunButton();

            UpdateWindowTitle();
            UpdateMenuState();

            this.Icon = Properties.Resources.cpu_icon_212120;
        }

        private void DisplayAnalysisResults(AnalysisResult result)
        {
            _lastAnalysisResult = result;

            //textBoxResults.Clear();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"РЕЗУЛЬТАТЫ ЛЕКСИЧЕСКОГО АНАЛИЗА - {DateTime.Now:HH:mm:ss}");
            sb.AppendLine("==========================================================");
            sb.AppendLine();

            sb.AppendLine($"{"КОД",-6} {"ТИП ЛЕКСЕМЫ",-20} {"ЛЕКСЕМА",-25} {"ПОЗИЦИЯ"}");
            sb.AppendLine(new string('=', 80));

            int currentLine = 0;
            foreach (var token in result.Tokens)
            {
                if (token.Line != currentLine)
                {
                    currentLine = token.Line;
                    sb.AppendLine($"--- Строка {currentLine} ---");
                }

                if (token.IsError)
                {
                    sb.AppendLine($"  {token.GetFormattedString()}");
                }
                else
                {
                    sb.AppendLine($"  {token.GetFormattedString()}");
                }
            }

            sb.AppendLine(new string('=', 80));
            int totalTokens = result.Tokens.Count(t => !t.IsError);
            int totalErrors = result.Tokens.Count(t => t.IsError);

            sb.AppendLine($"ИТОГО:");
            sb.AppendLine($"  ✓ Лексем: {totalTokens}");
            sb.AppendLine($"  ✗ Ошибок: {totalErrors}");
            sb.AppendLine($"  ∑ Всего элементов: {result.Tokens.Count}");

            if (result.HasErrors)
            {
                sb.AppendLine();
                sb.AppendLine("🔍 Дважды щелкните на строке с ошибкой для перехода к ней.");
                sb.AppendLine("📋 Используйте контекстное меню для копирования результатов.");
            }

            //textBoxResults.Text = sb.ToString();

            // Подсветка ошибок в редакторе
            HighlightErrors(result);

        }

        private void HighlightErrors(AnalysisResult result)
        {
            int currentSelectionStart = textBoxEditor.SelectionStart;
            int currentSelectionLength = textBoxEditor.SelectionLength;

            textBoxEditor.SelectAll();
            textBoxEditor.SelectionBackColor = Color.White;

            // Подсвечиваем ошибки
            foreach (var error in result.Errors)
            {
                int startPos = GetPositionFromLineAndColumn(error.Line, error.StartPosition);
                int length = error.EndPosition - error.StartPosition;

                if (startPos >= 0 && length > 0)
                {
                    textBoxEditor.Select(startPos, length);
                    textBoxEditor.SelectionBackColor = Color.LightCoral;
                }
            }

            textBoxEditor.Select(currentSelectionStart, currentSelectionLength);
            textBoxEditor.SelectionBackColor = Color.White; 
        }

        private void InitializeResultsDataGridView()
        {
            // Ищем DataGridView на форме
            dgvResults = this.Controls.OfType<DataGridView>().FirstOrDefault();

            // Если не нашли через Controls, ищем во всех контролах
            if (dgvResults == null)
            {
                dgvResults = FindControl<DataGridView>(this, "dgvResults");
            }

            if (dgvResults == null)
            {
                // Создаем DataGridView программно
                dgvResults = new DataGridView();
                dgvResults.Name = "dgvResults";

                // Добавляем в Panel2 splitContainer1
                if (splitContainer1 != null)
                {
                    splitContainer1.Panel2.Controls.Add(dgvResults);
                }
                else
                {
                    this.Controls.Add(dgvResults);
                }
            }

            // Настройка внешнего вида
            dgvResults.AllowUserToAddRows = false;
            dgvResults.AllowUserToDeleteRows = false;
            dgvResults.AllowUserToOrderColumns = false;
            dgvResults.AllowUserToResizeRows = false;
            dgvResults.ReadOnly = true;
            dgvResults.RowHeadersVisible = false;
            dgvResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvResults.BackgroundColor = Color.FromArgb(240, 240, 240);
            dgvResults.BorderStyle = BorderStyle.None;
            dgvResults.Font = new Font("Consolas", 10);
            dgvResults.Dock = DockStyle.Fill;
            dgvResults.Visible = true;

            // Настройка цветов
            dgvResults.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgvResults.DefaultCellStyle.BackColor = Color.White;
            dgvResults.DefaultCellStyle.ForeColor = Color.Black;
            dgvResults.DefaultCellStyle.SelectionBackColor = Color.LightSteelBlue;
            dgvResults.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvResults.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(60, 60, 60);
            dgvResults.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvResults.ColumnHeadersDefaultCellStyle.Font = new Font("Consolas", 10, FontStyle.Bold);
            dgvResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            // Создаем пустые колонки по умолчанию (чтобы избежать ошибки)
            dgvResults.Columns.Clear();
            dgvResults.Columns.Add("Message", "СООБЩЕНИЕ");
            dgvResults.Rows.Add("Готов к анализу.");

            // Обработчик двойного щелчка
            dgvResults.CellDoubleClick += DgvResults_CellDoubleClick;

            // Контекстное меню
            ContextMenuStrip resultsMenu = new ContextMenuStrip();
            ToolStripMenuItem copyItem = new ToolStripMenuItem("Копировать");
            copyItem.Click += (s, e) => CopyAllResultsToClipboard();
            resultsMenu.Items.Add(copyItem);

            ToolStripMenuItem copyAllItem = new ToolStripMenuItem("Копировать всё");
            copyAllItem.Click += (s, e) => CopyAllResultsToClipboard();
            resultsMenu.Items.Add(copyAllItem);

            ToolStripMenuItem clearItem = new ToolStripMenuItem("Очистить");
            clearItem.Click += (s, e) => ClearResults();
            resultsMenu.Items.Add(clearItem);

            dgvResults.ContextMenuStrip = resultsMenu;

            // Принудительное обновление
            dgvResults.Refresh();
        }

        // Вспомогательный метод для поиска контрола
        private T FindControl<T>(Control parent, string name) where T : Control
        {
            foreach (Control control in parent.Controls)
            {
                if (control is T && control.Name == name)
                    return (T)control;

                var result = FindControl<T>(control, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        //private void TextBoxResults_DoubleClick(object sender, EventArgs e)
        //{
        //    if (_lastSyntaxErrors == null || !_lastSyntaxErrors.Any()) return;

        //    int cursorPos = textBoxResults.SelectionStart;
        //    string text = textBoxResults.Text;

        //    // Находим строку, на которой произошел двойной щелчок
        //    int lineStart = text.LastIndexOf('\n', cursorPos - 1) + 1;
        //    int lineEnd = text.IndexOf('\n', cursorPos);
        //    if (lineEnd == -1) lineEnd = text.Length;

        //    string line = text.Substring(lineStart, lineEnd - lineStart);

        //    // Пытаемся найти ошибку в этой строке
        //    // Упрощенный подход - берем первую ошибку (для демо)
        //    // В реальном проекте нужно парсить номер строки из таблицы
        //    var firstError = _lastSyntaxErrors.FirstOrDefault();
        //    if (firstError != null)
        //    {
        //        NavigateToSyntaxError(firstError);
        //    }
        //}


        private void NavigateToErrorPosition(int startPos, int endPos)
        {
            if (startPos >= 0 && startPos < textBoxEditor.TextLength)
            {
                textBoxEditor.Focus();
                textBoxEditor.Select(startPos, Math.Min(endPos - startPos, textBoxEditor.TextLength - startPos));
                textBoxEditor.ScrollToCaret();

                textBoxEditor.SelectionBackColor = Color.Yellow;

                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                timer.Interval = 2000;
                timer.Tick += (s, e) =>
                {
                    textBoxEditor.SelectionBackColor = Color.White;
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        }

        private void NavigateToSyntaxError(Parser.SyntaxError error)
        {
            int position = GetPositionFromLineAndColumn(error.Line, error.Position);
            if (position >= 0)
            {
                textBoxEditor.Focus();
                textBoxEditor.Select(position, 1);
                textBoxEditor.ScrollToCaret();

                textBoxEditor.SelectionBackColor = Color.Yellow;

                Timer timer = new Timer();
                timer.Interval = 2000;
                timer.Tick += (s, e) =>
                {
                    textBoxEditor.SelectionBackColor = Color.White;
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        }

        //private void CopyResultsToClipboard()
        //{
        //    if (!string.IsNullOrEmpty(textBoxResults.Text))
        //    {
        //        Clipboard.SetText(textBoxResults.Text);
        //        MessageBox.Show("Результаты скопированы в буфер обмена",
        //            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //    }
        //}

        private void RunAnalysis()
        {
            try
            {
                ClearResults();
                _lastLexems = null;
                _lastSyntaxErrors = null;

                textBoxEditor.SelectAll();
                textBoxEditor.SelectionBackColor = Color.White;
                textBoxEditor.Select(0, 0);

                string code = textBoxEditor.Text;

                if (string.IsNullOrWhiteSpace(code))
                {
                    if (dgvResults != null)
                    {
                        dgvResults.Rows.Clear();
                        dgvResults.Columns.Clear();
                        dgvResults.Columns.Add("Message", "СООБЩЕНИЕ");
                        dgvResults.Rows.Add("Введите текст для анализа.");
                    }
                    return;
                }

                // Лексический анализ
                _lexer = new Lexer(code);
                _lastLexems = _lexer.Scan();

                // Синтаксический анализ
                _parser = new Parser(_lastLexems);
                bool syntaxValid = _parser.Parse();
                var errors = _parser.GetErrors();

                if (errors.Any())
                {
                    DisplaySyntaxErrors(errors);
                    HighlightErrorsFromSyntax(errors);

                    MessageBox.Show($"Обнаружены синтаксические ошибки.\n" +
                                  $"Всего ошибок: {errors.Count}\n\n" +
                                  $"Для перехода к ошибке щелкните на строке в таблице.",
                        "Результат анализа",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else
                {
                    // Если нет ошибок, показываем сообщение об успехе в DataGridView
                    if (dgvResults != null)
                    {
                        dgvResults.Rows.Clear();
                        dgvResults.Columns.Clear();
                        dgvResults.Columns.Add("Message", "СООБЩЕНИЕ");
                        dgvResults.Rows.Add("Синтаксический анализ выполнен успешно.");
                        dgvResults.Rows.Add("Ошибок не обнаружено.");
                    }

                    MessageBox.Show("Синтаксический анализ выполнен успешно!\n" +
                                  "Ошибок не обнаружено.",
                        "Результат анализа",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при анализе: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HighlightErrorsFromSyntax(List<Parser.SyntaxError> errors)
        {
            if (errors == null || !errors.Any()) return;

            int currentSelectionStart = textBoxEditor.SelectionStart;
            int currentSelectionLength = textBoxEditor.SelectionLength;

            textBoxEditor.SelectAll();
            textBoxEditor.SelectionBackColor = Color.White;

            foreach (var error in errors)
            {
                int position = GetPositionFromLineAndColumn(error.Line, error.Position);
                if (position >= 0 && position < textBoxEditor.TextLength)
                {
                    // Подсвечиваем ошибочный фрагмент (или позицию)
                    int length = error.Fragment?.Length ?? 1;
                    textBoxEditor.Select(position, Math.Min(length, textBoxEditor.TextLength - position));
                    textBoxEditor.SelectionBackColor = Color.LightCoral;
                }
            }

            if (currentSelectionStart <= textBoxEditor.TextLength)
            {
                textBoxEditor.Select(currentSelectionStart,
                    Math.Min(currentSelectionLength, textBoxEditor.TextLength - currentSelectionStart));
            }
        }

        private void DisplaySyntaxErrors(List<Parser.SyntaxError> errors)
        {
            if (dgvResults == null)
            {
                InitializeResultsDataGridView();
                if (dgvResults == null) return;
            }

            // Очищаем и создаем колонки
            dgvResults.Rows.Clear();
            dgvResults.Columns.Clear();

            // Настройка колонок для таблицы ошибок
            dgvResults.Columns.Add("Fragment", "НЕВЕРНЫЙ ФРАГМЕНТ");
            dgvResults.Columns.Add("Line", "СТРОКА");
            dgvResults.Columns.Add("Position", "ПОЗИЦИЯ");
            dgvResults.Columns.Add("Description", "ОПИСАНИЕ");

            // Настройка ширины колонок
            dgvResults.Columns["Fragment"].Width = 200;
            dgvResults.Columns["Line"].Width = 60;
            dgvResults.Columns["Position"].Width = 70;
            dgvResults.Columns["Description"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Заполнение данными
            foreach (var error in errors)
            {
                string fragment = (error.Fragment?.Length > 30) ?
                    error.Fragment.Substring(0, 27) + "..." :
                    (error.Fragment ?? "null");

                int rowIndex = dgvResults.Rows.Add(fragment, error.Line, error.Position, error.Description);

                // Подсветка строк с ошибками
                if (error.Description.Contains("Недопустимый") || error.Description.Contains("Ошибка"))
                {
                    dgvResults.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightCoral;
                }
            }

            // Добавление строки с итогами
            int summaryRowIndex = dgvResults.Rows.Add("", "", "", "");
            dgvResults.Rows[summaryRowIndex].DefaultCellStyle.BackColor = Color.LightGray;
            dgvResults.Rows[summaryRowIndex].Cells[3].Value = $"ВСЕГО НАЙДЕНО ОШИБОК: {errors.Count}";

            // Сохраняем ошибки для навигации
            _lastSyntaxErrors = errors;

            dgvResults.Refresh();
        }

        private void DisplayLexemsOnly(List<Lexem> lexems)
        {
            if (dgvResults == null)
            {
                InitializeResultsDataGridView();
                if (dgvResults == null) return;
            }

            dgvResults.Rows.Clear();
            dgvResults.Columns.Clear();

            // Настройка колонок для лексем
            dgvResults.Columns.Add("Code", "КОД");
            dgvResults.Columns.Add("Type", "ТИП ЛЕКСЕМЫ");
            dgvResults.Columns.Add("Lexem", "ЛЕКСЕМА");
            dgvResults.Columns.Add("Position", "ПОЗИЦИЯ");

            // Настройка ширины колонок
            dgvResults.Columns["Code"].Width = 60;
            dgvResults.Columns["Type"].Width = 180;
            dgvResults.Columns["Lexem"].Width = 200;
            dgvResults.Columns["Position"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            int errorCount = 0;

            foreach (var lexem in lexems)
            {
                string location = $"({lexem.lexemStartPosition}-{lexem.lexemEndPosition})";

                if (lexem.lexemType == Lexem.LexemType.Error)
                {
                    errorCount++;
                    int rowIndex = dgvResults.Rows.Add(lexem.lexemCode, "ОШИБКА", lexem.lexemContaintment, location);
                    dgvResults.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightCoral;
                }
                else
                {
                    dgvResults.Rows.Add(lexem.lexemCode, lexem.lexemName, lexem.lexemContaintment, location);
                }
            }

            // Добавление строки с итогами
            int summaryRowIndex = dgvResults.Rows.Add("", "", "", "");
            dgvResults.Rows[summaryRowIndex].DefaultCellStyle.BackColor = Color.LightGray;
            dgvResults.Rows[summaryRowIndex].Cells[3].Value = $"✓ Лексем: {lexems.Count(l => l.lexemType != Lexem.LexemType.Error)} | ✗ Ошибок: {errorCount}";

            dgvResults.Refresh();
        }

        private void DisplayFullResults(List<Lexem> lexems, bool syntaxValid, Parser parser)
        {
            if (dgvResults == null) return;

            dgvResults.Rows.Clear();
            dgvResults.Columns.Clear();

            // Сначала отображаем лексемы
            dgvResults.Columns.Add("Line", "СТР");
            dgvResults.Columns.Add("Code", "КОД");
            dgvResults.Columns.Add("Type", "ТИП ЛЕКСЕМЫ");
            dgvResults.Columns.Add("Lexem", "ЛЕКСЕМА");
            dgvResults.Columns.Add("Position", "ПОЗИЦИЯ");

            // Настройка ширины колонок
            dgvResults.Columns["Line"].Width = 50;
            dgvResults.Columns["Code"].Width = 60;
            dgvResults.Columns["Type"].Width = 180;
            dgvResults.Columns["Lexem"].Width = 200;
            dgvResults.Columns["Position"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            int currentLine = 0;

            foreach (var lexem in lexems)
            {
                if (lexem.lexemLine != currentLine && currentLine > 0)
                {
                    // Разделитель между строками
                    dgvResults.Rows.Add("", "", "", "", "---");
                    dgvResults.Rows[dgvResults.Rows.Count - 1].DefaultCellStyle.BackColor = Color.LightGray;
                }
                currentLine = lexem.lexemLine;

                string location = $"({lexem.lexemStartPosition}-{lexem.lexemEndPosition})";

                if (lexem.lexemType == Lexem.LexemType.Error)
                {
                    dgvResults.Rows.Add(lexem.lexemLine, lexem.lexemCode, "ОШИБКА", lexem.lexemContaintment, location);
                    dgvResults.Rows[dgvResults.Rows.Count - 1].DefaultCellStyle.BackColor = Color.LightCoral;
                }
                else
                {
                    dgvResults.Rows.Add(lexem.lexemLine, lexem.lexemCode, lexem.lexemName, lexem.lexemContaintment, location);
                }
            }

            // Разделитель перед результатами синтаксического анализа
            dgvResults.Rows.Add("", "", "", "", "");
            dgvResults.Rows[dgvResults.Rows.Count - 1].DefaultCellStyle.BackColor = Color.LightGray;
            dgvResults.Rows.Add("", "", "СИНТАКСИЧЕСКИЙ АНАЛИЗ:", syntaxValid ? "УСПЕШНО" : "ОШИБКА", "");

            int totalLines = lexems.Max(l => l.lexemLine);
            int totalTokens = lexems.Count(l => l.lexemType != Lexem.LexemType.Error);
            int totalErrors = lexems.Count(l => l.lexemType == Lexem.LexemType.Error);

            dgvResults.Rows.Add("", "", "", "", "");
            dgvResults.Rows[dgvResults.Rows.Count - 1].DefaultCellStyle.BackColor = Color.LightGray;
            dgvResults.Rows.Add("", "", $"📄 Строк: {totalLines}", "", "");
            dgvResults.Rows.Add("", "", $"✓ Лексем: {totalTokens}", "", "");
            dgvResults.Rows.Add("", "", $"✗ Ошибок: {totalErrors}", "", "");
            dgvResults.Rows.Add("", "", $"∑ Всего элементов: {lexems.Count}", "", "");
        }

        private void NavigateToErrorPositionFromGrid(int line, int position)
        {
            int textPosition = GetPositionFromLineAndColumn(line, position);
            if (textPosition >= 0)
            {
                textBoxEditor.Focus();
                textBoxEditor.Select(textPosition, 1);
                textBoxEditor.ScrollToCaret();

                textBoxEditor.SelectionBackColor = Color.Yellow;

                Timer timer = new Timer();
                timer.Interval = 2000;
                timer.Tick += (s, e) =>
                {
                    textBoxEditor.SelectionBackColor = Color.White;
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        }

        private int GetLineFromRow(DataGridViewRow row)
        {
            if (row.Cells["Line"].Value != null && int.TryParse(row.Cells["Line"].Value.ToString(), out int line))
                return line;
            return 1;
        }

        private void CopyAllResultsToClipboard()
        {
            if (dgvResults == null || dgvResults.Rows.Count == 0) return;

            StringBuilder sb = new StringBuilder();

            // Заголовки колонок
            for (int i = 0; i < dgvResults.Columns.Count; i++)
            {
                sb.Append(dgvResults.Columns[i].HeaderText);
                if (i < dgvResults.Columns.Count - 1) sb.Append("\t");
            }
            sb.AppendLine();

            // Данные
            foreach (DataGridViewRow row in dgvResults.Rows)
            {
                for (int i = 0; i < dgvResults.Columns.Count; i++)
                {
                    string value = row.Cells[i].Value?.ToString() ?? "";
                    sb.Append(value);
                    if (i < dgvResults.Columns.Count - 1) sb.Append("\t");
                }
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString());
            MessageBox.Show("Результаты скопированы в буфер обмена",
                "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ClearResults()
        {
            if (dgvResults != null)
            {
                dgvResults.Rows.Clear();
                dgvResults.Columns.Clear();
                // Добавляем колонку по умолчанию, чтобы избежать ошибок
                dgvResults.Columns.Add("Message", "СООБЩЕНИЕ");
                dgvResults.Rows.Add("Готов к анализу.");
            }
            _lastSyntaxErrors = null;
            _lastLexems = null;
        }

        private void HighlightErrorsFromLexems(List<Lexem> lexems)
        {
            if (lexems == null) return;

            int currentSelectionStart = textBoxEditor.SelectionStart;
            int currentSelectionLength = textBoxEditor.SelectionLength;

            textBoxEditor.SelectAll();
            textBoxEditor.SelectionBackColor = Color.White;

            foreach (var error in lexems.Where(l => l.lexemType == Lexem.LexemType.Error))
            {
                int startPos = error.lexemStartPosition;
                int length = error.lexemEndPosition - error.lexemStartPosition;

                if (startPos >= 0 && length > 0 && startPos < textBoxEditor.TextLength)
                {
                    textBoxEditor.Select(startPos, Math.Min(length, textBoxEditor.TextLength - startPos));
                    textBoxEditor.SelectionBackColor = Color.LightCoral;
                }
            }

            if (currentSelectionStart <= textBoxEditor.TextLength)
            {
                textBoxEditor.Select(currentSelectionStart,
                    Math.Min(currentSelectionLength, textBoxEditor.TextLength - currentSelectionStart));
            }
        }

        private void ShowAnalysisResultMessage(List<Lexem> lexems, bool syntaxValid)
        {
            int lexicalErrors = lexems.Count(l => l.lexemType == Lexem.LexemType.Error);

            if (lexicalErrors > 0)
            {
                MessageBox.Show($"Обнаружены ошибки на этапе лексического анализа.\n" +
                              $"Всего ошибок: {lexicalErrors}\n\n" +
                              $"Дважды щелкните на строке с ошибкой в окне результатов,\n" +
                              $"чтобы перейти к проблемному месту.",
                    "Результат анализа",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else if (!syntaxValid)
            {
                MessageBox.Show("Лексический анализ выполнен успешно, но обнаружены\n" +
                              "синтаксические ошибки в структуре кода.",
                    "Результат анализа",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("Анализ выполнен успешно!\n" +
                              $"Лексем: {lexems.Count}\n" +
                              "Синтаксических ошибок не обнаружено.",
                    "Результат анализа",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private int GetPositionFromLineAndColumn(int line, int column)
        {
            string text = textBoxEditor.Text;
            int currentLine = 1;
            int position = 0;

            while (position < text.Length && currentLine < line)
            {
                if (text[position] == '\n')
                {
                    currentLine++;
                }
                position++;
            }

            if (currentLine == line)
            {
                return position + column - 1;
            }

            return -1;
        }

        private void InitializeRunButton()
        {
            Button btnRun = this.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Пуск");
            if (btnRun != null)
            {
                btnRun.Click += (s, e) => RunAnalysis();
            }

            ToolStripMenuItem runMenuItem = this.GetMenuItem("runToolStripMenuItem");
            if (runMenuItem != null)
            {
                runMenuItem.Click += (s, e) => RunAnalysis();
            }
        }

        private ToolStripMenuItem GetMenuItem(string name)
        {
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is MenuStrip menuStrip)
                {
                    foreach (ToolStripMenuItem item in menuStrip.Items)
                    {
                        var found = FindMenuItem(item, name);
                        if (found != null) return found;
                    }
                }
            }
            return null;
        }

        private ToolStripMenuItem FindMenuItem(ToolStripMenuItem item, string name)
        {
            if (item.Name == name) return item;

            foreach (ToolStripMenuItem subItem in item.DropDownItems.OfType<ToolStripMenuItem>())
            {
                var found = FindMenuItem(subItem, name);
                if (found != null) return found;
            }

            return null;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!CheckUnsavedChanges())
            {
                e.Cancel = true;
                return;
            }

            base.OnFormClosing(e);
        }

        private void InitializeEditMenu()
        {
            отменитьToolStripMenuItem.Click += (s, e) => Undo();
            отменитьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Z;

            повторитьToolStripMenuItem.Click += (s, e) => Redo();
            повторитьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Y;

            вырезатьToolStripMenuItem.Click += (s, e) => Cut();
            вырезатьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.X;

            копироватьToolStripMenuItem.Click += (s, e) => Copy();
            копироватьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.C;

            вставитьToolStripMenuItem.Click += (s, e) => Paste();
            вставитьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.V;

            удалитьToolStripMenuItem.Click += (s, e) => Delete();
            удалитьToolStripMenuItem.ShortcutKeys = Keys.Delete;

            выделитьВсеToolStripMenuItem.Click += (s, e) => SelectAll();
            выделитьВсеToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.A;

            textBoxEditor.TextChanged += TextBoxEditor_TextChanged;
            textBoxEditor.KeyUp += (s, e) => UpdateMenuState();
            textBoxEditor.MouseUp += (s, e) => UpdateMenuState();

            splitContainer1.Panel1.Padding = new Padding(47);

            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Orientation = Orientation.Horizontal;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.SplitterDistance = this.Width / 2;

            textBoxEditor.Dock = DockStyle.Fill;
            textBoxEditor.Multiline = true;
            textBoxEditor.ScrollBars = RichTextBoxScrollBars.Both;

            // Настройка DataGridView
            if (dgvResults != null)
            {
                dgvResults.Dock = DockStyle.Fill;
                dgvResults.BackgroundColor = Color.FromArgb(240, 240, 240);
                dgvResults.BorderStyle = BorderStyle.None;
                dgvResults.Font = new Font("Consolas", 10);
            }

            // Очищаем Panel2 и добавляем DataGridView
            splitContainer1.Panel2.Controls.Clear();
            splitContainer1.Panel2.Controls.Add(dgvResults);

            splitContainer1.Panel1.Controls.Add(textBoxEditor);

            splitContainer1.Panel1MinSize = 200;
            splitContainer1.Panel2MinSize = 200;
        }

        private void TextBoxEditor_TextChanged(object sender, EventArgs e)
        {
            if (isUndoRedoOperation) return;

            if (previousText != textBoxEditor.Text)
            {
                undoStack.Push(previousText);
                previousText = textBoxEditor.Text;

                redoStack.Clear();
            }

            UpdateMenuState();
        }

        private void Undo()
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push(textBoxEditor.Text);

                isUndoRedoOperation = true;
                textBoxEditor.Text = undoStack.Pop();
                textBoxEditor.SelectionStart = textBoxEditor.TextLength;
                previousText = textBoxEditor.Text;
                isUndoRedoOperation = false;

                UpdateMenuState();
            }
        }

        private void Redo()
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push(textBoxEditor.Text);

                isUndoRedoOperation = true;
                textBoxEditor.Text = redoStack.Pop();
                textBoxEditor.SelectionStart = textBoxEditor.TextLength;
                previousText = textBoxEditor.Text;
                isUndoRedoOperation = false;

                UpdateMenuState();
            }
        }

        private void Cut()
        {
            if (!string.IsNullOrEmpty(textBoxEditor.SelectedText))
            {
                SaveStateBeforeAction();

                Clipboard.SetText(textBoxEditor.SelectedText);
                int start = textBoxEditor.SelectionStart;
                textBoxEditor.Text = textBoxEditor.Text.Remove(start, textBoxEditor.SelectionLength);
                textBoxEditor.SelectionStart = start;
            }
        }

        private void Copy()
        {
            if (!string.IsNullOrEmpty(textBoxEditor.SelectedText))
            {
                Clipboard.SetText(textBoxEditor.SelectedText);
            }
        }

        private void Paste()
        {
            if (Clipboard.ContainsText())
            {
                SaveStateBeforeAction();

                string textToPaste = Clipboard.GetText();
                int start = textBoxEditor.SelectionStart;

                if (textBoxEditor.SelectionLength > 0)
                {
                    textBoxEditor.Text = textBoxEditor.Text.Remove(start, textBoxEditor.SelectionLength);
                }

                textBoxEditor.Text = textBoxEditor.Text.Insert(start, textToPaste);
                textBoxEditor.SelectionStart = start + textToPaste.Length;
            }
        }

        private void Delete()
        {
            if (!string.IsNullOrEmpty(textBoxEditor.SelectedText))
            {
                SaveStateBeforeAction();

                int start = textBoxEditor.SelectionStart;
                textBoxEditor.Text = textBoxEditor.Text.Remove(start, textBoxEditor.SelectionLength);
                textBoxEditor.SelectionStart = start;
            }
        }

        private void SelectAll()
        {
            textBoxEditor.SelectAll();
            textBoxEditor.Focus();
        }

        private void SaveStateBeforeAction()
        {
            if (!isUndoRedoOperation && previousText != textBoxEditor.Text)
            {
                undoStack.Push(previousText);
                redoStack.Clear();
            }
        }

        private void UpdateMenuState()
        {
            отменитьToolStripMenuItem.Enabled = undoStack.Count > 0;

            повторитьToolStripMenuItem.Enabled = redoStack.Count > 0;

            bool hasSelection = !string.IsNullOrEmpty(textBoxEditor.SelectedText);
            вырезатьToolStripMenuItem.Enabled = hasSelection;
            копироватьToolStripMenuItem.Enabled = hasSelection;
            удалитьToolStripMenuItem.Enabled = hasSelection;

            вставитьToolStripMenuItem.Enabled = Clipboard.ContainsText();

            выделитьВсеToolStripMenuItem.Enabled = !string.IsNullOrEmpty(textBoxEditor.Text);
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string htmlFilePath = Application.StartupPath + "\\Info\\AboutProgram.html";

            System.Diagnostics.Process.Start(htmlFilePath);
        }

        private void вызовСправкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string htmlFilePath = Application.StartupPath + "\\Info\\UserHelp.html";

            System.Diagnostics.Process.Start(htmlFilePath);
        }

        private void left_Click(object sender, EventArgs e)
        {
            Undo();
        }

        private void right_Click(object sender, EventArgs e)
        {
            Redo();
        }

        private void copy_Click(object sender, EventArgs e)
        {
            Copy();
        }

        private void scissors_Click(object sender, EventArgs e)
        {
            Cut();
        }

        private void insert_Click(object sender, EventArgs e)
        {
            Paste();
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CheckUnsavedChanges())
            {
                Application.Exit();
            }
        }

        private void InitializeFileMenu()
        {
            создатьToolStripMenuItem.Click += (s, e) => CreateNewFile();
            создатьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.N;

            открытьToolStripMenuItem.Click += (s, e) => OpenFile();
            открытьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;

            сохранитьToolStripMenuItem.Click += (s, e) => SaveFile();
            сохранитьToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;

            сохранитьКакToolStripMenuItem.Click += (s, e) => SaveFileAs();
            сохранитьКакToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;

            выходToolStripMenuItem.Click += (s, e) => this.Close();

            textBoxEditor.TextChanged += (s, e) => CheckForModifications();
        }

        private void CreateNewFile()
        {
            if (isTextModified)
            {
                DialogResult result = MessageBox.Show(
                    "Сохранить изменения в текущем файле?",
                    "Создание нового файла",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    if (!SaveFile())
                    {
                        return;
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            textBoxEditor.Clear();
            currentFilePath = string.Empty;
            isTextModified = false;
            lastSavedText = "";

            undoStack.Clear();
            redoStack.Clear();
            undoStack.Push("");

            UpdateWindowTitle();
        }

        private void OpenFile()
        {
            if (isTextModified)
            {
                DialogResult result = MessageBox.Show(
                    "Сохранить изменения в текущем файле?",
                    "Открытие файла",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    if (!SaveFile())
                    {
                        return;
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Файлы исходного кода (*.cs;*.cpp;*.java)|*.cs;*.cpp;*.java|Все файлы (*.*)|*.*"; openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Title = "Открыть файл";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string fileContent = System.IO.File.ReadAllText(openFileDialog.FileName, Encoding.UTF8);

                        textBoxEditor.Text = fileContent;

                        currentFilePath = openFileDialog.FileName;
                        isTextModified = false;
                        lastSavedText = fileContent;

                        undoStack.Clear();
                        redoStack.Clear();
                        undoStack.Push(fileContent);
                        previousText = fileContent;

                        UpdateWindowTitle();
                        UpdateMenuState();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при открытии файла:\n{ex.Message}",
                            "Ошибка",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }

        private bool SaveFile()
        {

            if (string.IsNullOrEmpty(currentFilePath))
            {
                return SaveFileAs();
            }
            else
            {
                return SaveToFile(currentFilePath);
            }
        }

        private bool SaveFileAs()
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.Title = "Сохранить файл как";
                saveFileDialog.FileName = string.IsNullOrEmpty(currentFilePath)
                    ? "Новый документ.txt"
                    : System.IO.Path.GetFileName(currentFilePath);

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return SaveToFile(saveFileDialog.FileName);
                }
            }

            return false;
        }
        private bool SaveToFile(string filePath)
        {
            try
            {
                string textToSave = textBoxEditor.Text;

                System.IO.File.WriteAllText(filePath, textToSave, Encoding.UTF8);

                currentFilePath = filePath;
                isTextModified = false;
                lastSavedText = textToSave;

                UpdateWindowTitle();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }
        private void CheckForModifications()
        {
            isTextModified = (textBoxEditor.Text != lastSavedText);
            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            string fileName = string.IsNullOrEmpty(currentFilePath)
                ? "Безымянный Файл"
                : System.IO.Path.GetFileName(currentFilePath);

            string modifiedIndicator = isTextModified ? " *" : "";

            this.Text = $"{fileName}{modifiedIndicator} - Компиляторный Редактор";
        }

        private bool CheckUnsavedChanges()
        {
            if (isTextModified)
            {
                string fileName = string.IsNullOrEmpty(currentFilePath)
                    ? "Безымянный Файл"
                    : System.IO.Path.GetFileName(currentFilePath);

                DialogResult result = MessageBox.Show(
                    $"Сохранить изменения в файле \"{fileName}\"?",
                    "Несохраненные изменения",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    return SaveFile();
                }
                else if (result == DialogResult.Cancel)
                {
                    return false;
                }
            }
            return true;
        }

        private void folder_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void save_Click(object sender, EventArgs e)
        {
            SaveFile();
            //SaveFileAs();
        }

        private void file_Click(object sender, EventArgs e)
        {
            CreateNewFile();
        }

        private void runButton_Click(object sender, EventArgs e)
        {
            RunAnalysis();
        }

        private void questionButton_Click(object sender, EventArgs e)
        {
            вызовСправкиToolStripMenuItem_Click(sender, e);
        }

        private void infoButton_Click(object sender, EventArgs e)
        {
            оПрограммеToolStripMenuItem_Click(sender, e);
        }

        private void DgvResults_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvResults.Rows.Count - 1) return;

            DataGridViewRow row = dgvResults.Rows[e.RowIndex];

            // Проверяем, есть ли в строке информация об ошибке
            if (row.Cells["Description"] != null && !string.IsNullOrEmpty(row.Cells["Description"].Value?.ToString()))
            {
                if (int.TryParse(row.Cells["Line"].Value?.ToString(), out int line) &&
                    int.TryParse(row.Cells["Position"].Value?.ToString(), out int position))
                {
                    NavigateToErrorPositionFromGrid(line, position);
                }
            }
            else if (row.Cells["Position"] != null && !string.IsNullOrEmpty(row.Cells["Position"].Value?.ToString()))
            {
                string posStr = row.Cells["Position"].Value.ToString();
                int dashIndex = posStr.IndexOf('-');
                if (dashIndex > 0 && int.TryParse(posStr.Substring(1, dashIndex - 1), out int startPos))
                {
                    NavigateToErrorPositionFromGrid(GetLineFromRow(row), startPos);
                }
            }
        }
    }
}