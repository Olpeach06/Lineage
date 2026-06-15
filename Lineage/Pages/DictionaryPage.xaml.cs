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
using System.Collections.ObjectModel;

namespace Lineage.Pages
{
    public enum DictionaryType
    {
        Species,        // Виды
        Breeds,         // Породы
        Colors,         // Окрасы
        PedigreeClasses // Племенные классы
    }

    public class DictionaryItem
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public Visibility HasDescription => string.IsNullOrWhiteSpace(Description) ? Visibility.Collapsed : Visibility.Visible;
        public int? SpeciesId { get; set; }
        public int? Code { get; set; }
    }

    public partial class DictionaryPage : Page
    {
        private DictionaryType currentType;
        private ObservableCollection<DictionaryItem> items = new ObservableCollection<DictionaryItem>();

        public DictionaryPage(DictionaryType type)
        {
            InitializeComponent();
            currentType = type;
            // Подписываемся на событие навигации
            this.Loaded += DictionaryPage_Loaded;
        }

        private void DictionaryPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверка прав (только админ)
            if (!Session.IsAdmin)
            {
                MessageBox.Show("Доступ запрещён! Только для администраторов.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.GoBack();
                return;
            }

            if (Session.IsFamilyMode)
            {
                MessageBox.Show("Справочники доступны только в режиме животноводства!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.GoBack();
                return;
            }

            SetTitle();
            lvItems.ItemsSource = items;
            LoadData();

            // Подписываемся на событие возврата на страницу
            NavigationService.Navigated += NavigationService_Navigated;
        }

        // Обработчик возврата на страницу (после закрытия диалога)
        private void NavigationService_Navigated(object sender, NavigationEventArgs e)
        {
            // Если вернулись на эту страницу - обновляем данные
            if (e.Content == this)
            {
                LoadData();
            }
        }

        private void SetTitle()
        {
            switch (currentType)
            {
                case DictionaryType.Species:
                    txtTitle.Text = "СПРАВОЧНИК: ВИДЫ ЖИВОТНЫХ";
                    break;
                case DictionaryType.Breeds:
                    txtTitle.Text = "СПРАВОЧНИК: ПОРОДЫ";
                    break;
                case DictionaryType.Colors:
                    txtTitle.Text = "СПРАВОЧНИК: ОКРАСЫ / МАСТИ";
                    break;
                case DictionaryType.PedigreeClasses:
                    txtTitle.Text = "СПРАВОЧНИК: ПЛЕМЕННЫЕ КЛАССЫ";
                    break;
            }
        }

        private void LoadData()
        {
            try
            {
                items.Clear();

                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    switch (currentType)
                    {
                        case DictionaryType.Species:
                            var species = context.Species.OrderBy(s => s.Name).ToList();
                            foreach (var s in species)
                            {
                                items.Add(new DictionaryItem
                                {
                                    Id = s.Id,
                                    DisplayName = s.Name,
                                    Description = null
                                });
                            }
                            break;

                        case DictionaryType.Breeds:
                            var breeds = context.Breeds.OrderBy(b => b.Name).ToList();
                            foreach (var b in breeds)
                            {
                                var speciesName = context.Species.Find(b.SpeciesId)?.Name ?? "не указан";
                                items.Add(new DictionaryItem
                                {
                                    Id = b.Id,
                                    DisplayName = $"{b.Name} ({speciesName})",
                                    Description = b.Description,
                                    SpeciesId = b.SpeciesId
                                });
                            }
                            break;

                        case DictionaryType.Colors:
                            var colors = context.Colors.OrderBy(c => c.Name).ToList();
                            foreach (var c in colors)
                            {
                                var speciesName = c.SpeciesId.HasValue
                                    ? context.Species.Find(c.SpeciesId)?.Name
                                    : "все виды";
                                items.Add(new DictionaryItem
                                {
                                    Id = c.Id,
                                    DisplayName = $"{c.Name} ({speciesName})",
                                    Description = null,
                                    SpeciesId = c.SpeciesId
                                });
                            }
                            break;

                        case DictionaryType.PedigreeClasses:
                            var classes = context.PedigreeClasses.OrderBy(c => c.Code).ToList();
                            foreach (var pc in classes)
                            {
                                items.Add(new DictionaryItem
                                {
                                    Id = pc.Id,
                                    DisplayName = $"{pc.Name} (Код: {pc.Code})",
                                    Description = pc.Description,
                                    Code = pc.Code
                                });
                            }
                            break;
                    }
                }

                txtEmpty.Visibility = items.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверка прав (только админ)
            if (!Session.IsAdmin)
            {
                MessageBox.Show("Доступ запрещён! Только для администраторов.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.GoBack();
                return;
            }

            if (Session.IsFamilyMode)
            {
                MessageBox.Show("Справочники доступны только в режиме животноводства!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.GoBack();
                return;
            }

            SetTitle();
            LoadData();
        }
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new DictionaryEditDialogPage(currentType));
        }

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;
            int id = (int)button.Tag;
            NavigationService.Navigate(new DictionaryEditDialogPage(currentType, id));
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;
            int id = (int)button.Tag;
            var item = items.FirstOrDefault(i => i.Id == id);
            if (item == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите удалить \"{item.DisplayName}\"?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    switch (currentType)
                    {
                        case DictionaryType.Species:
                            var species = context.Species.Find(id);
                            if (species != null) context.Species.Remove(species);
                            break;
                        case DictionaryType.Breeds:
                            var breed = context.Breeds.Find(id);
                            if (breed != null) context.Breeds.Remove(breed);
                            break;
                        case DictionaryType.Colors:
                            var color = context.Colors.Find(id);
                            if (color != null) context.Colors.Remove(color);
                            break;
                        case DictionaryType.PedigreeClasses:
                            var pc = context.PedigreeClasses.Find(id);
                            if (pc != null) context.PedigreeClasses.Remove(pc);
                            break;
                    }
                    context.SaveChanges();
                    MessageBox.Show("Запись удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Отписываемся от события при уходе
            NavigationService.Navigated -= NavigationService_Navigated;
            NavigationService.GoBack();
        }
    }
}