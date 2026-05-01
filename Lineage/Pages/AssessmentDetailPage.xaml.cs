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
    public partial class AssessmentDetailPage : Page
    {
        private int assessmentId;

        public AssessmentDetailPage(int assessmentId)
        {
            InitializeComponent();
            this.assessmentId = assessmentId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAssessmentData();
        }

        private void LoadAssessmentData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities())
                {
                    var assessment = context.AnimalAssessments.FirstOrDefault(a => a.Id == assessmentId);
                    if (assessment == null)
                    {
                        MessageBox.Show("Оценка не найдена!");
                        NavigationService.GoBack();
                        return;
                    }

                    var animal = context.Animals.Find(assessment.AnimalId);
                    var pedigreeClass = context.PedigreeClasses.Find(assessment.ClassId);
                    var creator = context.Users.Find(assessment.CreatedByUserId);

                    txtAnimalName.Text = animal?.Nickname ?? $"ID: {assessment.AnimalId}";
                    txtAssessmentDate.Text = assessment.AssessmentDate.ToString("dd.MM.yyyy");
                    txtClass.Text = pedigreeClass?.Name ?? $"Класс {assessment.ClassId}";
                    txtOverallScore.Text = assessment.OverallScore?.ToString("F2") ?? "—";
                    txtExteriorScore.Text = assessment.ExteriorScore?.ToString("F2") ?? "—";
                    txtProductivityScore.Text = assessment.ProductivityScore?.ToString("F2") ?? "—";
                    txtOffspringScore.Text = assessment.OffspringScore?.ToString("F2") ?? "—";
                    txtCommissionMembers.Text = assessment.CommissionMembers ?? "—";
                    txtCertificateNumber.Text = assessment.CertificateNumber ?? "—";
                    txtValidUntil.Text = assessment.ValidUntil?.ToString("dd.MM.yyyy") ?? "—";
                    txtNotes.Text = assessment.Notes ?? "—";
                    txtCreatedBy.Text = creator?.Username ?? $"ID: {assessment.CreatedByUserId}";
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