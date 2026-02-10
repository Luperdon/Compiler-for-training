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
        public CompilerForm()
        {
            InitializeComponent();

            отменитьToolStripMenuItem.Click += (s, e) => Cancel();
            //повторитьToolStripMenuItem.Click += (s, e) => Repeat();
            //вырезатьToolStripMenuItem.Click += (s, e) => Вырезать();
            //копироватьToolStripMenuItem.Click += (s, e) => Копировать();
            //вставитьToolStripMenuItem.Click += (s, e) => Вставить();
            //удалитьToolStripMenuItem.Click += (s, e) => Удалить();
            //выделитьВсеToolStripMenuItem.Click += (s, e) => ВыделитьВсе();

            //textBoxEditor.TextChanged += (s, e) => ОбновитьСостояниеМеню();
            //textBoxEditor.SelectionChanged += (s, e) => ОбновитьСостояниеМеню();

            //ОбновитьСостояниеМеню();
        }

        private void Cancel()
        {
            if (textBoxEditor.CanUndo)
            {
                textBoxEditor.Undo();
            }
        }

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
    }
}
