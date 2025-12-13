import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap'; // Para cerrar el modal
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
  
  @Input() destinationId: string = ''; // Recibimos el ID del destino
  @Input() destinationName: string = ''; // Recibimos el nombre para mostrarlo

  form: FormGroup;
  isSaving = false;
  
  // Array para dibujar las 5 estrellas
  stars = [1, 2, 3, 4, 5]; 

  constructor(
    private fb: FormBuilder,
    private experienceService: TravelExperienceService,
    private activeModal: NgbActiveModal,
    private toaster: ToasterService
  ) {}

  ngOnInit(): void {
    this.buildForm();
  }

  buildForm() {
    this.form = this.fb.group({
      destinationId: [this.destinationId, Validators.required],
      title: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.required, Validators.maxLength(4000)]],
      rating: [5, [Validators.required, Validators.min(1), Validators.max(5)]], // Default 5 estrellas
      date: [new Date().toISOString(), Validators.required]
    });
  }

  // Función para cambiar el rating al hacer clic en una estrella
  setRating(star: number) {
    this.form.patchValue({ rating: star });
  }

  save() {
    if (this.form.invalid) return;

    this.isSaving = true;

    this.experienceService.create(this.form.value).subscribe({
      next: () => {
        this.toaster.success('¡Gracias por compartir tu experiencia!', 'Éxito');
        this.activeModal.close(true); // Cerramos devolviendo "true" (éxito)
      },
      error: (err) => {
        this.isSaving = false;
        this.toaster.error('Ocurrió un error al guardar.', 'Error');
        console.error(err);
      }
    });
  }

  close() {
    this.activeModal.dismiss();
  }
}