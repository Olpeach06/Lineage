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
using Lineage.AppData;
using Lineage.Classes;

namespace Lineage.Pages
{
    public partial class ExhibitionDetailPage : Page
    {
        private int exhibitionId;

        public ExhibitionDetailPage(int exhibitionId)
        {
            InitializeComponent();
            this.exhibitionId = exhibitionId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Session.IsBreedingMode)
            {
                MessageBox.Show("Эта страница доступна только в режиме племенной книги!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.GoBack();
                return;
            }

            LoadExhibitionData();
        }

        private void LoadExhibitionData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var exhibition = context.Exhibitions.FirstOrDefault(e => e.Id == exhibitionId);
                    if (exhibition == null)
                    {
                        MessageBox.Show("Выставка не найдена!");
                        NavigationService.GoBack();
                        return;
                    }

                    var animal = context.Animals.Find(exhibition.AnimalId);

                    txtExhibitionName.Text = exhibition.ExhibitionName;
                    txtAnimalName.Text = animal?.Nickname ?? $"ID: {exhibition.AnimalId}";
                    txtExhibitionDate.Text = exhibition.ExhibitionDate.ToString("dd.MM.yyyy");
                    txtLocation.Text = exhibition.Location ?? "—";
                    txtResult.Text = exhibition.Result ?? "—";
                    txtJudgeName.Text = exhibition.JudgeName ?? "—";
                    txtRating.Text = exhibition.Rating?.ToString() ?? "—";
                    txtCertificateNumber.Text = exhibition.CertificateNumber ?? "—";
                    txtNotes.Text = exhibition.Notes ?? "—";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}