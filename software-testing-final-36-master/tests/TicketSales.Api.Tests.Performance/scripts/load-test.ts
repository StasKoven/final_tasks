// Load test: Ramp to 20 VUs - test events list with filtering under sustained load
import { check, sleep } from "k6";
import { Options } from "k6/options";
import { THRESHOLDS, EventResponse, TicketResponse, parseBody, randomEmail, futureDate } from "../helpers/config.ts";
import {
  getEvents,
  createEvent,
  getEventById,
  purchaseTickets,
  getAttendees,
} from "../helpers/api-client.ts";

export const options: Options = {
  stages: [
    { duration: "1m", target: 10 },
    { duration: "3m", target: 20 },
    { duration: "1m", target: 0 },
  ],
  thresholds: THRESHOLDS,
};

const VENUE_ID = parseInt(__ENV.VENUE_ID || "1");

export default function () {
  // Heavily test the events list endpoint (most common read path)
  const listRes = getEvents();
  check(listRes, { "GET /api/events -> 200": (r) => r.status === 200 });

  const filteredRes = getEvents(VENUE_ID);
  check(filteredRes, { "GET /api/events?venueId -> 200": (r) => r.status === 200 });

  // Create event + purchase tickets
  const createRes = createEvent({
    title: `Load Event ${__VU}-${__ITER}`,
    description: "Load testing event submission endpoint with concurrent users",
    venueId: VENUE_ID,
    date: futureDate(30 + __ITER),
    startTime: "18:00:00",
    endTime: "22:00:00",
    totalTickets: 50,
    ticketPrice: 25.0,
  });
  check(createRes, { "POST /api/events -> 201": (r) => r.status === 201 });

  if (createRes.status === 201) {
    const event = parseBody<EventResponse>(createRes);

    const getRes = getEventById(event.id);
    check(getRes, { "GET /api/events/{id} -> 200": (r) => r.status === 200 });

    // Purchase tickets
    const purchaseRes = purchaseTickets(event.id, {
      buyerName: `VU ${__VU}`,
      buyerEmail: randomEmail(`load-buyer-${__VU}`),
      quantity: 2,
    });
    check(purchaseRes, { "POST /api/events/{id}/tickets -> 201": (r) => r.status === 201 });

    // Get attendees
    const attendeesRes = getAttendees(event.id);
    check(attendeesRes, { "GET /api/events/{id}/attendees -> 200": (r) => r.status === 200 });
  }

  sleep(0.5);
}