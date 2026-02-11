import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, Router } from '@angular/router'; // Importamos Router
import { ConfigStateService } from '@abp/ng.core'; // Importamos para saber quién es el usuario actual
import { AppUserService } from 'src/app/proxy/users'; 
import { PublicUserDto } from 'src/app/proxy/users/models';
import { TravelExperienceService, TravelExperienceDto } from 'src/app/proxy/experiences';

@Component({
  selector: 'app-public-profile',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './public-profile.html',
  styleUrls: ['./public-profile.scss']
})
export class PublicProfileComponent implements OnInit {

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private userService = inject(AppUserService);
  private experienceService = inject(TravelExperienceService);
  private configState = inject(ConfigStateService); // Inyectado para comparar IDs

  userId = '';
  user: PublicUserDto | null = null;
  experiences: TravelExperienceDto[] = [];
  isLoading = true;
  isOwnProfile = false; // Flag para mostrar/ocultar el botón de editar

  // Datos para la Vista
  userInitial = '?';
  fullName = '';
  
  // Estadísticas (Reseñas y Promedio)
  stats = {
    reviews: 0,
    average: 0
  };

  ngOnInit() {
    this.userId = this.route.snapshot.paramMap.get('id') || '';

    if (this.userId) {
      this.checkIfIsOwnProfile(); // Verificamos propiedad
      this.loadUser();
      this.loadExperiences();
    }
  }

  checkIfIsOwnProfile() {
    // Obtenemos el ID del usuario que tiene la sesión iniciada
    const currentUserId = this.configState.getOne("currentUser")?.id;
    // Si coinciden, activamos el botón de edición
    this.isOwnProfile = currentUserId === this.userId;
  }

  goToEdit() {
    this.router.navigate(['/my-profile']);
  }

  goBack() {
    this.router.navigate(['/']);
  }

  loadUser() {
    this.userService.getPublicProfile(this.userId).subscribe({
      next: (res) => {
        this.user = res;
        
        // Inicial
        const nameSource = res.name || res.userName || '?';
        this.userInitial = nameSource.charAt(0).toUpperCase();

        // Nombre Completo
        this.fullName = `${res.name || ''} ${res.surname || ''}`.trim() || res.userName;

        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }

  loadExperiences() {
    this.experienceService.getList({ userId: this.userId } as any).subscribe({
      next: (res) => {
        this.experiences = res.items;
        
        // CALCULAR ESTADÍSTICAS
        this.stats.reviews = res.totalCount;
        
        if (this.experiences.length > 0) {
          const sum = this.experiences.reduce((acc, curr) => acc + (curr.rating || 0), 0);
          this.stats.average = sum / this.experiences.length;
        } else {
          this.stats.average = 0;
        }
      }
    });
  }
}