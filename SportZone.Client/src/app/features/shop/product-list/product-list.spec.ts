import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { ProductList } from './product-list';

describe('ProductList loading', () => {
  const params = new BehaviorSubject(convertToParamMap({ id: 'products' }));
  let http: HttpTestingController;
  beforeEach(() => {
    params.next(convertToParamMap({ id: 'products' }));
    TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params.asObservable() } }],
    });
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());
  it('waits for one response and does not refetch when it arrives', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    expect(fixture.componentInstance.isLoading()).toBeTrue();
    http.expectOne(req => req.url.endsWith('/products')).flush([
      { id: 1, name: 'Gội', categoryId: 1, price: 100, productType: 'single' },
    ]);
    fixture.detectChanges();
    expect(fixture.componentInstance.products().length).toBe(1);
    expect(fixture.componentInstance.isLoading()).toBeFalse();
    http.expectNone(req => req.url.endsWith('/products'));
  });
  it('cancels stale requests when the category changes', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    const stale = http.expectOne(req => req.url.endsWith('/products'));
    params.next(convertToParamMap({ id: 'combos' }));
    fixture.detectChanges();
    expect(stale.cancelled).toBeTrue();
    http.expectOne(req => req.url.endsWith('/products')).flush([
      { id: 1, categoryId: 1, price: 100, productType: 'single' },
      { id: 2, name: 'Combo Toàn Diện', categoryId: 6, price: 150, productType: 'single' },
    ]);
    fixture.detectChanges();
    expect(fixture.componentInstance.products().map(p => p.id)).toEqual([2]);
  });
  it('shows an error and supports retry without falling back to all products', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    http.expectOne(req => req.url.endsWith('/products')).flush('error', { status: 500, statusText: 'Error' });
    fixture.detectChanges();
    expect(fixture.componentInstance.error()).toBeTruthy();
    fixture.componentInstance.retry();
    fixture.detectChanges();
    http.expectOne(req => req.url.endsWith('/products')).flush([]);
    fixture.detectChanges();
    expect(fixture.componentInstance.error()).toBe('');
    expect(fixture.componentInstance.isLoading()).toBeFalse();
  });
});

