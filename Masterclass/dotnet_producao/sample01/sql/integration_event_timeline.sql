-- Linha do tempo das integrações, alimentada pelo consumer de eventos da plataforma (PlatformEventsConsumer).
-- Criada por migração/DBA: nenhum worker cria tabela ou índice na subida.
CREATE TABLE IF NOT EXISTS integration_event_timeline (
    kafka_topic     text        NOT NULL,
    kafka_partition integer     NOT NULL,
    kafka_offset    bigint      NOT NULL,
    control_code    uuid        NOT NULL,
    event_name      text        NOT NULL,
    event_time      timestamptz NOT NULL,
    payload         jsonb       NOT NULL,
    PRIMARY KEY (kafka_topic, kafka_partition, kafka_offset)   -- idempotência do consumer at-least-once
);

CREATE INDEX IF NOT EXISTS ix_integration_event_timeline_control_code
    ON integration_event_timeline (control_code, event_time);
