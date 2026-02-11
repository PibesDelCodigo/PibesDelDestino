import { RoutesService, eLayoutType } from '@abp/ng.core';
import { APP_INITIALIZER } from '@angular/core';
import { eThemeSharedRouteNames } from '@abp/ng.theme.shared';

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
      // --- Buscador de Ciudades ---
      {
        path: '/city-search',
        name: 'Buscar Ciudades',
        iconClass: 'fas fa-search',
        order: 2, // Segundo lugar
        layout: eLayoutType.application,
      },
      // --- NUEVO: Mis Favoritos ---
      {
        path: '/favorites',
        name: 'Mis Favoritos',
        iconClass: 'fas fa-heart',
        order: 3, // Tercer lugar (Corregido para que no se pise con el anterior)
        layout: eLayoutType.application,
      },
      // --- Dashboard de Métricas ---
      {
        path: '/metrics',
        name: 'Dashboard de Métricas',     // El texto que se verá
        parentName: 'AbpUiNavigation::Menu:Administration', // 👈 ESTO LO METE EN "ADMINISTRACIÓN"
        layout: eLayoutType.application,
        iconClass: 'fa fa-bar-chart',      // Icono de gráfico
        order: 1,                          // Para que salga arriba del todo
        requiredPolicy: 'AbpIdentity.Users', // Solo visible si tienes permisos (Admin)
      },

      {
        path: '/my-profile',       // La ruta a donde querés ir
        name: 'Mi Perfil',         // Lo que va a decir el botón
        iconClass: 'fas fa-user-circle', // Un ícono lindo de usuario
        order: 2,                  // El orden (2 para que salga abajo del Home)
        layout: eLayoutType.application,
      },

      {
    path: '/profile-redirect', // Usamos una ruta "puente"
    name: 'Perfil Público',
    order: 2,
    iconClass: 'fa fa-external-link-alt', // Un ícono que indique "ver"
    layout: eLayoutType.application,
  },

    ]);
  };
}