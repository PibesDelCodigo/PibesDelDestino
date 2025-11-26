import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, filter, switchMap, tap } from 'rxjs/operators';
import { CityDto } from '../proxy/cities';
import { DestinationService } from '../proxy/destinations';
import { CreateUpdateDestinationDto } from '../proxy/application/contracts/destinations';

@Component({
  selector: 'app-city-search',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './city-search.html',
  styleUrls: ['./city-search.scss']
})
export class CitySearch implements OnInit {

  searchForm: FormGroup;
  cities: CityDto[] = [];
  isLoading = false;
  errorMessage = '';

  constructor(
    private destinationService: DestinationService,
    private fb: FormBuilder
  ) {
    this.searchForm = this.fb.group({
      partialName: [''],
      countryId: [''],
      minPopulation: [null]
    });
  }

  ngOnInit(): void {
    // ... (Tu código del ngOnInit queda igual que antes) ...
    this.searchForm.valueChanges.pipe(
      debounceTime(800),
      distinctUntilChanged((prev, curr) => JSON.stringify(prev) === JSON.stringify(curr)),
      filter(val => {
        const text = val.partialName;
        if (!text || text.length < 3) {
          this.cities = [];
          return false;
        }
        return true;
      }),
      tap(() => {
        this.isLoading = true;
        this.errorMessage = '';
      }),
      switchMap(val => {
        return this.destinationService.searchCities({ 
          partialName: val.partialName,
          countryId: val.countryId || null,
          minPopulation: val.minPopulation || null
        });
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

  // --- NUEVA FUNCIÓN: GUARDAR CIUDAD ---
  saveCity(city: CityDto) {
    if(!confirm(`¿Querés guardar a ${city.name} en tu lista de destinos?`)) return;

    // Mapeamos la ciudad de la API (CityDto) al DTO de nuestra Base de Datos
    const newDestination: CreateUpdateDestinationDto = {
      name: city.name,
      country: city.country,
      city: city.region || city.name, // Si no hay región, repetimos el nombre
      population: city.population || 0,
      photo: '', // La API de búsqueda simple no trae foto
      updateDate: new Date().toISOString(),
      coordinates: {
        latitude: city.latitude,
        longitude: city.longitude
      }
    };

    this.destinationService.create(newDestination).subscribe({
      next: () => {
        alert('✅ ¡Destino guardado con éxito!');
      },
      error: (err) => {
        console.error(err);
        alert('❌ Error al guardar. ¿Quizás ya existe?');
      }
    });
  }
}