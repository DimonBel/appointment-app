import type { Slot } from '../types/api';

// Demo data for development and testing
export const MOCK_SLOTS: Slot[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    date: '2026-02-13T09:00:00.000Z',
    doctorId: '11111111-1111-1111-1111-111111111111',
    doctorName: 'Dr. Sarah Johnson',
    cost: 150,
    isReserved: false
  },
  {
    id: '22222222-2222-2222-2222-222222222222',
    date: '2026-02-13T10:30:00.000Z',
    doctorId: '22222222-2222-2222-2222-222222222222',
    doctorName: 'Dr. Michael Chen',
    cost: 125,
    isReserved: false
  },
  {
    id: '33333333-3333-3333-3333-333333333333',
    date: '2026-02-13T14:00:00.000Z',
    doctorId: '33333333-3333-3333-3333-333333333333',
    doctorName: 'Dr. Emily Rodriguez',
    cost: 100,
    isReserved: false
  },
  {
    id: '44444444-4444-4444-4444-444444444444',
    date: '2026-02-14T09:30:00.000Z',
    doctorId: '44444444-4444-4444-4444-444444444444',
    doctorName: 'Dr. James Williams',
    cost: 200,
    isReserved: false
  },
  {
    id: '55555555-5555-5555-5555-555555555555',
    date: '2026-02-14T11:00:00.000Z',
    doctorId: '55555555-5555-5555-5555-555555555555',
    doctorName: 'Dr. Olivia Martinez',
    cost: 175,
    isReserved: false
  },
  {
    id: '66666666-6666-6666-6666-666666666666',
    date: '2026-02-14T15:30:00.000Z',
    doctorId: '66666666-6666-6666-6666-666666666666',
    doctorName: 'Dr. David Kim',
    cost: 150,
    isReserved: false
  },
  {
    id: '77777777-7777-7777-7777-777777777777',
    date: '2026-02-15T10:00:00.000Z',
    doctorId: '77777777-7777-7777-7777-777777777777',
    doctorName: 'Dr. Jessica Taylor',
    cost: 125,
    isReserved: false
  },
  {
    id: '88888888-8888-8888-8888-888888888888',
    date: '2026-02-15T13:00:00.000Z',
    doctorId: '88888888-8888-8888-8888-888888888888',
    doctorName: 'Dr. Robert Anderson',
    cost: 100,
    isReserved: false
  },
  {
    id: '11111111-1111-1111-1111-111111111112',
    date: '2026-02-16T09:00:00.000Z',
    doctorId: '11111111-1111-1111-1111-111111111111',
    doctorName: 'Dr. Sarah Johnson',
    cost: 150,
    isReserved: false
  },
  {
    id: '22222222-2222-2222-2222-222222222223',
    date: '2026-02-16T14:30:00.000Z',
    doctorId: '22222222-2222-2222-2222-222222222222',
    doctorName: 'Dr. Michael Chen',
    cost: 125,
    isReserved: false
  },
  {
    id: '33333333-3333-3333-3333-333333333334',
    date: '2026-02-17T10:30:00.000Z',
    doctorId: '33333333-3333-3333-3333-333333333333',
    doctorName: 'Dr. Emily Rodriguez',
    cost: 100,
    isReserved: false
  },
  {
    id: '44444444-4444-4444-4444-444444444445',
    date: '2026-02-17T15:00:00.000Z',
    doctorId: '44444444-4444-4444-4444-444444444444',
    doctorName: 'Dr. James Williams',
    cost: 200,
    isReserved: false
  },
  {
    id: '55555555-5555-5555-5555-555555555556',
    date: '2026-02-18T09:30:00.000Z',
    doctorId: '55555555-5555-5555-5555-555555555555',
    doctorName: 'Dr. Olivia Martinez',
    cost: 175,
    isReserved: true
  },
  {
    id: '66666666-6666-6666-6666-666666666667',
    date: '2026-02-18T13:30:00.000Z',
    doctorId: '66666666-6666-6666-6666-666666666666',
    doctorName: 'Dr. David Kim',
    cost: 150,
    isReserved: false
  },
  {
    id: '77777777-7777-7777-7777-777777777778',
    date: '2026-02-19T11:00:00.000Z',
    doctorId: '77777777-7777-7777-7777-777777777777',
    doctorName: 'Dr. Jessica Taylor',
    cost: 125,
    isReserved: true
  },
  {
    id: '88888888-8888-8888-8888-888888888889',
    date: '2026-02-19T16:00:00.000Z',
    doctorId: '88888888-8888-8888-8888-888888888888',
    doctorName: 'Dr. Robert Anderson',
    cost: 100,
    isReserved: false
  },
  {
    id: '11111111-1111-1111-1111-111111111113',
    date: '2026-02-20T10:00:00.000Z',
    doctorId: '11111111-1111-1111-1111-111111111111',
    doctorName: 'Dr. Sarah Johnson',
    cost: 150,
    isReserved: false
  },
  {
    id: '22222222-2222-2222-2222-222222222224',
    date: '2026-02-20T14:00:00.000Z',
    doctorId: '22222222-2222-2222-2222-222222222222',
    doctorName: 'Dr. Michael Chen',
    cost: 125,
    isReserved: false
  },
  {
    id: '33333333-3333-3333-3333-333333333335',
    date: '2026-02-21T09:00:00.000Z',
    doctorId: '33333333-3333-3333-3333-333333333333',
    doctorName: 'Dr. Emily Rodriguez',
    cost: 100,
    isReserved: false
  },
  {
    id: '44444444-4444-4444-4444-444444444446',
    date: '2026-02-21T15:30:00.000Z',
    doctorId: '44444444-4444-4444-4444-444444444444',
    doctorName: 'Dr. James Williams',
    cost: 200,
    isReserved: false
  }
];

export const USE_MOCK_DATA = true; // Set to false to use real API
