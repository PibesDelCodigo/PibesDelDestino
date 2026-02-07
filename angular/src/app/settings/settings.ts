import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToasterService } from '@abp/ng.theme.shared';
import { SettingsService } from '../proxy/settings';
import { NotificationService } from 'src/app/proxy/notifications'; // 👈 Asegúrate que este path sea correcto

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.html' // 👈 Verifica que se llame así tu archivo HTML
})
export class SettingsComponent implements OnInit {

  // Inyecciones
  private settingsService = inject(SettingsService);
  private notificationService = inject(NotificationService);
  private toaster = inject(ToasterService);

  // Variables de Estado
  isLoading = true;
  notificationsEnabled = true; // Interruptor general

  // 👇 ESTA ES LA VARIABLE QUE TE FALTABA (La que pide el error 1)
  currentPreference: string = 'Ambas';

  // 👇 ESTA ES LA VARIABLE PARA EL MENSAJE (La que pide el error 3)
  saved: boolean = false;

  ngOnInit() {
    this.loadSettings();
  }

  loadSettings() {
    this.isLoading = true;

    // 1. Cargar preferencia General
    this.settingsService.getNotificationPreference().subscribe({
      next: (val) => {
        this.notificationsEnabled = val;
        this.checkLoadingComplete();
      },
      error: () => this.checkLoadingComplete()
    });

    // 2. Cargar preferencia de Canal
    this.notificationService.getNotificationPreference().subscribe({
      next: (pref) => {
        this.currentPreference = pref || 'Ambas';
        this.checkLoadingComplete();
      },
      error: () => this.checkLoadingComplete()
    });
  }

  private loadingCounter = 0;
  private checkLoadingComplete() {
    this.loadingCounter++;
    if (this.loadingCounter >= 2) this.isLoading = false;
  }

  toggleNotifications() {
    this.isLoading = true;
    const newValue = !this.notificationsEnabled;

    this.settingsService.updateNotificationPreference(newValue).subscribe({
      next: () => {
        this.notificationsEnabled = newValue;
        this.isLoading = false;
        const estado = newValue ? 'ACTIVADAS 🔔' : 'DESACTIVADAS 🔕';
        this.toaster.success(`Notificaciones generales ${estado}`);
      },
      error: () => {
        this.toaster.error('Error al guardar configuración');
        this.isLoading = false;
      }
    });
  }

  // 👇 ESTE ES EL MÉTODO QUE TE FALTABA (El que pide el error 2)
  savePreference() {
    this.isLoading = true;
    this.saved = false; // Resetear mensaje

    this.notificationService.setNotificationPreference(this.currentPreference).subscribe({
      next: () => {
        this.isLoading = false;
        this.saved = true; // Mostrar mensaje "Guardado"
        this.toaster.success('Preferencia de canal actualizada ✅');

        // Ocultar mensaje a los 3 seg
        setTimeout(() => this.saved = false, 3000);
      },
      error: (err) => {
        console.error(err);
        this.toaster.error('Error al guardar el canal');
        this.isLoading = false;
      }
    });
  }
}