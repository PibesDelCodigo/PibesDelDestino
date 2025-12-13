import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, filter, switchMap, tap } from 'rxjs/operators';
import { CityDto } from '../proxy/cities';
import { DestinationService } from '../proxy/destinations';
import { CreateUpdateDestinationDto } from '../proxy/application/contracts/destinations';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ExperienceModalComponent } from '../experiences/experience-modal/experience-modal';
import { AuthService } from '@abp/ng.core';



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
    private fb: FormBuilder,
    private modalService: NgbModal,
    private authService: AuthService
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

rateCity(city: CityDto) {

  if (!this.authService.isAuthenticated) {
      this.authService.navigateToLogin(); // Lo mandamos a loguearse
      return; // Cortamos la ejecución
    }

    this.isLoading = true;

    // 1. Preparamos el objeto
    const newDestination: CreateUpdateDestinationDto = {
      name: city.name,
      country: city.country,
      city: city.region || city.name,
      population: city.population || 0,
      photo: '',
      updateDate: new Date().toISOString(),
      coordinates: {
        latitude: city.latitude,
        longitude: city.longitude
      }
    };

    // 2. Intentamos CREAR
    this.destinationService.create(newDestination).subscribe({
      next: (savedDestination) => {
        // ESCENARIO A: Era nueva, se creó bien.
        this.isLoading = false;
        this.openRatingModal(savedDestination.id, savedDestination.name);
      },
      error: (err) => {
        // ESCENARIO B: Falló (probablemente ya existe). La buscamos.
        console.warn('No se pudo crear (¿ya existe?). Buscando en BD...', err);
        
        // Usamos getList filtrando por nombre para recuperar el ID
        // NOTA: Asegurate que tu servicio acepte un parametro de filtro, si no, traemos todo
       // ✅ SOLUCIÓN: Traemos hasta 100 destinos y buscamos nosotros el correcto
this.destinationService.getList({ maxResultCount: 100 }).subscribe({
          next: (response) => {
            this.isLoading = false;
            // Buscamos la coincidencia exacta por nombre
            const found = response.items.find(d => d.name === city.name);
            
            if (found) {
              this.openRatingModal(found.id, found.name);
            } else {
              alert('❌ Error: No se pudo guardar ni encontrar el destino para calificar.');
            }
          },
          error: (searchErr) => {
            this.isLoading = false;
            console.error('Error al buscar:', searchErr);
          }
        });
      }
    });
  }

  // Helper para no repetir código
  private openRatingModal(id: string, name: string) {
    const modalRef = this.modalService.open(ExperienceModalComponent, { size: 'lg' });
    modalRef.componentInstance.destinationId = id;
    modalRef.componentInstance.destinationName = name;
  }

}