import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'; // 👈 Necesario para el switch
import { SettingsService } from '../proxy/settings';
import { ToasterService } from '@abp/ng.theme.shared';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.html'
})
export class SettingsComponent implements OnInit {
  
  private settingsService = inject(SettingsService);
  private toaster = inject(ToasterService);

  notificationsEnabled = true;
  isLoading = true;

  ngOnInit() {
    // Cargar preferencia actual al entrar
    this.settingsService.getNotificationPreference().subscribe(val => {
      this.notificationsEnabled = val;
      this.isLoading = false;
    });
  }

  toggleNotifications() {
    this.isLoading = true;
    // Invertir valor y guardar
    const newValue = !this.notificationsEnabled;
    
    this.settingsService.updateNotificationPreference(newValue).subscribe({
      next: () => {
        this.notificationsEnabled = newValue;
        this.isLoading = false;
        
        const estado = newValue ? 'ACTIVADAS 🔔' : 'DESACTIVADAS 🔕';
        this.toaster.success(`Notificaciones ${estado}`);
      },
      error: (err) => {
        this.toaster.error('Error al guardar la configuración');
        this.isLoading = false;
      }
    });
  }
}