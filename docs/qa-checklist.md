# SCADA – QA checklist

Manual test plan for the WPF app. Run `dotnet run --project Scada.Wpf`.
Tip: to start completely fresh, close the app and delete
`%LOCALAPPDATA%\ScadaProject\scada.db`.

## Login & roles
- [ ] App opens the **login** screen first (not the main window).
- [ ] Wrong username/password → "Invalid username or password"; no entry.
- [ ] `admin` / `Admin!Password1` → main window opens, title shows `[admin / Admin]`.
- [ ] Closing the login window (no login) exits the app.
- [ ] As **admin**: Add, Write, Report, Filter, History, Users, Logs, Logout all visible/enabled.
- [ ] **Users** → create `op1` / `OperatorPass123!` / Operator.
- [ ] Log out → log in as `op1` → title `[op1 / Operator]`.
- [ ] As **operator**: Add disabled; no Users/Logs buttons; Remove does nothing;
      **Write and Acknowledge still work**.
- [ ] Password rules enforced (try a short password when creating a user).
- [ ] Users window: cannot delete yourself; cannot delete the last admin.

## Tags
- [ ] Grid shows Name / Type / Address / Value / Units; AI values tick each second.
- [ ] **Add** → each type (AI/AO/DI/DO/Alarm) reshapes the form.
- [ ] Scan Time disabled until "Scanning on" is ticked.
- [ ] Invalid inputs show per-field and summary errors (blank name, bad address, wrong range).
- [ ] Duplicate name and duplicate address rejected.
- [ ] AI with limits + "create alarms from limits" → Details shows a high and a low alarm.
- [ ] **Remove** asks for confirmation; removing a tag also removes its alarms.

## Alarms
- [ ] Add an AI whose value enters an alarm zone → row turns **red**.
- [ ] Details → **Acknowledge** (one or all) → row turns **yellow**.
- [ ] Value returns to normal after acknowledge → row returns to normal.
- [ ] Unacknowledged alarm stays red even if the value recovers (latching).

## Write / Report / Filter / History
- [ ] **Write** lists only DO/AO tags; AO accepts any number; DO only 0 or 1.
- [ ] **Report** → `scada-report.txt` on Desktop lists AI readings near the midpoint.
- [ ] **Filter** → blank fields ignored; tag / date / value filters narrow results; **Generate TXT** exports.
- [ ] **History** → chart of a selected AI with alarm lines + min/max/avg; updates live.

## Logs (trace bits)
- [ ] **Logs** → untick a category, Save → that event type stops appearing in `system.log`.
- [ ] Traceword number persists across a restart.

## Persistence & session
- [ ] Add tags/alarms, restart → they reload.
- [ ] Auto-logout returns to login after inactivity (test with a shortened timer).
- [ ] `system.log` records login/logout, tag changes, writes, acks, alarms.
