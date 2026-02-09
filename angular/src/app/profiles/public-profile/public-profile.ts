import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { TravelExperienceService, TravelExperienceDto } from 'src/app/proxy/experiences';
// Asegurate de importar UserData si lo usas, aunque en tu código original no se usaba directamente en el HTML
// import { UserData } from '@abp/ng.identity/proxy'; 

@Component({
  selector: 'app-public-profile',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './public-profile.html', // Corregí el nombre para que coincida con el estándar
  styleUrls: ['./public-profile.scss']
})
export class PublicProfileComponent implements OnInit {
  
  // Inyecciones
  private route = inject(ActivatedRoute);
  private experienceService = inject(TravelExperienceService);

  // Variables de Estado
  userId: string = '';
  userName: string = 'Viajero'; 
  experiences: TravelExperienceDto[] = [];
  isLoading = true;

  // Estadísticas
  totalReviews = 0;
  averageRating = 0;

  ngOnInit(): void {
    this.userId = this.route.snapshot.params['id'];
    if (this.userId) {
      this.loadUserProfile();
    }
  }

  loadUserProfile() {
    this.isLoading = true;

    // Pedimos las experiencias filtradas por este UserId
    this.experienceService.getList({ userId: this.userId } as any).subscribe({
      next: (response) => {
        this.experiences = response.items;
        this.totalReviews = response.totalCount;
        
        if (this.experiences.length > 0) {
          // Sacamos el nombre del primer resultado
          this.userName = this.experiences[0].userName || 'Usuario Anónimo';
          
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

  // --- HELPERS VISUALES ---

  // Genera un color consistente basado en el nombre
  getAvatarColor(name: string): string {
    if (!name) return '#18427D';
    const colors = ['#F28C28', '#18427D', '#E74C3C', '#2ECC71', '#9B59B6', '#F39C12', '#1ABC9C'];
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    const index = Math.abs(hash % colors.length);
    return colors[index];
  }

  // Helper para iterar estrellas en el HTML
  getStars(rating: number): number[] {
    return Array(5).fill(0).map((x, i) => i + 1); // [1,2,3,4,5]
  }
}