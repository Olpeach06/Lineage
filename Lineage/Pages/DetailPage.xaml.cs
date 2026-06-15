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
    public enum DetailType
    {
        Breeding,
        Exhibition,
        Assessment,
        HealthEvent
    }

    public partial class DetailPage : Page
    {
        private DetailType currentType;
        private int itemId;
        private string itemTitle;

        public DetailPage(DetailType type, int id)
        {
            InitializeComponent();
            currentType = type;
            itemId = id;

            switch (type)
            {
                case DetailType.Breeding:
                    itemTitle = "Детали вязки";
                    break;
                case DetailType.Exhibition:
                    itemTitle = "Детали выставки";
                    break;
                case DetailType.Assessment:
                    itemTitle = "Детали оценки";
                    break;
                case DetailType.HealthEvent:
                    itemTitle = "Детали события здоровья";
                    break;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            txtTitle.Text = itemTitle;

            switch (currentType)
            {
                case DetailType.Breeding:
                    LoadBreedingData();
                    break;
                case DetailType.Exhibition:
                    LoadExhibitionData();
                    break;
                case DetailType.Assessment:
                    LoadAssessmentData();
                    break;
                case DetailType.HealthEvent:
                    LoadHealthEventData();
                    break;
            }
        }

        // ==================== ВЯЗКА ====================
        private void LoadBreedingData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var breeding = context.Breedings.FirstOrDefault(b => b.Id == itemId);
                    if (breeding == null)
                    {
                        MessageBox.Show("Вязка не найдена!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                        return;
                    }

                    var male = context.Animals.Find(breeding.MaleId);
                    var female = context.Animals.Find(breeding.FemaleId);

                    var content = new StackPanel();

                    var mainBlock = CreateBlock("🔗 ИНФОРМАЦИЯ О ВЯЗКЕ");
                    var grid = CreateGrid(10);

                    AddGridRow(grid, 0, "Производитель:", male?.Nickname ?? $"ID: {breeding.MaleId}");
                    AddGridRow(grid, 1, "Матка:", female?.Nickname ?? $"ID: {breeding.FemaleId}");
                    AddGridRow(grid, 2, "Дата вязки:", breeding.BreedingDate.ToString("dd.MM.yyyy"));
                    AddGridRow(grid, 3, "Ожидаемая дата родов:", breeding.ExpectedBirthDate?.ToString("dd.MM.yyyy") ?? "---");
                    AddGridRow(grid, 4, "Фактическая дата родов:", breeding.ActualBirthDate?.ToString("dd.MM.yyyy") ?? "---");
                    AddGridRow(grid, 5, "Успешность:", breeding.IsSuccessful == true ? "Да" : (breeding.IsSuccessful == false ? "Нет" : "---"));
                    AddGridRow(grid, 6, "Количество потомков:", breeding.OffspringCount?.ToString() ?? "---");
                    AddGridRow(grid, 7, "Выжило:", breeding.AliveCount?.ToString() ?? "---");
                    AddGridRow(grid, 8, "Примечания:", breeding.Notes ?? "---");
                    AddGridRow(grid, 9, "Создано:", breeding.CreatedAt.ToString("dd.MM.yyyy HH:mm"));

                    mainBlock.Child = grid;
                    content.Children.Add(mainBlock);
                    DynamicContent.Content = content;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== ВЫСТАВКА ====================
        private void LoadExhibitionData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var exhibition = context.Exhibitions.FirstOrDefault(e => e.Id == itemId);
                    if (exhibition == null)
                    {
                        MessageBox.Show("Выставка не найдена!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                        return;
                    }

                    var animal = context.Animals.Find(exhibition.AnimalId);

                    var content = new StackPanel();

                    var mainBlock = CreateBlock("🏆 ИНФОРМАЦИЯ О ВЫСТАВКЕ");
                    var grid = CreateGrid(9);

                    AddGridRow(grid, 0, "Название:", exhibition.ExhibitionName);
                    AddGridRow(grid, 1, "Животное:", animal?.Nickname ?? $"ID: {exhibition.AnimalId}");
                    AddGridRow(grid, 2, "Дата проведения:", exhibition.ExhibitionDate.ToString("dd.MM.yyyy"));
                    AddGridRow(grid, 3, "Место проведения:", exhibition.Location ?? "---");
                    AddGridRow(grid, 4, "Результат:", exhibition.Result ?? "---");
                    AddGridRow(grid, 5, "Судья:", exhibition.JudgeName ?? "---");
                    AddGridRow(grid, 6, "Оценка:", exhibition.Rating?.ToString() ?? "---");
                    AddGridRow(grid, 7, "Номер сертификата:", exhibition.CertificateNumber ?? "---");
                    AddGridRow(grid, 8, "Примечания:", exhibition.Notes ?? "---");

                    mainBlock.Child = grid;
                    content.Children.Add(mainBlock);
                    DynamicContent.Content = content;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== ОЦЕНКА ====================
        private void LoadAssessmentData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var assessment = context.AnimalAssessments.FirstOrDefault(a => a.Id == itemId);
                    if (assessment == null)
                    {
                        MessageBox.Show("Оценка не найдена!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                        return;
                    }

                    var animal = context.Animals.Find(assessment.AnimalId);
                    var pedigreeClass = context.PedigreeClasses.Find(assessment.ClassId);
                    var creator = context.Users.Find(assessment.CreatedByUserId);

                    var content = new StackPanel();

                    var mainBlock = CreateBlock("⭐ ИНФОРМАЦИЯ ОБ ОЦЕНКЕ");
                    var grid = CreateGrid(12);

                    AddGridRow(grid, 0, "Животное:", animal?.Nickname ?? $"ID: {assessment.AnimalId}");
                    AddGridRow(grid, 1, "Дата оценки:", assessment.AssessmentDate.ToString("dd.MM.yyyy"));
                    AddGridRow(grid, 2, "Племенной класс:", pedigreeClass?.Name ?? $"Класс {assessment.ClassId}");
                    AddGridRow(grid, 3, "Общий балл:", assessment.OverallScore?.ToString("F2") ?? "---");
                    AddGridRow(grid, 4, "Оценка экстерьера:", assessment.ExteriorScore?.ToString("F2") ?? "---");
                    AddGridRow(grid, 5, "Оценка продуктивности:", assessment.ProductivityScore?.ToString("F2") ?? "---");
                    AddGridRow(grid, 6, "Оценка потомства:", assessment.OffspringScore?.ToString("F2") ?? "---");
                    AddGridRow(grid, 7, "Члены комиссии:", assessment.CommissionMembers ?? "---");
                    AddGridRow(grid, 8, "Номер свидетельства:", assessment.CertificateNumber ?? "---");
                    AddGridRow(grid, 9, "Срок действия:", assessment.ValidUntil?.ToString("dd.MM.yyyy") ?? "---");
                    AddGridRow(grid, 10, "Примечания:", assessment.Notes ?? "---");
                    AddGridRow(grid, 11, "Оценил:", creator?.Username ?? $"ID: {assessment.CreatedByUserId}");

                    mainBlock.Child = grid;
                    content.Children.Add(mainBlock);
                    DynamicContent.Content = content;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== ВЕТЕРИНАРНОЕ СОБЫТИЕ ====================
        private void LoadHealthEventData()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var vetEvent = context.VeterinaryEvents.FirstOrDefault(v => v.Id == itemId);
                    if (vetEvent == null)
                    {
                        MessageBox.Show("Событие не найдено!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        NavigationService.GoBack();
                        return;
                    }

                    var eventType = context.VeterinaryEventTypes.Find(vetEvent.EventTypeId);
                    var animal = context.Animals.Find(vetEvent.AnimalId);
                    var user = context.Users.Find(vetEvent.CreatedByUserId);

                    var content = new StackPanel();

                    var mainBlock = CreateBlock("💊 ИНФОРМАЦИЯ О СОБЫТИИ ЗДОРОВЬЯ");
                    var grid = CreateGrid(12);

                    AddGridRow(grid, 0, "Животное:", animal?.Nickname ?? $"ID: {vetEvent.AnimalId}");
                    AddGridRow(grid, 1, "Дата события:", vetEvent.EventDate.ToString("dd.MM.yyyy"));
                    AddGridRow(grid, 2, "Тип события:", eventType?.Name ?? "---");
                    AddGridRow(grid, 3, "Препарат:", vetEvent.MedicineName ?? "---");
                    AddGridRow(grid, 4, "Дозировка:", vetEvent.Dosage ?? "---");
                    AddGridRow(grid, 5, "Ветеринар:", vetEvent.VetName ?? "---");
                    AddGridRow(grid, 6, "Срок ожидания (дней):", vetEvent.WithdrawalDays.HasValue ? $"{vetEvent.WithdrawalDays} дней" : "---");
                    AddGridRow(grid, 7, "Следующая обработка:", vetEvent.NextDueDate?.ToString("dd.MM.yyyy") ?? "---");
                    AddGridRow(grid, 8, "Стоимость:", vetEvent.Cost.HasValue ? $"{vetEvent.Cost.Value:F2} руб." : "---");
                    AddGridRow(grid, 9, "Примечания:", vetEvent.Notes ?? "---");
                    AddGridRow(grid, 10, "Добавил:", user?.Username ?? "Неизвестный пользователь");
                    AddGridRow(grid, 11, "Дата добавления:", vetEvent.CreatedAt.ToString("dd.MM.yyyy HH:mm"));

                    mainBlock.Child = grid;
                    content.Children.Add(mainBlock);
                    DynamicContent.Content = content;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================

        private Border CreateBlock(string title)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDF8F0")),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(25),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B7A48B")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C4E3D")),
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            border.Child = stackPanel;
            return border;
        }

        private Grid CreateGrid(int rowsCount)
        {
            var grid = new Grid();

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) }); // Отступ
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < rowsCount; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            grid.Margin = new Thickness(0, 10, 0, 0);
            return grid;
        }

        private void AddGridRow(Grid grid, int row, string label, string value)
        {
            var labelBlock = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B5E4A")),
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetRow(labelBlock, row);
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            var valueBlock = new TextBlock
            {
                Text = value,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C4E3D")),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 12)
            };
            Grid.SetRow(valueBlock, row);
            Grid.SetColumn(valueBlock, 2);
            grid.Children.Add(valueBlock);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}