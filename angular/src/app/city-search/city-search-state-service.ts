import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root' // Esto hace que el servicio viva para siempre en la app
})
export class CitySearchStateService {
  // Acá guardamos lo que escribiste en el formulario
  lastFormValues: any = {}; 
  
  // Acá guardamos la lista de ciudades que encontraste
  lastResults: any[] = [];
  
  // Acá guardamos el caché de estrellas para no perderlo
  lastCache = new Map<string, { id: string, rating: number }>();

  hasData(): boolean {
    return this.lastResults.length > 0;
  }
}