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

      {
        path: '/city-search',
        name: 'Buscar Ciudades',
        iconClass: 'fas fa-search',
        order: 2,
        layout: eLayoutType.application,
      },
      
      {
        path: '/favorites',
        name: 'Mis Favoritos',
        iconClass: 'fas fa-heart',
        order: 3, 
        layout: eLayoutType.application,
      },
      
      {
        path: '/metrics',
        name: 'Dashboard de Métricas',     
        parentName: 'AbpUiNavigation::Menu:Administration', 
        layout: eLayoutType.application,
        iconClass: 'fa fa-bar-chart',      
        order: 1,                          
        requiredPolicy: 'AbpIdentity.Users', 
      },

      {
        path: '/my-profile',       
        name: 'Mi Perfil',         
        iconClass: 'fas fa-user-circle', 
        order: 2,                  
        layout: eLayoutType.application,
      },

      {
    path: '/profile-redirect', 
    name: 'Perfil Público',
    order: 2,
    iconClass: 'fa fa-external-link-alt', 
    layout: eLayoutType.application,
  },

    ]);
  };
}