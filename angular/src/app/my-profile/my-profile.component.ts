import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToasterService } from '@abp/ng.theme.shared';
import { RestService, AuthService, CoreModule } from '@abp/ng.core';
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
      // EL CAMPO CLAVE: TEXTO SIMPLE PARA LA URL
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

  loadProfile() {
    this.loading = true;
    // GET al backend estándar de ABP
    this.rest.request<void, UserProfileDto>({
      method: 'GET',
      url: '/api/account/my-profile'
    }).subscribe({
      next: (user) => {
        this.personalForm.patchValue(user);
        
        // RECUPERAR URL: Si existe en extraProperties, la ponemos en el form
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
    
    // Armamos el objeto para guardar
    const body = {
      userName: formValues.userName,
      email: formValues.email,
      name: formValues.name,
      surname: formValues.surname,
      phoneNumber: formValues.phoneNumber,
      concurrencyStamp: formValues.concurrencyStamp,
      
      // GUARDAR URL: La metemos en extraProperties como texto
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
        // Actualizamos valores por si cambiaron
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
}