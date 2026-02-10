using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        public CompilerForm()
        {
            InitializeComponent();

            previousText = textBoxEditor.Text;
            undoStack.Push(previousText);

            InitializeEditMenu();

            InitializeFileMenu();

            UpdateWindowTitle();

            UpdateMenuState();
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

            //UpdateToolStripButtons();
        }

        //private void UpdateToolStripButtons()
        //{
        //    // Обновляем состояние кнопок на ToolStrip (если они есть)
        //    if (toolStripButtonUndo != null)
        //    {
        //        toolStripButtonUndo.Enabled = отменитьToolStripMenuItem.Enabled;
        //    }

        //    if (toolStripButtonRedo != null)
        //    {
        //        toolStripButtonRedo.Enabled = повторитьToolStripMenuItem.Enabled;
        //    }

        //    if (toolStripButtonCut != null)
        //    {
        //        toolStripButtonCut.Enabled = вырезатьToolStripMenuItem.Enabled;
        //    }

        //    if (toolStripButtonCopy != null)
        //    {
        //        toolStripButtonCopy.Enabled = копироватьToolStripMenuItem.Enabled;
        //    }

        //    if (toolStripButtonPaste != null)
        //    {
        //        toolStripButtonPaste.Enabled = вставитьToolStripMenuItem.Enabled;
        //    }
        //}

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutProgramForm aboutProgram = new AboutProgramForm();
            aboutProgram.Show();
        }

        private void вызовСправкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserHelpForm userHelpForm = new UserHelpForm();
            userHelpForm.Show();
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
            this.Close();
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
                ? "Безымянный"
                : System.IO.Path.GetFileName(currentFilePath);

            string modifiedIndicator = isTextModified ? " *" : "";

            this.Text = $"{fileName}{modifiedIndicator} - Компиляторный Редактор";
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
    }
}