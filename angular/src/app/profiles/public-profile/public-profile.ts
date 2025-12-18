import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TravelExperienceService } from 'src/app/proxy/experiences';
import { UserData } from '@abp/ng.identity/proxy';
import { TravelExperienceDto } from 'src/app/proxy/experiences';
@Component({
  selector: 'app-public-profile',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './public-profile.html',
  styleUrls: ['./public-profile.scss']
})
export class PublicProfileComponent implements OnInit {
  
  // Inyecciones
  private route = inject(ActivatedRoute);
  private experienceService = inject(TravelExperienceService);

  // Variables de Estado
  userId: string = '';
  userName: string = 'Usuario'; // Valor por defecto hasta que cargue
  experiences: TravelExperienceDto[] = [];
  isLoading = true;

  // Estadísticas
  totalReviews = 0;
  averageRating = 0;

  ngOnInit(): void {
    // 1. Capturamos el ID del usuario de la URL
    this.userId = this.route.snapshot.params['id'];

    if (this.userId) {
      this.loadUserProfile();
    }
  }

  loadUserProfile() {
    this.isLoading = true;

    // 2. Pedimos las experiencias filtradas por este UserId
    // Gracias al cambio en el Backend, ahora podemos mandar { userId: ... }
    this.experienceService.getList({ userId: this.userId } as any).subscribe({
      next: (response) => {
        this.experiences = response.items;
        this.totalReviews = response.totalCount;
        
        // Calculamos datos extra
        if (this.experiences.length > 0) {
          // Sacamos el nombre del primer resultado (ya que el backend lo manda)
          this.userName = this.experiences[0].userName || 'Usuario';
          
          // Calculamos su promedio personal
          const sum = this.experiences.reduce((a, b) => a + b.rating, 0);
          this.averageRating = sum / this.experiences.length;
        }

        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error cargando perfil', err);
        this.isLoading = false;
      }
    });
  }
}