import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router'; 
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { TravelExperienceService, TravelExperienceDto } from 'src/app/proxy/experiences';
import { TranslationService } from 'src/app/proxy/translation';
import { ConfigStateService } from '@abp/ng.core';
import { ConfirmationService, Confirmation, ToasterService } from '@abp/ng.theme.shared';
import { ExperienceModalComponent } from '../experience-modal/experience-modal'; 

@Component({
  selector: 'app-experience-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule], 
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
  translatedTexts: { [key: string]: string } = {}; 
  isTranslating: { [key: string]: boolean } = {};  

  private router = inject(Router);
  private translationService = inject(TranslationService);
  private toaster = inject(ToasterService);
  
  constructor(
    private experienceService: TravelExperienceService,
    private config: ConfigStateService, 
    private confirmation: ConfirmationService,
    private modalService: NgbModal
  ) {}

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

  goToUserProfile(userId: string | undefined, userName: string | undefined) {
    console.log('👉 Intentando ir al perfil de:', userName);
    console.log('🔑 ID del usuario:', userId);

    if (!userId) {
        console.error('❌ ERROR: El userId está vacío o indefinido. No se puede navegar.');
        return;
    }
    this.router.navigate(['/profile', userId]);
  }

getAvatarColor(name: string | undefined): string {
  if (!name) return '#ccc';
  const colors = ['#F28C28', '#18427D', '#E74C3C', '#2ECC71', '#9B59B6', '#F39C12', '#1ABC9C'];
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  const index = Math.abs(hash % colors.length);
  return colors[index];
}

createExperience() { 
  const modalRef = this.modalService.open(ExperienceModalComponent, { size: 'lg', centered: true });
  
  modalRef.componentInstance.destinationId = this.destinationId;
  modalRef.result.then((result) => {
    if (result === 'success') {
      this.loadExperiences();
    }
  }, () => {});
}

  translate(id: string, text: string) {
    if (this.translatedTexts[id]) return;
    this.isTranslating[id] = true;

    this.translationService.translate({ 
      textToTranslate: text, 
      targetLanguage: 'en' 
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