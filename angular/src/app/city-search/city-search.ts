import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, filter, switchMap, tap } from 'rxjs/operators';

// IMPORTANTE: Si estos imports te dan error, borralos y volvé a escribirlos
// para que VS Code encuentre la ruta correcta de tus proxies generados.
import { DestinationService } from '../proxy/destinations';
import { CityDto } from '../proxy/cities';
@Component({
  selector: 'app-city-search',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './city-search.html',     // Sin .component
  styleUrls: ['./city-search.scss']      // Sin .component
})
export class CitySearch implements OnInit { // Nombre de clase corto

  searchControl = new FormControl('');
  cities: CityDto[] = [];
  isLoading = false;
  errorMessage = '';

  constructor(private destinationService: DestinationService) {}

  ngOnInit(): void {
    this.searchControl.valueChanges.pipe(
      // 1. Esperamos 500ms a que termines de escribir
      debounceTime(500),
      
      // 2. Si escribís lo mismo que antes, no buscamos
      distinctUntilChanged(),
      
      // 3. Solo buscamos si hay 3 letras o más
      filter(text => {
        if (!text || text.length < 3) {
          this.cities = [];
          return false;
        }
        return true;
      }),

      // 4. Activamos el "Cargando..."
      tap(() => {
        this.isLoading = true;
        this.errorMessage = '';
      }),

      // 5. Llamamos a la API (si buscas de nuevo rápido, cancela la anterior)
      switchMap(text => {
        return this.destinationService.searchCities({ partialName: text || '' });
      })
    ).subscribe({
      next: (response) => {
        this.cities = response.cities || [];
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error:', err);
        this.errorMessage = 'Error al buscar ciudades.';
        this.isLoading = false;
      }
    });
  }
}