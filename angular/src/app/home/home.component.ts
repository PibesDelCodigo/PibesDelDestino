import { Component, OnInit, inject } from '@angular/core';
import { AuthService } from '@abp/ng.core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { DestinationService } from '../proxy/destinations';
import { TravelExperienceService } from '../proxy/experiences';
import { DestinationDto } from '../proxy/application/contracts/destinations';
import { TravelExperienceDto } from '../proxy/experiences'; 

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
  private experienceService = inject(TravelExperienceService); 
  private router = inject(Router);

  topDestinations: DestinationDto[] = [];
  recentExperiences: TravelExperienceDto[] = []; 
  isLoadingTop = true;
  isLoadingCommunity = true;

  get hasLoggedIn(): boolean {
    return this.authService.isAuthenticated;
  }

  ngOnInit() {
    this.loadTopDestinations();
    this.loadCommunityFeed();
  }

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

  loadCommunityFeed() {
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

  login() {
    this.authService.navigateToLogin();
  }

  goToDetail(id: string) {
    this.router.navigate(['/destination-detail', id]);
  }

goToExplore() {
  this.router.navigate(['/city-search']);
}

goToTopDestinations() {
    const element = document.getElementById('ranking-section');
    if (element) {
      element.scrollIntoView({ behavior: 'smooth' });
    }
  }

  goToMyFavorites() {
    if (this.hasLoggedIn) {
      this.router.navigate(['/favorites']); 
    } else {
      this.login();
    }
  }

  goToCommunity() {
    this.router.navigate(['/city-search']);
  }
}