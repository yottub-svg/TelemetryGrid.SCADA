CREATE TABLE IF NOT EXISTS telemetry_readings (
    sensor_id text NOT NULL,
    recorded_at timestamptz NOT NULL,
    value double precision NOT NULL,
    unit text NOT NULL,
    quality_code integer NOT NULL
);

SELECT create_hypertable('telemetry_readings', by_range('recorded_at'), if_not_exists => TRUE);
