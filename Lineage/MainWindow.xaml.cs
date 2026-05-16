using Lineage.AppData;
using Lineage.Classes;
using Lineage.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Collections.Specialized.BitVector32;

namespace Lineage
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AppConnect.modeldb = new GenealogyUnifiedDBEntities1();
            AppFrame.frameMain = mainFrame;
            mainFrame.Navigate(new LoginPage());
        }

        // Публичное свойство для доступа к Frame из других страниц
        public Frame MainFrame => mainFrame;

        // Метод для установки режима и загрузки главной страницы
        public void SetModeAndLoadMainPage(int mode)
        {
            AppSettings.CurrentMode = mode;
            Session.CurrentMode = mode;

            // Обновляем заголовок окна в зависимости от режима
            UpdateWindowTitle();

            // Загружаем главную страницу
            mainFrame.Navigate(new MainPage());
        }

        // Обновление заголовка окна
        private void UpdateWindowTitle()
        {
            string title = "Генеалогическая система";

            if (AppSettings.IsFamilyMode)
                title += " - Семейное древо";
            else if (AppSettings.IsBreedingMode)
                title += " - Племенная книга";

            this.Title = title;
        }
    }
}