import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { TravelExperienceService } from 'src/app/proxy/experiences';
import { ToasterService } from '@abp/ng.theme.shared';

@Component({
  selector: 'app-experience-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './experience-modal.html',
  styleUrls: ['./experience-modal.scss']
})
export class ExperienceModalComponent implements OnInit {
  
  @Input() destinationId: string = '';
  @Input() destinationName: string = '';

  // 👇 1. NUEVO: Recibimos la experiencia para editar (si viene vacía, es creación)
  @Input() selectedExperience: any = null; 

  form: FormGroup;
  isSaving = false;
  stars = [1, 2, 3, 4, 5]; 

  constructor(
    private fb: FormBuilder,
    private experienceService: TravelExperienceService,
    private activeModal: NgbActiveModal,
    private toaster: ToasterService
  ) {}

  ngOnInit(): void {
    this.buildForm();

    // 👇 2. LOGICA DE RELLENADO: Si es edición, cargamos los datos
    if (this.selectedExperience) {
      this.form.patchValue({
        title: this.selectedExperience.title,
        description: this.selectedExperience.description,
        rating: this.selectedExperience.rating,
        date: this.selectedExperience.date || new Date().toISOString()
      });
    }
  }

  buildForm() {
    this.form = this.fb.group({
      destinationId: [this.destinationId, Validators.required],
      title: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.required, Validators.maxLength(4000)]],
      rating: [5, [Validators.required, Validators.min(1), Validators.max(5)]],
      date: [new Date().toISOString(), Validators.required]
    });
  }

  setRating(star: number) {
    this.form.patchValue({ rating: star });
  }

  save() {
    if (this.form.invalid) return;

    this.isSaving = true;
    const formData = this.form.value;

    // 👇 3. DECISIÓN: ¿CREAR O ACTUALIZAR?
    if (this.selectedExperience) {
      
      // --- MODO EDICIÓN (UPDATE) ---
      this.experienceService.update(this.selectedExperience.id, formData).subscribe({
        next: () => {
          this.toaster.success('¡Experiencia actualizada con éxito!', 'Guardado');
          this.activeModal.close(true);
        },
        error: (err) => {
          this.isSaving = false;
          this.toaster.error('Error al actualizar la reseña.', 'Error');
          console.error(err);
        }
      });

    } else {

      // --- MODO CREACIÓN (CREATE) ---
      this.experienceService.create(formData).subscribe({
        next: () => {
          this.toaster.success('¡Gracias por compartir tu experiencia!', 'Éxito');
          this.activeModal.close(true);
        },
        error: (err) => {
          this.isSaving = false;
          this.toaster.error('Ocurrió un error al guardar.', 'Error');
          console.error(err);
        }
      });
      
    }
  }

  close() {
    this.activeModal.dismiss();
  }
}