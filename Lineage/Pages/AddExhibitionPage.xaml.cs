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
    public partial class AddExhibitionPage : Page
    {
        private int animalId;
        private int currentTreeId;

        public AddExhibitionPage(int animalId)
        {
            InitializeComponent();
            this.animalId = animalId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            currentTreeId = Session.CurrentTreeId;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtExhibitionName.Text))
                {
                    MessageBox.Show("Введите название выставки!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtExhibitionName.Focus();
                    return;
                }

                if (!dpExhibitionDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Укажите дату проведения выставки!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int rating = 0;
                if (!string.IsNullOrWhiteSpace(txtRating.Text))
                    int.TryParse(txtRating.Text, out rating);

                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var exhibition = new Exhibitions
                    {
                        TreeId = currentTreeId,
                        AnimalId = animalId,
                        ExhibitionName = txtExhibitionName.Text.Trim(),
                        ExhibitionDate = dpExhibitionDate.SelectedDate.Value,
                        Location = string.IsNullOrWhiteSpace(txtLocation.Text) ? null : txtLocation.Text.Trim(),
                        Result = string.IsNullOrWhiteSpace(txtResult.Text) ? null : txtResult.Text.Trim(),
                        JudgeName = string.IsNullOrWhiteSpace(txtJudgeName.Text) ? null : txtJudgeName.Text.Trim(),
                        Rating = rating > 0 ? (int?)rating : null,
                        CertificateNumber = string.IsNullOrWhiteSpace(txtCertificateNumber.Text) ? null : txtCertificateNumber.Text.Trim(),
                        Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim(),
                        CreatedByUserId = Session.UserId,
                        CreatedAt = DateTime.Now
                    };

                    context.Exhibitions.Add(exhibition);
                    context.SaveChanges();

                    MessageBox.Show("Выставка добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}