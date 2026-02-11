import { Component, OnInit, inject } from '@angular/core';
import { AuthService } from '@abp/ng.core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { DestinationService } from '../proxy/destinations';
import { TravelExperienceService } from '../proxy/experiences'; // 👈 Agregado
import { DestinationDto } from '../proxy/application/contracts/destinations';
import { TravelExperienceDto } from '../proxy/experiences'; // 👈 Agregado

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  standalone: true,
  imports: [CommonModule, RouterModule]
})
export class HomeComponent implements OnInit {
  
  private authService = inject(AuthService);
  private destinationService = inject(DestinationService);
  private experienceService = inject(TravelExperienceService); // 👈 Inyectado
  private router = inject(Router);

  // Variables para la vista
  topDestinations: DestinationDto[] = [];
  recentExperiences: TravelExperienceDto[] = []; // 👈 Para la sección Comunidad
  isLoadingTop = true;
  isLoadingCommunity = true;

  get hasLoggedIn(): boolean {
    return this.authService.isAuthenticated;
  }

  ngOnInit() {
    this.loadTopDestinations();
    this.loadCommunityFeed();
  }

  // 1. Cargar Destinos Top
  loadTopDestinations() {
    this.destinationService.getTopDestinations().subscribe({
      next: (list) => {
        this.topDestinations = list;
        this.isLoadingTop = false;
      },
      error: (err) => {
        console.error('Error cargando top destinos', err);
        this.isLoadingTop = false;
      }
    });
  }

  // 2. Cargar Comunidad (Recientes)
  loadCommunityFeed() {
    // Usamos el GetList normal de ABP o el nuevo que creamos
    this.experienceService.getList({ maxResultCount: 3 }).subscribe({
      next: (response) => {
        this.recentExperiences = response.items;
        this.isLoadingCommunity = false;
      },
      error: (err) => {
        console.error('Error cargando comunidad', err);
        this.isLoadingCommunity = false;
      }
    });
  }

  // --- NAVEGACIÓN ---

  login() {
    this.authService.navigateToLogin();
  }

  goToDetail(id: string) {
    this.router.navigate(['/destination-detail', id]);
  }

goToExplore() {
  // Este es el que mandamos a la búsqueda general
  this.router.navigate(['/city-search']);
}

goToTopDestinations() {
    const element = document.getElementById('ranking-section');
    if (element) {
      element.scrollIntoView({ behavior: 'smooth' });
    }
  }

  // 2. Tus Favoritos -> A la ruta que ya existe
  goToMyFavorites() {
    if (this.hasLoggedIn) {
      this.router.navigate(['/favorites']); 
    } else {
      this.login();
    }
  }

  // 3. Comunidad -> Te mando a buscar un destino para que veas sus experiencias
  goToCommunity() {
    this.router.navigate(['/city-search']);
  }
}