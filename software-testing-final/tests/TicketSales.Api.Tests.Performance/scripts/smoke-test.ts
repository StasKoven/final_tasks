// Smoke test: 1 VU, 30s - verify API is alive and basic ticket purchase flow works
import { check, sleep } from "k6";
import { Options } from "k6/options";
import { THRESHOLDS, EventResponse, TicketResponse, parseBody, randomEmail, futureDate } from "../helpers/config.ts";
import {
  getEvents,
  createEvent,
  getEventById,
  purchaseTickets,
  getTicketByCode,
  useTicket,
  getAttendees,
} from "../helpers/api-client.ts";

export const options: Options = {
  vus: 1,
  duration: "30s",
  thresholds: THRESHOLDS,
};

const VENUE_ID = parseInt(__ENV.VENUE_ID || "1");

export default function () {
  // List upcoming events
  const listRes = getEvents();
  check(listRes, { "GET /api/events -> 200": (r) => r.status === 200 });

  // Create event
  const createRes = createEvent({
    title: `Smoke Event ${__VU}-${__ITER}`,
    description: "Smoke test event",
    venueId: VENUE_ID,
    date: futureDate(60 + __ITER),
    startTime: "10:00:00",
    endTime: "14:00:00",
    totalTickets: 100,
    ticketPrice: 0,
  });
  check(createRes, { "POST /api/events -> 201": (r) => r.status === 201 });

  if (createRes.status === 201) {
    const event = parseBody<EventResponse>(createRes);

    // Get event by id
    const getRes = getEventById(event.id);
    check(getRes, { "GET /api/events/{id} -> 200": (r) => r.status === 200 });

    // Purchase ticket
    const purchaseRes = purchaseTickets(event.id, {
      buyerName: "Smoke Tester",
      buyerEmail: randomEmail("smoke-buyer"),
      quantity: 1,
    });
    check(purchaseRes, { "POST /api/events/{id}/tickets -> 201": (r) => r.status === 201 });

    if (purchaseRes.status === 201) {
      const ticketsArr = parseBody<TicketResponse[]>(purchaseRes);
      const code = ticketsArr[0].ticketCode;

      // Validate ticket
      const validateRes = getTicketByCode(code);
      check(validateRes, { "GET /api/tickets/{code} -> 200": (r) => r.status === 200 });

      // Use ticket (gate scanning)
      const useRes = useTicket(code);
      check(useRes, { "PATCH /api/tickets/{code}/use -> 200": (r) => r.status === 200 });

      // Try to use again (should fail - double-scan prevention)
      const reUseRes = useTicket(code);
      check(reUseRes, { "PATCH /api/tickets/{code}/use (duplicate) -> 409": (r) => r.status === 409 });
    }

    // Get attendees
    const attendeesRes = getAttendees(event.id);
    check(attendeesRes, { "GET /api/events/{id}/attendees -> 200": (r) => r.status === 200 });
  }

  sleep(1);
}