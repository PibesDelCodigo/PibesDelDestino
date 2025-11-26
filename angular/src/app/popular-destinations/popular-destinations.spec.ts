import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PopularDestinations } from './popular-destinations';

describe('PopularDestinations', () => {
  let component: PopularDestinations;
  let fixture: ComponentFixture<PopularDestinations>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PopularDestinations]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PopularDestinations);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
