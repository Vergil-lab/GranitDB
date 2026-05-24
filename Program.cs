// Program.cs
using System;
using System.Windows.Forms;
using StroiSnabApp.Forms;

namespace StroiSnabApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Критическая ошибка запуска:\n\n" + ex.Message +
                    "\n\nПроверьте подключение к SQL Server и наличие базы StroiSnabDB.",
                    "Ошибка запуска", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
