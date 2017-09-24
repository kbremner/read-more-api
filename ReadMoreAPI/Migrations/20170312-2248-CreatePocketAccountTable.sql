CREATE TABLE "PocketAccounts"(
   Id			UUID PRIMARY KEY    NOT NULL default gen_random_uuid(),
   RedirectUrl  TEXT				NOT NULL,
   RequestToken	TEXT,
   AccessToken	TEXT
);