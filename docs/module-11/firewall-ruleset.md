# OT/IT Firewall Ruleset

Политика по умолчанию: `deny all`, разрешаются только перечисленные потоки.

| ID | Источник | Назначение | Протокол/порт | Действие | Обоснование |
|---|---|---|---|---|---|
| F-01 | PLC VLAN 130 | OT-шлюз | OPC UA TCP 4840 | allow | сбор телеметрии |
| F-02 | PLC VLAN 130 | OT-шлюз | Modbus TCP 502 | allow | legacy PLC, whitelist |
| F-03 | OT-шлюз | DMZ proxy | HTTPS 443 | allow | однонаправленная доставка |
| F-04 | IT-АРМ | DMZ proxy | HTTPS 443 | allow | чтение агрегатов |
| F-05 | Jump host | DMZ proxy | SSH/WinRM только из admin VLAN | allow | администрирование |
| F-06 | IT VLAN | PLC VLAN | any | deny + log | защита от бокового перемещения |
| F-07 | User VLAN | OT/DMZ | NetBIOS, LLMNR, SMB | deny + log | запрещённые протоколы |
| F-08 | Internet | OT/DMZ | any | deny | air-gapped контур |

Дополнительно запрещаются RDP/SSH из офисной сети к PLC. Правила применяются по IP/MAC whitelist, журналируются с временем, источником, назначением и решением firewall.

## Проверки

- `Test-NetConnection <PLC> -Port 502` с IT-АРМ должен завершаться отказом.
- HTTPS к DMZ proxy с read-only АРМ должен проходить.
- Прямое соединение PLC → IT VLAN отсутствует в таблице маршрутизации.
- NetBIOS/LLMNR/SMB блокируются и попадают в журнал.
