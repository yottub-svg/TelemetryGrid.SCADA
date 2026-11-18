# Windows Firewall GPO Policy

## Входящие правила

- Block: TCP 135, 139, 445 из User VLAN.
- Block: UDP 137–138 и LLMNR UDP 5355.
- Allow: HTTPS TCP 443 только от DMZ proxy к серверу TelemetryGrid.
- Allow: WinRM только от jump host к серверу и только для группы `Telemetry-Admins`.
- Block: RDP к PLC и OT-шлюзу из офисной сети.

## Исходящие правила

- Allow: АРМ → DMZ proxy TCP 443.
- Deny: АРМ → OT VLAN любой порт.
- Deny: Telemetry server → Internet.
- Allow: DNS/NTP только к утверждённым внутренним серверам.

## Управление

GPO связывается с OU серверов и рабочих мест, правила наследуются с запретом локального переопределения. После применения выполнить `gpupdate /force`, снять `Get-NetFirewallRule` и сохранить экспорт `netsh advfirewall export` в журнал изменения.
