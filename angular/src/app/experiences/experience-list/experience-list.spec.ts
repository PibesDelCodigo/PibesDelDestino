import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExperienceList } from './experience-list';

describe('ExperienceList', () => {
  let component: ExperienceList;
  let fixture: ComponentFixture<ExperienceList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExperienceList]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ExperienceList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
