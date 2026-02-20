import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router'; 
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { TravelExperienceService, TravelExperienceDto } from 'src/app/proxy/experiences';
import { TranslationService } from 'src/app/proxy/translation';
import { ConfigStateService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared'; // Saqué ConfirmationService si no lo usás en otro lado
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
    private modalService: NgbModal
  ) {}

  get currentUserId(): string {
    return this.config.getOne('currentUser')?.id;
  }

  get hasAlreadyReviewed(): boolean {
    if (!this.currentUserId || !this.experiences.length) return false;
    return this.experiences.some(exp => exp.userId === this.currentUserId);
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
        this.isLoading = false;
        console.error(err);
      }
    });
  }

  createExperience() { 
    const modalRef = this.modalService.open(ExperienceModalComponent, { size: 'lg', centered: true });
    modalRef.componentInstance.destinationId = this.destinationId;
    
    modalRef.result.then((result) => {
      if (result) this.loadExperiences();
    }, () => {});
  }

  editExperience(experience: TravelExperienceDto) {
    const modalRef = this.modalService.open(ExperienceModalComponent, { size: 'lg', centered: true });
    modalRef.componentInstance.destinationId = this.destinationId;
    modalRef.componentInstance.selectedExperience = experience; 

    modalRef.result.then((result) => {
      if (result) this.loadExperiences();
    }, () => {}); 
  }

  // MÉTODO DELETE ELIMINADO

  translate(id: string, text: string) {
    if (this.translatedTexts[id]) return;
    this.isTranslating[id] = true;
    this.translationService.translate({ textToTranslate: text, targetLanguage: 'en' }).subscribe({
      next: (res) => {
        this.translatedTexts[id] = res.translatedText;
        this.isTranslating[id] = false;
      },
      error: () => {
        this.isTranslating[id] = false;
      }
    });
  }

  goToUserProfile(userId: string | undefined, userName: string | undefined) {
    if (userId) this.router.navigate(['/profile', userId]);
  }

  getAvatarColor(name: string | undefined): string {
    if (!name) return '#ccc';
    const colors = ['#F28C28', '#18427D', '#E74C3C', '#2ECC71', '#9B59B6', '#F39C12', '#1ABC9C'];
    let hash = 0;
    for (let i = 0; i < name.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash);
    return colors[Math.abs(hash % colors.length)];
  }
}