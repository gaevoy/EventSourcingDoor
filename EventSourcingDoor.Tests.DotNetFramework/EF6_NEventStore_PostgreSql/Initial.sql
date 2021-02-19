-- For some reason EntityFramework didn't want to create tables.

CREATE TABLE public."UserAggregate"
(
    "Version" bigint,
    "Name" text COLLATE pg_catalog."default",
    "Id" uuid NOT NULL,
    "DeletedAt" date,
    CONSTRAINT "UserAggregate_pkey" PRIMARY KEY ("Id")
)

    TABLESPACE pg_default;

ALTER TABLE public."UserAggregate"
    OWNER to postgres;