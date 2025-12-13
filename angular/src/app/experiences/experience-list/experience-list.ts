import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TravelExperienceService } from 'src/app/proxy/experiences';
import { FormsModule } from '@angular/forms';
import { TravelExperienceDto } from 'src/app/proxy/experiences';
import { ConfigStateService } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';

@Component({
  selector: 'app-experience-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './experience-list.html', // Ojo: asegurate que sea .component.html si así se llama tu archivo
  styleUrls: ['./experience-list.scss']   // Lo mismo para el scss
})
export class ExperienceListComponent implements OnInit {

  @Input() destinationId: string = '';
  
  experiences: TravelExperienceDto[] = [];
  isLoading = true;
  stars = [1, 2, 3, 4, 5];
  searchText: string = '';
  filterType: number = null; 

  constructor(
    private experienceService: TravelExperienceService,
    private config: ConfigStateService, 
    private confirmation: ConfirmationService
  ) {}

  // Getter para obtener MI ID de usuario actual
  get currentUserId(): string {
    return this.config.getOne('currentUser')?.id;
  }

  ngOnInit(): void {
    if (this.destinationId) {
      this.loadExperiences();
    }
  }

  loadExperiences() {
    this.isLoading = true;
    
    const filterInput: any = { 
      destinationId: this.destinationId,
      filterText: this.searchText || null, 
      type: this.filterType !== null ? Number(this.filterType) : null 
    };

    this.experienceService.getList(filterInput).subscribe({
      next: (response) => {
        this.experiences = response.items || [];
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error cargando reseñas', err);
        this.isLoading = false;
      }
    });
  }

  // --- NUEVA FUNCIÓN ELIMINAR ---
  delete(experience: TravelExperienceDto) {
    this.confirmation.warn('¿Seguro que querés borrar esta reseña?', 'Confirmar eliminación')
      .subscribe((status: Confirmation.Status) => {
        if (status === Confirmation.Status.confirm) {
          
          this.experienceService.delete(experience.id).subscribe(() => {
            this.loadExperiences();
          });

        }
      });
  }
}