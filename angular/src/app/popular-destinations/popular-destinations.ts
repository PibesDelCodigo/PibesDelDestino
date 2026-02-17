import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DestinationService } from '../proxy/destinations';
import { DestinationDto } from '../proxy/application/contracts/destinations';
import { ExperienceListComponent } from '../experiences/experience-list/experience-list';
import { FavoriteService } from '../proxy/favorites';
import { AuthService } from '@abp/ng.core';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ExperienceModalComponent } from '../experiences/experience-modal/experience-modal';
@Component({
  selector: 'app-popular-destinations',
  standalone: true,
  imports: [CommonModule, ExperienceListComponent],
  templateUrl: './popular-destinations.html',
  styleUrls: ['./popular-destinations.scss']
})
export class PopularDestinationsComponent implements OnInit {

  destinations: DestinationDto[] = [];
  favoriteIds = new Set<string>();

  constructor(
    private destinationService: DestinationService,
    private favoriteService: FavoriteService,
    private authService: AuthService,
    private modalService: NgbModal 
  ) {}

  ngOnInit(): void {
    this.loadDestinations();
  }

loadDestinations() {
    this.destinationService.getList({ maxResultCount: 50 }).subscribe(response => {
      const rawList = response.items || [];
      const uniqueList = rawList.filter((item, index, self) =>
        index === self.findIndex((t) => (
          t.name === item.name 
        ))
      );

      this.destinations = uniqueList.slice(0, 10);
      
      this.checkFavoritesStatus();
    });
  }

  checkFavoritesStatus() {
    if (!this.authService.isAuthenticated) return;

    this.destinations.forEach(dest => {
      this.favoriteService.isFavorite({ destinationId: dest.id }).subscribe(isFav => {
        if (isFav) {
          this.favoriteIds.add(dest.id);
        }
      });
    });
  }

  toggleFavorite(dest: DestinationDto) {
    if (!this.authService.isAuthenticated) {
      this.authService.navigateToLogin();
      return;
    }

    this.favoriteService.toggle({ destinationId: dest.id }).subscribe(isNowFavorite => {
      if (isNowFavorite) {
        this.favoriteIds.add(dest.id);
      } else {
        this.favoriteIds.delete(dest.id);
      }
    });
  }

  rateDestination(destination: DestinationDto) {
    if (!this.authService.isAuthenticated) {
        this.authService.navigateToLogin();
        return;
    }

    const modalRef = this.modalService.open(ExperienceModalComponent, { size: 'lg' });
    modalRef.componentInstance.destinationId = destination.id;
    modalRef.componentInstance.destinationName = destination.name;
  }
}