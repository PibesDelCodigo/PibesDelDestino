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
  @Input() selectedExperience: any = null; 

  form: FormGroup;
  isSaving = false;
  stars = [1, 2, 3, 4, 5]; 

  constructor(
    private fb: FormBuilder,
    private experienceService: TravelExperienceService, // Asegurate que este proxy esté actualizado
    private activeModal: NgbActiveModal,
    private toaster: ToasterService
  ) {}

  ngOnInit(): void {
    this.buildForm();

    if (this.selectedExperience) {
      this.form.patchValue({
        destinationId: this.destinationId,
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
    this.form.get('rating').setValue(star); // Forma más limpia de setear valor
  }

  save() {
    if (this.form.invalid) return;

    this.isSaving = true;
    const formData = this.form.value;

    // MAPEANDO AL DTO DE C#
    const payload = {
      ...formData,
      score: formData.rating,       // Mapea a 'Score' en C#
      comment: formData.description  // Mapea a 'Comment' en C#
    };

    if (this.selectedExperience) {
      // MODO EDICIÓN: Llama a UpdateAsync en el back
      this.experienceService.update(this.selectedExperience.id, payload).subscribe({
        next: () => {
          this.toaster.success('¡Reseña actualizada con éxito!', 'Guardado');
          this.activeModal.close(true);
        },
        error: (err) => {
          this.isSaving = false;
          const serverMessage = err.error?.error?.message || 'Error al actualizar la reseña.';
          this.toaster.error(serverMessage, 'Error');
        }
      });
    } else {
      // MODO CREACIÓN: Ahora llama a CreateAsync en el back
      // Nota: Si usaste 'abp generate-proxy', el método se llamará 'create'
      this.experienceService.create(payload).subscribe({
        next: () => {
          this.toaster.success('¡Gracias por compartir tu experiencia!', 'Éxito');
          this.activeModal.close(true);
        },
        error: (err) => {
          this.isSaving = false;
          const serverMessage = err.error?.error?.message || 'Ocurrió un error al guardar.';
          this.toaster.error(serverMessage, 'Error');
        }
      });
    }
  }

  close() {
    this.activeModal.dismiss();
  }
}