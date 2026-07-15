# Substation SCADA I/O reference

A realistic example I/O list for a 110 kV feeder + power-transformer bay, for
seeding/populating the SCADA demo with believable tags. Addresses follow the
classic **Modbus** convention: `0xxxx` coils (DO), `1xxxx` discrete inputs (DI),
`3xxxx` input registers (AI), `4xxxx` holding registers (AO).

## I/O points

| Signal | Type | Units | Nominal / range | Address |
|--------|------|-------|-----------------|---------|
| Bus voltage (busbar A) | AI | kV | 110 (100–121) | 30001 |
| Line current (feeder) | AI | A | 0–1200 (~600) | 30002 |
| Active power | AI | MW | 0–100 (~60) | 30003 |
| Reactive power | AI | MVAr | −40..+40 | 30004 |
| System frequency | AI | Hz | 50.0 (49–51) | 30005 |
| Transformer oil temp (OTI) | AI | °C | 20–95 (~65) | 30006 |
| Transformer winding temp (WTI) | AI | °C | 20–115 (~75) | 30007 |
| OLTC tap position | AI | step | 1–17 (9) | 30008 |
| Power factor | AI | — | 0.80–1.00 (~0.95) | 30009 |
| Circuit breaker status (52), closed=1 | DI | — | 0/1 | 10001 |
| Disconnector/isolator status, closed=1 | DI | — | 0/1 | 10002 |
| Earth switch status, closed=1 | DI | — | 0/1 | 10003 |
| Overcurrent trip (50/51) | DI | — | 0/1 | 10004 |
| Differential trip (87T) | DI | — | 0/1 | 10005 |
| Buchholz relay operated | DI | — | 0/1 | 10006 |
| Breaker spring charged | DI | — | 0/1 | 10007 |
| SF6 gas pressure low | DI | — | 0/1 | 10008 |
| Breaker trip command | DO | — | pulse | 00001 |
| Breaker close command | DO | — | pulse | 00002 |
| Alarm reset / acknowledge | DO | — | pulse | 00003 |
| Voltage regulator (AVR) setpoint | AO | kV | 108–112 (110) | 40001 |
| Tap raise setpoint | AO | step | 1–17 | 40002 |
| Tap lower setpoint | AO | step | 1–17 | 40003 |

## Analog alarm limits

| Signal | Low | High | Notes |
|--------|-----|------|-------|
| Bus voltage (110 kV) | 104.5 | 115.5 | ±5% band (±10% = 99 / 121 trip) |
| Line current | — | ~1000 A | overload alarm (~1200 near rating) |
| Active power | — | ~90 MW | loading |
| Reactive power | −40 | +40 | export/import limits |
| Frequency | 49.5 | 50.5 | normal band |
| Oil temp (OTI) | — | 85 | alarm (95 = trip) |
| Winding temp (WTI) | — | 105 | alarm (115 = trip) |
| Power factor | 0.85 | — | low-PF alarm |

Digital inputs (breaker trip, 50/51, 87T, Buchholz, SF6 low) are alarm points by
nature — the `1` state raises the event directly, no threshold needed.

## Sources

- OTI/WTI setpoints: https://electricalsphere.com/oti-and-wti-in-transformer-and-its-setting/
- Substation transformer alarms: https://electrical-engineering-portal.com/substation-transformer-alarms
- ANSI device numbers (50/51/87/52): https://en.wikipedia.org/wiki/ANSI_device_numbers
- RTU I/O types & telemetry: https://en.wikipedia.org/wiki/Remote_terminal_unit
- Modbus register map convention: https://www.accuenergy.com/support/modbus-map/

_Note: real devices often pack an analog value into two 16-bit registers (32-bit
float), so a production map consumes two addresses per AI. Kept one-per-address
here for a clean teaching example._
