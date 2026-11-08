import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    })
      .compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the application navigation shell', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.brand')?.textContent).toContain('KINTRAFFIC');
    expect(compiled.querySelectorAll('.nav-list a')).toHaveLength(8);
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
    expect(compiled.querySelector('.nav-index')).toBeNull();
  });

  it('should collapse and expand the navigation', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const app = fixture.componentInstance;

    app.toggleSidebar();
    fixture.detectChanges();
    expect(app.isSidebarCollapsed()).toBe(true);
    expect(fixture.nativeElement.querySelector('.app-shell')?.classList.contains('sidebar-collapsed')).toBe(true);

    app.toggleSidebar();
    fixture.detectChanges();
    expect(app.isSidebarCollapsed()).toBe(false);
  });
});
