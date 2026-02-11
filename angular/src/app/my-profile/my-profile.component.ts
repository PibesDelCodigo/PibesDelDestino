import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToasterService, ConfirmationService, Confirmation } from '@abp/ng.theme.shared'; // Importamos ConfirmationService
import { RestService, AuthService, CoreModule, ConfigStateService } from '@abp/ng.core';
import { Router } from '@angular/router'; // Importamos Router
import { UserProfileDto, NotificationChannel, NotificationFrequency } from '../account/models/user-profile.dto';


@Component({
  selector: 'app-my-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CoreModule],
  templateUrl: './my-profile.component.html',
  styleUrls: ['./my-profile.component.scss']
})
export class MyProfileComponent implements OnInit {
  selectedTab = 0;
  personalForm: FormGroup;
  passwordForm: FormGroup;
  loading = false;

  // Enums para el HTML
  eNotificationChannel = NotificationChannel;
  eNotificationFrequency = NotificationFrequency;
   
  private fb = inject(FormBuilder);
  private rest = inject(RestService);
  private toaster = inject(ToasterService);
  private confirmation = inject(ConfirmationService); // Inyectamos confirmación
  private authService = inject(AuthService); // Inyectamos para el Logout
  private router = inject(Router);
  private configState = inject(ConfigStateService); // Para obtener info del usuario actual

  constructor() {
    this.buildForms();
  }

  ngOnInit() {
    this.loadProfile();
  }

  buildForms() {
    this.personalForm = this.fb.group({
      userName: [{ value: '', disabled: true }],
      email: ['', [Validators.required, Validators.email]],
      name: ['', Validators.required],
      surname: ['', Validators.required],
      phoneNumber: [''],
      profilePictureUrl: [''], 
      notificationChannel: [null], 
      notificationFrequency: [null],
      concurrencyStamp: [null]
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmNewPassword: ['', Validators.required]
    });
  }

  // ... (Mantenemos loadProfile, savePersonalData y changePassword igual) ...

  loadProfile() {
    this.loading = true;
    this.rest.request<void, UserProfileDto>({
      method: 'GET',
      url: '/api/account/my-profile'
    }).subscribe({
      next: (user) => {
        this.personalForm.patchValue(user);
        if (user.extraProperties) {
          this.personalForm.patchValue({
            profilePictureUrl: user.extraProperties.ProfilePictureUrl || '',
            notificationChannel: user.extraProperties.NotificationChannel,
            notificationFrequency: user.extraProperties.NotificationFrequency
          });
        }
        this.loading = false;
      },
      error: () => {
        this.toaster.error('Error al cargar perfil');
        this.loading = false;
      }
    });
  }

  savePersonalData() {
    if (this.personalForm.invalid) return;
    this.loading = true;
    const formValues = this.personalForm.getRawValue();
    const body = {
      userName: formValues.userName,
      email: formValues.email,
      name: formValues.name,
      surname: formValues.surname,
      phoneNumber: formValues.phoneNumber,
      concurrencyStamp: formValues.concurrencyStamp,
      extraProperties: {
        ProfilePictureUrl: formValues.profilePictureUrl,
        NotificationChannel: Number(formValues.notificationChannel),
        NotificationFrequency: Number(formValues.notificationFrequency)
      }
    };

    this.rest.request<any, UserProfileDto>({
      method: 'PUT',
      url: '/api/account/my-profile',
      body: body
    }).subscribe({
      next: (res) => {
        this.toaster.success('Datos actualizados correctamente');
        this.personalForm.patchValue(res);
        if (res.extraProperties) {
             this.personalForm.patchValue({
                profilePictureUrl: res.extraProperties.ProfilePictureUrl,
                notificationChannel: res.extraProperties.NotificationChannel,
                notificationFrequency: res.extraProperties.NotificationFrequency
             });
        }
        this.loading = false;
      },
      error: (err) => {
        this.toaster.error(err.error?.message || 'Error al guardar');
        this.loading = false;
      }
    });
  }

goToPublicProfile() {
  const user = this.configState.getOne("currentUser");
  if (user && user.id) {
    this.router.navigate(['/profile', user.id]);
  } else {
    this.toaster.warn('No se pudo encontrar el ID de usuario.');
  }
}

  changePassword() {
    if (this.passwordForm.invalid) return;
    const { currentPassword, newPassword, confirmNewPassword } = this.passwordForm.value;
    if (newPassword !== confirmNewPassword) {
      this.toaster.error('Las contraseñas no coinciden');
      return;
    }
    this.loading = true;
    this.rest.request({
      method: 'POST',
      url: '/api/account/my-profile/change-password',
      body: { currentPassword, newPassword }
    }).subscribe({
      next: () => {
        this.toaster.success('Contraseña cambiada');
        this.passwordForm.reset();
        this.loading = false;
      },
      error: (err) => {
        this.toaster.error(err.error?.message || 'Error al cambiar contraseña');
        this.loading = false;
      }
    });
  }

  setTab(index: number) {
    this.selectedTab = index;
  }

  // --- NUEVA FUNCIÓN: ELIMINAR CUENTA ---
  confirmDeleteAccount() {
    this.confirmation.warn(
      '¿Verdaderamente querés eliminar tu cuenta? Esta acción borrará tus favoritos y reseñas de forma permanente.',
      'Confirmar eliminación',
      { messageLocalizationParams: [] }
    ).subscribe((status: Confirmation.Status) => {
      if (status === Confirmation.Status.confirm) {
        this.loading = true;
        // Llamada al endpoint de borrado (ajustar URL según tu backend)
        this.rest.request({
          method: 'DELETE',
          url: '/api/app/user-profile/my-account' 
        }).subscribe({
          next: () => {
            this.toaster.success('Tu cuenta ha sido eliminada. ¡Esperamos verte pronto!');
            this.authService.logout().subscribe(() => {
              this.router.navigate(['/']);
            });
          },
          error: (err) => {
            this.toaster.error(err.error?.message || 'Hubo un error al eliminar la cuenta');
            this.loading = false;
          }
        });
      }
    });
  }
}