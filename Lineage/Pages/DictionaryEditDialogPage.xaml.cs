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

namespace Lineage.Pages
{
    public partial class DictionaryEditDialogPage : Page
    {
        private DictionaryType currentType;
        private int? editId = null;

        public DictionaryEditDialogPage(DictionaryType type, int? id = null)
        {
            InitializeComponent();
            currentType = type;
            editId = id;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigureForm();
            LoadSpeciesCombo();

            if (editId.HasValue)
            {
                txtTitle.Text = "РЕДАКТИРОВАНИЕ";
                LoadDataForEdit();
            }
            else
            {
                txtTitle.Text = "ДОБАВЛЕНИЕ";
            }
        }

        private void ConfigureForm()
        {
            panelSpecies.Visibility = Visibility.Collapsed;
            panelCode.Visibility = Visibility.Collapsed;
            chkAllSpecies.Visibility = Visibility.Collapsed;

            switch (currentType)
            {
                case DictionaryType.Breeds:
                    panelSpecies.Visibility = Visibility.Visible;
                    break;
                case DictionaryType.Colors:
                    panelSpecies.Visibility = Visibility.Visible;
                    chkAllSpecies.Visibility = Visibility.Visible;
                    break;
                case DictionaryType.PedigreeClasses:
                    panelCode.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void LoadSpeciesCombo()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    var species = context.Species.OrderBy(s => s.Name).ToList();
                    cmbSpecies.ItemsSource = species;
                    if (species.Any()) cmbSpecies.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки видов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDataForEdit()
        {
            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    switch (currentType)
                    {
                        case DictionaryType.Species:
                            var species = context.Species.Find(editId.Value);
                            if (species != null) txtName.Text = species.Name;
                            break;

                        case DictionaryType.Breeds:
                            var breed = context.Breeds.Find(editId.Value);
                            if (breed != null)
                            {
                                txtName.Text = breed.Name;
                                txtDescription.Text = breed.Description;
                                if (breed.SpeciesId.HasValue)
                                    cmbSpecies.SelectedValue = breed.SpeciesId.Value;
                            }
                            break;

                        case DictionaryType.Colors:
                            var color = context.Colors.Find(editId.Value);
                            if (color != null)
                            {
                                txtName.Text = color.Name;
                                if (color.SpeciesId.HasValue)
                                {
                                    cmbSpecies.SelectedValue = color.SpeciesId.Value;
                                    chkAllSpecies.IsChecked = false;
                                    cmbSpecies.IsEnabled = true;
                                }
                                else
                                {
                                    chkAllSpecies.IsChecked = true;
                                    cmbSpecies.IsEnabled = false;
                                }
                            }
                            break;

                        case DictionaryType.PedigreeClasses:
                            var pc = context.PedigreeClasses.Find(editId.Value);
                            if (pc != null)
                            {
                                txtName.Text = pc.Name;
                                txtCode.Text = pc.Code.ToString();
                                txtDescription.Text = pc.Description;
                            }
                            break;
                    }
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
            string name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите название!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();

            try
            {
                using (var context = new GenealogyUnifiedDBEntities2())
                {
                    switch (currentType)
                    {
                        case DictionaryType.Species:
                            if (editId.HasValue)
                            {
                                var species = context.Species.Find(editId.Value);
                                if (species != null) species.Name = name;
                            }
                            else
                            {
                                context.Species.Add(new Species { Name = name });
                            }
                            break;

                        case DictionaryType.Breeds:
                            if (cmbSpecies.SelectedValue == null)
                            {
                                MessageBox.Show("Выберите вид!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                            int speciesId = (int)cmbSpecies.SelectedValue;

                            if (editId.HasValue)
                            {
                                var breed = context.Breeds.Find(editId.Value);
                                if (breed != null)
                                {
                                    breed.Name = name;
                                    breed.SpeciesId = speciesId;
                                    breed.Description = description;
                                }
                            }
                            else
                            {
                                context.Breeds.Add(new Breeds
                                {
                                    Name = name,
                                    SpeciesId = speciesId,
                                    Description = description
                                });
                            }
                            break;

                        case DictionaryType.Colors:
                            int? colorSpeciesId = chkAllSpecies.IsChecked == true ? null : (int?)cmbSpecies.SelectedValue;

                            if (editId.HasValue)
                            {
                                var color = context.Colors.Find(editId.Value);
                                if (color != null)
                                {
                                    color.Name = name;
                                    color.SpeciesId = colorSpeciesId;
                                }
                            }
                            else
                            {
                                context.Colors.Add(new AppData.Colors
                                {
                                    Name = name,
                                    SpeciesId = colorSpeciesId
                                });
                            }
                            break;

                        case DictionaryType.PedigreeClasses:
                            if (!int.TryParse(txtCode.Text, out int code))
                            {
                                MessageBox.Show("Введите корректный числовой код!", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            if (editId.HasValue)
                            {
                                var pc = context.PedigreeClasses.Find(editId.Value);
                                if (pc != null)
                                {
                                    pc.Name = name;
                                    pc.Code = code;
                                    pc.Description = description;
                                }
                            }
                            else
                            {
                                context.PedigreeClasses.Add(new PedigreeClasses
                                {
                                    Name = name,
                                    Code = code,
                                    Description = description
                                });
                            }
                            break;
                    }

                    context.SaveChanges();
                    MessageBox.Show(editId.HasValue ? "Запись обновлена!" : "Запись добавлена!",
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