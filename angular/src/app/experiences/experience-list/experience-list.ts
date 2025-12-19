import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router'; // Importante para navegar
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';

// Proxies de ABP
import { TravelExperienceService, TravelExperienceDto } from 'src/app/proxy/experiences';
import { TranslationService } from 'src/app/proxy/translation'; // Servicio de traducción nuevo

// Servicios de ABP Core/Theme
import { ConfigStateService } from '@abp/ng.core';
import { ConfirmationService, Confirmation, ToasterService } from '@abp/ng.theme.shared';

// Componentes
import { ExperienceModalComponent } from '../experience-modal/experience-modal'; 

@Component({
  selector: 'app-experience-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule], // Agregamos RouterModule acá
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

  // Variables para la Traducción (Req 7.1)
  translatedTexts: { [key: string]: string } = {}; // Diccionario: ID -> Texto Traducido
  isTranslating: { [key: string]: boolean } = {};  // Diccionario: ID -> Cargando...

  // Inyecciones
  private router = inject(Router);
  private translationService = inject(TranslationService);
  private toaster = inject(ToasterService);
  
  constructor(
    private experienceService: TravelExperienceService,
    private config: ConfigStateService, 
    private confirmation: ConfirmationService,
    private modalService: NgbModal
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

  // --- FUNCIÓN PARA IR AL PERFIL PÚBLICO ---
  goToUserProfile(userId: string | undefined, userName: string | undefined) {
    console.log('👉 Intentando ir al perfil de:', userName);
    console.log('🔑 ID del usuario:', userId);

    if (!userId) {
        console.error('❌ ERROR: El userId está vacío o indefinido. No se puede navegar.');
        // Opcional: mostrar alerta
        return;
    }

    // Navegamos manualmente
    this.router.navigate(['/profile', userId]);
  }

  // --- FUNCIÓN DE TRADUCCIÓN (API EXTERNA + MÉTRICAS) ---
  translate(id: string, text: string) {
    // Si ya está traducido, no hacemos nada para no gastar API
    if (this.translatedTexts[id]) return;

    // Activamos spinner solo para esta tarjeta
    this.isTranslating[id] = true;

    this.translationService.translate({ 
      textToTranslate: text, 
      targetLanguage: 'en' // Traducimos al Inglés (o podés parametrizar esto)
    }).subscribe({
      next: (res) => {
        this.translatedTexts[id] = res.translatedText;
        this.isTranslating[id] = false;
      },
      error: (err) => {
        console.error(err);
        this.toaster.error('Error al intentar traducir el texto.');
        this.isTranslating[id] = false;
      }
    });
  }

  // --- FUNCIÓN ELIMINAR ---
  delete(experience: TravelExperienceDto) {
    this.confirmation.warn('¿Seguro que querés borrar esta reseña?', 'Confirmar eliminación')
      .subscribe((status: Confirmation.Status) => {
        if (status === Confirmation.Status.confirm) {
          this.experienceService.delete(experience.id).subscribe(() => {
            this.toaster.success('Reseña eliminada correctamente');
            this.loadExperiences();
          });
        }
      });
  }

  // --- FUNCIÓN EDITAR ---
  editExperience(experience: TravelExperienceDto) {
    const modalRef = this.modalService.open(ExperienceModalComponent, { size: 'lg' });

    modalRef.componentInstance.destinationId = this.destinationId;
    modalRef.componentInstance.destinationName = ''; 
    modalRef.componentInstance.selectedExperience = experience; 

    modalRef.result.then((result) => {
        if (result) {
            this.loadExperiences();
        }
    }, () => {}); 
  }
}