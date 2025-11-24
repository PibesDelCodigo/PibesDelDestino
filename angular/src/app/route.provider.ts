import { RoutesService, eLayoutType } from '@abp/ng.core';
import { APP_INITIALIZER } from '@angular/core';

export const APP_ROUTE_PROVIDER = [
  { provide: APP_INITIALIZER, useFactory: configureRoutes, deps: [RoutesService], multi: true },
];

function configureRoutes(routesService: RoutesService) {
  return () => {
    routesService.add([
      {
        path: '/',
        name: '::Menu:Home',
        iconClass: 'fas fa-home',
        order: 1,
        layout: eLayoutType.application,
      },
      // --- NUEVO: Buscador de Ciudades ---
      {
        path: '/city-search',
        name: 'Buscar Ciudades', // Lo que se lee en el menú
        iconClass: 'fas fa-search', // Icono de lupa
        order: 2,
        layout: eLayoutType.application,
      },
      // --- NUEVO: Mi Cuenta ---
      {
        path: '/my-account',
        name: 'Eliminar Cuenta',
        iconClass: 'fas fa-user-cog', // Icono de usuario/config
        order: 3,
        layout: eLayoutType.application,
        requiredPolicy: '', // Opcional: Podés poner permisos aquí si quisieras
      },
    ]);
  };
}