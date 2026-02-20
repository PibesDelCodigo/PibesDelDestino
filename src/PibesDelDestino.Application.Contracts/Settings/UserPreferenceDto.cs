using System;
using PibesDelDestino.Settings;

namespace PibesDelDestino.Settings
{
    public class UserPreferencesDto
    {
        public bool ReceiveNotifications { get; set; }

        public NotificationChannel PreferredChannel { get; set; }
    }
}