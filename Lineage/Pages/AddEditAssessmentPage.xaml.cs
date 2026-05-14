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
    public partial class AddEditAssessmentPage : Page
    {
        private int animalId;
        private int? assessmentId; // null = добавление, не null = редактирование

        public AddEditAssessmentPage(int animalId, int? assessmentId = null)
        {
            InitializeComponent();
            this.animalId = animalId;
            this.assessmentId = assessmentId;

            // Устанавливаем сегодняшнюю дату
            dpAssessmentDate.SelectedDate = DateTime.Today;

            LoadClasses();

            if (assessmentId.HasValue)
            {
                txtTitle.Text = "РЕДАКТИРОВАНИЕ ПЛЕМЕННОЙ ОЦЕНКИ";
                LoadAssessmentData();
            }
            else
            {
                txtTitle.Text = "ДОБАВЛЕНИЕ ПЛЕМЕННОЙ ОЦЕНКИ";
            }
        }

        private void LoadClasses()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var classes = context.PedigreeClasses.OrderBy(c => c.Code).ToList();
                    cmbClass.ItemsSource = classes;
                    if (classes.Any())
                        cmbClass.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки классов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAssessmentData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var assessment = context.AnimalAssessments.Find(assessmentId.Value);
                    if (assessment == null)
                    {
                        MessageBox.Show("Оценка не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                        return;
                    }

                    dpAssessmentDate.SelectedDate = assessment.AssessmentDate;
                    cmbClass.SelectedValue = assessment.ClassId;
                    txtOverallScore.Text = assessment.OverallScore?.ToString("F2") ?? "";
                    txtExteriorScore.Text = assessment.ExteriorScore?.ToString("F2") ?? "";
                    txtProductivityScore.Text = assessment.ProductivityScore?.ToString("F2") ?? "";
                    txtOffspringScore.Text = assessment.OffspringScore?.ToString("F2") ?? "";
                    txtCommissionMembers.Text = assessment.CommissionMembers ?? "";
                    txtCertificateNumber.Text = assessment.CertificateNumber ?? "";
                    dpValidUntil.SelectedDate = assessment.ValidUntil;
                    txtNotes.Text = assessment.Notes ?? "";
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
                if (!dpAssessmentDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Укажите дату оценки!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbClass.SelectedItem == null)
                {
                    MessageBox.Show("Выберите племенной класс!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    AnimalAssessments assessment;

                    if (assessmentId.HasValue)
                    {
                        assessment = context.AnimalAssessments.Find(assessmentId.Value);
                        if (assessment == null)
                        {
                            MessageBox.Show("Оценка не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        assessment = new AnimalAssessments();
                        context.AnimalAssessments.Add(assessment);
                    }

                    assessment.AnimalId = animalId;
                    assessment.AssessmentDate = dpAssessmentDate.SelectedDate.Value;
                    assessment.ClassId = (int)cmbClass.SelectedValue;
                    assessment.OverallScore = ParseDecimal(txtOverallScore.Text);
                    assessment.ExteriorScore = ParseDecimal(txtExteriorScore.Text);
                    assessment.ProductivityScore = ParseDecimal(txtProductivityScore.Text);
                    assessment.OffspringScore = ParseDecimal(txtOffspringScore.Text);
                    assessment.CommissionMembers = string.IsNullOrWhiteSpace(txtCommissionMembers.Text) ? null : txtCommissionMembers.Text.Trim();
                    assessment.CertificateNumber = string.IsNullOrWhiteSpace(txtCertificateNumber.Text) ? null : txtCertificateNumber.Text.Trim();
                    assessment.ValidUntil = dpValidUntil.SelectedDate;
                    assessment.Notes = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim();
                    assessment.CreatedByUserId = Session.UserId;
                    assessment.CreatedAt = DateTime.Now;

                    context.SaveChanges();

                    MessageBox.Show(assessmentId.HasValue ? "Оценка успешно обновлена!" : "Оценка успешно добавлена!",
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

        private decimal? ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (decimal.TryParse(text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal result))
                return result;

            return null;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}