import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, filter, switchMap, tap } from 'rxjs/operators';
import { DestinationService } from '../proxy/destinations';
import { CityDto } from '../proxy/cities';
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
    private fb: FormBuilder // Inyectamos el constructor de formularios
  ) {
    // Inicializamos el formulario
    this.searchForm = this.fb.group({
      partialName: [''],
      countryId: [''],
      minPopulation: [null]
    });
  }

  ngOnInit(): void {
    this.searchForm.valueChanges.pipe(
      debounceTime(800), // Esperamos un poco más porque hay más campos
      distinctUntilChanged((prev, curr) => JSON.stringify(prev) === JSON.stringify(curr)),
      filter(val => {
        // Solo buscamos si hay nombre escrito (al menos 3 letras)
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
        // Enviamos el objeto completo con los filtros
        return this.destinationService.searchCities({ 
          partialName: val.partialName,
          countryId: val.countryId || null, // Si está vacío mandamos null
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
}