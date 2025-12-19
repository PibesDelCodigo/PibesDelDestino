import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ApiMetrics } from './api-metrics';

describe('ApiMetrics', () => {
  let component: ApiMetrics;
  let fixture: ComponentFixture<ApiMetrics>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApiMetrics]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ApiMetrics);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
