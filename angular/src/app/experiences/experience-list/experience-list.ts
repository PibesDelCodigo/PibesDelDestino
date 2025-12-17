import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TravelExperienceService } from 'src/app/proxy/experiences';
import { FormsModule } from '@angular/forms';
import { TravelExperienceDto } from 'src/app/proxy/experiences';
import { ConfigStateService } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';

// 👇 1. IMPORTAMOS LO NECESARIO PARA EL MODAL
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
// Asegurate que esta ruta sea correcta según tu carpeta
import { ExperienceModalComponent } from '../experience-modal/experience-modal'; 

@Component({
  selector: 'app-experience-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './experience-list.html',
  styleUrls: ['./experience-list.scss']
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
    private confirmation: ConfirmationService,
    private modalService: NgbModal // 👈 2. INYECTAMOS EL MODAL SERVICE
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

  // --- FUNCIÓN ELIMINAR ---
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

  // --- 👇 3. FUNCIÓN EDITAR (NUEVA) ---
  editExperience(experience: TravelExperienceDto) {
    // Abrimos el modal
    const modalRef = this.modalService.open(ExperienceModalComponent, { size: 'lg' });

    // Le pasamos los datos: ID de destino y LA RESEÑA ENTERA
    modalRef.componentInstance.destinationId = this.destinationId;
    modalRef.componentInstance.destinationName = ''; // Opcional, si lo tenés a mano
    modalRef.componentInstance.selectedExperience = experience; // <--- CLAVE PARA QUE SEPA QUE ES EDICIÓN

    // Cuando se cierre el modal, si guardó algo, recargamos la lista
    modalRef.result.then((result) => {
        if (result) {
            this.loadExperiences();
        }
    }, () => {}); // Catch para evitar errores si cierra sin guardar
  }
}