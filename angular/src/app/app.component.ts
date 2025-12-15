import { Component, OnInit } from '@angular/core';
import { NavItemsService, eThemeSharedRouteNames, LoaderBarComponent } from '@abp/ng.theme.shared'; // <--- 1. Importar Servicio
import { NotificationBellComponent } from './notifications/notification-bell/notification-bell';
import { DynamicLayoutComponent } from "@abp/ng.core";

@Component({
  selector: 'app-root',
  template: `
    <abp-loader-bar></abp-loader-bar>
    <abp-dynamic-layout></abp-dynamic-layout>
  `,
  imports: [LoaderBarComponent, DynamicLayoutComponent],
})
export class AppComponent implements OnInit { // <--- 3. Implementar OnInit

  constructor(private navItems: NavItemsService) {} // <--- 4. Inyectar

  ngOnInit(): void {
    // 5. Agregar el ítem al Navbar
    this.navItems.addItems([
      {
        id: 'NotificationBell',
        order: 1, // El orden decide dónde aparece (jugá con este número si queda mal ubicado)
        component: NotificationBellComponent, // <--- Nuestro componente
      },
    ]);
  }
}