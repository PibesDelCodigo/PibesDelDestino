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
      url: '/api/app/settings/preferences' 
    }).subscribe({
      next: (res) => {
        this.notificationsEnabled = res.receiveNotifications;
        this.currentPreference = this.mapIntToString(res.notificationType);
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
    const payload = {
      receiveNotifications: this.notificationsEnabled,
      notificationType: this.mapStringToInt(this.currentPreference)
    };

    this.rest.request({
      method: 'PUT',
      url: '/api/app/settings/preferences',
      body: payload
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.saved = true;
        this.toaster.success('Preferencias guardadas ✅');
        setTimeout(() => this.saved = false, 3000);
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
        this.toaster.error('Error al guardar. Intenta de nuevo.');
      }
    });
  }

  private mapStringToInt(pref: string): number {
    const valor = (pref || '').toLowerCase().trim();
    if (valor === 'pantalla') return 0;
    if (valor === 'email') return 1;
    return 2;
  }

  private mapIntToString(type: number): string {
    switch (type) {
      case 0: return 'Pantalla';
      case 1: return 'Email';
      case 2: return 'Ambas';
      default: return 'Ambas';
    }
  }
}