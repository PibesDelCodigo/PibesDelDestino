import { Component, OnInit, inject } from '@angular/core';
import { AuthService } from '@abp/ng.core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { DestinationService } from '../proxy/destinations';
import { DestinationDto } from '../proxy/application/contracts/destinations';
@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  standalone: true,
  imports: [CommonModule, RouterModule] // Quitamos PopularDestinations por ahora
})
export class HomeComponent implements OnInit {
  
  // Inyecciones con estilo moderno
  private authService = inject(AuthService);
  private destinationService = inject(DestinationService);
  private router = inject(Router);

  // Variables para la vista
  topDestinations: DestinationDto[] = [];
  isLoading = true;

  get hasLoggedIn(): boolean {
    return this.authService.isAuthenticated;
  }

  ngOnInit() {
    // 👇 Llamamos al Backend para pedir el Top 10
    this.destinationService.getTopDestinations().subscribe({
      next: (list) => {
        this.topDestinations = list;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error cargando top destinos', err);
        this.isLoading = false;
      }
    });
  }

  login() {
    this.authService.navigateToLogin();
  }

  // 👇 Función para ir al detalle
  goToDetail(id: string) {
    this.router.navigate(['/destination-detail', id]);
  }
}