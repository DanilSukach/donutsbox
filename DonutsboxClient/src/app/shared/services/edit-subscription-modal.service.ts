import { Injectable, ComponentRef, inject } from '@angular/core';
import { Overlay, OverlayConfig, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { Subject } from 'rxjs';
import { EditSubscriptionModal } from '../components/edit-subscription-modal/edit-subscription-modal';
import { SubscriptionDto } from '@app/api/donutsbox';

@Injectable({
  providedIn: 'root'
})
export class EditSubscriptionModalService {
  private overlay = inject(Overlay);
  private overlayRef: OverlayRef | null = null;
  private componentRef: ComponentRef<EditSubscriptionModal> | null = null;

  private subscriptionUpdated$ = new Subject<void>();
  readonly subscriptionUpdated = this.subscriptionUpdated$.asObservable();

  open(subscription: SubscriptionDto): void {
    if (this.overlayRef?.hasAttached()) {
      this.close();
    }

    const config = this.getOverlayConfig();
    this.overlayRef = this.overlay.create(config);

    const portal = new ComponentPortal(EditSubscriptionModal);
    this.componentRef = this.overlayRef.attach(portal);

    // Устанавливаем подписку
    this.componentRef.instance.subscription = subscription;

    this.componentRef.instance.closeModal.subscribe(() => this.close());
    this.componentRef.instance.subscriptionUpdated.subscribe(() => {
      this.subscriptionUpdated$.next();
      this.close();
    });

    this.overlayRef.backdropClick().subscribe(() => this.close());
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
      backdropClass: 'edit-subscription-modal-backdrop',
      panelClass: 'edit-subscription-modal-panel',
      positionStrategy: this.overlay.position().global().centerHorizontally().centerVertically(),
      scrollStrategy: this.overlay.scrollStrategies.block()
    };
  }
}

