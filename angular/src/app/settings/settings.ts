import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToasterService } from '@abp/ng.theme.shared';
import { RestService } from '@abp/ng.core';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings.html', // Asegúrate de que este archivo exista
})
export class SettingsComponent implements OnInit {

  private rest = inject(RestService);
  private toaster = inject(ToasterService);

  isLoading = true;
  notificationsEnabled = true;
  currentPreference: string = 'Ambas'; // Valores posibles: 'Pantalla', 'Email', 'Ambas'
  saved = false;

  ngOnInit() {
    this.loadSettings();
  }

  loadSettings() {
    this.isLoading = true;

    // 1. LLAMADA AL NUEVO SERVICIO (GET)
    // ABP genera la ruta basada en el nombre del AppService
    this.rest.request<void, any>({
      method: 'GET',
      url: '/api/app/settings/preferences' 
    }).subscribe({
      next: (res) => {
        // 2. RECIBIMOS EL DTO LIMPIO
        this.notificationsEnabled = res.receiveNotifications;
        
        // Convertimos el número del backend (0,1,2) a tu string del frontend
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

    // 3. PREPARAMOS EL DTO PARA ENVIAR
    const payload = {
      receiveNotifications: this.notificationsEnabled,
      notificationType: this.mapStringToInt(this.currentPreference)
    };

    // 4. LLAMADA AL NUEVO SERVICIO (PUT)
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

  // --- HELPERS PARA CONVERTIR DATOS ---

  // Convierte Frontend (String) -> Backend (Int)
  private mapStringToInt(pref: string): number {
    const valor = (pref || '').toLowerCase().trim();
    if (valor === 'pantalla') return 0;
    if (valor === 'email') return 1;
    return 2;
  }

  // Convierte Backend (Int) -> Frontend (String)
  private mapIntToString(type: number): string {
    switch (type) {
      case 0: return 'Pantalla';
      case 1: return 'Email';
      case 2: return 'Ambas';
      default: return 'Ambas';
    }
  }
}