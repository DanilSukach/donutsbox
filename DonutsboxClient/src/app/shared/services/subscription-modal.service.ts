import { Injectable, inject, ComponentRef, TemplateRef } from '@angular/core';
import { Overlay, OverlayRef, OverlayConfig } from '@angular/cdk/overlay';
import { ComponentPortal, TemplatePortal } from '@angular/cdk/portal';
import { SubscriptionModal } from '../components/subscription-modal/subscription-modal';
import { AuthorRequestDto } from '@app/api/donutsbox/model/authorRequestDto';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SubscriptionModalService {
  private overlay = inject(Overlay);
  private overlayRef: OverlayRef | null = null;
  private componentRef: ComponentRef<SubscriptionModal> | null = null;
  
  private subscriptionSuccess$ = new Subject<void>();
  readonly subscriptionSuccess = this.subscriptionSuccess$.asObservable();

  open(author: AuthorRequestDto): void {
    if (this.overlayRef?.hasAttached()) {
      this.close();
    }

    const config = this.getOverlayConfig();
    this.overlayRef = this.overlay.create(config);
    
    const portal = new ComponentPortal(SubscriptionModal);
    this.componentRef = this.overlayRef.attach(portal);
    
    // Устанавливаем данные автора
    this.componentRef.instance.author = author;
    this.componentRef.instance.isOpen = true;
    
    // Подписываемся на события
    this.componentRef.instance.closeModal.subscribe(() => {
      this.close();
    });
    
    this.componentRef.instance.subscriptionSuccess.subscribe(() => {
      this.subscriptionSuccess$.next();
      // Не закрываем модальное окно автоматически, чтобы пользователь мог подписаться еще раз
      // this.close();
    });

    // Закрытие при клике на backdrop
    this.overlayRef.backdropClick().subscribe(() => {
      this.close();
    });
  }

  close(): void {
    if (this.overlayRef?.hasAttached()) {
      this.overlayRef.detach();
      this.overlayRef.dispose();
      this.overlayRef = null;
      this.componentRef = null;
    }
  }

  private getOverlayConfig(): OverlayConfig {
    return {
      hasBackdrop: true,
      backdropClass: 'subscription-modal-backdrop',
      panelClass: 'subscription-modal-panel',
      positionStrategy: this.overlay.position()
        .global()
        .centerHorizontally()
        .centerVertically(),
      scrollStrategy: this.overlay.scrollStrategies.block(),
      width: '90%',
      minWidth: '48rem',
      maxWidth: '80rem',
      maxHeight: '90vh'
    };
  }
}

