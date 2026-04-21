// Stress test: Push to 100 VUs - concurrent ticket purchasing (flash sale scenario)
import { check, sleep } from "k6";
import { Options } from "k6/options";
import { EventResponse, TicketResponse, parseBody, randomEmail, futureDate } from "../helpers/config.ts";
import {
  getEvents,
  createEvent,
  purchaseTickets,
  useTicket,
} from "../helpers/api-client.ts";

export const options: Options = {
  stages: [
    { duration: "1m", target: 10 },
    { duration: "2m", target: 10 },
    { duration: "1m", target: 50 },
    { duration: "2m", target: 50 },
    { duration: "1m", target: 100 },
    { duration: "2m", target: 100 },
    { duration: "2m", target: 0 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<1000", "p(99)<2000"],
    http_req_failed: ["rate<0.05"],
  },
};

const VENUE_ID = parseInt(__ENV.VENUE_ID || "1");

// Pre-created event ID for flash sale simulation (set via env)
const FLASH_SALE_EVENT_ID = parseInt(__ENV.FLASH_SALE_EVENT_ID || "0");

export default function () {
  // Simulate concurrent ticket purchase (flash sale - all racing for limited tickets)
  const targetEventId = FLASH_SALE_EVENT_ID > 0
    ? FLASH_SALE_EVENT_ID
    : createFlashSaleEvent();

  if (targetEventId <= 0) {
    return;
  }

  const purchaseRes = purchaseTickets(targetEventId, {
    buyerName: `Stress Buyer ${__VU}`,
    buyerEmail: randomEmail(`stress-buyer-${__VU}`),
    quantity: 1,
  });
  check(purchaseRes, {
    "POST tickets (stress) -> 201 or 409": (r) =>
      r.status === 201 || r.status === 409,
  });

  if (purchaseRes.status === 201) {
    const ticketsArr = parseBody<TicketResponse[]>(purchaseRes);
    const code = ticketsArr[0].ticketCode;

    // Immediately scan ticket (gate entry simulation)
    const useRes = useTicket(code);
    check(useRes, {
      "PATCH use ticket (stress) -> 200 or 409": (r) =>
        r.status === 200 || r.status === 409,
    });
  }

  // Also load the events list under stress
  const listRes = getEvents();
  check(listRes, { "GET /api/events (stress) -> 200": (r) => r.status === 200 });

  sleep(0.3);
}

function createFlashSaleEvent(): number {
  const createRes = createEvent({
    title: `Flash Sale ${__VU}-${__ITER}`,
    description: "Stress test flash sale event",
    venueId: VENUE_ID,
    date: futureDate(1),
    startTime: "20:00:00",
    endTime: "23:59:00",
    totalTickets: 200,
    ticketPrice: 99.0,
  });
  if (createRes.status === 201) {
    return parseBody<EventResponse>(createRes).id;
  }
  return 0;
}