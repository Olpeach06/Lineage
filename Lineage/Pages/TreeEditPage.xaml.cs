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
    public partial class TreeEditPage : Page
    {
        public string TreeName { get; private set; }
        public string TreeDescription { get; private set; }
        public bool IsPublic { get; private set; }

        private bool isEditMode = false;
        private int? editTreeId = null;

        public TreeEditPage()
        {
            InitializeComponent();
            txtTitle.Text = "СОЗДАНИЕ ПРОЕКТА";
            chkIsPublic.IsChecked = false;
        }

        public TreeEditPage(FamilyTrees tree) : this()
        {
            isEditMode = true;
            editTreeId = tree.Id;
            txtTitle.Text = "РЕДАКТИРОВАНИЕ ПРОЕКТА";
            txtName.Text = tree.Name;
            txtDescription.Text = tree.Description;
            chkIsPublic.IsChecked = tree.IsPublic;
            ValidateFields(null, null);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAdmin && !Session.IsEditor && !isEditMode)
            {
                MessageBox.Show("У вас нет прав для создания проекта!", "Доступ запрещён",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.GoBack();
            }
        }

        private void ValidateFields(object sender, TextChangedEventArgs e)
        {
            btnSave.IsEnabled = !string.IsNullOrWhiteSpace(txtName.Text);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите название проекта!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TreeName = txtName.Text.Trim();
            TreeDescription = txtDescription.Text?.Trim();
            IsPublic = chkIsPublic.IsChecked ?? false;

            if (isEditMode && editTreeId.HasValue)
            {
                // Режим редактирования
                try
                {
                    using (var context = new GenealogyUnifiedDBEntities2())
                    {
                        var tree = context.FamilyTrees.Find(editTreeId.Value);
                        if (tree != null)
                        {
                            tree.Name = TreeName;
                            tree.Description = TreeDescription;
                            tree.IsPublic = IsPublic;
                            context.SaveChanges();
                            MessageBox.Show("Проект обновлён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления проекта: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                NavigationService.GoBack();
            }
            else
            {
                // Режим создания - сразу сохраняем в БД
                try
                {
                    using (var context = new GenealogyUnifiedDBEntities2())
                    {
                        int projectTypeId = Session.IsFamilyMode ? 1 : 2;
                        var newTree = new FamilyTrees
                        {
                            Name = TreeName,
                            Description = TreeDescription,
                            ProjectTypeId = projectTypeId,
                            CreatedByUserId = Session.UserId,
                            CreatedAt = DateTime.Now,
                            IsPublic = IsPublic
                        };
                        context.FamilyTrees.Add(newTree);
                        context.SaveChanges();

                        var treeCount = context.FamilyTrees.Count(t => t.CreatedByUserId == Session.UserId);
                        if (treeCount == 1)
                        {
                            Session.CurrentTreeId = newTree.Id;
                        }

                        MessageBox.Show("Новый проект создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка создания проекта: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                NavigationService.GoBack();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}