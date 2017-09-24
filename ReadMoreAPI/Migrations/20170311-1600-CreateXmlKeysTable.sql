CREATE TABLE "XmlKeys"(
   Id UUID PRIMARY KEY     NOT NULL default gen_random_uuid(),
   Xml             TEXT    NOT NULL
);