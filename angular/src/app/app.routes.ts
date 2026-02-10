import { authGuard, permissionGuard } from '@abp/ng.core'; // Usamos este que ya trajiste
import { Routes } from '@angular/router';
import { DestinationDetailComponent } from './destinations/destination-detail/destination-detail';
import { PublicProfileComponent } from './users/public-profile/public-profile';
import { MyProfileComponent } from './my-profile/my-profile.component';


export const APP_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./home/home.component').then(c => c.HomeComponent),
  },
  {
    path: 'city-search',
    loadComponent: () => import('./city-search/city-search').then(c => c.CitySearch),
  },

{
    path: 'metrics',
    loadComponent: () => import('./metrics/api-metrics/api-metrics').then(m => m.ApiMetricsComponent),
    canActivate: [authGuard]
  },

  {
    path: 'account',
    loadChildren: () => import('@abp/ng.account').then(c => c.createRoutes()),
  },
  {
    path: 'identity',
    loadChildren: () => import('@abp/ng.identity').then(c => c.createRoutes()),
  },

  {
  path: 'settings',
  loadComponent: () => import('./settings/settings').then(m => m.SettingsComponent),
  canActivate: [authGuard] // 👈 Importante: Solo si está logueado
},

  {
    path: 'setting-management',
    loadChildren: () => import('@abp/ng.setting-management').then(c => c.createRoutes()),
  },

{
    path: 'destination-detail/:id', // 👈 El ":id" es la clave mágica
    component: DestinationDetailComponent,
  },

{
    path: 'profile/:id', // 👈 Recibimos el ID del usuario
    component: PublicProfileComponent, // 👈 Mostramos el componente de perfil público
  },

  {
    path: 'favorites',
    loadComponent: () => 
      // CORRECCIÓN: Agregué .component al final del nombre del archivo
      import('./favorites/my-favorites/my-favorites') 
      .then(m => m.MyFavoritesComponent),
    canActivate: [authGuard] // CORRECCIÓN: Usamos authGuard (minúscula) que importaste en la línea 1
  },

{
    path: 'notifications',
    loadComponent: () => import('./notifications/notification-list/notification-list').then(m => m.NotificationListComponent),
    canActivate: [authGuard]
  },

  {
    path: 'users/:id', 
    loadComponent: () => import('./users/public-profile/public-profile').then(c => c.PublicProfileComponent),
  },

  {
    path: 'my-account',
    loadComponent: () => 
      import('./users/my-account/my-account') 
      .then(c => c.MyAccount),  
  },

  { path: 'my-profile', component: MyProfileComponent },

];