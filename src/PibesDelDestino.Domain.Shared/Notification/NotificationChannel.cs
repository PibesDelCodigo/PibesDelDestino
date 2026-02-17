using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PibesDelDestino.Settings
{
    public enum NotificationChannel
    {
        None = 0,       // No molestar
        Email = 1,      // Solo correo
        Push = 2,       // Solo notificación en el celu/web
        All = 3         // Ambas (Email + Push)
    }
}