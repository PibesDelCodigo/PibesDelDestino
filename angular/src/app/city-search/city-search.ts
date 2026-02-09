import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, filter, switchMap, tap } from 'rxjs/operators';
import { CityDto } from '../proxy/cities';
import { DestinationService } from '../proxy/destinations';
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

  // Set para guardar las ciudades favoritas (Clave: "Nombre_Pais")
  likedCities = new Set<string>();
  
  destinationCache = new Map<string, { id: string, rating: number }>(); 

  constructor(
    private destinationService: DestinationService,
    private fb: FormBuilder,
    private modalService: NgbModal,
    private authService: AuthService,
    private favoriteService: FavoriteService,
    private router: Router,
    private stateService: CitySearchStateService
  ) {
    this.searchForm = this.fb.group({
      partialName: [''],
      countryId: [''],
      minPopulation: [null]
    });
  }

  ngOnInit(): void {
    
    // 1. RECUPERAR ESTADO
    if (this.stateService.hasData()) {
        this.cities = this.stateService.lastResults;
        this.destinationCache = this.stateService.lastCache;
        this.searchForm.patchValue(this.stateService.lastFormValues, { emitEvent: false });
    } else {
        this.preloadLocalDestinations();
    }

    // 2. CONFIGURACIÓN DEL BUSCADOR
    this.searchForm.valueChanges.pipe(
      debounceTime(800),
      distinctUntilChanged((prev, curr) => JSON.stringify(prev) === JSON.stringify(curr)),
      filter(val => {
        const text = val.partialName;
        if (!text || text.length < 3) {
          if (!text) this.cities = []; 
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

        this.stateService.lastResults = this.cities;
        this.stateService.lastFormValues = this.searchForm.value;
        this.stateService.lastCache = this.destinationCache;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error:', err);
        this.errorMessage = 'Ocurrió un problema al buscar ciudades.';
        this.isLoading = false;
      }
    });

    // 3. CARGAR LIKES
    if (this.authService.isAuthenticated) {
      this.favoriteService.getMyFavorites().subscribe(favs => {
        favs.forEach(f => {
          const key = this.getUniqueKey(f.name, f.country);
          this.likedCities.add(key); 
        });
      });
    }
  }

  private getUniqueKey(name: string, country: string): string {
    return `${name.toLowerCase().trim()}_${country.toLowerCase().trim()}`;
  }

  isLiked(city: CityWithRating): boolean {
    const key = this.getUniqueKey(city.name, city.country);
    return this.likedCities.has(key);
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
    
    const key = this.getUniqueKey(city.name, city.country);

    this.ensureDestinationExists(city, (id) => {
        this.favoriteService.toggle({ destinationId: id }).subscribe(() => {
            if (this.likedCities.has(key)) {
                this.likedCities.delete(key);
            } else {
                this.likedCities.add(key);
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

  // 👇 ESTA ES LA FUNCIÓN QUE FALTABA 👇
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