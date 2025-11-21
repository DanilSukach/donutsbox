import { Injectable, ComponentRef, inject } from '@angular/core';
import { Overlay, OverlayConfig, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { Subject } from 'rxjs';
import { CreateSubscriptionModal } from '../components/create-subscription-modal/create-subscription-modal';

@Injectable({
  providedIn: 'root'
})
export class CreateSubscriptionModalService {
  private overlay = inject(Overlay);
  private overlayRef: OverlayRef | null = null;
  private componentRef: ComponentRef<CreateSubscriptionModal> | null = null;

  private subscriptionCreated$ = new Subject<void>();
  readonly subscriptionCreated = this.subscriptionCreated$.asObservable();

  open(): void {
    if (this.overlayRef?.hasAttached()) {
      this.close();
    }

    const config = this.getOverlayConfig();
    this.overlayRef = this.overlay.create(config);

    const portal = new ComponentPortal(CreateSubscriptionModal);
    this.componentRef = this.overlayRef.attach(portal);

    this.componentRef.instance.closeModal.subscribe(() => this.close());
    this.componentRef.instance.subscriptionCreated.subscribe(() => {
      this.subscriptionCreated$.next();
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
      backdropClass: 'create-subscription-modal-backdrop',
      panelClass: 'create-subscription-modal-panel',
      positionStrategy: this.overlay.position().global().centerHorizontally().centerVertically(),
      scrollStrategy: this.overlay.scrollStrategies.block()
    };
  }
}

