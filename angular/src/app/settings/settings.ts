import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToasterService } from '@abp/ng.theme.shared';
import { RestService } from '@abp/ng.core';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.html',
})
export class SettingsComponent implements OnInit {

  private rest = inject(RestService);
  private toaster = inject(ToasterService);

  isLoading = true;
  notificationsEnabled = true;
  currentPreference: string = 'Ambas';
  saved = false;

  ngOnInit() {
    this.loadSettings();
  }

  loadSettings() {
    this.isLoading = true;

    this.rest.request<void, any>({
      method: 'GET',
      url: '/api/account/my-profile'
    }).subscribe({
      next: (res) => {
        const extras = res.extraProperties || {};
        this.notificationsEnabled = extras.NotificationEnabled ?? true;
        this.currentPreference = extras.NotificationChannel ?? 'Ambas';
        this.isLoading = false;
      },
      error: () => {
        this.toaster.error('Error al cargar configuración');
        this.isLoading = false;
      }
    });
  }

  saveSettings() {
    this.isLoading = true;
    this.saved = false;

    this.rest.request({
      method: 'PUT',
      url: '/api/account/my-profile',
      body: {
        extraProperties: {
          NotificationEnabled: this.notificationsEnabled,
          NotificationChannel: this.currentPreference
        }
      }
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.saved = true;
        this.toaster.success('Preferencias guardadas ✅');
        setTimeout(() => this.saved = false, 3000);
      },
      error: () => {
        this.isLoading = false;
        this.toaster.error('Error al guardar');
      }
    });
  }
}
