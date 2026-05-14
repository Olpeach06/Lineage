using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lineage.Classes
{
    public static class AppSettings
    {
        // Текущий режим (синхронизируется с Session.CurrentMode)
        public static int CurrentMode
        {
            get { return Session.CurrentMode; }
            set { Session.CurrentMode = value; }
        }

        // Проверка режимов
        public static bool IsFamilyMode => CurrentMode == 1;
        public static bool IsBreedingMode => CurrentMode == 2;

        // Суффикс для заголовков
        public static string ModeSuffix => IsFamilyMode ? " (Семейный режим)" : " (Племенная книга)";

        // Обновить режим по проекту
        public static void UpdateModeByTreeId(int treeId)
        {
            Session.UpdateModeByTreeId(treeId);
        }
    }
}