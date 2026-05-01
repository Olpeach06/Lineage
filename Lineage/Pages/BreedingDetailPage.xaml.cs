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
    public partial class BreedingDetailPage : Page
    {
        private int breedingId;

        public BreedingDetailPage(int breedingId)
        {
            InitializeComponent();
            this.breedingId = breedingId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadBreedingData();
        }

        private void LoadBreedingData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var breeding = context.Breedings.FirstOrDefault(b => b.Id == breedingId);
                    if (breeding == null)
                    {
                        MessageBox.Show("Вязка не найдена!");
                        NavigationService.GoBack();
                        return;
                    }

                    var male = context.Animals.Find(breeding.MaleId);
                    var female = context.Animals.Find(breeding.FemaleId);

                    txtMale.Text = male?.Nickname ?? $"ID: {breeding.MaleId}";
                    txtFemale.Text = female?.Nickname ?? $"ID: {breeding.FemaleId}";
                    txtBreedingDate.Text = breeding.BreedingDate.ToString("dd.MM.yyyy");
                    txtExpectedBirth.Text = breeding.ExpectedBirthDate?.ToString("dd.MM.yyyy") ?? "—";
                    txtActualBirth.Text = breeding.ActualBirthDate?.ToString("dd.MM.yyyy") ?? "—";
                    txtIsSuccessful.Text = breeding.IsSuccessful == true ? "Да" : (breeding.IsSuccessful == false ? "Нет" : "—");
                    txtOffspringCount.Text = breeding.OffspringCount?.ToString() ?? "—";
                    txtAliveCount.Text = breeding.AliveCount?.ToString() ?? "—";
                    txtNotes.Text = breeding.Notes ?? "—";
                    txtCreatedAt.Text = breeding.CreatedAt.ToString("dd.MM.yyyy HH:mm");
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
