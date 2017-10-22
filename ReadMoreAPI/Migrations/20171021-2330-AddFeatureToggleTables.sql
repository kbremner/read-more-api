CREATE TABLE "FeatureToggles"(
   Id			UUID PRIMARY KEY NOT NULL default gen_random_uuid(),
   Name			TEXT	NOT NULL,
   Description	TEXT
);

CREATE TABLE "PocketAccountFeatureToggles"(
	AccountId UUID NOT NULL REFERENCES "PocketAccounts" (id),
	ToggleId UUID NOT NULL REFERENCES "FeatureToggles" (id),

	PRIMARY KEY (AccountId, ToggleId)
);