using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public static int CurrentMode { get; set; } = 1;

        // Последний использованный режим
        public static int? LastUsedMode { get; set; }

        // Режим гостя
        public static bool IsGuest { get; set; } = false;

        // Проверка прав
        public static bool IsAdmin => RoleId == 1;
        public static bool IsEditor => RoleId == 2 || IsAdmin;
        public static bool IsViewer => RoleId == 3;

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
    }
}
