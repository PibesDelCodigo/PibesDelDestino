import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { ConfigStateService } from '@abp/ng.core';
import { AppUserService } from 'src/app/proxy/users'; 
import { PublicUserDto } from 'src/app/proxy/users/models';
import { TravelExperienceService, TravelExperienceDto } from 'src/app/proxy/experiences';

@Component({
  selector: 'app-public-profile',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './public-profile.html',
  styleUrls: ['./public-profile.scss']
})
export class PublicProfileComponent implements OnInit {

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private userService = inject(AppUserService);
  private experienceService = inject(TravelExperienceService);
  private configState = inject(ConfigStateService);

  userId = '';
  user: PublicUserDto | null = null;
  experiences: TravelExperienceDto[] = [];
  isLoading = true;
  isOwnProfile = false;
  userInitial = '?';
  fullName = '';
  stats = {
    reviews: 0,
    average: 0
  };

  ngOnInit() {
    this.userId = this.route.snapshot.paramMap.get('id') || '';

    if (this.userId) {
      this.checkIfIsOwnProfile();
      this.loadUser();
      this.loadExperiences();
    }
  }

  checkIfIsOwnProfile() {
    const currentUserId = this.configState.getOne("currentUser")?.id;
    this.isOwnProfile = currentUserId === this.userId;
  }

  goToEdit() {
    this.router.navigate(['/my-profile']);
  }

  goBack() {
    this.router.navigate(['/']);
  }

  loadUser() {
    this.userService.getPublicProfile(this.userId).subscribe({
      next: (res) => {
        this.user = res;

        const nameSource = res.name || res.userName || '?';
        this.userInitial = nameSource.charAt(0).toUpperCase();
        this.fullName = `${res.name || ''} ${res.surname || ''}`.trim() || res.userName;
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }

  loadExperiences() {
    this.experienceService.getList({ userId: this.userId } as any).subscribe({
      next: (res) => {
        this.experiences = res.items;
        this.stats.reviews = res.totalCount;
        
        if (this.experiences.length > 0) {
          const sum = this.experiences.reduce((acc, curr) => acc + (curr.rating || 0), 0);
          this.stats.average = sum / this.experiences.length;
        } else {
          this.stats.average = 0;
        }
      }
    });
  }
}