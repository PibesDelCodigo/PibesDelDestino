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

  // 1. VISUAL: Set para pintar los corazones (Solo nombres)
  likedCities = new Set<string>();
  
  // 2. LÓGICA: Mapa para recordar los IDs reales de la BD y NO duplicar
  // Clave: Nombre Ciudad -> Valor: Guid (ID del Destino)
  destinationCache = new Map<string, string>(); 

  constructor(
    private destinationService: DestinationService,
    private fb: FormBuilder,
    private modalService: NgbModal,
    private authService: AuthService,
    private favoriteService: FavoriteService
  ) {
    this.searchForm = this.fb.group({
      partialName: [''],
      countryId: [''],
      minPopulation: [null]
    });
  }

  ngOnInit(): void {
    // A. CONFIGURACIÓN DEL BUSCADOR
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

    // B. CARGA DE FAVORITOS (SOLUCIÓN PERSISTENCIA)
    if (this.authService.isAuthenticated) {
      this.favoriteService.getMyFavorites().subscribe(favs => {
        favs.forEach(f => {
          this.likedCities.add(f.name);         // Pintar corazón
          this.destinationCache.set(f.name, f.id); // Guardar ID para no duplicar
        });
      });
    }
  }

  // --- HELPER: BUSCAR O CREAR (SOLUCIÓN DUPLICADOS) ---
  // Este método se encarga de conseguir un ID válido sin crear basura
  private ensureDestinationExists(city: CityDto, callback: (id: string) => void) {
    
    // CASO 1: Ya lo tenemos en memoria (Caché)
    if (this.destinationCache.has(city.name)) {
      callback(this.destinationCache.get(city.name)!);
      return;
    }

    // CASO 2: No está en memoria, buscamos en la BD por si existe
    this.destinationService.getList({ maxResultCount: 100 }).subscribe(response => {
      const found = response.items.find(d => d.name === city.name);
      
      if (found) {
        // EXISTÍA EN BD: Lo guardamos en caché y lo usamos
        this.destinationCache.set(city.name, found.id);
        callback(found.id);
      } else {
        // CASO 3: NO EXISTE EN NINGÚN LADO: Recién ahí creamos
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
                this.destinationCache.set(city.name, created.id);
                callback(created.id);
            },
            error: (err) => {
                console.error('Error creando destino', err);
                this.isLoading = false;
                alert('Ocurrió un error al procesar el destino.');
            }
        });
      }
    });
  }

  // --- BOTÓN LIKE ---
  likeCity(city: CityDto) {
    if (!this.authService.isAuthenticated) {
      this.authService.navigateToLogin();
      return;
    }
    this.isLoading = true;

    // Usamos el helper inteligente
    this.ensureDestinationExists(city, (id) => {
        this.favoriteService.toggle({ destinationId: id }).subscribe(() => {
            this.isLoading = false;
            
            // Actualizamos visualmente
            if (this.likedCities.has(city.name)) {
                this.likedCities.delete(city.name);
            } else {
                this.likedCities.add(city.name);
            }
        });
    });
  }

  // --- BOTÓN CALIFICAR (También arreglado para usar el helper) ---
  rateCity(city: CityDto) {
    if (!this.authService.isAuthenticated) {
      this.authService.navigateToLogin();
      return;
    }
    this.isLoading = true;

    this.ensureDestinationExists(city, (id) => {
        this.isLoading = false;
        this.openRatingModal(id, city.name);
    });
  }

  // --- BOTÓN GUARDAR (Manual) ---
  saveCity(city: CityDto) {
    if (!confirm(`¿Guardar ${city.name}?`)) return;
    
    // Reutilizamos lógica para no duplicar si ya existe
    this.ensureDestinationExists(city, (id) => {
        alert('✅ Destino asegurado en la base de datos.');
    });
  }

  private openRatingModal(id: string, name: string) {
    const modalRef = this.modalService.open(ExperienceModalComponent, { size: 'lg' });
    modalRef.componentInstance.destinationId = id;
    modalRef.componentInstance.destinationName = name;
  }
}