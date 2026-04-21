import http, { RefinedResponse, ResponseType } from "k6/http";
import {
  ENDPOINTS,
  HEADERS,
  CreateEventRequest,
  PurchaseTicketsRequest,
} from "./config.ts";

export function getEvents(venueId?: number): RefinedResponse<ResponseType> {
  const url = venueId
    ? `${ENDPOINTS.events}?venueId=${venueId}`
    : ENDPOINTS.events;
  return http.get(url, { headers: HEADERS });
}

export function getEventById(id: number): RefinedResponse<ResponseType> {
  return http.get(ENDPOINTS.eventById(id), { headers: HEADERS });
}

export function createEvent(payload: CreateEventRequest): RefinedResponse<ResponseType> {
  return http.post(ENDPOINTS.events, JSON.stringify(payload), { headers: HEADERS });
}

export function purchaseTickets(
  eventId: number,
  payload: PurchaseTicketsRequest
): RefinedResponse<ResponseType> {
  return http.post(
    ENDPOINTS.eventTickets(eventId),
    JSON.stringify(payload),
    { headers: HEADERS }
  );
}

export function getTicketByCode(code: string): RefinedResponse<ResponseType> {
  return http.get(ENDPOINTS.ticketByCode(code), { headers: HEADERS });
}

export function useTicket(
  code: string,
  params?: Parameters<typeof http.patch>[2]
): RefinedResponse<ResponseType> {
  return http.patch(ENDPOINTS.useTicket(code), null, { headers: HEADERS, ...params });
}

export function getAttendees(eventId: number): RefinedResponse<ResponseType> {
  return http.get(ENDPOINTS.eventAttendees(eventId), { headers: HEADERS });
}

