using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lineage.AppData;

namespace Lineage.Classes
{
    public static class Session
    {
        // Данные пользователя
        public static int UserId { get; set; }
        public static string Username { get; set; }
        public static string Email { get; set; }
        public static int RoleId { get; set; }
        public static string RoleName { get; set; }
        public static int? PersonId { get; set; }
        public static string FullName { get; set; }
        public static DateTime LoginTime { get; set; }

        // Текущий проект
        public static int CurrentTreeId { get; set; }

        // Текущий режим
        private static int _currentMode = 1;
        public static int CurrentMode
        {
            get => _currentMode;
            set
            {
                _currentMode = value;
                System.Diagnostics.Debug.WriteLine($"Session.CurrentMode установлен в: {(value == 1 ? "Семейный" : "Животноводство")}");
            }
        }

        // Последний использованный режим
        public static int? LastUsedMode { get; set; }

        // Режим гостя
        public static bool IsGuest { get; set; } = false;

        // Проверка прав
        public static bool IsAdmin => RoleId == 1;
        public static bool IsEditor => RoleId == 2 || IsAdmin;
        public static bool IsViewer => RoleId == 3;

        // Проверка текущего режима (удобные свойства)
        public static bool IsFamilyMode => CurrentMode == 1;
        public static bool IsBreedingMode => CurrentMode == 2;

        // Сброс сессии
        public static void Clear()
        {
            UserId = 0;
            Username = null;
            Email = null;
            RoleId = 0;
            RoleName = null;
            PersonId = null;
            FullName = null;
            LoginTime = DateTime.MinValue;
            CurrentTreeId = 0;
            CurrentMode = 1;
            LastUsedMode = null;
            IsGuest = false;
        }

        // Обновить режим по ID проекта
        public static void UpdateModeByTreeId(int treeId)
        {
            if (treeId == 0) return;

            try
            {
                using (var context = new GenealogyUnifiedDBEntities1())
                {
                    var tree = context.FamilyTrees.Find(treeId);
                    if (tree != null)
                    {
                        CurrentMode = tree.ProjectTypeId;
                        System.Diagnostics.Debug.WriteLine($"Режим обновлён по проекту ID {treeId}: {(CurrentMode == 1 ? "Семейный" : "Животноводство")}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления режима: {ex.Message}");
            }
        }
    }
}