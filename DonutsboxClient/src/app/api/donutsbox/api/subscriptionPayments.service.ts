/**
 * Donutsbox.Api
 *
 *
 *
 * NOTE: This class is partially inspired by the auto generated API services.
 */
/* tslint:disable:no-unused-variable member-ordering */

import { Inject, Injectable, Optional } from '@angular/core';
import { HttpClient, HttpContext, HttpEvent, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';

import { SubscriptionPaymentRequestDto } from '../model/subscriptionPaymentRequestDto';
import { SubscriptionPaymentResponseDto } from '../model/subscriptionPaymentResponseDto';
import { SubscriptionPaymentStatusDto } from '../model/subscriptionPaymentStatusDto';

import { BASE_PATH } from '../variables';
import { Configuration } from '../configuration';
import { BaseService } from '../api.base.service';

@Injectable({
  providedIn: 'root'
})
export class SubscriptionPaymentsService extends BaseService {
  constructor(
    protected httpClient: HttpClient,
    @Optional() @Inject(BASE_PATH) basePath?: string | string[],
    @Optional() configuration?: Configuration
  ) {
    super(basePath, configuration);
  }

  /**
   * Создать платеж YooKassa для подписки
   */
  public apiPaymentsSubscriptionsPost(
    subscriptionPaymentRequestDto: SubscriptionPaymentRequestDto,
    observe?: 'body',
    reportProgress?: boolean,
    options?: {
      httpHeaderAccept?: 'application/json';
      context?: HttpContext;
      transferCache?: boolean;
    }
  ): Observable<SubscriptionPaymentResponseDto>;
  public apiPaymentsSubscriptionsPost(
    subscriptionPaymentRequestDto: SubscriptionPaymentRequestDto,
    observe?: 'response',
    reportProgress?: boolean,
    options?: {
      httpHeaderAccept?: 'application/json';
      context?: HttpContext;
      transferCache?: boolean;
    }
  ): Observable<HttpResponse<SubscriptionPaymentResponseDto>>;
  public apiPaymentsSubscriptionsPost(
    subscriptionPaymentRequestDto: SubscriptionPaymentRequestDto,
    observe?: 'events',
    reportProgress?: boolean,
    options?: {
      httpHeaderAccept?: 'application/json';
      context?: HttpContext;
      transferCache?: boolean;
    }
  ): Observable<HttpEvent<SubscriptionPaymentResponseDto>>;
  public apiPaymentsSubscriptionsPost(
    subscriptionPaymentRequestDto: SubscriptionPaymentRequestDto,
    observe: any = 'body',
    reportProgress: boolean = false,
    options?: {
      httpHeaderAccept?: 'application/json';
      context?: HttpContext;
      transferCache?: boolean;
    }
  ): Observable<any> {
    if (subscriptionPaymentRequestDto === null || subscriptionPaymentRequestDto === undefined) {
      throw new Error('Required parameter subscriptionPaymentRequestDto was null or undefined when calling apiPaymentsSubscriptionsPost.');
    }

    let localVarHeaders = this.defaultHeaders;
    localVarHeaders = this.configuration.addCredentialToHeaders('Bearer', 'Authorization', localVarHeaders, 'Bearer ');

    const localVarHttpHeaderAcceptSelected: string | undefined =
      options?.httpHeaderAccept ?? this.configuration.selectHeaderAccept(['application/json']);
    if (localVarHttpHeaderAcceptSelected !== undefined) {
      localVarHeaders = localVarHeaders.set('Accept', localVarHttpHeaderAcceptSelected);
    }

    const localVarHttpContext: HttpContext = options?.context ?? new HttpContext();
    const localVarTransferCache: boolean = options?.transferCache ?? true;

    const consumes: string[] = ['application/json'];
    const httpContentTypeSelected: string | undefined = this.configuration.selectHeaderContentType(consumes);
    if (httpContentTypeSelected !== undefined) {
      localVarHeaders = localVarHeaders.set('Content-Type', httpContentTypeSelected);
    }

    let responseType_: 'text' | 'json' | 'blob' = 'json';
    if (localVarHttpHeaderAcceptSelected) {
      if (localVarHttpHeaderAcceptSelected.startsWith('text')) {
        responseType_ = 'text';
      } else if (this.configuration.isJsonMime(localVarHttpHeaderAcceptSelected)) {
        responseType_ = 'json';
      } else {
        responseType_ = 'blob';
      }
    }

    const localVarPath = `/api/Payments/subscriptions`;
    const { basePath, withCredentials } = this.configuration;
    return this.httpClient.request<SubscriptionPaymentResponseDto>('post', `${basePath}${localVarPath}`, {
      context: localVarHttpContext,
      body: subscriptionPaymentRequestDto,
      responseType: responseType_ as any,
      ...(withCredentials ? { withCredentials } : {}),
      headers: localVarHeaders,
      observe,
      transferCache: localVarTransferCache,
      reportProgress
    });
  }

  /**
   * Получить статус платежа подписки
   */
  public apiPaymentsSubscriptionsPaymentRequestIdGet(
    paymentRequestId: string,
    observe?: 'body',
    reportProgress?: boolean,
    options?: {
      httpHeaderAccept?: 'application/json';
      context?: HttpContext;
      transferCache?: boolean;
    }
  ): Observable<SubscriptionPaymentStatusDto>;
  public apiPaymentsSubscriptionsPaymentRequestIdGet(
    paymentRequestId: string,
    observe?: 'response',
    reportProgress?: boolean,
    options?: {
      httpHeaderAccept?: 'application/json';
      context?: HttpContext;
      transferCache?: boolean;
    }
  ): Observable<HttpResponse<SubscriptionPaymentStatusDto>>;
  public apiPaymentsSubscriptionsPaymentRequestIdGet(
    paymentRequestId: string,
    observe?: 'events',
    reportProgress?: boolean,
    options?: {
      httpHeaderAccept?: 'application/json';
      context?: HttpContext;
      transferCache?: boolean;
    }
  ): Observable<HttpEvent<SubscriptionPaymentStatusDto>>;
  public apiPaymentsSubscriptionsPaymentRequestIdGet(
    paymentRequestId: string,
    observe: any = 'body',
    reportProgress: boolean = false,
    options?: {
      httpHeaderAccept?: 'application/json';
      context?: HttpContext;
      transferCache?: boolean;
    }
  ): Observable<any> {
    if (paymentRequestId === null || paymentRequestId === undefined) {
      throw new Error('Required parameter paymentRequestId was null or undefined when calling apiPaymentsSubscriptionsPaymentRequestIdGet.');
    }

    let localVarHeaders = this.defaultHeaders;
    localVarHeaders = this.configuration.addCredentialToHeaders('Bearer', 'Authorization', localVarHeaders, 'Bearer ');

    const localVarHttpHeaderAcceptSelected: string | undefined =
      options?.httpHeaderAccept ?? this.configuration.selectHeaderAccept(['application/json']);
    if (localVarHttpHeaderAcceptSelected !== undefined) {
      localVarHeaders = localVarHeaders.set('Accept', localVarHttpHeaderAcceptSelected);
    }

    const localVarHttpContext: HttpContext = options?.context ?? new HttpContext();
    const localVarTransferCache: boolean = options?.transferCache ?? true;

    let responseType_: 'text' | 'json' | 'blob' = 'json';
    if (localVarHttpHeaderAcceptSelected) {
      if (localVarHttpHeaderAcceptSelected.startsWith('text')) {
        responseType_ = 'text';
      } else if (this.configuration.isJsonMime(localVarHttpHeaderAcceptSelected)) {
        responseType_ = 'json';
      } else {
        responseType_ = 'blob';
      }
    }

    const localVarPath = `/api/Payments/subscriptions/${encodeURIComponent(String(paymentRequestId))}`;
    const { basePath, withCredentials } = this.configuration;
    return this.httpClient.request<SubscriptionPaymentStatusDto>('get', `${basePath}${localVarPath}`, {
      context: localVarHttpContext,
      responseType: responseType_ as any,
      ...(withCredentials ? { withCredentials } : {}),
      headers: localVarHeaders,
      observe,
      transferCache: localVarTransferCache,
      reportProgress
    });
  }
}

