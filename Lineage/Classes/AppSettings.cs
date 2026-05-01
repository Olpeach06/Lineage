using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lineage.Classes
{
    public static class AppSettings
    {
        private static int _currentMode = 1; // 1 - семейный, 2 - животноводство

        public static int CurrentMode
        {
            get => _currentMode;
            set
            {
                _currentMode = value;
                System.Diagnostics.Debug.WriteLine($"Режим изменён на: {(value == 1 ? "Семейный" : "Животноводство")}");
            }
        }

        public static bool IsFamilyMode => CurrentMode == 1;
        public static bool IsBreedingMode => CurrentMode == 2;
        public static string ModeSuffix => IsFamilyMode ? " (Семейный режим)" : " (Племенная книга)";
    }
}
