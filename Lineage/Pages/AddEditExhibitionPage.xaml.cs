using Lineage.AppData;
using Lineage.Classes;
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

namespace Lineage.Pages
{
    public partial class AddEditExhibitionPage : Page
    {
        private int animalId;           // ID текущего животного
        private int? exhibitionId;      // null = добавление, не null = редактирование
        private int currentAnimalTreeId;

        public AddEditExhibitionPage(int animalId, int? exhibitionId = null)
        {
            InitializeComponent();
            this.animalId = animalId;
            this.exhibitionId = exhibitionId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAnimalInfo();

            if (exhibitionId.HasValue)
            {
                txtTitle.Text = "РЕДАКТИРОВАНИЕ ВЫСТАВКИ";
                LoadExhibitionData();
            }
            else
            {
                txtTitle.Text = "ДОБАВЛЕНИЕ ВЫСТАВКИ";
            }
        }

        private void LoadAnimalInfo()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var animal = context.Animals.Find(animalId);
                    if (animal == null)
                    {
                        MessageBox.Show("Животное не найдено", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                        return;
                    }
                    currentAnimalTreeId = animal.TreeId;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки информации о животном: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadExhibitionData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var exhibition = context.Exhibitions.Find(exhibitionId.Value);
                    if (exhibition == null)
                    {
                        MessageBox.Show("Запись о выставке не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                        return;
                    }

                    txtExhibitionName.Text = exhibition.ExhibitionName;
                    dpExhibitionDate.SelectedDate = exhibition.ExhibitionDate;
                    txtLocation.Text = exhibition.Location ?? "";
                    txtResult.Text = exhibition.Result ?? "";
                    txtJudgeName.Text = exhibition.JudgeName ?? "";
                    txtRating.Text = exhibition.Rating?.ToString() ?? "";
                    txtCertificateNumber.Text = exhibition.CertificateNumber ?? "";
                    txtNotes.Text = exhibition.Notes ?? "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка обязательных полей
                if (string.IsNullOrWhiteSpace(txtExhibitionName.Text))
                {
                    MessageBox.Show("Введите название выставки!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!dpExhibitionDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Укажите дату проведения выставки!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получаем рейтинг
                int? rating = null;
                if (!string.IsNullOrWhiteSpace(txtRating.Text))
                {
                    if (int.TryParse(txtRating.Text, out int r))
                        rating = r;
                    else
                    {
                        MessageBox.Show("Оценка должна быть целым числом!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    Exhibitions exhibition;

                    if (exhibitionId.HasValue)
                    {
                        // Режим редактирования
                        exhibition = context.Exhibitions.Find(exhibitionId.Value);
                        if (exhibition == null)
                        {
                            MessageBox.Show("Запись о выставке не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        // Режим добавления
                        exhibition = new Exhibitions();
                        context.Exhibitions.Add(exhibition);
                    }

                    exhibition.TreeId = currentAnimalTreeId;
                    exhibition.AnimalId = animalId;
                    exhibition.ExhibitionName = txtExhibitionName.Text.Trim();
                    exhibition.ExhibitionDate = dpExhibitionDate.SelectedDate.Value;
                    exhibition.Location = string.IsNullOrWhiteSpace(txtLocation.Text) ? null : txtLocation.Text.Trim();
                    exhibition.Result = string.IsNullOrWhiteSpace(txtResult.Text) ? null : txtResult.Text.Trim();
                    exhibition.JudgeName = string.IsNullOrWhiteSpace(txtJudgeName.Text) ? null : txtJudgeName.Text.Trim();
                    exhibition.Rating = rating;
                    exhibition.CertificateNumber = string.IsNullOrWhiteSpace(txtCertificateNumber.Text) ? null : txtCertificateNumber.Text.Trim();
                    exhibition.Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim();
                    exhibition.CreatedByUserId = Session.UserId;
                    exhibition.CreatedAt = DateTime.Now;

                    context.SaveChanges();

                    MessageBox.Show(exhibitionId.HasValue ? "Запись о выставке успешно обновлена!" : "Запись о выставке успешно добавлена!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}