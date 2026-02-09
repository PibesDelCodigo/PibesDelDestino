// REQUERIMIENTO 2.1 y 2.2 (UI): Interfaz de Búsqueda.
// Captura el input del usuario y los filtros seleccionados para invocar
// al servicio de GeoDB

// REQUERIMIENTOS 5.1 y 5.2: Acción de Favoritos (Toggle)
// Verifica autenticación y asegura que el destino exista localmente.
// Luego invoca al servicio para agregar o quitar de la lista personal. 
// Metodo likeCity(city: CityWithRating) {
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, filter, switchMap, tap } from 'rxjs/operators';
import { CityDto } from '../proxy/cities';
import { DestinationService } from '../proxy/destinations';
import { DestinationDto } from '../proxy/application/contracts/destinations';
import { CreateUpdateDestinationDto } from '../proxy/application/contracts/destinations';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ExperienceModalComponent } from '../experiences/experience-modal/experience-modal';
import { FavoriteService } from '../proxy/favorites';
import { AuthService } from '@abp/ng.core';
import { Router } from '@angular/router';
import { CitySearchStateService } from './city-search-state-service';

interface CityWithRating extends CityDto {
  averageRating?: number;
}

@Component({
  selector: 'app-city-search',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './city-search.html',
  styleUrls: ['./city-search.scss']
})
export class CitySearch implements OnInit {

  searchForm: FormGroup;
  cities: CityWithRating[] = [];
  isLoading = false;
  errorMessage = '';

  likedCities = new Set<string>();
  
  // Clave: Nombre + País -> Valor: ID y Rating
  destinationCache = new Map<string, { id: string, rating: number }>(); 

  constructor(
    private destinationService: DestinationService,
    private fb: FormBuilder,
    private modalService: NgbModal,
    private authService: AuthService,
    private favoriteService: FavoriteService,
    private router: Router,
    // 👇 2. INYECTAMOS EL SERVICIO DE ESTADO
    private stateService: CitySearchStateService
  ) {
    this.searchForm = this.fb.group({
      partialName: [''],
      countryId: [''],
      minPopulation: [null]
    });
  }

  ngOnInit(): void {
    
    // 👇 3. LÓGICA DE RECUPERACIÓN (SI VOLVEMOS DE DETALLES)
    if (this.stateService.hasData()) {
        console.log('♻️ Recuperando estado anterior...');
        
        // A. Recuperamos resultados y caché
        this.cities = this.stateService.lastResults;
        this.destinationCache = this.stateService.lastCache;

        // B. Recuperamos el formulario (sin disparar evento para no buscar de nuevo)
        this.searchForm.patchValue(this.stateService.lastFormValues, { emitEvent: false });
    } else {
        // Si no hay datos guardados, cargamos lo local normalmente
        this.preloadLocalDestinations();
    }

    // CONFIGURACIÓN DEL BUSCADOR
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
        this.cities = (response.cities || []).map(city => {
          const uniqueKey = this.getUniqueKey(city.name, city.country);
          const cached = this.destinationCache.get(uniqueKey);
          
          return {
            ...city,
            averageRating: cached ? cached.rating : 0 
          };
        });

        // 👇 4. GUARDAR ESTADO CADA VEZ QUE BUSCAMOS
        this.stateService.lastResults = this.cities;
        this.stateService.lastFormValues = this.searchForm.value;
        this.stateService.lastCache = this.destinationCache;

        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error:', err);
        this.errorMessage = 'Error al buscar ciudades.';
        this.isLoading = false;
      }
    });

    if (this.authService.isAuthenticated) {
      this.favoriteService.getMyFavorites().subscribe(favs => {
        favs.forEach(f => {
          this.likedCities.add(f.name); 
        });
      });
    }
  }

  private getUniqueKey(name: string, country: string): string {
    return `${name.toLowerCase().trim()}_${country.toLowerCase().trim()}`;
  }

  private preloadLocalDestinations() {
    this.destinationService.getList({ maxResultCount: 100 }).subscribe(response => {
      response.items.forEach(dest => {
        const key = this.getUniqueKey(dest.name, dest.country);
        this.destinationCache.set(key, { 
          id: dest.id, 
          rating: dest.averageRating || 0 
        });
      });
      // Actualizamos el estado también aquí por si acaso
      this.stateService.lastCache = this.destinationCache;
    });
  }

  private ensureDestinationExists(city: CityWithRating, callback: (id: string) => void) {
    const key = this.getUniqueKey(city.name, city.country);

    if (this.destinationCache.has(key)) {
      callback(this.destinationCache.get(key)!.id);
      return;
    }

    const newDestination: CreateUpdateDestinationDto = {
      name: city.name,
      country: city.country,
      city: city.region || city.name,
      population: city.population || 0,
      photo: '',
      updateDate: new Date().toISOString(),
      coordinates: { latitude: city.latitude, longitude: city.longitude }
    };

    this.destinationService.create(newDestination).subscribe({
        next: (created) => {
            this.destinationCache.set(key, { id: created.id, rating: 0 });
            // Guardamos el caché actualizado en el servicio
            this.stateService.lastCache = this.destinationCache;
            
            callback(created.id);
        },
        error: (err) => {
            console.error('Error creando destino', err);
            this.isLoading = false;
            alert('Ocurrió un error al procesar el destino.');
        }
    });
  }

  likeCity(city: CityWithRating) {
    if (!this.authService.isAuthenticated) {
      this.authService.navigateToLogin();
      return;
    }
    this.isLoading = true;

    this.ensureDestinationExists(city, (id) => {
        this.favoriteService.toggle({ destinationId: id }).subscribe(() => {
            this.isLoading = false;
            if (this.likedCities.has(city.name)) {
                this.likedCities.delete(city.name);
            } else {
                this.likedCities.add(city.name);
            }
        });
    });
  }

  rateCity(city: CityWithRating) {
    if (!this.authService.isAuthenticated) {
      this.authService.navigateToLogin();
      return;
    }
    
    this.ensureDestinationExists(city, (id) => {
        this.openRatingModal(id, city.name);
    });
  }

// REQUERIMIENTO 2.5 (UI): Acción de Guardar.
// Al seleccionar un resultado de la API externa, se envía la información
// completa al Backend para registrarla permanentemente.

  saveCity(city: CityWithRating) {
    if (!confirm(`¿Guardar ${city.name}?`)) return;
    this.ensureDestinationExists(city, (id) => {
        alert('✅ Destino asegurado en la base de datos.');
    });
  }

  private openRatingModal(id: string, name: string) {
    const modalRef = this.modalService.open(ExperienceModalComponent, { size: 'lg' });
    modalRef.componentInstance.destinationId = id;
    modalRef.componentInstance.destinationName = name;
    
    modalRef.result.then(() => {
        this.preloadLocalDestinations(); 
    }, () => {});
  }

  goToDetails(city: CityWithRating) {
      this.isLoading = true;
      this.ensureDestinationExists(city, (id) => {
          this.isLoading = false;
          this.router.navigate(['/destination-detail', id]);
      });
  }
}