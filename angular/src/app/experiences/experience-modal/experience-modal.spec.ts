import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExperienceModal } from './experience-modal';

describe('ExperienceModal', () => {
  let component: ExperienceModal;
  let fixture: ComponentFixture<ExperienceModal>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExperienceModal]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ExperienceModal);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
