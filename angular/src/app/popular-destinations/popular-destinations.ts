import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
// Servicios y DTOs existentes
import { DestinationService } from '../proxy/destinations';
import { DestinationDto } from '../proxy/application/contracts/destinations';
import { ExperienceListComponent } from '../experiences/experience-list/experience-list';
// --- NUEVOS IMPORTS PARA FAVORITOS Y CALIFICAR ---
import { FavoriteService } from '../proxy/favorites'; // El servicio nuevo
import { AuthService } from '@abp/ng.core';         // Para chequear login
import { NgbModal } from '@ng-bootstrap/ng-bootstrap'; // Para calificar
import { ExperienceModalComponent } from '../experiences/experience-modal/experience-modal';
@Component({
  selector: 'app-popular-destinations',
  standalone: true,
  imports: [CommonModule, ExperienceListComponent],
  templateUrl: './popular-destinations.html',
  styleUrls: ['./popular-destinations.scss']
})
export class PopularDestinationsComponent implements OnInit {

  destinations: DestinationDto[] = [];

  // Usamos un Set para guardar los IDs de los que tienen Like (es eficiente)
  favoriteIds = new Set<string>();

  constructor(
    private destinationService: DestinationService,
    private favoriteService: FavoriteService, // <--- INYECTAR
    private authService: AuthService,         // <--- INYECTAR
    private modalService: NgbModal            // <--- INYECTAR (Para calificar)
  ) {}

  ngOnInit(): void {
    this.loadDestinations();
  }

loadDestinations() {
    // 1. Pedimos MÁS items (ej: 50) para tener margen de filtrado
    this.destinationService.getList({ maxResultCount: 50 }).subscribe(response => {
      const rawList = response.items || [];

      // 2. FILTRO DE UNICIDAD 
      // Solo dejamos pasar el item si es la PRIMERA vez que vemos ese nombre
      const uniqueList = rawList.filter((item, index, self) =>
        index === self.findIndex((t) => (
          t.name === item.name // Criterio: Mismo Nombre
        ))
      );

      // 3. Recortamos a los top 10 (o los que quieras mostrar)
      this.destinations = uniqueList.slice(0, 10);
      
      this.checkFavoritesStatus();
    });
  }

  // --- LÓGICA DE FAVORITOS ---

  checkFavoritesStatus() {
    // Si no está logueado, no puede tener favoritos, así que no consultamos nada
    if (!this.authService.isAuthenticated) return;

    this.destinations.forEach(dest => {
      // Preguntamos al backend: "¿Este destino es favorito de este usuario?"
      this.favoriteService.isFavorite({ destinationId: dest.id }).subscribe(isFav => {
        if (isFav) {
          this.favoriteIds.add(dest.id);
        }
      });
    });
  }

  toggleFavorite(dest: DestinationDto) {
    if (!this.authService.isAuthenticated) {
      this.authService.navigateToLogin();
      return;
    }

    // Llamamos al Toggle (Interruptor)
    this.favoriteService.toggle({ destinationId: dest.id }).subscribe(isNowFavorite => {
      if (isNowFavorite) {
        this.favoriteIds.add(dest.id); // Pintar Rojo
      } else {
        this.favoriteIds.delete(dest.id); // Quitar Rojo (Volver gris)
      }
    });
  }

  // --- LÓGICA DE CALIFICAR (Recuperada) ---
  
  rateDestination(destination: DestinationDto) {
    if (!this.authService.isAuthenticated) {
        this.authService.navigateToLogin();
        return;
    }

    const modalRef = this.modalService.open(ExperienceModalComponent, { size: 'lg' });
    modalRef.componentInstance.destinationId = destination.id;
    modalRef.componentInstance.destinationName = destination.name;
  }
}