# Минимальные права и сегментация портов/ролей

| Роль | Доступ | Сегмент | Запрещено |
|---|---|---|---|
| Telemetry-Viewer | чтение агрегатов через proxy 443 | IT VLAN | PLC, запись порогов, администрирование |
| Shift-Operator | чтение тревог и подтверждение AlarmEvent через API | IT VLAN | изменение сетевых правил и PLC |
| OT-Engineer | диагностика PLC через jump host | OT/DMZ | прямой доступ из User VLAN |
| Telemetry-Admin | обслуживание proxy/API по bastion | Admin VLAN | доступ к секретам приложения в открытом виде |
| Backup-Operator | backup/restore TimescaleDB | DMZ/Admin | изменение firewall и PLC |

## PostgreSQL ACL

Приложение получает отдельную роль только на `SELECT/INSERT` телеметрии. Миграции выполняются отдельным оператором deploy во время окна изменений. `pg_hba.conf` разрешает подключение только из DMZ subnet и запрещает `trust`.

## MAC/Port Security

На access-портах фиксируются MAC PLC и OT-шлюза; неизвестный MAC переводит порт в shutdown и создаёт событие безопасности. Trunk разрешает только VLAN 130/140/150, native VLAN не используется.
